using System.Net.Sockets;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>接收错误处理契约</summary>
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
}
