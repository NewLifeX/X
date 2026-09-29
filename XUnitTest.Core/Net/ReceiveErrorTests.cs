using System.Net;
using System.Net.Sockets;
using NewLife;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>接收错误处理契约</summary>
[Collection("Integration")]
public class ReceiveErrorTests
{
    [Fact(DisplayName = "接收错误_连接型会话非ConnectionReset错误_也关闭会话并通知")]
    public void ReceiveError_StreamSession_ClosesOnAnyError()
    {
        var ready = new ManualResetEventSlim(false);
        var closed = new ManualResetEventSlim(false);
        SessionBase? ss = null;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.NewSession += (s, e) =>
        {
            ss = e.Session?.Session as SessionBase;
            if (ss != null) ss.Closed += (a, b) => closed.Set();

            ready.Set();
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}") { AutoReconnect = false };
        Assert.True(client.Open());
        Assert.True(ready.Wait(3000));
        Assert.NotNull(ss);

        // NetworkDown 与 ConnectionReset 一样说明连接不可用。只对 ConnectionReset 关闭会留下半死会话：
        // Active 仍为 true、不再收数据、也不触发 Closed，上层自动重连永不启动
        var se = new SocketAsyncEventArgs { SocketError = SocketError.NetworkDown };
        Assert.True(ss!.OnReceiveError(se));

        Assert.True(closed.Wait(3000));
        Assert.False(ss.Active);
    }

    [Fact(DisplayName = "UDP接收错误_报文级错误_不销毁接收槽")]
    public void UdpReceiveError_KeepsSlot()
    {
        using var server = new UdpServer { Port = 0, MaxAsync = 2 };
        server.Open();

        // MessageSize（报文超出接收缓冲）此前返回 true，会让 ProcessEvent 释放该接收槽；
        // 槽位只在启动时创建一次、销毁后不重建，累计耗尽后整台服务器静默停收
        var size = new SocketAsyncEventArgs
        {
            SocketError = SocketError.MessageSize,
            RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 12345),
        };
        Assert.False(server.OnReceiveError(size));

        // Reset/Aborted 只关对应会话，同样销毁接收槽
        var reset = new SocketAsyncEventArgs
        {
            SocketError = SocketError.ConnectionReset,
            RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 12345),
        };
        Assert.False(server.OnReceiveError(reset));

        Assert.True(server.Active);
    }

    [Fact(DisplayName = "UDP接收_超大报文触发MessageSize_服务仍可收包")]
    public async Task UdpMessageSize_KeepsReceiving()
    {
        // 接收缓冲刻意调小以迫使大报文触发 MessageSize；MaxAsync=2，旧实现下两次错误即耗尽全部接收槽，
        // 服务随后静默失聪（后续报文一律收不到）
        using var server = new UdpServer { Port = 0, BufferSize = 1024, MaxAsync = 2 };
        server.Received += (s, e) =>
        {
            if (s is UdpSession session && e.Packet != null && e.Packet.Length > 0) session.Send(e.Packet);
        };
        server.Open();

        var remote = new IPEndPoint(IPAddress.Loopback, server.Port);

        // 连发多个超出接收缓冲的报文；其回显可能被截断，本用例不关心
        using (var flood = NetHelper.CreateUdpClient())
        {
            var big = new Byte[2048];
            for (var i = 0; i < 6; i++) await flood.SendAsync(big, big.Length, remote);

            await Task.Delay(200);
        }

        // 换一个客户端（新远端端点）发普通报文，必须仍能被接收并回显
        using var client = NetHelper.CreateUdpClient();
        var payload = new Byte[] { 9, 8, 7, 6 };
        await client.SendAsync(payload, payload.Length, remote);

        var result = await client.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(payload, result.Buffer);
    }
}
