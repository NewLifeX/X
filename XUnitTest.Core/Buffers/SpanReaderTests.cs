using System;
using System.Text;
using NewLife;
using NewLife.Buffers;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Buffers;

public class SpanReaderTests
{
    [Fact]
    public void CtorTest()
    {
        Span<Byte> span = stackalloc Byte[100];
        var reader = new SpanReader(span);

        Assert.Equal(0, reader.Position);
        Assert.Equal(span.Length, reader.Capacity);
        Assert.Equal(span.Length, reader.FreeCapacity);
        Assert.Equal(span.Length, reader.GetSpan().Length);

        reader.Advance(33);

        Assert.Equal(33, reader.Position);
        Assert.Equal(span.Length, reader.Capacity);
        Assert.Equal(span.Length - 33, reader.FreeCapacity);
        Assert.Equal(span.Length - 33, reader.GetSpan().Length);

        //Assert.Throws<ArgumentOutOfRangeException>(() => reader.GetSpan(100));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Test2(Boolean isLittle)
    {
        Span<Byte> span = new Byte[1024];
        var reader = new SpanReader(span) { IsLittleEndian = isLittle };
        var writer = new SpanWriter(span) { IsLittleEndian = isLittle };

        var b = (Byte)(Rand.Next(255) - 128);
        writer.Write(b);
        Assert.Equal(b, reader.ReadByte());

        var n16 = (Int16)Rand.Next(65536);
        writer.Write(n16);
        Assert.Equal(n16, reader.ReadInt16());

        var u16 = (UInt16)Rand.Next(65536);
        writer.Write(u16);
        Assert.Equal(u16, reader.ReadUInt16());

        var n32 = (Int32)Rand.Next();
        writer.Write(n32);
        Assert.Equal(n32, reader.ReadInt32());

        var u32 = (UInt32)Rand.Next();
        writer.Write(u32);
        Assert.Equal(u32, reader.ReadUInt32());

        var n64 = (Int64)Rand.Next();
        writer.Write(n64);
        Assert.Equal(n64, reader.ReadInt64());

        var u64 = (UInt64)Rand.Next();
        writer.Write(u64);
        Assert.Equal(u64, reader.ReadUInt64());

        var s = (Single)Rand.Next() / 333f;
        writer.Write(s);
        Assert.Equal(s, reader.ReadSingle());

        var d = (Double)Rand.Next() / 777d;
        writer.Write(d);
        Assert.Equal(d, reader.ReadDouble());

        var n = Rand.Next(128);
        writer.WriteEncodedInt(n);
        Assert.Equal(n, reader.ReadEncodedInt());

        n = Rand.Next(128, 65536);
        writer.WriteEncodedInt(n);
        Assert.Equal(n, reader.ReadEncodedInt());

        n = Rand.Next(65536);
        writer.WriteEncodedInt(n);
        Assert.Equal(n, reader.ReadEncodedInt());

        var buf = Rand.NextBytes(57);
        writer.Write(buf);
        Assert.Equal(buf, reader.ReadBytes(buf.Length));

        var span2 = new Span<Byte>(buf);
        writer.Write(span2);
        Assert.Equal(span2, reader.ReadBytes(buf.Length));

        var span3 = new ReadOnlySpan<Byte>(buf);
        writer.Write(span3);
        Assert.Equal(span3, reader.ReadBytes(buf.Length));

        var str = Rand.NextString(33);
        writer.Write(str);
        Assert.Equal(str, reader.ReadString());

        writer.Write(str, 32, Encoding.ASCII);
        Assert.Equal(str[..32], reader.ReadString(32, Encoding.ASCII));

        writer.Write(str, 44, Encoding.ASCII);
        Assert.Equal(str + new String('\0', 44 - 33), reader.ReadString(44, Encoding.ASCII));

        // 这个测试必须在最后
        writer.Write(str, -1, Encoding.Default);
        Assert.Equal(str, reader.ReadString(-1, Encoding.Default)[..str.Length]);
    }
}
