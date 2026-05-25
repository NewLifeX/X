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

        public override Task<Boolean> SubscribeAsync(IEventHandler<TestEvent> handler, String clientId = "", CancellationToken cancellationToken = default)
        {
            SubscribeCount++;
            return base.SubscribeAsync(handler, clientId, cancellationToken);
        }

        public override Task<Boolean> UnsubscribeAsync(String clientId = "", CancellationToken cancellationToken = default)
        {
            UnsubscribeCount++;
            return base.UnsubscribeAsync(clientId, cancellationToken);
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

    // ----------- OnReceiveAsync(String) 协议解析 -----------

    [Fact(DisplayName = "OnReceiveAsync_String 前缀不匹配时应返回 0")]
    public async Task OnReceiveAsync_String_ShouldReturn0_WhenPrefixNotMatched()
    {
        var hub = new EventHub<TestEvent>();
        Assert.Equal(0, await hub.OnReceiveAsync("not-event#topic#client#{\"Message\":\"a\"}", null));
    }

    [Fact(DisplayName = "OnReceiveAsync_String 头部无效时应返回 0")]
    public async Task OnReceiveAsync_String_ShouldReturn0_WhenHeaderInvalid()
    {
        var hub = new EventHub<TestEvent>();
        Assert.Equal(0, await hub.OnReceiveAsync("event#", null));
        Assert.Equal(0, await hub.OnReceiveAsync("event#topic#", null));
        Assert.Equal(0, await hub.OnReceiveAsync("event#topic#client", null));
        Assert.Equal(0, await hub.OnReceiveAsync("event#topic#client#", null));
    }

    [Fact(DisplayName = "OnReceiveAsync_String 消息体为空时应返回 0")]
    public async Task OnReceiveAsync_String_EmptyBody_ReturnsZero()
    {
        var hub = new EventHub<TestEvent>();
        Assert.Equal(0, await hub.OnReceiveAsync("event#test#c1#"));
    }

    [Fact(DisplayName = "OnReceiveAsync_String subscribe 动作应路由到订阅")]
    public async Task OnReceiveAsync_String_ShouldRouteSubscribeAction()
    {
        var factory = new TestEventBusFactory();
        var hub = new EventHub<TestEvent> { Factory = factory };

        var ctx = new EventContext();
        ctx["Handler"] = new TestEventHandler();

        var rs = await hub.OnReceiveAsync("event#test#c1#subscribe", ctx);

        Assert.Equal(1, rs);
        Assert.Equal("test", factory.LastTopic);
        Assert.Equal("c1", factory.LastClientId);
        Assert.NotNull(factory.LastBus);
        Assert.Equal(1, factory.LastBus!.SubscribeCount);
        Assert.Equal(0, factory.LastBus!.PublishCount);
    }

    [Fact(DisplayName = "OnReceiveAsync_String unsubscribe 动作应取消订阅并清理空闲总线")]
    public async Task OnReceiveAsync_String_ShouldRouteUnsubscribeAction_AndRemoveBusWhenEmpty()
    {
        var factory = new TestEventBusFactory();
        var hub = new EventHub<TestEvent> { Factory = factory };

        // 预先创建总线以便取得 LastBus 引用
        _ = hub.GetEventBus("test", "seed");

        var ctx = new EventContext();
        ctx["Handler"] = new TestEventHandler();
        Assert.Equal(1, await hub.OnReceiveAsync("event#test#c1#subscribe", ctx));
        var bus = factory.LastBus!;

        Assert.Equal(1, await hub.OnReceiveAsync("event#test#c1#unsubscribe", null));
        Assert.Equal(1, bus.UnsubscribeCount);
        Assert.Equal(0, bus.PublishCount);

        // 总线被清理后，发布事件返回 0
        var rs = await hub.OnReceiveAsync("event#test#any#{\"Message\":\"hello\"}", null);
        Assert.Equal(0, rs);
        Assert.Equal(0, bus.PublishCount);
    }

    [Fact(DisplayName = "OnReceiveAsync_String unknown 动作字符串应返回 0")]
    public async Task OnReceiveAsync_String_UnknownAction_ReturnsZero()
    {
        var hub = new EventHub<TestEvent>();
        var rs = await hub.OnReceiveAsync("event#test#c1#unknownAction", null);
        Assert.Equal(0, rs);
    }

    [Fact(DisplayName = "OnReceiveAsync_String JSON 消息体无处理器时应返回 0")]
    public async Task OnReceiveAsync_String_JsonBody_NoHandler_ReturnsZero()
    {
        var hub = new EventHub<TestEvent>();
        var rs = await hub.OnReceiveAsync("event#test#c1#{\"Message\":\"hi\"}", null);
        Assert.Equal(0, rs);
    }

    [Fact(DisplayName = "OnReceiveAsync_String 应将原始消息写入 context Raw")]
    public async Task OnReceiveAsync_String_PopulatesContextRaw()
    {
        var hub = new EventHub<TestEvent>();
        hub.GetEventBus("test").Subscribe(new TestEventHandler());

        var ctx = new EventContext();
        var raw = "event#test#c1#{\"Message\":\"raw-test\"}";
        await hub.OnReceiveAsync(raw, ctx);

        Assert.Equal(raw, ctx["Raw"] as String);
    }

    [Fact(DisplayName = "OnReceiveAsync_String 应路由到已注册的处理器")]
    public async Task OnReceiveAsync_String_ShouldRouteToRegisteredHandler()
    {
        var hub = new EventHub<TestEvent>();
        var handler = new TestEventHandler();
        hub.GetEventBus("test").Subscribe(handler);

        var rs = await hub.OnReceiveAsync("event#test#sender#{\"Message\":\"hi\"}", null);

        Assert.Equal(1, rs);
        Assert.Equal("hi", handler.HandledMessage);
    }

    [Fact(DisplayName = "OnReceiveAsync_String TEvent=String 时直接路由不经 JSON 反序列化")]
    public async Task OnReceiveAsync_String_WhenTEventIsString_RoutesDirectly()
    {
        var hub = new EventHub<String>();
        var handler = new StringEventHandler();
        hub.GetEventBus("test").Subscribe(handler);

        var rs = await hub.OnReceiveAsync("event#test#sender#hello");

        Assert.Equal(1, rs);
        Assert.Equal("hello", handler.LastMessage);
    }

    [Fact(DisplayName = "OnReceiveAsync_String subscribe 时上下文无 Handler 应返回 0 而非抛出")]
    public async Task OnReceiveAsync_String_Subscribe_WithoutHandler_ReturnsZero()
    {
        var hub = new EventHub<TestEvent>();
        var rs = await hub.OnReceiveAsync("event#test#c1#subscribe", null);
        Assert.Equal(0, rs);
    }

    // ----------- OnReceiveAsync(IPacket) 协议解析 -----------

    [Fact(DisplayName = "OnReceiveAsync_IPacket 前缀不匹配时应返回 0")]
    public async Task OnReceiveAsync_Packet_PrefixNotMatched_ReturnsZero()
    {
        var hub = new EventHub<TestEvent>();
        var packet = new ArrayPacket("not-event#test#c1#{\"Message\":\"x\"}".GetBytes());
        Assert.Equal(0, await hub.OnReceiveAsync(packet));
    }

    [Fact(DisplayName = "OnReceiveAsync_IPacket 头部无效时应返回 0")]
    public async Task OnReceiveAsync_Packet_InvalidHeader_ReturnsZero()
    {
        var hub = new EventHub<TestEvent>();
        Assert.Equal(0, await hub.OnReceiveAsync(new ArrayPacket("event#".GetBytes())));
        Assert.Equal(0, await hub.OnReceiveAsync(new ArrayPacket("event#test#".GetBytes())));
        Assert.Equal(0, await hub.OnReceiveAsync(new ArrayPacket("event#test#c1".GetBytes())));
    }

    [Fact(DisplayName = "OnReceiveAsync_IPacket 消息体为空时应返回 0")]
    public async Task OnReceiveAsync_Packet_EmptyBody_ReturnsZero()
    {
        var hub = new EventHub<TestEvent>();
        Assert.Equal(0, await hub.OnReceiveAsync(new ArrayPacket("event#test#c1#".GetBytes())));
    }

    [Fact(DisplayName = "OnReceiveAsync_IPacket 有效 JSON 消息应路由到处理器")]
    public async Task OnReceiveAsync_Packet_ValidJson_RoutesToHandler()
    {
        var hub = new EventHub<TestEvent>();
        var handler = new TestEventHandler();
        hub.GetEventBus("test").Subscribe(handler);

        var packet = new ArrayPacket("event#test#c1#{\"Message\":\"packet-hi\"}".GetBytes());
        var rs = await hub.OnReceiveAsync(packet);

        Assert.Equal(1, rs);
        Assert.Equal("packet-hi", handler.HandledMessage);
    }

    // ----------- PublishAsync / SubscribeAsync / UnsubscribeAsync -----------

    [Fact(DisplayName = "SubscribeAsync 后 PublishAsync 应将事件路由到总线")]
    public async Task SubscribeAsync_ThenPublishAsync_RoutesToBus()
    {
        var factory = new TestEventBusFactory();
        var hub = new EventHub<TestEvent> { Factory = factory };

        var handler = new TestEventHandler();
        await hub.SubscribeAsync("test", "c1", handler);
        var bus = factory.LastBus!;

        var rs = await hub.PublishAsync("test", new TestEvent { Message = "hi" });

        Assert.Equal(1, rs);
        Assert.Equal("hi", handler.HandledMessage);
        Assert.Equal(1, bus.PublishCount);
    }

    [Fact(DisplayName = "PublishAsync 无订阅者时应返回 0")]
    public async Task PublishAsync_NoSubscribers_ReturnsZero()
    {
        var hub = new EventHub<TestEvent>();
        var rs = await hub.PublishAsync("nonexistent", new TestEvent { Message = "x" });
        Assert.Equal(0, rs);
    }

    [Fact(DisplayName = "SubscribeAsync 通过工厂创建总线并完成订阅")]
    public async Task SubscribeAsync_WithFactory_CreatesAndSubscribes()
    {
        var factory = new TestEventBusFactory();
        var hub = new EventHub<TestEvent> { Factory = factory };

        var handler = new TestEventHandler();
        await hub.SubscribeAsync("test", "c1", handler);

        Assert.NotNull(factory.LastBus);
        Assert.Equal(1, factory.LastBus!.SubscribeCount);
    }

    [Fact(DisplayName = "UnsubscribeAsync 应取消订阅并在空时清理总线")]
    public async Task UnsubscribeAsync_ShouldUnsubscribeAndRemoveBusWhenEmpty()
    {
        var factory = new TestEventBusFactory();
        var hub = new EventHub<TestEvent> { Factory = factory };

        var handler = new TestEventHandler();
        await hub.SubscribeAsync("test", "c1", handler);
        var bus = factory.LastBus!;

        Assert.True(await hub.UnsubscribeAsync("test", "c1"));
        Assert.Equal(1, bus.UnsubscribeCount);

        // 总线已被清理，再次取消订阅返回 false
        Assert.False(await hub.UnsubscribeAsync("test", "c1"));
    }

    // ----------- RegisterBus / GetEventBus / TryGetBus -----------

    [Fact(DisplayName = "RegisterBus 后 PublishAsync 应路由到注册的总线")]
    public async Task RegisterBus_ThenPublishAsync_RoutesToBus()
    {
        var bus = new EventBus<TestEvent>();
        var handler = new TestEventHandler();
        bus.Subscribe(handler, "h1");

        var hub = new EventHub<TestEvent>();
        hub.RegisterBus("test", bus);

        var rs = await hub.PublishAsync("test", new TestEvent { Message = "direct-bus" });

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

        var found = hub.TryGetBus("test", out var bus);

        Assert.True(found);
        Assert.NotNull(bus);
    }

    [Fact(DisplayName = "TryGetBus 未找到时应返回 false")]
    public void TryGetBus_WhenNotFound_ReturnsFalse()
    {
        var hub = new EventHub<TestEvent>();
        Assert.False(hub.TryGetBus("nonexistent", out var bus));
        Assert.Null(bus);
    }

    // ----------- ReceiveAsync -----------

    [Fact(DisplayName = "Hub.ReceiveAsync 接收后应自动清理空闲总线")]
    public async Task Hub_ReceiveAsync_ShouldAutoCleanupEmptyBus()
    {
        var hub = new EventHub<TestEvent>();

        var receiveTask = hub.ReceiveAsync("device-001");
        await hub.PublishAsync("device-001", new TestEvent { Message = "ok" });
        await receiveTask;

        Assert.False(hub.TryGetBus("device-001", out _));
    }

    [Fact(DisplayName = "Hub.ReceiveAsync 有长期订阅者时不清理总线")]
    public async Task Hub_ReceiveAsync_ShouldNotCleanup_WhenOtherSubscribersExist()
    {
        var hub = new EventHub<TestEvent>();

        var permanentBus = hub.GetEventBus("device-002");
        permanentBus.Subscribe(new TestEventHandler(), "permanent");

        var receiveTask = hub.ReceiveAsync("device-002");
        await hub.PublishAsync("device-002", new TestEvent { Message = "ok" });
        await receiveTask;

        Assert.True(hub.TryGetBus("device-002", out _));
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

    // ----------- 显式接口实现 -----------

    [Fact(DisplayName = "显式接口 IEventHandler<IPacket> 应委托到 OnReceiveAsync")]
    public async Task ExplicitInterface_IPacketHandler_DelegatesToOnReceiveAsync()
    {
        var hub = new EventHub<TestEvent>();
        var handler = new TestEventHandler();
        hub.GetEventBus("test").Subscribe(handler);

        IEventHandler<IPacket> iface = hub;
        var packet = new ArrayPacket("event#test#c1#{\"Message\":\"via-iface\"}".GetBytes());
        await iface.HandleAsync(packet, null, default);

        Assert.Equal("via-iface", handler.HandledMessage);
    }

    [Fact(DisplayName = "显式接口 IEventHandler<String> 应委托到 OnReceiveAsync")]
    public async Task ExplicitInterface_StringHandler_DelegatesToOnReceiveAsync()
    {
        var hub = new EventHub<TestEvent>();
        var handler = new TestEventHandler();
        hub.GetEventBus("test").Subscribe(handler);

        IEventHandler<String> iface = hub;
        await iface.HandleAsync("event#test#c1#{\"Message\":\"via-str-iface\"}", null, default);

        Assert.Equal("via-str-iface", handler.HandledMessage);
    }

    // ----------- Helper types -----------

    private sealed class StringEventHandler : IEventHandler<String>
    {
        public String LastMessage { get; private set; } = String.Empty;

        public Task HandleAsync(String @event, IEventContext? context, CancellationToken cancellationToken)
        {
            LastMessage = @event;
            return Task.CompletedTask;
        }
    }
}
