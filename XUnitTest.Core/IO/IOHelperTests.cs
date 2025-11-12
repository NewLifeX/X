using System.Text;
using NewLife;
using NewLife.Data;
using Xunit;

namespace XUnitTest.IO;

public class IOHelperTests
{
    [Fact]
    public void IndexOf()
    {
        var d = "------WebKitFormBoundary3ZXeqQWNjAzojVR7".GetBytes();

        var buf = new Byte[8 * 1024 * 1024];
        buf.Write(7 * 1024 * 1024, d);

        var p = buf.IndexOf(d);
        Assert.Equal(7 * 1024 * 1024, p);

        p = buf.IndexOf(d, 7 * 1024 * 1024 - 1);
        Assert.Equal(7 * 1024 * 1024, p);

        p = buf.IndexOf(d, 7 * 1024 * 1024 + 1);
        Assert.Equal(-1, p);
    }

    private static readonly Byte[] NewLine2 = "\r\n\r\n"u8.ToArray();
    [Fact]
    public void IndexOf2()
    {
        var str = "Content-Disposition: form-data; name=\"name\"\r\n\r\n大石头";

        var buf = str.GetBytes();

        var p = buf.IndexOf("\r\n\r\n".GetBytes());
        Assert.Equal(43, p);

        p = buf.IndexOf(NewLine2);
        Assert.Equal(43, p);

        var pk = new Packet(buf);

        var value = pk.Slice(p + 4).ToStr();
        Assert.Equal("大石头", value);
    }

    [Fact]
    public void ToHex()
    {
        var buf = "NewLife".GetBytes();
        var hex = buf.ToHex();

        Assert.Equal("4E65774C696665", hex);

        hex = buf.ToHex("-");
        Assert.Equal("4E-65-77-4C-69-66-65", hex);

        hex = buf.ToHex("-", 4, 6);
        Assert.Equal("4E65774C-6966", hex);

        Byte b = 0x05;
        var str = b.ToHex();
        Assert.Equal("05", str);

        b = 0xab;
        str = b.ToHex();
        Assert.Equal("AB", str);
    }

    [Fact]
    public void Swap()
    {
        var data = "12345678";

        var buf = data.ToHex().Swap(false, false);
        Assert.Equal("12345678", buf.ToHex());

        buf = data.ToHex().Swap(false, true);
        Assert.Equal("56781234", buf.ToHex());

        buf = data.ToHex().Swap(true, false);
        Assert.Equal("34127856", buf.ToHex());

        buf = data.ToHex().Swap(true, true);
        Assert.Equal("78563412", buf.ToHex());
    }

    [Fact]
    public void Swap64()
    {
        var data = "12345678AABBCCDD";

        var buf = data.ToHex().Swap(false, false);
        Assert.Equal("12345678AABBCCDD", buf.ToHex());

        buf = data.ToHex().Swap(false, true);
        Assert.Equal("56781234CCDDAABB", buf.ToHex());

        buf = data.ToHex().Swap(true, false);
        Assert.Equal("34127856BBAADDCC", buf.ToHex());

        buf = data.ToHex().Swap(true, true);
        Assert.Equal("78563412DDCCBBAA", buf.ToHex());
    }

    [Fact]
    public void ToBase64()
    {
        var buf = "Stone".GetBytes();

        var b64 = buf.ToBase64();
        Assert.Equal("U3RvbmU=", b64);

        b64 = buf.ToUrlBase64();
        Assert.Equal("U3RvbmU", b64);

        var buf2 = b64.ToBase64();
        Assert.Equal(buf.ToHex(), buf2.ToHex());

        var buf3 = (b64 + Environment.NewLine + " ").ToBase64();
        Assert.Equal(buf.ToHex(), buf3.ToHex());
    }

    [Fact]
    public void ToUInt16()
    {
        var buf = "00-12-34".ToHex();

        var value = buf.ToUInt16(1, false);
        Assert.Equal(0x1234, value);

        value = buf.ToUInt16(1, true);
        Assert.Equal(0x3412, value);

        value = buf.ToUInt16(0, true);
        Assert.Equal(0x1200, value);

        // Write
        value = buf.ToUInt16(1, false);
        var buf2 = new Byte[buf.Length];
        buf2.Write(value, 1, false);
        Assert.Equal(buf, buf2);

        // GetBytes
        var buf3 = value.GetBytes(false);
        Assert.Equal(buf.Skip(1).ToArray(), buf3);
    }

    [Fact]
    public void ToUInt32()
    {
        var buf = "00-12-34-56-78".ToHex();

        var value = buf.ToUInt32(1, false);
        Assert.Equal(0x12345678u, value);

        value = buf.ToUInt32(1, true);
        Assert.Equal(0x78563412u, value);

        value = buf.ToUInt32(0, true);
        Assert.Equal(0x56341200u, value);

        // Write
        value = buf.ToUInt32(1, false);
        var buf2 = new Byte[buf.Length];
        buf2.Write(value, 1, false);
        Assert.Equal(buf, buf2);

        // GetBytes
        var buf3 = value.GetBytes(false);
        Assert.Equal(buf.Skip(1).ToArray(), buf3);
    }

    [Fact]
    public void ToUInt64()
    {
        var buf = "00-12-34-56-78-00-AB-CD-EF".ToHex();

        var value = buf.ToUInt64(1, false);
        Assert.Equal(0x1234567800abcdeful, value);

        value = buf.ToUInt64(1, true);
        Assert.Equal(0xefcdab0078563412ul, value);

        value = buf.ToUInt64(0, true);
        Assert.Equal(0xcdab007856341200ul, value);

        // Write
        value = buf.ToUInt64(1, false);
        var buf2 = new Byte[buf.Length];
        buf2.Write(value, 1, false);
        Assert.Equal(buf, buf2);

        // GetBytes
        var buf3 = value.GetBytes(false);
        Assert.Equal(buf.Skip(1).ToArray(), buf3);
    }

    [Fact]
    public void ToSingle()
    {
        var v = 1.2f;
        var buf = BitConverter.GetBytes(v);

        var value = buf.ToSingle();
        Assert.Equal(v, value);

        v = -1.2345f;
        buf = BitConverter.GetBytes(v);

        //buf = buf.Reverse().ToArray();
        buf=Enumerable.Reverse(buf).ToArray();
        value = buf.ToSingle(0, false);
        Assert.Equal(v, value);

        // Write
        value = buf.ToSingle(0, false);
        var buf2 = new Byte[buf.Length];
        buf2.Write(value, 0, false);
        Assert.Equal(buf, buf2);

        // GetBytes
        var buf3 = value.GetBytes(false);
        Assert.Equal(buf, buf3);
    }

    [Fact]
    public void ToDouble()
    {
        var v = 1.2d;
        var buf = BitConverter.GetBytes(v);

        var value = buf.ToDouble();
        Assert.Equal(v, value);

        v = -1.2345d;
        buf = BitConverter.GetBytes(v);

        //buf = buf.Reverse().ToArray();
        buf = Enumerable.Reverse(buf).ToArray();
        value = buf.ToDouble(0, false);
        Assert.Equal(v, value);

        // Write
        value = buf.ToDouble(0, false);
        var buf2 = new Byte[buf.Length];
        buf2.Write(value, 0, false);
        Assert.Equal(buf, buf2);

        // GetBytes
        var buf3 = value.GetBytes(false);
        Assert.Equal(buf, buf3);
    }

    #region ReadAtLeast Tests
    [Fact(DisplayName = "ReadAtLeast-正常读取达到最小值")]
    public void ReadAtLeast_Normal()
    {
        var src = Enumerable.Range(1, 10).Select(i => (Byte)i).ToArray();
        using var ms = new MemoryStream(src);
        var buf = new Byte[10];
        var n = IOHelper.ReadAtLeast(ms, buf, 0, 10, 6, true);
        Assert.True(n >= 6);
        Assert.Equal(src.AsSpan(0, n).ToArray(), buf.AsSpan(0, n).ToArray());
    }

    [Fact(DisplayName = "ReadAtLeast-EOF抛异常")]
    public void ReadAtLeast_ThrowOnEOF()
    {
        var src = new Byte[] { 1, 2, 3 };
        using var ms = new MemoryStream(src);
        var buf = new Byte[10];
        Assert.Throws<EndOfStreamException>(() => IOHelper.ReadAtLeast(ms, buf, 0, 10, 5, true));
    }

    [Fact(DisplayName = "ReadAtLeast-EOF不抛异常")]
    public void ReadAtLeast_NoThrowEOF()
    {
        var src = new Byte[] { 1, 2, 3 };
        using var ms = new MemoryStream(src);
        var buf = new Byte[10];
        var n = IOHelper.ReadAtLeast(ms, buf, 0, 10, 5, false);
        Assert.Equal(3, n);
    }

    [Fact(DisplayName = "ReadAtLeast-minimum为0快速返回")]
    public void ReadAtLeast_MinZero()
    {
        var src = new Byte[] { 1, 2, 3 };
        using var ms = new MemoryStream(src);
        var buf = new Byte[3];
        var n = IOHelper.ReadAtLeast(ms, buf, 0, 3, 0, true);
        Assert.Equal(0, n);
        Assert.Equal(0, ms.Position); // 未读取
    }

    [Fact(DisplayName = "ReadAtLeast-count为0快速返回")]
    public void ReadAtLeast_CountZero()
    {
        var src = new Byte[] { 1, 2, 3 };
        using var ms = new MemoryStream(src);
        var buf = new Byte[3];
        var n = IOHelper.ReadAtLeast(ms, buf, 0, 0, 0, true);
        Assert.Equal(0, n);
        Assert.Equal(0, ms.Position);
    }

    [Fact(DisplayName = "ReadAtLeast-参数异常验证")]
    public void ReadAtLeast_ParamErrors()
    {
        using var ms = new MemoryStream(new Byte[] { 1 });
        var buf = new Byte[5];
        Assert.Throws<ArgumentOutOfRangeException>(() => IOHelper.ReadAtLeast(ms, buf, -1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => IOHelper.ReadAtLeast(ms, buf, 0, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => IOHelper.ReadAtLeast(ms, buf, 0, 5, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => IOHelper.ReadAtLeast(ms, buf, 0, 5, 6));
        Assert.Throws<ArgumentOutOfRangeException>(() => IOHelper.ReadAtLeast(ms, buf, 4, 2, 1)); // count 越界
    }
    #endregion

    #region ReadExactly Tests
    // 模拟底层流每次只返回 1 字节，触发多轮循环
    private sealed class SlowReadStream : MemoryStream
    {
        public SlowReadStream(Byte[] data) : base(data) { }
        public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count) => base.Read(buffer, offset, Math.Min(1, count));
    }

    [Fact(DisplayName = "ReadExactly-正常完整读取")]
    public void ReadExactly_Normal()
    {
        var src = Enumerable.Range(0, 16).Select(i => (Byte)i).ToArray();
        using var ms = new MemoryStream(src);
        var buf = new Byte[16];
        var n = IOHelper.ReadExactly(ms, buf, 0, 16);
        Assert.Equal(16, n);
        Assert.Equal(src, buf);
    }

    [Fact(DisplayName = "ReadExactly-底层分段多次循环")]
    public void ReadExactly_MultiLoop()
    {
        var src = Enumerable.Range(1, 8).Select(i => (Byte)i).ToArray();
        using var ms = new SlowReadStream(src);
        var buf = new Byte[8];
        var n = IOHelper.ReadExactly(ms, buf, 0, 8);
        Assert.Equal(8, n);
        Assert.Equal(src, buf);
    }

    [Fact(DisplayName = "ReadExactly-数据不足抛出异常")]
    public void ReadExactly_EOF_Throws()
    {
        var src = new Byte[] { 10, 11, 12 };
        using var ms = new MemoryStream(src);
        var buf = new Byte[5];
        Assert.Throws<EndOfStreamException>(() => IOHelper.ReadExactly(ms, buf, 0, 5));
    }

    [Fact(DisplayName = "ReadExactly-偏移写入正确不破坏前缀")]
    public void ReadExactly_WithOffset()
    {
        var src = new Byte[] { 1, 2, 3, 4 };
        using var ms = new MemoryStream(src);
        var buf = Enumerable.Repeat((Byte)0xCC, 10).ToArray();
        var n = IOHelper.ReadExactly(ms, buf, 2, 4);
        Assert.Equal(4, n);
        // 前缀保持
        Assert.True(buf[0] == 0xCC && buf[1] == 0xCC);
        // 数据写入
        Assert.Equal(src, buf.Skip(2).Take(4).ToArray());
        // 尾部保持
        Assert.True(buf.Skip(6).All(b => b == 0xCC));
    }

    [Fact(DisplayName = "ReadExactly-count为0快速返回且不移动位置")]
    public void ReadExactly_CountZero()
    {
        var src = new Byte[] { 1, 2, 3 };
        using var ms = new MemoryStream(src);
        var buf = new Byte[3];
        var n = IOHelper.ReadExactly(ms, buf, 0, 0);
        Assert.Equal(0, n);
        Assert.Equal(0, ms.Position);
        Assert.True(buf.All(b => b == 0));
    }
    #endregion
}
