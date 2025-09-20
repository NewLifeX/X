using NewLife.Caching;
using NewLife.Messaging;
using Xunit;

namespace XUnitTest.Caching;

public class QueueEventBusTests
{
    private sealed class StringHandler(Action<String> onHandle) : IEventHandler<String>
    {
        public Task HandleAsync(String @event, IEventContext<String>? context, CancellationToken cancellationToken)
        {
            onHandle(@event);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task PublishAndConsume_SingleHandler()
    {
        var cache = new MemoryCache();
        var topic = $"queuebus:{Guid.NewGuid():N}";
        var bus = new QueueEventBus<String>(cache, topic);

        var tcs = new TaskCompletionSource<Boolean>(TaskCreationOptions.RunContinuationsAsynchronously);
        var received = new List<String>();
        bus.Subscribe(new StringHandler(s =>
        {
            received.Add(s);
            if (received.Count >= 1) tcs.TrySetResult(true);
        }));

        var rs = await bus.PublishAsync("hello");
        Assert.Equal(1, rs);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(2_000));
        Assert.Same(tcs.Task, completed);
        Assert.Equal(["hello"], received);

        bus.Dispose();
    }

    [Fact]
    public async Task PublishAndConsume_MultipleHandlers()
    {
        var cache = new MemoryCache();
        var topic = $"queuebus:{Guid.NewGuid():N}";
        var bus = new QueueEventBus<String>(cache, topic);

        var tcs = new TaskCompletionSource<Boolean>(TaskCreationOptions.RunContinuationsAsynchronously);
        var count = 0;
        bus.Subscribe(new StringHandler(_ => { if (Interlocked.Increment(ref count) >= 2) tcs.TrySetResult(true); }), "c1");
        bus.Subscribe(new StringHandler(_ => { if (Interlocked.Increment(ref count) >= 2) tcs.TrySetResult(true); }), "c2");

        await bus.PublishAsync("hi");

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(2_000));
        Assert.Same(tcs.Task, completed);
        Assert.Equal(2, count);

        bus.Dispose();
    }

    [Fact]
    public async Task Dispose_ShouldStopConsuming()
    {
        var cache = new MemoryCache();
        var topic = $"queuebus:{Guid.NewGuid():N}";
        var bus = new QueueEventBus<String>(cache, topic);

        var count = 0;
        bus.Subscribe(new StringHandler(_ => Interlocked.Increment(ref count)));

        await bus.PublishAsync("one");

        // 等待第一条被消费
        for (var i = 0; i < 20 && Volatile.Read(ref count) < 1; i++) await Task.Delay(50);
        Assert.True(count >= 1);

        // 释放并确认后台循环退出
        bus.Dispose();

        // 再发布一条，此时后台消费应已停止
        await bus.PublishAsync("two");

        // 稍等，确保不会被消费
        await Task.Delay(200);
        Assert.Equal(1, count);

        // 队列中应仍有未消费数据
        var queue = cache.GetQueue<String>(topic);
        Assert.False(queue.IsEmpty);
        Assert.True(queue.Count >= 1);

        // 清理残留，避免影响其它用例
        _ = queue.Take(queue.Count);
    }

    [Fact]
    public async Task PublishAsync_WithCancelledToken_ReturnsCanceledTask()
    {
        var cache = new MemoryCache();
        var topic = $"queuebus:{Guid.NewGuid():N}";
        var bus = new QueueEventBus<String>(cache, topic);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(async () => await bus.PublishAsync("x", null, cts.Token));

        bus.Dispose();
    }
}
