using NewLife;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Integration;

/// <summary>WebSocket 集成测试固定装置。HttpServer 继承自 NetServer，/ws 挂载 WebSocketHandler</summary>
public class WebSocketServerFixture : IDisposable
{
    public HttpServer Server { get; }

    public Int32 Port => Server.Port;

    public WebSocketServerFixture()
    {
        var server = new HttpServer
        {
            Name = "WebSocket集成测试服务器",
            Port = 0,
            Log = XTrace.Log,
#if DEBUG
            SessionLog = XTrace.Log,
#endif
        };
        server.Map("/ws", new WsEchoHandler());

        Server = server;
        Server.Start();
    }

    public void Dispose() => Server?.Dispose();
}

/// <summary>WebSocket 回显处理器：文本 echo，二进制原样 echo</summary>
class WsEchoHandler : WebSocketHandler
{
    public override void ProcessMessage(WebSocket socket, WebSocketMessage message)
    {
        if (message.Type == WebSocketMessageType.Text)
        {
            var text = message.Payload?.ToStr() ?? String.Empty;
            socket.Send($"ws-echo:{text}");
            return;
        }

        if (message.Type == WebSocketMessageType.Binary)
        {
            var data = message.Payload?.ToArray() ?? [];
            socket.Send(data, WebSocketMessageType.Binary);
            return;
        }

        base.ProcessMessage(socket, message);
    }
}

/// <summary>NetServer + WebSocketClient + WebSocketCodec 集成测试</summary>
[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
public class WebSocketIntegrationTests : IClassFixture<WebSocketServerFixture>
{
    private readonly WebSocketServerFixture _fixture;

    public WebSocketIntegrationTests(WebSocketServerFixture fixture) => _fixture = fixture;

    [Fact(DisplayName = "01-WebSocket服务端已启动")]
    public void Test01_ServerStarted()
    {
        Assert.True(_fixture.Server.Active);
        Assert.True(_fixture.Port > 0);
    }

    [Fact(DisplayName = "02-WebSocketClient 建连成功（含WebSocketCodec握手）")]
    public async Task Test02_Connect()
    {
        var ws = new WebSocketClient($"ws://127.0.0.1:{_fixture.Port}/ws")
        {
            Log = XTrace.Log,
        };

        var opened = await ws.OpenAsync();
        Assert.True(opened);
        Assert.True(ws.Active);

        await ws.CloseAsync(1000, "done");
    }

    [Fact(DisplayName = "03-文本消息同步异步收发")]
    public async Task Test03_TextEcho()
    {
        var ws = new WebSocketClient($"ws://127.0.0.1:{_fixture.Port}/ws")
        {
            Log = XTrace.Log,
        };

        Assert.True(await ws.OpenAsync());

        var text = "hello-websocket";
        var wait = new TaskCompletionSource<String>();
        ws.Received += (s, e) =>
        {
            if (e.Message is WebSocketMessage m && m.Type == WebSocketMessageType.Text)
            {
                var str = m.Payload?.ToStr() ?? String.Empty;
                wait.TrySetResult(str);
            }
        };

        await ws.SendTextAsync(text);

        var reply = await wait.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal($"ws-echo:{text}", reply);

        await ws.CloseAsync(1000, "done");
    }

    [Fact(DisplayName = "04-二进制消息原样回显")]
    public async Task Test04_BinaryEcho()
    {
        var ws = new WebSocketClient($"ws://127.0.0.1:{_fixture.Port}/ws")
        {
            Log = XTrace.Log,
        };

        Assert.True(await ws.OpenAsync());

        var payload = new Byte[256];
        Random.Shared.NextBytes(payload);
        var wait = new TaskCompletionSource<Byte[]>();
        ws.Received += (s, e) =>
        {
            if (e.Message is WebSocketMessage m && m.Type == WebSocketMessageType.Binary)
            {
                var data = m.Payload?.ToArray() ?? [];
                wait.TrySetResult(data);
            }
        };

        await ws.SendBinaryAsync((ArrayPacket)payload);

        var reply = await wait.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(payload, reply);

        await ws.CloseAsync(1000, "done");
    }

    [Fact(DisplayName = "05-多客户端并发收发")]
    public async Task Test05_ConcurrentClients()
    {
        const Int32 clients = 5;

        var tasks = Enumerable.Range(0, clients).Select(async i =>
        {
            var ws = new WebSocketClient($"ws://127.0.0.1:{_fixture.Port}/ws")
            {
                Log = XTrace.Log,
            };

            Assert.True(await ws.OpenAsync());

            var text = $"c{i}-{Guid.NewGuid():N}";
            var wait = new TaskCompletionSource<String>();
            ws.Received += (s, e) =>
            {
                if (e.Message is WebSocketMessage m && m.Type == WebSocketMessageType.Text)
                {
                    var str = m.Payload?.ToStr() ?? String.Empty;
                    wait.TrySetResult(str);
                }
            };

            await ws.SendTextAsync(text);

            var reply = await wait.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await ws.CloseAsync(1000, "done");

            return (text, reply);
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        foreach (var item in results)
            Assert.Equal($"ws-echo:{item.text}", item.reply);
    }
}
