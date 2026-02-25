using System.Buffers;
using BenchmarkDotNet.Attributes;
using NewLife.Data;

namespace Benchmark.PacketBenchmarks;

/// <summary>OwnerPacket 性能测试</summary>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 20)]
public class OwnerPacketBenchmark
{
    private Byte[] _data = null!;

    [Params(64, 1024, 8192)]
    public Int32 Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _data = new Byte[Size];
        Random.Shared.NextBytes(_data);
    }

    [Benchmark(Description = "构造+释放")]
    public void CreateAndDispose()
    {
        using var pk = new OwnerPacket(Size);
    }

    [Benchmark(Description = "GetSpan")]
    public Span<Byte> GetSpan()
    {
        using var pk = new OwnerPacket(Size);
        return pk.GetSpan();
    }

    [Benchmark(Description = "GetMemory")]
    public Memory<Byte> GetMemory()
    {
        using var pk = new OwnerPacket(Size);
        return pk.GetMemory();
    }

    [Benchmark(Description = "TryGetArray")]
    public Boolean TryGetArray()
    {
        using var pk = new OwnerPacket(Size);
        return pk.TryGetArray(out _);
    }

    [Benchmark(Description = "Slice")]
    public IPacket SliceTest()
    {
        var pk = new OwnerPacket(_data, 0, _data.Length, false);
        return pk.Slice(10, Size / 2, false);
    }

    [Benchmark(Description = "Resize")]
    public OwnerPacket ResizeTest()
    {
        var pk = new OwnerPacket(_data, 0, _data.Length, false);
        return pk.Resize(Size / 2);
    }

    [Benchmark(Description = "Indexer读")]
    public Byte IndexerRead()
    {
        var pk = new OwnerPacket(_data, 0, _data.Length, false);
        return pk[Size / 2];
    }

    [Benchmark(Description = "Indexer写")]
    public void IndexerWrite()
    {
        var pk = new OwnerPacket(_data, 0, _data.Length, false);
        pk[Size / 2] = 0xFF;
    }
}

/// <summary>OwnerPacket 多线程性能测试</summary>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 20)]
public class OwnerPacketConcurrencyBenchmark
{
    private Byte[] _data = null!;

    [Params(1024)]
    public Int32 Size { get; set; }

    [Params(1, 4, 16, 32)]
    public Int32 ThreadCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _data = new Byte[Size];
        Random.Shared.NextBytes(_data);
    }

    [Benchmark(Description = "多线程构造+释放")]
    public void ConcurrentCreateAndDispose()
    {
        Parallel.For(0, ThreadCount, t =>
        {
            for (var i = 0; i < 1000; i++)
            {
                using var pk = new OwnerPacket(Size);
            }
        });
    }

    [Benchmark(Description = "多线程GetSpan")]
    public void ConcurrentGetSpan()
    {
        Parallel.For(0, ThreadCount, t =>
        {
            for (var i = 0; i < 1000; i++)
            {
                using var pk = new OwnerPacket(Size);
                _ = pk.GetSpan();
            }
        });
    }

    [Benchmark(Description = "多线程Slice")]
    public void ConcurrentSlice()
    {
        Parallel.For(0, ThreadCount, t =>
        {
            for (var i = 0; i < 1000; i++)
            {
                var pk = new OwnerPacket(_data, 0, _data.Length, false);
                _ = pk.Slice(10, Size / 2, false);
            }
        });
    }
}
