using BenchmarkDotNet.Attributes;
using NewLife.Data;

namespace Benchmark.PacketBenchmarks;

/// <summary>IPacket 多线程并发性能基准测试</summary>
/// <remarks>
/// 测试各 IPacket 实现在不同并发度下的性能表现，
/// 并发度覆盖 1/4/16/32 线程。
/// </remarks>
[MemoryDiagnoser]
[SimpleJob]
public class PacketConcurrencyBenchmark
{
    private Byte[] _data = null!;
    private const Int32 OperationsPerThread = 1000;

    [Params(1, 4, 16, 32)]
    public Int32 ThreadCount;

    [GlobalSetup]
    public void Setup()
    {
        _data = new Byte[1024];
        Random.Shared.NextBytes(_data);
    }

    #region OwnerPacket 并发
    [Benchmark(Description = "OwnerPacket_构造与释放")]
    public void OwnerPacket_CreateDispose()
    {
        RunParallel(() =>
        {
            for (var i = 0; i < OperationsPerThread; i++)
            {
                using var pk = new OwnerPacket(1024);
            }
        });
    }

    [Benchmark(Description = "OwnerPacket_GetSpan")]
    public void OwnerPacket_GetSpan()
    {
        RunParallel(() =>
        {
            for (var i = 0; i < OperationsPerThread; i++)
            {
                using var pk = new OwnerPacket(_data, 0, _data.Length, false);
                _ = pk.GetSpan();
            }
        });
    }

    [Benchmark(Description = "OwnerPacket_Slice")]
    public void OwnerPacket_Slice()
    {
        RunParallel(() =>
        {
            for (var i = 0; i < OperationsPerThread; i++)
            {
                using var pk = new OwnerPacket(_data, 0, _data.Length, false);
                _ = pk.Slice(256, 512, false);
            }
        });
    }
    #endregion

    #region ArrayPacket 并发
    [Benchmark(Description = "ArrayPacket_构造")]
    public void ArrayPacket_Create()
    {
        RunParallel(() =>
        {
            for (var i = 0; i < OperationsPerThread; i++)
            {
                _ = new ArrayPacket(_data);
            }
        });
    }

    [Benchmark(Description = "ArrayPacket_GetSpan")]
    public void ArrayPacket_GetSpan()
    {
        RunParallel(() =>
        {
            for (var i = 0; i < OperationsPerThread; i++)
            {
                var pk = new ArrayPacket(_data);
                _ = pk.GetSpan();
            }
        });
    }

    [Benchmark(Description = "ArrayPacket_Slice")]
    public void ArrayPacket_Slice()
    {
        RunParallel(() =>
        {
            for (var i = 0; i < OperationsPerThread; i++)
            {
                var pk = new ArrayPacket(_data);
                _ = pk.Slice(256, 512);
            }
        });
    }

    [Benchmark(Description = "ArrayPacket_ToArray")]
    public void ArrayPacket_ToArray()
    {
        RunParallel(() =>
        {
            for (var i = 0; i < OperationsPerThread; i++)
            {
                var pk = new ArrayPacket(_data);
                _ = pk.ToArray();
            }
        });
    }
    #endregion

    #region MemoryPacket 并发
    [Benchmark(Description = "MemoryPacket_构造")]
    public void MemoryPacket_Create()
    {
        var memory = new Memory<Byte>(_data);
        RunParallel(() =>
        {
            for (var i = 0; i < OperationsPerThread; i++)
            {
                _ = new MemoryPacket(memory, _data.Length);
            }
        });
    }

    [Benchmark(Description = "MemoryPacket_GetSpan")]
    public void MemoryPacket_GetSpan()
    {
        var memory = new Memory<Byte>(_data);
        RunParallel(() =>
        {
            for (var i = 0; i < OperationsPerThread; i++)
            {
                var pk = new MemoryPacket(memory, _data.Length);
                _ = pk.GetSpan();
            }
        });
    }

    [Benchmark(Description = "MemoryPacket_Slice")]
    public void MemoryPacket_Slice()
    {
        var memory = new Memory<Byte>(_data);
        RunParallel(() =>
        {
            for (var i = 0; i < OperationsPerThread; i++)
            {
                var pk = new MemoryPacket(memory, _data.Length);
                _ = pk.Slice(256, 512);
            }
        });
    }
    #endregion

    #region ReadOnlyPacket 并发
    [Benchmark(Description = "ReadOnlyPacket_构造")]
    public void ReadOnlyPacket_Create()
    {
        RunParallel(() =>
        {
            for (var i = 0; i < OperationsPerThread; i++)
            {
                _ = new ReadOnlyPacket(_data);
            }
        });
    }

    [Benchmark(Description = "ReadOnlyPacket_GetSpan")]
    public void ReadOnlyPacket_GetSpan()
    {
        RunParallel(() =>
        {
            for (var i = 0; i < OperationsPerThread; i++)
            {
                var pk = new ReadOnlyPacket(_data);
                _ = pk.GetSpan();
            }
        });
    }

    [Benchmark(Description = "ReadOnlyPacket_Slice")]
    public void ReadOnlyPacket_Slice()
    {
        RunParallel(() =>
        {
            for (var i = 0; i < OperationsPerThread; i++)
            {
                var pk = new ReadOnlyPacket(_data);
                _ = pk.Slice(256, 512);
            }
        });
    }
    #endregion

    #region PacketHelper 并发
    [Benchmark(Description = "PacketHelper_ToStr")]
    public void PacketHelper_ToStr()
    {
        RunParallel(() =>
        {
            for (var i = 0; i < OperationsPerThread; i++)
            {
                var pk = new ArrayPacket(_data);
                _ = pk.ToStr();
            }
        });
    }

    [Benchmark(Description = "PacketHelper_ToHex")]
    public void PacketHelper_ToHex()
    {
        RunParallel(() =>
        {
            for (var i = 0; i < OperationsPerThread; i++)
            {
                var pk = new ArrayPacket(_data, 0, 32);
                _ = pk.ToHex();
            }
        });
    }

    [Benchmark(Description = "PacketHelper_Clone")]
    public void PacketHelper_Clone()
    {
        RunParallel(() =>
        {
            for (var i = 0; i < OperationsPerThread; i++)
            {
                var pk = new ArrayPacket(_data);
                _ = pk.Clone();
            }
        });
    }

    [Benchmark(Description = "PacketHelper_ReadBytes")]
    public void PacketHelper_ReadBytes()
    {
        RunParallel(() =>
        {
            for (var i = 0; i < OperationsPerThread; i++)
            {
                var pk = new ArrayPacket(_data);
                _ = pk.ReadBytes(0, 512);
            }
        });
    }

    [Benchmark(Description = "PacketHelper_ToSegment")]
    public void PacketHelper_ToSegment()
    {
        RunParallel(() =>
        {
            for (var i = 0; i < OperationsPerThread; i++)
            {
                var pk = new ArrayPacket(_data);
                _ = pk.ToSegment();
            }
        });
    }
    #endregion

    private void RunParallel(Action action)
    {
        if (ThreadCount == 1)
        {
            action();
            return;
        }

        var threads = new Thread[ThreadCount];
        for (var i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(() => action()) { IsBackground = true };
        }
        for (var i = 0; i < threads.Length; i++)
        {
            threads[i].Start();
        }
        for (var i = 0; i < threads.Length; i++)
        {
            threads[i].Join();
        }
    }
}
