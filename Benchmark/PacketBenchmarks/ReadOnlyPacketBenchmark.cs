using BenchmarkDotNet.Attributes;
using NewLife.Data;

namespace Benchmark.PacketBenchmarks;

/// <summary>ReadOnlyPacket 性能基准测试</summary>
[MemoryDiagnoser]
[SimpleJob]
public class ReadOnlyPacketBenchmark
{
    private Byte[] _data = null!;
    private ArraySegment<Byte> _segment;

    [Params(64, 1024, 65536)]
    public Int32 DataSize;

    [GlobalSetup]
    public void Setup()
    {
        _data = new Byte[DataSize];
        Random.Shared.NextBytes(_data);
        _segment = new ArraySegment<Byte>(_data, 0, DataSize);
    }

    [Benchmark(Description = "构造_字节数组")]
    public ReadOnlyPacket Create_ByteArray() => new(_data);

    [Benchmark(Description = "构造_ArraySegment")]
    public ReadOnlyPacket Create_Segment() => new(_segment);

    [Benchmark(Description = "构造_偏移")]
    public ReadOnlyPacket Create_WithOffset() => new(_data, DataSize / 4, DataSize / 2);

    [Benchmark(Description = "构造_从IPacket深拷贝")]
    public ReadOnlyPacket Create_FromIPacket()
    {
        var src = new ArrayPacket(_data);
        return new ReadOnlyPacket(src);
    }

    [Benchmark(Description = "GetSpan")]
    public Span<Byte> GetSpan()
    {
        var pk = new ReadOnlyPacket(_data);
        return pk.GetSpan();
    }

    [Benchmark(Description = "GetMemory")]
    public Memory<Byte> GetMemory()
    {
        var pk = new ReadOnlyPacket(_data);
        return pk.GetMemory();
    }

    [Benchmark(Description = "TryGetArray")]
    public ArraySegment<Byte> TryGetArray()
    {
        var pk = new ReadOnlyPacket(_data);
        pk.TryGetArray(out var seg);
        return seg;
    }

    [Benchmark(Description = "Slice")]
    public IPacket Slice()
    {
        var pk = new ReadOnlyPacket(_data);
        return pk.Slice(DataSize / 4, DataSize / 2);
    }

    [Benchmark(Description = "索引器读取")]
    public Byte Indexer_Read()
    {
        var pk = new ReadOnlyPacket(_data);
        return pk[DataSize / 2];
    }

    [Benchmark(Description = "Total属性")]
    public Int32 Total()
    {
        var pk = new ReadOnlyPacket(_data);
        return pk.Total;
    }

    [Benchmark(Description = "ToArray")]
    public Byte[] ToArray()
    {
        var pk = new ReadOnlyPacket(_data);
        return pk.ToArray();
    }

    [Benchmark(Description = "隐式转换_字节数组")]
    public ReadOnlyPacket ImplicitConvert_ByteArray()
    {
        ReadOnlyPacket pk = _data;
        return pk;
    }
}
