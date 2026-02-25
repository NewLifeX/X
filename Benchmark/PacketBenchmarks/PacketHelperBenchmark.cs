using System.Text;
using BenchmarkDotNet.Attributes;
using NewLife.Data;

namespace Benchmark.PacketBenchmarks;

/// <summary>PacketHelper 扩展方法性能基准测试</summary>
[MemoryDiagnoser]
[SimpleJob]
public class PacketHelperBenchmark
{
    private Byte[] _data = null!;
    private Byte[] _appendData = null!;

    [Params(64, 1024, 65536)]
    public Int32 DataSize;

    [GlobalSetup]
    public void Setup()
    {
        _data = new Byte[DataSize];
        _appendData = new Byte[64];
        Random.Shared.NextBytes(_data);
        Random.Shared.NextBytes(_appendData);
    }

    #region 链式操作
    [Benchmark(Description = "Append_IPacket")]
    public IPacket Append_IPacket()
    {
        var pk = new ArrayPacket(_data);
        var next = new ArrayPacket(_appendData);
        return pk.Append(next);
    }

    [Benchmark(Description = "Append_ByteArray")]
    public IPacket Append_ByteArray()
    {
        var pk = new ArrayPacket(_data);
        return pk.Append(_appendData);
    }
    #endregion

    #region 数据转换
    [Benchmark(Description = "ToStr_单包")]
    public String ToStr_Single()
    {
        var pk = new ArrayPacket(_data);
        return pk.ToStr(Encoding.UTF8);
    }

    [Benchmark(Description = "ToStr_链式包")]
    public String ToStr_Chained()
    {
        var pk = new ArrayPacket(_data);
        pk.Next = new ArrayPacket(_appendData);
        return pk.ToStr(Encoding.UTF8);
    }

    [Benchmark(Description = "ToHex_单包")]
    public String ToHex_Single()
    {
        var pk = new ArrayPacket(_data, 0, Math.Min(DataSize, 32));
        return pk.ToHex();
    }

    [Benchmark(Description = "ToHex_带分隔符")]
    public String ToHex_WithSeparator()
    {
        var pk = new ArrayPacket(_data, 0, Math.Min(DataSize, 32));
        return pk.ToHex(32, "-");
    }

    [Benchmark(Description = "ToHex_链式包")]
    public String ToHex_Chained()
    {
        var pk = new ArrayPacket(_data, 0, Math.Min(DataSize, 16));
        pk.Next = new ArrayPacket(_appendData, 0, 16);
        return pk.ToHex(32);
    }
    #endregion

    #region 流操作
    [Benchmark(Description = "CopyTo")]
    public void CopyTo()
    {
        var pk = new ArrayPacket(_data);
        using var ms = new MemoryStream(DataSize);
        pk.CopyTo(ms);
    }

    [Benchmark(Description = "GetStream_单包")]
    public Stream GetStream_Single()
    {
        var pk = new ArrayPacket(_data);
        var stream = pk.GetStream();
        stream.Dispose();
        return stream;
    }

    [Benchmark(Description = "GetStream_链式包")]
    public Stream GetStream_Chained()
    {
        var pk = new ArrayPacket(_data);
        pk.Next = new ArrayPacket(_appendData);
        var stream = pk.GetStream();
        stream.Dispose();
        return stream;
    }
    #endregion

    #region 数据段操作
    [Benchmark(Description = "ToSegment_单包")]
    public ArraySegment<Byte> ToSegment_Single()
    {
        var pk = new ArrayPacket(_data);
        return pk.ToSegment();
    }

    [Benchmark(Description = "ToSegment_链式包")]
    public ArraySegment<Byte> ToSegment_Chained()
    {
        var pk = new ArrayPacket(_data);
        pk.Next = new ArrayPacket(_appendData);
        return pk.ToSegment();
    }

    [Benchmark(Description = "ToSegments")]
    public IList<ArraySegment<Byte>> ToSegments()
    {
        var pk = new ArrayPacket(_data);
        pk.Next = new ArrayPacket(_appendData);
        return pk.ToSegments();
    }

    [Benchmark(Description = "ToArray_单包")]
    public Byte[] ToArray_Single()
    {
        var pk = new ArrayPacket(_data);
        return pk.ToArray();
    }

    [Benchmark(Description = "ToArray_链式包")]
    public Byte[] ToArray_Chained()
    {
        var pk = new ArrayPacket(_data);
        pk.Next = new ArrayPacket(_appendData);
        return pk.ToArray();
    }
    #endregion

    #region 数据读取
    [Benchmark(Description = "ReadBytes_全部")]
    public Byte[] ReadBytes_All()
    {
        var pk = new ArrayPacket(_data);
        return pk.ReadBytes();
    }

    [Benchmark(Description = "ReadBytes_切片")]
    public Byte[] ReadBytes_Slice()
    {
        var pk = new ArrayPacket(_data);
        return pk.ReadBytes(DataSize / 4, DataSize / 2);
    }

    [Benchmark(Description = "Clone")]
    public IPacket Clone()
    {
        var pk = new ArrayPacket(_data);
        return pk.Clone();
    }

    [Benchmark(Description = "Clone_链式包")]
    public IPacket Clone_Chained()
    {
        var pk = new ArrayPacket(_data);
        pk.Next = new ArrayPacket(_appendData);
        return pk.Clone();
    }
    #endregion

    #region 内存访问
    [Benchmark(Description = "TryGetSpan_单包")]
    public Boolean TryGetSpan_Single()
    {
        var pk = new ArrayPacket(_data);
        return ((IPacket)pk).TryGetSpan(out _);
    }

    [Benchmark(Description = "TryGetSpan_链式包")]
    public Boolean TryGetSpan_Chained()
    {
        var pk = new ArrayPacket(_data);
        pk.Next = new ArrayPacket(_appendData);
        return ((IPacket)pk).TryGetSpan(out _);
    }
    #endregion

    #region 头部扩展
    [Benchmark(Description = "ExpandHeader_ArrayPacket有空间")]
    public IPacket ExpandHeader_ArrayPacketHasSpace()
    {
        var pk = new ArrayPacket(_data, 16, DataSize - 16);
        return pk.ExpandHeader(8);
    }

    [Benchmark(Description = "ExpandHeader_创建新包")]
    public IPacket ExpandHeader_NewPacket()
    {
        var pk = new ArrayPacket(_data);
        var result = pk.ExpandHeader(8);
        (result as IDisposable)?.Dispose();
        return result;
    }

    [Benchmark(Description = "ExpandHeader_OwnerPacket有空间")]
    public IPacket ExpandHeader_OwnerPacketHasSpace()
    {
        using var pk = new OwnerPacket(_data, 16, DataSize - 16, false);
        return pk.ExpandHeader(8);
    }
    #endregion
}
