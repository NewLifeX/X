using NewLife.Messaging;
using Xunit;

namespace XUnitTest.Messaging;

public class EventBusTests
{
    private class TestEvent { public String Message { get; set; } = String.Empty; }

    private class TestEventHandler : IEventHandler<TestEvent>
    {
        public String HandledMessage { get; private set; } = String.Empty;

        public Task HandleAsync(TestEvent @event, IEventContext<TestEvent> context)
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
}
