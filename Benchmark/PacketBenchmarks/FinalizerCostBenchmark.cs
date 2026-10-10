using System.Buffers;
using BenchmarkDotNet.Attributes;

namespace Benchmark.PacketBenchmarks;

/// <summary>析构兜底成本对照：无析构 vs 带析构的所有权句柄</summary>
/// <remarks>
/// 两个迷你类镜像 OwnerPacket 的关键结构（ArrayPool 租借 + 引用计数 owner + Dispose/SuppressFinalize），
/// 实验组额外带析构函数。对照点：
/// 1）分配路径：终结队列注册（现代 CoreCLR 每次分配都会登记可终结对象）；
/// 2）JIT 逃逸分析栈分配资格（带析构类型一律禁止栈分配）；
/// 3）Dispose 期 GC.SuppressFinalize 成本。
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class FinalizerCostBenchmark
{
    private const Int32 BufferSize = 1024;
    private const Int32 ThreadCount = 4;
    private const Int32 InnerCount = 1000;

    [Benchmark(Description = "构造+释放（无析构）")]
    public void ConstructDispose_Plain()
    {
        using var handle = new PlainHandle(BufferSize);
    }

    [Benchmark(Description = "构造+释放（带析构）")]
    public void ConstructDispose_Finalizable()
    {
        using var handle = new FinalizableHandle(BufferSize);
    }

    [Benchmark(Description = "4线程构造+释放（无析构）")]
    public void Parallel_Plain()
    {
        Parallel.For(0, ThreadCount, _ =>
        {
            for (var i = 0; i < InnerCount; i++)
            {
                using var handle = new PlainHandle(BufferSize);
            }
        });
    }

    [Benchmark(Description = "4线程构造+释放（带析构）")]
    public void Parallel_Finalizable()
    {
        Parallel.For(0, ThreadCount, _ =>
        {
            for (var i = 0; i < InnerCount; i++)
            {
                using var handle = new FinalizableHandle(BufferSize);
            }
        });
    }
}

/// <summary>漏释放场景对照：丢弃句柄后强制完整回收，观察复活/终结线程成本</summary>
/// <remarks>
/// 句柄创建后直接丢弃（模拟漏 Dispose）。无析构版本的池缓冲永久流失；
/// 带析构版本由终结线程兜底归还，但需要额外一轮完整回收并等待 WaitForPendingFinalizers。
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 8)]
public class FinalizerLeakBenchmark
{
    private const Int32 BufferSize = 1024;
    private const Int32 BatchSize = 1000;

    private Object? _sink;

    [Benchmark(Description = "泄漏1000句柄+强制回收（无析构）")]
    public void LeakBatch_Plain()
    {
        // 写入字段强制对象逃逸到堆，避免被逃逸分析栈分配
        for (var i = 0; i < BatchSize; i++) _sink = new PlainHandle(BufferSize);
        _sink = null;

        CollectAll();
    }

    [Benchmark(Description = "泄漏1000句柄+强制回收（带析构）")]
    public void LeakBatch_Finalizable()
    {
        for (var i = 0; i < BatchSize; i++) _sink = new FinalizableHandle(BufferSize);
        _sink = null;

        CollectAll();
    }

    [GlobalCleanup]
    public void Report()
    {
        Console.WriteLine($"[FinalizerLeakBenchmark] GC 暂停合计 {GC.GetTotalPauseDuration().TotalMilliseconds:F1}ms，Gen2 收集 {GC.CollectionCount(2)} 次");
    }

    /// <summary>强制完整回收并等待终结线程排空</summary>
    private static void CollectAll()
    {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
    }
}

/// <summary>JIT 栈分配对照：反汇编构造+释放路径，检查是否存在堆分配助手调用（CORINFO_HELP_NEWSFAST）</summary>
/// <remarks>无析构组若不出现堆分配助手调用，说明节点被逃逸分析栈分配；带析构组因 canAllocateOnStack 返回 false，必然保留堆分配。</remarks>
[MemoryDiagnoser]
[DisassemblyDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 5)]
public class FinalizerAsmBenchmark
{
    private const Int32 BufferSize = 1024;

    [Benchmark(Description = "构造+释放（无析构）")]
    public void ConstructDispose_Plain()
    {
        using var handle = new PlainHandle(BufferSize);
    }

    [Benchmark(Description = "构造+释放（带析构）")]
    public void ConstructDispose_Finalizable()
    {
        using var handle = new FinalizableHandle(BufferSize);
    }
}

/// <summary>引用计数缓冲区所有者（镜像 ArrayOwner，保留对照所需的最小结构）</summary>
internal sealed class RefCountedPoolOwner(Byte[] buffer)
{
    private Int32 _refCount = 1;

    /// <summary>释放一个引用，归零时归还内存池</summary>
    public void Release()
    {
        if (Interlocked.Decrement(ref _refCount) == 0) ArrayPool<Byte>.Shared.Return(buffer);
    }
}

/// <summary>对照组：无析构的所有权句柄（镜像 OwnerPacket 的构造/释放路径）</summary>
internal sealed class PlainHandle : IDisposable
{
    private RefCountedPoolOwner? _owner;

    public PlainHandle(Int32 length)
    {
        var buffer = ArrayPool<Byte>.Shared.Rent(length);
        _owner = new RefCountedPoolOwner(buffer);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        var owner = _owner;
        if (owner != null)
        {
            _owner = null;
            owner.Release();
        }
    }
}

/// <summary>实验组：带析构兜底的所有权句柄（镜像 OwnerPacket 的析构兜底）</summary>
internal sealed class FinalizableHandle : IDisposable
{
    private RefCountedPoolOwner? _owner;

    public FinalizableHandle(Int32 length)
    {
        var buffer = ArrayPool<Byte>.Shared.Rent(length);
        _owner = new RefCountedPoolOwner(buffer);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        var owner = _owner;
        if (owner != null)
        {
            _owner = null;
            owner.Release();
        }
    }

    /// <summary>析构兜底：漏释放的句柄在 GC 时归还引用（镜像 OwnerPacket 的析构路径）</summary>
    ~FinalizableHandle()
    {
        var owner = _owner;
        if (owner != null)
        {
            _owner = null;
            owner.Release();
        }
    }
}

/// <summary>Server GC 下的对照：验证终结登记队列按堆分离后，多线程锁竞争是否缓解</summary>
/// <remarks>与 <see cref="FinalizerCostBenchmark"/> 同方法集，仅 GC 模式不同（Server GC 下每堆一条终结登记队列）。</remarks>
[MemoryDiagnoser]
[GcServer(true)]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class FinalizerCostServerGcBenchmark : FinalizerCostBenchmark
{
}
