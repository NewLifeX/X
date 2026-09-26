using System.Buffers;
using System.ComponentModel;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.Messaging;
using Xunit;

namespace XUnitTest.Messaging;

/// <summary>压缩消息编解码器测试</summary>
[DisplayName("压缩编解码器")]
public class CompressedCodecTests
{
    private static CompressedCodec NewCodec() => new(new SrmpCodec());

    [Fact]
    [DisplayName("压缩编解码_装饰器链_暴露内层编解码")]
    public void Decorator_ExposesInner()
    {
        var inner = new SrmpCodec();
        var codec = new CompressedCodec(inner);

        // 会话层靠这个属性递归解包，找到真正的配对器
        Assert.Same(inner, ((IMessageCodecDecorator)codec).Inner);
    }

    [Fact]
    [DisplayName("压缩编解码_嵌套装饰_逐层解包到内层")]
    public void Decorator_Nested_UnwrapsToInner()
    {
        var srmp = new SrmpCodec();
        var outer = new CompressedCodec(new CompressedCodec(srmp));

        var mid = ((IMessageCodecDecorator)outer).Inner;
        Assert.IsType<CompressedCodec>(mid);

        // 第二层仍可继续解包，递归到此为止
        Assert.Same(srmp, ((IMessageCodecDecorator)mid!).Inner);
    }

    [Fact]
    [DisplayName("压缩编解码_构建_不改动消息负载")]
    public void Build_KeepsMessagePayload()
    {
        var codec = NewCodec();
        var msg = new DefaultMessage { Sequence = 9 };

        using var owner = new OwnerPacket(32);
        var expect = new Byte[32];
        for (var i = 0; i < expect.Length; i++) expect[i] = (Byte)(i + 1);
        expect.CopyTo(owner.GetSpan());
        msg.SetBody(owner);

        var origin = msg.Payload;
        using var pk = codec.Build(msg);
        Assert.NotNull(pk);

        // 构建前后负载必须是同一个句柄且内容不变：构建不得归还调用方还在用的负载
        Assert.Same(origin, msg.Payload);
        Assert.Equal(expect, msg.Payload!.ToArray());
    }

    [Fact]
    [DisplayName("压缩编解码_往返_体完整还原")]
    public void RoundTrip()
    {
        var codec = NewCodec();
        var payload = Encoding.UTF8.GetBytes("hello compressed world");

        var msg = new DefaultMessage { Sequence = 7 };
        msg.SetBody(new ArrayPacket(payload));
        var pk = codec.Build(msg);
        Assert.NotNull(pk);

        var rs = codec.TryParse(pk!.AsReadOnlySequence());
        Assert.NotNull(rs);
        var msg2 = (DefaultMessage)rs.Value.Message!;
        Assert.Equal(7, msg2.Sequence);

        // 预绑定体：解压后直接可用（内存模式）
        Assert.False(msg2.Body!.IsStreaming);
        Assert.Equal(payload, msg2.Payload!.ToArray());
        msg2.Dispose();
        pk.TryDispose();
    }

    [Fact]
    [DisplayName("压缩编解码_重复数据_压缩率显著")]
    public void CompressionRatio()
    {
        var codec = NewCodec();
        var payload = new Byte[4096];
        for (var i = 0; i < payload.Length; i++) payload[i] = (Byte)('A' + i % 4);

        var msg = new DefaultMessage { Sequence = 1 };
        msg.SetBody(new ArrayPacket(payload));
        var pk = codec.Build(msg);

        // 整帧 = 4 字节 SRMP 头 + 压缩体；重复数据压缩后应远小于原始
        var frame = pk!.AsReadOnlySequence().ToArray();
        Assert.True(frame.Length < payload.Length / 4, $"压缩后 {frame.Length} 字节，未达预期");

        pk.TryDispose();
    }

    [Fact]
    [DisplayName("压缩编解码_空体_直通不压缩")]
    public void EmptyBody_PassThrough()
    {
        var codec = NewCodec();
        var msg = new DefaultMessage { Sequence = 2 };
        var pk = codec.Build(msg);
        Assert.NotNull(pk);
        Assert.Equal(4, pk!.Total);   // 仅 SRMP 4 字节头

        var rs = codec.TryParse(pk.AsReadOnlySequence());
        Assert.NotNull(rs);
        Assert.Equal(2, ((DefaultMessage)rs.Value.Message!).Sequence);
        pk.TryDispose();
    }

    [Fact]
    [DisplayName("压缩编解码_帧未完整_声明需整帧不消费")]
    public void PartialFrame_Waits()
    {
        var codec = NewCodec();
        var msg = new DefaultMessage { Sequence = 3 };
        msg.SetBody(new ArrayPacket(Encoding.UTF8.GetBytes("partial-data")));
        var pk = codec.Build(msg)!;

        var full = pk.AsReadOnlySequence();
        // 只给一半：帧已定界但需整帧，声明 NeedFullFrame 等补齐（不消费、不产出消息，
        // 也不能当成“无法定界的残余”，否则帧泵会误断连接）
        var half = full.Slice(0, full.Length / 2);
        var rs = codec.TryParse(half);
        Assert.NotNull(rs);
        Assert.True(rs.Value.NeedFullFrame);
        Assert.Null(rs.Value.Message);
        Assert.Equal(full.Length, rs.Value.HeaderSize + rs.Value.BodyLength);
        pk.TryDispose();
    }

    [Fact]
    [DisplayName("压缩编解码_流式构建_拒绝")]
    public void BuildHeader_Throws()
    {
        var codec = NewCodec();
        Assert.Throws<NotSupportedException>(() => codec.BuildHeader(new DefaultMessage(), 100));
    }

    [Fact]
    [DisplayName("压缩编解码_帧泵集成_解压交付与下一帧对齐")]
    public async Task Pump_Integration()
    {
        var codec = NewCodec();
        var pump = new MessagePump(codec);
        using var pipe = new Pipe();

        // 两帧连续写入
        var p1 = codec.Build(NewMsg(0x11, "first-frame"))!;
        var p2 = codec.Build(NewMsg(0x12, "second-frame"))!;
        pipe.Writer.Append(p1);
        pipe.Writer.Append(p2);

        for (var i = 0; i < 2; i++)
        {
            Assert.True(pump.TryRead(pipe.Reader, out var m));
            Assert.NotNull(m);
            var body = await m!.Body!.ReadAllAsync();
            var text = Encoding.UTF8.GetString(body.AsReadOnlySequence().ToArray());
            Assert.Equal(i == 0 ? "first-frame" : "second-frame", text);
            Assert.Equal(i == 0 ? 0x11 : 0x12, ((DefaultMessage)m).Sequence);
            body.TryDispose();
            m.Dispose();
        }

        Assert.Equal(0, pipe.UnconsumedLength);
        p1.TryDispose();
        p2.TryDispose();
    }

    [Fact]
    [DisplayName("压缩编解码_跨段序列_解压还原")]
    public void CrossSegment_Decompress()
    {
        var codec = NewCodec();
        var payload = Encoding.UTF8.GetBytes("hello compressed world");

        var pk = codec.Build(NewMsg(5, "hello compressed world"))!;
        var frame = pk.AsReadOnlySequence().ToArray();
        pk.TryDispose();

        // 在体首字节处拆成两段：压缩体跨段，解压须先拼出完整压缩数据
        var first = new Segment(frame.AsMemory(0, 5));
        var last = first.Append(frame.AsMemory(5));
        var sequence = new ReadOnlySequence<Byte>(first, 0, last, frame.Length - 5);

        var rs = codec.TryParse(sequence);
        Assert.NotNull(rs);
        var msg2 = (DefaultMessage)rs.Value.Message!;
        Assert.Equal(5, msg2.Sequence);
        Assert.Equal(payload, msg2.Payload!.ToArray());
        msg2.Dispose();
    }

    [Fact]
    [DisplayName("压缩编解码_链式负载_压缩还原")]
    public void ChainedPayload_RoundTrip()
    {
        var codec = NewCodec();

        // 负载由两段链式挂接，压缩时须完整读出；构建不得改动原句柄
        var body = new ArrayPacket(Encoding.UTF8.GetBytes("chained-")) { Next = new ArrayPacket(Encoding.UTF8.GetBytes("payload")) };
        var msg = new DefaultMessage { Sequence = 6 };
        msg.SetBody(body);

        var pk = codec.Build(msg)!;

        // 构建不得改动原负载（ArrayPacket 为值类型，按内容比对）
        Assert.Equal("chained-payload", Encoding.UTF8.GetString(msg.Payload!.ToArray()));

        var rs = codec.TryParse(pk.AsReadOnlySequence());
        Assert.NotNull(rs);
        var msg2 = (DefaultMessage)rs.Value.Message!;
        Assert.Equal("chained-payload", Encoding.UTF8.GetString(msg2.Payload!.ToArray()));
        msg2.Dispose();
        pk.TryDispose();
    }

    [Fact]
    [DisplayName("压缩编解码_帧泵_大帧分批到达_不受最大缓存误报")]
    public async Task Pump_LargeFrame_InChunks_NoMaxCacheFalsePositive()
    {
        var codec = NewCodec();
        var pump = new MessagePump(codec) { MaxCache = 512 };    // 远小于帧长
        using var pipe = new Pipe();

        // 随机负载：压缩后仍显著大于 MaxCache
        var payload = new Byte[64 * 1024];
        new Random(2026).NextBytes(payload);

        var msg = new DefaultMessage { Sequence = 0x21 };
        msg.SetBody(new ArrayPacket(payload));
        var pk = codec.Build(msg)!;
        var frame = pk.AsReadOnlySequence().ToArray();
        Assert.True(frame.Length > 512);

        // 先写入不足整帧的一大段（已超过 MaxCache）：已定界但未完整，帧泵应等待而不误报残余超限
        pipe.Writer.Append(new ArrayPacket(frame[..(frame.Length - 1)]));

        var task = pump.ReadAsync(pipe.Reader).AsTask();
        Assert.False(task.IsCompleted);

        // 补上最后一字节：整帧到齐，解压交付
        pipe.Writer.Append(new ArrayPacket(frame[(frame.Length - 1)..]));

        var recv = await task;
        Assert.NotNull(recv);
        Assert.Equal(0x21, ((DefaultMessage)recv!).Sequence);
        Assert.Equal(payload, recv.Payload!.ToArray());
        recv.Dispose();
        pk.TryDispose();
    }

    private static DefaultMessage NewMsg(Int32 seq, String text)
    {
        var msg = new DefaultMessage { Sequence = seq };
        msg.SetBody(new ArrayPacket(Encoding.UTF8.GetBytes(text)));

        return msg;
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
}
