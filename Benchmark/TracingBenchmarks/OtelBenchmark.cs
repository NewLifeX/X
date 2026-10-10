using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using NewLife.Log;

namespace Benchmark.TracingBenchmarks;

/// <summary>APM 埋点开销基准。同一份被测代码，只切换跟踪器档位</summary>
/// <remarks>
/// 回答两个问题：NewLife 原生埋点（星尘侧）相对"不用 APM"增加多少；再加上 OpenTelemetry 后增加多少。
/// 所有档位共用同一个被测方法体，保证可比性。
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 10)]
public class OtelBenchmark
{
    /// <summary>跟踪器档位</summary>
    public enum TracerKind
    {
        /// <summary>无 APM。tracer 为 null，埋点调用被 ?. 短路</summary>
        None,

        /// <summary>NewLife 原生：DefaultTracer 本地聚合（StarTracer 的每片段路径与之相同）</summary>
        Star,

        /// <summary>OpenTelemetry，但没有任何监听者</summary>
        OtelNoListener,

        /// <summary>OpenTelemetry，存在活动监听者</summary>
        OtelListener,

        /// <summary>OpenTelemetry 输出 + NewLife 原生双写</summary>
        Dual,
    }

    /// <summary>跟踪器档位</summary>
    [ParamsAllValues]
    public TracerKind Kind { get; set; }

    private ITracer? _tracer;
    private ActivityListener? _listener;

    private readonly Object _tag = new { Id = 123, Name = "订单", Amount = 99.5, Time = DateTime.Now };
    private readonly Exception _error = new InvalidOperationException("模拟业务异常");

    /// <summary>初始化。只监听指定源名，使"无监听者"档位保持真实短路</summary>
    [GlobalSetup]
    public void Setup()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "Bench.Otel.Listener" || s.Name == "Bench.Otel.Dual",
            Sample = (ref ActivityCreationOptions<ActivityContext> o) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(_listener);

        switch (Kind)
        {
            case TracerKind.None:
                _tracer = null;
                break;
            case TracerKind.Star:
                _tracer = new DefaultTracer();
                break;
            case TracerKind.OtelNoListener:
                _tracer = new OpenTelemetryTracer { SourceName = "Bench.Otel.NoListener" };
                break;
            case TracerKind.OtelListener:
                _tracer = new OpenTelemetryTracer { SourceName = "Bench.Otel.Listener" };
                break;
            case TracerKind.Dual:
                _tracer = new OpenTelemetryTracer { SourceName = "Bench.Otel.Dual", Inner = new DefaultTracer() };
                break;
        }
    }

    /// <summary>清理</summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        _listener?.Dispose();
        _listener = null;
    }

    /// <summary>空埋点。真实代码中最常见的形态</summary>
    [Benchmark]
    public void EmptySpan()
    {
        using var span = _tracer?.NewSpan("bench.op");
    }

    /// <summary>带数据标签的埋点</summary>
    [Benchmark]
    public void TaggedSpan()
    {
        using var span = _tracer?.NewSpan("bench.op", _tag);
    }

    /// <summary>嵌套两层埋点</summary>
    [Benchmark]
    public void NestedSpans()
    {
        using var outer = _tracer?.NewSpan("bench.outer");
        using var inner = _tracer?.NewSpan("bench.inner", _tag);
    }

    /// <summary>异常埋点</summary>
    [Benchmark]
    public void ErrorSpan()
    {
        using var span = _tracer?.NewSpan("bench.op", _tag);
        span?.SetError(_error, _tag);
    }
}

/// <summary>星尘侧周期成本基准。聚合数据每采样周期被取走一次</summary>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 10)]
public class OtelPeriodicBenchmark
{
    private DefaultTracer _tracer = null!;

    /// <summary>每个周期内累计的埋点条数</summary>
    [Params(100, 1000)]
    public Int32 Count { get; set; }

    /// <summary>初始化</summary>
    [GlobalSetup]
    public void Setup() => _tracer = new DefaultTracer();

    /// <summary>填充埋点数据。不计入被测路径</summary>
    [IterationSetup]
    public void Fill()
    {
        for (var i = 0; i < Count; i++)
        {
            using var span = _tracer.NewSpan("bench.op" + (i % 8));
        }
    }

    /// <summary>取走并重置聚合数据</summary>
    /// <returns></returns>
    [Benchmark]
    public ISpanBuilder[] TakeAll() => _tracer.TakeAll();
}
