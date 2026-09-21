using System.Net;
using System.Net.Sockets;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>连接断开路径测试：服务端踢出、对端 RST 强制重置、拉取等待中对端断开</summary>
[Collection("Net")]
public class NetDisconnectTests
{
    /// <summary>服务端主动关闭会话，客户端应确定性感知断开</summary>
    [Fact(DisplayName = "断开_服务端踢出会话_客户端感知Closed且不可发送")]
    public void Kick_ServerCloseSession_ClientDetects()
    {
        var sessionReady = new ManualResetEventSlim(false);
        INetSession? serverSession = null;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };
        server.NewSession += (s, e) => { serverSession = e.Session; sessionReady.Set(); };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}")
        {
            AutoReconnect = false,
            Log = XTrace.Log,
        };
        var clientClosed = new ManualResetEventSlim(false);
        client.Closed += (s, e) => clientClosed.Set();
        Assert.True(client.Open());

        Assert.True(sessionReady.Wait(3000));
        Assert.NotNull(serverSession);

        // 服务端踢出：按契约主动断开与客户端的连接
        serverSession!.Close("kick");

        Assert.True(clientClosed.Wait(5000), "客户端应在服务端踢出后触发 Closed 事件");

        // 状态收敛后不允许继续发送
        for (var i = 0; i < 20 && client.Active; i++) Thread.Sleep(50);
        Assert.False(client.Active);
        Assert.Throws<InvalidOperationException>(() => client.Send("after-kick"));
    }

    /// <summary>客户端强制 RST 断开，服务端应感知 ConnectionReset 并关闭会话</summary>
    [Fact(DisplayName = "断开_对端RST重置_服务端感知ConnectionReset并关闭会话")]
    public void Rst_ClientAbort_ServerDetects()
    {
        var sessionReady = new ManualResetEventSlim(false);
        INetSession? serverSession = null;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };
        server.NewSession += (s, e) => { serverSession = e.Session; sessionReady.Set(); };
        server.Start();

        var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        sock.Connect(IPAddress.Loopback, server.Port);

        Assert.True(sessionReady.Wait(3000));
        Assert.NotNull(serverSession);

        // 先发一点数据确保服务端会话进入接收
        _ = sock.Send("hello"u8);
        Thread.Sleep(100);

        // 强制 RST：Linger 0 后关闭，不走四次挥手
        sock.LingerState = new LingerOption(true, 0);
        sock.Close();

        // 服务端应快速感知并关闭会话
        var socket = (SessionBase)serverSession!.Session;
        for (var i = 0; i < 100 && socket.Active; i++) Thread.Sleep(50);
        Assert.False(socket.Active, "服务端应感知 RST 并关闭会话");
        Assert.Equal("ConnectionReset", socket.CloseReason);
    }

    /// <summary>拉取模式等待过程中对端断开，同步拉取应及时返回而不是挂死</summary>
    [Fact(DisplayName = "断开_拉取等待中对端关闭_同步拉取及时返回")]
    public async Task Pull_RemoteClosed_SyncReceiveReturns()
    {
        var sessionReady = new ManualResetEventSlim(false);
        TcpSession? serverSession = null;

        using var server = new TcpServer { Port = 0, Log = XTrace.Log };
        server.NewSession += (s, e) => { serverSession = e.Session as TcpSession; sessionReady.Set(); };
        server.Start();

        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
            AutoReceive = false,
            Timeout = 5_000,
            Log = XTrace.Log,
        };
        client.Open();

        Assert.True(sessionReady.Wait(3000));
        Assert.NotNull(serverSession);

        // 服务端关闭连接后，拉取应感知断开
        serverSession!.Close("server-close");
        Thread.Sleep(100);

        // 后台任务 + 超时保护：验证“不挂死”（超时抛 TimeoutException 即失败）
        var task = Task.Run(() =>
        {
            try { return (Object?)client.Receive(); }
            catch (Exception ex) { return ex; }
        });

        var result = await task.WaitAsync(TimeSpan.FromSeconds(3));

        // 允许两种结果：空包（FIN 读到 0 字节）或明确的连接异常
        if (result is IOwnerPacket pk) Assert.Equal(0, pk.Length);
        else Assert.IsAssignableFrom<Exception>(result);
    }
}
