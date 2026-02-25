using BenchmarkDotNet.Attributes;
using NewLife.Data;

namespace Benchmark.PacketBenchmarks;

/// <summary>OwnerPacket 性能基准测试</summary>
[MemoryDiagnoser]
[SimpleJob]
public class OwnerPacketBenchmark
{
    private Byte[] _data = null!;

    [Params(64, 1024, 65536)]
    public Int32 DataSize;

    [GlobalSetup]
    public void Setup()
    {
        _data = new Byte[DataSize];
        Random.Shared.NextBytes(_data);
    }

    [Benchmark(Description = "构造_从池租用")]
    public OwnerPacket Create_FromPool()
    {
        using var pk = new OwnerPacket(DataSize);
        return pk;
    }

    [Benchmark(Description = "构造_包装数组")]
    public OwnerPacket Create_WrapArray()
    {
        var pk = new OwnerPacket(_data, 0, _data.Length, false);
        return pk;
    }

    [Benchmark(Description = "GetSpan")]
    public Span<Byte> GetSpan()
    {
        using var pk = new OwnerPacket(_data, 0, _data.Length, false);
        return pk.GetSpan();
    }

    [Benchmark(Description = "GetMemory")]
    public Memory<Byte> GetMemory()
    {
        using var pk = new OwnerPacket(_data, 0, _data.Length, false);
        return pk.GetMemory();
    }

    [Benchmark(Description = "TryGetArray")]
    public ArraySegment<Byte> TryGetArray()
    {
        using var pk = new OwnerPacket(_data, 0, _data.Length, false);
        ((IPacket)pk).TryGetArray(out var seg);
        return seg;
    }

    [Benchmark(Description = "Slice_不转移所有权")]
    public IPacket Slice_NoTransfer()
    {
        using var pk = new OwnerPacket(_data, 0, _data.Length, false);
        return pk.Slice(DataSize / 4, DataSize / 2, false);
    }

    [Benchmark(Description = "Slice_转移所有权")]
    public IPacket Slice_Transfer()
    {
        var pk = new OwnerPacket(DataSize);
        var slice = pk.Slice(DataSize / 4, DataSize / 2, true);
        (slice as IDisposable)?.Dispose();
        return slice;
    }

    [Benchmark(Description = "索引器读取")]
    public Byte Indexer_Read()
    {
        using var pk = new OwnerPacket(_data, 0, _data.Length, false);
        return pk[DataSize / 2];
    }

    [Benchmark(Description = "索引器写入")]
    public void Indexer_Write()
    {
        using var pk = new OwnerPacket(_data, 0, _data.Length, false);
        pk[DataSize / 2] = 0xFF;
    }

    [Benchmark(Description = "Resize")]
    public OwnerPacket Resize()
    {
        using var pk = new OwnerPacket(DataSize);
        pk.Resize(DataSize / 2);
        return pk;
    }

    [Benchmark(Description = "Total属性")]
    public Int32 Total()
    {
        using var pk = new OwnerPacket(_data, 0, _data.Length, false);
        return pk.Total;
    }

    [Benchmark(Description = "Dispose归还池")]
    public void Dispose_ReturnPool()
    {
        using var pk = new OwnerPacket(DataSize);
    }
}
