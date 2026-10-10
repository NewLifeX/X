using System.Net.Sockets;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>内核接收缓冲设置（SocketSetting.ReceiveBufferSize）的生效契约</summary>
[Collection("Net.B")]
public class TcpReceiveBufferTests
{
    [Fact(DisplayName = "TCP内核接收缓冲_按设置应用到连接两端")]
    public async Task TcpReceiveBufferSize_AppliedToBothEnds()
    {
        const Int32 size = 256 * 1024;

        var old = SocketSetting.Current.ReceiveBufferSize;
        try
        {
            SocketSetting.Current.ReceiveBufferSize = size;

            using var server = new NetServer { Port = 0, ProtocolType = NetType.Tcp };
            SessionBase? accepted = null;
            server.NewSession += (s, e) =>
            {
                if (e.Session?.Session is SessionBase sb) accepted = sb;
            };
            server.Received += (s, e) => { };
            server.Start();

            using var client = new TcpSession { Remote = new NetUri($"tcp://127.0.0.1:{server.Port}") };
            client.Open();

            var payload = "rcvbuf"u8.ToArray();
            client.Send(payload);

            // 等待服务端接受连接
            var end = Environment.TickCount64 + 5_000;
            while (accepted == null && Environment.TickCount64 < end) await Task.Delay(20);

            Assert.NotNull(accepted);

            // 两端套接字都应带上设置值（系统可能向上取整，故用 >=）
            Assert.True(accepted!.Client!.ReceiveBufferSize >= size, $"服务端 {accepted.Client.ReceiveBufferSize} < {size}");
            Assert.True(client.Client!.ReceiveBufferSize >= size, $"客户端 {client.Client.ReceiveBufferSize} < {size}");
        }
        finally
        {
            SocketSetting.Current.ReceiveBufferSize = old;
        }
    }
}
