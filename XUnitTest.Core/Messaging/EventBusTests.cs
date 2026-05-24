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
        Assert.Equal(1, bus.Handlers.Count);

        await bus.PublishAsync(new TestEvent { Message = "once" });
        await receiveTask;

        Assert.Equal(0, bus.Handlers.Count);
    }
}
