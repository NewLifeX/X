using BenchmarkDotNet.Attributes;
using NewLife.Data;

namespace Benchmark.PacketBenchmarks;

/// <summary>IPacket 实现者横向对比基准测试</summary>
/// <remarks>
/// 在相同操作下横向对比四种 IPacket 实现的性能差异，
/// 帮助开发者选择最合适的实现。
/// </remarks>
[MemoryDiagnoser]
[SimpleJob]
public class PacketComparisonBenchmark
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

    #region 构造对比
    [Benchmark(Description = "构造_OwnerPacket", Baseline = true)]
    public IPacket Create_OwnerPacket()
    {
        using var pk = new OwnerPacket(DataSize);
        return pk;
    }

    [Benchmark(Description = "构造_MemoryPacket")]
    public IPacket Create_MemoryPacket() => new MemoryPacket(_memory, DataSize);

    [Benchmark(Description = "构造_ArrayPacket")]
    public IPacket Create_ArrayPacket() => new ArrayPacket(_data);

    [Benchmark(Description = "构造_ReadOnlyPacket")]
    public IPacket Create_ReadOnlyPacket() => new ReadOnlyPacket(_data);
    #endregion

    #region GetSpan 对比
    [Benchmark(Description = "GetSpan_OwnerPacket")]
    public Span<Byte> GetSpan_OwnerPacket()
    {
        using var pk = new OwnerPacket(_data, 0, _data.Length, false);
        return pk.GetSpan();
    }

    [Benchmark(Description = "GetSpan_MemoryPacket")]
    public Span<Byte> GetSpan_MemoryPacket()
    {
        var pk = new MemoryPacket(_memory, DataSize);
        return pk.GetSpan();
    }

    [Benchmark(Description = "GetSpan_ArrayPacket")]
    public Span<Byte> GetSpan_ArrayPacket()
    {
        var pk = new ArrayPacket(_data);
        return pk.GetSpan();
    }

    [Benchmark(Description = "GetSpan_ReadOnlyPacket")]
    public Span<Byte> GetSpan_ReadOnlyPacket()
    {
        var pk = new ReadOnlyPacket(_data);
        return pk.GetSpan();
    }
    #endregion

    #region GetMemory 对比
    [Benchmark(Description = "GetMemory_OwnerPacket")]
    public Memory<Byte> GetMemory_OwnerPacket()
    {
        using var pk = new OwnerPacket(_data, 0, _data.Length, false);
        return pk.GetMemory();
    }

    [Benchmark(Description = "GetMemory_MemoryPacket")]
    public Memory<Byte> GetMemory_MemoryPacket()
    {
        var pk = new MemoryPacket(_memory, DataSize);
        return pk.GetMemory();
    }

    [Benchmark(Description = "GetMemory_ArrayPacket")]
    public Memory<Byte> GetMemory_ArrayPacket()
    {
        var pk = new ArrayPacket(_data);
        return pk.GetMemory();
    }

    [Benchmark(Description = "GetMemory_ReadOnlyPacket")]
    public Memory<Byte> GetMemory_ReadOnlyPacket()
    {
        var pk = new ReadOnlyPacket(_data);
        return pk.GetMemory();
    }
    #endregion

    #region Slice 对比
    [Benchmark(Description = "Slice_OwnerPacket")]
    public IPacket Slice_OwnerPacket()
    {
        using var pk = new OwnerPacket(_data, 0, _data.Length, false);
        return pk.Slice(DataSize / 4, DataSize / 2, false);
    }

    [Benchmark(Description = "Slice_MemoryPacket")]
    public IPacket Slice_MemoryPacket()
    {
        var pk = new MemoryPacket(_memory, DataSize);
        return pk.Slice(DataSize / 4, DataSize / 2);
    }

    [Benchmark(Description = "Slice_ArrayPacket")]
    public IPacket Slice_ArrayPacket()
    {
        var pk = new ArrayPacket(_data);
        return pk.Slice(DataSize / 4, DataSize / 2);
    }

    [Benchmark(Description = "Slice_ReadOnlyPacket")]
    public IPacket Slice_ReadOnlyPacket()
    {
        var pk = new ReadOnlyPacket(_data);
        return pk.Slice(DataSize / 4, DataSize / 2);
    }
    #endregion

    #region TryGetArray 对比
    [Benchmark(Description = "TryGetArray_OwnerPacket")]
    public Boolean TryGetArray_OwnerPacket()
    {
        using var pk = new OwnerPacket(_data, 0, _data.Length, false);
        return ((IPacket)pk).TryGetArray(out _);
    }

    [Benchmark(Description = "TryGetArray_MemoryPacket")]
    public Boolean TryGetArray_MemoryPacket()
    {
        var pk = new MemoryPacket(_memory, DataSize);
        return pk.TryGetArray(out _);
    }

    [Benchmark(Description = "TryGetArray_ArrayPacket")]
    public Boolean TryGetArray_ArrayPacket()
    {
        var pk = new ArrayPacket(_data);
        return pk.TryGetArray(out _);
    }

    [Benchmark(Description = "TryGetArray_ReadOnlyPacket")]
    public Boolean TryGetArray_ReadOnlyPacket()
    {
        var pk = new ReadOnlyPacket(_data);
        return pk.TryGetArray(out _);
    }
    #endregion

    #region 索引器对比
    [Benchmark(Description = "Indexer_OwnerPacket")]
    public Byte Indexer_OwnerPacket()
    {
        using var pk = new OwnerPacket(_data, 0, _data.Length, false);
        return pk[DataSize / 2];
    }

    [Benchmark(Description = "Indexer_MemoryPacket")]
    public Byte Indexer_MemoryPacket()
    {
        var pk = new MemoryPacket(_memory, DataSize);
        return pk[DataSize / 2];
    }

    [Benchmark(Description = "Indexer_ArrayPacket")]
    public Byte Indexer_ArrayPacket()
    {
        var pk = new ArrayPacket(_data);
        return pk[DataSize / 2];
    }

    [Benchmark(Description = "Indexer_ReadOnlyPacket")]
    public Byte Indexer_ReadOnlyPacket()
    {
        var pk = new ReadOnlyPacket(_data);
        return pk[DataSize / 2];
    }
    #endregion
}
