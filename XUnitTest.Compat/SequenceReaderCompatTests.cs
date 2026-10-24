using System;
using System.Buffers;
using System.Buffers.Binary;
using System.ComponentModel;
using Xunit;

namespace XUnitTest.Compat;

/// <summary>SequenceReader 兼容性测试：net462 走 NewLife.Core 垫片、net8.0 走 BCL，同一套用例校验行为一致</summary>
public class SequenceReaderCompatTests
{
    [Fact]
    [DisplayName("TryRead_跨段顺序读取_值正确且推进完整")]
    public void TryRead_Across()
    {
        var seq = SequenceFactory.Build(new Byte[] { 1, 2 }, new Byte[] { 3 }, new Byte[] { 4, 5, 6 });
        var reader = new SequenceReader<Byte>(seq);
        for (var i = 1; i <= 6; i++)
        {
            Assert.True(reader.TryRead(out var b));
            Assert.Equal((Byte)i, b);
        }

        Assert.False(reader.TryRead(out _));
        Assert.True(reader.End);
        Assert.Equal(6L, reader.Consumed);
        Assert.Equal(0L, reader.Remaining);
    }

    [Fact]
    [DisplayName("TryPeek_段边界_不推进位置")]
    public void TryPeek_Boundary()
    {
        var seq = SequenceFactory.Build(new Byte[] { 1, 2 }, new Byte[] { 3 });
        var reader = new SequenceReader<Byte>(seq);
        Assert.True(reader.TryPeek(out var b));
        Assert.Equal((Byte)1, b);

        reader.Advance(2);
        Assert.Equal(2L, reader.Consumed);
        Assert.True(reader.TryPeek(out b));
        Assert.Equal((Byte)3, b);
    }

    [Fact]
    [DisplayName("Rewind_跨段回退_重读一致")]
    public void Rewind_Across()
    {
        var seq = SequenceFactory.Build(new Byte[] { 1, 2 }, new Byte[] { 3, 4 }, new Byte[] { 5 });
        var reader = new SequenceReader<Byte>(seq);
        reader.Advance(4);
        Assert.Equal(4L, reader.Consumed);

        reader.Rewind(3);
        Assert.Equal(1L, reader.Consumed);
        Assert.True(reader.TryRead(out var b));
        Assert.Equal((Byte)2, b);
    }

    [Fact]
    [DisplayName("TryCopyTo_跨段拷贝成功_数据不足失败_均不推进")]
    public void TryCopyTo_Across()
    {
        var seq = SequenceFactory.Build(new Byte[] { 1, 2 }, new Byte[] { 3, 4, 5 });
        var reader = new SequenceReader<Byte>(seq);
        reader.Advance(1);

        Span<Byte> dest = stackalloc Byte[4];
        Assert.True(reader.TryCopyTo(dest));
        Assert.Equal(new Byte[] { 2, 3, 4, 5 }, dest.ToArray());
        Assert.Equal(1L, reader.Consumed);

        Span<Byte> tooBig = stackalloc Byte[5];
        Assert.False(reader.TryCopyTo(tooBig));
        Assert.Equal(1L, reader.Consumed);
    }

    [Fact]
    [DisplayName("TryReadLittleEndian_跨段边界_与BinaryPrimitives一致")]
    public void LittleEndian_Boundary()
    {
        var value = 0x12345678;
        var bytes = new Byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        var seq = SequenceFactory.Build(new Byte[] { 0xAA, bytes[0] }, new Byte[] { bytes[1], bytes[2], bytes[3] });
        var reader = new SequenceReader<Byte>(seq);

        Assert.True(reader.TryRead(out var head));
        Assert.Equal(0xAA, head);
        Assert.True(reader.TryReadLittleEndian(out Int32 actual));
        Assert.Equal(value, actual);
        Assert.Equal(5L, reader.Consumed);
    }

    [Fact]
    [DisplayName("TryReadBigEndian_Int16与Int64跨段_与BinaryPrimitives一致")]
    public void BigEndian_Boundary()
    {
        var i16 = 0x1234;
        var i64 = 0x0102030405060708L;
        var b16 = new Byte[2];
        var b64 = new Byte[8];
        BinaryPrimitives.WriteInt16BigEndian(b16, (Int16)i16);
        BinaryPrimitives.WriteInt64BigEndian(b64, i64);

        var seq = SequenceFactory.Build(
            new Byte[] { b16[0] },
            new Byte[] { b16[1], b64[0], b64[1] },
            new Byte[] { b64[2], b64[3], b64[4], b64[5], b64[6], b64[7] });
        var reader = new SequenceReader<Byte>(seq);

        Assert.True(reader.TryReadBigEndian(out Int16 v16));
        Assert.Equal((Int16)i16, v16);
        Assert.True(reader.TryReadBigEndian(out Int64 v64));
        Assert.Equal(i64, v64);
        Assert.Equal(10L, reader.Consumed);
    }

    [Fact]
    [DisplayName("TryReadLittleEndian_数据不足_返回false且不推进")]
    public void LittleEndian_Insufficient()
    {
        var seq = SequenceFactory.Build(new Byte[] { 1, 2, 3 });
        var reader = new SequenceReader<Byte>(seq);
        reader.Advance(2);

        Assert.False(reader.TryReadLittleEndian(out Int32 _));
        Assert.Equal(2L, reader.Consumed);
    }

    [Fact]
    [DisplayName("TryAdvanceTo_跨段查找分隔符_前进到分隔符之后")]
    public void TryAdvanceTo_Found()
    {
        var seq = SequenceFactory.Build(new Byte[] { 1, 2, (Byte)'#' }, new Byte[] { 9, 9 });
        var reader = new SequenceReader<Byte>(seq);
        Assert.True(reader.TryAdvanceTo((Byte)'#', true));
        Assert.Equal(3L, reader.Consumed);
        Assert.True(reader.TryRead(out var b));
        Assert.Equal((Byte)9, b);
    }

    [Fact]
    [DisplayName("TryAdvanceTo_未找到_不改变读取位置")]
    public void TryAdvanceTo_NotFound()
    {
        var seq = SequenceFactory.Build(new Byte[] { 1, 2 }, new Byte[] { 3 });
        var reader = new SequenceReader<Byte>(seq);
        Assert.False(reader.TryAdvanceTo((Byte)'#', true));
        Assert.Equal(0L, reader.Consumed);
    }

    [Fact]
    [DisplayName("TryReadTo_跨段读取到分隔符_返回切片且正确推进")]
    public void TryReadTo_Delimiter()
    {
        var seq = SequenceFactory.Build(new Byte[] { 1, 2 }, new Byte[] { 3, (Byte)';', 4 });
        var reader = new SequenceReader<Byte>(seq);
        Assert.True(reader.TryReadTo(out ReadOnlySequence<Byte> line, (Byte)';', true));
        Assert.Equal(3L, line.Length);
        Assert.Equal(new Byte[] { 1, 2, 3 }, line.ToArray());
        Assert.True(reader.TryRead(out var next));
        Assert.Equal((Byte)4, next);
    }

    [Fact]
    [DisplayName("AdvancePast_跨段连续相同值_返回推进数量")]
    public void AdvancePast_Across()
    {
        var seq = SequenceFactory.Build(new Byte[] { 0, 0 }, new Byte[] { 0, 5 });
        var reader = new SequenceReader<Byte>(seq);
        Assert.Equal(3L, reader.AdvancePast((Byte)0));
        Assert.True(reader.TryRead(out var b));
        Assert.Equal((Byte)5, b);
    }

    [Fact]
    [DisplayName("AdvancePastAny_跨段跳过两种空白字符")]
    public void AdvancePastAny_Across()
    {
        var seq = SequenceFactory.Build(new Byte[] { 0x20 }, new Byte[] { 0x09, 0x20, (Byte)'x' });
        var reader = new SequenceReader<Byte>(seq);
        Assert.Equal(3L, reader.AdvancePastAny((Byte)0x20, (Byte)0x09));
        Assert.True(reader.TryRead(out var b));
        Assert.Equal((Byte)'x', b);
    }

    [Fact]
    [DisplayName("IsNext_跨段比对_可选推进")]
    public void IsNext_Across()
    {
        var seq = SequenceFactory.Build(new Byte[] { 1 }, new Byte[] { 2, 3 });
        var reader = new SequenceReader<Byte>(seq);
        Assert.False(reader.IsNext(new Byte[] { 1, 9 }, false));
        Assert.True(reader.IsNext(new Byte[] { 1, 2 }, true));
        Assert.Equal(2L, reader.Consumed);
        Assert.True(reader.TryRead(out var b));
        Assert.Equal((Byte)3, b);
    }

    [Fact]
    [DisplayName("Rewind_超过已消费数量_抛ArgumentOutOfRangeException")]
    public void Rewind_Overflow_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var reader = new SequenceReader<Byte>(SequenceFactory.Build(new Byte[] { 1, 2 }));
            reader.Advance(1);
            reader.Rewind(2);
        });
    }

    [Fact]
    [DisplayName("Advance_超出剩余数据_抛ArgumentOutOfRangeException")]
    public void Advance_Overflow_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var reader = new SequenceReader<Byte>(SequenceFactory.Build(new Byte[] { 1, 2 }));
            reader.Advance(3);
        });
    }

    [Fact]
    [DisplayName("空序列_End为真且读取失败")]
    public void Empty_Sequence()
    {
        var reader = new SequenceReader<Byte>(ReadOnlySequence<Byte>.Empty);
        Assert.True(reader.End);
        Assert.False(reader.TryRead(out _));
        Assert.Equal(0L, reader.Length);
        Assert.Equal(0L, reader.Remaining);
    }

    [Fact]
    [DisplayName("首段为空的多段链_构造后直接定位到有效数据")]
    public void Leading_Empty_Segment()
    {
        var seq = SequenceFactory.Build(Array.Empty<Byte>(), new Byte[] { 7, 8 });
        var reader = new SequenceReader<Byte>(seq);
        Assert.False(reader.End);
        Assert.True(reader.TryRead(out var b));
        Assert.Equal((Byte)7, b);
        Assert.Equal(1L, reader.Consumed);
    }

    [Fact]
    [DisplayName("Position_跨段推进_与GetPosition结果一致")]
    public void Position_Across()
    {
        var seq = SequenceFactory.Build(new Byte[] { 1, 2 }, new Byte[] { 3, 4 });
        var reader = new SequenceReader<Byte>(seq);
        reader.Advance(3);
        Assert.True(reader.Position.Equals(seq.GetPosition(3)));
        Assert.Equal(3L, reader.Consumed);
    }

    [Fact]
    [DisplayName("UnreadSpan_恰好读完一段_指向下一段")]
    public void UnreadSpan_Next()
    {
        var seq = SequenceFactory.Build(new Byte[] { 1, 2 }, new Byte[] { 3, 4, 5 });
        var reader = new SequenceReader<Byte>(seq);
        reader.Advance(2);
        var span = reader.UnreadSpan;
        Assert.Equal(3, span.Length);
        Assert.Equal((Byte)3, span[0]);
    }

    [Fact]
    [DisplayName("TryRead_64个单字节段_连续读取完整")]
    public void Many_Segments()
    {
        var segments = new Byte[64][];
        for (var i = 0; i < 64; i++)
            segments[i] = new[] { (Byte)i };

        var reader = new SequenceReader<Byte>(SequenceFactory.Build(segments));
        for (var i = 0; i < 64; i++)
        {
            Assert.True(reader.TryRead(out var b));
            Assert.Equal((Byte)i, b);
        }

        Assert.True(reader.End);
        Assert.Equal(64L, reader.Consumed);
    }

    [Fact]
    [DisplayName("TryReadToAny_跨段任一分隔符_返回切片")]
    public void TryReadToAny_Across()
    {
        var seq = SequenceFactory.Build(new Byte[] { 1 }, new Byte[] { 2, (Byte)';', 3 });
        var reader = new SequenceReader<Byte>(seq);
        Assert.True(reader.TryReadToAny(out ReadOnlySequence<Byte> line, new Byte[] { (Byte)',', (Byte)';' }, true));
        Assert.Equal(new Byte[] { 1, 2 }, line.ToArray());
        Assert.Equal(3L, reader.Consumed);
        Assert.True(reader.TryRead(out var next));
        Assert.Equal((Byte)3, next);
    }

    [Fact]
    [DisplayName("TryAdvanceToAny_跨段查找并跳过分隔符")]
    public void TryAdvanceToAny_Across()
    {
        var seq = SequenceFactory.Build(new Byte[] { 1, (Byte)',' }, new Byte[] { 9 });
        var reader = new SequenceReader<Byte>(seq);
        Assert.True(reader.TryAdvanceToAny(new Byte[] { (Byte)';', (Byte)',' }, true));
        Assert.Equal(2L, reader.Consumed);
    }

    [Fact]
    [DisplayName("AdvancePastAny_三参数重载_跳过分隔符")]
    public void AdvancePastAny_Overloads()
    {
        var seq = SequenceFactory.Build(new Byte[] { 0x20, 0x09 }, new Byte[] { 0x0D, (Byte)'x' });
        var reader = new SequenceReader<Byte>(seq);
        Assert.Equal(3L, reader.AdvancePastAny((Byte)0x20, (Byte)0x09, (Byte)0x0D));
        Assert.True(reader.TryRead(out var b));
        Assert.Equal((Byte)'x', b);
    }

    [Fact]
    [DisplayName("TryReadTo_转义分隔符_跳过被转义项")]
    public void TryReadTo_Escape()
    {
        // a % ; b ; z：第二个 ';' 被 '%' 转义跳过，读到末尾第一个未转义 ';'
        var seq = SequenceFactory.Build(new Byte[] { (Byte)'a', (Byte)'%' }, new Byte[] { (Byte)';', (Byte)'b', (Byte)';', (Byte)'z' });
        var reader = new SequenceReader<Byte>(seq);
        Assert.True(reader.TryReadTo(out ReadOnlySequence<Byte> line, (Byte)';', (Byte)'%', true));
        Assert.Equal(new Byte[] { (Byte)'a', (Byte)'%', (Byte)';', (Byte)'b' }, line.ToArray());
        Assert.True(reader.TryRead(out var next));
        Assert.Equal((Byte)'z', next);
    }

    [Fact]
    [DisplayName("TryReadTo_多字节分隔符跨段_匹配完整分隔符")]
    public void TryReadTo_MultiByteDelimiter()
    {
        var seq = SequenceFactory.Build(new Byte[] { 1, 2, 3 }, new Byte[] { (Byte)'\r', (Byte)'\n', 9 });
        var reader = new SequenceReader<Byte>(seq);
        Assert.True(reader.TryReadTo(out ReadOnlySequence<Byte> line, new Byte[] { (Byte)'\r', (Byte)'\n' }, true));
        Assert.Equal(new Byte[] { 1, 2, 3 }, line.ToArray());
        Assert.True(reader.TryRead(out var next));
        Assert.Equal((Byte)9, next);
    }
}
