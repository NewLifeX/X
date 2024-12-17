using System;
using System.Text;
using NewLife.Buffers;
using Xunit;

namespace XUnitTest.Buffers;

public class SpanWriterTests
{
    [Fact]
    public void CtorTest()
    {
        Span<Byte> span = stackalloc Byte[100];
        var writer = new SpanWriter(span);

        Assert.Equal(0, writer.Position);
        Assert.Equal(span.Length, writer.Capacity);
        Assert.Equal(span.Length, writer.FreeCapacity);
        Assert.Equal(span.Length, writer.GetSpan().Length);

        writer.Advance(33);

        Assert.Equal(33, writer.Position);
        Assert.Equal(span.Length, writer.Capacity);
        Assert.Equal(span.Length - 33, writer.FreeCapacity);
        Assert.Equal(span.Length - 33, writer.GetSpan().Length);

        //Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetSpan(100));
    }

    [Fact]
    public void TestAdvance()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        writer.Advance(5);
        Assert.Equal(5, writer.Position);
    }

    [Fact]
    public void TestGetSpan()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        var span = writer.GetSpan(5);
        Assert.Equal(10, span.Length);
    }

    [Fact]
    public void TestWriteByte()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        writer.WriteByte(1);
        Assert.Equal(1, writer.Position);
        Assert.Equal(1, buffer[0]);
    }

    [Fact]
    public void TestWriteInt16()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        writer.Write((Int16)1);
        Assert.Equal(2, writer.Position);
        Assert.Equal(1, BitConverter.ToInt16(buffer, 0));
    }

    [Fact]
    public void TestWriteUInt16()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        writer.Write((UInt16)1);
        Assert.Equal(2, writer.Position);
        Assert.Equal(1, BitConverter.ToUInt16(buffer, 0));
    }

    [Fact]
    public void TestWriteInt32()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        writer.Write(1);
        Assert.Equal(4, writer.Position);
        Assert.Equal(1, BitConverter.ToInt32(buffer, 0));
    }

    [Fact]
    public void TestWriteUInt32()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        writer.Write((UInt32)1);
        Assert.Equal(4, writer.Position);
        Assert.Equal(1u, BitConverter.ToUInt32(buffer, 0));
    }

    [Fact]
    public void TestWriteInt64()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        writer.Write((Int64)1);
        Assert.Equal(8, writer.Position);
        Assert.Equal(1, BitConverter.ToInt64(buffer, 0));
    }

    [Fact]
    public void TestWriteUInt64()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        writer.Write((UInt64)1);
        Assert.Equal(8, writer.Position);
        Assert.Equal(1u, BitConverter.ToUInt64(buffer, 0));
    }

    [Fact]
    public void TestWriteSingle()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        writer.Write((Single)1.0);
        Assert.Equal(4, writer.Position);
        Assert.Equal(1.0f, BitConverter.ToSingle(buffer, 0));
    }

    [Fact]
    public void TestWriteDouble()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        writer.Write((Double)1.0);
        Assert.Equal(8, writer.Position);
        Assert.Equal(1.0, BitConverter.ToDouble(buffer, 0));
    }

    [Fact]
    public void TestWriteString()
    {
        var buffer = new Byte[50];
        var writer = new SpanWriter(buffer);
        writer.Write("Hello", -1, Encoding.UTF8);
        Assert.Equal(5, writer.Position);
        Assert.Equal("Hello", Encoding.UTF8.GetString(buffer, 0, 5));
    }

    [Fact]
    public void TestWriteByteArray()
    {
        var buffer = new Byte[10];

        var memory = GC.GetAllocatedBytesForCurrentThread();
        var writer = new SpanWriter(buffer);
        writer.Write([1, 2, 3]);

        Assert.Equal(memory, GC.GetAllocatedBytesForCurrentThread());
        Assert.Equal(3, writer.Position);
        Assert.Equal(new Byte[] { 1, 2, 3 }, buffer[..3]);
    }

    [Fact]
    public void TestWriteReadOnlySpan()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        writer.Write(new ReadOnlySpan<Byte>([1, 2, 3]));
        Assert.Equal(3, writer.Position);
        Assert.Equal(new Byte[] { 1, 2, 3 }, buffer[..3]);
    }

    [Fact]
    public void TestWriteSpan()
    {
        var buffer = new Byte[10];
        var buf = new Byte[] { 1, 2, 3 };

        var memory = GC.GetAllocatedBytesForCurrentThread();
        var writer = new SpanWriter(buffer);
        writer.Write(new Span<Byte>(buf));

        Assert.Equal(memory, GC.GetAllocatedBytesForCurrentThread());
        Assert.Equal(3, writer.Position);
        Assert.Equal(buf, buffer[..3]);
    }

    [Fact]
    public void TestWriteStruct()
    {
        var buffer = new Byte[10];

        var memory = GC.GetAllocatedBytesForCurrentThread();
        var writer = new SpanWriter(buffer);
        writer.Write(new TestStruct { Value = 1 });

        Assert.Equal(memory, GC.GetAllocatedBytesForCurrentThread());
        Assert.Equal(4, writer.Position);
        Assert.Equal(1, BitConverter.ToInt32(buffer, 0));
    }

    [Fact]
    public void TestWriteEncodedInt()
    {
        var buffer = new Byte[10];

        var memory = GC.GetAllocatedBytesForCurrentThread();
        var writer = new SpanWriter(buffer);
        writer.WriteEncodedInt(128);

        Assert.Equal(memory, GC.GetAllocatedBytesForCurrentThread());
        Assert.Equal(2, writer.Position);
        Assert.Equal(new Byte[] { 0x80, 0x01 }, buffer[..2]);
    }

    private struct TestStruct
    {
        public Int32 Value;
    }
}
