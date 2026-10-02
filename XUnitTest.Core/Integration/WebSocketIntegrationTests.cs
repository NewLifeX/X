using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using NewLife;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
using NewLife.Messaging;
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
    /// <summary>服务端收到的客户端帧（类型与掩码键），供测试观察协议细节</summary>
    public static ConcurrentQueue<(WebSocketMessageType Type, Byte[]? MaskKey)> ReceivedFrames { get; } = new();

    /// <summary>服务端直达回调收到的消息（协议模式 MessageHandler，WsMessage 原语）</summary>
    public static ConcurrentQueue<(WebSocketMessageType Type, Int32 Length)> DirectMessages { get; } = new();

    public override void ProcessRequest(IHttpContext context)
    {
        base.ProcessRequest(context);

        if (context.WebSocket is { } ws)
        {
            // 阶段2 直达回调：先于旧 Handler 触发，收 WsMessage 原语（链式保留基类回显回调）
            var inner = ws.MessageHandler;
            ws.MessageHandler = (s, m) =>
            {
                DirectMessages.Enqueue((m.Type, m.Payload?.Length ?? 0));
                inner?.Invoke(s, m);
            };
        }
    }

    public override void ProcessMessage(WebSocket socket, WsMessage message)
    {
        ReceivedFrames.Enqueue((message.Type, message.MaskKey?.ToArray()));

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
    /// 建连+文本+二进制收发+Active验证，走 Received 事件路径（事件模式，后台管道接收循环）。
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
            if (e.Message is WsMessage m)
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
        // ToPacket 会原地 XOR 修改数组，先保存副本
        var originalPayload = payload.ToArray();
        await ws.SendBinaryAsync((ArrayPacket)payload);
        var binaryReply = await binaryWait.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(originalPayload, binaryReply);

        // 发送 WS Close 帧，接收循环检测服务端关闭后 Active 变 false
        await ws.CloseAsync(1000, "done");
        for (var i = 0; i < 50 && ws.Active; i++) await Task.Delay(100);
        Assert.False(ws.Active, "CloseAsync 后 Active 应为 false");
    }

    /// <summary>
    /// 建连+文本+二进制收发+Active验证，走 ReceiveMessageAsync 路径（协议模式：消息泵事件驱动入队）。
    /// 消息到达即入内部队列，ReceiveMessageAsync 出队返回；关闭使用 CloseAsync 发关闭帧。
    /// </summary>
    /// <remarks>
    /// 接收经消息泵事件驱动，支持流水线发送多条消息，不存在粘包丢失问题。
    /// </remarks>
    [Fact(DisplayName = "03-建连+文本+二进制收发+Active验证（ReceiveMessageAsync路径）")]
    public async Task Test03_BasicEcho_ReceiveMessageAsync()
    {
        var ws = new WebSocketClient($"ws://127.0.0.1:{fixture.Port}/ws")
        {
            Log = XTrace.Log,
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
        // ToPacket 会原地 XOR 修改数组，先保存副本
        var originalPayload = payload.ToArray();
        await ws.SendBinaryAsync((ArrayPacket)payload);
        var binaryMsg = await ws.ReceiveMessageAsync().WaitAsync(TimeSpan.FromSeconds(10));
        Assert.NotNull(binaryMsg);
        Assert.Equal(WebSocketMessageType.Binary, binaryMsg.Type);
        Assert.Equal(originalPayload, binaryMsg.Payload?.ToArray());

        // 协议模式：接收环检测服务端关闭后 Active 变 false
        await ws.CloseAsync("done");
        for (var i = 0; i < 50 && ws.Active; i++) await Task.Delay(100);
        Assert.False(ws.Active, "CloseAsync 后 Active 应为 false");
    }

    /// <summary>
    /// 4KB 二进制 SHA256 完整性校验 + DefaultMessage 格式字节完整性，走 ReceiveMessageAsync 路径（协议模式队列）。
    /// </summary>
    [Fact(DisplayName = "04-4KB二进制SHA256校验+DefaultMessage格式完整性（ReceiveMessageAsync路径）")]
    public async Task Test04_LargeBinary_And_DefaultMessage()
    {
        // ── 子测试 1：4 KB 随机数据，SHA256 校验内容完整性 ──────────────────────────
        var payload4K = new Byte[4 * 1024];
        Random.Shared.NextBytes(payload4K);
        var sentHash = SHA256.HashData(payload4K);

        var ws1 = new WebSocketClient($"ws://127.0.0.1:{fixture.Port}/ws") { Log = XTrace.Log };
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
        // ToPacket 会原地 XOR 修改数组，先保存副本
        var originalFrame = frame.ToArray();

        var ws2 = new WebSocketClient($"ws://127.0.0.1:{fixture.Port}/ws") { Log = XTrace.Log };
        Assert.True(await ws2.OpenAsync());

        await ws2.SendBinaryAsync(new ArrayPacket(frame));
        var replyDM = await ws2.ReceiveMessageAsync().WaitAsync(TimeSpan.FromSeconds(10));
        await ws2.CloseAsync("done");

        Assert.NotNull(replyDM);
        Assert.Equal(WebSocketMessageType.Binary, replyDM.Type);
        Assert.Equal(originalFrame, replyDM.Payload?.ToArray());
    }

    /// <summary>
    /// 20 客户端并发文本收发，走 ReceiveMessageAsync 路径（协议模式队列）。
    /// 每个客户端发送后直接 await ReceiveMessageAsync 取回显。
    /// </summary>
    [Fact(DisplayName = "05-20客户端并发收发（ReceiveMessageAsync路径）")]
    public async Task Test05_ConcurrentClients_ReceiveMessageAsync()
    {
        const Int32 count = 20;

        var tasks = Enumerable.Range(0, count).Select(async i =>
        {
            var ws = new WebSocketClient($"ws://127.0.0.1:{fixture.Port}/ws")
            {
                Log = XTrace.Log,
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
    /// 高吞吐 TPS 测试（Received 事件路径，事件模式）：
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
                    if (e.Message is WsMessage m && m.Type == WebSocketMessageType.Text)
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

            // 消息泵负责粘包/拆包，每帧独立触发 Received
            ws.Received += (s, e) =>
            {
                if (e.Message is WsMessage m && m.Type == WebSocketMessageType.Text)
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
        Assert.True(tps >= 50_000, $"TPS={tps:N0}，低于50000，耗时={sw.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// 半帧断开健壮性：客户端发送不完整的大帧头部后立即断开（连接重置），
    /// 服务端 HttpSession/WebSocket 销毁时归还粘包编码器段链缓冲（该路径曾无人释放）；
    /// 随后新连接仍可正常回显，验证服务端未被半帧污染。
    /// </summary>
    [Fact(DisplayName = "07-半帧断开：连接重置后编码器缓冲归还且服务可用")]
    public async Task Test07_PartialFrame_AbruptClose()
    {
        // 构造“声明 10000 字节负载”的 WS 帧头 + 少量负载（客户端方向带掩码）：82 FE 2710 <4字节掩码> + 10字节
        var partial = new Byte[18];
        partial[0] = 0x82;          // FIN + Binary
        partial[1] = 0x80 | 126;    // 带掩码 + 16位扩展长度
        partial[2] = 0x27;          // 10000 高字节
        partial[3] = 0x10;          // 10000 低字节
        // partial[4..8] 掩码（全零即可），partial[8..18] 少量负载

        {
            var ws = new WebSocketClient($"ws://127.0.0.1:{fixture.Port}/ws");
            Assert.True(await ws.OpenAsync());

            ws.Send(new ArrayPacket(partial));

            // 不发送剩余字节，直接断开连接（服务端缓存了半帧）
            ws.Dispose();
        }

        // 等待服务端检测连接重置并清理
        await Task.Delay(500);
        Assert.True(fixture.Server.Active, "半帧断开后服务器应保持可用");

        // 新连接回显正常
        var ws2 = new WebSocketClient($"ws://127.0.0.1:{fixture.Port}/ws");
        Assert.True(await ws2.OpenAsync());

        var text = $"after-partial-{Guid.NewGuid():N}";
        await ws2.SendTextAsync(text);
        var msg = await ws2.ReceiveMessageAsync().WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal($"ws-echo:{text}", msg?.Payload?.ToStr());
        await ws2.CloseAsync("done");
    }

    /// <summary>
    /// 60KB 二进制（客户端掩码 + 跨接收轮链式）SHA256 完整性校验：
    /// 客户端帧按 RFC 6455 自动掩码，服务端在跨段链式帧上逐段 XOR 解码；
    /// 大帧超过单次接收缓冲，走 Received 事件路径（管道内跨轮组链零拷贝）。
    /// </summary>
    [Fact(DisplayName = "08-60KB二进制（掩码+跨段链式）SHA256完整性")]
    public async Task Test08_LargeBinary_MaskedChained()
    {
        var payload = new Byte[60_000];
        Random.Shared.NextBytes(payload);
        // ToPacket 会原地 XOR 修改数组（客户端掩码），先保存副本
        var original = payload.ToArray();
        var sentHash = SHA256.HashData(original);

        var ws = new WebSocketClient($"ws://127.0.0.1:{fixture.Port}/ws");
        Assert.True(await ws.OpenAsync());

        var wait = new TaskCompletionSource<Byte[]>();
        ws.Received += (s, e) =>
        {
            if (e.Message is WsMessage m && m.Type == WebSocketMessageType.Binary)
            {
                // 交付契约：事件内读满（大帧为流式体），数据物化后入信号
                var all = m.Body!.ReadAllAsync().AsTask().GetAwaiter().GetResult();
                var data = all.ToArray();
                all.TryDispose();
                wait.TrySetResult(data);
            }
        };

        await ws.SendBinaryAsync(new ArrayPacket(payload));

        var data = await wait.Task.WaitAsync(TimeSpan.FromSeconds(30));

        await ws.CloseAsync("done");

        Assert.Equal(original.Length, data.Length);
        Assert.Equal(sentHash, SHA256.HashData(data));
    }

    /// <summary>
    /// 客户端出站帧每帧使用新的随机掩码键（RFC 6455 §5.3 要求每帧 fresh key），
    /// 显式设置客户端掩码时优先使用且不被覆盖。
    /// </summary>
    [Fact(DisplayName = "09-客户端逐帧随机掩码键+显式掩码优先")]
    public async Task Test09_PerFrameMaskKey()
    {
        var ws = new WebSocketClient($"ws://127.0.0.1:{fixture.Port}/ws")
        {
            Log = XTrace.Log,
        };
        Assert.True(await ws.OpenAsync());

        var echoCount = 0;
        var done = new TaskCompletionSource<Boolean>();
        ws.Received += (s, e) =>
        {
            if (e.Message is WsMessage m && m.Type == WebSocketMessageType.Text)
                if (Interlocked.Increment(ref echoCount) >= 3) done.TrySetResult(true);
        };

        var start = WsEchoHandler.ReceivedFrames.Count;

        await ws.SendTextAsync("mask-1");
        await ws.SendTextAsync("mask-2");

        // 显式设置客户端级掩码：后续帧使用该掩码（不被每帧随机策略覆盖）
        var custom = new Byte[] { 0x12, 0x34, 0x56, 0x78 };
        ws.MaskKey = custom;

        await ws.SendTextAsync("mask-3");
        await done.Task.WaitAsync(TimeSpan.FromSeconds(10));

        var keys = WsEchoHandler.ReceivedFrames.ToArray()
            .Skip(start)
            .Where(e => e.Type == WebSocketMessageType.Text)
            .Take(3)
            .Select(e => e.MaskKey)
            .ToArray();
        Assert.Equal(3, keys.Length);
        Assert.All(keys, k => Assert.NotNull(k));
        Assert.All(keys, k => Assert.Equal(4, k!.Length));

        // 前两帧每帧新的随机键（同一 4 字节随机值重复概率约 2^-32）
        Assert.NotEqual(keys[0], keys[1]);

        // 第三帧使用显式设置的掩码
        Assert.Equal(custom, keys[2]);

        await ws.CloseAsync(1000, "done");
    }

    /// <summary>
    /// wss（TLS）建连+握手+文本回显：自签证书启动 HTTPS 服务端；
    /// 覆盖打开窗口内同步 Receive 直读 SSL 流的握手路径（修复前读到 TLS 密文导致握手失败）。
    /// </summary>
    [Fact(DisplayName = "10-wss建连+握手+文本回显（自签证书）")]
    public async Task Test10_WssEcho_SelfSigned()
    {
        // 自签证书启动 wss 服务端（随机端口）
        var asm = typeof(WebSocketIntegrationTests).Assembly;
        using var stream = asm.GetManifestResourceStream("XUnitTest.certs.newlifex.com.pfx")!;
        var pfx = stream.ReadBytes(-1);
#if NET9_0_OR_GREATER
        var cert = X509CertificateLoader.LoadPkcs12(pfx, "123456");
#else
        var cert = new X509Certificate2(pfx, "123456", X509KeyStorageFlags.DefaultKeySet);
#endif
        using var server = new HttpServer
        {
            Name = "wss集成测试服务器",
            Local = new NetUri(NetType.Https, IPAddress.Loopback, 0),
            Certificate = cert,
            SslProtocol = SslProtocols.Tls12,
            Log = XTrace.Log,
        };
        server.Map("/ws", new WsEchoHandler());
        server.Start();

        var ws = new WebSocketClient($"wss://127.0.0.1:{server.Port}/ws")
        {
            SslProtocol = SslProtocols.Tls12,
            Log = XTrace.Log,
        };

        Assert.True(await ws.OpenAsync(), "wss 建连与握手应成功");
        Assert.True(ws.Active);

        var textWait = new TaskCompletionSource<String>();
        ws.Received += (s, e) =>
        {
            if (e.Message is WsMessage m && m.Type == WebSocketMessageType.Text)
                textWait.TrySetResult(m.Payload?.ToStr() ?? String.Empty);
        };

        var text = "hello-wss";
        await ws.SendTextAsync(text);
        var reply = await textWait.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal($"ws-echo:{text}", reply);

        await ws.CloseAsync(1000, "done");
    }

    /// <summary>60KB 大帧走 ReceiveMessageAsync 拉取路径：内部队列按交付契约物化流式体，拉取方获得完整负载</summary>
    [Fact(DisplayName = "11-60KB大帧走ReceiveMessageAsync拉取（队列物化流式体）")]
    public async Task Test11_LargeBinary_PullPath()
    {
        var ws = new WebSocketClient($"ws://127.0.0.1:{fixture.Port}/ws") { Log = XTrace.Log };
        Assert.True(await ws.OpenAsync());

        var payload = new Byte[60 * 1024];
        Random.Shared.NextBytes(payload);
        var sentHash = SHA256.HashData(payload);
        // ToPacket 会原地 XOR 修改数组，先保存副本
        var original = payload.ToArray();

        await ws.SendBinaryAsync(new ArrayPacket(payload));
        var reply = await ws.ReceiveMessageAsync().WaitAsync(TimeSpan.FromSeconds(30));
        await ws.CloseAsync("done");

        Assert.NotNull(reply);
        Assert.Equal(WebSocketMessageType.Binary, reply.Type);
        var data = reply.Payload?.ToArray() ?? [];
        Assert.Equal(original.Length, data.Length);
        Assert.Equal(sentHash, SHA256.HashData(data));
    }

    /// <summary>服务端 MessageHandler 直达回调：先于旧 Handler 收到 WsMessage 原语（协议模式旁路监听）</summary>
    [Fact(DisplayName = "12-服务端MessageHandler直达回调收WsMessage")]
    public async Task Test12_ServerMessageHandler_Direct()
    {
        while (WsEchoHandler.DirectMessages.TryDequeue(out _)) { }

        var ws = new WebSocketClient($"ws://127.0.0.1:{fixture.Port}/ws") { Log = XTrace.Log };
        Assert.True(await ws.OpenAsync());

        await ws.SendTextAsync("direct-callback");
        // 等待直达回调入队
        for (var i = 0; i < 50 && WsEchoHandler.DirectMessages.IsEmpty; i++) await Task.Delay(100);

        Assert.True(WsEchoHandler.DirectMessages.TryDequeue(out var item), "MessageHandler 未收到消息");
        Assert.Equal(WebSocketMessageType.Text, item.Type);
        Assert.True(item.Length > 0, "直达消息应携带负载长度");

        await ws.CloseAsync(1000, "done");
    }

    /// <summary>流水线收取：连发 10 条不等回显，ReceiveMessageAsync 逐条出队不丢帧且保序</summary>
    [Fact(DisplayName = "13-流水线连发10条逐条收取（不丢帧保序）")]
    public async Task Test13_PipelinedReceive_NoLoss()
    {
        var ws = new WebSocketClient($"ws://127.0.0.1:{fixture.Port}/ws") { Log = XTrace.Log };
        Assert.True(await ws.OpenAsync());

        const Int32 count = 10;
        for (var i = 0; i < count; i++)
        {
            await ws.SendTextAsync($"pipe-{i}");
        }

        var received = new List<String>();
        for (var i = 0; i < count; i++)
        {
            var msg = await ws.ReceiveMessageAsync().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.NotNull(msg);
            received.Add(msg.Payload?.ToStr() ?? String.Empty);
        }
        await ws.CloseAsync("done");

        Assert.Equal(count, received.Count);
        for (var i = 0; i < count; i++)
        {
            // 服务端串行处理回显，TCP 保序
            Assert.Equal($"ws-echo:pipe-{i}", received[i]);
        }
    }

    /// <summary>分片重组：客户端发送两片（FIN=0 + FIN=1），服务端合并为一条完整消息回显</summary>
    [Fact(DisplayName = "14-分片重组：两片分帧发送合并为完整消息回显")]
    public async Task Test14_FragmentedMessage_Reassembled()
    {
        var ws = new WebSocketClient($"ws://127.0.0.1:{fixture.Port}/ws") { Log = XTrace.Log };
        Assert.True(await ws.OpenAsync());

        // 手工构造两片带掩码帧（绕过 codec 构建）：首片 Text/"hel"（FIN=0）+ 末片续帧/"lo"（FIN=1）
        ws.Send(new ArrayPacket(MakeFragmentFrame(0x01, false, "hel"u8.ToArray())));
        ws.Send(new ArrayPacket(MakeFragmentFrame(0x00, true, "lo"u8.ToArray())));

        // 服务端应把两片重组为一条完整消息 "hello" 后回显
        var reply = await ws.ReceiveMessageAsync().WaitAsync(TimeSpan.FromSeconds(10));
        Assert.NotNull(reply);
        Assert.Equal(WebSocketMessageType.Text, reply.Type);
        Assert.True(reply.Fin);
        Assert.Equal("ws-echo:hello", reply.Payload?.ToStr());

        await ws.CloseAsync(1000, "done");
    }

    [Fact(DisplayName = "15-未掩码客户端帧_服务端按协议错误关闭连接")]
    public async Task Test15_UnmaskedClientFrame_ConnectionClosed()
    {
        var ws = new WebSocketClient($"ws://127.0.0.1:{fixture.Port}/ws");
        Assert.True(await ws.OpenAsync());

        // 手工构造未掩码文本帧（违反 RFC 6455 §5.1：客户端发给服务端的每一帧都必须带掩码）
        ws.Send(new ArrayPacket(new Byte[] { 0x81, 0x02, (Byte)'h', (Byte)'i' }));

        // 服务端应判定协议错误并关闭连接
        var sw = Stopwatch.StartNew();
        while (ws.Active && sw.ElapsedMilliseconds < 5_000) await Task.Delay(50);

        Assert.False(ws.Active, "未掩码帧应导致服务端关闭连接");
        ws.Dispose();

        Assert.True(fixture.Server.Active, "服务器应保持可用");
    }

    [Fact(DisplayName = "16-超长控制帧_服务端按协议错误关闭连接")]
    public async Task Test16_OversizedControlFrame_ConnectionClosed()
    {
        var ws = new WebSocketClient($"ws://127.0.0.1:{fixture.Port}/ws");
        Assert.True(await ws.OpenAsync());

        // 手工构造 200 字节负载的 Ping 帧（违反 RFC 6455 §5.5：控制帧负载不得超过 125 字节）。
        // 不校验会让对端用超大 Ping 触发等量 Pong 回显（反射放大）
        ws.Send(new ArrayPacket(MakeMaskedFrame(0x09, true, new Byte[200])));

        var sw = Stopwatch.StartNew();
        while (ws.Active && sw.ElapsedMilliseconds < 5_000) await Task.Delay(50);

        Assert.False(ws.Active, "超长控制帧应导致服务端关闭连接");
        ws.Dispose();

        Assert.True(fixture.Server.Active, "服务器应保持可用");
    }

    [Fact(DisplayName = "17-CloseAsync带状态码_发送关闭帧并关闭连接")]
    public async Task Test17_CloseAsyncWithStatus_ClosesConnection()
    {
        var ws = new WebSocketClient($"ws://127.0.0.1:{fixture.Port}/ws");
        Assert.True(await ws.OpenAsync());
        Assert.True(ws.Active);

        await ws.CloseAsync(1000, "done");

        // 旧实现只发 Close 帧就返回，会话仍 Active、心跳继续运行
        Assert.False(ws.Active, "CloseAsync(状态码) 应关闭连接");
        ws.Dispose();

        Assert.True(fixture.Server.Active, "服务器应保持可用");
    }

    [Fact(DisplayName = "18-分片累计超限_服务端失败连接而非静默丢弃")]
    public async Task Test18_FragmentTooBig_ConnectionClosed()
    {
        var ws = new WebSocketClient($"ws://127.0.0.1:{fixture.Port}/ws");
        Assert.True(await ws.OpenAsync());

        // 分片上限是累计口径，单帧都不大也能触发：用 60KB 首片(Binary,FIN=0)+续片(Data)把累计推过 16MB，
        // 避免测试为制造超限而分配单个 16MB 大帧
        var chunk = new Byte[60_000];
        try
        {
            ws.Send(new ArrayPacket(MakeMaskedFrame(0x02, false, chunk)));
            for (var i = 0; i < 300 && ws.Active; i++)
            {
                ws.Send(new ArrayPacket(MakeMaskedFrame(0x00, false, chunk)));
            }
        }
        catch
        {
            // 服务端可能在中途就已失败连接，尾部分片发不出去
        }

        // RFC 6455 §7.4.1：分片超限应失败连接（1009 Message Too Big），而非丢弃后维持连接——
        // 静默丢弃会让客户端以为消息已送达，后续续片还会被当成孤立续片一路吞掉
        var sw = Stopwatch.StartNew();
        while (ws.Active && sw.ElapsedMilliseconds < 10_000) await Task.Delay(50);

        Assert.False(ws.Active, "分片累计超限应导致服务端失败连接");
        ws.Dispose();

        Assert.True(fixture.Server.Active, "服务器应保持可用");
    }

    /// <summary>构造客户端掩码原始帧（支持 126 扩展长度）</summary>
    /// <param name="opcode">操作码（1=Text，2=Binary，8=Close，9=Ping，10=Pong）</param>
    /// <param name="fin">是否末片</param>
    /// <param name="payload">负载</param>
    private static Byte[] MakeMaskedFrame(Byte opcode, Boolean fin, Byte[] payload)
    {
        var key = new Byte[] { 0x11, 0x22, 0x33, 0x44 };
        var ext = payload.Length > 125 ? 2 : 0;
        var buf = new Byte[2 + ext + 4 + payload.Length];
        buf[0] = (Byte)((fin ? 0x80 : 0) | opcode);
        if (ext == 0)
            buf[1] = (Byte)(0x80 | payload.Length);
        else
        {
            buf[1] = 0x80 | 126;
            buf[2] = (Byte)(payload.Length >> 8);
            buf[3] = (Byte)(payload.Length & 0xFF);
        }

        key.CopyTo(buf, 2 + ext);
        for (var i = 0; i < payload.Length; i++) buf[2 + ext + 4 + i] = (Byte)(payload[i] ^ key[i & 3]);

        return buf;
    }

    /// <summary>构造分片原始帧（客户端方向：带固定掩码，短长度）</summary>
    /// <param name="opcode">操作码（1=Text 首片，0=续片）</param>
    /// <param name="fin">是否末片</param>
    /// <param name="payload">负载（不超过 125 字节）</param>
    private static Byte[] MakeFragmentFrame(Byte opcode, Boolean fin, Byte[] payload)
    {
        var key = new Byte[] { 0x12, 0x34, 0x56, 0x78 };
        var buf = new Byte[6 + payload.Length];
        buf[0] = (Byte)((fin ? 0x80 : 0) | opcode);
        buf[1] = (Byte)(0x80 | payload.Length);
        key.CopyTo(buf, 2);
        for (var i = 0; i < payload.Length; i++) buf[6 + i] = (Byte)(payload[i] ^ key[i & 3]);

        return buf;
    }
}
