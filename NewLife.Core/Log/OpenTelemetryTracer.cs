#if NET6_0_OR_GREATER
using System.Diagnostics;
using NewLife.Http;
using NewLife.Reflection;
using NewLife.Remoting;

namespace NewLife.Log;

/// <summary>OpenTelemetry 跟踪器。把 NewLife 埋点转换为标准 活动(Activity)，接入 OpenTelemetry 生态</summary>
/// <remarks>
/// <para>NewLife 自带 <see cref="ITracer"/>/<see cref="ISpan"/> 轻量 APM，本类把它桥接到 .NET 标准诊断设施
/// <see cref="ActivitySource"/>/<see cref="Activity"/> 上，从而被 OpenTelemetry SDK、Collector、Jaeger、Tempo 等直接采集。
/// NewLife 各组件无需任何改造，它们调用的 <c>Tracer?.NewSpan()</c> 会原样产出标准活动。</para>
/// <para>可以作为装饰器使用。<see cref="Inner"/> 指向底盘跟踪器（例如星尘 StarTracer）时，同一埋点同时写入两边，
/// 且内层片段会继承活动的 W3C 标识，使星尘链路与 OpenTelemetry 链路可以互相对照定位。</para>
/// <para>当且仅当没有任何活动监听者且未设置底盘时，所有埋点立即返回共享的空实现，不产生分配，
/// 因此可以安全地作为全局默认跟踪器长期挂着。</para>
/// </remarks>
/// <example>
/// <code>
/// // 方式一：一行启用，本进程所有 NewLife 埋点以 OpenTelemetry 标准输出
/// OpenTelemetryTracer.Enable();
///
/// // 方式二：与星尘双写，内层片段继承 W3C 标识
/// OpenTelemetryTracer.Enable(new OpenTelemetryTracer
/// {
///     SourceName = "MyApp",
///     Inner = new StarTracer("http://star:5000"),
/// });
///
/// // 方式三：局部使用
/// var tracer = new OpenTelemetryTracer { SourceName = "MyApp" };
/// using var span = tracer.NewSpan("业务操作", new { Id = 123 });
/// </code>
/// </example>
public class OpenTelemetryTracer : DisposeBase, ITracer, ILogFeature
{
    #region 静态
    private static OpenTelemetryTracer? _default;

    /// <summary>默认实例。全局单例</summary>
    public static OpenTelemetryTracer Default => _default ??= new OpenTelemetryTracer();

    /// <summary>启用 OpenTelemetry 跟踪器，并注入到各组件全局入口</summary>
    /// <remarks>与星尘 StarTracer.AttachGlobal 采用同一套注入机制，覆盖 NewLife 组件、XCode 与 Cube</remarks>
    /// <param name="tracer">跟踪器，为空时使用 <see cref="Default"/></param>
    /// <returns></returns>
    public static OpenTelemetryTracer Enable(OpenTelemetryTracer? tracer = null)
    {
        tracer ??= Default;

        DefaultTracer.Instance = tracer;
        ApiHelper.Tracer = tracer;
        HttpHelper.Tracer = tracer;

        // 反射处理XCode追踪
        {
            var type = "XCode.DataAccessLayer.DAL".GetTypeEx();
            var pi = type?.GetPropertyEx("GlobalTracer");
            if (pi != null && pi.PropertyType == typeof(ITracer)) pi.SetValue(null, tracer, null);
        }

        // 反射处理Cube追踪
        {
            var type = "NewLife.Cube.WebMiddleware.TracerMiddleware".GetTypeEx();
            var pi = type?.GetPropertyEx("Tracer");
            if (pi != null && pi.PropertyType == typeof(ITracer)) pi.SetValue(null, tracer, null);
        }

        // 反射处理Stardust的TracerMiddleware追踪
        {
            var type = "Stardust.Extensions.TracerMiddleware".GetTypeEx();
            var pi = type?.GetPropertyEx("Tracer");
            if (pi != null && pi.PropertyType == typeof(ITracer)) pi.SetValue(null, tracer, null);
        }

        return tracer;
    }
    #endregion

    #region 属性
    private String _sourceName = "NewLife.Core";
    /// <summary>活动源名称。OpenTelemetry 通过 AddSource 订阅该名称，默认 NewLife.Core</summary>
    public String SourceName
    {
        get => _sourceName;
        set
        {
            if (_sourceName == value) return;

            _sourceName = value;
            _source?.Dispose();
            _source = null;
        }
    }

    /// <summary>活动源版本。默认取当前程序集版本</summary>
    public String? SourceVersion { get; set; } = typeof(OpenTelemetryTracer).Assembly.GetName().Version?.ToString();

    /// <summary>活动类型。默认 Internal，表示进程内操作</summary>
    public ActivityKind Kind { get; set; } = ActivityKind.Internal;

    /// <summary>底盘跟踪器。为空时仅输出 OpenTelemetry，不为空时同时写入底盘（如星尘 StarTracer）</summary>
    public ITracer? Inner { get; set; }

    private ActivitySource? _source;
    private ActivitySource Source
    {
        get
        {
            var source = _source;
            if (source != null) return source;

            // 并发创建无害，重复实例会被直接丢弃
            source = new ActivitySource(SourceName, SourceVersion);
            _source = source;

            return source;
        }
    }

    /// <summary>当前是否有采集方。无监听者且无底盘时，埋点走空实现</summary>
    public Boolean IsActive
    {
        get
        {
            if (Inner != null) return true;

            return Source.HasListeners();
        }
    }

    /// <summary>采样周期。单位秒。OpenTelemetry 由采集端处理器控制上报节奏，此处仅为接口兼容</summary>
    public Int32 Period { get; set; } = 15;

    /// <summary>最大正常采样数。此处仅为接口兼容</summary>
    public Int32 MaxSamples { get; set; } = 1;

    /// <summary>最大异常采样数。此处仅为接口兼容</summary>
    public Int32 MaxErrors { get; set; } = 10;

    /// <summary>超时时间。单位毫秒。此处仅为接口兼容</summary>
    public Int32 Timeout { get; set; } = 15000;

    /// <summary>最大标签长度。超过该长度时将截断，默认1024字符</summary>
    public Int32 MaxTagLength { get; set; } = 1024;

    /// <summary>向http/rpc请求注入TraceId的参数名。默认W3C标准的traceparent</summary>
    /// <remarks>
    /// traceparent 采用与 <see cref="DefaultTracer"/> 相同的注入方式，且 NewLife 的标识已经符合 W3C 规范，
    /// 因此即使没有启用 OpenTelemetry 的 HttpClient 埋点，跨进程链路依然完整。
    /// </remarks>
    public String? AttachParameter { get; set; } = "traceparent";

    private ITracerResolver? _resolver;
    /// <summary>埋点解析器。默认实现会在无采集方时直接返回空，避免 HttpClient 路径产生多余工作</summary>
    public ITracerResolver Resolver
    {
        get => _resolver ??= new OpenTelemetryTracerResolver();
        set => _resolver = value;
    }

    /// <summary>日志</summary>
    public ILog Log { get; set; } = Logger.Null;
    #endregion

    #region 构造
    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        _source?.Dispose();
        _source = null;
    }
    #endregion

    #region 方法
    /// <summary>建立片段构建器</summary>
    /// <param name="name">操作名称</param>
    /// <returns></returns>
    public virtual ISpanBuilder BuildSpan(String name) => new OpenTelemetrySpanBuilder(this, name ?? "");

    /// <summary>开始一个片段</summary>
    /// <param name="name">操作名称</param>
    /// <returns></returns>
    public virtual ISpan NewSpan(String name)
    {
        // 无采集方时直接返回空实现，省掉片段构建器的分配
        if (!IsActive) return OpenTelemetrySpan.Empty;

        return BuildSpan(name).Start();
    }

    /// <summary>开始一个片段，指定数据标签</summary>
    /// <param name="name">操作名称</param>
    /// <param name="tag">数据标签</param>
    /// <returns></returns>
    public virtual ISpan NewSpan(String name, Object? tag)
    {
        var span = NewSpan(name);
        if (tag != null && span != OpenTelemetrySpan.Empty) span.SetTag(tag);

        return span;
    }

    /// <summary>截断所有片段构建器数据。OpenTelemetry 的上报由采集端处理器负责，此处始终为空</summary>
    /// <returns></returns>
    public virtual ISpanBuilder[] TakeAll() => [];

    /// <summary>创建活动。无监听者时返回空</summary>
    /// <param name="name">操作名称</param>
    /// <returns></returns>
    internal Activity? StartActivity(String name)
    {
        var source = Source;
        if (!source.HasListeners()) return null;

        return source.StartActivity(name, Kind);
    }
    #endregion

    #region 日志
    /// <summary>写日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object?[] args) => Log?.Info(format, args);
    #endregion
}

/// <summary>OpenTelemetry 埋点解析器。无采集方时直接返回空，避免无谓的字符串解析与请求头注入</summary>
internal class OpenTelemetryTracerResolver : DefaultTracerResolver
{
    /// <summary>创建Http请求埋点</summary>
    /// <param name="tracer">跟踪器</param>
    /// <param name="uri">请求地址</param>
    /// <param name="userState">用户状态</param>
    /// <returns></returns>
    public override ISpan? CreateSpan(ITracer tracer, Uri uri, Object? userState)
    {
        if (tracer is OpenTelemetryTracer otel && !otel.IsActive) return null;

        return base.CreateSpan(tracer, uri, userState);
    }
}
#endif
