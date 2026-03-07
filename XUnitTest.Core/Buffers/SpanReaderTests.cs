using System.Text;
using NewLife.Buffers;
using NewLife.Data;
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
        Assert.Equal(span.Length, reader.Available);
        Assert.Equal(span.Length, reader.GetSpan().Length);

        reader.Advance(33);

        Assert.Equal(33, reader.Position);
        Assert.Equal(span.Length, reader.Capacity);
        Assert.Equal(span.Length - 33, reader.Available);
        Assert.Equal(span.Length - 33, reader.GetSpan().Length);
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

        var n32 = Rand.Next();
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

        var s = Rand.Next() / 333f;
        writer.Write(s);
        Assert.Equal(s, reader.ReadSingle());

        var d = Rand.Next() / 777d;
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
        reader.Advance(1);

        var result = reader.ReadBytes(3);
        Assert.Equal(new Byte[] { 2, 3, 4 }, result.ToArray());
    }

    [Fact]
    public void ReadTest()
    {
        var data = new Byte[] { 1, 2, 3, 4, 5 };
        var reader = new SpanReader(data);
        reader.Advance(1);

        var buf = new Byte[3];
        var result = reader.Read(buf);
        Assert.Equal(new Byte[] { 2, 3, 4 }, buf);
    }

    [Fact]
    public void ReadPacket()
    {
        var data = new Byte[] { 1, 2, 3, 4, 5 };
        var reader = new SpanReader(data);
        reader.Advance(1);

        try
        {
            reader.ReadPacket(3);
        }
        catch (Exception ex)
        {
            Assert.NotNull(ex as InvalidOperationException);
        }

        var pk = new ArrayPacket(data);
        reader = new SpanReader(pk);
        reader.Advance(1);

        var result = reader.ReadPacket(3);
        Assert.Equal(new Byte[] { 2, 3, 4 }, result.ToArray());
        Assert.True(data == ((ArrayPacket)result).Buffer);
    }

    [Fact]
    public void StreamReadTest()
    {
        var data = new Byte[] { 1, 2, 3, 4, 5 };
        var ms = new MemoryStream(data);
        var reader = new SpanReader(ms);
        reader.ReadByte();

        var result = reader.ReadBytes(3);
        Assert.Equal(new Byte[] { 2, 3, 4 }, result.ToArray());
    }

    [Fact]
    public void StreamAutoExpansion()
    {
        var head = new Byte[] { 0x05 }; // 长度前缀
        var body = Encoding.UTF8.GetBytes("Hello");
        using var ms = new MemoryStream();
        ms.Write(head, 0, head.Length);
        ms.Write(body, 0, body.Length);
        ms.Position = 0;

        var reader = new SpanReader(ms, bufferSize: 2) { MaxCapacity = 32 };
        var len = reader.ReadByte();
        Assert.Equal(5, len);
        var payload = reader.ReadBytes(len);
        Assert.Equal("Hello", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public void ReadPacketFromChainedPacket()
    {
        ArrayPacket head = new(new Byte[] { 0x81, 0x05 });
        ArrayPacket payload = new(Encoding.UTF8.GetBytes("Hello"));
        head.Next = payload;

        var reader = new SpanReader(head);
        var frameHead = reader.ReadPacket(2);
        Assert.Equal(2, frameHead.Length);
        var body = reader.ReadPacket(5);
        Assert.Equal(5, body.Length);
        Assert.Equal("Hello", body.ToStr());
    }

    [Fact]
    public void MaxCapacityLimit()
    {
        using var ms = new MemoryStream();
        ms.WriteByte(10); // 后续要读的长度
        ms.Write(new Byte[10], 0, 10);
        ms.Position = 0;

        var reader = new SpanReader(ms, bufferSize: 4) { MaxCapacity = 8 };
        var len = reader.ReadByte();
        Assert.Equal(10, len);

        var threw = false;
        try
        {
            _ = reader.ReadBytes(len);
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }
        Assert.True(threw);
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

    #region 构造函数补充测试
    [Fact]
    public void CtorFromReadOnlySpan()
    {
        ReadOnlySpan<Byte> span = new Byte[] { 1, 2, 3 };
        var reader = new SpanReader(span);

        Assert.Equal(0, reader.Position);
        Assert.Equal(3, reader.Capacity);
        Assert.Equal(3, reader.Available);
    }

    [Fact]
    public void CtorFromByteArrayWithOffset()
    {
        var data = new Byte[] { 0, 0, 1, 2, 3, 0, 0 };
        var reader = new SpanReader(data, 2, 3);

        Assert.Equal(0, reader.Position);
        Assert.Equal(3, reader.Capacity);
        Assert.Equal(1, reader.ReadByte());
        Assert.Equal(2, reader.ReadByte());
        Assert.Equal(3, reader.ReadByte());
    }

    [Fact]
    public void CtorFromByteArrayDefaultCount()
    {
        var data = new Byte[] { 0, 0, 1, 2, 3 };
        var reader = new SpanReader(data, 2);

        Assert.Equal(3, reader.Capacity);
        Assert.Equal(1, reader.ReadByte());
    }

    [Fact]
    public void CtorFromIPacketSingle()
    {
        var pk = new ArrayPacket(new Byte[] { 0xAA, 0xBB });
        var reader = new SpanReader(pk);

        Assert.Equal(2, reader.Capacity);
        Assert.Equal(0xAA, reader.ReadByte());
        Assert.Equal(0xBB, reader.ReadByte());
    }

    [Fact]
    public void CtorFromIPacketNull()
    {
        var threw = false;
        try { _ = new SpanReader((IPacket)null!); } catch (ArgumentNullException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void CtorFromStreamWithData()
    {
        var ms = new MemoryStream(new Byte[] { 1, 2, 3 });
        var pk = new ArrayPacket(new Byte[] { 0xAA });
        var reader = new SpanReader(ms, pk, 4);

        Assert.Equal(1, reader.Capacity);
        Assert.Equal(0xAA, reader.ReadByte());
    }
    #endregion

    #region Advance 边界测试
    [Fact]
    public void AdvanceNegativeThrows()
    {
        var reader = new SpanReader(new Byte[10]);
        var threw = false;
        try { reader.Advance(-1); } catch (ArgumentOutOfRangeException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void AdvanceExceedsAvailableThrows()
    {
        var reader = new SpanReader(new Byte[5]);
        var threw = false;
        try { reader.Advance(6); } catch (InvalidOperationException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void AdvanceZeroNoop()
    {
        var reader = new SpanReader(new Byte[5]);
        reader.Advance(0);
        Assert.Equal(0, reader.Position);
    }
    #endregion

    #region GetSpan 边界测试
    [Fact]
    public void GetSpanExceedsCapacityThrows()
    {
        var reader = new SpanReader(new Byte[5]);
        reader.Advance(3);
        var threw = false;
        try { reader.GetSpan(3); } catch (ArgumentOutOfRangeException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void GetSpanNoHint()
    {
        var reader = new SpanReader(new Byte[5]);
        reader.Advance(2);
        var span = reader.GetSpan();
        Assert.Equal(3, span.Length);
    }
    #endregion

    #region 大端字节序独立测试
    [Fact]
    public void ReadBigEndianInt16()
    {
        var data = new Byte[] { 0x01, 0x02 };
        var reader = new SpanReader(data) { IsLittleEndian = false };
        Assert.Equal(0x0102, reader.ReadInt16());
    }

    [Fact]
    public void ReadBigEndianUInt16()
    {
        var data = new Byte[] { 0x03, 0x04 };
        var reader = new SpanReader(data) { IsLittleEndian = false };
        Assert.Equal(0x0304, reader.ReadUInt16());
    }

    [Fact]
    public void ReadBigEndianInt32()
    {
        var data = new Byte[] { 0x01, 0x02, 0x03, 0x04 };
        var reader = new SpanReader(data) { IsLittleEndian = false };
        Assert.Equal(0x01020304, reader.ReadInt32());
    }

    [Fact]
    public void ReadBigEndianUInt32()
    {
        var data = new Byte[] { 0x05, 0x06, 0x07, 0x08 };
        var reader = new SpanReader(data) { IsLittleEndian = false };
        Assert.Equal(0x05060708u, reader.ReadUInt32());
    }

    [Fact]
    public void ReadBigEndianInt64()
    {
        var data = new Byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
        var reader = new SpanReader(data) { IsLittleEndian = false };
        Assert.Equal(0x0102030405060708L, reader.ReadInt64());
    }

    [Fact]
    public void ReadBigEndianUInt64()
    {
        var data = new Byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
        var reader = new SpanReader(data) { IsLittleEndian = false };
        Assert.Equal(0x0102030405060708UL, reader.ReadUInt64());
    }
    #endregion

    #region ReadString 补充测试
    [Fact]
    public void ReadStringNegativeLengthReadsAll()
    {
        var text = "Hello";
        var data = Encoding.UTF8.GetBytes(text);
        var reader = new SpanReader(data);
        var result = reader.ReadString(-1);
        Assert.Equal(text, result);
        Assert.Equal(data.Length, reader.Position);
    }

    [Fact]
    public void ReadStringEncodedLengthZeroReturnsEmpty()
    {
        // 首字节 0 表示 7 位编码长度为 0
        var data = new Byte[] { 0x00 };
        var reader = new SpanReader(data);
        var result = reader.ReadString(0);
        Assert.Equal(String.Empty, result);
    }

    [Fact]
    public void ReadStringWithEncoding()
    {
        var text = "Test";
        var encoded = Encoding.ASCII.GetBytes(text);
        var reader = new SpanReader(encoded);
        var result = reader.ReadString(4, Encoding.ASCII);
        Assert.Equal(text, result);
    }
    #endregion

    #region ReadBytes/Read 边界测试
    [Fact]
    public void ReadBytesNegativeLengthThrows()
    {
        var reader = new SpanReader(new Byte[5]);
        var threw = false;
        try { reader.ReadBytes(-1); } catch (ArgumentOutOfRangeException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void ReadBytesZeroLength()
    {
        var reader = new SpanReader(new Byte[5]);
        var result = reader.ReadBytes(0);
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public void ReadBytesExceedsAvailableThrows()
    {
        var reader = new SpanReader(new Byte[3]);
        var threw = false;
        try { reader.ReadBytes(4); } catch (InvalidOperationException) { threw = true; }
        Assert.True(threw);
    }
    #endregion

    #region ReadPacket 边界测试
    [Fact]
    public void ReadPacketNegativeLengthThrows()
    {
        var reader = new SpanReader(new Byte[5]);
        var threw = false;
        try { reader.ReadPacket(-1); } catch (ArgumentOutOfRangeException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void ReadPacketFromSpanCopies()
    {
        // 从 Span 构造（无底层 IPacket），ReadPacket 会拷贝到 OwnerPacket
        var data = new Byte[] { 1, 2, 3, 4, 5 };
        var reader = new SpanReader(new ReadOnlySpan<Byte>(data));
        reader.Advance(1);

        var pk = reader.ReadPacket(3);
        Assert.Equal(3, pk.Length);
        Assert.Equal(new Byte[] { 2, 3, 4 }, pk.ToArray());
    }
    #endregion

    #region Read<T> 结构体测试
    [Fact]
    public void ReadStructTest()
    {
        var buffer = new Byte[20];
        var writer = new SpanWriter(buffer);
        writer.Write(new TestPoint { X = 10, Y = 20 });

        var reader = new SpanReader(buffer);
        var result = reader.Read<TestPoint>();
        Assert.Equal(10, result.X);
        Assert.Equal(20, result.Y);
    }

    private struct TestPoint
    {
        public Int32 X;
        public Int32 Y;
    }
    #endregion

    #region ReadEncodedInt 边界测试
    [Fact]
    public void ReadEncodedIntZero()
    {
        var data = new Byte[] { 0x00 };
        var reader = new SpanReader(data);
        Assert.Equal(0, reader.ReadEncodedInt());
    }

    [Fact]
    public void ReadEncodedIntMaxSingleByte()
    {
        var data = new Byte[] { 0x7F };
        var reader = new SpanReader(data);
        Assert.Equal(127, reader.ReadEncodedInt());
    }

    [Fact]
    public void ReadEncodedIntTwoBytes()
    {
        // 128 = 0x80 0x01
        var data = new Byte[] { 0x80, 0x01 };
        var reader = new SpanReader(data);
        Assert.Equal(128, reader.ReadEncodedInt());
    }

    [Fact]
    public void ReadEncodedIntNegativeRoundtrip()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        writer.WriteEncodedInt(-1);

        var reader = new SpanReader(buffer);
        Assert.Equal(-1, reader.ReadEncodedInt());
    }

    [Fact]
    public void ReadEncodedIntOverflowThrows()
    {
        // 5 个连续高位字节会触发 FormatException
        var data = new Byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01 };
        var reader = new SpanReader(data);
        var threw = false;
        try { reader.ReadEncodedInt(); } catch (FormatException) { threw = true; }
        Assert.True(threw);
    }
    #endregion

    #region ReadArray 测试
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void ReadArrayRoundtrip(Int32 sizeOf)
    {
        var buffer = new Byte[64];
        var writer = new SpanWriter(buffer);
        ReadOnlySpan<Byte> data = [0xAA, 0xBB, 0xCC];
        writer.WriteArray(data, sizeOf);

        var reader = new SpanReader(buffer);
        var result = reader.ReadArray(sizeOf);
        Assert.Equal(data.ToArray(), result.ToArray());
    }

    [Fact]
    public void ReadArrayEmptyData()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        writer.WriteArray([], 2);

        var reader = new SpanReader(buffer);
        var result = reader.ReadArray(2);
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public void ReadArrayInvalidSizeOfThrows()
    {
        var reader = new SpanReader(new Byte[10]);
        var threw = false;
        try { reader.ReadArray(3); } catch (ArgumentOutOfRangeException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void ReadArrayBigEndian()
    {
        var buffer = new Byte[64];
        var writer = new SpanWriter(buffer) { IsLittleEndian = false };
        ReadOnlySpan<Byte> data = [1, 2, 3];
        writer.WriteArray(data, 2);

        var reader = new SpanReader(buffer) { IsLittleEndian = false };
        var result = reader.ReadArray(2);
        Assert.Equal(data.ToArray(), result.ToArray());
    }
    #endregion

    #region ReadLengthString 测试
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void ReadLengthStringRoundtrip(Int32 sizeOf)
    {
        var buffer = new Byte[64];
        var writer = new SpanWriter(buffer);
        writer.WriteLengthString("World", sizeOf);

        var reader = new SpanReader(buffer);
        var result = reader.ReadLengthString(sizeOf);
        Assert.Equal("World", result);
    }

    [Fact]
    public void ReadLengthStringEmpty()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        writer.WriteLengthString("", 2);

        var reader = new SpanReader(buffer);
        var result = reader.ReadLengthString(2);
        Assert.Equal(String.Empty, result);
    }

    [Fact]
    public void ReadLengthStringNull()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        writer.WriteLengthString(null, 1);

        var reader = new SpanReader(buffer);
        var result = reader.ReadLengthString(1);
        Assert.Equal(String.Empty, result);
    }

    [Fact]
    public void ReadLengthStringInvalidSizeOfThrows()
    {
        var reader = new SpanReader(new Byte[10]);
        var threw = false;
        try { reader.ReadLengthString(5); } catch (ArgumentOutOfRangeException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void ReadLengthStringWithCustomEncoding()
    {
        var buffer = new Byte[64];
        var writer = new SpanWriter(buffer);
        writer.WriteLengthString("XYZ", 2, Encoding.ASCII);

        var reader = new SpanReader(buffer);
        var result = reader.ReadLengthString(2, Encoding.ASCII);
        Assert.Equal("XYZ", result);
    }

    [Fact]
    public void ReadLengthStringChineseUtf8()
    {
        var buffer = new Byte[64];
        var writer = new SpanWriter(buffer);
        var text = "诺娃数据库";
        writer.WriteLengthString(text, 2);

        var reader = new SpanReader(buffer);
        var result = reader.ReadLengthString(2);
        Assert.Equal(text, result);
    }

    [Fact]
    public void ReadLengthStringBigEndian()
    {
        var buffer = new Byte[64];
        var writer = new SpanWriter(buffer) { IsLittleEndian = false };
        writer.WriteLengthString("Test", 2);

        var reader = new SpanReader(buffer) { IsLittleEndian = false };
        var result = reader.ReadLengthString(2);
        Assert.Equal("Test", result);
    }
    #endregion

    #region 预览方法测试
    [Fact]
    public void PeekByteTest()
    {
        var data = new Byte[] { 0xAA, 0xBB };
        var reader = new SpanReader(data);
        Assert.Equal(0xAA, reader.PeekByte());
        // 不移动位置
        Assert.Equal(0, reader.Position);
        Assert.Equal(0xAA, reader.PeekByte());
    }

    [Fact]
    public void PeekByteEmptyThrows()
    {
        var reader = new SpanReader(new Byte[1]);
        reader.Advance(1);
        var threw = false;
        try { reader.PeekByte(); } catch (InvalidOperationException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void TryPeekByteSuccess()
    {
        var data = new Byte[] { 0xCC };
        var reader = new SpanReader(data);
        Assert.True(reader.TryPeekByte(out var value));
        Assert.Equal(0xCC, value);
        Assert.Equal(0, reader.Position);
    }

    [Fact]
    public void TryPeekByteFailure()
    {
        var reader = new SpanReader(new Byte[0]);
        Assert.False(reader.TryPeekByte(out var value));
        Assert.Equal(0, value);
    }

    [Fact]
    public void PeekMultipleBytes()
    {
        var data = new Byte[] { 1, 2, 3, 4, 5 };
        var reader = new SpanReader(data);
        reader.Advance(1);
        var peeked = reader.Peek(3);
        Assert.Equal(new Byte[] { 2, 3, 4 }, peeked.ToArray());
        Assert.Equal(1, reader.Position); // 不移动
    }

    [Fact]
    public void PeekExceedsAvailableThrows()
    {
        var reader = new SpanReader(new Byte[3]);
        var threw = false;
        try { reader.Peek(4); } catch (InvalidOperationException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void TryPeekSuccess()
    {
        var data = new Byte[] { 0xAA, 0xBB, 0xCC };
        var reader = new SpanReader(data);
        Assert.True(reader.TryPeek(2, out var result));
        Assert.Equal(new Byte[] { 0xAA, 0xBB }, result.ToArray());
        Assert.Equal(0, reader.Position);
    }

    [Fact]
    public void TryPeekFailure()
    {
        var reader = new SpanReader(new Byte[2]);
        Assert.False(reader.TryPeek(3, out var result));
        Assert.Equal(0, result.Length);
    }
    #endregion

    #region EnsureSpace 无流异常测试
    [Fact]
    public void EnsureSpaceNoStreamThrows()
    {
        var data = new Byte[] { 1, 2, 3 };
        var reader = new SpanReader(data);
        reader.Advance(2);
        // 剩余 1 字节，要求 4 字节
        var threw = false;
        try { reader.EnsureSpace(4); } catch (InvalidOperationException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void EnsureSpaceZeroNoop()
    {
        var reader = new SpanReader(new Byte[3]);
        reader.EnsureSpace(0);
        reader.EnsureSpace(-1);
        Assert.Equal(0, reader.Position);
    }
    #endregion

    #region Position 设置测试
    [Fact]
    public void SetPositionTest()
    {
        var data = new Byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
        var reader = new SpanReader(data);
        reader.ReadByte(); // 0xAA
        reader.ReadByte(); // 0xBB

        // 回退到位置 1
        reader.Position = 1;
        Assert.Equal(1, reader.Position);
        Assert.Equal(0xBB, reader.ReadByte());
    }
    #endregion

    #region 浮点数独立测试
    [Fact]
    public void ReadSingleTest()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        writer.Write(3.14f);

        var reader = new SpanReader(buffer);
        Assert.Equal(3.14f, reader.ReadSingle());
    }

    [Fact]
    public void ReadDoubleTest()
    {
        var buffer = new Byte[10];
        var writer = new SpanWriter(buffer);
        writer.Write(2.718281828d);

        var reader = new SpanReader(buffer);
        Assert.Equal(2.718281828d, reader.ReadDouble());
    }
    #endregion
}
