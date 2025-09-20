using BenchmarkDotNet.Attributes;
using NewLife.Collections;

namespace XUnitTest.Collections;

/// <summary>Pool 性能基准</summary>
[MemoryDiagnoser]
public class PoolBenchmark
{
    private Pool<Object> _pool;

    [GlobalSetup]
    public void Setup()
    {
        _pool = new Pool<Object>(128);
    }

    [Benchmark]
    public void GetReturn()
    {
        for (var i = 0; i < 100_000; i++)
        {
            var obj = _pool.Get();
            _pool.Return(obj);
        }
    }
}
