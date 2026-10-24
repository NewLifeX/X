using System.Collections.Concurrent;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Http;
using NewLife.Model;
using NewLife.Serialization;
using NewLife.Threading;

namespace NewLife.Log;

/// <summary>性能跟踪器。轻量级APM</summary>
public interface ITracer
{
    #region 属性
    /// <summary>采样周期。单位秒</summary>
    Int32 Period { get; set; }

    /// <summary>最大正常采样数。采样周期内，最多只记录指定数量的正常事件，用于绘制依赖关系</summary>
    Int32 MaxSamples { get; set; }

    /// <summary>最大异常采样数。采样周期内，最多只记录指定数量的异常事件，默认10</summary>
    Int32 MaxErrors { get; set; }

    /// <summary>超时时间。超过该时间时强制采样，单位毫秒</summary>
    Int32 Timeout { get; set; }

    /// <summary>最大标签长度。超过该长度时将截断，默认1024字符</summary>
    Int32 MaxTagLength { get; set; }

    /// <summary>向http/rpc请求注入TraceId的参数名，为空表示不注入，默认W3C标准的traceparent</summary>
    String? AttachParameter { get; set; }

    /// <summary>埋点解析器，扩展自定义功能</summary>
    ITracerResolver Resolver { get; set; }
    #endregion

    /// <summary>建立Span构建器</summary>
    /// <param name="name">操作名称，用于标识不同的埋点类型</param>
    /// <returns>Span构建器实例</returns>
    ISpanBuilder BuildSpan(String name);

    /// <summary>开始一个Span</summary>
    /// <param name="name">操作名称，用于标识不同的埋点类型</param>
    /// <returns>Span实例，使用完毕需要Dispose</returns>
    ISpan NewSpan(String name);

    /// <summary>开始一个Span，指定数据标签</summary>
    /// <param name="name">操作名称，用于标识不同的埋点类型</param>
    /// <param name="tag">数据标签，记录关键参数信息</param>
    /// <returns>Span实例，使用完毕需要Dispose</returns>
    ISpan NewSpan(String name, Object? tag);

    /// <summary>截断所有Span构建器数据，重置集合</summary>
    /// <returns>当前周期内所有Span构建器数组</returns>
    ISpanBuilder[] TakeAll();
}

/// <summary>性能跟踪器。轻量级APM</summary>
public class DefaultTracer : DisposeBase, ITracer, ILogFeature
{
    #region 静态
    /// <summary>全局实例。可影响X组件各模块的跟踪器</summary>
    public static ITracer? Instance { get; set; }

    static DefaultTracer()
    {
        // 注册默认类型，便于Json反序列化时为接口属性创造实例
        var ioc = ObjectContainer.Current;
        ioc.TryAddTransient<ITracer, DefaultTracer>();
        ioc.TryAddTransient<ISpanBuilder, DefaultSpanBuilder>();
        ioc.TryAddTransient<ISpan, DefaultSpan>();
    }
    #endregion

    #region 属性
    /// <summary>采样周期。默认15s</summary>
    public Int32 Period { get; set; } = 15;

    /// <summary>最大正常采样数。采样周期内，最多只记录指定数量的正常事件，用于绘制依赖关系</summary>
    public Int32 MaxSamples { get; set; } = 1;

    /// <summary>最大异常采样数。采样周期内，最多只记录指定数量的异常事件，默认10</summary>
    public Int32 MaxErrors { get; set; } = 10;

    /// <summary>超时时间。超过该时间时强制采样，默认15000毫秒</summary>
    public Int32 Timeout { get; set; } = 15000;

    /// <summary>最大标签长度。超过该长度时将截断，默认1024字符</summary>
    public Int32 MaxTagLength { get; set; } = 1024;

    /// <summary>向http/rpc请求注入TraceId的参数名，为空表示不注入，默认是W3C标准的traceparent</summary>
    public String? AttachParameter { get; set; } = "traceparent";

    /// <summary>埋点解析器，扩展自定义功能</summary>
    public ITracerResolver Resolver { get; set; } = new DefaultTracerResolver();

    /// <summary>Json序列化选项</summary>
    public JsonOptions JsonOptions { get; set; } = new JsonOptions
    {
        //CamelCase = false,
        IgnoreNullValues = false,
        IgnoreCycles = true,
        WriteIndented = false,
        FullTime = true,
        EnumString = true,
    };

    private IPool<ISpanBuilder>? _BuilderPool;
    /// <summary>片段对象池</summary>
    [IgnoreDataMember]
    public IPool<ISpanBuilder> BuilderPool => _BuilderPool ??= new MyBuilderPool(this);

    private IPool<ISpan>? _SpanPool;
    /// <summary>埋点对象池</summary>
    [IgnoreDataMember]
    public IPool<ISpan> SpanPool => _SpanPool ??= new MySpanPool();

    /// <summary>Span构建器集合</summary>
    protected ConcurrentDictionary<String, ISpanBuilder> _builders = new();

    /// <summary>构建器工厂。预先缓存委托，避免热路径上反复分配委托对象</summary>
    private readonly Func<String, ISpanBuilder> _builderFactory;

    /// <summary>采样定时器</summary>
    protected TimerX? _timer;
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public DefaultTracer() => _builderFactory = OnBuildSpan;

    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        _timer?.Dispose();

        DoProcessSpans();
    }
    #endregion

    #region 方法
    private Int32 _inited;
    private void InitTimer()
    {
        if (Interlocked.CompareExchange(ref _inited, 1, 0) == 0)
        {
            _timer ??= new TimerX(s => DoProcessSpans(), null, 5_000, Period * 1000) { Async = true };
        }
    }

    private void DoProcessSpans()
    {
        var builders = TakeAll();
        if (builders != null && builders.Length > 0)
        {
            ProcessSpans(builders);

            // 归还对象池
            foreach (var item in builders)
            {
                if (item is DefaultSpanBuilder builder)
                {
                    builder.ReturnToPool(BuilderPool);
                }
            }
        }

        // 采样周期可能改变
        if (Period > 0 && _timer != null && _timer.Period != Period * 1000) _timer.Period = Period * 1000;
    }

    /// <summary>处理Span集合。默认输出日志，可重定义输出控制台</summary>
    protected virtual void ProcessSpans(ISpanBuilder[] builders)
    {
        if (builders == null) return;

        foreach (var bd in builders)
        {
            if (bd.Total > 0)
            {
                var ms = bd.EndTime - bd.StartTime;
                var speed = ms == 0 ? 0 : bd.Total * 1000d / ms;
                WriteLog("Tracer[{0}] Total={1:n0} Errors={2:n0} Speed={3:n2}tps Cost={4:n0}ms MaxCost={5:n0}ms MinCost={6:n0}ms", bd.Name, bd.Total, bd.Errors, speed, bd.Cost / bd.Total, bd.MaxCost, bd.MinCost);

#if DEBUG
                if (bd.Samples != null)
                {
                    foreach (var span in bd.Samples)
                    {
                        WriteLog("Span Id={0} ParentId={1} TraceId={2} Tag={3} Error={4}", span.Id, span.ParentId, span.TraceId, span.Tag, span.Error);
                    }
                }
#endif
            }
        }
    }

    private static readonly Char[] _seps = ['?', '#', '&'];
    /// <summary>建立Span构建器</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public virtual ISpanBuilder BuildSpan(String name)
    {
        InitTimer();

        //if (name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(name));
        name ??= "";

        // http 中可能有问号，需要截断。问号开头就不管了
        var p = name.IndexOfAny(_seps);
        if (p > 0) name = name[..p];

        return _builders.GetOrAdd(name, _builderFactory);
    }

    /// <summary>实例化Span构建器</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    protected virtual ISpanBuilder OnBuildSpan(String name)
    {
        var builder = BuilderPool.Get();
        if (builder is DefaultSpanBuilder dsb)
            dsb.Init(this, name);
        else
            builder.Name = name;

        return builder;
    }

    /// <summary>开始一个Span</summary>
    /// <param name="name">操作名</param>
    /// <returns></returns>
    public virtual ISpan NewSpan(String name) => BuildSpan(name).Start();

    /// <summary>开始一个Span，指定数据标签</summary>
    /// <param name="name">操作名</param>
    /// <param name="tag">数据</param>
    /// <returns></returns>
    public virtual ISpan NewSpan(String name, Object? tag)
    {
        var span = BuildSpan(name).Start();

        if (tag != null)
        {
            // 仅当需要采样时才处理复杂标签
            var needSample = span is DefaultSpan ds && ds.TraceFlag > 0;
            BuildTag(span, tag, needSample);
        }

        return span;
    }

    /// <summary>构建数据标签</summary>
    /// <param name="span">跟踪片段</param>
    /// <param name="tag">标签数据</param>
    /// <param name="needSample">是否需要采样，决定是否处理复杂对象</param>
    public virtual void BuildTag(ISpan span, Object? tag, Boolean needSample)
    {
        if (tag == null) return;

        var len = MaxTagLength;
        if (len <= 0) return;

        if (tag is String str)
        {
            span.Tag = str.Cut(len);
            //span.Value = str.Length;
        }
        else if (tag is StringBuilder builder)
        {
            span.Tag = builder.Length <= len ? builder.ToString() : builder.ToString(0, len);
            span.Value = builder.Length;
        }
        else if (needSample)
        {
            if (tag is IPacket pk)
            {
                // 头尾是Xml/Json时，使用字符串格式
                var total = pk.Total;
                if (total >= 2 && (pk[0] == '{' || pk[0] == '<') && (pk[total - 1] == '}' || pk[total - 1] == '>'))
                    span.Tag = pk.ToStr(null, 0, len);
                else
                    span.Tag = pk.ToHex(len / 2);

                span.Value = total;
            }
            //else if (tag is IMessage msg)
            //    span.Tag = msg.ToPacket().ToHex(len / 2);
            else if (tag is EndPoint ep)
            {
                span.Tag = ep.ToString();
            }
            else if (tag is IPAddress addr)
            {
                span.Tag = addr.ToString();
            }
            else
            {
                // 避免数据标签的序列化抛出异常影响业务层
                try
                {
                    span.Tag = tag.ToJson(JsonOptions).Cut(len);
                }
                catch
                {
                    span.Tag = tag.ToString()?.Cut(len);
                }
            }
        }
    }

    /// <summary>截断所有Span构建器数据，重置集合</summary>
    /// <returns></returns>
    public virtual ISpanBuilder[] TakeAll()
    {
        var bs = _builders;
        if (bs.IsEmpty) return [];

        _builders = new ConcurrentDictionary<String, ISpanBuilder>();

        var bs2 = bs.Values.Where(e => e.Total > 0).ToArray();

        // 设置结束时间
        foreach (var item in bs2)
        {
            item.EndTime = Runtime.UtcNow.ToUnixTimeMilliseconds();
        }

        return bs2;
    }
    #endregion

    #region 对象池
    class MyBuilderPool(ITracer tracer) : Pool<ISpanBuilder>
    {
        protected override ISpanBuilder? OnCreate() => new DefaultSpanBuilder(tracer, null!);
    }

    class MySpanPool : Pool<ISpan>
    {
        protected override ISpan? OnCreate() => new DefaultSpan();
    }
    #endregion

    #region 日志
    /// <summary>日志</summary>
    public ILog Log { get; set; } = Logger.Null;

    /// <summary>写日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object?[] args) => Log?.Info(format, args);
    #endregion
}

/// <summary>跟踪扩展</summary>
public static class TracerExtension
{
    #region 扩展方法
    /// <summary>创建受跟踪的HttpClient</summary>
    /// <param name="tracer">跟踪器</param>
    /// <param name="handler">http处理器</param>
    /// <returns></returns>
    public static HttpClient CreateHttpClient(this ITracer? tracer, HttpMessageHandler? handler = null)
    {
        handler ??= HttpHelper.CreateHandler(false, false);

        var client = tracer == null
            ? new HttpClient(handler)
            : new HttpClient(new HttpTraceHandler(handler) { Tracer = tracer });

        //// 默认UserAgent
        //client.SetUserAgent();

        return client;
    }

    /// <summary>开始一个Span，指定数据标签和用户数值</summary>
    /// <param name="tracer">跟踪器</param>
    /// <param name="name">操作名</param>
    /// <param name="tag">数据</param>
    /// <param name="value">用户数值。星尘平台汇总统计</param>
    /// <returns></returns>
    public static ISpan NewSpan(this ITracer tracer, String name, Object? tag, Int64 value)
    {
        var span = tracer.NewSpan(name, tag);
        span?.Value = value;

        return span!;
    }

    /// <summary>为Http请求创建Span</summary>
    /// <param name="tracer">跟踪器</param>
    /// <param name="request">Http请求</param>
    /// <returns></returns>
    public static ISpan? NewSpan(this ITracer tracer, HttpRequestMessage request)
    {
        //if (tracer == null) return null;
        if (request.RequestUri == null) return null;

        var span = tracer.Resolver.CreateSpan(tracer, request.RequestUri, request);
        if (span == null) return null;

        span.Attach(request);

        var len = request?.Content?.Headers?.ContentLength;
        if (len != null) span.Value = len.Value;

        return span;
    }

    /// <summary>为Http请求创建Span</summary>
    /// <param name="tracer">跟踪器</param>
    /// <param name="request">Http请求</param>
    /// <returns></returns>
    public static ISpan? NewSpan(this ITracer tracer, WebRequest request)
    {
        //if (tracer == null) return null;

        var span = tracer.Resolver.CreateSpan(tracer, request.RequestUri, request);
        if (span == null) return null;

        span.Attach(request);

        var len = request?.Headers["Content-Length"];
        if (len != null) span.Value = len.ToInt();

        return span;
    }

    /// <summary>直接创建错误Span</summary>
    /// <param name="tracer">跟踪器</param>
    /// <param name="name">操作名</param>
    /// <param name="error">Exception 异常对象，或错误信息</param>
    /// <returns></returns>
    public static ISpan NewError(this ITracer tracer, String name, Object? error)
    {
        //if (tracer == null) return null;

        var span = tracer.NewSpan(name);
        if (error is Exception ex)
            span.SetError(ex, null);
        else
            span.Error = error + "";

        span.Dispose();

        return span;
    }

    /// <summary>直接创建错误Span</summary>
    /// <param name="tracer">跟踪器</param>
    /// <param name="name">操作名</param>
    /// <param name="error">Exception 异常对象，或错误信息</param>
    /// <param name="tag">数据标签</param>
    /// <returns></returns>
    public static ISpan NewError(this ITracer tracer, String name, Object error, Object tag)
    {
        //if (tracer == null) return null;

        var span = tracer.NewSpan(name);
        if (error is Exception ex)
            span.SetError(ex, null);
        else
            span.Error = error + "";

        span.SetTag(tag);

        span.Dispose();

        return span;
    }

    /// <summary>开始一个服务端片段，并从请求头继承上游链路</summary>
    /// <remarks>
    /// <para>星尘等跟踪器的片段标识是可变字段，先创建片段再 <c>Detach(headers)</c> 就能吸收上游链路；
    /// 而 OpenTelemetry 的活动一旦启动就无法再指定父级，必须在创建时就传入上游上下文。
    /// 本方法屏蔽这一差异，供服务端中间件统一调用。</para>
    /// <para>OpenTelemetry 只认 W3C 的 traceparent（含 NewLife RPC 使用的 TraceId 头，其值同为 traceparent 格式）；
    /// Request-Id、Eagleeye-Traceid 等历史格式不支持跨进程继承。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// using var span = Tracer.NewServerSpan(action, req.Headers);
    /// span.Tag = $"{ctx.GetUserHost()} {req.Method} {req.GetRawUrl()}";
    /// </code>
    /// </example>
    /// <param name="tracer">跟踪器</param>
    /// <param name="name">操作名称</param>
    /// <param name="headers">请求头，用于提取上游链路标识</param>
    /// <returns>片段实例，使用完毕需要 Dispose</returns>
    public static ISpan? NewServerSpan<T>(this ITracer? tracer, String name, IDictionary<String, T>? headers)
    {
        if (tracer == null) return null;

#if NET6_0_OR_GREATER
        // OpenTelemetry 的父级只能在活动创建时指定
        if (tracer is OpenTelemetryTracer otel) return otel.NewSpanFromHeaders(name, headers);
#endif

        var span = tracer.NewSpan(name);
        if (span != null && headers != null) span.Detach(headers);

        return span;
    }
    #endregion
}