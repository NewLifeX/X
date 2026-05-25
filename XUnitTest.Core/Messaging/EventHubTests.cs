using NewLife;
using NewLife.Data;
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

    [Fact(DisplayName = "Hub.ReceiveAsync 接收后应自动清理空闲总线")]
    public async Task Hub_ReceiveAsync_ShouldAutoCleanupEmptyBus()
    {
        var hub = new EventHub<TestEvent>();

        var receiveTask = hub.ReceiveAsync("device-001");
        await hub.PublishAsync("device-001", new TestEvent { Message = "ok" });
        await receiveTask;

        Assert.False(hub.TryGetBus<TestEvent>("device-001", out _));
    }

    [Fact(DisplayName = "Hub.ReceiveAsync 有长期订阅者时不清理总线")]
    public async Task Hub_ReceiveAsync_ShouldNotCleanup_WhenOtherSubscribersExist()
    {
        var hub = new EventHub<TestEvent>();

        // 添加永久订阅者
        var permanentBus = hub.GetEventBus("device-002");
        permanentBus.Subscribe(new TestEventHandler(), "permanent");

        var receiveTask = hub.ReceiveAsync("device-002");
        await hub.PublishAsync("device-002", new TestEvent { Message = "ok" });
        await receiveTask;

        // permanent 订阅者仍在，总线不应被清理
        Assert.True(hub.TryGetBus<TestEvent>("device-002", out _));
    }

    [Fact(DisplayName = "Hub.PublishAsync 应将事件分发给等待的 ReceiveAsync")]
    public async Task Hub_PublishAsync_ShouldDispatchToWaiter()
    {
        var hub = new EventHub<TestEvent>();

        var receiveTask = hub.ReceiveAsync("cmd-001");
        await hub.PublishAsync("cmd-001", new TestEvent { Message = "done" });
        var result = await receiveTask;

        Assert.Equal("done", result.Message);
    }

    private sealed class TestExtendContext : IEventContext, IExtend
    {
        public IEventBus EventBus { get; set; } = null!;
        public IDictionary<String, Object?> Items { get; } = new Dictionary<String, Object?>();

        public Object? this[String key]
        {
            get => Items.TryGetValue(key, out var v) ? v : null;
            set => Items[key] = value;
        }
    }

    private sealed class StringEventHandler : IEventHandler<String>
    {
        public String LastMessage { get; private set; } = String.Empty;

        public Task HandleAsync(String @event, IEventContext? context, CancellationToken cancellationToken)
        {
            LastMessage = @event;
            return Task.CompletedTask;
        }
    }

    [Fact(DisplayName = "HandleAsync_IPacket 前缀不匹配时应返回 0")]
    public async Task HandleAsync_Packet_PrefixNotMatched_ReturnsZero()
    {
        var hub = new EventHub<TestEvent>();
        var packet = new ArrayPacket("not-event#test#c1#{\"Message\":\"x\"}".GetBytes());
        Assert.Equal(0, await hub.HandleAsync(packet));
    }

    [Fact(DisplayName = "HandleAsync_IPacket 头部无效时应返回 0")]
    public async Task HandleAsync_Packet_InvalidHeader_ReturnsZero()
    {
        var hub = new EventHub<TestEvent>();
        Assert.Equal(0, await hub.HandleAsync(new ArrayPacket("event#".GetBytes())));
        Assert.Equal(0, await hub.HandleAsync(new ArrayPacket("event#test#".GetBytes())));
        Assert.Equal(0, await hub.HandleAsync(new ArrayPacket("event#test#c1".GetBytes())));
    }

    [Fact(DisplayName = "HandleAsync_IPacket 消息体为空时应返回 0")]
    public async Task HandleAsync_Packet_EmptyBody_ReturnsZero()
    {
        var hub = new EventHub<TestEvent>();
        Assert.Equal(0, await hub.HandleAsync(new ArrayPacket("event#test#c1#".GetBytes())));
    }

    [Fact(DisplayName = "HandleAsync_IPacket 有效 JSON 消息应路由到处理器")]
    public async Task HandleAsync_Packet_ValidJson_RoutesToHandler()
    {
        var hub = new EventHub<TestEvent>();
        var handler = new TestEventHandler();
        hub.Add("test", handler);

        var packet = new ArrayPacket("event#test#c1#{\"Message\":\"packet-hi\"}".GetBytes());
        var rs = await hub.HandleAsync(packet);

        Assert.Equal(1, rs);
        Assert.Equal("packet-hi", handler.HandledMessage);
    }

    [Fact(DisplayName = "HandleAsync_String 消息体为空时应返回 0")]
    public async Task HandleAsync_String_EmptyBody_ReturnsZero()
    {
        var hub = new EventHub<TestEvent>();
        Assert.Equal(0, await hub.HandleAsync("event#test#c1#"));
    }

    [Fact(DisplayName = "HandleAsync_String 应将原始消息写入 context Raw")]
    public async Task HandleAsync_String_PopulatesContextRaw()
    {
        var hub = new EventHub<TestEvent>();
        hub.Add("test", new TestEventHandler());

        var ctx = new EventContext();
        var raw = "event#test#c1#{\"Message\":\"raw-test\"}";
        await hub.HandleAsync(raw, ctx);

        Assert.Equal(raw, ctx["Raw"] as String);
    }

    [Fact(DisplayName = "HandleAsync_String TEvent=String 时直接路由不经 JSON 反序列化")]
    public async Task HandleAsync_String_WhenTEventIsString_RoutesDirectly()
    {
        var hub = new EventHub<String>();
        var handler = new StringEventHandler();
        hub.Add("test", handler);

        var rs = await hub.HandleAsync("event#test#sender#hello");

        Assert.Equal(1, rs);
        Assert.Equal("hello", handler.LastMessage);
    }

    [Fact(DisplayName = "Add(topic,bus) 注册总线后 DispatchAsync 可路由到该总线")]
    public async Task Add_Bus_RegistersAndDispatchesToBus()
    {
        var bus = new EventBus<TestEvent>();
        var handler = new TestEventHandler();
        bus.Subscribe(handler, "h1");

        var hub = new EventHub<TestEvent>();
        hub.Add("test", bus);

        var rs = await hub.DispatchAsync("test", "sender", new TestEvent { Message = "direct-bus" });

        Assert.Equal(1, rs);
        Assert.Equal("direct-bus", handler.HandledMessage);
    }

    [Fact(DisplayName = "GetEventBus 两次调用应返回同一实例")]
    public void GetEventBus_TwoCalls_ReturnsSameInstance()
    {
        var hub = new EventHub<TestEvent>();
        var bus1 = hub.GetEventBus("test");
        var bus2 = hub.GetEventBus("test");
        Assert.Same(bus1, bus2);
    }

    [Fact(DisplayName = "GetEventBus 有工厂时应使用工厂创建实例")]
    public void GetEventBus_WithFactory_UsesFactory()
    {
        var factory = new TestEventBusFactory();
        var hub = new EventHub<TestEvent> { Factory = factory };

        var bus = hub.GetEventBus("t", "client1");

        Assert.Equal("t", factory.LastTopic);
        Assert.Equal("client1", factory.LastClientId);
        Assert.NotNull(factory.LastBus);
        Assert.Same(factory.LastBus, bus);
    }

    [Fact(DisplayName = "TryGetBus 找到时应返回 true 并输出总线")]
    public void TryGetBus_WhenFound_ReturnsTrueAndBus()
    {
        var hub = new EventHub<TestEvent>();
        hub.GetEventBus("test");

        var found = hub.TryGetBus<TestEvent>("test", out var bus);

        Assert.True(found);
        Assert.NotNull(bus);
    }

    [Fact(DisplayName = "TryGetBus 未找到时应返回 false")]
    public void TryGetBus_WhenNotFound_ReturnsFalse()
    {
        var hub = new EventHub<TestEvent>();
        Assert.False(hub.TryGetBus<TestEvent>("nonexistent", out var bus));
        Assert.Null(bus);
    }

    [Fact(DisplayName = "TryGetBus 类型不匹配时应返回 false")]
    public void TryGetBus_WhenTypeMismatch_ReturnsFalse()
    {
        var hub = new EventHub<TestEvent>();
        hub.GetEventBus("test");
        // 查询不同的泛型参数，应匹配失败
        Assert.False(hub.TryGetBus<String>("test", out var bus));
        Assert.Null(bus);
    }

    [Fact(DisplayName = "DispatchAsync topic 为空时应抛出 ArgumentNullException")]
    public async Task DispatchAsync_EmptyTopic_ThrowsArgumentNullException()
    {
        var hub = new EventHub<TestEvent>();
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            hub.DispatchAsync(String.Empty, "c1", new TestEvent()));
    }

    [Fact(DisplayName = "DispatchAsync 应将 topic 和 clientId 写入 EventContext")]
    public async Task DispatchAsync_SetsEventContextTopicAndClientId()
    {
        var hub = new EventHub<TestEvent>();
        var ctx = new EventContext();
        await hub.DispatchAsync("test", "sender", new TestEvent { Message = "x" }, ctx);

        Assert.Equal("test", ctx.Topic);
        Assert.Equal("sender", ctx.ClientId);
    }

    [Fact(DisplayName = "DispatchAsync 应将 topic 和 clientId 写入 IExtend 上下文")]
    public async Task DispatchAsync_SetsIExtendTopicAndClientId()
    {
        var hub = new EventHub<TestEvent>();
        var ctx = new TestExtendContext();
        await hub.DispatchAsync("test", "sender", new TestEvent { Message = "x" }, ctx);

        Assert.Equal("test", ctx["Topic"] as String);
        Assert.Equal("sender", ctx["ClientId"] as String);
    }

    [Fact(DisplayName = "DispatchAsync 未注册 topic 时应返回 0")]
    public async Task DispatchAsync_NoRegisteredBus_ReturnsZero()
    {
        var hub = new EventHub<TestEvent>();
        var rs = await hub.DispatchAsync("nonexistent", "c1", new TestEvent { Message = "x" });
        Assert.Equal(0, rs);
    }

    [Fact(DisplayName = "Hub.ReceiveAsync 超时应抛出 OperationCanceledException")]
    public async Task Hub_ReceiveAsync_WithTimeout_ThrowsOnTimeout()
    {
        var hub = new EventHub<TestEvent>();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            hub.ReceiveAsync("timeout-topic", TimeSpan.FromMilliseconds(50)));
    }

    [Fact(DisplayName = "Hub.ReceiveAsync 取消令牌触发时应抛出 OperationCanceledException")]
    public async Task Hub_ReceiveAsync_WithCancellation_ThrowsOnCancel()
    {
        var hub = new EventHub<TestEvent>();
        using var cts = new CancellationTokenSource();
        var receiveTask = hub.ReceiveAsync("cancel-topic", cts.Token);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => receiveTask);
    }

    [Fact(DisplayName = "DispatchActionAsync subscribe 时上下文无 Handler 应抛出 ArgumentNullException")]
    public async Task DispatchActionAsync_Subscribe_WithoutHandler_Throws()
    {
        var hub = new EventHub<TestEvent>();
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            hub.DispatchActionAsync("test", "c1", "subscribe", null));
    }

    [Fact(DisplayName = "显式接口 IEventHandler<IPacket> 应委托到公共 HandleAsync")]
    public async Task ExplicitInterface_IPacketHandler_DelegatesToPublicMethod()
    {
        var hub = new EventHub<TestEvent>();
        var handler = new TestEventHandler();
        hub.Add("test", handler);

        IEventHandler<IPacket> iface = hub;
        var packet = new ArrayPacket("event#test#c1#{\"Message\":\"via-iface\"}".GetBytes());
        await iface.HandleAsync(packet, null, default);

        Assert.Equal("via-iface", handler.HandledMessage);
    }

    [Fact(DisplayName = "显式接口 IEventHandler<String> 应委托到公共 HandleAsync")]
    public async Task ExplicitInterface_StringHandler_DelegatesToPublicMethod()
    {
        var hub = new EventHub<TestEvent>();
        var handler = new TestEventHandler();
        hub.Add("test", handler);

        IEventHandler<String> iface = hub;
        await iface.HandleAsync("event#test#c1#{\"Message\":\"via-str-iface\"}", null, default);

        Assert.Equal("via-str-iface", handler.HandledMessage);
    }
}
