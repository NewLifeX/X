using System.Text;
using NewLife;
using NewLife.Data;
using Xunit;

namespace XUnitTest.Data;

public class PacketTests
{
    [Fact]
    public void CtorTest()
    {
        var buf = "Stone".GetBytes();

        var pk = new Packet(buf, 1, buf.Length - 2);
        Assert.Equal(buf, pk.Data);
        Assert.Equal(1, pk.Offset);
        Assert.Equal(buf.Length - 2, pk.Count);
        Assert.Equal(buf.Length - 2, pk.Total);
        Assert.Null(pk.Next);

        Assert.Equal(buf.ReadBytes(1, 3), pk.ToArray());

        Assert.Equal((Byte)'t', pk[0]);
        pk[1] = (Byte)'X';
        Assert.Equal((Byte)'X', buf[2]);
    }

    [Fact]
    public void CtorTest2()
    {
        var buf = "Stone".GetBytes();
        var arr = new ArraySegment<Byte>(buf, 1, buf.Length - 2);

        var pk = new Packet(arr);
        Assert.Equal(buf, pk.Data);
        Assert.Equal(1, pk.Offset);
        Assert.Equal(buf.Length - 2, pk.Count);
        Assert.Equal(buf.Length - 2, pk.Total);
        Assert.Null(pk.Next);

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

        var pk = new Packet(ms);
        Assert.Equal(1, ms.Position);
        Assert.Equal(buf.ReadBytes(0, 4), pk.Data.ReadBytes(0, 4));
        Assert.Equal(1, pk.Offset);
        Assert.Equal(buf.Length - 2, pk.Count);
        Assert.Equal(buf.Length - 2, pk.Total);
        Assert.Null(pk.Next);
    }

    [Fact]
    public void StreamCtor2()
    {
        var buf = "Stone".GetBytes();
        var ms = new MemoryStream(buf, 0, buf.Length - 1);
        ms.Position = 1;

        var pk = new Packet(ms);
        Assert.Equal(1, ms.Position);
        Assert.Equal(buf.ReadBytes(1, 3), pk.Data.ReadBytes(0, 3));
        Assert.Equal(0, pk.Offset);
        Assert.Equal(buf.Length - 2, pk.Count);
        Assert.Equal(buf.Length - 2, pk.Total);
        Assert.Null(pk.Next);
    }

    [Fact]
    public void CtorTest4()
    {
        var buf = "Stone".GetBytes();
        var ms = new MemoryStream(buf);
        ms.Position = 1;

        var pk = new Packet(ms);
        Assert.Equal(buf.ReadBytes(1, 4), pk.Data);
        Assert.Equal(0, pk.Offset);
        Assert.Equal(buf.Length - 1, pk.Count);
        Assert.Equal(buf.Length - 1, pk.Total);
        Assert.Null(pk.Next);
    }

    [Fact]
    public void SetTest()
    {
        var buf = "Stone".GetBytes();

        var pk = new Packet(buf);
        pk.Set(buf, 1, buf.Length - 2);

        Assert.Equal(buf, pk.Data);
        Assert.Equal(1, pk.Offset);
        Assert.Equal(buf.Length - 2, pk.Count);
        Assert.Equal(buf.Length - 2, pk.Total);
        Assert.Null(pk.Next);

        pk.Set(null);

        Assert.Null(pk.Data);
        Assert.Equal(0, pk.Offset);
        Assert.Equal(0, pk.Count);
    }

    [Fact]
    public void SliceTest()
    {
        var buf = "Stone".GetBytes();

        var pk = new Packet(buf);
        pk = pk.Slice(1, 3);

        Assert.Equal("ton", pk.ToStr());
    }

    [Fact]
    public void IndexOfTest()
    {
        var buf = "Stone".GetBytes();

        var pk = new Packet(buf);
        var p = pk.IndexOf("on".GetBytes(), 1);

        Assert.Equal(2, p);
    }

    [Fact]
    public void IndexOfBigTest()
    {
        var buf = "Stone ------WebKitFormBoundary3ZXeqQWNjAzojVR7".GetBytes();

        var pk = new Packet(new Byte[1024]);
        for (var i = 0; i < 5 * 1024; i++)
        {
            pk.Append(new Byte[1024]);
        }
        pk.Append(buf);
        var p = pk.IndexOf("------WebKitFormBoundary3ZXeqQWNjAzojVR7".GetBytes(), 1);

        Assert.Equal(pk.Total - buf.Length + 6, p);
    }

    [Fact]
    public void AppendTest()
    {
        var buf = "Stone".GetBytes();

        var pk = new Packet(buf);
        pk.Append("NewLife".GetBytes());

        Assert.NotNull(pk.Next);
        Assert.Equal("StoneNewLife", pk.ToStr());
    }

    [Fact]
    public void AfterAppendSetTest()
    {
        var buf = "Stone".GetBytes();

        var pk = new Packet(buf);
        pk.Append("11111".GetBytes());
        pk.Append("22222".GetBytes());
        pk[12] = 0x11;
        Assert.NotNull(pk.Next);
        Assert.Equal(pk[12],(byte)0x11);
    }

    [Fact]
    public void NextTest()
    {
        var buf = "Stone".GetBytes();

        var pk = new Packet(buf);
        pk.Append("NewLife".GetBytes());

        Assert.NotNull(pk.Next);
        Assert.Equal("StoneNewLife", pk.ToStr());

        var pk2 = pk.Slice(2, 6);
        Assert.Equal("oneNew", pk2.ToStr());

        var p = pk.IndexOf("eNe".GetBytes());
        Assert.Equal(4, p);

        Assert.Equal("StoneNewLife", pk.ToArray().ToStr());

        Assert.Equal("eNe", pk.ReadBytes(4, 3).ToStr());

        var arr = pk.ToSegment();
        Assert.Equal("StoneNewLife", arr.Array.ToStr());
        Assert.Equal(0, arr.Offset);
        Assert.Equal(5 + 7, arr.Count);

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
        pk.CopyToAsync(ms).Wait();
        Assert.Equal(5 + 7, ms.Position);
        Assert.Equal(5 + 7, ms.Length);
        ms.Position = 0;
        Assert.Equal("StoneNewLife", ms.ToStr());

        var buf2 = new Byte[7];
        pk.WriteTo(buf2, 1, 5);
        Assert.Equal(0, buf2[0]);
        Assert.Equal(0, buf2[6]);
        Assert.Equal("Stone", buf2.ToStr(null, 1, 5));

        var pk3 = pk.Clone();
        Assert.NotEqual(pk.Data, pk3.Data);
        Assert.Equal(pk.Total, pk3.Total);
        Assert.NotEqual(pk.Count, pk3.Count);
        Assert.Null(pk3.Next);
    }
}
