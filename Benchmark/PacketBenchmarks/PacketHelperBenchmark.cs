using BenchmarkDotNet.Attributes;
using NewLife.Data;

namespace Benchmark.PacketBenchmarks;

/// <summary>PacketHelper 扩展方法性能测试</summary>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 20)]
public class PacketHelperBenchmark
{
    private Byte[] _data = null!;
    private ArrayPacket _arrayPacket;
    private ReadOnlyPacket _readOnlyPacket;

    [Params(64, 1024, 8192)]
    public Int32 Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _data = new Byte[Size];
        Random.Shared.NextBytes(_data);
        _arrayPacket = new ArrayPacket(_data);
        _readOnlyPacket = new ReadOnlyPacket(_data);
    }

    #region 链式操作
    [Benchmark(Description = "Append(IPacket)")]
    public IPacket AppendPacket()
    {
        var pk1 = new ArrayPacket(_data);
        var pk2 = new ArrayPacket(_data);
        return pk1.Append(pk2);
    }

    [Benchmark(Description = "Append(byte[])")]
    public IPacket AppendBytes()
    {
        var pk = new ArrayPacket(_data);
        return pk.Append(_data);
    }
    #endregion

    #region 数据转换
    [Benchmark(Description = "ToStr(单包)")]
    public String ToStrSingle()
    {
        IPacket pk = _arrayPacket;
        return pk.ToStr();
    }

    [Benchmark(Description = "ToStr(链式包)")]
    public String ToStrChained()
    {
        var pk1 = new ArrayPacket(_data);
        var pk2 = new ArrayPacket(_data);
        pk1.Next = pk2;
        IPacket pk = pk1;
        return pk.ToStr();
    }

    [Benchmark(Description = "ToHex(单包)")]
    public String ToHexSingle()
    {
        IPacket pk = _arrayPacket;
        return pk.ToHex();
    }

    [Benchmark(Description = "ToHex(链式包)")]
    public String ToHexChained()
    {
        var pk1 = new ArrayPacket(_data);
        var pk2 = new ArrayPacket(_data);
        pk1.Next = pk2;
        IPacket pk = pk1;
        return pk.ToHex();
    }
    #endregion

    #region 流操作
    [Benchmark(Description = "CopyTo")]
    public void CopyToStream()
    {
        IPacket pk = _arrayPacket;
        using var ms = new MemoryStream();
        pk.CopyTo(ms);
    }

    [Benchmark(Description = "GetStream")]
    public Stream GetStreamTest()
    {
        IPacket pk = _arrayPacket;
        return pk.GetStream();
    }
    #endregion

    #region 数据段操作
    [Benchmark(Description = "ToSegment(单包)")]
    public ArraySegment<Byte> ToSegmentSingle()
    {
        IPacket pk = _arrayPacket;
        return pk.ToSegment();
    }

    [Benchmark(Description = "ToSegment(链式包)")]
    public ArraySegment<Byte> ToSegmentChained()
    {
        var pk1 = new ArrayPacket(_data);
        var pk2 = new ArrayPacket(_data);
        pk1.Next = pk2;
        IPacket pk = pk1;
        return pk.ToSegment();
    }

    [Benchmark(Description = "ToSegments")]
    public IList<ArraySegment<Byte>> ToSegmentsTest()
    {
        IPacket pk = _arrayPacket;
        return pk.ToSegments();
    }

    [Benchmark(Description = "ToArray")]
    public Byte[] ToArrayTest()
    {
        IPacket pk = _arrayPacket;
        return pk.ToArray();
    }
    #endregion

    #region 数据读取
    [Benchmark(Description = "ReadBytes")]
    public Byte[] ReadBytesTest()
    {
        IPacket pk = _arrayPacket;
        return pk.ReadBytes(0, Size / 2);
    }

    [Benchmark(Description = "Clone")]
    public IPacket CloneTest()
    {
        IPacket pk = _arrayPacket;
        return pk.Clone();
    }
    #endregion

    #region 内存访问
    [Benchmark(Description = "TryGetSpan")]
    public Boolean TryGetSpanTest()
    {
        IPacket pk = _arrayPacket;
        return pk.TryGetSpan(out _);
    }
    #endregion

    #region 头部扩展
    [Benchmark(Description = "ExpandHeader(ArrayPacket)")]
    public IPacket ExpandHeaderArrayPacket()
    {
        var pk = new ArrayPacket(_data, 16, Size - 16);
        return pk.ExpandHeader(8);
    }

    [Benchmark(Description = "ExpandHeader(新建)")]
    public IPacket ExpandHeaderNew()
    {
        IPacket pk = _arrayPacket;
        return pk.ExpandHeader(8);
    }
    #endregion
}

/// <summary>PacketHelper 多线程性能测试</summary>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 20)]
public class PacketHelperConcurrencyBenchmark
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

    [Benchmark(Description = "多线程ToStr")]
    public void ConcurrentToStr()
    {
        Parallel.For(0, ThreadCount, t =>
        {
            for (var i = 0; i < 1000; i++)
            {
                IPacket pk = new ArrayPacket(_data);
                _ = pk.ToStr();
            }
        });
    }

    [Benchmark(Description = "多线程ToArray")]
    public void ConcurrentToArray()
    {
        Parallel.For(0, ThreadCount, t =>
        {
            for (var i = 0; i < 1000; i++)
            {
                IPacket pk = new ArrayPacket(_data);
                _ = pk.ToArray();
            }
        });
    }

    [Benchmark(Description = "多线程Clone")]
    public void ConcurrentClone()
    {
        Parallel.For(0, ThreadCount, t =>
        {
            for (var i = 0; i < 1000; i++)
            {
                IPacket pk = new ArrayPacket(_data);
                _ = pk.Clone();
            }
        });
    }

    [Benchmark(Description = "多线程ReadBytes")]
    public void ConcurrentReadBytes()
    {
        Parallel.For(0, ThreadCount, t =>
        {
            for (var i = 0; i < 1000; i++)
            {
                IPacket pk = new ArrayPacket(_data);
                _ = pk.ReadBytes(0, Size / 2);
            }
        });
    }

    [Benchmark(Description = "多线程ExpandHeader")]
    public void ConcurrentExpandHeader()
    {
        Parallel.For(0, ThreadCount, t =>
        {
            for (var i = 0; i < 1000; i++)
            {
                IPacket pk = new ArrayPacket(_data);
                _ = pk.ExpandHeader(8);
            }
        });
    }
}
