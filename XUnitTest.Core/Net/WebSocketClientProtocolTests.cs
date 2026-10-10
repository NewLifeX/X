using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using NewLife;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>WebSocket 客户端接收侧协议行为测试。RFC 6455 §5.1/§5.5 要求客户端检出违规帧时失败连接（1002），
/// 且不得把违规帧交付业务、不得按违规内容回写；§5.5.1 要求收到 Close 时回写关闭帧并关闭连接。
/// 使用裸套接字服务端构造帧，不依赖自家服务端实现</summary>
[Collection("Net.D")]
public class WebSocketClientProtocolTests
{
    [Fact]
    [DisplayName("ws协议_服务端带掩码帧_客户端以1002失败连接且不交付消息")]
    public async Task ServerMaskedFrame_ClientFails1002()
    {
        using var listener = StartListener(out var port);

        // 服务端方向违规：带掩码的文本帧（RFC 6455 §5.1 服务端不得掩码）
        var key = new Byte[] { 0x11, 0x22, 0x33, 0x44 };
        var masked = new Byte[] { 0x81, 0x80 | 2, key[0], key[1], key[2], key[3], (Byte)('h' ^ key[0]), (Byte)('i' ^ key[1]) };
        var server = AcceptAndReplyAsync(listener, masked);

        using var client = new WebSocketClient($"ws://127.0.0.1:{port}/ws") { Timeout = 5_000 };
        Assert.True(await client.OpenAsync());

        var (opcode, status) = await server.WaitAsync(TimeSpan.FromSeconds(10));

        // 客户端应答的首帧必须是关闭帧 1002，而不是把掩码负载当业务数据
        Assert.Equal(0x88, opcode);                         // FIN=1 + Close
        Assert.Equal(1002, status);

        // 拉取 API 不得交付该违规帧：连接关闭后返回 null
        var msg = await client.ReceiveMessageAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Null(msg);
    }

    [Fact]
    [DisplayName("ws协议_超大Ping控制帧_客户端不回Pong并以1002失败连接")]
    public async Task OversizedPing_ClientFails1002WithoutPong()
    {
        using var listener = StartListener(out var port);

        // 违规：Ping 负载 126 字节，超过控制帧上限 125（RFC 6455 §5.5）
        var payload = new Byte[126];
        var ping = new Byte[4 + payload.Length];
        ping[0] = 0x89;                                     // FIN=1 + Ping
        ping[1] = 126;                                      // 2 字节扩展长度
        ping[3] = 126;
        var server = AcceptAndReplyAsync(listener, ping);

        using var client = new WebSocketClient($"ws://127.0.0.1:{port}/ws") { Timeout = 5_000 };
        Assert.True(await client.OpenAsync());

        var (opcode, status) = await server.WaitAsync(TimeSpan.FromSeconds(10));

        // 首帧必须是关闭帧：若客户端按违规 Ping 回了 Pong，Pong 会先于关闭帧到达
        Assert.Equal(0x88, opcode);
        Assert.Equal(1002, status);
    }

    [Fact]
    [DisplayName("ws协议_分片Ping控制帧_客户端不回Pong并以1002失败连接")]
    public async Task FragmentedPing_ClientFails1002WithoutPong()
    {
        using var listener = StartListener(out var port);

        // 违规：Ping 帧 FIN=0（RFC 6455 §5.5 控制帧不得分片）
        var ping = new Byte[] { 0x09, 0x00 };
        var server = AcceptAndReplyAsync(listener, ping);

        using var client = new WebSocketClient($"ws://127.0.0.1:{port}/ws") { Timeout = 5_000 };
        Assert.True(await client.OpenAsync());

        var (opcode, status) = await server.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(0x88, opcode);
        Assert.Equal(1002, status);
    }

    [Fact]
    [DisplayName("ws关闭_服务端发Close_客户端回显同码并关闭，拉取先得Close消息再得null")]
    public async Task ServerClose_ClientRepliesEchoAndCloses()
    {
        using var listener = StartListener(out var port);

        // 服务端 Close(1000, "bye")：FIN=1 + Close，长度 5（2 字节状态码 + 3 字节描述）
        var close = new Byte[] { 0x88, 0x05, 0x03, 0xE8, (Byte)'b', (Byte)'y', (Byte)'e' };
        var server = AcceptAndReplyAsync(listener, close);

        using var client = new WebSocketClient($"ws://127.0.0.1:{port}/ws") { Timeout = 5_000 };
        Assert.True(await client.OpenAsync());

        var (opcode, status) = await server.WaitAsync(TimeSpan.FromSeconds(10));

        // 客户端必须回写关闭帧并回显状态码（RFC 6455 §5.5.1）
        Assert.Equal(0x88, opcode);
        Assert.Equal(1000, status);

        // 拉取 API 仍能先观察到 Close 消息，随后连接关闭返回 null
        var msg = await client.ReceiveMessageAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(msg);
        Assert.Equal(NewLife.Messaging.WebSocketMessageType.Close, msg!.Type);
        Assert.Equal(1000, msg.CloseStatus);

        Assert.Null(await client.ReceiveMessageAsync().WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    [DisplayName("ws关闭_服务端空Close_客户端回空负载关闭帧")]
    public async Task ServerEmptyClose_ClientRepliesEmptyClose()
    {
        using var listener = StartListener(out var port);

        // 无状态码关闭帧：{FIN=1 + Close, 长度 0}
        var close = new Byte[] { 0x88, 0x00 };
        var server = AcceptAndReplyAsync(listener, close);

        using var client = new WebSocketClient($"ws://127.0.0.1:{port}/ws") { Timeout = 5_000 };
        Assert.True(await client.OpenAsync());

        var (opcode, status) = await server.WaitAsync(TimeSpan.FromSeconds(10));

        // 回写空负载关闭帧：不得把 1005 等保留值发到线上（RFC 6455 §7.4.1）
        Assert.Equal(0x88, opcode);
        Assert.Equal(0, status);
    }

    [Fact]
    [DisplayName("ws关闭_服务端保留码Close_客户端回1002")]
    public async Task ServerCloseWithReservedCode_ClientReplies1002()
    {
        using var listener = StartListener(out var port);

        // 保留状态码 1005（RFC 6455 §7.4.1 禁止出现在线路上）：{FIN=1 + Close, 长度 2, 1005}
        var close = new Byte[] { 0x88, 0x02, 0x03, 0xED };
        var server = AcceptAndReplyAsync(listener, close);

        using var client = new WebSocketClient($"ws://127.0.0.1:{port}/ws") { Timeout = 5_000 };
        Assert.True(await client.OpenAsync());

        var (opcode, status) = await server.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(0x88, opcode);
        Assert.Equal(1002, status);
    }

    [Fact]
    [DisplayName("ws关闭_应用主动Close_客户端只发一帧关闭帧并关闭")]
    public async Task AppInitiatedClose_SendsSingleCloseFrame()
    {
        using var listener = StartListener(out var port);

        // 服务端只握手：读取客户端主动发出的关闭帧，再确认连接随后关闭（无第二帧）
        var server = AcceptAndExpectSingleCloseAsync(listener);

        using var client = new WebSocketClient($"ws://127.0.0.1:{port}/ws") { Timeout = 5_000 };
        Assert.True(await client.OpenAsync());

        await client.CloseAsync(1000, "app");

        var (opcode, status, hasMore) = await server.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(0x88, opcode);
        Assert.Equal(1000, status);
        Assert.False(hasMore, "会话关闭后不得再发送任何帧");
    }

    private static Socket StartListener(out Int32 port)
    {
        var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);
        port = ((IPEndPoint)listener.LocalEndPoint!).Port;

        return listener;
    }

    /// <summary>接受一个连接，完成握手、发送违规帧，并读取客户端回写的首帧（opcode 与关闭状态码）</summary>
    private static async Task<(Int32 Opcode, Int32 CloseStatus)> AcceptAndReplyAsync(Socket listener, Byte[] frame)
    {
        using var socket = await listener.AcceptWithinAsync();
        socket.NoDelay = true;
        using var ns = new NetworkStream(socket);

        await HandshakeAsync(ns);

        // 发送违规帧
        await ns.WriteAsync(frame);
        await ns.FlushAsync();

        // 读取客户端应答的首帧（客户端所有帧都带掩码）
        return await ReadFrameAsync(ns);
    }

    /// <summary>接受一个连接，完成握手，读取客户端主动发出的关闭帧，并确认随后连接关闭、无后续帧</summary>
    private static async Task<(Int32 Opcode, Int32 CloseStatus, Boolean HasMore)> AcceptAndExpectSingleCloseAsync(Socket listener)
    {
        using var socket = await listener.AcceptWithinAsync();
        socket.NoDelay = true;
        using var ns = new NetworkStream(socket);

        await HandshakeAsync(ns);

        var (opcode, status) = await ReadFrameAsync(ns);

        // 确认没有第二帧：连接关闭（EOF）或静默超时视为正常，读到任何字节视为多余发送
        var hasMore = false;
        try
        {
            using var cts = new CancellationTokenSource(1_000);
            var buffer = new Byte[16];
            var n = await ns.ReadAsync(buffer, cts.Token);
            hasMore = n > 0;
        }
        catch (OperationCanceledException) { }
        catch (IOException) { }

        return (opcode, status, hasMore);
    }

    /// <summary>完成 WebSocket 服务端握手（读取请求头、回写 101 响应）</summary>
    private static async Task HandshakeAsync(NetworkStream ns)
    {
        // 读握手请求直到空行
        var buf = new Byte[4096];
        var total = 0;
        while (true)
        {
            var n = await ns.ReadAsync(buf.AsMemory(total));
            if (n <= 0) throw new IOException("握手请求未读完连接即关闭");
            total += n;
            if (Encoding.ASCII.GetString(buf, 0, total).Contains("\r\n\r\n")) break;
            if (total >= buf.Length) throw new IOException("握手请求过长");
        }

        // 计算并回写 101 响应
        var key = "";
        foreach (var line in Encoding.ASCII.GetString(buf, 0, total).Split("\r\n"))
        {
            if (line.StartsWithIgnoreCase("Sec-WebSocket-Key:"))
            {
                key = line[(line.IndexOf(':') + 1)..].Trim();
                break;
            }
        }
        var accept = SHA1.Create().ComputeHash((key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11").GetBytes()).ToBase64();
        var head = $"HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: {accept}\r\n\r\n";
        await ns.WriteAsync(Encoding.ASCII.GetBytes(head));
        await ns.FlushAsync();
    }

    /// <summary>读取一帧（客户端帧带掩码），返回 opcode 与关闭状态码（非 Close 帧或无状态码时为 0）</summary>
    private static async Task<(Int32 Opcode, Int32 CloseStatus)> ReadFrameAsync(NetworkStream ns)
    {
        var header = await ReadExactlyAsync(ns, 2);
        var opcode = header[0];
        var masked = (header[1] & 0x80) != 0;
        var len = (Int64)(header[1] & 0x7F);
        if (len == 126)
        {
            var ext = await ReadExactlyAsync(ns, 2);
            len = (ext[0] << 8) | ext[1];
        }
        else if (len == 127)
        {
            var ext = await ReadExactlyAsync(ns, 8);
            len = 0;
            for (var i = 0; i < 8; i++) len = (len << 8) | ext[i];
        }

        var masks = masked ? await ReadExactlyAsync(ns, 4) : null;
        var payload = await ReadExactlyAsync(ns, (Int32)len);
        if (masks != null)
        {
            for (var i = 0; i < payload.Length; i++) payload[i] = (Byte)(payload[i] ^ masks[i & 3]);
        }

        var status = payload.Length >= 2 ? (payload[0] << 8) | payload[1] : 0;

        return (opcode, status);
    }

    /// <summary>按量收满指定字节数；连接提前关闭即抛异常</summary>
    private static async Task<Byte[]> ReadExactlyAsync(NetworkStream ns, Int32 count)
    {
        var buf = new Byte[count];
        var got = 0;
        while (got < count)
        {
            var n = await ns.ReadAsync(buf.AsMemory(got));
            if (n <= 0) throw new IOException($"连接提前关闭，仅读到 {got}/{count} 字节");
            got += n;
        }

        return buf;
    }
}
