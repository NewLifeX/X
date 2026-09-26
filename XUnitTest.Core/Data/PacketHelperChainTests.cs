using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using NewLife;
using NewLife.Data;
using Xunit;

namespace XUnitTest.Data;

/// <summary>数据包辅助方法的链式与零拷贝契约测试：单段快路径、跨段聚合、所有权转移与归还</summary>
public class PacketHelperChainTests
{
    private static Byte[] B(params Int32[] values)
    {
        var buf = new Byte[values.Length];
        for (var i = 0; i < values.Length; i++) buf[i] = (Byte)values[i];

        return buf;
    }

    private static IPacket Chain(String first, String second) => new ArrayPacket(first.GetBytes()).Append(second.GetBytes());

    #region TryGetSpan
    [Fact]
    [DisplayName("获取跨度_单段成功_链式失败")]
    public void TryGetSpan_SingleVsChain()
    {
        IPacket single = new ArrayPacket(B(1, 2, 3));
        Assert.True(single.TryGetSpan(out var span));
        Assert.Equal(B(1, 2, 3), span.ToArray());

        // 链式无法用单块跨度表示（会遗漏后续节点），必须返回 false 让调用方走拼读路径
        IPacket chain = Chain("\u0001", "\u0002");
        Assert.False(chain.TryGetSpan(out var none));
        Assert.True(none.IsEmpty);
    }
    #endregion

    #region GetPrefix
    [Fact]
    [DisplayName("前缀拼读_首段足够_直接返回首段跨度")]
    public void GetPrefix_FirstSegmentEnough()
    {
        IPacket pk = new ArrayPacket(B(1, 2, 3, 4, 5));
        Span<Byte> buffer = stackalloc Byte[8];

        var prefix = pk.GetPrefix(buffer, 4);

        // 首段已足够：零拷贝直引首段跨度，不裁剪也不拷贝（可能长于 count，调用方按需取前 count 字节）
        Assert.True(prefix.Length >= 4);
        Assert.Equal(B(1, 2, 3, 4), prefix[..4].ToArray());
    }

    [Fact]
    [DisplayName("前缀拼读_跨段_拼入缓冲")]
    public void GetPrefix_CrossSegment()
    {
        IPacket pk = Chain("\u0001\u0002", "\u0003\u0004\u0005");
        Span<Byte> buffer = stackalloc Byte[4];

        var prefix = pk.GetPrefix(buffer, 4);

        Assert.Equal(B(1, 2, 3, 4), prefix.ToArray());
    }

    [Fact]
    [DisplayName("前缀拼读_数据不足_返回短跨度由解析方判定")]
    public void GetPrefix_NotEnoughData()
    {
        IPacket pk = new ArrayPacket(B(1, 2));
        Span<Byte> buffer = stackalloc Byte[4];

        var prefix = pk.GetPrefix(buffer, 4);

        Assert.Equal(B(1, 2), prefix.ToArray());
    }
    #endregion

    #region ToSegment
    [Fact]
    [DisplayName("转数组段_单包零拷贝_直接引用底层数组")]
    public void ToSegment_Single_ZeroCopy()
    {
        var buf = "Stone".GetBytes();
        IPacket pk = new ArrayPacket(buf);

        var seg = pk.ToSegment();

        Assert.Same(buf, seg.Array);
        Assert.Equal(0, seg.Offset);
        Assert.Equal(5, seg.Count);
    }

    [Fact]
    [DisplayName("转数组段_链式_聚合为单段副本")]
    public void ToSegment_Chain_Aggregates()
    {
        IPacket pk = Chain("Stone", "NewLife");

        var seg = pk.ToSegment();

        Assert.Equal(12, seg.Count);
        Assert.Equal("StoneNewLife", seg.Array!.ToStr(null, seg.Offset, seg.Count));
    }

    [Fact]
    [DisplayName("转数组段_窗口偏移_保持窗口与长度")]
    public void ToSegment_Window()
    {
        var buf = "StoneNewLife".GetBytes();
        IPacket pk = new ArrayPacket(buf, 5, 7);

        var seg = pk.ToSegment();

        Assert.Same(buf, seg.Array);
        Assert.Equal(5, seg.Offset);
        Assert.Equal(7, seg.Count);
    }
    #endregion

    #region ReadBytes
    [Fact]
    [DisplayName("读字节_单段整段_零拷贝返回底层数组")]
    public void ReadBytes_FullSingle_ReturnsUnderlyingArray()
    {
        var buf = "Stone".GetBytes();
        IPacket pk = new ArrayPacket(buf);

        // 读取全部且数组段完整时直接返回底层数组（调用方需知晓这是别名而非副本）
        Assert.Same(buf, pk.ReadBytes());

        // 带窗口的包不满足“数组段完整”条件，必须复制
        var window = new ArrayPacket(buf, 1, 3);
        Assert.NotSame(buf, window.ReadBytes());
        Assert.Equal("ton", window.ReadBytes().ToStr());
    }

    [Fact]
    [DisplayName("读字节_链式跨段_按全局偏移取值")]
    public void ReadBytes_Chain_CrossSegment()
    {
        IPacket pk = Chain("Stone", "NewLife");

        Assert.Equal("StoneNewLife", pk.ReadBytes().ToStr());
        Assert.Equal("eNe", pk.ReadBytes(4, 3).ToStr());
        Assert.Equal("NewLife", pk.ReadBytes(5).ToStr());

        // 超出范围夹紧到可用长度，不做越窗读取
        Assert.Equal("NewLife", pk.ReadBytes(5, 999).ToStr());
        Assert.Empty(pk.ReadBytes(12));
    }
    #endregion

    #region Clone
    [Fact]
    [DisplayName("克隆_单包_独立副本且改副本不影响源")]
    public void Clone_Single_Independent()
    {
        var buf = "Stone".GetBytes();
        IPacket src = new ArrayPacket(buf);

        using var clone = (IOwnerPacket)src.Clone();

        Assert.Equal(src.Total, clone.Total);
        Assert.Equal("Stone", clone.ToStr());

        // 深拷贝：底层缓冲不共享
        clone.GetSpan()[0] = (Byte)'X';
        Assert.Equal("Stone", src.ToStr());
        Assert.Equal("Xtone", clone.ToStr());
    }

    [Fact]
    [DisplayName("克隆_链式_聚合为单段拥有句柄")]
    public void Clone_Chain_AggregatesToSingleSegment()
    {
        IPacket src = Chain("Stone", "NewLife");

        using var clone = (IOwnerPacket)src.Clone();

        Assert.Equal(src.Total, clone.Total);
        Assert.Equal("StoneNewLife", clone.ToStr());

        // 克隆结果自带独立缓冲，不沿用源链
        Assert.Null(clone.Next);
        Assert.NotSame(((ArrayPacket)src).Buffer, ((OwnerPacket)clone).Buffer);
    }

    [Fact]
    [DisplayName("克隆_产出的拥有句柄_释放后归还引用")]
    public void Clone_Dispose_ReleasesReference()
    {
        IPacket src = new ArrayPacket("Stone".GetBytes());

        var clone = (OwnerPacket)src.Clone();
        Assert.Equal(1, clone.RefCount);

        clone.Dispose();
        Assert.Equal(0, clone.RefCount);
    }
    #endregion

    #region WriteTo
    [Fact]
    [DisplayName("写文本_单包_按编码解码写入")]
    public void WriteTo_SinglePacket()
    {
        IPacket pk = new ArrayPacket("StoneNewLife".GetBytes());
        var writer = new StringWriter();

        pk.WriteTo(writer);

        Assert.Equal("StoneNewLife", writer.ToString());
    }

    [Fact]
    [DisplayName("写文本_链式_逐段顺序拼接")]
    public void WriteTo_Chain()
    {
        IPacket pk = Chain("Stone", "NewLife");
        var writer = new StringWriter();

        pk.WriteTo(writer);

        Assert.Equal("StoneNewLife", writer.ToString());
    }

    [Fact]
    [DisplayName("写文本_指定编码与空包_安全处理")]
    public void WriteTo_EncodingAndEmpty()
    {
        IPacket pk = new ArrayPacket(Encoding.Unicode.GetBytes("新生命"));
        var writer = new StringWriter();
        pk.WriteTo(writer, Encoding.Unicode);
        Assert.Equal("新生命", writer.ToString());

        // 空包与空写入器调用安全，不抛异常
        var empty = new StringWriter();
        new ArrayPacket(Array.Empty<Byte>()).WriteTo(empty);
        Assert.Equal(String.Empty, empty.ToString());

        PacketHelper.WriteTo(null!, empty);
        Assert.Equal(String.Empty, empty.ToString());
    }
    #endregion
}
