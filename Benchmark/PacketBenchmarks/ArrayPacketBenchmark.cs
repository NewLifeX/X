using BenchmarkDotNet.Attributes;
using NewLife.Data;

namespace Benchmark.PacketBenchmarks;

/// <summary>ArrayPacket 性能基准测试</summary>
[MemoryDiagnoser]
[SimpleJob]
public class ArrayPacketBenchmark
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
    public ArrayPacket Create_ByteArray() => new(_data);

    [Benchmark(Description = "构造_ArraySegment")]
    public ArrayPacket Create_Segment() => new(_segment);

    [Benchmark(Description = "构造_偏移")]
    public ArrayPacket Create_WithOffset() => new(_data, DataSize / 4, DataSize / 2);

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
    public ArraySegment<Byte> TryGetArray()
    {
        var pk = new ArrayPacket(_data);
        pk.TryGetArray(out var seg);
        return seg;
    }

    [Benchmark(Description = "Slice")]
    public ArrayPacket Slice()
    {
        var pk = new ArrayPacket(_data);
        return pk.Slice(DataSize / 4, DataSize / 2);
    }

    [Benchmark(Description = "索引器读取")]
    public Byte Indexer_Read()
    {
        var pk = new ArrayPacket(_data);
        return pk[DataSize / 2];
    }

    [Benchmark(Description = "索引器写入")]
    public void Indexer_Write()
    {
        var pk = new ArrayPacket(_data);
        pk[DataSize / 2] = 0xFF;
    }

    [Benchmark(Description = "Total属性")]
    public Int32 Total()
    {
        var pk = new ArrayPacket(_data);
        return pk.Total;
    }

    [Benchmark(Description = "隐式转换_字节数组")]
    public ArrayPacket ImplicitConvert_ByteArray()
    {
        ArrayPacket pk = _data;
        return pk;
    }

    [Benchmark(Description = "隐式转换_字符串")]
    public ArrayPacket ImplicitConvert_String()
    {
        ArrayPacket pk = "Hello, World!";
        return pk;
    }
}
