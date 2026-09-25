using System.Buffers;
using System.ComponentModel;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.Messaging;
using Xunit;

namespace XUnitTest.Messaging;

/// <summary>分隔符消息编解码器（SplitDataCodec）测试</summary>
public class SplitDataCodecTests
{
    #region 工具
    private static readonly SplitDataCodec _codec = new();

    private static MessagePump NewPump() => new(new SplitDataCodec());

    private static async Task<String> ReadLineAsync(IMessage message)
    {
        var body = await message.Body!.ReadAllAsync();
        var str = Encoding.UTF8.GetString(body.AsReadOnlySequence().ToArray());
        body.TryDispose();

        return str;
    }
    #endregion

    [Fact]
    [DisplayName("分隔符编解码_单行_内容定界")]
    public void TryParse_Line()
    {
        var frame = Encoding.UTF8.GetBytes("hello\r\n");
        var rs = _codec.TryParse(new ArrayPacket(frame).AsReadOnlySequence());
        Assert.NotNull(rs);
        Assert.NotNull(rs.Value.Message);
        Assert.Equal(0, rs.Value.HeaderSize);
        Assert.Equal(5L, rs.Value.BodyLength);

        // 分隔符未出现：等待（不产生对象）
        Assert.Null(_codec.TryParse(new ArrayPacket(Encoding.UTF8.GetBytes("hel")).AsReadOnlySequence()));
    }

    [Fact]
    [DisplayName("分隔符编解码_空行_无消息帧语义")]
    public void TryParse_EmptyLine_NoMessage()
    {
        // "a\r\n\r\nb\r\n"：从 a 的帧尾分隔符开始解析（[sep][sep][b][sep]）
        var seq = new ArrayPacket(Encoding.UTF8.GetBytes("a\r\n\r\nb\r\n")).AsReadOnlySequence();

        var rs1 = _codec.TryParse(seq);
        Assert.NotNull(rs1);
        Assert.NotNull(rs1.Value.Message);
        Assert.Equal(0, rs1.Value.HeaderSize);
        Assert.Equal(1L, rs1.Value.BodyLength);

        // 第二帧：前导分隔符 + 空行分隔符 → 无消息帧（跳过 4 字节）
        var rs2 = _codec.TryParse(seq.Slice(rs1.Value.HeaderSize + rs1.Value.BodyLength));
        Assert.NotNull(rs2);
        Assert.Null(rs2.Value.Message);
        Assert.Equal(4, rs2.Value.HeaderSize);
        Assert.Equal(0L, rs2.Value.BodyLength);
    }

    [Fact]
    [DisplayName("帧泵_分隔符两行连续_逐条交付")]
    public async Task TryRead_TwoLines()
    {
        var pump = NewPump();
        using var pipe = new Pipe();

        pipe.Writer.Append(new ArrayPacket(Encoding.UTF8.GetBytes("one\r\ntwo\r\n")));

        Assert.True(pump.TryRead(pipe.Reader, out var m1));
        Assert.Equal("one", await ReadLineAsync(m1!));
        m1!.Dispose();

        Assert.True(pump.TryRead(pipe.Reader, out var m2));
        Assert.Equal("two", await ReadLineAsync(m2!));
        m2!.Dispose();

        // 帧尾分隔符无后继内容：等待（留在窗口）
        Assert.False(pump.TryRead(pipe.Reader, out _));
        Assert.Equal(2, pipe.UnconsumedLength);
    }

    [Fact]
    [DisplayName("帧泵_空行跳过_不产出空消息")]
    public async Task TryRead_EmptyLine_Skipped()
    {
        var pump = NewPump();
        using var pipe = new Pipe();

        pipe.Writer.Append(new ArrayPacket(Encoding.UTF8.GetBytes("a\r\n\r\nb\r\n")));

        Assert.True(pump.TryRead(pipe.Reader, out var m1));
        Assert.Equal("a", await ReadLineAsync(m1!));
        m1!.Dispose();

        // 空行被自动跳过，直接产出 b
        Assert.True(pump.TryRead(pipe.Reader, out var m2));
        Assert.Equal("b", await ReadLineAsync(m2!));
        m2!.Dispose();
    }

    [Fact]
    [DisplayName("帧泵_分隔符跨段_正常定界")]
    public async Task TryRead_SplitAcrossSegments()
    {
        var pump = NewPump();
        using var pipe = new Pipe();

        // \r 已到、\n 未到（分隔符跨段）
        pipe.Writer.Append(new ArrayPacket(Encoding.UTF8.GetBytes("hello\r")));
        Assert.False(pump.TryRead(pipe.Reader, out _));
        Assert.Equal(6, pipe.UnconsumedLength);

        // \n 到达：定界出 hello，随后 two 续上
        pipe.Writer.Append(new ArrayPacket(Encoding.UTF8.GetBytes("\ntwo\r\n")));
        Assert.True(pump.TryRead(pipe.Reader, out var m1));
        Assert.Equal("hello", await ReadLineAsync(m1!));
        m1!.Dispose();

        Assert.True(pump.TryRead(pipe.Reader, out var m2));
        Assert.Equal("two", await ReadLineAsync(m2!));
        m2!.Dispose();
    }

    [Fact]
    [DisplayName("分隔符编解码_整帧构建_内容加分隔符")]
    public void Build_AppendsSplit()
    {
        var msg = new Message();
        msg.SetBody(new ArrayPacket(Encoding.UTF8.GetBytes("hello")));

        var pk = _codec.Build(msg);
        Assert.NotNull(pk);
        Assert.Equal(Encoding.UTF8.GetBytes("hello\r\n"), pk!.AsReadOnlySequence().ToArray());
        Assert.NotNull(msg.Payload);     // 构建不消费消息负载
        pk.TryDispose();

        // 空消息：仅分隔符
        var pk2 = _codec.Build(new Message());
        Assert.NotNull(pk2);
        Assert.Equal(Encoding.UTF8.GetBytes("\r\n"), pk2!.AsReadOnlySequence().ToArray());
        pk2.TryDispose();

        // 头部构建不支持
        Assert.Throws<NotSupportedException>(() => _codec.BuildHeader(msg, 10));
    }
}
