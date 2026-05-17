using System.Diagnostics;
using System.Security.Cryptography;
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
[Collection("Integration")]
[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
public class WebSocketIntegrationTests(WebSocketServerFixture fixture) : IClassFixture<WebSocketServerFixture>
{
    [Fact(DisplayName = "01-WebSocket服务端已启动")]
    public void Test01_ServerStarted()
    {
        Assert.True(fixture.Server.Active);
        Assert.True(fixture.Port > 0);
    }

    /// <summary>
    /// 建连+文本+二进制收发+Active验证，走 Received 事件路径（MaxAsync=1，后台管道接收循环）。
    /// WS Close 帧由接收循环检测服务端关闭，Active 异步变 false。
    /// </summary>
    [Fact(DisplayName = "02-建连+文本+二进制收发+Active验证（Received事件路径）")]
    public async Task Test02_BasicEcho_ReceivedEvent()
    {
        var ws = new WebSocketClient($"ws://127.0.0.1:{fixture.Port}/ws")
        {
            Log = XTrace.Log,
        };

        Assert.True(await ws.OpenAsync());
        Assert.True(ws.Active, "建连后 Active 应为 true");

        var textWait = new TaskCompletionSource<String>();
        var binaryWait = new TaskCompletionSource<Byte[]>();
        ws.Received += (s, e) =>
        {
            if (e.Message is WebSocketMessage m)
            {
                if (m.Type == WebSocketMessageType.Text)
                    textWait.TrySetResult(m.Payload?.ToStr() ?? String.Empty);
                else if (m.Type == WebSocketMessageType.Binary)
                    binaryWait.TrySetResult(m.Payload?.ToArray() ?? []);
            }
        };

        // 文本收发
        var text = "hello-received-event";
        await ws.SendTextAsync(text);
        var textReply = await textWait.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal($"ws-echo:{text}", textReply);

        // 二进制收发
        var payload = new Byte[64];
        Random.Shared.NextBytes(payload);
        await ws.SendBinaryAsync((ArrayPacket)payload);
        var binaryReply = await binaryWait.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(payload, binaryReply);

        // 发送 WS Close 帧，接收循环检测服务端关闭后 Active 变 false
        await ws.CloseAsync(1000, "done");
        await Task.Delay(300);
        Assert.False(ws.Active, "CloseAsync 后 Active 应为 false");
    }

    /// <summary>
    /// 建连+文本+二进制收发+Active验证，走 ReceiveMessageAsync 路径（MaxAsync=0，禁用后台接收循环）。
    /// 无后台循环时直接调用 ReceiveMessageAsync 读取 WS 帧；关闭使用 SessionBase.CloseAsync 直接关 TCP。
    /// </summary>
    /// <remarks>
    /// 前提：每次只有一条消息在途（发一条→立即等回显→再发），不存在粘包可能。
    /// 若需流水线发送多条消息，必须改用 Received 事件（MaxAsync=1），
    /// 由 WebSocketCodec+PacketCodec 在管道内完成粘包/拆包。
    /// </remarks>
    [Fact(DisplayName = "03-建连+文本+二进制收发+Active验证（ReceiveMessageAsync路径，MaxAsync=0）")]
    public async Task Test03_BasicEcho_ReceiveMessageAsync()
    {
        var ws = new WebSocketClient($"ws://127.0.0.1:{fixture.Port}/ws")
        {
            Log = XTrace.Log,
            MaxAsync = 0,   // 禁用后台接收循环，ReceiveMessageAsync 直接读原始 WS 帧
        };

        Assert.True(await ws.OpenAsync());
        Assert.True(ws.Active, "建连后 Active 应为 true");

        // 文本收发
        var text = "hello-receive-message-async";
        await ws.SendTextAsync(text);
        var textMsg = await ws.ReceiveMessageAsync().WaitAsync(TimeSpan.FromSeconds(10));
        Assert.NotNull(textMsg);
        Assert.Equal(WebSocketMessageType.Text, textMsg.Type);
        Assert.Equal($"ws-echo:{text}", textMsg.Payload?.ToStr());

        // 二进制收发
        var payload = new Byte[64];
        Random.Shared.NextBytes(payload);
        await ws.SendBinaryAsync((ArrayPacket)payload);
        var binaryMsg = await ws.ReceiveMessageAsync().WaitAsync(TimeSpan.FromSeconds(10));
        Assert.NotNull(binaryMsg);
        Assert.Equal(WebSocketMessageType.Binary, binaryMsg.Type);
        Assert.Equal(payload, binaryMsg.Payload?.ToArray());

        // MaxAsync=0 时无接收循环检测关闭帧，直接关闭 TCP，Active 同步变 false
        await ws.CloseAsync("done");
        Assert.False(ws.Active, "CloseAsync 后 Active 应为 false");
    }

    /// <summary>
    /// 4KB 二进制 SHA256 完整性校验 + DefaultMessage 格式字节完整性，走 ReceiveMessageAsync 路径（MaxAsync=0）。
    /// loopback 环境下单次 ReceiveAsync 可携带完整 WS 帧（loopback MTU=65536），且仅一条消息在途，
    /// 不存在粘包，ReceiveMessageAsync 可安全使用。
    /// </summary>
    [Fact(DisplayName = "04-4KB二进制SHA256校验+DefaultMessage格式完整性（ReceiveMessageAsync路径）")]
    public async Task Test04_LargeBinary_And_DefaultMessage()
    {
        // ── 子测试 1：4 KB 随机数据，SHA256 校验内容完整性 ──────────────────────────
        var payload4K = new Byte[4 * 1024];
        Random.Shared.NextBytes(payload4K);
        var sentHash = SHA256.HashData(payload4K);

        var ws1 = new WebSocketClient($"ws://127.0.0.1:{fixture.Port}/ws") { Log = XTrace.Log, MaxAsync = 0 };
        Assert.True(await ws1.OpenAsync());

        await ws1.SendBinaryAsync(new ArrayPacket(payload4K));
        var reply4K = await ws1.ReceiveMessageAsync().WaitAsync(TimeSpan.FromSeconds(30));
        await ws1.CloseAsync("done");

        Assert.NotNull(reply4K);
        Assert.Equal(WebSocketMessageType.Binary, reply4K.Type);
        Assert.Equal(payload4K.Length, reply4K.Payload?.Length);
        Assert.Equal(sentHash, SHA256.HashData(reply4K.Payload?.ToArray() ?? []));

        // ── 子测试 2：DefaultMessage 格式二进制，字节完整性顶对顶验证 ─────────────
        var userPayload = "hello-ws-srmp-binary"u8.ToArray();
        var frame = new Byte[4 + userPayload.Length];
        frame[0] = 0x01;                    // Request + Packet kind
        frame[1] = 0x42;                    // 序列号
        frame[2] = (Byte)userPayload.Length;
        frame[3] = 0x00;
        userPayload.CopyTo(frame, 4);

        var ws2 = new WebSocketClient($"ws://127.0.0.1:{fixture.Port}/ws") { Log = XTrace.Log, MaxAsync = 0 };
        Assert.True(await ws2.OpenAsync());

        await ws2.SendBinaryAsync(new ArrayPacket(frame));
        var replyDM = await ws2.ReceiveMessageAsync().WaitAsync(TimeSpan.FromSeconds(10));
        await ws2.CloseAsync("done");

        Assert.NotNull(replyDM);
        Assert.Equal(WebSocketMessageType.Binary, replyDM.Type);
        Assert.Equal(frame, replyDM.Payload?.ToArray());
    }

    /// <summary>
    /// 20 客户端并发文本收发，走 ReceiveMessageAsync 路径（MaxAsync=0）。
    /// 无需事件订阅，代码更简洁；每个客户端发送后直接 await ReceiveMessageAsync 取回显。
    /// </summary>
    [Fact(DisplayName = "05-20客户端并发收发（ReceiveMessageAsync路径，MaxAsync=0）")]
    public async Task Test05_ConcurrentClients_ReceiveMessageAsync()
    {
        const Int32 count = 20;

        var tasks = Enumerable.Range(0, count).Select(async i =>
        {
            var ws = new WebSocketClient($"ws://127.0.0.1:{fixture.Port}/ws")
            {
                Log = XTrace.Log,
                MaxAsync = 0,
            };
            Assert.True(await ws.OpenAsync());

            var text = $"c{i}-{Guid.NewGuid():N}";
            await ws.SendTextAsync(text);

            var msg = await ws.ReceiveMessageAsync().WaitAsync(TimeSpan.FromSeconds(10));
            var reply = msg?.Payload?.ToStr() ?? String.Empty;

            await ws.CloseAsync("done");
            return (text, reply);
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        foreach (var item in results)
            Assert.Equal($"ws-echo:{item.text}", item.reply);
    }

    /// <summary>
    /// 高吞吐 TPS 测试（Received 事件路径，MaxAsync=1 默认）：
    /// 流水线发送（发完全部再统一接收），由管道内 WebSocketCodec+PacketCodec 负责粘包/拆包，
    /// 确保每个 WS 帧完整触发 Received 事件；
    /// 先预热 500条/客户端，稀释 JIT/线程池扩张冷启动；
    /// 再预先建立所有连接，计时仅覆盖发送+接收阶段；
    /// 正式量为 50 客户端×10000 条（50万总量），断言 TPS≥100000。
    /// </summary>
    [Fact(DisplayName = "06-WebSocket并发吞吐：预热后50客户端×10000消息，TPS≥100000")]
    public async Task Test06_HighThroughput_TPS()
    {
        const Int32 clientCount = 50;
        const Int32 warmupPerClient = 500;
        const Int32 perClient = 10_000;
        const Int32 total = clientCount * perClient;  // 50万

        // ── 预热阶段：500条/客户端，流水线发送+事件接收，稀释冷启动开销 ──────────────
        {
            var warmupTasks = Enumerable.Range(0, clientCount).Select(async i =>
            {
                var localCount = 0;
                var localDone = new TaskCompletionSource<Boolean>();

                var ws = new WebSocketClient($"ws://127.0.0.1:{fixture.Port}/ws")
                {
                    KeepAlive = TimeSpan.Zero,  // 禁用 Ping 计时器，避免 Pong 帧混入计数
                };
                ws.Received += (s, e) =>
                {
                    if (e.Message is WebSocketMessage m && m.Type == WebSocketMessageType.Text)
                        if (Interlocked.Increment(ref localCount) >= warmupPerClient)
                            localDone.TrySetResult(true);
                };
                Assert.True(await ws.OpenAsync());

                for (var j = 0; j < warmupPerClient; j++)
                    await ws.SendTextAsync($"w{i}");

                await localDone.Task.WaitAsync(TimeSpan.FromSeconds(60));
                await ws.CloseAsync(1000, "warmup");
            }).ToArray();

            await Task.WhenAll(warmupTasks).WaitAsync(TimeSpan.FromSeconds(60));
        }

        // ── 预先建立所有连接，计时仅覆盖发送+接收阶段 ──────────────────────────────
        var clients = await Task.WhenAll(Enumerable.Range(0, clientCount).Select(async i =>
        {
            var ws = new WebSocketClient($"ws://127.0.0.1:{fixture.Port}/ws")
            {
                KeepAlive = TimeSpan.Zero,
            };
            Assert.True(await ws.OpenAsync());
            return ws;
        })).WaitAsync(TimeSpan.FromSeconds(30));

        // ── 正式测试：计时从第一次发送到最后一条回显收到 ────────────────────────────
        var completed = 0;
        var sw = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, clientCount).Select(async i =>
        {
            var localCount = 0;
            var localDone = new TaskCompletionSource<Boolean>();
            var ws = clients[i];
            var sendMsg = $"t{i:D3}";  // 预先计算，避免循环内重复分配字符串

            // 管道内 WebSocketCodec+PacketCodec 负责粘包/拆包，每帧独立触发 Received
            ws.Received += (s, e) =>
            {
                if (e.Message is WebSocketMessage m && m.Type == WebSocketMessageType.Text)
                {
                    var local = Interlocked.Increment(ref localCount);
                    Interlocked.Increment(ref completed);
                    if (local >= perClient) localDone.TrySetResult(true);
                }
            };

            for (var j = 0; j < perClient; j++)
                await ws.SendTextAsync(sendMsg);

            await localDone.Task.WaitAsync(TimeSpan.FromSeconds(120));
            await ws.CloseAsync(1000, "done");
        }).ToArray();

        await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(120));
        sw.Stop();

        var tps = total / sw.Elapsed.TotalSeconds;
        XTrace.WriteLine("WebSocket 高吞吐 TPS：{0}条/{1}ms，TPS={2:N0}", total, sw.ElapsedMilliseconds, tps);

        Assert.Equal(total, completed);
        Assert.True(tps >= 100_000, $"TPS={tps:N0}，低于100000，耗时={sw.ElapsedMilliseconds}ms");
    }
}
