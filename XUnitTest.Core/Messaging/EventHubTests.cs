using NewLife.Messaging;
using Xunit;

namespace XUnitTest.Messaging;

public class EventHubTests
{
    private sealed class TestEvent
    {
        public String Message { get; set; } = String.Empty;
    }

    private sealed class TestEventHandler : IEventHandler<TestEvent>
    {
        public String HandledMessage { get; private set; } = String.Empty;

        public Task HandleAsync(TestEvent @event, IEventContext? context, CancellationToken cancellationToken)
        {
            HandledMessage = @event.Message;
            return Task.CompletedTask;
        }
    }

    private sealed class TestEventBus : EventBus<TestEvent>
    {
        public Int32 SubscribeCount { get; private set; }
        public Int32 UnsubscribeCount { get; private set; }
        public Int32 PublishCount { get; private set; }

        public override Boolean Subscribe(IEventHandler<TestEvent> handler, String clientId = "")
        {
            SubscribeCount++;
            return base.Subscribe(handler, clientId);
        }

        public override Boolean Unsubscribe(String clientId = "")
        {
            UnsubscribeCount++;
            return base.Unsubscribe(clientId);
        }

        public override Task<Int32> PublishAsync(TestEvent @event, IEventContext? context = null, CancellationToken cancellationToken = default)
        {
            PublishCount++;
            return base.PublishAsync(@event, context, cancellationToken);
        }
    }

    private sealed class TestEventBusFactory : IEventBusFactory
    {
        public TestEventBus? LastBus { get; private set; }
        public String? LastTopic { get; private set; }
        public String? LastClientId { get; private set; }

        public IEventBus<T> CreateEventBus<T>(String topic, String clientId)
        {
            LastTopic = topic;
            LastClientId = clientId;

            if (typeof(T) == typeof(TestEvent))
            {
                var bus = new TestEventBus();
                LastBus = bus;
                return (IEventBus<T>)(Object)bus;
            }

            throw new NotSupportedException("Only TestEvent is supported by this test factory");
        }
    }

    [Fact]
    public async Task HandleAsync_String_ShouldReturn0_WhenPrefixNotMatched()
    {
        var hub = new EventHub<TestEvent>();

        var rs = await hub.HandleAsync("not-event#topic#client#{\"Message\":\"a\"}", null);

        Assert.Equal(0, rs);
    }

    [Fact]
    public async Task HandleAsync_String_ShouldReturn0_WhenHeaderInvalid()
    {
        var hub = new EventHub<TestEvent>();

        Assert.Equal(0, await hub.HandleAsync("event#", null));
        Assert.Equal(0, await hub.HandleAsync("event#topic#", null));
        Assert.Equal(0, await hub.HandleAsync("event#topic#client", null));
        Assert.Equal(0, await hub.HandleAsync("event#topic#client#", null));
    }

    [Fact]
    public async Task HandleAsync_String_ShouldRouteSubscribeAction()
    {
        var factory = new TestEventBusFactory();
        var hub = new EventHub<TestEvent> { Factory = factory };

        // subscribe 需要在上下文中提供 Handler
        var ctx = new EventContext();
        ctx["Handler"] = new TestEventHandler();

        var rs = await hub.HandleAsync("event#test#c1#subscribe", ctx);

        Assert.Equal(1, rs);
        Assert.Equal("test", factory.LastTopic);
        Assert.Equal("c1", factory.LastClientId);
        Assert.NotNull(factory.LastBus);
        Assert.Equal(1, factory.LastBus!.SubscribeCount);
        Assert.Equal(0, factory.LastBus!.PublishCount);
    }

    [Fact]
    public async Task HandleAsync_String_ShouldRouteUnsubscribeAction_AndRemoveBusWhenEmpty()
    {
        var factory = new TestEventBusFactory();
        var hub = new EventHub<TestEvent> { Factory = factory };

        // Pre-create bus and dispatcher mapping for this topic.
        _ = hub.GetEventBus("test", "seed");

        // subscribe first, ensures bus exists and handler is attached under c1.
        var ctx = new EventContext();
        ctx["Handler"] = new TestEventHandler();
        Assert.Equal(1, await hub.HandleAsync("event#test#c1#subscribe", ctx));
        var bus = factory.LastBus!;

        // unsubscribe should remove handler and then remove bus+dispatcher from hub when empty.
        Assert.Equal(1, await hub.HandleAsync("event#test#c1#unsubscribe", null));
        Assert.Equal(1, bus.UnsubscribeCount);

        // After removal, publishing a normal event with same topic should not hit previous bus.
        Assert.Equal(0, bus.PublishCount);

        var rs = await hub.HandleAsync("event#test#any#{\"Message\":\"hello\"}", null);
        Assert.Equal(0, rs);
        Assert.Equal(0, bus.PublishCount);
    }

    [Fact]
    public async Task DispatchAsync_ShouldPublishJsonEventToBus()
    {
        var factory = new TestEventBusFactory();
        var hub = new EventHub<TestEvent> { Factory = factory };

        // 先显式注册该 topic 的事件总线，确保后续 DispatchAsync(topic,...) 能命中 _eventBuses。
        var bus = (TestEventBus)hub.GetEventBus("test", "seed");

        // subscribe 会创建/注册总线并绑定 handler。
        var ctx = new EventContext();
        ctx["Handler"] = new TestEventHandler();
        Assert.True(await hub.DispatchActionAsync("test", "c1", "subscribe", ctx));

        var rs = await hub.DispatchAsync("test", "sender", new TestEvent { Message = "hi" }, null);

        Assert.True(rs >= 0);
        Assert.Equal(1, bus.PublishCount);
    }

    [Fact]
    public async Task HandleAsync_String_ShouldRouteToRegisteredDispatcher_WhenNoBus()
    {
        var hub = new EventHub<TestEvent>();

        var called = 0;
        var handler = new TestEventHandler();
        hub.Add("test", handler);

        var rs = await hub.HandleAsync("event#test#sender#{\"Message\":\"hi\"}", null);

        // 分发器命中后返回 1
        Assert.Equal(1, rs);
        Assert.Equal("hi", handler.HandledMessage);
    }

    [Fact]
    public async Task DispatchActionAsync_ShouldReturnFalse_WhenActionIsJson()
    {
        var hub = new EventHub<TestEvent>();

        var rs = await hub.DispatchActionAsync("test", "c1", "{\"Message\":\"hi\"}", null);

        Assert.False(rs);
    }

    [Fact]
    public async Task DispatchActionAsync_ShouldReturnFalse_WhenActionUnknown()
    {
        var hub = new EventHub<TestEvent>();

        var rs = await hub.DispatchActionAsync("test", "c1", "unknown", null);

        Assert.False(rs);
    }

    [Fact]
    public async Task DispatchActionAsync_ShouldSubscribeViaFactory()
    {
        var factory = new TestEventBusFactory();
        var hub = new EventHub<TestEvent> { Factory = factory };

        var ctx = new EventContext();
        ctx["Handler"] = new TestEventHandler();

        var rs = await hub.DispatchActionAsync("test", "c1", "subscribe", ctx);

        Assert.True(rs);
        Assert.NotNull(factory.LastBus);
        Assert.Equal(1, factory.LastBus!.SubscribeCount);
    }

    [Fact]
    public async Task DispatchActionAsync_ShouldUnsubscribeAndRemoveBusWhenEmpty()
    {
        var factory = new TestEventBusFactory();
        var hub = new EventHub<TestEvent> { Factory = factory };

        var ctx = new EventContext();
        ctx["Handler"] = new TestEventHandler();

        Assert.True(await hub.DispatchActionAsync("test", "c1", "subscribe", ctx));
        var bus = factory.LastBus!;

        Assert.True(await hub.DispatchActionAsync("test", "c1", "unsubscribe", null));
        Assert.Equal(1, bus.UnsubscribeCount);

        // Now bus should be removed from hub, so another unsubscribe should be false.
        Assert.False(await hub.DispatchActionAsync("test", "c1", "unsubscribe", null));
    }
}
