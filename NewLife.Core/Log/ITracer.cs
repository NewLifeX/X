using System.Collections.Concurrent;
using System.Net;
using System.Text;
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
    /// <summary>采样周期</summary>
    Int32 Period { get; set; }

    /// <summary>最大正常采样数。采样周期内，最多只记录指定数量的正常事件，用于绘制依赖关系</summary>
    Int32 MaxSamples { get; set; }

    /// <summary>最大异常采样数。采样周期内，最多只记录指定数量的异常事件，默认10</summary>
    Int32 MaxErrors { get; set; }

    /// <summary>超时时间。超过该时间时强制采样，毫秒</summary>
    Int32 Timeout { get; set; }

    /// <summary>最大标签长度。超过该长度时将截断，默认1024字符</summary>
    Int32 MaxTagLength { get; set; }

    /// <summary>向http/rpc请求注入TraceId的参数名，为空表示不注入，默认W3C标准的traceparent</summary>
    String AttachParameter { get; set; }
    #endregion

    /// <summary>建立Span构建器</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    ISpanBuilder BuildSpan(String name);

    /// <summary>开始一个Span</summary>
    /// <param name="name">操作名</param>
    /// <returns></returns>
    ISpan NewSpan(String name);

    /// <summary>开始一个Span，指定数据标签</summary>
    /// <param name="name">操作名</param>
    /// <param name="tag">数据</param>
    /// <returns></returns>
    ISpan NewSpan(String name, Object tag);

    /// <summary>截断所有Span构建器数据，重置集合</summary>
    /// <returns></returns>
    ISpanBuilder[] TakeAll();
}

/// <summary>性能跟踪器。轻量级APM</summary>
public class DefaultTracer : DisposeBase, ITracer, ILogFeature
{
    #region 静态
    /// <summary>全局实例。可影响X组件各模块的跟踪器</summary>
    public static ITracer Instance { get; set; }

    static DefaultTracer()
    {
        // 注册默认类型，便于Json反序列化时为接口属性创造实例
        var ioc = ObjectContainer.Current;
        ioc.AddTransient<ITracer, DefaultTracer>();
        ioc.AddTransient<ISpanBuilder, DefaultSpanBuilder>();
        ioc.AddTransient<ISpan, DefaultSpan>();
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
    public String AttachParameter { get; set; } = "traceparent";

    /// <summary>Span构建器集合</summary>
    protected ConcurrentDictionary<String, ISpanBuilder> _builders = new();

    /// <summary>采样定时器</summary>
    protected TimerX _timer;
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public DefaultTracer() { }

    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        _timer.TryDispose();

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
                foreach (var span in bd.Samples)
                {
                    WriteLog("Span Id={0} ParentId={1} TraceId={2} Tag={3} Error={4}", span.Id, span.ParentId, span.TraceId, span.Tag, span.Error);
                }
#endif
            }
        }
    }

    /// <summary>建立Span构建器</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public virtual ISpanBuilder BuildSpan(String name)
    {
        InitTimer();

        //if (name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(name));
        name ??= "";

        // http 中可能有问号，需要截断。问号开头就不管了
        var p = name.IndexOf('?');
        if (p > 0) name = name[..p];

        return _builders.GetOrAdd(name, k => OnBuildSpan(k));
    }

    /// <summary>实例化Span构建器</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    protected virtual ISpanBuilder OnBuildSpan(String name) => new DefaultSpanBuilder(this, name);

    /// <summary>开始一个Span</summary>
    /// <param name="name">操作名</param>
    /// <returns></returns>
    public virtual ISpan NewSpan(String name) => BuildSpan(name).Start();

    /// <summary>开始一个Span，指定数据标签</summary>
    /// <param name="name">操作名</param>
    /// <param name="tag">数据</param>
    /// <returns></returns>
    public virtual ISpan NewSpan(String name, Object tag)
    {
        var span = BuildSpan(name).Start();

        var len = MaxTagLength;
        if (len <= 0 || tag == null) return span;

        if (tag is String str)
            span.Tag = str.Cut(len);
        else if (tag is StringBuilder builder)
            span.Tag = builder.Length <= len ? builder.ToString() : builder.ToString(0, len);
        else if (tag != null && span is DefaultSpan ds && ds.TraceFlag > 0)
        {
            if (tag is Packet pk)
            {
                // 头尾是Xml/Json时，使用字符串格式
                if (pk.Total >= 2 && (pk[0] == '{' || pk[0] == '<' || pk[pk.Total - 1] == '}' || pk[pk.Total - 1] == '>'))
                    span.Tag = pk.ToStr(null, 0, len);
                else
                    span.Tag = pk.ToHex(len / 2);
            }
            //else if (tag is IMessage msg)
            //    span.Tag = msg.ToPacket().ToHex(len / 2);
            else
                span.Tag = tag.ToJson().Cut(len);
        }

        return span;
    }

    /// <summary>截断所有Span构建器数据，重置集合</summary>
    /// <returns></returns>
    public virtual ISpanBuilder[] TakeAll()
    {
        var bs = _builders;
        if (!bs.Any()) return null;

        _builders = new ConcurrentDictionary<String, ISpanBuilder>();

        var bs2 = bs.Values.Where(e => e.Total > 0).ToArray();

        // 设置结束时间
        foreach (var item in bs2)
        {
            item.EndTime = DateTime.UtcNow.ToLong();
        }

        return bs2;
    }
    #endregion

    #region 日志
    /// <summary>日志</summary>
    public ILog Log { get; set; } = Logger.Null;

    /// <summary>写日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
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
    public static HttpClient CreateHttpClient(this ITracer tracer, HttpMessageHandler handler = null)
    {
        handler ??= HttpHelper.CreateHandler(false, false);

        var client = tracer == null ?
            new HttpClient(handler) :
            new HttpClient(new HttpTraceHandler(handler) { Tracer = tracer });

        //// 默认UserAgent
        //client.SetUserAgent();

        return client;
    }

    /// <summary>为Http请求创建Span</summary>
    /// <param name="tracer">跟踪器</param>
    /// <param name="request">Http请求</param>
    /// <returns></returns>
    public static ISpan NewSpan(this ITracer tracer, HttpRequestMessage request)
    {
        if (tracer == null) return null;

        var span = CreateSpan(tracer, request.Method.Method, request.RequestUri, request);
        span.Attach(request);

        return span;
    }

    /// <summary>支持作为标签数据的内容类型</summary>
    static String[] _TagTypes = new[] {
        "text/plain", "text/xml", "application/json", "application/xml", "application/x-www-form-urlencoded"
    };
    static String[] _ExcludeHeaders = new[] { "traceparent", "Cookie" };
    private static ISpan CreateSpan(ITracer tracer, String method, Uri uri, HttpRequestMessage request)
    {
        var url = uri.ToString();

        // 太长的Url分段，不适合作为埋点名称
        if (url.Length > 20 + 16)
        {
            var ss = url.Split('/', '?');
            // 从第三段开始查，跳过开头的http://和域名
            for (var i = 3; i < ss.Length; i++)
            {
                if (ss[i].Length > 16)
                {
                    url = ss.Take(i).Join("/");
                    break;
                }
            }
        }

        var p1 = url.IndexOf('?');
        var span = tracer.NewSpan(p1 < 0 ? url : url[..p1]);
        var tag = $"{method} {uri}";

        if (span is DefaultSpan ds && ds.TraceFlag > 0 && request != null)
        {
            var maxLength = ds.Builder?.Tracer?.MaxTagLength ?? 1024;
            if (request.Content is ByteArrayContent content &&
                content.Headers.ContentLength != null &&
                content.Headers.ContentLength < 1024 * 8 &&
                content.Headers.ContentType != null &&
                content.Headers.ContentType.MediaType.StartsWithIgnoreCase(_TagTypes))
            {
                // 既然都读出来了，不管多长，都要前面1024字符
                var str = request.Content.ReadAsStringAsync().Result;
                if (!str.IsNullOrEmpty()) tag += "\r\n" + (str.Length > maxLength ? str[..maxLength] : str);
            }

            if (tag.Length < 500)
            {
                var vs = request.Headers.Where(e => !e.Key.EqualIgnoreCase(_ExcludeHeaders)).ToDictionary(e => e.Key, e => e.Value.Join(";"));
                tag += "\r\n" + vs.Join("\r\n", e => $"{e.Key}: {e.Value}");
            }
        }
        span.SetTag(tag);

        return span;
    }

    /// <summary>为Http请求创建Span</summary>
    /// <param name="tracer">跟踪器</param>
    /// <param name="request">Http请求</param>
    /// <returns></returns>
    public static ISpan NewSpan(this ITracer tracer, WebRequest request)
    {
        if (tracer == null) return null;

        var span = CreateSpan(tracer, request.Method, request.RequestUri, null);
        span.Attach(request);

        return span;
    }

    /// <summary>直接创建错误Span</summary>
    /// <param name="tracer">跟踪器</param>
    /// <param name="name">操作名</param>
    /// <param name="error">Exception 异常对象，或错误信息</param>
    /// <returns></returns>
    public static ISpan NewError(this ITracer tracer, String name, Object error)
    {
        if (tracer == null) return null;

        var span = tracer.NewSpan(name);
        if (error is Exception ex)
            span.SetError(ex, null);
        else
            span.Error = error + "";

        span.Dispose();

        return span;
    }
    #endregion
}