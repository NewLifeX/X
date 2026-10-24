using System.Net;
using System.Net.Sockets;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>测试端口分配工具单元测试</summary>
public class TestPortTests
{
    /// <summary>候选端口被抢占时顺延下一个候选端口重试，且半启动的实例被释放</summary>
    [Fact(DisplayName = "候选端口被抢占_顺延下一端口重试且释放半启动实例")]
    public void StartAnyRetriesOnPortTaken()
    {
        var attempts = 0;
        var created = new List<FakeServer>();
        var server = TestPort.StartAny(26500, port =>
        {
            var item = new FakeServer(port);
            created.Add(item);
            return item;
        }, e =>
        {
            // 首轮模拟“探测通过后被出站连接抢占”：服务器绑定抛 WSAEADDRINUSE
            if (Interlocked.Increment(ref attempts) == 1) throw new SocketException((Int32)SocketError.AddressAlreadyInUse);

            e.Start();
        });

        Assert.Equal(2, attempts);      // 首次被抢占后顺延重试
        Assert.Equal(2, created.Count); // 每次都创建新实例，不复用半启动状态
        Assert.Equal(1, created[0].Disposed);
        Assert.False(created[0].Started);
        Assert.True(server.Started);
        Assert.Same(created[1], server);
    }

    /// <summary>单协议被静默跳过时校验双协议并顺延，避免半可用服务端让整套用例假失败</summary>
    [Fact(DisplayName = "单协议被静默跳过_校验双协议后顺延下一端口")]
    public void StartAnyRetriesWhenSingleProtocolDropped()
    {
        TcpListener? blocker = null;
        var fired = false;
        var server = TestPort.StartAny(28500, port => new NetServer
        {
            Port = port,
            AddressFamily = AddressFamily.InterNetwork,
        }, e =>
        {
            if (!fired)
            {
                // 模拟探测通过后被出站连接抢占：TCP 只剩对手监听，NetServer 静默跳过它只启动 UDP
                fired = true;
                blocker = new TcpListener(IPAddress.Any, e.Port);
                blocker.Start();
            }

            e.Start();
        });

        try
        {
            Assert.True(fired);
            Assert.NotEqual(0, server.Port);

            var tcp = false;
            var udp = false;
            foreach (var item in server.Servers)
            {
                if (!item.Active) continue;

                if (item.Local.IsTcp)
                    tcp = true;
                else if (!item.Local.IsUnix)
                    udp = true;
            }
            Assert.True(tcp);
            Assert.True(udp);
        }
        finally
        {
            server.Dispose();
            blocker?.Stop();
        }
    }

    private class FakeServer(Int32 port) : IDisposable
    {
        public Int32 Port { get; } = port;

        public Boolean Started { get; private set; }

        public Int32 Disposed { get; private set; }

        public void Start() => Started = true;

        public void Dispose() => Disposed++;
    }
}
