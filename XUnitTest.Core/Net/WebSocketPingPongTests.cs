using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>WebSocket 客户端心跳回应测试。RFC 6455 §5.5.2/§5.5.3：收到 Ping 必须回 Pong 并回传同一 Application Data。
/// 使用裸套接字服务端校验线上帧，不依赖自家服务端实现</summary>
[Collection("Net.D")]
public class WebSocketPingPongTests
{
    [Fact]
    [DisplayName("ws心跳_服务端Ping_客户端自动回Pong并回传同一负载")]
    public async Task ServerPing_ClientRepliesPong()
    {
        using var listener = StartListener(out var port);

        // 无掩码 Ping：FIN=1 OPCODE=9，负载 "hi"
        var ping = new Byte[] { 0x89, 0x02, (Byte)'h', (Byte)'i' };
        // 客户端应答帧：FIN=1+Pong(1) + MASK+长度(1) + 掩码键(4) + 负载(2) = 10 字节
        var server = AcceptAndExpectPongAsync(listener, ping, 10);

        using var client = new WebSocketClient($"ws://127.0.0.1:{port}/ws") { Timeout = 5_000 };
        Assert.True(await client.OpenAsync());

        var reply = await server.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(0x8A, reply[0]);                           // FIN=1 + Pong
        Assert.True((reply[1] & 0x80) != 0, "客户端帧必须带掩码");
        Assert.Equal(2, reply[1] & 0x7F);
        Assert.Equal("hi", Unmask(reply));

        // 消息照常入队：拉取 API 仍能观察 Ping，业务行为不变
        var msg = await client.ReceiveMessageAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(msg);
        Assert.Equal(WebSocketMessageType.Ping, msg!.Type);
        Assert.Equal("hi", msg.Payload?.ToStr());
    }

    [Fact]
    [DisplayName("ws心跳_空负载Ping_客户端回空负载Pong")]
    public async Task EmptyPing_ClientRepliesEmptyPong()
    {
        using var listener = StartListener(out var port);

        // 空负载 Ping：{FIN=1 OPCODE=9, 长度0}
        var ping = new Byte[] { 0x89, 0x00 };
        // 客户端应答：FIN=1+Pong + MASK+长度0 + 掩码键(4) = 6 字节
        var server = AcceptAndExpectPongAsync(listener, ping, 6);

        using var client = new WebSocketClient($"ws://127.0.0.1:{port}/ws") { Timeout = 5_000 };
        Assert.True(await client.OpenAsync());

        var reply = await server.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(0x8A, reply[0]);
        Assert.True((reply[1] & 0x80) != 0, "客户端帧必须带掩码");
        Assert.Equal(0, reply[1] & 0x7F);
    }

    private static Socket StartListener(out Int32 port)
    {
        var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);
        port = ((IPEndPoint)listener.LocalEndPoint!).Port;

        return listener;
    }

    /// <summary>接受一个连接，完成握手、发送 Ping 帧，并读取客户端回写的应答帧（长度确定，按量收满）</summary>
    private static async Task<Byte[]> AcceptAndExpectPongAsync(Socket listener, Byte[] pingFrame, Int32 replyLength)
    {
        using var socket = await listener.AcceptAsync();
        socket.NoDelay = true;
        using var ns = new NetworkStream(socket);

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

        // 发送 Ping
        await ns.WriteAsync(pingFrame);
        await ns.FlushAsync();

        // 按量收满应答帧
        var reply = new Byte[replyLength];
        var got = 0;
        while (got < reply.Length)
        {
            var n = await ns.ReadAsync(reply.AsMemory(got));
            if (n <= 0) throw new IOException("客户端未回写应答帧");
            got += n;
        }

        return reply;
    }

    /// <summary>解码客户端掩码帧负载（帧头 2 字节 + 掩码键 4 字节）</summary>
    private static String Unmask(Byte[] frame)
    {
        var len = frame[1] & 0x7F;
        var payload = new Byte[len];
        for (var i = 0; i < len; i++) payload[i] = (Byte)(frame[6 + i] ^ frame[2 + i % 4]);

        return Encoding.UTF8.GetString(payload);
    }
}
