using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NewLife;
using NewLife.Log;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>会话生命周期权威契约测试</summary>
/// <remarks>
/// 会话“还在不在、活没活”同时记在多处状态里，本组用例把《网络库架构》§10 定下的口径钉住：
/// Disposed 是终态权威、Sessions 是存在性权威、Active 只表示传输可用；
/// “Active=false 但未释放、仍在集合里”只能是必然收敛的瞬态。
/// </remarks>
[Collection("Net")]
public class SessionAuthorityContractTests
{
    #region 工具
    /// <summary>等待条件成立，超时抛异常</summary>
    private static async Task WaitUntilAsync(Func<Boolean> condition, Int32 timeoutMs = 10_000)
    {
        var sw = Stopwatch.StartNew();
        while (!condition())
        {
            if (sw.ElapsedMilliseconds > timeoutMs) throw new TimeoutException("等待条件超时");

            await Task.Delay(10);
        }
    }
    #endregion

    /// <summary>服务端会话由宿主接管：构造时即视为活动，Open 幂等成功（不执行打开流程）</summary>
    [Fact(DisplayName = "生命周期权威_服务端会话构造即为活动_打开幂等成功")]
    public async Task ServerSession_ActiveOnConstruction_OpenIdempotent()
    {
        using var server = new TcpServer { Port = 0, Log = XTrace.Log };

        ISocketSession? session = null;
        server.NewSession += (s, e) => session = e.Session;
        server.Start();

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);

        await WaitUntilAsync(() => session != null);

        var tcp = Assert.IsType<TcpSession>(session);
        Assert.True(tcp.Active, "服务端会话在构造时即置为活动（表示连接已被接受）");

        // Active 已为 true，打开会直接短路；若真进了 OnOpenAsync，服务端会话会返回 false
        Assert.True(tcp.Open(), "服务端会话的打开是幂等成功");
        Assert.True(tcp.Active);
    }

    /// <summary>服务端会话关闭后必须收敛为已释放并移出会话集合</summary>
    [Fact(DisplayName = "生命周期权威_服务端会话释放后_必出会话集合")]
    public async Task ServerSession_Disposed_RemovedFromCollection()
    {
        using var server = new TcpServer { Port = 0, Log = XTrace.Log };

        ISocketSession? session = null;
        server.NewSession += (s, e) => session = e.Session;
        server.Start();

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);

        await WaitUntilAsync(() => session != null && server.Sessions.Count == 1);

        var tcp = Assert.IsType<TcpSession>(session);

        // 关闭过程中允许出现“活动为假、尚未释放、仍在集合里”的瞬态，但必须收敛
        tcp.Close("契约用例");

        await WaitUntilAsync(() => tcp.Disposed && server.Sessions.Count == 0);

        Assert.True(tcp.Disposed);
        Assert.False(tcp.Active);
        Assert.Empty(server.Sessions);
    }

    /// <summary>会话集合只收未释放会话，已释放的会话不得再被登记</summary>
    [Fact(DisplayName = "生命周期权威_已释放会话_不再被会话集合接受")]
    public async Task DisposedSession_RejectedByCollection()
    {
        using var server = new TcpServer { Port = 0, Log = XTrace.Log };

        ISocketSession? session = null;
        server.NewSession += (s, e) => session = e.Session;
        server.Start();

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);

        await WaitUntilAsync(() => session != null && server.Sessions.Count == 1);

        var tcp = Assert.IsType<TcpSession>(session);
        tcp.Close("契约用例");
        await WaitUntilAsync(() => tcp.Disposed && server.Sessions.Count == 0);

        var sessions = (SessionCollection)server.Sessions;
        Assert.False(sessions.Add(tcp), "已释放的会话不应被重新登记");
        Assert.Empty(sessions);
    }

    /// <summary>底层连接不可用时，服务端会话仍必须释放并出集合，不能留在集合里等超时清理</summary>
    /// <remarks>实测该场景不会进入 OnCloseAsync 的异常分支（接收环错误路径先一步收敛），
    /// 因此本用例锁定的是“无论如何都会收敛到已释放”的最终契约，无负向验证</remarks>
    [Fact(DisplayName = "生命周期权威_关闭动作失败_服务端会话仍被释放")]
    public async Task ServerSession_CloseFails_StillDisposed()
    {
        using var server = new TcpServer { Port = 0, Log = XTrace.Log };

        ISocketSession? session = null;
        server.NewSession += (s, e) => session = e.Session;
        server.Start();

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);

        await WaitUntilAsync(() => session != null && server.Sessions.Count == 1);

        var tcp = Assert.IsType<TcpSession>(session);

        // 制造“关闭动作无法成功”的现场：先把底层套接字释放
        tcp.Client!.Dispose();

        tcp.Close("契约用例-关闭失败");

        // 无论关闭动作是否成功，服务端会话都必须收敛到终态并移出集合
        await WaitUntilAsync(() => tcp.Disposed && server.Sessions.Count == 0);

        Assert.True(tcp.Disposed);
        Assert.False(tcp.Active);
        Assert.Empty(server.Sessions);
    }

    /// <summary>对端 RST 断开时，服务端会话必须及时释放并出集合（不能只靠会话超时清理兜底）</summary>
    [Fact(DisplayName = "生命周期权威_对端重置连接_服务端会话及时释放")]
    public async Task ServerSession_PeerReset_SessionReleased()
    {
        using var server = new TcpServer { Port = 0, Log = XTrace.Log };

        ISocketSession? session = null;
        server.NewSession += (s, e) => session = e.Session;
        server.Start();

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);

        await WaitUntilAsync(() => session != null && server.Sessions.Count == 1);

        var tcp = Assert.IsType<TcpSession>(session);

        // LingerState(true, 0)：客户端关闭时直接发 RST 而不是 FIN，服务端接收环随即报 ConnectionReset
        client.LingerState = new LingerOption(true, 0);
        client.Close();

        // 不缩短 SessionTimeout，用等待窗口区分“立即释放”与“等超时清理”
        await WaitUntilAsync(() => tcp.Disposed && server.Sessions.Count == 0, 5_000);

        Assert.True(tcp.Disposed);
        Assert.False(tcp.Active);
        Assert.Empty(server.Sessions);
    }

    /// <summary>登记会话时，会话数与集合数一致；客户端断开后两者一起归零</summary>
    [Fact(DisplayName = "生命周期权威_登记会话时_会话数与集合数一致")]
    public async Task NetServer_SessionCount_MatchesCollection()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            UseSession = true,
            Log = XTrace.Log,
        };
        server.Start();

        var client1 = new TcpClient();
        var client2 = new TcpClient();
        try
        {
            await client1.ConnectAsync(IPAddress.Loopback, server.Port);
            await client2.ConnectAsync(IPAddress.Loopback, server.Port);

            await WaitUntilAsync(() => server.SessionCount == 2 && server.Sessions.Count == 2);

            Assert.Equal(2, server.SessionCount);
            Assert.Equal(2, server.Sessions.Count);

            client1.Close();
            client2.Close();

            await WaitUntilAsync(() => server.SessionCount == 0 && server.Sessions.Count == 0);

            Assert.Equal(0, server.SessionCount);
            Assert.Empty(server.Sessions);
        }
        finally
        {
            client1.Dispose();
            client2.Dispose();
        }
    }

    /// <summary>不登记会话时集合恒空，但会话计数仍反映真实会话数</summary>
    [Fact(DisplayName = "生命周期权威_不登记会话时_集合为空但计数仍准确")]
    public async Task NetServer_UseSessionFalse_CountStillAccurate()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            UseSession = false,
            Log = XTrace.Log,
        };
        server.Start();

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);

        await WaitUntilAsync(() => server.SessionCount == 1);

        // 计数权威是 _SessionCount 计数器，与是否登记进会话集合无关
        Assert.Equal(1, server.SessionCount);
        Assert.Empty(server.Sessions);

        client.Close();

        await WaitUntilAsync(() => server.SessionCount == 0);
        Assert.Equal(0, server.SessionCount);
    }

    /// <summary>统计输出不得反写会话计数：不登记会话（UseSession=false）时字典恒空，不能把计数清零</summary>
    [Fact(DisplayName = "生命周期权威_统计输出不反写会话计数")]
    public async Task NetServer_GetStat_DoesNotOverwriteSessionCount()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            UseSession = false,
            Log = XTrace.Log,
        };
        server.Start();

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);

        await WaitUntilAsync(() => server.SessionCount == 1);
        Assert.Empty(server.Sessions);

        var stat = server.GetStat();
        Assert.False(stat.IsNullOrEmpty(), "有会话时统计输出不应为空");

        // 权威计数是 _SessionCount 计数器，不因统计输出而改变
        Assert.Equal(1, server.SessionCount);
    }
}
