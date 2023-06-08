using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Remoting;
using NewLife.Serialization;

namespace NewLife.Log;

/// <summary>性能跟踪片段。轻量级APM</summary>
public interface ISpan : IDisposable
{
    /// <summary>唯一标识。随线程上下文、Http、Rpc传递，作为内部片段的父级</summary>
    String Id { get; set; }

    /// <summary>父级片段标识</summary>
    String ParentId { get; set; }

    /// <summary>跟踪标识。可用于关联多个片段，建立依赖关系，随线程上下文、Http、Rpc传递</summary>
    String TraceId { get; set; }

    /// <summary>开始时间。Unix毫秒</summary>
    Int64 StartTime { get; set; }

    /// <summary>结束时间。Unix毫秒</summary>
    Int64 EndTime { get; set; }

    /// <summary>数据标签。记录一些附加数据</summary>
    String Tag { get; set; }

    /// <summary>错误信息</summary>
    String Error { get; set; }

    /// <summary>设置错误信息，ApiException除外</summary>
    /// <param name="ex">异常</param>
    /// <param name="tag">标签</param>
    void SetError(Exception ex, Object tag);

    /// <summary>设置数据标签。内部根据长度截断</summary>
    /// <param name="tag">标签</param>
    void SetTag(Object tag);
}

/// <summary>性能跟踪片段。轻量级APM</summary>
/// <remarks>
/// spanId/traceId采用W3C标准，https://www.w3.org/TR/trace-context/
/// </remarks>
public class DefaultSpan : ISpan
{
    #region 属性
    /// <summary>构建器</summary>
    [XmlIgnore, ScriptIgnore, IgnoreDataMember]
    public ISpanBuilder Builder { get; private set; }

    /// <summary>唯一标识。随线程上下文、Http、Rpc传递，作为内部片段的父级</summary>
    public String Id { get; set; }

    /// <summary>父级片段标识</summary>
    public String ParentId { get; set; }

    /// <summary>跟踪标识。可用于关联多个片段，建立依赖关系，随线程上下文、Http、Rpc传递</summary>
    public String TraceId { get; set; }

    /// <summary>开始时间。Unix毫秒</summary>
    public Int64 StartTime { get; set; }

    /// <summary>结束时间。Unix毫秒</summary>
    public Int64 EndTime { get; set; }

    /// <summary>数据标签。记录一些附加数据</summary>
    public String Tag { get; set; }

    ///// <summary>版本</summary>
    //public Byte Version { get; set; }

    /// <summary>跟踪标识。强制采样，确保链路采样完整，上下文传递</summary>
    public Byte TraceFlag { get; set; }

    /// <summary>错误信息</summary>
    public String Error { get; set; }

#if NET45
    private static readonly ThreadLocal<ISpan> _Current = new();
#else
    private static readonly AsyncLocal<ISpan> _Current = new();
#endif
    /// <summary>当前线程正在使用的上下文</summary>
    public static ISpan Current { get => _Current.Value; set => _Current.Value = value; }

    private ISpan _parent;
    private Int32 _finished;
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public DefaultSpan() { }

    /// <summary>实例化</summary>
    /// <param name="builder"></param>
    public DefaultSpan(ISpanBuilder builder)
    {
        Builder = builder;
        StartTime = DateTime.UtcNow.ToLong();
    }

    static DefaultSpan()
    {
        IPAddress ip;
        try
        {
            ip = NetHelper.MyIP();
        }
        catch
        {
            ip = IPAddress.Loopback;
        }
        ip ??= IPAddress.Parse("127.0.0.1");
        _myip = ip.GetAddressBytes().ToHex().ToLower().PadLeft(8, '0');
        var pid = Process.GetCurrentProcess().Id;
        _pid = (pid & 0xFFFF).ToString("x4").PadLeft(4, '0');
    }

    /// <summary>释放资源</summary>
    public void Dispose() => Finish();
    #endregion

    #region 方法
    /// <summary>设置跟踪标识</summary>
    public virtual void Start()
    {
        //if (Id.IsNullOrEmpty()) Id = Rand.NextBytes(8).ToHex().ToLower();
        if (Id.IsNullOrEmpty()) Id = CreateId();

        // 设置父级
        var span = Current;
        _parent = span;
        if (span != null && span != this)
        {
            ParentId = span.Id;
            TraceId = span.TraceId;

            // 继承跟踪标识，该TraceId下全量采样，确保链路采样完整
            if (span is DefaultSpan ds) TraceFlag = ds.TraceFlag;
        }

        // 否则创建新的跟踪标识
        if (TraceId.IsNullOrEmpty()) TraceId = CreateTraceId();

        // 设置当前片段
        Current = this;
    }

    private static readonly String _myip;
    private static Int32 _seq;
    private static Int32 _seq2;
    private static readonly String _pid;

    /// <summary>创建分片编号</summary>
    /// <returns></returns>
    protected virtual String CreateId()
    {
        // IPv4(8) + PID(4) + 顺序数(4)
        var id = Interlocked.Increment(ref _seq) & 0xFFFF;
        return _myip + _pid + id.ToString("x4").PadLeft(4, '0');
    }

    /// <summary>创建跟踪编号</summary>
    /// <returns></returns>
    protected virtual String CreateTraceId()
    {
        /*
         * 阿里云EagleEye全链路追踪
         * 7ae122a215982779017518707e
         * IPv4(8) + 毫秒时间(13) + 顺序数(4) + 标识位(1) + PID(4)
         * 7ae122a2 + 1598277901751 + 8707 + e
         */

        var sb = Pool.StringBuilder.Get();
        sb.Append(_myip);
        sb.Append(DateTime.UtcNow.ToLong());
        var id = Interlocked.Increment(ref _seq2) & 0xFFFF;
        sb.Append(id.ToString("x4").PadLeft(4, '0'));
        sb.Append('e');
        sb.Append(_pid);

        //return _myip + DateTime.UtcNow.ToLong() + Interlocked.Increment(ref _seq) + "e" + _pid;
        return sb.Put(true);
    }

    /// <summary>完成跟踪</summary>
    protected virtual void Finish()
    {
        if (Interlocked.CompareExchange(ref _finished, 1, 0) != 0) return;

        EndTime = DateTime.UtcNow.ToLong();

        // 从本线程中清除跟踪标识
        Current = _parent;

        // Builder这一批可能已经上传，重新取一次，以防万一
        var builder = Builder.Tracer.BuildSpan(Builder.Name);
        builder.Finish(this);

        // 打断对Builder的引用，当前Span可能还被放在AsyncLocal字典中
        // 也有可能原来的Builder已经上传，现在加入了新的builder集合
        Builder = null;
    }

    /// <summary>设置错误信息，ApiException除外</summary>
    /// <param name="ex">异常</param>
    /// <param name="tag">标签</param>
    public virtual void SetError(Exception ex, Object tag)
    {
        SetTag(tag);

        if (ex != null)
        {
            var name = $"ex:{ex.GetType().Name}";

            // 业务异常，不属于异常，而是正常流程
            if (ex is ApiException aex)
            {
                name = $"ex:{ex.GetType().Name}[{aex.Code}]";
                this.AppendTag($"Api[{aex.Code}]:{aex.Message}\r\n{aex.Source}");
            }
            else
                Error = ex?.GetMessage();

            // 所有异常，独立记录埋点，便于按异常分类统计
            using var span = Builder?.Tracer?.NewSpan(name, tag);
            span?.AppendTag(ex.ToString());
            if (span != null) span.StartTime = StartTime;
        }
    }

    /// <summary>设置数据标签。内部根据长度截断</summary>
    /// <param name="tag">标签</param>
    public virtual void SetTag(Object tag)
    {
        if (tag == null) return;

        var len = Builder?.Tracer?.MaxTagLength ?? 0;
        if (len <= 0) return;

        if (tag is String str)
            Tag = str.Cut(len);
        else if (tag is StringBuilder builder)
            Tag = builder.Length <= len ? builder.ToString() : builder.ToString(0, len);
        else if (tag is Packet pk)
        {
            // 头尾是Xml/Json时，使用字符串格式
            if (pk.Total >= 2 && (pk[0] == '{' || pk[0] == '<' || pk[pk.Total - 1] == '}' || pk[pk.Total - 1] == '>'))
                Tag = pk.ToStr(null, 0, len);
            else
                Tag = pk.ToHex(len / 2);
        }
        else
            Tag = tag.ToJson().Cut(len);
    }

    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => $"00-{TraceId}-{Id}-{TraceFlag:x2}";
    #endregion
}

/// <summary>跟踪片段扩展</summary>
public static class SpanExtension
{
    #region 扩展方法
    private static String GetAttachParameter(ISpan span)
    {
        var builder = (span as DefaultSpan)?.Builder;
        var tracer = (builder as DefaultSpanBuilder)?.Tracer;
        return tracer?.AttachParameter;
    }

    /// <summary>把片段信息附加到http请求头上</summary>
    /// <param name="span">片段</param>
    /// <param name="request">http请求</param>
    /// <returns></returns>
    public static HttpRequestMessage Attach(this ISpan span, HttpRequestMessage request)
    {
        if (span == null || request == null) return request;

        // 注入参数名
        var name = GetAttachParameter(span);
        if (name.IsNullOrEmpty()) return request;

        var headers = request.Headers;
        if (!headers.Contains(name)) headers.Add(name, span.ToString());

        return request;
    }

    ///// <summary>把片段信息附加到http请求头上</summary>
    ///// <param name="span">片段</param>
    ///// <param name="headers">http请求头</param>
    ///// <returns></returns>
    //public static HttpRequestHeaders Attach(this ISpan span, HttpRequestHeaders headers)
    //{
    //    if (span == null || headers == null) return headers;

    //    // 注入参数名
    //    var name = GetAttachParameter(span);
    //    if (name.IsNullOrEmpty()) return headers;

    //    if (!headers.Contains(name)) headers.Add(name, span.ToString());

    //    return headers;
    //}

    /// <summary>把片段信息附加到http请求头上</summary>
    /// <param name="span">片段</param>
    /// <param name="request">http请求</param>
    /// <returns></returns>
    public static WebRequest Attach(this ISpan span, WebRequest request)
    {
        if (span == null || request == null) return request;

        // 注入参数名
        var name = GetAttachParameter(span);
        if (name.IsNullOrEmpty()) return request;

        var headers = request.Headers;
        if (!headers.AllKeys.Contains(name)) headers.Add(name, span.ToString());

        return request;
    }

    /// <summary>把片段信息附加到api请求头上</summary>
    /// <param name="span">片段</param>
    /// <param name="args">api请求参数</param>
    /// <returns></returns>
    public static Object Attach(this ISpan span, Object args)
    {
        if (span == null || args == null || args is Packet || args is Byte[] || args is IAccessor) return args;
        if (Type.GetTypeCode(args.GetType()) != TypeCode.Object) return args;

        // 注入参数名
        var name = GetAttachParameter(span);
        if (name.IsNullOrEmpty()) return args;

        var headers = args.ToDictionary();
        if (!headers.ContainsKey(name)) headers.Add(name, span.ToString());

        return headers;
    }

    /// <summary>从http请求头释放片段信息</summary>
    /// <param name="span">片段</param>
    /// <param name="headers">http请求头</param>
    public static void Detach(this ISpan span, NameValueCollection headers)
    {
        if (span == null || headers == null || headers.Count == 0) return;

        // 不区分大小写比较头部
        var dic = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in headers.AllKeys)
        {
            dic[item] = headers[item];
        }

        Detach2(span, dic);
    }

    /// <summary>从api请求释放片段信息</summary>
    /// <param name="span">片段</param>
    /// <param name="parameters">参数</param>
    public static void Detach(this ISpan span, IDictionary<String, Object> parameters)
    {
        if (span == null || parameters == null || parameters.Count == 0) return;

        // 不区分大小写比较头部
        var dic = parameters.ToDictionary(e => e.Key, e => e.Value, StringComparer.OrdinalIgnoreCase);
        Detach2(span, dic);
    }

    /// <summary>从api请求释放片段信息</summary>
    /// <param name="span">片段</param>
    /// <param name="parameters">参数</param>
    public static void Detach<T>(this ISpan span, IDictionary<String, T> parameters)
    {
        if (span == null || parameters == null || parameters.Count == 0) return;

        // 不区分大小写比较头部
        var dic = parameters.ToDictionary(e => e.Key, e => e.Value, StringComparer.OrdinalIgnoreCase);
        Detach2(span, dic);
    }

    private static void Detach2<T>(ISpan span, IDictionary<String, T> dic)
    {
        // 不区分大小写比较头部
        if (dic.TryGetValue("traceparent", out var tid))
        {
            var ss = (tid + "").Split('-');
            if (ss.Length > 1) span.TraceId = ss[1];
            if (ss.Length > 2) span.ParentId = ss[2];
            if (ss.Length > 3 && !ss[3].IsNullOrEmpty() && span is DefaultSpan ds && ds.TraceFlag == 0)
            {
                var buf = ss[3].ToHex(0, 2);
                if (buf.Length > 0) ds.TraceFlag = buf[0];
            }
        }
        else if (dic.TryGetValue("Request-Id", out tid))
        {
            // HierarchicalId编码取最后一段作为父级
            var ss = (tid + "").Split('.', '_');
            if (ss.Length > 0) span.TraceId = ss[0].TrimStart('|');
            if (ss.Length > 1) span.ParentId = ss[^1];
        }
        else if (dic.TryGetValue("Eagleeye-Traceid", out tid))
        {
            var ss = (tid + "").Split('-');
            if (ss.Length > 0) span.TraceId = ss[0];
            if (ss.Length > 1) span.ParentId = ss[1];
        }
        else if (dic.TryGetValue("TraceId", out tid))
        {
            span.Detach(tid + "");
        }
    }

    /// <summary>从数据流traceId中释放片段信息</summary>
    /// <param name="span">片段</param>
    /// <param name="traceId">W3C标准TraceId，可以是traceparent</param>
    public static void Detach(this ISpan span, String traceId)
    {
        if (span == null || traceId.IsNullOrEmpty()) return;

        var ss = traceId.Split('-');
        if (ss.Length > 1) span.TraceId = ss[1];
        if (ss.Length > 2) span.ParentId = ss[2];

        if (ss.Length > 3 && !ss[3].IsNullOrEmpty() && span is DefaultSpan ds && ds.TraceFlag == 0)
        {
            // 识别跟踪标识，该TraceId之下，全量采样，确保链路采样完整
            var buf = ss[3].ToHex(0, 2);
            if (buf.Length > 0) ds.TraceFlag = buf[0];
        }
    }

    /// <summary>附加Tag信息在原Tag信息后面</summary>
    /// <param name="span">片段</param>
    /// <param name="tag">Tag信息</param>
    public static void AppendTag(this ISpan span, Object tag)
    {
        if (span == null || tag == null) return;

        if (span is DefaultSpan ds && ds.TraceFlag > 0)
        {
            if (span.Tag.IsNullOrEmpty())
                span.SetTag(tag);
            else if (span.Tag.Length < 1024)
            {
                var old = span.Tag;
                span.SetTag(tag);
                span.Tag = (old + "\r\n" + span.Tag).Cut(1024);
            }
        }
    }

    /// <summary>附加Http响应内容在原Tag信息后面</summary>
    /// <param name="span"></param>
    /// <param name="response"></param>
    public static void AppendTag(this ISpan span, HttpResponseMessage response)
    {
        // 正常响应，部分作为Tag信息
        if (response.StatusCode == HttpStatusCode.OK)
        {
            if (span is DefaultSpan ds && ds.TraceFlag > 0 && (span.Tag.IsNullOrEmpty() || span.Tag.Length < 1024))
            {
                // 判断类型和长度
                var content = response.Content;
                var mediaType = content.Headers?.ContentType?.MediaType;
                var len = content.Headers?.ContentLength ?? 0;
                if (len >= 0 && len < 1024 && mediaType.EndsWithIgnoreCase("json", "xml", "text", "html"))
                {
                    var result = content.ReadAsStringAsync().Result;
                    if (!result.IsNullOrEmpty())
                        span.Tag = (span.Tag + "\r\n" + result).Cut(1024);
                }
            }
        }
        // 异常响应，记录错误
        else if (response.StatusCode > (HttpStatusCode)299)
        {
            if (span.Error.IsNullOrEmpty()) span.Error = response.ReasonPhrase;
        }
    }
    #endregion
}