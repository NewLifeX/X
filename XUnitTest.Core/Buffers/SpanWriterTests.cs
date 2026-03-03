using System;
using System.IO;
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

    #region 流模式测试
    [Fact]
    public void StreamCtor_BasicWrite()
    {
        var ms = new MemoryStream();
        var buf = new Byte[16];
        using var writer = new SpanWriter(buf, ms);

        writer.Write(42);
        writer.Write((Int16)7);
        writer.Flush();

        Assert.Equal(6, writer.TotalWritten);
        Assert.Equal(6, ms.Length);

        ms.Position = 0;
        var reader = new SpanReader(ms.ToArray());
        Assert.Equal(42, reader.ReadInt32());
        Assert.Equal(7, reader.ReadInt16());
    }

    [Fact]
    public void StreamCtor_AutoFlushOnBufferFull()
    {
        var ms = new MemoryStream();
        var buf = new Byte[8];
        using var writer = new SpanWriter(buf, ms);

        // 写入 4 字节，缓冲区还够
        writer.Write(0x11223344);
        Assert.Equal(0, ms.Length);

        // 再写 8 字节，超出缓冲区 → 触发自动 Flush + 继续写入
        writer.Write((Int64)0x5566778899AABBCC);
        Assert.True(ms.Length > 0);

        writer.Flush();
        Assert.Equal(12, writer.TotalWritten);

        ms.Position = 0;
        var reader = new SpanReader(ms.ToArray());
        Assert.Equal(0x11223344, reader.ReadInt32());
        Assert.Equal(0x5566778899AABBCC, reader.ReadInt64());
    }

    [Fact]
    public void StreamCtor_LargeWriteExceedsBuffer()
    {
        var ms = new MemoryStream();
        var buf = new Byte[8];
        using var writer = new SpanWriter(buf, ms);

        // 写入 20 字节数组，远超 8 字节缓冲区
        var data = new Byte[20];
        for (var i = 0; i < data.Length; i++) data[i] = (Byte)i;

        writer.Write(data);
        writer.Flush();

        Assert.Equal(20, writer.TotalWritten);
        Assert.Equal(data, ms.ToArray());
    }

    [Fact]
    public void StreamCtor_MultipleFlush()
    {
        var ms = new MemoryStream();
        var buf = new Byte[4];
        using var writer = new SpanWriter(buf, ms);

        for (var i = 0; i < 100; i++)
        {
            writer.Write((Byte)i);
        }
        writer.Flush();

        Assert.Equal(100, writer.TotalWritten);
        Assert.Equal(100, ms.Length);

        var result = ms.ToArray();
        for (var i = 0; i < 100; i++)
        {
            Assert.Equal((Byte)i, result[i]);
        }
    }

    [Fact]
    public void StreamCtor_WriteStringWithLength()
    {
        var ms = new MemoryStream();
        var buf = new Byte[16];
        using var writer = new SpanWriter(buf, ms);

        writer.Write("Hello, World!", 0, Encoding.UTF8);
        writer.Flush();

        Assert.Equal(14, writer.TotalWritten);

        var reader = new SpanReader(ms.ToArray());
        var s = reader.ReadString(0, Encoding.UTF8);
        Assert.Equal("Hello, World!", s);
    }

    [Fact]
    public void StreamCtor_TotalWrittenTracking()
    {
        var ms = new MemoryStream();
        var buf = new Byte[8];
        using var writer = new SpanWriter(buf, ms);

        Assert.Equal(0, writer.TotalWritten);

        writer.Write(42);
        Assert.Equal(4, writer.TotalWritten);

        // 触发 Flush（8字节缓冲区，已写4，再写8需 Flush）
        writer.Write((Int64)99);
        Assert.Equal(12, writer.TotalWritten);

        writer.Flush();
        Assert.Equal(12, writer.TotalWritten);
    }

    [Fact]
    public void StreamCtor_NonStreamMode_ThrowsOnOverflow()
    {
        var buffer = new Byte[4];
        var writer = new SpanWriter(buffer);
        writer.Write((Int16)1);

        var ex = false;
        try
        {
            writer.Write(42);
        }
        catch (InvalidOperationException)
        {
            ex = true;
        }
        Assert.True(ex);
    }

    [Fact]
    public void StreamCtor_FlushNoopInNonStreamMode()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        writer.Write((Byte)42);
        writer.Flush();

        Assert.Equal(1, writer.Position);
        Assert.Equal(42, buffer[0]);
    }
    #endregion
}
