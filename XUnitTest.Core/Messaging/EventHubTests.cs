using NewLife.Messaging;
using Xunit;

namespace XUnitTest.Messaging;

public class EventHubTests
{
    private sealed class TestEvent
    {
        public String Message { get; set; } = String.Empty;
    }

    private sealed class TestEventBus : EventBus<TestEvent>
    {
        public Int32 SubscribeCount { get; private set; }
        public Int32 UnsubscribeCount { get; private set; }
        public Int32 PublishCount { get; private set; }

        public override Boolean Subscribe(IEventHandler<TestEvent> handler, String? clientId = null)
        {
            SubscribeCount++;
            return base.Subscribe(handler, clientId ?? String.Empty);
        }

        public override Boolean Unsubscribe(String? clientId = null)
        {
            UnsubscribeCount++;
            return base.Unsubscribe(clientId ?? String.Empty);
        }

        public override Task<Int32> PublishAsync(TestEvent @event, IEventContext<TestEvent>? context = null, CancellationToken cancellationToken = default)
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
    public async Task DispatchAsync_String_ShouldReturn0_WhenPrefixNotMatched()
    {
        var hub = new EventHub<TestEvent>();

        var rs = await hub.DispatchAsync("not-event#topic#client#{\"Message\":\"a\"}");

        Assert.Equal(0, rs);
    }

    [Fact]
    public async Task DispatchAsync_String_ShouldReturn0_WhenHeaderInvalid()
    {
        var hub = new EventHub<TestEvent>();

        Assert.Equal(0, await hub.DispatchAsync("event#"));
        Assert.Equal(0, await hub.DispatchAsync("event#topic#"));
        Assert.Equal(0, await hub.DispatchAsync("event#topic#client"));
        Assert.Equal(0, await hub.DispatchAsync("event#topic#client#"));
    }

    [Fact]
    public async Task DispatchAsync_String_ShouldRouteSubscribeAction()
    {
        var factory = new TestEventBusFactory();
        var hub = new EventHub<TestEvent> { Factory = factory };

        var rs = await hub.DispatchAsync("event#test#c1#subscribe");

        Assert.Equal(1, rs);
        Assert.Equal("test", factory.LastTopic);
        Assert.Equal("c1", factory.LastClientId);
        Assert.NotNull(factory.LastBus);
        Assert.Equal(1, factory.LastBus!.SubscribeCount);
        Assert.Equal(0, factory.LastBus!.PublishCount);
    }

    [Fact]
    public async Task DispatchAsync_String_ShouldRouteUnsubscribeAction_AndRemoveBusWhenEmpty()
    {
        var factory = new TestEventBusFactory();
        var hub = new EventHub<TestEvent> { Factory = factory };

        // Pre-create bus and dispatcher mapping for this topic.
        _ = hub.GetEventBus("test", "seed");

        // subscribe first, ensures bus exists and handler is attached under c1.
        Assert.Equal(1, await hub.DispatchAsync("event#test#c1#subscribe"));
        var bus = factory.LastBus!;

        // unsubscribe should remove handler and then remove bus+dispatcher from hub when empty.
        Assert.Equal(1, await hub.DispatchAsync("event#test#c1#unsubscribe"));
        Assert.Equal(1, bus.UnsubscribeCount);

        // After removal, publishing a normal event with same topic should not hit previous bus.
        Assert.Equal(0, bus.PublishCount);

        var rs = await hub.DispatchAsync("event#test#any#{\"Message\":\"hello\"}");
        Assert.Equal(0, rs);
        Assert.Equal(0, bus.PublishCount);
    }

    [Fact]
    public async Task DispatchAsync_String_ShouldPublishJsonEventToBus()
    {
        var factory = new TestEventBusFactory();
        var hub = new EventHub<TestEvent> { Factory = factory };

        // 先显式注册该 topic 的事件总线，确保后续 DispatchAsync(topic,...) 能命中 _eventBuses。
        var bus = (TestEventBus)hub.GetEventBus("test", "seed");

        // subscribe 会创建/注册总线并绑定 handler。
        Assert.True(await hub.DispatchActionAsync("test", "c1", "subscribe"));

        var rs = await hub.DispatchAsync("test", "sender", new TestEvent { Message = "hi" });

        Assert.True(rs >= 0);
        Assert.Equal(1, bus.PublishCount);
    }

    [Fact]
    public async Task DispatchAsync_String_ShouldRouteToRegisteredCallback_WhenNoBus()
    {
        var hub = new EventHub<TestEvent>();

        var called = 0;
        hub.Add("test", (@event, _) =>
        {
            called++;
            Assert.Equal("hi", @event.Message);
            return Task.FromResult(2);
        });

        var rs = await hub.DispatchAsync("event#test#sender#{\"Message\":\"hi\"}");

        Assert.Equal(2, rs);
        Assert.Equal(1, called);
    }

    [Fact]
    public async Task DispatchActionAsync_ShouldReturnFalse_WhenActionIsJson()
    {
        var hub = new EventHub<TestEvent>();

        var rs = await hub.DispatchActionAsync("test", "c1", "{\"Message\":\"hi\"}");

        Assert.False(rs);
    }

    [Fact]
    public async Task DispatchActionAsync_ShouldReturnFalse_WhenActionUnknown()
    {
        var hub = new EventHub<TestEvent>();

        var rs = await hub.DispatchActionAsync("test", "c1", "unknown");

        Assert.False(rs);
    }

    [Fact]
    public async Task DispatchActionAsync_ShouldSubscribeViaFactory()
    {
        var factory = new TestEventBusFactory();
        var hub = new EventHub<TestEvent> { Factory = factory };

        var rs = await hub.DispatchActionAsync("test", "c1", "subscribe");

        Assert.True(rs);
        Assert.NotNull(factory.LastBus);
        Assert.Equal(1, factory.LastBus!.SubscribeCount);
    }

    [Fact]
    public async Task DispatchActionAsync_ShouldUnsubscribeAndRemoveBusWhenEmpty()
    {
        var factory = new TestEventBusFactory();
        var hub = new EventHub<TestEvent> { Factory = factory };

        Assert.True(await hub.DispatchActionAsync("test", "c1", "subscribe"));
        var bus = factory.LastBus!;

        Assert.True(await hub.DispatchActionAsync("test", "c1", "unsubscribe"));
        Assert.Equal(1, bus.UnsubscribeCount);

        // Now bus should be removed from hub, so another unsubscribe should be false.
        Assert.False(await hub.DispatchActionAsync("test", "c1", "unsubscribe"));
    }
}
