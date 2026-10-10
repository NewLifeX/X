using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using NewLife.Log;

namespace Benchmark.TracingBenchmarks;

/// <summary>APM 埋点的并发开销基准。单线程微基准看不到对象池与聚合字典的争用</summary>
/// <remarks>每轮固定 1 万次埋点、8 线程并发，按 OperationsPerInvoke 折算单次开销，可与单线程结果对照。</remarks>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 10)]
public class OtelConcurrencyBenchmark
{
    private const Int32 Operations = 10_000;

    /// <summary>跟踪器档位</summary>
    [ParamsAllValues]
    public OtelBenchmark.TracerKind Kind { get; set; }

    /// <summary>并发线程数</summary>
    [Params(8)]
    public Int32 Threads { get; set; }

    private ITracer? _tracer;
    private ActivityListener? _listener;

    /// <summary>初始化。与 OtelBenchmark 保持一致，保证两组数据可比</summary>
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
            case OtelBenchmark.TracerKind.None:
                _tracer = null;
                break;
            case OtelBenchmark.TracerKind.Star:
                _tracer = new DefaultTracer();
                break;
            case OtelBenchmark.TracerKind.OtelNoListener:
                _tracer = new OpenTelemetryTracer { SourceName = "Bench.Otel.NoListener" };
                break;
            case OtelBenchmark.TracerKind.OtelListener:
                _tracer = new OpenTelemetryTracer { SourceName = "Bench.Otel.Listener" };
                break;
            case OtelBenchmark.TracerKind.Dual:
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

    /// <summary>多线程并发埋点</summary>
    [Benchmark(OperationsPerInvoke = Operations)]
    public void ParallelSpans()
    {
        var tracer = _tracer;
        Parallel.For(0, Operations, new ParallelOptions { MaxDegreeOfParallelism = Threads }, _ =>
        {
            using var span = tracer?.NewSpan("bench.op");
        });
    }
}
