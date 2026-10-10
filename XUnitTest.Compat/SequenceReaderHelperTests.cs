using System;
using System.Buffers;
using System.Buffers.Binary;
using System.ComponentModel;
using NewLife.Buffers;
using Xunit;

namespace XUnitTest.Compat;

/// <summary>无符号端序扩展（NewLife.Buffers.SequenceReaderHelper）测试，全 TFM 由 NewLife.Core 提供</summary>
public class SequenceReaderHelperTests
{
    [Fact]
    [DisplayName("TryReadBigEndian_UInt16跨段_与BinaryPrimitives一致")]
    public void UInt16_BigEndian_AcrossSegments()
    {
        var buffer = new Byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(buffer, 0xABCD);
        var reader = new SequenceReader<Byte>(SequenceFactory.Build(new[] { buffer[0] }, new[] { buffer[1] }));

        Assert.True(reader.TryReadBigEndian(out UInt16 value));
        Assert.Equal((UInt16)0xABCD, value);
        Assert.Equal(2L, reader.Consumed);
    }

    [Fact]
    [DisplayName("TryReadLittleEndian_UInt16连续段_与BinaryPrimitives一致")]
    public void UInt16_LittleEndian_Contiguous()
    {
        var buffer = new Byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, 0x1234);
        var reader = new SequenceReader<Byte>(SequenceFactory.Build(buffer));

        Assert.True(reader.TryReadLittleEndian(out UInt16 value));
        Assert.Equal((UInt16)0x1234, value);
        Assert.Equal(2L, reader.Consumed);
    }

    [Fact]
    [DisplayName("TryReadBigEndian_UInt32跨段_与BinaryPrimitives一致")]
    public void UInt32_BigEndian_AcrossSegments()
    {
        var buffer = new Byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, 0x89ABCDEF);
        var reader = new SequenceReader<Byte>(SequenceFactory.Build(new[] { buffer[0] }, new[] { buffer[1], buffer[2], buffer[3] }));

        Assert.True(reader.TryReadBigEndian(out UInt32 value));
        Assert.Equal(0x89ABCDEFu, value);
        Assert.Equal(4L, reader.Consumed);
    }

    [Fact]
    [DisplayName("TryReadLittleEndian_UInt32跨段_与BinaryPrimitives一致")]
    public void UInt32_LittleEndian_AcrossSegments()
    {
        var buffer = new Byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, 0x76543210);
        var reader = new SequenceReader<Byte>(SequenceFactory.Build(new[] { buffer[0], buffer[1] }, new[] { buffer[2], buffer[3] }));

        Assert.True(reader.TryReadLittleEndian(out UInt32 value));
        Assert.Equal(0x76543210u, value);
        Assert.Equal(4L, reader.Consumed);
    }

    [Fact]
    [DisplayName("TryReadBigEndian_UInt64跨段_与BinaryPrimitives一致")]
    public void UInt64_BigEndian_AcrossSegments()
    {
        var buffer = new Byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(buffer, 0x0102030405060708UL);
        var reader = new SequenceReader<Byte>(SequenceFactory.Build(new[] { buffer[0], buffer[1], buffer[2] }, new[] { buffer[3], buffer[4], buffer[5], buffer[6], buffer[7] }));

        Assert.True(reader.TryReadBigEndian(out UInt64 value));
        Assert.Equal(0x0102030405060708UL, value);
        Assert.Equal(8L, reader.Consumed);
    }

    [Fact]
    [DisplayName("TryReadLittleEndian_UInt64跨段_与BinaryPrimitives一致")]
    public void UInt64_LittleEndian_AcrossSegments()
    {
        var buffer = new Byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, 0x8877665544332211UL);
        var reader = new SequenceReader<Byte>(SequenceFactory.Build(new[] { buffer[0] }, new[] { buffer[1], buffer[2], buffer[3], buffer[4], buffer[5], buffer[6], buffer[7] }));

        Assert.True(reader.TryReadLittleEndian(out UInt64 value));
        Assert.Equal(0x8877665544332211UL, value);
        Assert.Equal(8L, reader.Consumed);
    }

    [Fact]
    [DisplayName("TryPeekBigEndian_UInt32跨段_不推进位置")]
    public void Peek_UInt32_BigEndian()
    {
        var buffer = new Byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, 0xDEADBEEF);
        var reader = new SequenceReader<Byte>(SequenceFactory.Build(new[] { buffer[0] }, new[] { buffer[1], buffer[2], buffer[3] }));

        Assert.True(reader.TryPeekBigEndian(out UInt32 value));
        Assert.Equal(0xDEADBEEFu, value);
        Assert.Equal(0L, reader.Consumed);
        Assert.True(reader.TryReadBigEndian(out value));
        Assert.Equal(0xDEADBEEFu, value);
    }

    [Fact]
    [DisplayName("TryPeekLittleEndian_UInt64连续段_不推进位置")]
    public void Peek_UInt64_LittleEndian()
    {
        var buffer = new Byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, 0x1122334455667788UL);
        var reader = new SequenceReader<Byte>(SequenceFactory.Build(buffer));

        Assert.True(reader.TryPeekLittleEndian(out UInt64 value));
        Assert.Equal(0x1122334455667788UL, value);
        Assert.Equal(0L, reader.Consumed);
    }

    [Fact]
    [DisplayName("TryReadLittleEndian_UInt64数据不足_返回false且不推进")]
    public void Read_Insufficient()
    {
        var reader = new SequenceReader<Byte>(SequenceFactory.Build(new Byte[] { 1, 2, 3 }));
        Assert.False(reader.TryReadLittleEndian(out UInt64 _));
        Assert.Equal(0L, reader.Consumed);
    }

    [Fact]
    [DisplayName("TryPeekBigEndian_UInt32数据不足_返回false且不推进")]
    public void Peek_Insufficient()
    {
        var reader = new SequenceReader<Byte>(SequenceFactory.Build(new Byte[] { 1, 2, 3 }));
        Assert.False(reader.TryPeekBigEndian(out UInt32 _));
        Assert.Equal(0L, reader.Consumed);
    }
}
