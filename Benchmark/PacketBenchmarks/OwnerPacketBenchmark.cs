using System.Buffers;
using BenchmarkDotNet.Attributes;
using NewLife;
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

    [Benchmark(Description = "无主源切片+双方释放")]
    public void SliceDispose()
    {
        var pk = new OwnerPacket(_data, 0, _data.Length, false);
        var slice = pk.Slice(10, Size / 2);
        slice.TryDispose();
        pk.TryDispose();
    }

    [Benchmark(Description = "切片+遗弃（未释放，观察终结代价）")]
    public IPacket SliceAbandon()
    {
        var pk = new OwnerPacket(_data, 0, _data.Length, false);
        return pk.Slice(10, Size / 2);
    }

    [Benchmark(Description = "共享切片+双方释放")]
    public void SliceThenDispose()
    {
        using var pk = new OwnerPacket(Size);
        var slice = pk.Slice(10, Size - 10);
        slice.TryDispose();
    }

    [Benchmark(Description = "链式共享切片+释放")]
    public void ChainSliceDispose()
    {
        var first = new OwnerPacket(Size / 2);
        var second = new OwnerPacket(Size / 2);
        first.Next = second;

        var slice = first.Slice(Size / 4, Size - Size / 4);
        slice.TryDispose();
        first.TryDispose();
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
                _ = pk.Slice(10, Size / 2);
            }
        });
    }
}

/// <summary>Server GC 下的多线程性能对照（与 OwnerPacketConcurrencyBenchmark 同方法集）</summary>
[MemoryDiagnoser]
[GcServer(true)]
[SimpleJob(iterationCount: 20)]
public class OwnerPacketConcurrencyServerGcBenchmark : OwnerPacketConcurrencyBenchmark
{
}
