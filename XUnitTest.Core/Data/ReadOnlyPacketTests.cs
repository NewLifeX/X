using System;
using System.ComponentModel;
using System.Text;
using NewLife;
using NewLife.Data;
using Xunit;

namespace XUnitTest.Data;

/// <summary>只读数据包测试：构造窗口、索引器只读约束、切片、总是复制的 ToArray、链式不支持的契约</summary>
public class ReadOnlyPacketTests
{
    private static Byte[] B(params Int32[] values)
    {
        var buf = new Byte[values.Length];
        for (var i = 0; i < values.Length; i++) buf[i] = (Byte)values[i];

        return buf;
    }

    #region 构造
    [Fact]
    [DisplayName("只读包_缺省参数_窗口取到数组末尾")]
    public void Ctor_Defaults()
    {
        var buf = B(1, 2, 3, 4);

        var pk = new ReadOnlyPacket(buf);
        Assert.Same(buf, pk.Buffer);
        Assert.Equal(0, pk.Offset);
        Assert.Equal(4, pk.Length);
        Assert.Equal(4, pk.Total);
    }

    [Fact]
    [DisplayName("只读包_指定偏移与长度_只暴露窗口")]
    public void Ctor_Window()
    {
        var buf = B(1, 2, 3, 4, 5);

        var pk = new ReadOnlyPacket(buf, 1, 3);
        Assert.Equal(1, pk.Offset);
        Assert.Equal(3, pk.Length);
        Assert.Equal(B(2, 3, 4), pk.GetSpan().ToArray());
    }

    [Fact]
    [DisplayName("只读包_负长度_表示到数组末尾")]
    public void Ctor_NegativeCount_ToEnd()
    {
        var pk = new ReadOnlyPacket(B(1, 2, 3, 4), 2);

        Assert.Equal(2, pk.Length);
        Assert.Equal(B(3, 4), pk.GetSpan().ToArray());
    }

    [Fact]
    [DisplayName("只读包_偏移越界_抛参数异常而不是产生负长度")]
    public void Ctor_OffsetBeyondBuffer_Throws()
    {
        var buf = B(1, 2, 3);

        // 越界偏移若放过，“到末尾”的长度推导会得到负值，形成 Length 为负的坏包
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlyPacket(buf, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlyPacket(buf, -1));
    }

    [Fact]
    [DisplayName("只读包_长度超出数组_抛参数异常")]
    public void Ctor_CountBeyondBuffer_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlyPacket(B(1, 2, 3), 1, 5));

        // Int32.MaxValue 这类极端值不能靠 offset + count 相加溢出绕过校验
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlyPacket(B(1, 2, 3), 1, Int32.MaxValue));
    }

    [Fact]
    [DisplayName("只读包_数组段构造")]
    public void Ctor_Segment()
    {
        var buf = B(1, 2, 3, 4);

        var pk = new ReadOnlyPacket(new ArraySegment<Byte>(buf, 1, 2));
        Assert.Same(buf, pk.Buffer);
        Assert.Equal(B(2, 3), pk.GetSpan().ToArray());
    }

    [Fact]
    [DisplayName("只读包_从数据包构造_复制数据且与源独立")]
    public void Ctor_FromPacket_Copies()
    {
        using var source = new OwnerPacket(4);
        B(7, 8, 9, 10).CopyTo(source.GetSpan());

        var pk = new ReadOnlyPacket((IPacket)source);

        Assert.NotSame(source.Buffer, pk.Buffer);
        Assert.Equal(B(7, 8, 9, 10), pk.GetSpan().ToArray());

        // 副本独占缓冲：改副本不影响源
        pk.Buffer[0] = 99;
        Assert.Equal(7, source.GetSpan()[0]);
    }
    #endregion

    #region 索引与只读约束
    [Fact]
    [DisplayName("只读包_索引器_按窗口读取")]
    public void Indexer_Read()
    {
        var pk = new ReadOnlyPacket(B(1, 2, 3, 4), 1, 2);

        Assert.Equal(2, pk[0]);
        Assert.Equal(3, pk[1]);
    }

    [Fact]
    [DisplayName("只读包_索引器_越界抛范围异常")]
    public void Indexer_OutOfRange_Throws()
    {
        var pk = new ReadOnlyPacket(B(1, 2, 3));

        Assert.Throws<IndexOutOfRangeException>(() => pk[-1]);
        Assert.Throws<IndexOutOfRangeException>(() => pk[3]);
    }

    [Fact]
    [DisplayName("只读包_索引器_赋值被拒绝")]
    public void Indexer_Set_Throws()
    {
        var pk = new ReadOnlyPacket(B(1, 2, 3));

        Assert.Throws<NotSupportedException>(() => pk[0] = 9);
    }
    #endregion

    #region 视图
    [Fact]
    [DisplayName("只读包_GetSpan与GetMemory_仅覆盖窗口")]
    public void Views_OnlyWindow()
    {
        var pk = new ReadOnlyPacket(B(1, 2, 3, 4), 1, 2);

        Assert.Equal(B(2, 3), pk.GetSpan().ToArray());
        Assert.Equal(B(2, 3), pk.GetMemory().ToArray());
    }

    [Fact]
    [DisplayName("只读包_TryGetArray_返回窗口段")]
    public void TryGetArray_Window()
    {
        var buf = B(1, 2, 3, 4);
        var pk = new ReadOnlyPacket(buf, 1, 2);

        Assert.True(pk.TryGetArray(out var seg));
        Assert.Same(buf, seg.Array);
        Assert.Equal(1, seg.Offset);
        Assert.Equal(2, seg.Count);
    }
    #endregion

    #region 切片
    [Fact]
    [DisplayName("只读包_切片_按窗口偏移与长度")]
    public void Slice_Window()
    {
        var pk = new ReadOnlyPacket(B(1, 2, 3, 4, 5), 1, 4);

        var sub = pk.Slice(1, 2);
        Assert.Equal(2, sub.Offset);
        Assert.Equal(2, sub.Length);
        Assert.Equal(B(3, 4), sub.GetSpan().ToArray());
    }

    [Fact]
    [DisplayName("只读包_切片_负长度取到末尾_超长夹紧")]
    public void Slice_CountClamp()
    {
        var pk = new ReadOnlyPacket(B(1, 2, 3, 4, 5), 1, 4);

        Assert.Equal(B(2, 3, 4, 5), pk.Slice(0).GetSpan().ToArray());
        Assert.Equal(B(2, 3, 4, 5), pk.Slice(0, 999).GetSpan().ToArray());
        Assert.Equal(0, pk.Slice(4).Length);
    }

    [Fact]
    [DisplayName("只读包_切片_负偏移抛参数异常")]
    public void Slice_NegativeOffset_Throws()
    {
        var pk = new ReadOnlyPacket(B(1, 2, 3));

        Assert.Throws<ArgumentOutOfRangeException>(() => pk.Slice(-1));
    }
    #endregion

    #region 转换与契约
    [Fact]
    [DisplayName("只读包_ToArray_总是复制_不暴露内部缓冲")]
    public void ToArray_AlwaysCopies()
    {
        var buf = B(1, 2, 3);
        var pk = new ReadOnlyPacket(buf);

        var arr = pk.ToArray();

        Assert.NotSame(buf, arr);
        Assert.Equal(B(1, 2, 3), arr);

        // 复制品可改，内部缓冲不受影响（整段时直接返回内部数组会让调用方绕过只读约束）
        arr[0] = 99;
        Assert.Equal(1, buf[0]);
    }

    [Fact]
    [DisplayName("只读包_隐式转换_字节数组与数组段")]
    public void ImplicitConvert()
    {
        ReadOnlyPacket pk1 = B(1, 2, 3);
        Assert.Equal(3, pk1.Length);

        ReadOnlyPacket pk2 = new ArraySegment<Byte>(B(4, 5, 6, 7), 1, 2);
        Assert.Equal(B(5, 6), pk2.GetSpan().ToArray());
    }

    [Fact]
    [DisplayName("只读包_空包与头部契约")]
    public void EmptyAndHeaderContract()
    {
        Assert.Equal(0, ReadOnlyPacket.Empty.Length);
        Assert.True(ReadOnlyPacket.Empty.GetSpan().IsEmpty);

        var pk = new ReadOnlyPacket(B(1, 2, 3), 1, 2);
        // 不预留头部、不支持链式：Total 等于窗口长度，FreeHeader 恒 0，Next 恒空
        Assert.Equal(2, pk.Total);
        Assert.Equal(0, pk.FreeHeader);
        Assert.Null(((IPacket)pk).Next);

        IPacket p = pk;
        p.Next = new ArrayPacket(B(9));
        Assert.Null(p.Next);
    }

    [Fact]
    [DisplayName("只读包_IPacket接口切片_返回只读包")]
    public void InterfaceSlice()
    {
        IPacket pk = new ReadOnlyPacket(B(1, 2, 3, 4), 1, 3);

        var sub = pk.Slice(1, 2);
        Assert.IsType<ReadOnlyPacket>(sub);
        Assert.Equal(B(3, 4), sub.GetSpan().ToArray());
    }

    [Fact]
    [DisplayName("只读包_ToString_含缓冲区与窗口")]
    public void ToStringTest()
    {
        var pk = new ReadOnlyPacket(B(1, 2, 3), 1, 2);

        var text = pk.ToString();
        Assert.Contains("ReadOnlyPacket", text, StringComparison.Ordinal);
        Assert.Contains("(1, 2)", text, StringComparison.Ordinal);
    }
    #endregion
}
