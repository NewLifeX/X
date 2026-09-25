using System.Buffers;
using System.ComponentModel;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.Messaging;
using Xunit;

namespace XUnitTest.Messaging;

/// <summary>长度字段消息编解码器（LengthFieldCodec）测试</summary>
public class LengthFieldCodecTests
{
    #region 工具
    private static async Task<Byte[]> ReadBodyAsync(IMessage message)
    {
        var body = await message.Body!.ReadAllAsync();
        var data = body.AsReadOnlySequence().ToArray();
        body.TryDispose();

        return data;
    }
    #endregion

    [Fact]
    [DisplayName("长度字段编解码_2字节小端_定界")]
    public void TryParse_Size2_LittleEndian()
    {
        var codec = new LengthFieldCodec();
        var frame = new Byte[] { 0x05, 0x00, (Byte)'h', (Byte)'e', (Byte)'l', (Byte)'l', (Byte)'o' };

        var rs = codec.TryParse(new ArrayPacket(frame).AsReadOnlySequence());
        Assert.NotNull(rs);
        Assert.NotNull(rs.Value.Message);
        Assert.Equal(2, rs.Value.HeaderSize);
        Assert.Equal(5L, rs.Value.BodyLength);

        // 头部不足：等待（不产生对象）
        Assert.Null(codec.TryParse(new ArrayPacket(new Byte[] { 0x05 }).AsReadOnlySequence()));
    }

    [Fact]
    [DisplayName("长度字段编解码_MQTT风格_头部偏移跳过")]
    public void TryParse_Offset2()
    {
        var codec = new LengthFieldCodec { Offset = 2 };
        var frame = new Byte[] { 0xA0, 0x01, 0x03, 0x00, (Byte)'a', (Byte)'b', (Byte)'c' };

        var rs = codec.TryParse(new ArrayPacket(frame).AsReadOnlySequence());
        Assert.NotNull(rs);
        Assert.NotNull(rs.Value.Message);
        Assert.Equal(4, rs.Value.HeaderSize);
        Assert.Equal(3L, rs.Value.BodyLength);

        // 仅有固定头部无长度字段：等待
        Assert.Null(codec.TryParse(new ArrayPacket(new Byte[] { 0xA0, 0x01 }).AsReadOnlySequence()));
    }

    [Fact]
    [DisplayName("长度字段编解码_大端与变长_定界")]
    public void TryParse_BigEndian_Varint()
    {
        // 大端 2 字节：0x00 0x05
        var codecBe = new LengthFieldCodec { Size = -2 };
        var rs1 = codecBe.TryParse(new ArrayPacket(new Byte[] { 0x00, 0x05 }).AsReadOnlySequence());
        Assert.NotNull(rs1);
        Assert.NotNull(rs1.Value.Message);
        Assert.Equal(2, rs1.Value.HeaderSize);
        Assert.Equal(5L, rs1.Value.BodyLength);

        var codecVar = new LengthFieldCodec { Size = 0 };

        // 变长：127 → 1 字节
        var rs2 = codecVar.TryParse(new ArrayPacket(new Byte[] { 0x7F }).AsReadOnlySequence());
        Assert.NotNull(rs2);
        Assert.NotNull(rs2.Value.Message);
        Assert.Equal(1, rs2.Value.HeaderSize);
        Assert.Equal(127L, rs2.Value.BodyLength);

        // 变长：128 → [0x80, 0x01] 两字节
        var rs3 = codecVar.TryParse(new ArrayPacket(new Byte[] { 0x80, 0x01 }).AsReadOnlySequence());
        Assert.NotNull(rs3);
        Assert.NotNull(rs3.Value.Message);
        Assert.Equal(2, rs3.Value.HeaderSize);
        Assert.Equal(128L, rs3.Value.BodyLength);

        // 变长未终止：等待
        Assert.Null(codecVar.TryParse(new ArrayPacket(new Byte[] { 0x80 }).AsReadOnlySequence()));
    }

    [Fact]
    [DisplayName("长度字段编解码_非法长度_返回失败")]
    public void TryParse_Invalid()
    {
        // 4 字节长度为负（最高位 1）：损坏帧
        var codec = new LengthFieldCodec { Size = 4 };
        Assert.Null(codec.TryParse(new ArrayPacket(new Byte[] { 0xFF, 0xFF, 0xFF, 0xFF }).AsReadOnlySequence()));

        // 变长编码超过 32 位（5 字节仍未终止）：非法
        var codecVar = new LengthFieldCodec { Size = 0 };
        Assert.Null(codecVar.TryParse(new ArrayPacket(new Byte[] { 0x80, 0x80, 0x80, 0x80, 0x80 }).AsReadOnlySequence()));
    }

    [Fact]
    [DisplayName("长度字段编解码_构建与解析_整帧往返")]
    public async Task Build_RoundTrip()
    {
        var codec = new LengthFieldCodec();
        var msg = new Message();
        msg.SetBody(new ArrayPacket(Encoding.UTF8.GetBytes("hello")));

        var pk = codec.Build(msg);
        Assert.NotNull(pk);
        Assert.NotNull(msg.Payload);     // 构建不消费消息负载

        // 帧字节：[0x05, 0x00][hello]
        Assert.Equal(new Byte[] { 0x05, 0x00, (Byte)'h', (Byte)'e', (Byte)'l', (Byte)'l', (Byte)'o' }, pk!.AsReadOnlySequence().ToArray());

        // 帧泵解析 + 读体
        var pump = new MessagePump(codec);
        using var pipe = new Pipe();
        pipe.Writer.Append(pk);

        Assert.True(pump.TryRead(pipe.Reader, out var m2));
        Assert.Equal("hello", Encoding.UTF8.GetString(await ReadBodyAsync(m2!)));
        m2!.Dispose();
    }

    [Fact]
    [DisplayName("长度字段编解码_构建_头部偏移零填充")]
    public void Build_Offset2_FillZero()
    {
        var codec = new LengthFieldCodec { Offset = 2 };
        var msg = new Message();
        msg.SetBody(new ArrayPacket(Encoding.UTF8.GetBytes("abc")));

        var pk = codec.Build(msg);
        Assert.NotNull(pk);
        Assert.Equal(new Byte[] { 0x00, 0x00, 0x03, 0x00, (Byte)'a', (Byte)'b', (Byte)'c' }, pk!.AsReadOnlySequence().ToArray());
        pk.TryDispose();
    }

    [Fact]
    [DisplayName("长度字段编解码_大端与变长_构建")]
    public void Build_BigEndian_Varint()
    {
        // 大端 2 字节：长度 5 → [0x00, 0x05]
        var codecBe = new LengthFieldCodec { Size = -2 };
        var msg = new Message();
        msg.SetBody(new ArrayPacket(Encoding.UTF8.GetBytes("hello")));
        var pk = codecBe.Build(msg);
        Assert.NotNull(pk);
        Assert.Equal(new Byte[] { 0x00, 0x05, (Byte)'h', (Byte)'e', (Byte)'l', (Byte)'l', (Byte)'o' }, pk!.AsReadOnlySequence().ToArray());
        pk.TryDispose();

        // 变长：128 字节负载 → 头 [0x80, 0x01]
        var codecVar = new LengthFieldCodec { Size = 0 };
        var msg2 = new Message();
        msg2.SetBody(new ArrayPacket(new Byte[128]));
        var pk2 = codecVar.Build(msg2);
        Assert.NotNull(pk2);
        var data = pk2!.AsReadOnlySequence().ToArray();
        Assert.Equal(130, data.Length);
        Assert.Equal(0x80, data[0]);
        Assert.Equal(0x01, data[1]);
        pk2.TryDispose();
    }

    [Fact]
    [DisplayName("长度字段编解码_容量超限_构建抛出")]
    public void Build_Overflow_Throws()
    {
        var codec = new LengthFieldCodec { Size = 1 };
        var msg = new Message();
        msg.SetBody(new ArrayPacket(new Byte[300]));

        Assert.Throws<ArgumentOutOfRangeException>(() => codec.Build(msg));
    }

    [Fact]
    [DisplayName("长度字段编解码_头部构建_声明长度")]
    public void BuildHeader_DeclaresLength()
    {
        var codec = new LengthFieldCodec { Offset = 2 };
        var pk = codec.BuildHeader(new Message(), 100);
        Assert.Equal(new Byte[] { 0x00, 0x00, 0x64, 0x00 }, pk.AsReadOnlySequence().ToArray());
        pk.TryDispose();

        // 负数拒绝
        Assert.Throws<ArgumentOutOfRangeException>(() => codec.BuildHeader(new Message(), -1));
    }

    [Fact]
    [DisplayName("帧泵_长度字段跨段_等待后定界")]
    public async Task Pump_HeaderAcrossSegments()
    {
        var pump = new MessagePump(new LengthFieldCodec());
        using var pipe = new Pipe();

        // 长度字段只到了 1 字节
        pipe.Writer.Append(new ArrayPacket(new Byte[] { 0x03 }));
        Assert.False(pump.TryRead(pipe.Reader, out _));
        Assert.Equal(1, pipe.UnconsumedLength);

        // 第二段到达：0x00 + 负载
        pipe.Writer.Append(new ArrayPacket(new Byte[] { 0x00, (Byte)'a', (Byte)'b', (Byte)'c' }));
        Assert.True(pump.TryRead(pipe.Reader, out var m));
        Assert.Equal("abc", Encoding.UTF8.GetString(await ReadBodyAsync(m!)));
        m!.Dispose();
    }

    [Fact]
    [DisplayName("帧泵_头到齐体未齐_流式交付")]
    public async Task Pump_HeadReady_BodyStreaming()
    {
        var pump = new MessagePump(new LengthFieldCodec());
        using var pipe = new Pipe();

        // 头 + 部分体：3 字节体只到了 1 字节，头到齐即交付
        pipe.Writer.Append(new ArrayPacket(new Byte[] { 0x03, 0x00, (Byte)'a' }));
        Assert.True(pump.TryRead(pipe.Reader, out var m));
        Assert.NotNull(m);
        Assert.True(m!.Body!.IsStreaming);

        // 补体：读满
        var readTask = ReadBodyAsync(m);
        pipe.Writer.Append(new ArrayPacket(new Byte[] { (Byte)'b', (Byte)'c' }));
        Assert.Equal("abc", Encoding.UTF8.GetString(await readTask));
        m.Dispose();
    }
}
