using System.Buffers;
using System.ComponentModel;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.Http;
using NewLife.Messaging;
using Xunit;

namespace XUnitTest.Messaging;

/// <summary>WebSocket 消息编解码器（WebSocketCodec）测试</summary>
public class WebSocketCodecTests
{
    #region 工具
    private static readonly WebSocketCodec _serverCodec = new() { IsServer = true };
    private static readonly WebSocketCodec _clientCodec = new() { IsServer = false };

    private static Byte[] MakePayload(Int32 count, Byte seed = 0x5A)
    {
        var buf = new Byte[count];
        for (var i = 0; i < count; i++) buf[i] = (Byte)(i * 13 + seed);

        return buf;
    }
    #endregion

    #region 解析
    [Fact]
    [DisplayName("WS编解码_文本帧无掩码_定界")]
    public void TryParse_TextFrame()
    {
        var frame = new Byte[] { 0x81, 0x05, (Byte)'h', (Byte)'e', (Byte)'l', (Byte)'l', (Byte)'o' };

        var rs = _serverCodec.TryParse(new ArrayPacket(frame).AsReadOnlySequence());
        Assert.NotNull(rs);
        var ws = Assert.IsType<WsMessage>(rs.Value.Message);
        Assert.True(ws.Fin);
        Assert.Equal(WebSocketMessageType.Text, ws.Type);
        Assert.Null(ws.MaskKey);
        Assert.Equal(2, rs.Value.HeaderSize);
        Assert.Equal(5L, rs.Value.BodyLength);

        // 头不足：等待（不产生对象）
        Assert.Null(_serverCodec.TryParse(new ArrayPacket(new Byte[] { 0x81 }).AsReadOnlySequence()));
    }

    [Fact]
    [DisplayName("WS编解码_掩码帧_密钥随头部消费")]
    public void TryParse_MaskedFrame()
    {
        // 客户端帧：FIN+Text + MASK+len5 + key(4) + 掩码负载
        var key = new Byte[] { 0x11, 0x22, 0x33, 0x44 };
        var payload = Encoding.UTF8.GetBytes("hello");
        var masked = new Byte[payload.Length];
        for (var i = 0; i < payload.Length; i++) masked[i] = (Byte)(payload[i] ^ key[i % 4]);

        var frame = new Byte[] { 0x81, 0x85, key[0], key[1], key[2], key[3], masked[0], masked[1], masked[2], masked[3], masked[4] };

        var rs = _serverCodec.TryParse(new ArrayPacket(frame).AsReadOnlySequence());
        Assert.NotNull(rs);
        var ws = Assert.IsType<WsMessage>(rs.Value.Message);
        Assert.Equal(key, ws.MaskKey);
        Assert.Equal(6, rs.Value.HeaderSize);
        Assert.Equal(5L, rs.Value.BodyLength);
    }

    [Fact]
    [DisplayName("WS编解码_扩展长度_2字节与8字节")]
    public void TryParse_ExtendedLength()
    {
        // 126 → 2 字节大端（200）
        var f2 = new Byte[] { 0x82, 0x7E, 0x00, 0xC8 };
        var rs2 = _serverCodec.TryParse(new ArrayPacket(f2).AsReadOnlySequence());
        Assert.NotNull(rs2);
        Assert.Equal(4, rs2.Value.HeaderSize);
        Assert.Equal(200L, rs2.Value.BodyLength);

        // 127 → 8 字节大端（70000）
        var f8 = new Byte[] { 0x82, 0x7F, 0, 0, 0, 0, 0, 0x01, 0x11, 0x70 };
        var rs8 = _serverCodec.TryParse(new ArrayPacket(f8).AsReadOnlySequence());
        Assert.NotNull(rs8);
        Assert.Equal(10, rs8.Value.HeaderSize);
        Assert.Equal(70000L, rs8.Value.BodyLength);
    }

    [Fact]
    [DisplayName("WS编解码_非法帧_返回失败")]
    public void TryParse_Invalid()
    {
        // 分片帧（FIN=0）：照常解析（Fin=false），重组由消费侧（WebSocket/WebSocketClient）完成
        var frag = _serverCodec.TryParse(new ArrayPacket(new Byte[] { 0x01, 0x03, 0x61 }).AsReadOnlySequence());
        Assert.NotNull(frag);
        var fragMsg = (WsMessage)frag.Value.Message!;
        Assert.False(fragMsg.Fin);
        Assert.Equal(WebSocketMessageType.Text, fragMsg.Type);
        fragMsg.Dispose();

        // 8 字节长度最高位为 1（负数）
        var neg = new Byte[] { 0x82, 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        Assert.Null(_serverCodec.TryParse(new ArrayPacket(neg).AsReadOnlySequence()));

        // 掩码位已置但密钥不齐：等待
        Assert.Null(_serverCodec.TryParse(new ArrayPacket(new Byte[] { 0x81, 0x85, 0x11, 0x22 }).AsReadOnlySequence()));
    }
    #endregion

    #region 构建
    [Fact]
    [DisplayName("WS编解码_服务端构建_无掩码字节与解析一致")]
    public async Task Build_Server()
    {
        var msg = new WsMessage { Type = WebSocketMessageType.Text };
        msg.SetBody(new ArrayPacket(Encoding.UTF8.GetBytes("hello")));

        var pk = _serverCodec.Build(msg);
        Assert.NotNull(pk);
        Assert.Null(msg.Payload);

        // 字节：[0x81, 0x05][hello]
        Assert.Equal(new Byte[] { 0x81, 0x05, (Byte)'h', (Byte)'e', (Byte)'l', (Byte)'l', (Byte)'o' }, pk!.AsReadOnlySequence().ToArray());

        // 帧泵端到端：绑定体并读满
        var pump = new MessagePump(_serverCodec);
        using var pipe = new Pipe();
        pipe.Writer.Append(pk);

        Assert.True(pump.TryRead(pipe.Reader, out var recv));
        Assert.Equal("hello", Encoding.UTF8.GetString(recv!.Payload!.ToArray()));
        recv.Dispose();
    }

    [Fact]
    [DisplayName("WS编解码_客户端构建_自动掩码可解码")]
    public void Build_Client_AutoMask()
    {
        var payload = MakePayload(37);
        var expected = MakePayload(37);     // 期望基准（独立副本：掩码 XOR 会原地改写负载）
        var msg = new WsMessage { Type = WebSocketMessageType.Binary };
        msg.SetBody(new ArrayPacket(payload));

        var pk = _clientCodec.Build(msg)!;
        var data = pk.AsReadOnlySequence().ToArray();

        // 掩码位置 1（长度字节）
        Assert.Equal(0x82, data[0]);
        Assert.True((data[1] & 0x80) != 0);

        // 服务端解析：掩码键挂在消息上，消费方解码还原
        var pump = new MessagePump(_serverCodec);
        using var pipe = new Pipe();
        pipe.Writer.Append(pk);

        Assert.True(pump.TryRead(pipe.Reader, out var bound));
        var ws = (WsMessage)bound!;
        Assert.NotNull(ws.MaskKey);
        Assert.True(ws.Demask());
        Assert.Equal(expected, ws.Payload!.ToArray());
        ws.Dispose();
        pk.TryDispose();
    }

    [Fact]
    [DisplayName("WS编解码_客户端指定密钥_构建确定性且可解码")]
    public void Build_Client_FixedMask_Deterministic()
    {
        var key = new Byte[] { 0x0A, 0x0B, 0x0C, 0x0D };

        // 同密钥两次构建：字节一致（确定性）
        var nw = new WsMessage { Type = WebSocketMessageType.Binary, MaskKey = key };
        nw.SetBody(new ArrayPacket(MakePayload(64)));
        var frame1 = _clientCodec.Build(nw)!;

        var nw2 = new WsMessage { Type = WebSocketMessageType.Binary, MaskKey = key };
        nw2.SetBody(new ArrayPacket(MakePayload(64)));
        var frame2 = _clientCodec.Build(nw2)!;

        Assert.Equal(frame2.AsReadOnlySequence().ToArray(), frame1.AsReadOnlySequence().ToArray());

        // 服务端解码还原
        var pump = new MessagePump(_serverCodec);
        using var pipe = new Pipe();
        pipe.Writer.Append(frame1);
        Assert.True(pump.TryRead(pipe.Reader, out var bound));
        var ws = (WsMessage)bound!;
        Assert.Equal(key, ws.MaskKey);
        Assert.True(ws.Demask());
        Assert.Equal(MakePayload(64), ws.Payload!.ToArray());
        ws.Dispose();

        frame1.TryDispose();
        frame2.TryDispose();
    }

    [Fact]
    [DisplayName("WS编解码_服务端构建_全边界长度往返")]
    public void Build_Server_RoundTripAllSizes()
    {
        foreach (var count in new[] { 0, 5, 125, 126, 65535, 65536 })
        {
            var payload = MakePayload(count);

            var nw = new WsMessage { Type = WebSocketMessageType.Text };
            nw.SetBody(new ArrayPacket(payload));
            var frame = _serverCodec.Build(nw)!;

            // 客户端解析回读：服务端帧无掩码
            var pump = new MessagePump(_clientCodec);
            using var pipe = new Pipe();
            pipe.Writer.Append(frame);
            Assert.True(pump.TryRead(pipe.Reader, out var bound));
            var ws = (WsMessage)bound!;
            Assert.Equal(payload, ws.Payload!.ToArray());
            ws.Dispose();

            frame.TryDispose();
        }
    }

    [Fact]
    [DisplayName("WS编解码_头部构建_声明长度")]
    public void BuildHeader_DeclaresLength()
    {
        var pk = _serverCodec.BuildHeader(new WsMessage { Type = WebSocketMessageType.Binary }, 300);
        Assert.Equal(new Byte[] { 0x82, 0x7E, 0x01, 0x2C }, pk.AsReadOnlySequence().ToArray());
        pk.TryDispose();

        // 客户端方向：带掩码帧不支持流式发送
        Assert.Throws<NotSupportedException>(() => _clientCodec.BuildHeader(new WsMessage(), 10));

        // 非法长度
        Assert.Throws<ArgumentOutOfRangeException>(() => _serverCodec.BuildHeader(new WsMessage(), -1));
    }

    [Fact]
    [DisplayName("WS编解码_关闭帧_状态码与描述解析")]
    public void CloseStatus_Parse()
    {
        var desc = "bye";
        var descBytes = Encoding.UTF8.GetBytes(desc);
        var body = new Byte[2 + descBytes.Length];
        body[0] = 0x03;
        body[1] = 0xE8;     // 1000 网络序
        descBytes.CopyTo(body, 2);

        var msg = new WsMessage { Type = WebSocketMessageType.Close };
        msg.SetBody(new ArrayPacket(body));

        Assert.True(msg.TryReadCloseStatus());
        Assert.Equal(1000, msg.CloseStatus);
        Assert.Equal(desc, msg.StatusDescription);

        msg.Dispose();
    }
    #endregion

    [Fact]
    [DisplayName("WS编解码_接收掩码帧_消息解码还原")]
    public void Receive_MaskedFrame_Demask()
    {
        var expected = MakePayload(20);
        var msg = new WsMessage { Type = WebSocketMessageType.Binary };
        msg.SetBody(new ArrayPacket(MakePayload(20)));
        var frame = _clientCodec.Build(msg)!;

        // 服务端解析：掩码键挂在消息上
        var rs = _serverCodec.TryParse(frame.AsReadOnlySequence());
        Assert.NotNull(rs);
        Assert.NotNull(((WsMessage)rs.Value.Message!).MaskKey);

        // 帧泵绑体（内存视图）后解码还原
        var pump = new MessagePump(_serverCodec);
        using var pipe = new Pipe();
        pipe.Writer.Append(frame);

        Assert.True(pump.TryRead(pipe.Reader, out var bound));
        var ws = (WsMessage)bound!;
        Assert.True(ws.Demask());
        Assert.Equal(expected, ws.Payload!.ToArray());
        ws.Dispose();

        // 无掩码消息：解码为无操作
        var plain = new WsMessage { Type = WebSocketMessageType.Text };
        plain.SetBody(new ArrayPacket(expected));
        Assert.False(plain.Demask());
        plain.Dispose();
    }

    #region 辅助
    #endregion
}
