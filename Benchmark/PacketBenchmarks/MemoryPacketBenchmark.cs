using BenchmarkDotNet.Attributes;
using NewLife.Data;

namespace Benchmark.PacketBenchmarks;

/// <summary>MemoryPacket 性能基准测试</summary>
[MemoryDiagnoser]
[SimpleJob]
public class MemoryPacketBenchmark
{
    private Byte[] _data = null!;
    private Memory<Byte> _memory;

    [Params(64, 1024, 65536)]
    public Int32 DataSize;

    [GlobalSetup]
    public void Setup()
    {
        _data = new Byte[DataSize];
        Random.Shared.NextBytes(_data);
        _memory = new Memory<Byte>(_data);
    }

    [Benchmark(Description = "构造")]
    public MemoryPacket Create() => new(_memory, DataSize);

    [Benchmark(Description = "GetSpan")]
    public Span<Byte> GetSpan()
    {
        var pk = new MemoryPacket(_memory, DataSize);
        return pk.GetSpan();
    }

    [Benchmark(Description = "GetMemory")]
    public Memory<Byte> GetMemory()
    {
        var pk = new MemoryPacket(_memory, DataSize);
        return pk.GetMemory();
    }

    [Benchmark(Description = "TryGetArray")]
    public ArraySegment<Byte> TryGetArray()
    {
        var pk = new MemoryPacket(_memory, DataSize);
        pk.TryGetArray(out var seg);
        return seg;
    }

    [Benchmark(Description = "Slice")]
    public IPacket Slice()
    {
        var pk = new MemoryPacket(_memory, DataSize);
        return pk.Slice(DataSize / 4, DataSize / 2);
    }

    [Benchmark(Description = "索引器读取")]
    public Byte Indexer_Read()
    {
        var pk = new MemoryPacket(_memory, DataSize);
        return pk[DataSize / 2];
    }

    [Benchmark(Description = "索引器写入")]
    public void Indexer_Write()
    {
        var pk = new MemoryPacket(_memory, DataSize);
        pk[DataSize / 2] = 0xFF;
    }

    [Benchmark(Description = "Total属性")]
    public Int32 Total()
    {
        var pk = new MemoryPacket(_memory, DataSize);
        return pk.Total;
    }
}
