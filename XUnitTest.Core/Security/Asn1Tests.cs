using System.Security.Cryptography;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Security;

/// <summary>ASN.1编解码测试</summary>
public class Asn1Tests
{
    [Fact(DisplayName = "读取Integer类型")]
    public void ReadInteger()
    {
        // TLV: Tag=0x02(Integer), Length=0x01, Value=0x05
        var data = new Byte[] { 0x02, 0x01, 0x05 };
        var asn = Asn1.Read(data);

        Assert.NotNull(asn);
        Assert.Equal(Asn1Tags.Integer, asn.Tag);
        Assert.Equal(1, asn.Length);
        var val = asn.Value as Byte[];
        Assert.NotNull(val);
        Assert.Equal(0x05, val[0]);
    }

    [Fact(DisplayName = "读取Null类型")]
    public void ReadNull()
    {
        // TLV: Tag=0x05(Null), Length=0x00
        var data = new Byte[] { 0x05, 0x00 };
        var asn = Asn1.Read(data);

        Assert.NotNull(asn);
        Assert.Equal(Asn1Tags.Null, asn.Tag);
        Assert.Equal(0, asn.Length);
    }

    [Fact(DisplayName = "读取OctetString类型")]
    public void ReadOctetString()
    {
        // Tag=0x04(OctetString), Length=3, Value=0x01,0x02,0x03
        var data = new Byte[] { 0x04, 0x03, 0x01, 0x02, 0x03 };
        var asn = Asn1.Read(data);

        Assert.NotNull(asn);
        Assert.Equal(Asn1Tags.OctetString, asn.Tag);
        var val = asn.Value as Byte[];
        Assert.NotNull(val);
        Assert.Equal(3, val.Length);
        Assert.Equal(new Byte[] { 0x01, 0x02, 0x03 }, val);
    }

    [Fact(DisplayName = "读取Sequence类型")]
    public void ReadSequence()
    {
        // Sequence containing Integer(5) and Null
        // 30 05 02 01 05 05 00
        var data = new Byte[] { 0x30, 0x05, 0x02, 0x01, 0x05, 0x05, 0x00 };
        var asn = Asn1.Read(data);

        Assert.NotNull(asn);
        Assert.Equal(Asn1Tags.Sequence, asn.Tag);
        var children = asn.Value as Asn1[];
        Assert.NotNull(children);
        Assert.Equal(2, children.Length);
        Assert.Equal(Asn1Tags.Integer, children[0].Tag);
        Assert.Equal(Asn1Tags.Null, children[1].Tag);
    }

    [Fact(DisplayName = "读取ObjectIdentifier")]
    public void ReadOid()
    {
        // OID 1.2.840.113549.1.1.1 (RSA encryption)
        var data = new Byte[] { 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01 };
        var asn = Asn1.Read(data);

        Assert.NotNull(asn);
        Assert.Equal(Asn1Tags.ObjectIdentifier, asn.Tag);
        var oid = asn.Value as Oid;
        Assert.NotNull(oid);
        Assert.Equal("1.2.840.113549.1.1.1", oid.Value);
    }

    [Fact(DisplayName = "GetOids从嵌套结构提取")]
    public void GetOidsFromNestedStructure()
    {
        // Sequence { OID(1.2.840.113549.1.1.1), Null }
        var data = new Byte[] { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 };
        var asn = Asn1.Read(data);
        Assert.NotNull(asn);

        var oids = asn.GetOids();
        Assert.NotNull(oids);
        Assert.True(oids.Length >= 1);
        Assert.Equal("1.2.840.113549.1.1.1", oids[0].Value);
    }

    [Fact(DisplayName = "GetByteArray返回值")]
    public void GetByteArray()
    {
        var data = new Byte[] { 0x04, 0x03, 0x01, 0x02, 0x03 };
        var asn = Asn1.Read(data);
        Assert.NotNull(asn);

        var bytes = asn.GetByteArray();
        Assert.NotNull(bytes);
        Assert.Equal(new Byte[] { 0x01, 0x02, 0x03 }, bytes);
    }

    [Fact(DisplayName = "GetByteArray去除前导零")]
    public void GetByteArrayTrimZero()
    {
        // 手动构造
        var asn = new Asn1 { Tag = Asn1Tags.Integer, Value = new Byte[] { 0x00, 0x01, 0x02 } };
        var bytes = asn.GetByteArray(true);

        Assert.NotNull(bytes);
        Assert.Equal(2, bytes.Length);
        Assert.Equal(new Byte[] { 0x01, 0x02 }, bytes);
    }

    [Fact(DisplayName = "BitString去除前导零")]
    public void ReadBitString()
    {
        // Tag=0x03(BitString), Length=3, unused_bits=0, data=0xAB,0xCD
        var data = new Byte[] { 0x03, 0x03, 0x00, 0xAB, 0xCD };
        var asn = Asn1.Read(data);

        Assert.NotNull(asn);
        Assert.Equal(Asn1Tags.BitString, asn.Tag);
        var val = asn.Value as Byte[];
        Assert.NotNull(val);
        // 去除前导0x00后应该是{ 0xAB, 0xCD }
        Assert.Equal(new Byte[] { 0xAB, 0xCD }, val);
    }

    [Fact(DisplayName = "ToString格式化输出")]
    public void ToStringFormatted()
    {
        var data = new Byte[] { 0x05, 0x00 };
        var asn = Asn1.Read(data);
        Assert.NotNull(asn);
        Assert.Equal("Null", asn.ToString());
    }

    [Fact(DisplayName = "读取Stream")]
    public void ReadFromStream()
    {
        var data = new Byte[] { 0x02, 0x01, 0x0A };
        using var ms = new MemoryStream(data);
        var asn = Asn1.Read(ms);

        Assert.NotNull(asn);
        Assert.Equal(Asn1Tags.Integer, asn.Tag);
    }
}
