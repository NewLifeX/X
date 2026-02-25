using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NewLife;
using NewLife.Data;
using Xunit;

namespace XUnitTest.Buffer;

public class ArrayPacketTests
{
    [Fact]
    public void CtorTest()
    {
        var buf = "Stone".GetBytes();

        var pk = new ArrayPacket(buf, 1, buf.Length - 2);
        Assert.Equal(buf, pk.Buffer);
        Assert.Equal(1, pk.Offset);
        Assert.Equal(buf.Length - 2, pk.Length);
        Assert.Equal(buf.Length - 2, pk.Total);
        Assert.Null(pk.Next);

        Assert.Equal(buf.ReadBytes(1, 3), pk.ToArray());

        Assert.Equal((Byte)'t', pk[0]);
        pk[1] = (Byte)'X';
        Assert.Equal((Byte)'X', buf[2]);
    }

    [Fact]
    public void StreamCtor()
    {
        var buf = "Stone".GetBytes();
        var ms = new MemoryStream();
        ms.Write(buf, 0, buf.Length - 1);
        ms.Position = 1;

        var pk = new ArrayPacket(ms);
        Assert.Equal(1, ms.Position);
        Assert.Equal(buf.ReadBytes(0, 4), pk.Buffer.ReadBytes(0, 4));
        Assert.Equal(1, pk.Offset);
        Assert.Equal(buf.Length - 2, pk.Length);
        Assert.Equal(buf.Length - 2, pk.Total);
        Assert.Null(pk.Next);
    }

    [Fact]
    public void StreamCtor2()
    {
        var buf = "Stone".GetBytes();
        var ms = new MemoryStream(buf, 0, buf.Length - 1);
        ms.Position = 1;

        var pk = new ArrayPacket(ms);
        Assert.Equal(1, ms.Position);
        Assert.Equal(buf.ReadBytes(1, 3), pk.Buffer.ReadBytes(0, 3));
        Assert.Equal(0, pk.Offset);
        Assert.Equal(buf.Length - 2, pk.Length);
        Assert.Equal(buf.Length - 2, pk.Total);
        Assert.Null(pk.Next);
    }

    [Fact]
    public void CtorTest4()
    {
        var buf = "Stone".GetBytes();
        var ms = new MemoryStream(buf);
        ms.Position = 1;

        var pk = new ArrayPacket(ms);
        Assert.Equal(buf.ReadBytes(1, 4), pk.Buffer);
        Assert.Equal(0, pk.Offset);
        Assert.Equal(buf.Length - 1, pk.Length);
        Assert.Equal(buf.Length - 1, pk.Total);
        Assert.Null(pk.Next);
    }

    [Fact]
    public void SliceTest()
    {
        var buf = "Stone".GetBytes();

        var pk = new ArrayPacket(buf);
        var pk2 = pk.Slice(1, 3);

        Assert.Equal("ton", pk2.ToStr());
    }

    [Fact]
    public void IndexOfTest()
    {
        var buf = "Stone".GetBytes();

        var pk = new ArrayPacket(buf);
        var p = pk.GetSpan().IndexOf("on".GetBytes());

        Assert.Equal(2, p);
    }

    [Fact]
    public void IndexOfBigTest()
    {
        var buf = "Stone ------WebKitFormBoundary3ZXeqQWNjAzojVR7".GetBytes();

        IPacket pk = new ArrayPacket(new Byte[1024]);
        for (var i = 0; i < 5 * 1024; i++)
        {
            pk = pk.Append(new Byte[1024]);
        }
        pk = pk.Append(buf);
        var p = pk.ReadBytes().AsSpan().IndexOf("------WebKitFormBoundary3ZXeqQWNjAzojVR7".GetBytes());

        Assert.Equal(pk.Total - buf.Length + 6, p);
    }

    [Fact]
    public void AppendTest()
    {
        var buf = "Stone".GetBytes();

        IPacket pk = new ArrayPacket(buf);
        pk = pk.Append("NewLife".GetBytes());

        Assert.NotNull(pk.Next);
        Assert.Equal("StoneNewLife", pk.ToStr());
    }

    [Fact]
    public void AfterAppendSetTest()
    {
        var buf = "Stone".GetBytes();

        IPacket pk = new ArrayPacket(buf);
        pk = pk.Append("11111".GetBytes());
        pk = pk.Append("22222".GetBytes());
        pk[12] = 0x11;
        Assert.NotNull(pk.Next);
        Assert.Equal((Byte)0x11, pk[12]);
    }

    [Fact]
    public async Task NextTest()
    {
        var buf = "Stone".GetBytes();

        IPacket pk = new ArrayPacket(buf);
        pk = pk.Append("NewLife".GetBytes());

        Assert.NotNull(pk.Next);
        Assert.Equal("StoneNewLife", pk.ToStr());

        var pk2 = pk.Slice(2, 6);
        Assert.Equal("oneNew", pk2.ToStr());

        var p = pk.ReadBytes().AsSpan().IndexOf("eNe".GetBytes());
        Assert.Equal(4, p);

        Assert.Equal("StoneNewLife", pk.ToArray().ToStr());

        Assert.Equal("eNe", pk.ReadBytes(4, 3).ToStr());

        //var arr = pk.ToSegment();
        //Assert.Equal("StoneNewLife", arr.Array.ToStr());
        //Assert.Equal(0, arr.Offset);
        //Assert.Equal(5 + 7, arr.Count);

        var arrs = pk.ToSegments();
        Assert.Equal(2, arrs.Count);
        Assert.Equal("Stone", arrs[0].Array.ToStr());
        Assert.Equal("NewLife", arrs[1].Array.ToStr());

        var ms = pk.GetStream();
        Assert.Equal(0, ms.Position);
        Assert.Equal(5 + 7, ms.Length);
        Assert.Equal("StoneNewLife", ms.ToStr());

        ms = new MemoryStream();
        pk.CopyTo(ms);
        Assert.Equal(5 + 7, ms.Position);
        Assert.Equal(5 + 7, ms.Length);
        ms.Position = 0;
        Assert.Equal("StoneNewLife", ms.ToStr());

        ms = new MemoryStream();
        await pk.CopyToAsync(ms);
        Assert.Equal(5 + 7, ms.Position);
        Assert.Equal(5 + 7, ms.Length);
        ms.Position = 0;
        Assert.Equal("StoneNewLife", ms.ToStr());

        var buf2 = new Byte[7];
        pk.GetSpan().CopyTo(new Span<Byte>(buf2, 1, 5));
        Assert.Equal(0, buf2[0]);
        Assert.Equal(0, buf2[6]);
        Assert.Equal("Stone", buf2.ToStr(null, 1, 5));

        //var pk3 = pk.Clone();
        //Assert.NotEqual(pk.Buffer, pk3.Buffer);
        //Assert.Equal(pk.Total, pk3.Total);
        //Assert.NotEqual(pk.Length, pk3.Count);
        //Assert.Null(pk3.Next);
    }

    [Fact(DisplayName = "IPacket.Slice接口调用不应装箱")]
    public void IPacketSlice_ShouldNotBox()
    {
        var buf = "HelloWorld".GetBytes();
        IPacket pk = new ArrayPacket(buf);

        // 通过接口调用 Slice(int,int)，验证修复装箱后行为正确
        var pk2 = pk.Slice(5, 5);
        Assert.Equal("World", pk2.ToStr());

        // Slice(int,int) 默认 transferOwner=true，对 ArrayPacket 无影响
        var pk3 = pk.Slice(0, 5);
        Assert.Equal("Hello", pk3.ToStr());
    }

    [Fact(DisplayName = "IPacket.Slice链式跨段切片")]
    public void IPacketSlice_ChainedPackets_ShouldCrossSegments()
    {
        IPacket pk = new ArrayPacket("Hello".GetBytes());
        pk = pk.Append("World".GetBytes());
        Assert.Equal(10, pk.Total);

        // 跨段切片：从 offset=3 取 4 字节 → "loWo"
        var pk2 = pk.Slice(3, 4);
        Assert.Equal("loWo", pk2.ToStr());

        // 完全在第二段：从 offset=5 取 5 字节 → "World"
        var pk3 = pk.Slice(5, 5);
        Assert.Equal("World", pk3.ToStr());

        // 切到末尾
        var pk4 = pk.Slice(7);
        Assert.Equal("rld", pk4.ToStr());
    }

    [Fact(DisplayName = "Slice空包")]
    public void Slice_ZeroCount_ShouldReturnEmpty()
    {
        IPacket pk = new ArrayPacket("Hello".GetBytes());
        var pk2 = pk.Slice(0, 0);
        Assert.Equal(0, pk2.Length);
    }

    [Fact(DisplayName = "Slice负count表示到末尾")]
    public void Slice_NegativeCount_ShouldSliceToEnd()
    {
        var pk = new ArrayPacket("HelloWorld".GetBytes());
        var pk2 = pk.Slice(5);
        Assert.Equal("World", pk2.ToStr());
        Assert.Equal(5, pk2.Length);
    }

    [Fact(DisplayName = "共享缓冲区验证")]
    public void Slice_ShouldShareBuffer()
    {
        var buf = "HelloWorld".GetBytes();
        var pk = new ArrayPacket(buf);
        var pk2 = pk.Slice(2, 5);

        Assert.Same(buf, pk2.Buffer);
        Assert.Equal(2, pk2.Offset);
        Assert.Equal(5, pk2.Length);
    }

    [Fact(DisplayName = "TryGetArray应返回正确数组段")]
    public void TryGetArray_ShouldReturnCorrectSegment()
    {
        var buf = "HelloWorld".GetBytes();
        var pk = new ArrayPacket(buf, 3, 5);

        var rs = ((IPacket)pk).TryGetArray(out var segment);
        Assert.True(rs);
        Assert.Same(buf, segment.Array);
        Assert.Equal(3, segment.Offset);
        Assert.Equal(5, segment.Count);
    }

    [Fact(DisplayName = "隐式转换")]
    public void ImplicitConversions_ShouldWork()
    {
        ArrayPacket pk1 = "Hello".GetBytes();
        Assert.Equal(5, pk1.Length);

        ArrayPacket pk2 = "Hello";
        Assert.Equal("Hello", pk2.ToStr());

        ArrayPacket pk3 = new ArraySegment<Byte>("World".GetBytes(), 1, 3);
        Assert.Equal("orl", pk3.ToStr());
    }
}
