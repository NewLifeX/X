using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>客户端 TLS 握手的超时与取消覆盖测试。服务端为“接受连接但从不进行 TLS 应答”的裸监听套接字</summary>
[Collection("Net.D")]
public class TcpSslHandshakeTests
{
    [Fact]
    [DisplayName("tls客户端_服务端不应答握手_超时预算生效")]
    public async Task Handshake_NoServerResponse_TimeoutBudget()
    {
        using var listener = StartSilentListener(out var port);

        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{port}"),
            SslProtocol = SslProtocols.Tls12,
            Timeout = 1_000,
        };

        var sw = Stopwatch.StartNew();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.OpenAsync()).WaitAsync(TimeSpan.FromSeconds(10));
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 8_000, $"超时应按预算失败，实际 {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    [DisplayName("tls客户端_握手期间取消_取消令牌生效")]
    public async Task Handshake_Cancelled_ByCallerToken()
    {
        using var listener = StartSilentListener(out var port);

        // Timeout 故意放长：证明是取消令牌在起作用，而不是超时预算兜底
        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{port}"),
            SslProtocol = SslProtocols.Tls12,
            Timeout = 60_000,
        };

        using var cts = new CancellationTokenSource(500);
        var sw = Stopwatch.StartNew();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.OpenAsync(cts.Token)).WaitAsync(TimeSpan.FromSeconds(10));
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 8_000, $"取消应尽快生效，实际 {sw.ElapsedMilliseconds}ms");
    }

    /// <summary>裸监听套接字：接受连接后保持沉默，不对 TLS 握手作答</summary>
    private static Socket StartSilentListener(out Int32 port)
    {
        var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);
        port = ((IPEndPoint)listener.LocalEndPoint!).Port;

        // 接受连接并保持沉默一段时间；客户端超时/取消后自行断开
        _ = Task.Run(async () =>
        {
            try
            {
                using var socket = await listener.AcceptAsync();
                await Task.Delay(TimeSpan.FromSeconds(20));
            }
            catch
            {
                // 监听套接字释放属预期
            }
        });

        return listener;
    }
}
