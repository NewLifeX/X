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

    /// <summary>业务在 NewSession 中释放会话（拒绝该端点）时，该会话被释放，不会被当成会话集合里的会话</summary>
    /// <remarks>
    /// 已释放的会话不会被会话集合接管（<c>SessionCollection.Add</c> 拒绝已释放的会话），此时它既不能留在无人释放的状态，
    /// 也不该作为"该端点的会话"交给调用方。注意同一数据报会取两次会话（<c>SessionBase.ProcessReceive</c> 先 <c>OnPreReceive</c> 再 <c>OnReceive</c>），
    /// 被拒绝的是前者，后者才进集合，因此这里断言的是"被拒绝者被释放且不是集合里的那个"。
    /// </remarks>
    [Fact(DisplayName = "UDP会话回滚_新会话被业务释放_被释放且不进入会话集合")]
    public async Task NewSessionReleasesSession_ReleasedAndNotInCollection()
    {
        using var server = new UdpServer { Port = 0, Log = XTrace.Log };

        var count = 0;
        ISocketSession? rejected = null;
        var payload = "second"u8.ToArray();
        var got = new TaskCompletionSource<Byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);

        server.NewSession += (s, e) =>
        {
            if (Interlocked.Increment(ref count) == 1)
            {
                rejected = e.Session;

                // 业务侧拒绝该端点：释放会话。已释放的会话不可能再进会话集合
                e.Session.Dispose();
            }
        };
        server.Received += (s, e) =>
        {
            if (e.GetBytes() is { } buf && buf.SequenceEqual(payload)) got.TrySetResult(buf);
        };
        server.Open();

        using var client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        var ep = new IPEndPoint(IPAddress.Loopback, server.Port);

        // 首个数据报触发拒绝：被拒绝的会话必须已被释放，且不得成为会话集合里的那个会话
        client.SendTo("first".GetBytes(), ep);
        await WaitUntilAsync(() => rejected != null && count >= 2 && server.Sessions.Count == 1);

        Assert.True(rejected!.Disposed);
        Assert.NotSame(rejected, server.Sessions.Values.First());

        // 接收环不受影响：后续数据报继续由该端点的会话交付
        client.SendTo(payload, ep);

        var buf = await got.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(payload, buf);
        Assert.Single(server.Sessions);
    }
}
