using NewLife.Messaging;
using Xunit;

namespace XUnitTest.Messaging;

public class EventBusTests
{
    private class TestEvent { public String Message { get; set; } = String.Empty; }

    private class TestEventHandler : IEventHandler<TestEvent>
    {
        public String HandledMessage { get; private set; } = String.Empty;

        public Task HandleAsync(TestEvent @event, IEventContext? context, CancellationToken cancellationToken)
        {
            HandledMessage = @event.Message;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task PublishAsync_ShouldInvokeSubscribedHandler()
    {
        // Arrange
        var bus = new EventBus<TestEvent>();
        var handler = new TestEventHandler();
        bus.Subscribe(handler);

        var testEvent = new TestEvent { Message = "Hello, World!" };

        // Act
        await bus.PublishAsync(testEvent);

        // Assert
        Assert.Equal("Hello, World!", handler.HandledMessage);
    }

    [Fact]
    public async Task Subscribe_ShouldAddHandler()
    {
        // Arrange
        var bus = new EventBus<TestEvent>();
        var handler = new TestEventHandler();

        // Act
        var rs = bus.Subscribe(handler);

        // Assert
        Assert.True(rs);

        var handler2 = new TestEventHandler();
        rs = bus.Subscribe(handler, "222");
        Assert.True(rs);

        var ms = await bus.PublishAsync(new TestEvent { Message = "Hello, World!" });
        Assert.Equal(2, ms);
    }

    [Fact]
    public void Unsubscribe_ShouldRemoveHandler()
    {
        // Arrange
        var bus = new EventBus<TestEvent>();
        var handler = new TestEventHandler();
        bus.Subscribe(handler);

        // Act
        var result = bus.Unsubscribe("");

        // Assert
        Assert.True(result);
    }

    [Fact(DisplayName = "ReceiveAsync 应返回发布的事件")]
    public async Task ReceiveAsync_ShouldReturnEvent_WhenPublished()
    {
        var bus = new EventBus<TestEvent>();
        var testEvent = new TestEvent { Message = "Hello" };

        var receiveTask = bus.ReceiveAsync();
        await bus.PublishAsync(testEvent);
        var result = await receiveTask;

        Assert.Equal("Hello", result.Message);
    }

    [Fact(DisplayName = "ReceiveAsync 应在取消令牌触发时抛出 OperationCanceledException")]
    public async Task ReceiveAsync_ShouldThrow_WhenCancelled()
    {
        var bus = new EventBus<TestEvent>();
        using var cts = new CancellationTokenSource();

        var receiveTask = bus.ReceiveAsync(cts.Token);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => receiveTask);
    }

    [Fact(DisplayName = "ReceiveAsync 超时后应抛出 OperationCanceledException")]
    public async Task ReceiveAsync_ShouldThrow_WhenTimeout()
    {
        var bus = new EventBus<TestEvent>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            bus.ReceiveAsync(TimeSpan.FromMilliseconds(50)));
    }

    [Fact(DisplayName = "ReceiveAsync 多个等待者应全部收到事件（广播语义）")]
    public async Task ReceiveAsync_MultipleWaiters_AllReceive()
    {
        var bus = new EventBus<TestEvent>();
        var testEvent = new TestEvent { Message = "Broadcast" };

        var task1 = bus.ReceiveAsync();
        var task2 = bus.ReceiveAsync();
        await bus.PublishAsync(testEvent);

        var r1 = await task1;
        var r2 = await task2;

        Assert.Equal("Broadcast", r1.Message);
        Assert.Equal("Broadcast", r2.Message);
    }

    [Fact(DisplayName = "ReceiveAsync 完成后应自动取消订阅")]
    public async Task ReceiveAsync_ShouldUnsubscribeAfterReceive()
    {
        var bus = new EventBus<TestEvent>();

        var receiveTask = bus.ReceiveAsync();
        Assert.Single(bus.Handlers);

        await bus.PublishAsync(new TestEvent { Message = "once" });
        await receiveTask;

        Assert.Empty(bus.Handlers);
    }

    private sealed class ThrowingEventHandler : IEventHandler<TestEvent>
    {
        public Task HandleAsync(TestEvent @event, IEventContext? context, CancellationToken cancellationToken)
            => throw new InvalidOperationException("测试异常");
    }

    [Fact(DisplayName = "无订阅者时 PublishAsync 应返回 0")]
    public async Task PublishAsync_NoSubscribers_ReturnsZero()
    {
        var bus = new EventBus<TestEvent>();
        var rs = await bus.PublishAsync(new TestEvent { Message = "x" });
        Assert.Equal(0, rs);
    }

    [Fact(DisplayName = "相同 clientId 重复订阅应替换旧处理器")]
    public void Subscribe_IdempotentByClientId_ReplacesHandler()
    {
        var bus = new EventBus<TestEvent>();
        var h1 = new TestEventHandler();
        var h2 = new TestEventHandler();

        bus.Subscribe(h1, "c1");
        bus.Subscribe(h2, "c1");

        Assert.Single(bus.Handlers);
        Assert.Same(h2, bus.Handlers["c1"]);
    }

    [Fact(DisplayName = "取消不存在的 clientId 应返回 false")]
    public void Unsubscribe_NonExistentClientId_ReturnsFalse()
    {
        var bus = new EventBus<TestEvent>();
        Assert.False(bus.Unsubscribe("nonexistent"));
    }

    [Fact(DisplayName = "ThrowOnHandlerError=false 时处理器异常不影响其他处理器")]
    public async Task PublishAsync_HandlerError_ContinuesOtherHandlers_WhenNotThrowing()
    {
        var bus = new EventBus<TestEvent> { ThrowOnHandlerError = false };
        var failHandler = new ThrowingEventHandler();
        var successHandler = new TestEventHandler();

        bus.Subscribe(failHandler, "fail");
        bus.Subscribe(successHandler, "success");

        var rs = await bus.PublishAsync(new TestEvent { Message = "test" });

        Assert.Equal(1, rs);
        Assert.Equal("test", successHandler.HandledMessage);
    }

    [Fact(DisplayName = "ThrowOnHandlerError=true 时处理器异常应向调用方传播")]
    public async Task PublishAsync_HandlerError_ThrowsWhenConfigured()
    {
        var bus = new EventBus<TestEvent> { ThrowOnHandlerError = true };
        bus.Subscribe(new ThrowingEventHandler(), "fail");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            bus.PublishAsync(new TestEvent { Message = "test" }));
    }

    [Fact(DisplayName = "通过 Action<TEvent> 委托订阅应收到事件")]
    public async Task Subscribe_ActionDelegate_InvokedOnPublish()
    {
        var bus = new EventBus<TestEvent>();
        var received = String.Empty;

        bus.Subscribe<TestEvent>(e => received = e.Message);
        await bus.PublishAsync(new TestEvent { Message = "action" });

        Assert.Equal("action", received);
    }

    [Fact(DisplayName = "通过 Func<TEvent,Task> 委托订阅应收到事件")]
    public async Task Subscribe_AsyncDelegate_InvokedOnPublish()
    {
        var bus = new EventBus<TestEvent>();
        var received = String.Empty;

        bus.Subscribe<TestEvent>(e => { received = e.Message; return Task.CompletedTask; });
        await bus.PublishAsync(new TestEvent { Message = "async-action" });

        Assert.Equal("async-action", received);
    }

    [Fact(DisplayName = "通过 Action<TEvent,IEventContext> 委托订阅应收到上下文")]
    public async Task Subscribe_ActionWithContextDelegate_ReceivesContext()
    {
        var bus = new EventBus<TestEvent>();
        // 在回调内立即捕获 EventBus 引用，避免上下文回池后被 Reset() 清空
        IEventBus? capturedEventBus = null;

        bus.Subscribe<TestEvent>((e, ctx) => capturedEventBus = ctx.EventBus);
        await bus.PublishAsync(new TestEvent { Message = "ctx" });

        Assert.NotNull(capturedEventBus);
        Assert.Same(bus, capturedEventBus);
    }

    [Fact(DisplayName = "相同 clientId 的发送方不收到自己发布的消息")]
    public async Task PublishAsync_SenderExcluded_WhenClientIdMatches()
    {
        var bus = new EventBus<TestEvent>();
        var handler = new TestEventHandler();
        bus.Subscribe(handler, "sender");

        var ctx = new EventContext { ClientId = "sender" };
        var rs = await bus.PublishAsync(new TestEvent { Message = "self" }, ctx);

        Assert.Equal(String.Empty, handler.HandledMessage);
        Assert.Equal(0, rs);
    }

    [Fact(DisplayName = "SubscribeAsync 和 UnsubscribeAsync 应正常工作")]
    public async Task SubscribeAsync_UnsubscribeAsync_Work()
    {
        var bus = new EventBus<TestEvent>();
        var handler = new TestEventHandler();

        Assert.True(await bus.SubscribeAsync(handler, "c1"));
        Assert.Contains("c1", bus.Handlers.Keys);

        Assert.True(await bus.UnsubscribeAsync("c1"));
        Assert.DoesNotContain("c1", bus.Handlers.Keys);
    }
}
