using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
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
}
