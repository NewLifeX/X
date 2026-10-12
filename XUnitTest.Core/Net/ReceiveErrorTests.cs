using System.Net;
using System.Net.Sockets;
using NewLife;
using NewLife.Data;
using NewLife.Net;
using Xunit;
using XUnitTest.Integration;

namespace XUnitTest.Net;

/// <summary>接收错误处理契约</summary>
[Collection("Integration")]
public class ReceiveErrorTests
{
    /// <summary>探针服务器：暴露 protected 的接收槽释放入口</summary>
    private sealed class ProbeUdpServer : UdpServer
    {
        public void Release(SocketAsyncEventArgs se, String reason) => ReleaseRecv(se, reason);
    }
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

        // UDP 在并行满载时可能丢包，重发直到收到回显
        Byte[]? result = null;
        var end = Environment.TickCount64 + 10_000;
        while (result == null && Environment.TickCount64 < end)
        {
            await client.SendAsync(payload, payload.Length, remote);

            try
            {
                var rs = await client.ReceiveAsync().WaitAsync(TimeSpan.FromMilliseconds(500));
                result = rs.Buffer;
            }
            catch (TimeoutException) { }
        }

        Assert.NotNull(result);
        Assert.Equal(payload, result);
    }

    [Fact(DisplayName = "接收槽释放_同一事件参数重复释放_只生效一次")]
    public void ReleaseRecv_IsIdempotent()
    {
        using var server = new ProbeUdpServer { Port = 0, MaxAsync = 2 };
        server.Open();
        Assert.True(server.IsReceiving);

        // 模拟 StartReceive 抛 ObjectDisposedException 冒泡回 ProcessEvent 的 catch 再次释放的路径
        var owner = new OwnerPacket(16);
        var se = new SocketAsyncEventArgs { UserToken = new RecvSlot(100, owner) };
        server.Release(se, "first");
        server.Release(se, "second");

        // 只递减一次：另一个接收槽仍在收。重复释放两次会让计数归零，拉取直读的互斥判定随之失效
        Assert.True(server.IsReceiving);
        Assert.Throws<ObjectDisposedException>(() => owner.Buffer);
    }

    [Fact(DisplayName = "接收槽释放_事件参数无接收槽_抛异常")]
    public void ReleaseRecv_MissingSlot_Throws()
    {
        using var server = new ProbeUdpServer { Port = 0, MaxAsync = 2 };
        server.Open();

        // 接收事件参数一律由 StartReceive 创建并挂接收槽；裸事件参数属契约破坏，直接抛错暴露
        var se = new SocketAsyncEventArgs();
        Assert.Throws<InvalidOperationException>(() => server.Release(se, "no-slot"));

        // 拒绝释放不影响其它接收槽
        Assert.True(server.IsReceiving);
    }

    [Fact(DisplayName = "接收槽释放_首释槽中无句柄_抛异常")]
    public void ReleaseRecv_MissingPacket_Throws()
    {
        using var server = new ProbeUdpServer { Port = 0, MaxAsync = 2 };
        server.Open();

        // 槽中句柄随缓冲借出时创建、此后一直挂槽；首释即缺失说明契约被破坏（如轮末换新缓冲失败）
        var orphan = new OwnerPacket(16);
        var se = new SocketAsyncEventArgs { UserToken = new RecvSlot(0, orphan) { Packet = null } };
        Assert.Throws<InvalidOperationException>(() => server.Release(se, "no-packet"));

        Assert.True(server.IsReceiving);

        orphan.Dispose();
    }

    [Fact(DisplayName = "接收槽释放_计数归零_释放并置空套接字")]
    public void ReleaseRecv_CountReachesZero_ReleasesClient()
    {
        using var server = new ProbeUdpServer { Port = 0, MaxAsync = 2 };
        server.Open();
        Assert.True(server.IsReceiving);

        var socket = server.Client;
        Assert.NotNull(socket);

        // 释放全部接收槽：计数归零代表接收环彻底停止，套接字应被释放并置空
        server.Release(new SocketAsyncEventArgs { UserToken = new RecvSlot(0, new OwnerPacket(16)) }, "drain-1");
        server.Release(new SocketAsyncEventArgs { UserToken = new RecvSlot(1, new OwnerPacket(16)) }, "drain-2");

        Assert.False(server.IsReceiving);
        Assert.Null(server.Client);
        Assert.True(socket.SafeHandle.IsClosed);

        // 计数已归零后的多余释放：不抛异常，也不影响已收口状态
        server.Release(new SocketAsyncEventArgs { UserToken = new RecvSlot(2, new OwnerPacket(16)) }, "drain-again");
        Assert.Null(server.Client);
    }

    [Fact(DisplayName = "接收槽释放_缓冲仍被共享切片持有_只释放自身引用不归还")]
    public void ReleaseRecv_SharedSlice_DoesNotReturnBuffer()
    {
        using var server = new ProbeUdpServer { Port = 0 };

        var owner = new OwnerPacket(64);
        var view = owner.Slice(0, 16);
        Assert.Equal(2, view.RefCount);

        var se = new SocketAsyncEventArgs { UserToken = new RecvSlot(0, owner) };
        server.Release(se, "shared");

        // 缓冲仍在被共享切片使用：本句柄只释放自己的引用，缓冲留给最后一个句柄归还。
        // 旧实现在该分支 Detach 抛异常被静默吞掉，本句柄的引用从未释放，池缓冲永久漏出
        Assert.Equal(1, view.RefCount);
        Assert.Equal(16, view.Length);

        view.Dispose();
    }

    [Fact(DisplayName = "接收槽释放_独占槽句柄_释放句柄自身")]
    public void ReleaseRecv_SoleHandle_DisposesHandle()
    {
        using var server = new ProbeUdpServer { Port = 0 };

        var owner = new OwnerPacket(64);
        Assert.Equal(1, owner.RefCount);

        // 接收槽句柄随缓冲借出时创建，是缓冲的唯一持有者
        var se = new SocketAsyncEventArgs { UserToken = new RecvSlot(0, owner) };
        server.Release(se, "sole");

        // 释放槽句柄即完成归还：引用归零、缓冲随 ArrayOwner 回池（不再 Detach 后手工归还）
        Assert.Equal(0, owner.RefCount);
        Assert.Throws<ObjectDisposedException>(() => owner.Buffer);
    }

    [Fact(DisplayName = "接收槽释放_槽句柄带链_连带释放链上各段")]
    public void ReleaseRecv_ChainHandle_DisposesWholeChain()
    {
        using var server = new ProbeUdpServer { Port = 0 };

        var head = new OwnerPacket(64);
        var tail = new OwnerPacket(32);
        head.Next = tail;

        var se = new SocketAsyncEventArgs { UserToken = new RecvSlot(0, head) };
        server.Release(se, "chain");

        // 槽句柄带链时同样直接释放：头节点归零归还缓冲，链上各段随 Dispose 递归释放，无需单独判别 Next
        Assert.Throws<ObjectDisposedException>(() => head.Buffer);
        Assert.Throws<ObjectDisposedException>(() => tail.Buffer);
    }

    [Fact(DisplayName = "UDP接收错误_报文超出缓冲_提示一次且不再偷偷加倍")]
    public void UdpReceiveError_MessageSize_HintOnce()
    {
        var log = new TestLog();
        using var server = new UdpServer { Port = 0, MaxAsync = 2, BufferSize = 1024, Log = log };
        server.Open();

        var origin = server.BufferSize;

        var se = new SocketAsyncEventArgs
        {
            SocketError = SocketError.MessageSize,
            RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 12345),
        };
        Assert.False(server.OnReceiveError(se));

        // 报文超出接收缓冲是"内核已丢弃该数据报"，必须可见，且逐包提示会刷屏，只提示一次
        Assert.Equal(1, log.Count("BufferSize=65536"));

        Assert.False(server.OnReceiveError(se));
        Assert.Equal(1, log.Count("BufferSize=65536"));

        // 旧实现按次翻倍 BufferSize：对已建接收槽无效（槽缓冲只在换新路径变），只留下"会自动长大"的错觉
        Assert.Equal(origin, server.BufferSize);
        Assert.True(server.Active);
    }

    [Fact(DisplayName = "UDP接收错误_非报文级错误_记日志保持可见")]
    public void UdpReceiveError_OtherError_KeepsVisible()
    {
        var log = new TestLog();
        using var server = new UdpServer { Port = 0, MaxAsync = 2, Log = log };
        server.Open();

        // 基类只在 OnReceiveError 返回 true 时才记日志并触发 Error 事件；UDP 恒返回 false（保接收槽），
        // 故必须自行补日志，否则整条接收链的错误全部静默
        var se = new SocketAsyncEventArgs
        {
            SocketError = SocketError.NetworkDown,
            RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 12345),
        };
        Assert.False(server.OnReceiveError(se));

        Assert.True(log.Contains("ReceiveAsyncError"));
    }
}
