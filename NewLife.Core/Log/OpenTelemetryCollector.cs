#if NET5_0_OR_GREATER
using System.Diagnostics;
using System.Linq;
using NewLife.Collections;

namespace NewLife.Log;

/// <summary>OpenTelemetry 采集器。把进程内所有 活动(Activity) 转成 NewLife 埋点，上报到 <see cref="ITracer"/></summary>
/// <remarks>
/// <para>很多开源组件原生输出 OpenTelemetry 埋点（<see cref="ActivitySource"/>）。本采集器通过全局
/// <see cref="ActivityListener"/> 订阅这些活动，转换成 <see cref="ISpan"/> 后交给 <see cref="Tracer"/>，
/// 从而在接入星尘监控的应用里也能看到第三方组件的调用链。</para>
/// <para>与 <see cref="DefaultSpan"/> 的聚合模型一致，采集到的活动按“源名:操作名”聚合统计，
/// 只有少量样本保留完整链路标识，因此不会把海量活动逐条上网。</para>
/// <para>为避免重复采集，<see cref="ExcludeSources"/> 默认排除已经由 DiagnosticListener 或 NewLife 原生埋点
/// 覆盖的源（HttpClient、SqlClient、EF Core、gRPC 等）。只采集 OpenTelemetry 埋点的场景，
/// 可以把 <see cref="IncludeSources"/> 设为白名单。</para>
/// </remarks>
/// <example>
/// <code>
/// var star = new StarTracer("http://star:5000");
/// var collector = new OpenTelemetryCollector { Tracer = star }.Attach();
///
/// // 只采集指定源
/// var collector2 = new OpenTelemetryCollector
/// {
///     Tracer = star,
///     ExcludeSources = null,
///     IncludeSources = ["StackExchange.Redis", "Npgsql"],
/// }.Attach();
/// </code>
/// </example>
public sealed class OpenTelemetryCollector : DisposeBase
{
    #region 属性
    /// <summary>目标跟踪器。采集到的活动将转成埋点交给它</summary>
    public ITracer? Tracer { get; set; }

    /// <summary>不采集的源名。支持星号通配，默认排除已被 DiagnosticListener 或原生生埋点覆盖的源；设为空表示不排除</summary>
    /// <remarks>这些来源在 .NET 中已经由 <c>DiagnosticListener</c> 事件或 NewLife 原生埋点覆盖，
    /// 若此处重复采集，会导致星尘平台上的埋点计数翻倍。</remarks>
    public IList<String>? ExcludeSources { get; set; } = [
        "System.Net.Http",
        "System.Net.Sockets",
        "System.Net.NameResolution",
        "Experimental.System.Net.*",
        "System.Data.SqlClient",
        "Microsoft.Data.SqlClient",
        "Microsoft.EntityFrameworkCore*",
        "Grpc.Net.Client",
        "MongoDB.Driver",
    ];

    /// <summary>只采集的源名白名单。支持星号通配，为空表示不限</summary>
    public IList<String>? IncludeSources { get; set; }

    private ActivityListener? _listener;
    #endregion

    #region 方法
    /// <summary>挂载全局活动监听</summary>
    /// <returns></returns>
    public OpenTelemetryCollector Attach()
    {
        if (_listener != null) return this;

        var listener = new ActivityListener
        {
            ShouldListenTo = ShouldListenTo,
            Sample = Sample,
            ActivityStopped = OnActivityStopped,
        };
        ActivitySource.AddActivityListener(listener);

        _listener = listener;

        return this;
    }

    /// <summary>是否监听该活动源</summary>
    /// <param name="source">活动源</param>
    /// <returns></returns>
    public Boolean ShouldListenTo(ActivitySource source)
    {
        var name = source?.Name;
        if (name.IsNullOrEmpty()) return false;

        var includes = IncludeSources;
        if (includes != null && includes.Count > 0)
        {
            foreach (var item in includes)
            {
                // IsMatch 的接收者是表达式，入参才是待匹配的字符串
                if (!item.IsNullOrEmpty() && item.IsMatch(name, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        var excludes = ExcludeSources;
        if (excludes != null)
        {
            foreach (var item in excludes)
            {
                if (!item.IsNullOrEmpty() && item.IsMatch(name, StringComparison.OrdinalIgnoreCase)) return false;
            }
        }

        return true;
    }

    private static ActivitySamplingResult Sample(ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded;

    private void OnActivityStopped(Activity activity)
    {
        var tracer = Tracer;
        if (tracer == null || activity == null) return;

        // 有源名才纳入，无源活动多来自 DiagnosticListener 路径
        var source = activity.Source?.Name;
        if (source.IsNullOrEmpty()) return;

        var name = $"{source}:{activity.DisplayName}";

        var span = new DefaultSpan(tracer)
        {
            // 活动采用 W3C 标识，直接沿用，保证与 OpenTelemetry 链路一致
            Name = name,
            Id = activity.SpanId.ToHexString(),
            TraceId = activity.TraceId.ToHexString(),
            ParentId = activity.ParentSpanId.ToHexString(),
            StartTime = (Int64)(activity.StartTimeUtc - DateTime.UnixEpoch).TotalMilliseconds,
            EndTime = (Int64)(activity.StartTimeUtc - DateTime.UnixEpoch).TotalMilliseconds + (Int64)activity.Duration.TotalMilliseconds,

            // 强制采样，确保链路样本被保留，从而可以在星尘平台还原调用链
            TraceFlag = 1,
        };

        if (activity.GetTagItem("newlife.value") is Object value && value is IConvertible)
        {
            try
            {
                span.Value = value.ToInt();
            }
            catch { }
        }

        if (activity.Status == ActivityStatusCode.Error)
        {
            span.Error = activity.StatusDescription;
            if (span.Error.IsNullOrEmpty()) span.Error = "Error";
        }

        span.Tag = BuildTag(activity, tracer.MaxTagLength);

        // 不走 Start/Dispose 生命周期，避免污染 DefaultSpan.Current
        tracer.BuildSpan(name).Finish(span);
    }

    /// <summary>把活动属性拼装成数据标签</summary>
    /// <param name="activity">活动</param>
    /// <param name="maxLength">最大长度</param>
    /// <returns></returns>
    private static String? BuildTag(Activity activity, Int32 maxLength)
    {
        if (maxLength <= 0) return null;

        var sb = Pool.StringBuilder.Get();
        foreach (var item in activity.TagObjects)
        {
            if (sb.Length > 0) sb.Append("\r\n");
            sb.Append(item.Key).Append(" = ").Append(item.Value);
        }

        var str = sb.Return(true);

        // 异常事件单独补充，便于追溯错误
        var events = activity.Events;
        if (events != null)
        {
            foreach (var item in events)
            {
                if (item.Name != "exception") continue;

                str += "\r\n" + item.Name;

                var tags = item.Tags;
                if (tags == null) continue;

                foreach (var tag in tags) str += $"\r\n  {tag.Key} = {tag.Value}";
            }
        }

        return str.IsNullOrEmpty() ? null : str.Cut(maxLength);
    }
    #endregion

    #region 构造
    /// <summary>销毁，注销全局活动监听</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        _listener?.Dispose();
        _listener = null;
    }
    #endregion
}
#endif
