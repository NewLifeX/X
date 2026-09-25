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
    [DisplayName("压缩编解码_帧未完整_等待不消费")]
    public void PartialFrame_Waits()
    {
        var codec = NewCodec();
        var msg = new DefaultMessage { Sequence = 3 };
        msg.SetBody(new ArrayPacket(Encoding.UTF8.GetBytes("partial-data")));
        var pk = codec.Build(msg)!;

        var full = pk.AsReadOnlySequence();
        // 只给一半：返回 null（不消费、不产出）
        var half = full.Slice(0, full.Length / 2);
        Assert.Null(codec.TryParse(half));
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

    private static DefaultMessage NewMsg(Int32 seq, String text)
    {
        var msg = new DefaultMessage { Sequence = seq };
        msg.SetBody(new ArrayPacket(Encoding.UTF8.GetBytes(text)));

        return msg;
    }
}
