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
}
