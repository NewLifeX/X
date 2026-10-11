#if NET6_0_OR_GREATER
using System.Diagnostics;
using NewLife.Serialization;

namespace NewLife.Log;

/// <summary>OpenTelemetry 跟踪片段。包装标准 活动(Activity)，并可选地同时写入底盘片段</summary>
internal sealed class OpenTelemetrySpan : ISpan
{
    /// <summary>共享空实现。无采集方时复用同一实例，不产生任何分配与动作</summary>
    public static readonly OpenTelemetrySpan Empty = new(null, null, null, "", null);

    private readonly OpenTelemetryTracer? _tracer;
    private readonly ISpan? _inner;
    private readonly OpenTelemetrySpanBuilder? _builder;
    private Activity? _activity;
    private String _name;
    private String? _tag;
    private String? _error;
    private Int64 _value;
    private Int64 _start;
    private String? _id;
    private String? _traceId;
    private String? _parentId;
    private Int32 _finished;

    /// <summary>是否为空实现</summary>
    private Boolean IsEmpty => _tracer == null;

    /// <summary>实例化</summary>
    /// <param name="tracer">跟踪器</param>
    /// <param name="activity">活动</param>
    /// <param name="inner">底盘片段</param>
    /// <param name="name">操作名称</param>
    /// <param name="builder">所属构建器</param>
    public OpenTelemetrySpan(OpenTelemetryTracer? tracer, Activity? activity, ISpan? inner, String name, OpenTelemetrySpanBuilder? builder)
    {
        _tracer = tracer;
        _activity = activity;
        _inner = inner;
        _name = name;
        _builder = builder;

        // 底盘片段继承活动的 W3C 标识，使星尘链路与 OpenTelemetry 链路可以互相对照
        if (activity != null && inner != null) AdoptInner(activity);
    }

    /// <summary>把活动的 W3C 标识回填到内层片段</summary>
    /// <param name="activity">活动</param>
    private void AdoptInner(Activity activity)
    {
        var inner = _inner;
        if (inner == null) return;

        inner.Id = activity.SpanId.ToHexString();
        inner.TraceId = activity.TraceId.ToHexString();
        inner.ParentId = activity.ParentSpanId.ToHexString();
    }

    #region 属性
    /// <summary>唯一标识。取活动的 W3C 片段标识，无活动时回退到底盘片段</summary>
    public String Id
    {
        get => _activity?.SpanId.ToHexString() ?? _inner?.Id ?? _id ?? "";
        set => _id = value;
    }

    /// <summary>埋点名</summary>
    public String Name
    {
        get => _activity?.DisplayName ?? _name;
        set
        {
            _name = value;
            if (_activity != null) _activity.DisplayName = value;
        }
    }

    /// <summary>父级片段标识。取活动的 W3C 父级标识，无活动时回退到底盘片段</summary>
    public String? ParentId
    {
        get => _activity?.ParentSpanId.ToHexString() ?? _inner?.ParentId ?? _parentId ?? "";
        set
        {
            _parentId = value;
            if (_inner != null) _inner.ParentId = value;
        }
    }

    /// <summary>跟踪标识。取活动的 W3C 跟踪标识，无活动时回退到底盘片段</summary>
    public String TraceId
    {
        get => _activity?.TraceId.ToHexString() ?? _inner?.TraceId ?? _traceId ?? "";
        set
        {
            _traceId = value;
            if (_inner != null) _inner.TraceId = value;
        }
    }

    /// <summary>开始时间。Unix毫秒</summary>
    public Int64 StartTime
    {
        get
        {
            var activity = _activity;
            if (activity != null) return (Int64)(activity.StartTimeUtc - DateTime.UnixEpoch).TotalMilliseconds;
            if (_start != 0) return _start;

            return _inner?.StartTime ?? 0;
        }
        set
        {
            _start = value;
            if (_inner != null) _inner.StartTime = value;
        }
    }

    /// <summary>结束时间。Unix毫秒</summary>
    public Int64 EndTime
    {
        get
        {
            var activity = _activity;
            if (activity == null) return _inner?.EndTime ?? 0;

            return (Int64)(activity.StartTimeUtc - DateTime.UnixEpoch).TotalMilliseconds + (Int64)activity.Duration.TotalMilliseconds;
        }
        set
        {
            if (_inner != null) _inner.EndTime = value;
        }
    }

    /// <summary>用户数值。以 newlife.value 属性的形式进入 OpenTelemetry</summary>
    public Int64 Value
    {
        get => _value;
        set
        {
            _value = value;
            _activity?.SetTag("newlife.value", value);
            if (_inner != null) _inner.Value = value;
        }
    }

    /// <summary>数据标签。以 newlife.tag 属性的形式进入 OpenTelemetry</summary>
    public String? Tag
    {
        get => _tag;
        set
        {
            _tag = value;
            SetActivityTag(value);
            if (_inner != null) _inner.Tag = value;
        }
    }

    /// <summary>错误信息</summary>
    public String? Error
    {
        get => _error;
        set
        {
            _error = value;
            _activity?.SetStatus(ActivityStatusCode.Error, value);
            if (_inner != null) _inner.Error = value;
        }
    }
    #endregion

    #region 方法
    /// <summary>设置错误信息</summary>
    /// <param name="ex">异常对象</param>
    /// <param name="tag">附加数据标签</param>
    public void SetError(Exception ex, Object? tag = null)
    {
        if (IsEmpty) return;

        if (tag != null) SetTag(tag);

        if (ex == null) return;

        var message = ex.GetMessage();
        _error = message;

        var activity = _activity;
        if (activity != null)
        {
            activity.SetStatus(ActivityStatusCode.Error, message);
            activity.SetTag("error.type", ex.GetType().FullName);
            activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
            {
                ["exception.type"] = ex.GetType().FullName,
                ["exception.message"] = message,
                ["exception.stacktrace"] = ex.ToString(),
            }));
        }

        _inner?.SetError(ex, tag);
    }

    /// <summary>设置数据标签</summary>
    /// <param name="tag">标签数据</param>
    public void SetTag(Object? tag)
    {
        if (IsEmpty || tag == null) return;

        var len = _tracer!.MaxTagLength;
        if (len <= 0) return;

        String? str;
        if (tag is String s)
            str = s.Cut(len);
        else if (tag is System.Text.StringBuilder builder)
            str = builder.Length <= len ? builder.ToString() : builder.ToString(0, len);
        else
        {
            // 避免数据标签的序列化抛出异常影响业务层
            try
            {
                str = tag.ToJson().Cut(len);
            }
            catch
            {
                str = tag.ToString()?.Cut(len);
            }
        }

        _tag = str;
        SetActivityTag(str);
        if (_inner != null) _inner.SetTag(tag);
    }

    /// <summary>抛弃埋点</summary>
    /// <remarks>
    /// <para>本片段不再结束活动，因此活动不会通知任何采集方，埋点不会被导出；底盘片段同时被抛弃。
    /// 实测抛弃 10000 次埋点导出 0 条，对照正常埋点 10000 条全部导出。</para>
    /// <para>已知限制：活动不停止就无法归还 <c>Activity.Current</c>，同一异步上下文内后续埋点仍会以这个
    /// 不会导出的片段为父级，连续抛弃还会让这条父级链一直被持有。活动生命周期归 OpenTelemetry SDK 管理，
    /// 库侧不介入；需要彻底过滤埋点的场景请改用采集端的源名过滤或采样处理器。详见《链路追踪ITracer》收口记录。</para>
    /// </remarks>
    public void Abandon()
    {
        Interlocked.Exchange(ref _finished, 1);

        // 不能还给对象池，因为外部还有引用，Dispose尚未调用
        _inner?.Abandon();
    }

    /// <summary>完成片段</summary>
    public void Dispose() => Finish();

    private void SetActivityTag(String? value)
    {
        var activity = _activity;
        if (activity == null || value.IsNullOrEmpty()) return;

        activity.SetTag("newlife.tag", value);
    }

    private void Finish()
    {
        if (Interlocked.CompareExchange(ref _finished, 1, 0) != 0) return;

        if (IsEmpty) return;

        _activity?.Stop();

        // 活动结束后耗时才可用，统计必须在置空活动之前完成
        _builder?.Finish(this);
        _activity = null;

        _inner?.Dispose();
    }
    #endregion

    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString()
    {
        var activity = _activity;
        if (activity == null) return _inner?.ToString() ?? $"{TraceId}-{Id}";

        return $"00-{activity.TraceId}-{activity.SpanId}-01";
    }
}

/// <summary>OpenTelemetry 跟踪片段构建器</summary>
/// <param name="tracer">跟踪器</param>
/// <param name="name">操作名称</param>
internal sealed class OpenTelemetrySpanBuilder(OpenTelemetryTracer tracer, String name) : ISpanBuilder
{
    #region 属性
    /// <summary>跟踪器</summary>
    public ITracer? Tracer { get; set; } = tracer;

    /// <summary>操作名</summary>
    public String Name { get; set; } = name;

    /// <summary>开始时间。Unix毫秒</summary>
    public Int64 StartTime { get; set; }

    /// <summary>结束时间。Unix毫秒</summary>
    public Int64 EndTime { get; set; }

    /// <summary>采样总数</summary>
    public Int32 Total { get => _total; set => _total = value; }

    /// <summary>错误次数</summary>
    public Int32 Errors { get => _errors; set => _errors = value; }

    /// <summary>总耗时。单位ms</summary>
    public Int64 Cost { get => _cost; set => _cost = value; }

    /// <summary>最大耗时。单位ms</summary>
    public Int32 MaxCost { get; set; }

    /// <summary>最小耗时。单位ms</summary>
    public Int32 MinCost { get; set; } = -1;

    /// <summary>用户数值</summary>
    public Int64 Value { get => _value; set => _value = value; }

    private Int32 _total;
    private Int32 _errors;
    private Int64 _cost;
    private Int64 _value;

    /// <summary>上游父级上下文。服务端片段用它继承上游链路——OpenTelemetry 的父级只能在活动创建时指定</summary>
    internal ActivityContext? Parent { get; set; }

    /// <summary>正常采样</summary>
    public IList<ISpan>? Samples { get; set; }

    /// <summary>异常采样</summary>
    public IList<ISpan>? ErrorSamples { get; set; }
    #endregion

    /// <summary>开始一个片段</summary>
    /// <returns></returns>
    public ISpan Start()
    {
        var tracer = Tracer as OpenTelemetryTracer;
        if (tracer == null) return OpenTelemetrySpan.Empty;

        // 底盘先建，保证无监听者时内层链路依旧完整
        var inner = tracer.Inner?.NewSpan(Name);

        var activity = tracer.StartActivity(Name, Parent);
        if (activity == null && inner == null) return OpenTelemetrySpan.Empty;

        StartTime = Runtime.UtcNow.ToUnixTimeMilliseconds();

        return new OpenTelemetrySpan(tracer, activity, inner, Name, this);
    }

    /// <summary>完成片段</summary>
    /// <param name="span"></param>
    public void Finish(ISpan span)
    {
        if (span is not OpenTelemetrySpan os || os == OpenTelemetrySpan.Empty) return;

        // 累计耗时。时间回退的耗时，一律清零，避免出现负数耗时
        var cost = (Int32)(span.EndTime - span.StartTime);
        if (cost < 0) cost = 0;

        Interlocked.Add(ref _cost, cost);
        Interlocked.Increment(ref _total);
        if (span.Error != null) Interlocked.Increment(ref _errors);
        if (span.Value != 0) Interlocked.Add(ref _value, span.Value);
        if (MaxCost < cost) MaxCost = cost;
        if (MinCost > cost || MinCost < 0) MinCost = cost;

        EndTime = Runtime.UtcNow.ToUnixTimeMilliseconds();
    }
}
#endif
