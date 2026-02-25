using BenchmarkDotNet.Attributes;
using NewLife.Data;

namespace Benchmark.PacketBenchmarks;

/// <summary>ArrayPacket 性能测试</summary>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 20)]
public class ArrayPacketBenchmark
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

    [Benchmark(Description = "构造(byte[])")]
    public ArrayPacket CreateFromArray() => new(_data);

    [Benchmark(Description = "构造(ArraySegment)")]
    public ArrayPacket CreateFromSegment() => new(new ArraySegment<Byte>(_data, 0, Size));

    [Benchmark(Description = "GetSpan")]
    public Span<Byte> GetSpan()
    {
        var pk = new ArrayPacket(_data);
        return pk.GetSpan();
    }

    [Benchmark(Description = "GetMemory")]
    public Memory<Byte> GetMemory()
    {
        var pk = new ArrayPacket(_data);
        return pk.GetMemory();
    }

    [Benchmark(Description = "TryGetArray")]
    public Boolean TryGetArray()
    {
        var pk = new ArrayPacket(_data);
        return pk.TryGetArray(out _);
    }

    [Benchmark(Description = "Slice(struct)")]
    public ArrayPacket SliceStruct()
    {
        var pk = new ArrayPacket(_data);
        return pk.Slice(10, Size / 2);
    }

    [Benchmark(Description = "Slice(IPacket)")]
    public IPacket SliceInterface()
    {
        IPacket pk = new ArrayPacket(_data);
        return pk.Slice(10, Size / 2);
    }

    [Benchmark(Description = "Indexer读")]
    public Byte IndexerRead()
    {
        var pk = new ArrayPacket(_data);
        return pk[Size / 2];
    }

    [Benchmark(Description = "Indexer写")]
    public void IndexerWrite()
    {
        var pk = new ArrayPacket(_data);
        pk[Size / 2] = 0xFF;
    }

    [Benchmark(Description = "隐式转换byte[]")]
    public ArrayPacket ImplicitFromByteArray() => _data;

    [Benchmark(Description = "隐式转换string")]
    public ArrayPacket ImplicitFromString() => "Hello, World!";
}

/// <summary>ArrayPacket 多线程性能测试</summary>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 20)]
public class ArrayPacketConcurrencyBenchmark
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

    [Benchmark(Description = "多线程构造")]
    public void ConcurrentCreate()
    {
        Parallel.For(0, ThreadCount, t =>
        {
            for (var i = 0; i < 1000; i++)
            {
                _ = new ArrayPacket(_data);
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
                var pk = new ArrayPacket(_data);
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
                var pk = new ArrayPacket(_data);
                _ = pk.Slice(10, Size / 2);
            }
        });
    }
}
