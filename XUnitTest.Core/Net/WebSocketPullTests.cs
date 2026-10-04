using System.ComponentModel;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>WebSocket 客户端拉取 API（ReceiveMessageAsync）的等待与关闭语义测试。不涉及真实连接</summary>
public class WebSocketPullTests
{
    /// <summary>可直接触发关闭置位的探针客户端</summary>
    /// <remarks>调用 OnCloseAsync 而不走 CloseAsync，构造“关闭已置位、会话仍标记为活动”的瞬间——
    /// 即关闭流程与等待登记竞争的最坏时刻（真实关闭中 Active 会在 OnCloseAsync 返回后才置为 false）</remarks>
    private sealed class CloseProbeClient : WebSocketClient
    {
        public Task<Boolean> CloseDirectlyAsync() => OnCloseAsync("probe", default);
    }

    [Fact]
    [DisplayName("ws拉取_关闭置位后登记等待_立即返回null")]
    public async Task WaitAfterClosed_ReturnsNull()
    {
        using var client = new CloseProbeClient { Active = true };

        // 关闭已置位：晚一步登记等待的调用方不能再等下去（旧实现里此次登记永远等不到唤醒）
        await client.CloseDirectlyAsync();

        var msg = await client.ReceiveMessageAsync().WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Null(msg);
    }

    [Fact]
    [DisplayName("ws拉取_等待中关闭_唤醒后返回null不重登记")]
    public async Task WaitWhileClosed_ReturnsNull()
    {
        using var client = new CloseProbeClient { Active = true };

        // 先进入等待（调用返回时已完成登记），关闭置位只唤醒一次
        var wait = client.ReceiveMessageAsync();

        await client.CloseDirectlyAsync();

        var msg = await wait.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Null(msg);
    }

    [Fact]
    [DisplayName("ws拉取_未关闭且无消息_继续等待不误返回")]
    public async Task WaitWhileOpen_KeepsWaiting()
    {
        using var client = new CloseProbeClient { Active = true };

        var wait = client.ReceiveMessageAsync();
        await Task.Delay(200);
        Assert.False(wait.IsCompleted, "未关闭且无消息时不应提前返回");

        // 关闭后必须返回，等待不能被丢弃
        await client.CloseDirectlyAsync();
        Assert.Null(await wait.WaitAsync(TimeSpan.FromSeconds(3)));
    }

    [Fact]
    [DisplayName("ws拉取_取消令牌_返回null")]
    public async Task Cancel_ReturnsNull()
    {
        using var client = new CloseProbeClient { Active = true };

        using var cts = new CancellationTokenSource(100);
        var msg = await client.ReceiveMessageAsync(cts.Token).WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Null(msg);
    }

    /// <summary>可直接执行打开流程的探针客户端（不置 Active）</summary>
    /// <remarks>用于构造“握手完成、心跳定时器已建，但会话仍未激活”的状态：OpenAsync 要到 OnOpenAsync 返回后才置位 Active</remarks>
    private sealed class RawOpenClient : WebSocketClient
    {
        public RawOpenClient(Uri uri) : base(uri) { }

        public Task<Boolean> OpenRawAsync(CancellationToken cancellationToken = default) => OnOpenAsync(cancellationToken);
    }

    [Fact]
    [DisplayName("ws客户端_未激活时销毁_心跳定时器一并释放")]
    public async Task DisposeBeforeActive_ReleasesHeartbeatTimer()
    {
        // 服务端：接受 WebSocket 握手
        var server = new NewLife.Http.HttpServer { Port = 0 };
        server.Map("/ws", new NewLife.Http.WebSocketHandler());
        server.Start();

        try
        {
            var client = new RawOpenClient(new Uri($"ws://127.0.0.1:{server.Port}/ws"));

            // 握手完成、定时器已建，但 Active 仍为 false（OpenAsync 才会置位）
            Assert.True(await client.OpenRawAsync());
            Assert.False(client.Active);

            var timer = GetHeartbeatTimer(client);
            Assert.NotNull(timer);
            Assert.NotEqual(0, timer!.Id);      // 已注册进调度器

            client.Dispose();

            // 定时器必须一并释放：否则 TimerX 持有会话对象，且每次到期都在已释放会话上发送 Ping
            Assert.Equal(0, timer.Id);          // 已从调度器摘除
            Assert.Null(GetHeartbeatTimer(client));
        }
        finally
        {
            server.Dispose();
        }
    }

    /// <summary>反射读取心跳定时器（私有字段，仅用于验证释放语义）</summary>
    private static NewLife.Threading.TimerX? GetHeartbeatTimer(WebSocketClient client) =>
        (NewLife.Threading.TimerX?)typeof(WebSocketClient)
            .GetField("_timer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.GetValue(client);
}
