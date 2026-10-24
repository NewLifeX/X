using System.Buffers;
using NewLife.Buffers;
using Xunit;

namespace XUnitTest.Buffers;

/// <summary>SequenceReaderHelper 扩展方法测试：7 位压缩整数读取（对齐 SpanReader/SpanWriter 格式）</summary>
public class SequenceReaderHelperTests
{
    [Fact(DisplayName = "7位压缩：小值单字节读取并推进")]
    public void EncodedInt_SingleByte()
    {
        var reader = new SequenceReader<Byte>(new ReadOnlySequence<Byte>([0x7F]));

        Assert.True(reader.TryReadEncodedInt(out var value));
        Assert.Equal(0x7F, value);
        Assert.Equal(1, reader.Consumed);
    }

    [Fact(DisplayName = "7位压缩：多字节读取（300=AC 02）")]
    public void EncodedInt_MultiByte()
    {
        var reader = new SequenceReader<Byte>(new ReadOnlySequence<Byte>([0xAC, 0x02]));

        Assert.True(reader.TryReadEncodedInt(out var value));
        Assert.Equal(300, value);
        Assert.Equal(2, reader.Consumed);
    }

    [Fact(DisplayName = "7位压缩：32位边界 Int32.MaxValue / -1（按无符号位模式）")]
    public void EncodedInt_Boundaries()
    {
        var max = new SequenceReader<Byte>(new ReadOnlySequence<Byte>([0xFF, 0xFF, 0xFF, 0xFF, 0x07]));
        Assert.True(max.TryReadEncodedInt(out var v1));
        Assert.Equal(Int32.MaxValue, v1);

        var neg = new SequenceReader<Byte>(new ReadOnlySequence<Byte>([0xFF, 0xFF, 0xFF, 0xFF, 0x0F]));
        Assert.True(neg.TryReadEncodedInt(out var v2));
        Assert.Equal(-1, v2);
    }

    [Fact(DisplayName = "7位压缩：数据不足返回false且不推进")]
    public void EncodedInt_Insufficient_NoAdvance()
    {
        // 0x80 表示"继续"，后面没有字节
        var reader = new SequenceReader<Byte>(new ReadOnlySequence<Byte>([0x80]));

        Assert.False(reader.TryReadEncodedInt(out _));
        Assert.Equal(0, reader.Consumed);
    }

    [Fact(DisplayName = "7位压缩：超过5字节的编码返回false且不推进")]
    public void EncodedInt_TooLong_NoAdvance()
    {
        var reader = new SequenceReader<Byte>(new ReadOnlySequence<Byte>([0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F]));

        Assert.False(reader.TryReadEncodedInt(out _));
        Assert.Equal(0, reader.Consumed);
    }

    [Fact(DisplayName = "7位压缩：末字节值位溢出返回false且不推进")]
    public void EncodedInt_TerminatorOverflow_NoAdvance()
    {
        // 第 5 字节只剩高 4 位可用（28+4=32）：0x7F 的值位溢出 Int32 范围。
        // 旧实现静默丢弃高位，返回 true 并给出与编码意图无关的值
        var reader = new SequenceReader<Byte>(new ReadOnlySequence<Byte>([0xFF, 0xFF, 0xFF, 0xFF, 0x7F]));

        Assert.False(reader.TryReadEncodedInt(out _));
        Assert.Equal(0, reader.Consumed);
    }

    [Fact(DisplayName = "7位压缩64位：末字节值位溢出返回false且不推进")]
    public void EncodedInt64_TerminatorOverflow_NoAdvance()
    {
        // 第 10 字节只剩最高位可用（63+1=64）
        var reader = new SequenceReader<Byte>(new ReadOnlySequence<Byte>([0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F]));

        Assert.False(reader.TryReadEncodedInt64(out _));
        Assert.Equal(0, reader.Consumed);
    }

    [Fact(DisplayName = "7位压缩64位：Int64.MaxValue（9字节）与 -1")]
    public void EncodedInt64_Boundaries()
    {
        var max = new SequenceReader<Byte>(new ReadOnlySequence<Byte>([0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F]));
        Assert.True(max.TryReadEncodedInt64(out var v1));
        Assert.Equal(Int64.MaxValue, v1);

        var neg = new SequenceReader<Byte>(new ReadOnlySequence<Byte>([0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01]));
        Assert.True(neg.TryReadEncodedInt64(out var v2));
        Assert.Equal(-1L, v2);
    }

    [Fact(DisplayName = "7位压缩：跨段读取")]
    public void EncodedInt_AcrossSegments()
    {
        var first = new Segment(new Byte[] { 0xAC });
        var last = first.Append(new Byte[] { 0x02 });
        var seq = new ReadOnlySequence<Byte>(first, 0, last, last.Memory.Length);

        var reader = new SequenceReader<Byte>(seq);
        Assert.True(reader.TryReadEncodedInt(out var value));
        Assert.Equal(300, value);
    }

    /// <summary>最小跨段实现，用于构造多段只读序列</summary>
    private sealed class Segment : ReadOnlySequenceSegment<Byte>
    {
        public Segment(ReadOnlyMemory<Byte> memory) => Memory = memory;

        public Segment Append(ReadOnlyMemory<Byte> memory)
        {
            var segment = new Segment(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = segment;
            return segment;
        }
    }

    [Fact(DisplayName = "7位压缩：与 SpanWriter 编码往返一致")]
    public void EncodedInt_RoundTripWithSpanWriter()
    {
        var values = new[] { 0, 1, 127, 128, 300, 16384, 1_000_000, Int32.MaxValue, -1, Int32.MinValue };
        foreach (var v in values)
        {
            var writer = new SpanWriter(new Byte[16]);
            writer.WriteEncodedInt(v);

            var buffer = new ReadOnlySequence<Byte>(writer.WrittenSpan.ToArray());
            var reader = new SequenceReader<Byte>(buffer);
            Assert.True(reader.TryReadEncodedInt(out var value));
            Assert.Equal(v, value);
        }
    }

    [Fact(DisplayName = "7位压缩64位：与 SpanWriter 编码往返一致")]
    public void EncodedInt64_RoundTripWithSpanWriter()
    {
        var values = new[] { 0L, 127, 128, 300, Int64.MaxValue, -1L, Int64.MinValue };
        foreach (var v in values)
        {
            var writer = new SpanWriter(new Byte[16]);
            writer.WriteEncodedInt64(v);

            var buffer = new ReadOnlySequence<Byte>(writer.WrittenSpan.ToArray());
            var reader = new SequenceReader<Byte>(buffer);
            Assert.True(reader.TryReadEncodedInt64(out var value));
            Assert.Equal(v, value);
        }
    }
}
