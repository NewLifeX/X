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

#if NET6_0_OR_GREATER
        var buf = Rand.NextBytes(57);
        writer.Write(buf);
        Assert.Equal(buf, reader.ReadBytes(buf.Length));

        var span2 = new Span<Byte>(buf);
        writer.Write(span2);
        Assert.Equal(span2, reader.ReadBytes(buf.Length));

        var span3 = new ReadOnlySpan<Byte>(buf);
        writer.Write(span3);
        Assert.Equal(span3, reader.ReadBytes(buf.Length));
#endif

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

    [Fact]
    public void ReadByteTest()
    {
        var data = new Byte[] { 1, 2, 3 };
        var reader = new SpanReader(data);

        Assert.Equal(1, reader.ReadByte());
        Assert.Equal(2, reader.ReadByte());
        Assert.Equal(3, reader.ReadByte());
    }

    [Fact]
    public void ReadInt16Test()
    {
        var data = new Byte[] { 1, 0, 2, 0 };
        var reader = new SpanReader(data);

        Assert.Equal(1, reader.ReadInt16());
        Assert.Equal(2, reader.ReadInt16());
    }

    [Fact]
    public void ReadInt32Test()
    {
        var data = new Byte[] { 1, 0, 0, 0, 2, 0, 0, 0 };
        var reader = new SpanReader(data);

        Assert.Equal(1, reader.ReadInt32());
        Assert.Equal(2, reader.ReadInt32());
    }

    [Fact]
    public void ReadStringTest()
    {
        var data = Encoding.UTF8.GetBytes("Hello, World!");
        var reader = new SpanReader(data);

        Assert.Equal("Hello, World!", reader.ReadString(data.Length));
    }

    [Fact]
    public void ReadBytesTest()
    {
        var data = new Byte[] { 1, 2, 3, 4, 5 };
        var reader = new SpanReader(data);

        var result = reader.ReadBytes(3);
        Assert.Equal(new Byte[] { 1, 2, 3 }, result.ToArray());
    }

    [Fact]
    public void GCAllocationTest()
    {
        var data = new Byte[100];
        var reader = new SpanReader(data);

        var initialGen0Collections = GC.CollectionCount(0);
        var initialGen1Collections = GC.CollectionCount(1);
        var initialGen2Collections = GC.CollectionCount(2);
        var memory = GC.GetAllocatedBytesForCurrentThread();

        reader.ReadBytes(50);

        Assert.Equal(memory, GC.GetAllocatedBytesForCurrentThread());
        Assert.Equal(initialGen0Collections, GC.CollectionCount(0));
        Assert.Equal(initialGen1Collections, GC.CollectionCount(1));
        Assert.Equal(initialGen2Collections, GC.CollectionCount(2));
    }
}
