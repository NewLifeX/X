using System.Buffers;
using BenchmarkDotNet.Attributes;

namespace Benchmark.PacketBenchmarks;

/// <summary>字节数组分配、池化与拷贝成本测试</summary>
/// <remarks>
/// 覆盖不同大小级别（64B ~ 1MB），分别测量：
/// 1. 堆分配（new byte[]）与 ArrayPool 池化的单位成本；
/// 2. 内存拷贝（Span.CopyTo / Buffer.BlockCopy）的单位成本；
/// 3. 分配+拷贝（等效 ToArray）与池化+拷贝的组合成本。
/// 数据用于评估"零拷贝/缓冲复用"优化在不同数据规模下的收益与代价。
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 20)]
public class BufferCostBenchmark
{
    private Byte[] _src = null!;
    private Byte[] _dst = null!;

    [Params(64, 512, 4096, 16384, 65536, 131072, 1048576)]
    public Int32 Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _src = new Byte[Size];
        _dst = new Byte[Size];
        Random.Shared.NextBytes(_src);
    }

    [Benchmark(Description = "new byte[]")]
    public Byte[] NewArray() => new Byte[Size];

    [Benchmark(Description = "池化：Rent+Return")]
    public void PoolRentReturn()
    {
        var buffer = ArrayPool<Byte>.Shared.Rent(Size);
        ArrayPool<Byte>.Shared.Return(buffer);
    }

    [Benchmark(Description = "拷贝：Span.CopyTo")]
    public void CopySpan() => _src.AsSpan(0, Size).CopyTo(_dst);

    [Benchmark(Description = "拷贝：Buffer.BlockCopy")]
    public void CopyBlock() => Buffer.BlockCopy(_src, 0, _dst, 0, Size);

    [Benchmark(Description = "分配+拷贝（等效 ToArray）")]
    public Byte[] AllocAndCopy() => _src.AsSpan(0, Size).ToArray();

    [Benchmark(Description = "池化+拷贝+归还")]
    public void PoolCopyReturn()
    {
        var buffer = ArrayPool<Byte>.Shared.Rent(Size);
        _src.AsSpan(0, Size).CopyTo(buffer);
        ArrayPool<Byte>.Shared.Return(buffer);
    }
}
