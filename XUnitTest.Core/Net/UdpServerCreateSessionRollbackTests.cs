using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NewLife;
using NewLife.Log;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>UDP 服务端创建会话失败时的回滚测试</summary>
/// <remarks>
/// 服务端在会话入集合之前先 Start 并触发 NewSession（用户代码在此订阅 Received），
/// 这两步抛异常时会话既不在集合、也无人释放；同一端点的每个数据报都会再新建一个，形成持续泄漏。
/// </remarks>
[Collection("Net")]
public class UdpServerCreateSessionRollbackTests
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
    #endregion

    /// <summary>新会话事件抛异常时会话被释放，且服务端继续收包</summary>
    [Fact(DisplayName = "UDP会话回滚_新会话事件抛异常_会话释放且服务端继续收包")]
    public async Task NewSessionThrows_SessionReleasedAndServerAlive()
    {
        using var server = new UdpServer { Port = 0, Log = XTrace.Log };

        var count = 0;
        ISocketSession? failed = null;
        var received = new TaskCompletionSource<Byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);

        server.NewSession += (s, e) =>
        {
            if (Interlocked.Increment(ref count) == 1)
            {
                failed = e.Session;
                throw new InvalidOperationException("测试：拒绝首个UDP会话");
            }
        };
        server.Received += (s, e) => received.TrySetResult(e.GetBytes());
        server.Open();

        using var client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        var ep = new IPEndPoint(IPAddress.Loopback, server.Port);

        // 首个数据报触发业务异常：会话必须被释放，且不得残留在会话集合里
        client.SendTo("first".GetBytes(), ep);
        await WaitUntilAsync(() => failed != null && failed.Disposed && server.Sessions.Count == 0);

        Assert.True(failed!.Disposed);
        Assert.Empty(server.Sessions);

        // 接收环必须照常工作：后续数据报仍能建立正常会话并交付数据
        var payload = "second"u8.ToArray();
        client.SendTo(payload, ep);

        var got = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(payload, got);

        await WaitUntilAsync(() => server.Sessions.Count == 1);
        Assert.Single(server.Sessions);
    }
}
