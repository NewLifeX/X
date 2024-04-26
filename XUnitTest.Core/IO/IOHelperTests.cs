using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    private static readonly Byte[] NewLine2 = new[] { (Byte)'\r', (Byte)'\n', (Byte)'\r', (Byte)'\n' };
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

        buf = buf.Reverse().ToArray();
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

        buf = buf.Reverse().ToArray();
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
}
