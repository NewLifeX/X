using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Log;

/// <summary>跟踪片段构建器</summary>
public interface ISpanBuilder
{
    #region 属性
    /// <summary>跟踪器</summary>
    [XmlIgnore, ScriptIgnore, IgnoreDataMember]
    ITracer? Tracer { get; }

    /// <summary>操作名</summary>
    String Name { get; set; }

    /// <summary>开始时间。Unix毫秒</summary>
    Int64 StartTime { get; set; }

    /// <summary>结束时间。Unix毫秒</summary>
    Int64 EndTime { get; set; }

    /// <summary>采样总数</summary>
    Int32 Total { get; }

    /// <summary>错误次数</summary>
    Int32 Errors { get; }

    /// <summary>总耗时。所有请求耗时累加，单位ms</summary>
    Int64 Cost { get; }

    /// <summary>最大耗时。单位ms</summary>
    Int32 MaxCost { get; }

    /// <summary>最小耗时。单位ms</summary>
    Int32 MinCost { get; }

    /// <summary>用户数值。记录数字型标量，如每次数据库操作行数，星尘平台汇总统计</summary>
    Int64 Value { get; set; }

    /// <summary>正常采样</summary>
    IList<ISpan>? Samples { get; set; }

    /// <summary>异常采样</summary>
    IList<ISpan>? ErrorSamples { get; set; }
    #endregion

    #region 方法
    /// <summary>开始一个Span</summary>
    /// <returns></returns>
    ISpan Start();

    /// <summary>完成Span</summary>
    /// <param name="span"></param>
    void Finish(ISpan span);
    #endregion
}

/// <summary>跟踪片段构建器</summary>
public class DefaultSpanBuilder : ISpanBuilder
{
    #region 属性
    /// <summary>跟踪器</summary>
    [XmlIgnore, ScriptIgnore, IgnoreDataMember]
    public ITracer? Tracer { get; set; }

    /// <summary>埋点名</summary>
    public String Name { get; set; } = null!;

    /// <summary>开始时间。Unix毫秒</summary>
    public Int64 StartTime { get; set; }

    /// <summary>结束时间。Unix毫秒</summary>
    public Int64 EndTime { get; set; }

    private Int32 _Total;
    /// <summary>采样总数</summary>
    public Int32 Total { get => _Total; set => _Total = value; }

    private Int32 _Errors;
    /// <summary>错误次数</summary>
    public Int32 Errors { get => _Errors; set => _Errors = value; }

    private Int64 _Cost;
    /// <summary>总耗时。所有请求耗时累加，单位ms</summary>
    public Int64 Cost { get => _Cost; set => _Cost = value; }

    /// <summary>最大耗时。单位ms</summary>
    public Int32 MaxCost { get; set; }

    /// <summary>最小耗时。单位ms</summary>
    public Int32 MinCost { get; set; }

    private Int64 _Value;
    /// <summary>用户数值。记录数字型标量，如每次数据库操作行数，星尘平台汇总统计</summary>
    public Int64 Value { get => _Value; set => _Value = value; }

    /// <summary>正常采样</summary>
    public IList<ISpan>? Samples { get; set; }

    /// <summary>异常采样</summary>
    public IList<ISpan>? ErrorSamples { get; set; }
    #endregion

    #region 构造函数
    /// <summary>实例化</summary>
    public DefaultSpanBuilder() { }

    /// <summary>实例化</summary>
    /// <param name="tracer"></param>
    /// <param name="name"></param>
    public DefaultSpanBuilder(ITracer tracer, String name)
    {
        Tracer = tracer;

        Init(name);
    }
    #endregion

    #region 方法
    /// <summary>初始化。重用已有对象</summary>
    /// <param name="name"></param>
    public void Init(String name)
    {
        Name = name;
        MinCost = -1;
        StartTime = DateTime.UtcNow.ToLong();
    }

    /// <summary>开始一个Span，开始计时</summary>
    /// <returns></returns>
    public virtual ISpan Start()
    {
        DefaultSpan? span = null;
        var tracer = Tracer;
        if (tracer is DefaultTracer tracer2)
        {
            span = tracer2.SpanPool.Get() as DefaultSpan;
            if (span != null)
            {
                span.Tracer = tracer2;
                span.Name = Name;
#pragma warning disable CS0612 // 类型或成员已过时
                span.Builder = this;
#pragma warning restore CS0612 // 类型或成员已过时
            }
        }

        span ??= new DefaultSpan(tracer!);
        span.Start();

        // 指示当前节点开始的后续节点强制采样
        if (span.TraceFlag == 0 && tracer != null && Total < tracer.MaxSamples) span.TraceFlag = 1;

        return span;
    }

    /// <summary>完成Span，每一个埋点结束都进入这里</summary>
    /// <param name="span"></param>
    public virtual void Finish(ISpan span)
    {
        var tracer = Tracer;
        if (tracer == null) return;

        // 累计耗时。时间回退的耗时，一律清零，避免出现负数耗时
        var cost = (Int32)(span.EndTime - span.StartTime);
        if (cost < 0) cost = 0;

        // 抛弃耗时过大的埋点，避免污染统计数据
        if (cost > 3600_000) return;

        Interlocked.Add(ref _Cost, cost);

        // 总次数
        var total = Interlocked.Increment(ref _Total);

        // 累加用户数值
        if (span.Value != 0) Interlocked.Add(ref _Value, span.Value);

        // 最大最小耗时
        if (MaxCost < cost) MaxCost = cost;
        if (MinCost > cost || MinCost < 0) MinCost = cost;

        // 检查跟踪标识，上游指示强制采样，确保链路采样完整
        var force = false;
        var ds = span as DefaultSpan;
        if (ds != null && ds.TraceFlag > 0) force = true;

        // 处理采样
        var flag = false;
        if (span.Error != null)
        {
            if (Interlocked.Increment(ref _Errors) <= tracer.MaxErrors || force && _Errors <= tracer.MaxErrors * 10)
            {
                var ss = ErrorSamples ??= [];
                lock (ss)
                {
                    ss.Add(span);
                    flag = true;
                }
            }
        }
        // 未达最大数采样，超时采样，强制采样
        else if (total <= tracer.MaxSamples || (tracer.Timeout > 0 && cost > tracer.Timeout || force) && total <= tracer.MaxSamples * 10)
        {
            var ss = Samples ??= [];
            lock (ss)
            {
                ss.Add(span);
                flag = true;
            }
        }

        // 如果埋点没有加入链表，则归还对象池
        if (!flag && Tracer is DefaultTracer tracer2)
        {
            ds?.Clear();
            tracer2.SpanPool.Return(span);
        }
    }

    /// <summary>把集合中的ISpan归还到池里</summary>
    /// <param name="spans"></param>
    internal void Return(IList<ISpan>? spans)
    {
        if (spans == null) return;
        if (Tracer is not DefaultTracer tracer) return;

        foreach (var elm in spans)
        {
            if (elm is DefaultSpan ds)
            {
                ds.Clear();
                tracer.SpanPool.Return(ds);
            }
        }
    }

    /// <summary>清空已有数据</summary>
    public void Clear()
    {
        //!!! 不能清空Tracer，否则长时间Span完成时，Builder已被处理，Span无法创建新的Builder来统计埋点数据
        Tracer = null;

        Name = null!;
        StartTime = 0;
        EndTime = 0;
        Value = 0;
        Samples = null;
        ErrorSamples = null;

        _Total = 0;
        _Errors = 0;
        _Cost = 0;
        MaxCost = 0;
        MinCost = 0;
    }

    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String? ToString() => Name;
    #endregion
}