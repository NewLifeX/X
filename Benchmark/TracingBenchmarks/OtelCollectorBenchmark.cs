using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using NewLife.Log;

namespace Benchmark.TracingBenchmarks;

/// <summary>未挂载采集器时的第三方活动源基线。进程内没有任何 ActivityListener</summary>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 10)]
public class OtelNoCollectorBenchmark
{
    private ActivitySource _source = null!;

    /// <summary>初始化</summary>
    [GlobalSetup]
    public void Setup() => _source = new ActivitySource("Bench.ThirdParty.NoCollector");

    /// <summary>源自己创建并结束一个活动</summary>
    [Benchmark(Baseline = true)]
    public void CreateActivity()
    {
        using var activity = _source.StartActivity("Op", ActivityKind.Client);
    }
}

/// <summary>挂载采集器后的第三方活动源成本。区分"采集命中"与"被过滤"</summary>
/// <remarks>
/// 采集器的 <see cref="OpenTelemetryCollector.ShouldListenTo"/> 在每次活动创建时被回调，
/// 本基准用于量化该回调（含默认排除清单的通配匹配）的成本。
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 10)]
public class OtelCollectorBenchmark
{
    private ActivitySource _collected = null!;
    private ActivitySource _filtered = null!;
    private OpenTelemetryCollector? _collector;

    /// <summary>初始化并挂载采集器</summary>
    [GlobalSetup]
    public void Setup()
    {
        _collected = new ActivitySource("Bench.ThirdParty.Collected");
        _filtered = new ActivitySource("Bench.ThirdParty.Filtered");

        _collector = new OpenTelemetryCollector
        {
            Tracer = new DefaultTracer(),
            ExcludeSources = null,
            IncludeSources = ["Bench.ThirdParty.Collected"],
        }.Attach();
    }

    /// <summary>清理</summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        _collector?.Dispose();
        _collector = null;
    }

    /// <summary>源不在白名单内。监听者存在但不应采集</summary>
    [Benchmark]
    public void FilteredOut()
    {
        using var activity = _filtered.StartActivity("Op", ActivityKind.Client);
    }

    /// <summary>源在白名单内，活动被采集并转成埋点</summary>
    [Benchmark]
    public void Collected()
    {
        using var activity = _collected.StartActivity("Op", ActivityKind.Client);
    }
}
