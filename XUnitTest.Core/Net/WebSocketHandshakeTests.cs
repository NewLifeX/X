using System.ComponentModel;
using System.Diagnostics;
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

/// <summary>WebSocket 客户端握手（打开链路内异步直读、超时失败）测试</summary>
[Collection("Net")]
public class WebSocketHandshakeTests
{
    [Fact]
    [DisplayName("ws握手_服务端不响应_超时返回失败且不阻塞")]
    public async Task Handshake_NoResponse_TimesOut()
    {
        // 裸监听套接字：接受连接但从不响应握手
        using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);
        var port = ((IPEndPoint)listener.LocalEndPoint!).Port;

        using var client = new WebSocketClient($"ws://127.0.0.1:{port}/ws")
        {
            Timeout = 1_000,
        };

        var sw = Stopwatch.StartNew();
        var ok = await client.OpenAsync().WaitAsync(TimeSpan.FromSeconds(10));
        sw.Stop();

        Assert.False(ok, "服务端不响应握手时应返回失败");
        Assert.False(client.Active, "握手失败不应置为活动状态");
    }

    [Fact]
    [DisplayName("ws握手_响应与首帧同段_首帧不丢失")]
    public async Task Handshake_ImmediateFirstFrame_NotLost()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint!).Port;

        var server = Task.Run(async () =>
        {
            try
            {
                using var tc = await listener.AcceptTcpClientAsync();
                tc.NoDelay = true;
                using var ns = tc.GetStream();

                // 读握手请求
                var buf = new Byte[4096];
                var n = await ns.ReadAsync(buf);
                var text = Encoding.ASCII.GetString(buf, 0, n);

                // 取 Sec-WebSocket-Key
                var key = "";
                foreach (var line in text.Split("\r\n"))
                {
                    if (line.StartsWithIgnoreCase("Sec-WebSocket-Key:"))
                    {
                        key = line[(line.IndexOf(':') + 1)..].Trim();
                        break;
                    }
                }

                var accept = SHA1.Create().ComputeHash((key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11").GetBytes()).ToBase64();
                var head = $"HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: {accept}\r\n\r\n";

                // 服务端首帧（无掩码文本帧 welcome）
                var frame = new Byte[] { 0x81, 0x07, (Byte)'w', (Byte)'e', (Byte)'l', (Byte)'c', (Byte)'o', (Byte)'m', (Byte)'e' };

                // 一次性写入：响应头与首帧落在同一 TCP 段（数据小 + NoDelay）
                var all = new Byte[head.Length + frame.Length];
                Encoding.ASCII.GetBytes(head).CopyTo(all, 0);
                frame.CopyTo(all, head.Length);
                await ns.WriteAsync(all);
                await ns.FlushAsync();

                // 保持连接，便于客户端读完首帧
                await Task.Delay(3_000);
            }
            catch
            {
                // 客户端提前断开属预期
            }
            finally
            {
                listener.Stop();
            }
        });

        using var client = new WebSocketClient($"ws://127.0.0.1:{port}/ws") { Timeout = 5_000 };
        Assert.True(await client.OpenAsync());

        // 服务器握手后立即推送的首帧必须能被收到（修复前它随握手响应一起被释放）
        var msg = await client.ReceiveMessageAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(msg);
        Assert.Equal(WebSocketMessageType.Text, msg!.Type);
        Assert.Equal("welcome", msg.Payload?.ToStr());

        await server;
    }
}
