using BenchmarkDotNet.Attributes;
using NewLife.Caching;

namespace Benchmark.CacheBenchmarks;

/// <summary>MemoryCache 单线程基础性能测试</summary>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 3)]
public class MemoryCacheBenchmark
{
    private MemoryCache _cache = null!;
    private String[] _keys = null!;
    private Dictionary<String, String> _batch = null!;

    [Params(1, 10, 100)]
    public Int32 BatchSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _cache = new MemoryCache { Capacity = 0 };

        // 预置100个key，避免Get测试里都是miss
        _keys = new String[100];
        for (var i = 0; i < _keys.Length; i++)
        {
            _keys[i] = $"bench_{i}";
            _cache.Set(_keys[i], $"value_{i}");
        }

        _batch = [];
        for (var i = 0; i < Math.Max(BatchSize, 100); i++)
            _batch[$"batch_{i}"] = $"val_{i}";
    }

    [GlobalCleanup]
    public void Cleanup() => _cache.Dispose();

    // ── 单key操作 ──────────────────────────────────────────────────────────────

    [Benchmark(Description = "Set")]
    public Boolean Set() => _cache.Set("bench_0", "hello");

    [Benchmark(Description = "Get")]
    public String? Get() => _cache.Get<String>("bench_0");

    [Benchmark(Description = "Remove")]
    public Int32 Remove() => _cache.Remove("bench_0");

    [Benchmark(Description = "Inc")]
    public Int64 Inc() => _cache.Increment("counter", 1L);

    // ── 批量操作 ───────────────────────────────────────────────────────────────

    [Benchmark(Description = "SetAll")]
    public void SetAll()
    {
        var dic = new Dictionary<String, String>(BatchSize);
        for (var i = 0; i < BatchSize; i++)
            dic[_keys[i % _keys.Length]] = "v";
        _cache.SetAll(dic);
    }

    [Benchmark(Description = "GetAll")]
    public IDictionary<String, String?> GetAll()
    {
        var keys = _keys.Take(BatchSize);
        return _cache.GetAll<String>(keys);
    }
}

/// <summary>MemoryCache 多线程并发性能测试</summary>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 3)]
public class MemoryCacheConcurrencyBenchmark
{
    private MemoryCache _cache = null!;
    private String[] _keys = null!;

    /// <summary>动态线程数：固定 1/4/8/32，若本机逻辑核心数不在其中则额外加入</summary>
    public static IEnumerable<Int32> ThreadCounts
    {
        get
        {
            var cores = Environment.ProcessorCount;
            var set = new SortedSet<Int32> { 1, 4, 8, 32 };
            set.Add(cores);
            return set;
        }
    }

    [ParamsSource(nameof(ThreadCounts))]
    public Int32 ThreadCount { get; set; }

    [Params(10_000)]
    public Int32 IterationsPerThread { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _cache = new MemoryCache { Capacity = 0 };
        _keys = new String[64];
        for (var i = 0; i < _keys.Length; i++)
        {
            _keys[i] = $"c_{i}";
            _cache.Set(_keys[i], i.ToString());
        }
    }

    [GlobalCleanup]
    public void Cleanup() => _cache.Dispose();

    [Benchmark(Description = "并发Set-顺序")]
    public void ConcurrentSet_Sequential()
    {
        Parallel.For(0, ThreadCount, t =>
        {
            var myKey = _keys[t % _keys.Length];
            for (var i = 0; i < IterationsPerThread; i++)
                _cache.Set(myKey, "v");
        });
    }

    [Benchmark(Description = "并发Get-顺序")]
    public void ConcurrentGet_Sequential()
    {
        Parallel.For(0, ThreadCount, t =>
        {
            var myKey = _keys[t % _keys.Length];
            for (var i = 0; i < IterationsPerThread; i++)
                _ = _cache.Get<String>(myKey);
        });
    }

    [Benchmark(Description = "并发Remove-顺序")]
    public void ConcurrentRemove_Sequential()
    {
        Parallel.For(0, ThreadCount, t =>
        {
            var myKey = _keys[t % _keys.Length];
            for (var i = 0; i < IterationsPerThread; i++)
                _cache.Remove(myKey);
        });
    }

    [Benchmark(Description = "并发Inc-顺序")]
    public void ConcurrentInc_Sequential()
    {
        Parallel.For(0, ThreadCount, t =>
        {
            var myKey = _keys[t % _keys.Length];
            for (var i = 0; i < IterationsPerThread; i++)
                _cache.Increment(myKey, 1L);
        });
    }

    [Benchmark(Description = "并发Set-随机")]
    public void ConcurrentSet_Random()
    {
        Parallel.For(0, ThreadCount, t =>
        {
            for (var i = t; i < ThreadCount * IterationsPerThread; i += ThreadCount)
                _cache.Set(_keys[i % _keys.Length], "v");
        });
    }

    [Benchmark(Description = "并发Get-随机")]
    public void ConcurrentGet_Random()
    {
        Parallel.For(0, ThreadCount, t =>
        {
            for (var i = t; i < ThreadCount * IterationsPerThread; i += ThreadCount)
                _ = _cache.Get<String>(_keys[i % _keys.Length]);
        });
    }
}
