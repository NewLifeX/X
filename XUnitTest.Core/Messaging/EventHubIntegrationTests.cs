using NewLife;
using NewLife.Data;
using NewLife.Messaging;
using Xunit;

namespace XUnitTest.Messaging;

public class EventHubIntegrationTests
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

    [Fact(DisplayName = "多客户端订阅同一主题_发布后所有客户端收到_发布者除外")]
    public async Task MultiClient_Subscribe_Publish_SenderExcluded()
    {
        var hub = new EventHub<TestEvent>();

        // c1 订阅
        var h1 = new TestEventHandler();
        var c1Ctx = new EventContext();
        c1Ctx["Handler"] = h1;
        await hub.OnReceiveAsync("event#chat#c1#subscribe", c1Ctx);

        // c2 订阅
        var h2 = new TestEventHandler();
        var c2Ctx = new EventContext();
        c2Ctx["Handler"] = h2;
        await hub.OnReceiveAsync("event#chat#c2#subscribe", c2Ctx);

        // sender 也订阅（发布时不应收到自己的消息）
        var hSender = new TestEventHandler();
        var senderSubCtx = new EventContext();
        senderSubCtx["Handler"] = hSender;
        await hub.OnReceiveAsync("event#chat#sender#subscribe", senderSubCtx);

        // 发布时传入上下文以触发发送方排除逻辑
        var pubCtx = new EventContext();
        var rs = await hub.OnReceiveAsync("event#chat#sender#{\"Message\":\"hello\"}", pubCtx);

        Assert.Equal("hello", h1.HandledMessage);
        Assert.Equal("hello", h2.HandledMessage);
        Assert.Equal(String.Empty, hSender.HandledMessage);
        Assert.Equal(2, rs);
    }

    [Fact(DisplayName = "二进制报文流 subscribe 后 publish IPacket 端到端")]
    public async Task PacketFlow_SubscribeThenPublish_EndToEnd()
    {
        var hub = new EventHub<TestEvent>();
        var handler = new TestEventHandler();

        // 通过二进制报文订阅
        var subCtx = new EventContext();
        subCtx["Handler"] = handler;
        await hub.OnReceiveAsync(new ArrayPacket("event#device#c1#subscribe".GetBytes()), subCtx);

        // 通过二进制报文发布
        var pubPacket = new ArrayPacket("event#device#sender#{\"Message\":\"ping\"}".GetBytes());
        var rs = await hub.OnReceiveAsync(pubPacket, null);

        Assert.Equal(1, rs);
        Assert.Equal("ping", handler.HandledMessage);
    }

    [Fact(DisplayName = "Unsubscribe 后总线自动清理_再 publish 返回 0")]
    public async Task Unsubscribe_AutoCleansUpBus_ThenPublishReturnsZero()
    {
        var hub = new EventHub<TestEvent>();
        var handler = new TestEventHandler();

        // 订阅
        var subCtx = new EventContext();
        subCtx["Handler"] = handler;
        await hub.OnReceiveAsync("event#device#c1#subscribe", subCtx);
        Assert.True(hub.TryGetBus("device", out _));

        // 取消订阅 — 总线应被清理
        await hub.OnReceiveAsync("event#device#c1#unsubscribe", null);
        Assert.False(hub.TryGetBus("device", out _));

        // 此时发布应返回 0
        var rs = await hub.OnReceiveAsync("event#device#sender#{\"Message\":\"lost\"}", null);
        Assert.Equal(0, rs);
    }

    [Fact(DisplayName = "并发主题互不干扰")]
    public async Task Topics_DoNotInterfere()
    {
        var hub = new EventHub<TestEvent>();
        var handlerA = new TestEventHandler();
        var handlerB = new TestEventHandler();

        hub.GetEventBus("topicA").Subscribe(handlerA);
        hub.GetEventBus("topicB").Subscribe(handlerB);

        await hub.OnReceiveAsync("event#topicA#sender#{\"Message\":\"msgA\"}", null);

        Assert.Equal("msgA", handlerA.HandledMessage);
        Assert.Equal(String.Empty, handlerB.HandledMessage);
    }

    [Fact(DisplayName = "IoT指令确认场景_ReceiveAsync在发布前已等待")]
    public async Task IoT_CommandConfirmation_PublishAfterReceiveStarted()
    {
        var hub = new EventHub<TestEvent>();

        var receiveTask = hub.ReceiveAsync("device-001", TimeSpan.FromSeconds(5));

        // 模拟设备异步应答
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            await hub.PublishAsync("device-001", new TestEvent { Message = "ACK" });
        });

        var result = await receiveTask;
        Assert.Equal("ACK", result.Message);
    }
}
