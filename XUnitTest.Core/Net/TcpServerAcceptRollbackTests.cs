using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NewLife.Log;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>服务端接受连接失败时的会话回滚测试</summary>
/// <remarks>
/// 会话入集合后，业务回调（CreateHandler/Init/Connected）或会话启动（SSL认证、启动接收环）抛异常时，
/// 会话必须立即移出集合并释放；否则它会一直留在集合里、Disconnected 永不触发、连接悬挂到会话超时。
/// </remarks>
[Collection("Net")]
public class TcpServerAcceptRollbackTests
{
    #region 工具
    private static async Task WaitUntilAsync(Func<Boolean> condition, Int32 timeoutMs = 10_000)
    {
        var sw = Stopwatch.StartNew();
        while (!condition())
        {
            if (sw.ElapsedMilliseconds > timeoutMs) throw new TimeoutException("等待条件超时");

            await Task.Delay(10);
        }
    }

    /// <summary>断言客户端立即感知连接关闭（读到流结束，或对端重置连接）</summary>
    private static async Task AssertClosedAsync(TcpClient client)
    {
        using var cts = new CancellationTokenSource(3_000);
        var buffer = new Byte[1];
        var closed = false;
        try
        {
            closed = await client.GetStream().ReadAsync(buffer, cts.Token) == 0;
        }
        catch (OperationCanceledException) { }
        catch (IOException) { closed = true; }

        Assert.True(closed, "会话释放后客户端应立即感知连接关闭");
    }
    #endregion

    /// <summary>新会话事件抛异常时，会话被移出集合并释放，客户端立即感知连接关闭</summary>
    [Fact(DisplayName = "接受回滚_新会话事件抛异常_会话移出集合并释放")]
    public async Task NewSessionThrows_SessionRolledBack()
    {
        using var server = new TcpServer { Port = 0, Log = XTrace.Log };

        ISocketSession? session = null;
        server.NewSession += (s, e) =>
        {
            session = e.Session;
            throw new InvalidOperationException("业务拒绝该连接");
        };
        server.Start();

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);

        // 等回滚完成：会话已释放，且它已经从会话集合里消失
        await WaitUntilAsync(() => session != null && session.Disposed && server.Sessions.Count == 0);

        Assert.True(session!.Disposed);
        Assert.Empty(server.Sessions);

        // 会话释放后连接必须真的断开
        await AssertClosedAsync(client);
    }

    /// <summary>首个连接被业务拒绝后，服务端仍能正常接受并处理后续连接</summary>
    [Fact(DisplayName = "接受回滚_异常连接被拒后_后续连接仍可正常收发")]
    public async Task NewSessionThrows_ServerStillAccepts()
    {
        using var server = new TcpServer { Port = 0, Log = XTrace.Log };

        var count = 0;
        ISocketSession? failed = null;
        var received = new TaskCompletionSource<Byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);

        server.NewSession += (s, e) =>
        {
            if (Interlocked.Increment(ref count) == 1)
            {
                failed = e.Session;
                throw new InvalidOperationException("业务拒绝首个连接");
            }

            e.Session.Received += (ss, ee) => received.TrySetResult(ee.GetBytes());
        };
        server.Start();

        // 首个连接触发业务异常
        using (var client1 = new TcpClient())
        {
            await client1.ConnectAsync(IPAddress.Loopback, server.Port);
            await WaitUntilAsync(() => failed != null && failed.Disposed && server.Sessions.Count == 0);
        }

        // 后续连接必须照常建立并收发数据
        using var client2 = new TcpClient();
        await client2.ConnectAsync(IPAddress.Loopback, server.Port);

        await WaitUntilAsync(() => server.Sessions.Count == 1);

        var active = server.Sessions.Values.First();
        Assert.False(active.Disposed);

        var payload = "hello after rollback"u8.ToArray();
        await client2.GetStream().WriteAsync(payload);

        var got = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(payload, got);
    }

    /// <summary>创建处理器抛异常时，网络会话与底层连接都必须被回收</summary>
    [Fact(DisplayName = "接受回滚_创建处理器抛异常_网络会话与连接均被回收")]
    public async Task CreateHandlerThrows_SessionAndConnectionReleased()
    {
        using var server = new ThrowingHandlerServer { Port = 0, Log = XTrace.Log };
        server.Start();

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);

        // 等回滚完成：网络会话已释放，网络层与Socket层的会话集合、会话计数全部归零
        await WaitUntilAsync(() => server.Created != null && server.Created.Disposed
            && server.Sessions.Count == 0 && server.SessionCount == 0
            && server.Servers[0].Sessions.Count == 0);

        Assert.True(server.Created!.Disposed);
        Assert.Empty(server.Sessions);
        Assert.Equal(0, server.SessionCount);
        Assert.Empty(server.Servers[0].Sessions);

        // 连接必须真的断开
        await AssertClosedAsync(client);
    }

    /// <summary>创建网络处理器的动作会抛异常的服务端，用于验证会话启动失败的回滚</summary>
    private sealed class ThrowingHandlerServer : NetServer
    {
        /// <summary>最近一次创建的网络会话</summary>
        public INetSession? Created;

        public override INetHandler? CreateHandler(INetSession session) => throw new InvalidOperationException("测试：创建网络处理器失败");

        protected override INetSession CreateSession(ISocketSession session)
        {
            var ns = base.CreateSession(session);
            Created = ns;

            return ns;
        }
    }
}
