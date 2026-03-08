using System.Text;
using NewLife.Buffers;
using NewLife.Data;
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
        writer.Write(0x5566778899AABBCC);
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

    #region 构造函数测试
    [Fact]
    public void CtorFromIPacket()
    {
        var data = new Byte[] { 0, 0, 0, 0, 0 };
        var pk = new ArrayPacket(data);
        var writer = new SpanWriter(pk);

        Assert.Equal(0, writer.Position);
        Assert.Equal(5, writer.Capacity);

        writer.Write((Byte)0xAB);
        Assert.Equal(0xAB, data[0]);
    }

    [Fact]
    public void CtorFromByteArrayWithOffset()
    {
        var data = new Byte[10];
        var writer = new SpanWriter(data, 3, 5);

        Assert.Equal(0, writer.Position);
        Assert.Equal(5, writer.Capacity);

        writer.Write((Byte)0xFF);
        Assert.Equal(0xFF, data[3]);
        Assert.Equal(0, data[0]);
    }

    [Fact]
    public void CtorFromByteArrayDefaultCount()
    {
        var data = new Byte[10];
        var writer = new SpanWriter(data, 2);

        Assert.Equal(8, writer.Capacity);
    }
    #endregion

    #region Advance/GetSpan 边界测试
    [Fact]
    public void AdvanceNegativeThrows()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        var threw = false;
        try { writer.Advance(-1); } catch (ArgumentOutOfRangeException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void AdvanceExceedsCapacityThrows()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        var threw = false;
        try { writer.Advance(11); } catch (ArgumentOutOfRangeException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void GetSpanWithSizeHintTriggersFlushInStreamMode()
    {
        var ms = new MemoryStream();
        var buf = new Byte[8];
        var writer = new SpanWriter(buf, ms);

        writer.Write(0x11223344);
        Assert.Equal(4, writer.Position);

        // 请求 8 字节空间，当前只有 4 字节剩余 → 自动 Flush
        var span = writer.GetSpan(8);
        Assert.True(span.Length >= 8);
        Assert.True(ms.Length > 0);
    }

    [Fact]
    public void GetSpanExceedsNonStreamThrows()
    {
        var buffer = new Byte[4];
        var writer = new SpanWriter(buffer);
        writer.Write((Int16)1);

        var threw = false;
        try { writer.GetSpan(4); } catch (InvalidOperationException) { threw = true; }
        Assert.True(threw);
    }
    #endregion

    #region 大端字节序测试
    [Fact]
    public void WriteBigEndianInt16()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer) { IsLittleEndian = false };
        writer.Write((Int16)0x0102);

        Assert.Equal(0x01, buffer[0]);
        Assert.Equal(0x02, buffer[1]);
    }

    [Fact]
    public void WriteBigEndianUInt16()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer) { IsLittleEndian = false };
        writer.Write((UInt16)0x0304);

        Assert.Equal(0x03, buffer[0]);
        Assert.Equal(0x04, buffer[1]);
    }

    [Fact]
    public void WriteBigEndianInt32()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer) { IsLittleEndian = false };
        writer.Write(0x01020304);

        Assert.Equal(0x01, buffer[0]);
        Assert.Equal(0x02, buffer[1]);
        Assert.Equal(0x03, buffer[2]);
        Assert.Equal(0x04, buffer[3]);
    }

    [Fact]
    public void WriteBigEndianUInt32()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer) { IsLittleEndian = false };
        writer.Write(0x05060708u);

        Assert.Equal(0x05, buffer[0]);
        Assert.Equal(0x06, buffer[1]);
        Assert.Equal(0x07, buffer[2]);
        Assert.Equal(0x08, buffer[3]);
    }

    [Fact]
    public void WriteBigEndianInt64()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer) { IsLittleEndian = false };
        writer.Write(0x0102030405060708L);

        Assert.Equal(0x01, buffer[0]);
        Assert.Equal(0x08, buffer[7]);
    }

    [Fact]
    public void WriteBigEndianUInt64()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer) { IsLittleEndian = false };
        writer.Write(0x0102030405060708UL);

        Assert.Equal(0x01, buffer[0]);
        Assert.Equal(0x08, buffer[7]);
    }
    #endregion

    #region Write(String) 边界测试
    [Fact]
    public void WriteStringNullWithEncodedLength()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        var n = writer.Write((String?)null, 0);
        // null 字符串 length=0 模式写入 7 位编码的 0
        Assert.Equal(1, n);
        Assert.Equal(0, buffer[0]);
    }

    [Fact]
    public void WriteStringEmptyWithEncodedLength()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        var n = writer.Write("", 0);
        Assert.Equal(1, n);
        Assert.Equal(0, buffer[0]);
    }

    [Fact]
    public void WriteStringNullWithFixedLength()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        var n = writer.Write((String?)null, 5);
        // 固定长度写入 5 字节零
        Assert.Equal(5, n);
        for (var i = 0; i < 5; i++)
            Assert.Equal(0, buffer[i]);
    }

    [Fact]
    public void WriteStringNullWithNegativeLength()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        var n = writer.Write((String?)null, -1);
        // null 字符串 length<0 模式不写入任何内容
        Assert.Equal(0, n);
        Assert.Equal(0, writer.Position);
    }

    [Fact]
    public void WriteStringFixedShorterThanContent()
    {
        var buffer = new Byte[50];
        var writer = new SpanWriter(buffer);
        // "Hello World" 11 字节 UTF8，截断到 5
        var n = writer.Write("Hello World", 5, Encoding.UTF8);
        Assert.Equal(5, n);
        Assert.Equal("Hello", Encoding.UTF8.GetString(buffer, 0, 5));
    }

    [Fact]
    public void WriteStringFixedLongerThanContent()
    {
        var buffer = new Byte[50];
        var writer = new SpanWriter(buffer);
        var n = writer.Write("Hi", 10, Encoding.UTF8);
        Assert.Equal(10, n);
        Assert.Equal("Hi", Encoding.UTF8.GetString(buffer, 0, 2));
        // 剩余部分填零
        for (var i = 2; i < 10; i++)
            Assert.Equal(0, buffer[i]);
    }
    #endregion

    #region Write(Byte[]) 和 Write(IPacket) 异常测试
    [Fact]
    public void WriteNullByteArrayThrows()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        var threw = false;
        try { writer.Write((Byte[]?)null); } catch (ArgumentNullException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void WriteIPacket()
    {
        var data = new Byte[] { 1, 2, 3 };
        IPacket pk = new ArrayPacket(data);
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);

        var n = writer.Write(pk);
        Assert.Equal(3, n);
        Assert.Equal(3, writer.Position);
        Assert.Equal(new Byte[] { 1, 2, 3 }, buffer[..3]);
    }

    [Fact]
    public void WriteIPacketChained()
    {
        var pk1 = new ArrayPacket(new Byte[] { 1, 2 });
        var pk2 = new ArrayPacket(new Byte[] { 3, 4, 5 });
        pk1.Next = pk2;

        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        var n = writer.Write((IPacket)pk1);
        Assert.Equal(5, n);
        Assert.Equal(new Byte[] { 1, 2, 3, 4, 5 }, buffer[..5]);
    }

    [Fact]
    public void WriteNullIPacketThrows()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        var threw = false;
        try { writer.Write((IPacket)null!); } catch (ArgumentNullException) { threw = true; }
        Assert.True(threw);
    }
    #endregion

    #region Fill/FillZero/WriteRepeat 测试
    [Fact]
    public void FillTest()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        writer.Advance(2);
        var n = writer.Fill(0xAA, 5);
        Assert.Equal(5, n);
        Assert.Equal(7, writer.Position);
        for (var i = 2; i < 7; i++)
            Assert.Equal(0xAA, buffer[i]);
        Assert.Equal(0, buffer[0]);
        Assert.Equal(0, buffer[7]);
    }

    [Fact]
    public void FillZeroCountReturnsZero()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        Assert.Equal(0, writer.Fill(0xAA, 0));
        Assert.Equal(0, writer.Fill(0xAA, -1));
        Assert.Equal(0, writer.Position);
    }

    [Fact]
    public void FillZeroTest()
    {
        var buffer = new Byte[10];
        for (var i = 0; i < 10; i++) buffer[i] = 0xFF;
        var writer = new SpanWriter(buffer);
        var n = writer.FillZero(5);
        Assert.Equal(5, n);
        for (var i = 0; i < 5; i++)
            Assert.Equal(0, buffer[i]);
        Assert.Equal(0xFF, buffer[5]);
    }

    [Fact]
    public void FillZeroNonPositiveReturnsZero()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        Assert.Equal(0, writer.FillZero(0));
        Assert.Equal(0, writer.FillZero(-1));
    }

    [Fact]
    public void WriteRepeatTest()
    {
        var buffer = new Byte[20];
        var writer = new SpanWriter(buffer);
        ReadOnlySpan<Byte> pattern = [0xAB, 0xCD];
        var n = writer.WriteRepeat(pattern, 3);
        Assert.Equal(6, n);
        Assert.Equal(6, writer.Position);
        Assert.Equal(new Byte[] { 0xAB, 0xCD, 0xAB, 0xCD, 0xAB, 0xCD }, buffer[..6]);
    }

    [Fact]
    public void WriteRepeatZeroReturnsZero()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        Assert.Equal(0, writer.WriteRepeat([0xAB], 0));
        Assert.Equal(0, writer.WriteRepeat([], 3));
        Assert.Equal(0, writer.Position);
    }
    #endregion

    #region WriteEncodedInt 边界测试
    [Fact]
    public void WriteEncodedIntSmallValue()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        var n = writer.WriteEncodedInt(0);
        Assert.Equal(1, n);
        Assert.Equal(0, buffer[0]);
    }

    [Fact]
    public void WriteEncodedIntMaxSingleByte()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        var n = writer.WriteEncodedInt(127);
        Assert.Equal(1, n);
        Assert.Equal(127, buffer[0]);
    }

    [Fact]
    public void WriteEncodedIntTwoBytes()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        var n = writer.WriteEncodedInt(128);
        Assert.Equal(2, n);
    }

    [Fact]
    public void WriteEncodedIntNegativeValueFiveBytes()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        // 负数占 5 字节
        var n = writer.WriteEncodedInt(-1);
        Assert.Equal(5, n);
    }

    [Theory(DisplayName = "WriteEncodedInt在缓冲区刚好足够时不应抛异常")]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(127, 1)]
    [InlineData(128, 2)]
    [InlineData(0x3FFF, 2)]
    [InlineData(0x4000, 3)]
    [InlineData(0x1F_FFFF, 3)]
    [InlineData(0x20_0000, 4)]
    [InlineData(0x0FFF_FFFF, 4)]
    [InlineData(0x1000_0000, 5)]
    [InlineData(-1, 5)]
    public void WriteEncodedIntWithExactBuffer(Int32 value, Int32 expectedBytes)
    {
        // 缓冲区大小恰好等于实际编码所需字节数，不应因多余的空间要求而失败
        var buffer = new Byte[expectedBytes];
        var writer = new SpanWriter(buffer);

        var n = writer.WriteEncodedInt(value);

        Assert.Equal(expectedBytes, n);
        Assert.Equal(expectedBytes, writer.Position);

        // 验证写入结果可被正确读回
        var reader = new SpanReader(buffer);
        Assert.Equal(value, reader.ReadEncodedInt());
    }

    [Theory(DisplayName = "WriteEncodedInt缓冲区不足实际编码长度时应抛异常")]
    [InlineData(128, 2)]
    [InlineData(0x4000, 3)]
    [InlineData(0x20_0000, 4)]
    public void WriteEncodedIntInsufficientBufferThrows(Int32 value, Int32 requiredBytes)
    {
        // 缓冲区比实际所需少 1 字节，必须抛异常
        var buffer = new Byte[requiredBytes - 1];
        var writer = new SpanWriter(buffer);

        var threw = false;
        try { writer.WriteEncodedInt(value); } catch (InvalidOperationException) { threw = true; }
        Assert.True(threw);
    }
    #endregion

    #region WriteArray 测试
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void WriteArrayThenReadArray(Int32 sizeOf)
    {
        var buffer = new Byte[64];
        var writer = new SpanWriter(buffer);
        ReadOnlySpan<Byte> data = [1, 2, 3, 4, 5];
        var n = writer.WriteArray(data, sizeOf);

        var reader = new SpanReader(buffer);
        var result = reader.ReadArray(sizeOf);
        Assert.Equal(data.ToArray(), result.ToArray());
        Assert.Equal(n, reader.Position);
    }

    [Fact]
    public void WriteArrayEmptyData()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        var n = writer.WriteArray([], 2);
        // 只写长度前缀 0
        Assert.Equal(2, n);
        Assert.Equal(0, buffer[0]);
        Assert.Equal(0, buffer[1]);
    }

    [Fact]
    public void WriteArrayInvalidSizeOfThrows()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        var threw = false;
        try { writer.WriteArray([1, 2], 3); } catch (ArgumentOutOfRangeException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void WriteArraySizeOf1()
    {
        var buffer = new Byte[20];
        var writer = new SpanWriter(buffer);
        ReadOnlySpan<Byte> data = [0xAA, 0xBB, 0xCC];
        var n = writer.WriteArray(data, 1);
        Assert.Equal(4, n); // 1 字节长度 + 3 字节数据
        Assert.Equal(3, buffer[0]); // 长度
        Assert.Equal(0xAA, buffer[1]);
        Assert.Equal(0xBB, buffer[2]);
        Assert.Equal(0xCC, buffer[3]);
    }

    [Fact]
    public void WriteArraySizeOf4()
    {
        var buffer = new Byte[20];
        var writer = new SpanWriter(buffer);
        ReadOnlySpan<Byte> data = [0x01, 0x02];
        var n = writer.WriteArray(data, 4);
        Assert.Equal(6, n); // 4 字节长度 + 2 字节数据

        var reader = new SpanReader(buffer);
        Assert.Equal(2, reader.ReadInt32()); // 长度
        Assert.Equal(0x01, reader.ReadByte());
        Assert.Equal(0x02, reader.ReadByte());
    }
    #endregion

    #region WriteLengthString 测试
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void WriteLengthStringThenReadLengthString(Int32 sizeOf)
    {
        var buffer = new Byte[64];
        var writer = new SpanWriter(buffer);
        var n = writer.WriteLengthString("Hello", sizeOf);

        var reader = new SpanReader(buffer);
        var result = reader.ReadLengthString(sizeOf);
        Assert.Equal("Hello", result);
        Assert.Equal(n, reader.Position);
    }

    [Fact]
    public void WriteLengthStringNull()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        var n = writer.WriteLengthString(null, 2);
        Assert.Equal(2, n);
        Assert.Equal(0, buffer[0]);
        Assert.Equal(0, buffer[1]);
    }

    [Fact]
    public void WriteLengthStringEmpty()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        var n = writer.WriteLengthString("", 1);
        Assert.Equal(1, n);
        Assert.Equal(0, buffer[0]);
    }

    [Fact]
    public void WriteLengthStringWithCustomEncoding()
    {
        var buffer = new Byte[64];
        var writer = new SpanWriter(buffer);
        var text = "ABC";
        var n = writer.WriteLengthString(text, 2, Encoding.ASCII);

        var reader = new SpanReader(buffer);
        var result = reader.ReadLengthString(2, Encoding.ASCII);
        Assert.Equal(text, result);
    }

    [Fact]
    public void WriteLengthStringInvalidSizeOfThrows()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        var threw = false;
        try { writer.WriteLengthString("Hi", 5); } catch (ArgumentOutOfRangeException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void WriteLengthStringChineseUtf8()
    {
        var buffer = new Byte[64];
        var writer = new SpanWriter(buffer);
        var text = "你好世界";
        var n = writer.WriteLengthString(text, 2);

        var reader = new SpanReader(buffer);
        var result = reader.ReadLengthString(2);
        Assert.Equal(text, result);
        Assert.Equal(n, reader.Position);
    }
    #endregion

    #region 流模式边界测试
    [Fact]
    public void StreamCtorNullStreamThrows()
    {
        var buf = new Byte[8];
        var threw = false;
        try { _ = new SpanWriter(buf, null!); } catch (ArgumentNullException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void StreamCtor_SingleWriteExceedsBufferThrows()
    {
        var ms = new MemoryStream();
        var buf = new Byte[4];
        var writer = new SpanWriter(buf, ms);

        // 单次写入 8 字节 Int64，超过 4 字节缓冲区
        // Flush 后 _index=0，但 size(8) > buf.Length(4) → 抛异常
        var threw = false;
        try { writer.Write(0x0102030405060708L); } catch (InvalidOperationException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void StreamCtor_DisposeFlushesRemaining()
    {
        var ms = new MemoryStream();
        var buf = new Byte[16];
        var writer = new SpanWriter(buf, ms);

        writer.Write((Byte)0xAA);
        writer.Write((Byte)0xBB);
        writer.Dispose();

        Assert.Equal(2, ms.Length);
        Assert.Equal(0xAA, ms.ToArray()[0]);
        Assert.Equal(0xBB, ms.ToArray()[1]);
    }

    [Fact]
    public void WrittenSpanAndWrittenCount()
    {
        var buffer = new Byte[20];
        var writer = new SpanWriter(buffer);
        writer.Write((Int16)0x1234);
        writer.Write((Byte)0xFF);

        Assert.Equal(3, writer.WrittenCount);
        var written = writer.WrittenSpan;
        Assert.Equal(3, written.Length);
        Assert.Equal(0x34, written[0]); // little-endian
        Assert.Equal(0x12, written[1]);
        Assert.Equal(0xFF, written[2]);
    }
    #endregion
}
