using BenchmarkDotNet.Attributes;
using NewLife.Data;

namespace Benchmark.PacketBenchmarks;

/// <summary>MemoryPacket 性能测试</summary>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 20)]
public class MemoryPacketBenchmark
{
    private Byte[] _data = null!;
    private Memory<Byte> _memory;

    [Params(64, 1024, 8192)]
    public Int32 Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _data = new Byte[Size];
        Random.Shared.NextBytes(_data);
        _memory = new Memory<Byte>(_data);
    }

    [Benchmark(Description = "构造")]
    public MemoryPacket Create() => new(_memory, Size);

    [Benchmark(Description = "GetSpan")]
    public Span<Byte> GetSpan()
    {
        var pk = new MemoryPacket(_memory, Size);
        return pk.GetSpan();
    }

    [Benchmark(Description = "GetMemory")]
    public Memory<Byte> GetMemory()
    {
        var pk = new MemoryPacket(_memory, Size);
        return pk.GetMemory();
    }

    [Benchmark(Description = "TryGetArray")]
    public Boolean TryGetArray()
    {
        var pk = new MemoryPacket(_memory, Size);
        return pk.TryGetArray(out _);
    }

    [Benchmark(Description = "Slice")]
    public IPacket SliceTest()
    {
        var pk = new MemoryPacket(_memory, Size);
        return pk.Slice(10, Size / 2);
    }

    [Benchmark(Description = "Indexer读")]
    public Byte IndexerRead()
    {
        var pk = new MemoryPacket(_memory, Size);
        return pk[Size / 2];
    }

    [Benchmark(Description = "Indexer写")]
    public void IndexerWrite()
    {
        var pk = new MemoryPacket(_memory, Size);
        pk[Size / 2] = 0xFF;
    }
}

/// <summary>MemoryPacket 多线程性能测试</summary>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 20)]
public class MemoryPacketConcurrencyBenchmark
{
    private Byte[] _data = null!;
    private Memory<Byte> _memory;

    [Params(1024)]
    public Int32 Size { get; set; }

    [Params(1, 4, 16, 32)]
    public Int32 ThreadCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _data = new Byte[Size];
        Random.Shared.NextBytes(_data);
        _memory = new Memory<Byte>(_data);
    }

    [Benchmark(Description = "多线程构造")]
    public void ConcurrentCreate()
    {
        Parallel.For(0, ThreadCount, t =>
        {
            for (var i = 0; i < 1000; i++)
            {
                _ = new MemoryPacket(_memory, Size);
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
                var pk = new MemoryPacket(_memory, Size);
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
                var pk = new MemoryPacket(_memory, Size);
                _ = pk.Slice(10, Size / 2);
            }
        });
    }
}
