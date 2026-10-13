using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
using NewLife.Net;
using NewLife.Remoting;
using Xunit;

// 本文件部分用例涉及数据包链式语义（接口形式上链，结构体需以 IPacket 变量持有才能拼接）

namespace XUnitTest.Http;

public class HttpServerTests : IDisposable
{
    private readonly HttpServer _server;
    private readonly Uri _baseUri;
    public HttpServerTests()
    {
        var server = new HttpServer
        {
            Port = 0, // 使用动态端口避免冲突
            Log = XTrace.Log,
            SessionLog = XTrace.Log
        };
        server.Start();

        _server = server;
        _baseUri = new Uri($"http://127.0.0.1:{server.Port}");
    }

    public void Dispose()
    {
        _server?.Dispose();
    }

    /// <summary>读取直到累积内容满足条件，用于流水线/临时响应等需要多次读取的场景</summary>
    private static async Task<String> ReadUntilAsync(NetworkStream ns, Func<String, Boolean> done)
    {
        var ms = new MemoryStream();
        var buf = new Byte[4096];
        while (!done(Encoding.ASCII.GetString(ms.ToArray())))
        {
            var n = await ns.ReadAsync(buf.AsMemory()).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
            if (n <= 0) break;

            ms.Write(buf, 0, n);
        }

        return Encoding.ASCII.GetString(ms.ToArray());
    }

    [Fact]
    [DisplayName("HTTP服务端_声明升级但握手不合法_回400而非404")]
    public async Task WebSocketHandshake_Invalid_ReturnsBadRequest()
    {
        // 带 Sec-WebSocket-Key 但缺 Upgrade/Connection：既不能静默当 WebSocket 接管，
        // 也不该落到普通路由报 404 掩盖真实原因
        var client = new HttpClient { BaseAddress = _baseUri };
        var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "/ws");
        req.Headers.TryAddWithoutValidation("Sec-WebSocket-Key", "dGhlIHNhbXBsZSBub25jZQ==");

        var res = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact(DisplayName = "HTTP服务端_WS握手与首帧同段到达_首帧不丢")]
    public async Task WebSocketUpgrade_FirstFrameInSameSegment_NotLost()
    {
        var handler = new WsCaptureHandler(1);
        _server.Map("/ws-same", handler);

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        // RFC 6455 §4.1 允许客户端发完握手请求就发帧，两者常落在同一个 TCP 段/同一轮接收里。
        // 升级后同轮剩余字节此前被直接丢弃，首帧静默消失（"连上了但第一条消息没到"）
        var head = BuildHandshake("/ws-same");
        var frame = BuildMaskedTextFrame("hello");
        await ns.WriteAsync(head.Concat(frame).ToArray());
        await ns.FlushAsync();

        var resp = await ReadUntilAsync(ns, s => s.Contains("101"));
        Assert.Contains("101 SwitchingProtocols", resp);

        await handler.Done.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(new[] { "hello" }, handler.Messages);
    }

    [Fact(DisplayName = "HTTP服务端_WS握手与两帧同段到达_两帧按序交付")]
    public async Task WebSocketUpgrade_TwoFramesInSameSegment_BothDelivered()
    {
        var handler = new WsCaptureHandler(2);
        _server.Map("/ws-two", handler);

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        var data = new List<Byte>();
        data.AddRange(BuildHandshake("/ws-two"));
        data.AddRange(BuildMaskedTextFrame("one"));
        data.AddRange(BuildMaskedTextFrame("two"));
        await ns.WriteAsync(data.ToArray());
        await ns.FlushAsync();

        var resp = await ReadUntilAsync(ns, s => s.Contains("101"));
        Assert.Contains("101 SwitchingProtocols", resp);

        // 同段多帧必须全部交付且保序（管道内完成粘包拆包）
        await handler.Done.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(new[] { "one", "two" }, handler.Messages);
    }

    [Fact(DisplayName = "HTTP服务端_WS单帧超限_按1009关闭")]
    public async Task WebSocket_OversizeFrame_ClosesWith1009()
    {
        // 单帧上限是整帧模式的内存安全阀；超限属“消息过大”（1009），此前一律按协议错误（1002）关闭
        var handler = new WsCaptureHandler(1) { MaxFrameSize = 64 };
        _server.Map("/ws-big", handler);

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        var head = BuildHandshake("/ws-big");
        await ns.WriteAsync(head);
        await ns.FlushAsync();
        var resp = await ReadUntilAsync(ns, s => s.Contains("101"));
        Assert.Contains("101 SwitchingProtocols", resp);

        // 100 字节文本帧 > 64 字节上限（整帧一次到达，上限对已完整帧同样生效）
        await ns.WriteAsync(BuildMaskedFrame(0x1, new Byte[100]));
        await ns.FlushAsync();

        Assert.Equal(1009, await ReadCloseStatusAsync(ns));
    }

    [Fact(DisplayName = "HTTP服务端_WS孤立续片_按1002关闭")]
    public async Task WebSocket_StrayContinuation_ClosesWith1002()
    {
        // 无首片的续片（opcode=0）属协议错误（RFC 6455 §5.4）：静默忽略会让对端以为消息已送达
        var handler = new WsCaptureHandler(1);
        _server.Map("/ws-stray", handler);

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        await ns.WriteAsync(BuildHandshake("/ws-stray"));
        await ns.FlushAsync();
        var resp = await ReadUntilAsync(ns, s => s.Contains("101"));
        Assert.Contains("101 SwitchingProtocols", resp);

        await ns.WriteAsync(BuildMaskedFrame(0x0, "tail".GetBytes()));
        await ns.FlushAsync();

        Assert.Equal(1002, await ReadCloseStatusAsync(ns));
    }

    [Fact(DisplayName = "HTTP服务端_WS文本帧非法UTF8_按1007关闭")]
    public async Task WebSocket_InvalidUtf8Text_ClosesWith1007()
    {
        // RFC 6455 §8.1：文本消息必须是合法 UTF-8（0xFF 不是），畸变数据按 1007 失败连接，不能交给业务
        var handler = new WsCaptureHandler(1);
        _server.Map("/ws-utf8", handler);

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        await ns.WriteAsync(BuildHandshake("/ws-utf8"));
        await ns.FlushAsync();
        var resp = await ReadUntilAsync(ns, s => s.Contains("101"));
        Assert.Contains("101 SwitchingProtocols", resp);

        await ns.WriteAsync(BuildMaskedFrame(0x1, new Byte[] { 0x41, 0xFF, 0x42 }));
        await ns.FlushAsync();

        Assert.Equal(1007, await ReadCloseStatusAsync(ns));
        Assert.Empty(handler.Messages);
    }

    /// <summary>读取服务端下发的关闭帧状态码（服务端帧不带掩码）</summary>
    private static async Task<Int32> ReadCloseStatusAsync(NetworkStream ns)
    {
        var head = await ReadExactAsync(ns, 2);
        Assert.Equal(0x88, head[0]);                    // FIN=1 + opcode=8（Close）

        var len = head[1] & 0x7F;
        var payload = await ReadExactAsync(ns, len);

        return payload.Length >= 2 ? (payload[0] << 8) | payload[1] : 0;
    }

    /// <summary>读满指定字节数（TCP 可能分片到达）</summary>
    private static async Task<Byte[]> ReadExactAsync(NetworkStream ns, Int32 count)
    {
        var buf = new Byte[count];
        var read = 0;
        while (read < count)
        {
            var n = await ns.ReadAsync(buf.AsMemory(read, count - read)).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
            if (n <= 0) break;

            read += n;
        }

        return buf;
    }

    /// <summary>构造 WebSocket 握手请求（客户端方向）</summary>
    private static Byte[] BuildHandshake(String path)
    {
        var key = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var head = $"GET {path} HTTP/1.1\r\nHost: 127.0.0.1\r\nUpgrade: websocket\r\nConnection: Upgrade\r\n" +
                   $"Sec-WebSocket-Key: {key}\r\nSec-WebSocket-Version: 13\r\n\r\n";

        return Encoding.ASCII.GetBytes(head);
    }

    /// <summary>构造带掩码的文本帧（客户端方向必须掩码，RFC 6455 §5.1）</summary>
    private static Byte[] BuildMaskedTextFrame(String text) => BuildMaskedFrame(0x1, Encoding.UTF8.GetBytes(text));

    /// <summary>构造带掩码的 WebSocket 帧（客户端方向必须掩码，RFC 6455 §5.1）</summary>
    private static Byte[] BuildMaskedFrame(Int32 opCode, Byte[] payload)
    {
        var frame = new Byte[payload.Length + 6];
        frame[0] = (Byte)(0x80 | opCode);         // FIN=1，opcode
        frame[1] = (Byte)(0x80 | payload.Length); // MASK=1，短长度
        var key = new Byte[] { 1, 2, 3, 4 };
        key.CopyTo(frame, 2);
        for (var i = 0; i < payload.Length; i++) frame[6 + i] = (Byte)(payload[i] ^ key[i % 4]);

        return frame;
    }

    /// <summary>捕获 WebSocket 文本帧的处理器：收满预期条数后置位 <see cref="Done"/></summary>
    private sealed class WsCaptureHandler : IHttpHandler
    {
        private readonly Int32 _expect;

        public WsCaptureHandler(Int32 expect) => _expect = expect;

        /// <summary>单帧长度上限（大于 0 时写入 WebSocket 会话）</summary>
        public Int32 MaxFrameSize { get; set; }

        /// <summary>收到的文本帧（按到达顺序）</summary>
        public List<String> Messages { get; } = [];

        /// <summary>收满预期条数时完成</summary>
        public TaskCompletionSource<Boolean> Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task ProcessRequestAsync(IHttpContext context)
        {
            var ws = context.WebSocket;
            if (ws == null) return TaskEx.CompletedTask;

            if (MaxFrameSize > 0) ws.MaxFrameSize = MaxFrameSize;

            ws.MessageHandler = (s, m) =>
            {
                if (m.Type != NewLife.Messaging.WebSocketMessageType.Text) return;

                lock (Messages)
                {
                    Messages.Add(m.Payload?.ToStr() ?? "");
                    if (Messages.Count >= _expect) Done.TrySetResult(true);
                }
            };

            return TaskEx.CompletedTask;
        }
    }

    [Fact(DisplayName = "HTTP服务端_主体分两轮到达且其后跟随其它字节_按Content-Length截断")]
    public async Task BodyTruncated_ByContentLength()
    {
        var tcs = new TaskCompletionSource<String>();
        _server.MapPost("/trunc", ctx => tcs.TrySetResult(ctx.Request.Body?.ToStr() ?? ""));

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        // 头部与一半主体先到，服务端进入缓存等待后续分片
        await ns.WriteAsync("POST /trunc HTTP/1.1\r\nHost: 127.0.0.1\r\nContent-Length: 4\r\n\r\n"u8.ToArray());
        await ns.WriteAsync("da"u8.ToArray());
        await ns.FlushAsync();
        await Task.Delay(100);

        // 剩余主体与其后紧跟的其它字节同轮到达，后者不属于本请求
        await ns.WriteAsync("taXXXX"u8.ToArray());
        await ns.FlushAsync();

        var body = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("data", body);
    }

    [Fact(DisplayName = "HTTP服务端_主体分片恰好是合法请求头_不得吞掉在途请求")]
    public async Task BodyRoundLooksLikeHead_KeepsPendingRequest()
    {
        // 在途请求的实体尚未收完时，本轮字节一律属于该实体，不得再尝试解析新的请求头。
        // 旧实现先 Parse，实体内恰好出现一段合法请求头（代理转发、恶意构造）时会在此覆盖在途请求并丢弃缓存：
        // 原请求永不回响应，其实体字节被当成下一个请求处理，连接边界失步（与前置代理共存即为请求走私面）
        var tcs = new TaskCompletionSource<String>();
        _server.MapPost("/echo", ctx => tcs.TrySetResult(ctx.Request.Body?.ToStr() ?? ""));

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        var evil = "GET /smuggle HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n";
        var body = "AAAA" + evil;
        var head = $"POST /echo HTTP/1.1\r\nHost: 127.0.0.1\r\nContent-Length: {body.Length}\r\n\r\n";

        // 第一轮：请求头 + 主体前 4 字节，服务端进入实体缓存等待后续分片
        await ns.WriteAsync((head + "AAAA").GetBytes());
        await ns.FlushAsync();
        await Task.Delay(100);

        // 第二轮：剩余主体恰好以一段合法请求头开头
        await ns.WriteAsync(evil.GetBytes());
        await ns.FlushAsync();

        // 在途的 POST /echo 必须收满实体并交付业务，而不是被实体里的“新请求”顶掉
        var received = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(body, received);

        // 响应必须是 /echo 的 200；旧行为回的是被误认请求的 404
        var buf = new Byte[4096];
        var n = await ns.ReadAsync(buf.AsMemory()).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.StartsWith("HTTP/1.1 200", buf.AsSpan(0, n).ToStr());
    }

    [Theory(DisplayName = "HTTP服务端_Transfer-Encoding非chunked_回400")]
    [InlineData("gzip", 400)]
    [InlineData("identity", 400)]
    public async Task TransferEncoding_Unsupported_Rejected(String te, Int32 status)
    {
        // 本库不实现其它传输编码：取值会退化成“无体”，声明的主体被当成后续请求字节（连接失步），回 400；
        // chunked 已支持，见分块请求体各用例
        _server.Map("/te", () => "OK");

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        await ns.WriteAsync($"POST /te HTTP/1.1\r\nHost: 127.0.0.1\r\nTransfer-Encoding: {te}\r\n\r\n".GetBytes());
        await ns.FlushAsync();

        var buf = new Byte[4096];
        var n = await ns.ReadAsync(buf.AsMemory()).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.StartsWith($"HTTP/1.1 {status}", buf.AsSpan(0, n).ToStr());
    }

    [Fact(DisplayName = "HTTP服务端_分块请求体_单轮到达_解码后交付")]
    public async Task ChunkedBody_SingleRound_Decoded()
    {
        var tcs = new TaskCompletionSource<String>();
        _server.MapPost("/chunked", ctx =>
        {
            var te = ctx.Request.Headers["Transfer-Encoding"] ?? "";
            var cl = ctx.Request.Headers["Content-Length"] ?? "";
            tcs.TrySetResult($"{ctx.Request.Body?.ToStr()}|TE={te}|CL={cl}");
        });

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        var head = "POST /chunked HTTP/1.1\r\nHost: 127.0.0.1\r\nTransfer-Encoding: chunked\r\n\r\n";
        var body = "5\r\nHello\r\n7\r\n, world\r\n0\r\n\r\n";
        await ns.WriteAsync((head + body).GetBytes());
        await ns.FlushAsync();

        // 旧实现回 411；新实现解码后按已知长度交付，业务侧看到的主体与 Content-Length 请求一致。
        // 头部同样要对齐：Transfer-Encoding 必须移除（下游按它判断“实体是否为原始分块”，留着会让下游重复解码），
        // Content-Length 补齐为解码后的长度
        Assert.Equal("Hello, world|TE=|CL=12", await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5)));

        var text = await ReadUntilAsync(ns, t => t.Contains("\r\n\r\n"));
        Assert.StartsWith("HTTP/1.1 200", text);
    }

    [Fact(DisplayName = "HTTP服务端_分块体与ContentLength同现_单轮到达仍正常解码")]
    public async Task ChunkedBody_WithContentLength_SingleRound_Decoded()
    {
        // RFC 9112 §6.1：TE 覆盖 CL。旧实现按 CL 截断实体，再把截断前缀当分块流解码，同轮其余字节被丢弃，
        // 请求永久悬停（实测无响应）。本用例锁定既有决策「不拒绝、按 chunked 处理」真正成立
        var tcs = new TaskCompletionSource<String>();
        _server.MapPost("/chunked-cl", ctx => tcs.TrySetResult(ctx.Request.Body?.ToStr() ?? ""));

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        var head = "POST /chunked-cl HTTP/1.1\r\nHost: 127.0.0.1\r\nTransfer-Encoding: chunked\r\nContent-Length: 5\r\n\r\n";
        var body = "5\r\nhello\r\n0\r\n\r\n";
        await ns.WriteAsync((head + body).GetBytes());
        await ns.FlushAsync();

        Assert.Equal("hello", await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5)));

        var text = await ReadUntilAsync(ns, t => t.Contains("\r\n\r\n"));
        Assert.StartsWith("HTTP/1.1 200", text);
    }

    [Fact(DisplayName = "HTTP服务端_分块请求体_跨轮分片_解码后交付")]
    public async Task ChunkedBody_SplitAcrossRounds_Decoded()
    {
        var tcs = new TaskCompletionSource<String>();
        _server.MapPost("/chunked2", ctx => tcs.TrySetResult(ctx.Request.Body?.ToStr() ?? ""));

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        var head = "POST /chunked2 HTTP/1.1\r\nHost: 127.0.0.1\r\nTransfer-Encoding: chunked\r\n\r\n";
        // 第一轮：头部 + 块长度行 + 半个块数据
        await ns.WriteAsync((head + "A\r\n01234").GetBytes());
        await ns.FlushAsync();
        await Task.Delay(100);

        // 第二轮：块数据尾部 + CRLF + 终止块
        await ns.WriteAsync("56789\r\n0\r\n\r\n".GetBytes());
        await ns.FlushAsync();

        Assert.Equal("0123456789", await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact(DisplayName = "HTTP服务端_分块请求体带trailer_其后流水线请求也得到响应")]
    public async Task ChunkedBody_WithTrailers_PipelinedAnswered()
    {
        var tcs = new TaskCompletionSource<String>();
        _server.MapPost("/chunked3", ctx => tcs.TrySetResult(ctx.Request.Body?.ToStr() ?? ""));
        _server.MapGet("/next", () => "NEXT");

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        var head = "POST /chunked3 HTTP/1.1\r\nHost: 127.0.0.1\r\nTransfer-Encoding: chunked\r\n\r\n";
        var body = "3\r\nabc\r\n0\r\nX-Trailer: v\r\n\r\n";
        var next = "GET /next HTTP/1.1\r\nHost: 127.0.0.1\r\nConnection: close\r\n\r\n";

        // trailer 段与在它之后的流水线请求同轮到达：trailer 必须被一并消费，剩余字节属于下一个请求
        await ns.WriteAsync((head + body + next).GetBytes());
        await ns.FlushAsync();

        Assert.Equal("abc", await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5)));

        var text = await ReadUntilAsync(ns, t => t.Contains("NEXT"));
        Assert.Contains("200", text);
        Assert.Contains("NEXT", text);
    }

    [Fact(DisplayName = "HTTP服务端_分块长度行非法_回400")]
    public async Task ChunkedBody_MalformedSize_ReturnsBadRequest()
    {
        _server.MapPost("/chunked4", ctx => { });

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        var head = "POST /chunked4 HTTP/1.1\r\nHost: 127.0.0.1\r\nTransfer-Encoding: chunked\r\n\r\n";
        // 坏块长度行（非十六进制）不得静默等待，按协议错误回 400 并关闭
        await ns.WriteAsync((head + "zz\r\nabc\r\n0\r\n\r\n").GetBytes());
        await ns.FlushAsync();

        var buf = new Byte[4096];
        var n = await ns.ReadAsync(buf.AsMemory()).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.StartsWith("HTTP/1.1 400", buf.AsSpan(0, n).ToStr());
    }

    /// <summary>把在途请求停滞上限压低的服务器。HttpSession 按连接创建，配置入口是重载 CreateHandler</summary>
    /// <remarks>阈值取 3 秒：满载调度抖动可达秒级，1 秒阈值会被误触（见下方两个用例的判定口径）</remarks>
    private sealed class SlowIdleHttpServer(Int32 idleMs = 3_000) : HttpServer
    {
        public override INetHandler? CreateHandler(INetSession session) => new HttpSession { MaxRequestIdle = idleMs };
    }

    [Fact(DisplayName = "HTTP服务端_在途请求长期无进展_回408并关闭")]
    public async Task StalledRequest_ReturnsRequestTimeout()
    {
        using var server = new SlowIdleHttpServer { Port = 0, Log = XTrace.Log };
        server.Map("/slow", () => "OK");
        server.Start();

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, server.Port);
        using var ns = client.GetStream();

        // 第一轮：只发半个请求头，服务端进入头部缓存等待后续分片
        await ns.WriteAsync("GET /slow HTTP/1.1\r\nHost: 127.0.0.1\r\n"u8.ToArray());
        await ns.FlushAsync();

        // 停滞超过 MaxRequestIdle（3s）后才有新字节：滴字节占用连接的慢速攻击应被回 408 并断开
        await Task.Delay(3_500);
        await ns.WriteAsync("X: 1\r\n\r\n"u8.ToArray());
        await ns.FlushAsync();

        var buf = new Byte[4096];
        var n = await ns.ReadAsync(buf.AsMemory()).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.StartsWith("HTTP/1.1 408", buf.AsSpan(0, n).ToStr());
    }

    [Fact(DisplayName = "HTTP服务端_持续推进的慢速上传_不触发停滞判定")]
    public async Task SlowButProgressingUpload_NotRejected()
    {
        using var server = new SlowIdleHttpServer { Port = 0, Log = XTrace.Log };
        var tcs = new TaskCompletionSource<String>();
        server.MapPost("/up", ctx => tcs.TrySetResult(ctx.Request.Body?.ToStr() ?? ""));
        server.Start();

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, server.Port);
        using var ns = client.GetStream();

        await ns.WriteAsync("POST /up HTTP/1.1\r\nHost: 127.0.0.1\r\nContent-Length: 6\r\n\r\n"u8.ToArray());

        // 单次间隔 200ms、阈值 3s（15 倍余量），总耗时远超阈值：判定看的是“停滞”，不得打断有进展的上传
        foreach (var ch in "abcdef")
        {
            await Task.Delay(200);
            await ns.WriteAsync(new[] { (Byte)ch });
            await ns.FlushAsync();
        }

        Assert.Equal("abcdef", await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact(DisplayName = "HTTP服务端_聘形请求行_回400而非静默挂起")]
    public async Task MalformedRequestLine_ReturnsBadRequest()
    {
        _server.MapGet("/mf", () => "OK");

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        // 头部完整（含空行）但请求行第三段不是 HTTP/：必须显式回 400 并关闭，不能既不回响应也不关连接
        var head = "GET /mf FOO/1.1\r\nHost: 127.0.0.1\r\n\r\n";
        await ns.WriteAsync(head.GetBytes());
        await ns.FlushAsync();

        var buf = new Byte[4096];
        var n = await ns.ReadAsync(buf.AsMemory()).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.StartsWith("HTTP/1.1 400", buf.AsSpan(0, n).ToStr());
    }

    [Fact(DisplayName = "HTTP服务端_首行不像HTTP且无空行_回400而非静默丢弃")]
    public async Task NotHttpHead_ReturnsBadRequest()
    {
        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        // 前10字节内既无空格也无空行：不可能再补齐成合法请求头。
        // 旧实现直接丢弃这些字节——既不回响应也不关连接，字节也不进缓存（停滞判定看不到），
        // 对端可用「大块垃圾 + 定期活动」零成本占住连接名额
        await ns.WriteAsync("NOT_A_HTTP_REQUEST_GARBAGE"u8.ToArray());
        await ns.FlushAsync();

        var buf = new Byte[4096];
        var n = await ns.ReadAsync(buf.AsMemory()).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.StartsWith("HTTP/1.1 400", buf.AsSpan(0, n).ToStr());
    }

    [Fact(DisplayName = "HTTP服务端_单包头部超限且无空行_回400")]
    public async Task OversizeHead_ReturnsBadRequest()
    {
        // 头部上限调到 32 字节：单包即超限（默认 64KB 需要十余万字节才能触发同一分支）
        using var server = new SmallHeadHttpServer { Port = 0, Log = XTrace.Log };
        server.Start();

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, server.Port);
        using var ns = client.GetStream();

        // 像 HTTP 开头但没有空行、单包已超过上限：不可能再补齐成合法请求头，必须回 400 并关闭。
        // 旧实现直接丢弃这些字节，连接被静默占住
        await ns.WriteAsync("GET /big HTTP/1.1\r\nHost: 127.0.0.1\r\nX: 1\r\n"u8.ToArray());
        await ns.FlushAsync();

        var buf = new Byte[4096];
        var n = await ns.ReadAsync(buf.AsMemory()).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.StartsWith("HTTP/1.1 400", buf.AsSpan(0, n).ToStr());
    }

    class SmallHeadHttpServer : HttpServer
    {
        public Int32 HeadLimit { get; set; } = 32;
        public override INetHandler? CreateHandler(INetSession session) => new HttpSession { MaxHeadLength = HeadLimit };
    }

    [Fact(DisplayName = "HTTP服务端_请求行在首个空格前分片_不丢字节")]
    public async Task SplitBeforeFirstSpace_NotLost()
    {
        _server.MapGet("/split", () => "OK");

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        // 首个分片不足10字节且未出现空格，还判断不出是否为 HTTP → 缓存等待后续分片。
        // 旧实现丢弃该分片，后续补齐的合法请求与残缺前缀之间失步，请求变成无效请求
        await ns.WriteAsync("GET"u8.ToArray());
        await ns.FlushAsync();
        await Task.Delay(100);
        await ns.WriteAsync(" /split HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n"u8.ToArray());
        await ns.FlushAsync();

        var resp = await ReadUntilAsync(ns, s => s.Contains("\r\n\r\n"));
        Assert.StartsWith("HTTP/1.1 200", resp);
    }

    [Fact(DisplayName = "HTTP服务端_失败候选路由参数_不覆盖同名查询参数")]
    public async Task FailedRouteCandidate_DoesNotShadowQueryParameter()
    {
        // 先注册的候选会写入 {id}=5 再因字面段 detail/info 不匹配而失败；命中路由只带 {ver}。
        // 失败候选残留的 id 在路由参数（最后合并）阶段会覆盖 ?id=999
        _server.MapGet<IHttpContext, String>("/api/{id}/detail", _ => "detail");
        _server.MapGet<IHttpContext, String>("/api/{ver}/info", ctx => ctx.Parameters["id"]?.ToString() ?? "none");

        using var client = new HttpClient { BaseAddress = _baseUri };
        var txt = await client.GetStringAsync("/api/5/info?id=999");
        Assert.Equal("999", txt);
    }

    [Fact(DisplayName = "HTTP服务端_重复Content-Length_回400")]
    public async Task DuplicateContentLength_Rejected()    {
        // 同名头部就地覆盖只会剩最后一个值，与前置代理（通常取第一个）理解不一致，构成 CL.CL 走私面；
        // 出现两个 Content-Length（不论大小写、值是否相同）即按协议错误回 400
        _server.MapPost<IHttpContext, String>("/dup", ctx => ctx.Request.Body?.ToStr() ?? "");

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        var head = "POST /dup HTTP/1.1\r\nHost: 127.0.0.1\r\nContent-Length: 5\r\ncontent-length: 5\r\n\r\nhello";
        await ns.WriteAsync(head.GetBytes());
        await ns.FlushAsync();

        var buf = new Byte[4096];
        var n = await ns.ReadAsync(buf.AsMemory()).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.StartsWith("HTTP/1.1 400", buf.AsSpan(0, n).ToStr());
    }

    [Fact(DisplayName = "HTTP服务端_字段名与冒号之间含制表符_回400")]
    public async Task HeaderTabBeforeColon_Rejected()
    {
        // RFC 7230 §3.2.4：字段名与冒号之间不得出现空白，服务端必须拒绝。
        // 不拒绝则与前置代理产生长度理解分歧——代理按 OWS（含制表符）剥去空白后认作 Content-Length，本端认不出，
        // 声明的那段实体在本端被当成后续请求字节（请求走私面）
        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        await ns.WriteAsync("POST / HTTP/1.1\r\nHost: 127.0.0.1\r\nContent-Length\t: 5\r\n\r\nHELLO".GetBytes());
        await ns.FlushAsync();

        var resp = await ReadUntilAsync(ns, s => s.Contains("\r\n\r\n"));
        Assert.StartsWith("HTTP/1.1 400", resp);
    }

    [Fact(DisplayName = "HTTP服务端_头部续行折叠_回400")]
    public async Task HeaderObsFold_Rejected()
    {
        // RFC 7230 §3.2.4：以空白开头的续行（obs-fold）禁止出现，服务端必须拒绝或替换为空格。
        // 旧实现 Trim 前导空格后把该行当独立头部解析——本端据此等待 5 字节实体，而前置代理视其为上一头部的续行（不产生长度头），
        // 后续字节在本端算实体、在代理算请求，长度理解分歧构成请求走私面
        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        await ns.WriteAsync("GET / HTTP/1.1\r\nHost: 127.0.0.1\r\n Content-Length: 5\r\n\r\n".GetBytes());
        await ns.FlushAsync();

        var resp = await ReadUntilAsync(ns, s => s.Contains("\r\n\r\n"));
        Assert.StartsWith("HTTP/1.1 400", resp);
    }

    [Fact(DisplayName = "HTTP服务端_Expect非100continue_忽略而非回417")]
    public async Task ExpectOtherThan100Continue_Ignored()
    {
        // 本端只处理 100-continue 这一个期望，其它 Expect 一律忽略（RFC 9110 §10.1.1 的 417 是 MAY 不是 MUST）。
        // 现实中没有客户端发送其它期望；回 417 只会把本可成功的请求变成失败
        _server.MapPost<IHttpContext, String>("/exp", ctx => "GOT:" + (ctx.Request.Body?.ToStr() ?? ""));

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        // 未知 Expect 不得阻断请求：实体照常接收，正常回 200
        await ns.WriteAsync("POST /exp HTTP/1.1\r\nHost: 127.0.0.1\r\nExpect: some-expectation\r\nContent-Length: 5\r\n\r\nHELLO".GetBytes());
        await ns.FlushAsync();

        var resp = await ReadUntilAsync(ns, s => s.Contains("GOT:HELLO"));
        Assert.StartsWith("HTTP/1.1 200", resp);
    }

    [Fact(DisplayName = "HTTP服务端_流水线两请求同轮到达_两个请求都得到响应")]
    public async Task PipelinedRequests_BothAnswered()
    {
        _server.MapPost<IHttpContext, String>("/pipe1", ctx => "P1:" + (ctx.Request.Body?.ToStr() ?? ""));
        _server.MapGet("/pipe2", () => "P2OK");

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        // HTTP/1.1 允许流水线（一次发出多个请求）：第一个请求体之后的字节属于第二个请求，不得丢弃。
        // 旧实现直接丢掉这部分字节，第二个请求永远等不到响应，客户端只能等到连接超时
        await ns.WriteAsync(("POST /pipe1 HTTP/1.1\r\nHost: 127.0.0.1\r\nContent-Length: 4\r\n\r\nbody" +
                             "GET /pipe2 HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n").GetBytes());
        await ns.FlushAsync();

        var resp = await ReadUntilAsync(ns, s => s.Contains("P1:body") && s.Contains("P2OK"));
        Assert.Contains("P1:body", resp);
        Assert.Contains("P2OK", resp);
    }

    [Fact(DisplayName = "HTTP服务端_流水线两条GET都无长度_两个请求都得到响应")]
    public async Task PipelinedGets_WithoutContentLength_BothAnswered()
    {
        _server.MapGet("/pa", () => "AAA");
        _server.MapGet("/pb", () => "BBB");

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        // 两条普通 GET（无 Content-Length）一次发出：GET 按 RFC 9112 §6.3 没有实体，
        // 头部之后的字节属于第二个请求。按实体吞掉时第二个请求被静默丢弃——既不回响应也不关连接，
        // 客户端只能等自身超时
        await ns.WriteAsync(("GET /pa HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n" +
                             "GET /pb HTTP/1.1\r\nHost: 127.0.0.1\r\nConnection: close\r\n\r\n").GetBytes());
        await ns.FlushAsync();

        var resp = await ReadUntilAsync(ns, s => s.Contains("AAA") && s.Contains("BBB"));

        Assert.Contains("AAA", resp);
        Assert.Contains("BBB", resp);

        // 响应顺序与请求顺序一致（HTTP/1.1 流水线要求）
        Assert.True(resp.IndexOf("AAA", StringComparison.Ordinal) < resp.IndexOf("BBB", StringComparison.Ordinal), "响应顺序必须与请求顺序一致");
        Assert.Equal(2, resp.Split("HTTP/1.1 200").Length - 1);
    }

    [Fact(DisplayName = "HTTP服务端_HTTP1.0的POST无长度_实体仍按连接内容读取")]
    public async Task Http10Post_WithoutContentLength_BodyStillRead()
    {
        // 老式客户端以连接关闭定界实体、不发 Content-Length，兼容语义不能随流水线修正一并去掉
        _server.MapPost<IHttpContext, String>("/legacy", ctx => "GOT:" + (ctx.Request.Body?.ToStr() ?? ""));

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        await ns.WriteAsync("POST /legacy HTTP/1.0\r\nHost: 127.0.0.1\r\n\r\nhello".GetBytes());
        await ns.FlushAsync();

        var resp = await ReadUntilAsync(ns, s => s.Contains("GOT:"));
        Assert.Contains("GOT:hello", resp);
    }

    [Fact(DisplayName = "HTTP服务端_实体跨轮收满后紧跟完整请求_后者也得到响应")]
    public async Task PipelinedAfterBody_SecondAnswered()
    {
        _server.MapPost<IHttpContext, String>("/pipeb1", ctx => "B1:" + (ctx.Request.Body?.ToStr() ?? ""));
        _server.MapGet("/pipeb2", () => "B2OK");

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        // 第一轮：请求头 + 一半主体，服务端进入实体缓存等待后续分片
        await ns.WriteAsync("POST /pipeb1 HTTP/1.1\r\nHost: 127.0.0.1\r\nContent-Length: 4\r\n\r\nda".GetBytes());
        await ns.FlushAsync();
        await Task.Delay(100);

        // 第二轮：剩余主体与完整的第二个请求同轮到达
        await ns.WriteAsync("taGET /pipeb2 HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n".GetBytes());
        await ns.FlushAsync();

        var resp = await ReadUntilAsync(ns, s => s.Contains("B1:data") && s.Contains("B2OK"));
        Assert.Contains("B1:data", resp);
        Assert.Contains("B2OK", resp);
    }

    [Fact(DisplayName = "HTTP服务端_Expect100continue_先回临时响应再收实体")]
    public async Task Expect100Continue_InterimResponse()
    {
        _server.MapPost<IHttpContext, String>("/cont", ctx => "GOT:" + (ctx.Request.Body?.ToStr() ?? ""));

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        // 客户端先只发请求头，等 100 Continue 才发实体（curl、.NET ExpectContinue 的默认行为）
        await ns.WriteAsync("POST /cont HTTP/1.1\r\nHost: 127.0.0.1\r\nContent-Length: 4\r\nExpect: 100-continue\r\n\r\n".GetBytes());
        await ns.FlushAsync();

        // 不应答临时响应时，客户端会白等到超时才发实体；1xx 不得携带实体与 Content-Length
        var interim = await ReadUntilAsync(ns, s => s.Contains("\r\n\r\n"));
        Assert.StartsWith("HTTP/1.1 100", interim);
        Assert.Contains("Continue", interim);

        // 收到临时响应后再发实体，最终响应正常返回
        await ns.WriteAsync("data".GetBytes());
        await ns.FlushAsync();

        var resp = await ReadUntilAsync(ns, s => s.Contains("GOT:data"));
        Assert.StartsWith("HTTP/1.1 200", resp);
    }

    [Fact]
    public async Task MapDelegate()
    {
        _server.Map("/", () => "<h1>Hello NewLife!</h1></br> " + DateTime.Now.ToFullString() + "</br><img src=\"logos/leaf.png\" />");
        _server.Map("/async", async () =>
        {
            await Task.Delay(100);
            return "Now is " + DateTime.Now.ToFullString();
        });

        var client = new HttpClient { BaseAddress = _baseUri };
        var html = await client.GetStringAsync("/");

        Assert.NotEmpty(html);
        Assert.StartsWith("<h1>Hello NewLife!</h1></br>", html);
        Assert.Contains("logos/leaf.png", html);

        html = await client.GetStringAsync("/async");

        Assert.NotEmpty(html);
        Assert.StartsWith("Now is ", html);
    }

    [Fact]
    public async Task MapApi()
    {
        _server.Map("/user", (String act, Int32 uid) => new { code = 0, data = $"User.{act}({uid}) success!" });

        var client = new HttpClient { BaseAddress = _baseUri };
        var rs = await client.GetAsync<String>("/user", new { act = "edit", uid = 1234 });

        Assert.Equal("User.edit(1234) success!", rs);
    }

    [Fact(DisplayName = "请求头跨轮分片：凑齐后正常响应")]
    public async Task SplitRequestHead()
    {
        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        // 请求头跨两次发送，第一次在 Host 头部中间截断（模拟头部跨接收轮分片）
        var head = "GET / HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n".GetBytes();
        await ns.WriteAsync(head.AsMemory(0, 18));
        await ns.FlushAsync();
        await Task.Delay(50);
        await ns.WriteAsync(head.AsMemory(18));
        await ns.FlushAsync();

        var buf = new Byte[2048];
        var n = await ns.ReadAsync(buf.AsMemory()).AsTask().WaitAsync(TimeSpan.FromSeconds(5));

        var resp = buf.AsSpan(0, n).ToStr();
        Assert.StartsWith("HTTP/1.1", resp);
    }

    [Fact(DisplayName = "KeepAlive 第二请求头分片中间轮：不得重放已完成的上一请求")]
    public async Task SplitRequestHead_SecondRequest_NoReplay()
    {
        _server.Map("/first", () => "FIRST");
        _server.Map("/second", () => "SECOND");

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        // 第一请求：完整 GET（无主体，KeepAlive）
        var head1 = "GET /first HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n".GetBytes();
        await ns.WriteAsync(head1);
        await ns.FlushAsync();

        var buf = new Byte[4096];
        var n1 = await ns.ReadAsync(buf.AsMemory()).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Contains("FIRST", buf.AsSpan(0, n1).ToStr());

        // 第二请求：请求头跨两片发送；第一片不完整（本轮不应产生任何响应）
        var head2 = "GET /second HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n".GetBytes();
        await ns.WriteAsync(head2.AsMemory(0, 12));
        await ns.FlushAsync();

        // 中间轮探测：旧行为会在这里重放 /first 响应；修复后应无数据（等待后续分片）
        var probe = new Byte[4096];
        var readTask = ns.ReadAsync(probe.AsMemory()).AsTask();
        var first = await Task.WhenAny(readTask, Task.Delay(500));
        Assert.NotSame(readTask, first);

        // 补齐请求头：挂起中的读取将收到 /second 的响应（NetworkStream 单一读取操作，探测读继续复用）
        await ns.WriteAsync(head2.AsMemory(12));
        await ns.FlushAsync();

        var n2 = await readTask.WaitAsync(TimeSpan.FromSeconds(5));
        var resp2 = probe.AsSpan(0, n2).ToStr();
        Assert.Contains("SECOND", resp2);
    }

    [Fact]
    public async Task MapStaticFiles()
    {
        XTrace.WriteLine("root: {0}", "Http/".GetFullPath());
        _server.MapStaticFiles("/logos", "Http/");

        var client = new HttpClient { BaseAddress = _baseUri };
        var rs = await client.GetStreamAsync("/logos/leaf.png");

        Assert.NotNull(rs);
        Assert.Equal(93917, rs.ReadBytes(-1).Length);
    }

    [Fact(DisplayName = "静态文件_大文件_零拷贝推送_内容逐字节一致")]
    public async Task StaticFile_LargeFile_ContentMatches()
    {
        var dir = Path.Combine(Path.GetTempPath(), "nl_static_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        // 2MB：远超 64KB 读块，确认文件内容不再经应用层读块搬运
        const Int32 size = 2 * 1024 * 1024;
        var payload = new Byte[size];
        Random.Shared.NextBytes(payload);
        await File.WriteAllBytesAsync(Path.Combine(dir, "big.bin"), payload);

        try
        {
            _server.MapStaticFiles("/static", dir);

            using var client = new HttpClient { BaseAddress = _baseUri };
            var bytes = await client.GetByteArrayAsync("/static/big.bin");

            Assert.Equal(size, bytes.Length);
            Assert.Equal(payload, bytes);
        }
        finally
        {
            // 零拷贝发送在响应体读完后可能仍在收尾，文件句柄被短暂占用，Windows 下删除会 IOException；限次重试
            for (var i = 0; i < 10; i++)
            {
                try
                {
                    Directory.Delete(dir, true);
                    break;
                }
                catch (IOException) when (i < 9)
                {
                    await Task.Delay(100);
                }
            }
        }
    }

    [Fact(DisplayName = "静态文件_HEAD请求_只返回头部不发实体")]
    public async Task HeadRequest_StaticFile_NoEntity()
    {
        _server.MapStaticFiles("/logos", "Http/");

        using var client = new HttpClient { BaseAddress = _baseUri };
        using var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Head, "/logos/leaf.png");
        using var rs = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, rs.StatusCode);

        // 需声明实体长度（与 GET 一致），但不得发送实体。
        // 静态文件走 BodyStream：修复前 HEAD 分支只看内存体，会把整个实体一起发出去
        Assert.Equal(93917, rs.Content.Headers.ContentLength);

        var body = await rs.Content.ReadAsByteArrayAsync();
        Assert.Empty(body);

        // 同连接复用：协议对齐。若 HEAD 发了实体，后续 GET 会读到残留字节或长度不符
        var txt = await client.GetStreamAsync("/logos/leaf.png");
        Assert.Equal(93917, txt.ReadBytes(-1).Length);
    }

    [Fact(DisplayName = "流式响应_可寻址流_按Content-Length完整到达")]
    public async Task StreamResponse_ContentLength()
    {
        var payload = new Byte[256 * 1024];
        Random.Shared.NextBytes(payload);
        _server.Map("/stream", new StreamTestHandler { Payload = payload });

        using var client = new HttpClient { BaseAddress = _baseUri };
        var rs = await client.GetAsync("/stream");
        Assert.Equal(HttpStatusCode.OK, rs.StatusCode);
        Assert.Null(rs.Headers.TransferEncodingChunked);
        Assert.Equal(payload, await rs.Content.ReadAsByteArrayAsync());
    }

    [Fact(DisplayName = "流式响应_不可寻址流_分块传输完整到达")]
    public async Task StreamResponse_Chunked()
    {
        var payload = new Byte[64 * 1024 + 123];
        Random.Shared.NextBytes(payload);
        _server.Map("/chunked", new StreamTestHandler { Payload = payload, NonSeekable = true });

        using var client = new HttpClient { BaseAddress = _baseUri };
        var rs = await client.GetAsync("/chunked");
        Assert.Equal(HttpStatusCode.OK, rs.StatusCode);
        Assert.True(rs.Headers.TransferEncodingChunked);
        Assert.Equal(payload, await rs.Content.ReadAsByteArrayAsync());

        // 同连接复用：分块传输结束后协议对齐，后续请求正常
        var rs2 = await client.GetAsync("/chunked");
        Assert.Equal(payload, await rs2.Content.ReadAsByteArrayAsync());
    }

    [Fact(DisplayName = "流式响应_文件路径已失效_降级读流完整到达")]
    public async Task StreamResponse_FileGone_FallsBackToStream()
    {
        var payload = new Byte[256 * 1024];
        Random.Shared.NextBytes(payload);
        _server.Map("/gone", new DeletedFileHandler { Payload = payload });

        using var client = new HttpClient { BaseAddress = _baseUri };
        var rs = await client.GetAsync("/gone");
        Assert.Equal(HttpStatusCode.OK, rs.StatusCode);
        Assert.Equal(payload, await rs.Content.ReadAsByteArrayAsync());
    }

    [Fact(DisplayName = "流式响应_慢客户端_数据完整送达")]
    public async Task StreamResponse_SlowClient_DataIntact()
    {
        var payload = new Byte[16 * 1024 * 1024];
        Random.Shared.NextBytes(payload);
        _server.Map("/slow", new StreamTestHandler { Payload = payload });

        // 客户端停读期间内核发送缓冲填满，服务端的流式发送会在“单块写”上挂起；
        // 网络层按块计时（SessionBase.Timeout 默认 3 s），挂起超过预算即按发送超时断开连接（设计行为）。
        // 本用例关注“背压不截断数据”，停顿窗口必须远小于发送预算；满载下 1 s 停顿可能被放大数倍，
        // 故仅为本用例服务端放宽预算，默认预算下的超时断开行为由网络层用例覆盖
        _server.NewSession += (s, e) =>
        {
            if (e.Session?.Session is TcpSession tcp) tcp.Timeout = 30_000;
        };

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = client.GetStream();

        await ns.WriteAsync("GET /slow HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n".GetBytes());
        await ns.FlushAsync();

        // 慢客户端：请求发出后间歇停读。直发模型下服务端每块写完（或挂起）才读下一块，
        // 内存恒为一块读缓冲——内核发送缓冲充当前水位的背压，不会把 16M 响应体撑进内存
        for (var i = 0; i < 10; i++)
        {
            await Task.Delay(100);
        }

        // 客户端开始读取：数据必须完整送达（背压只约束排队，不截断数据）
        var all = new MemoryStream();
        var buf = new Byte[64 * 1024];
        var headEnd = -1;
        while (headEnd < 0)
        {
            var n = await ns.ReadAsync(buf.AsMemory()).AsTask().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(n > 0, "连接提前结束");
            all.Write(buf, 0, n);

            headEnd = IndexOfHeadEnd(all.GetBuffer(), (Int32)all.Length);
        }

        var headText = all.GetBuffer().AsSpan(0, headEnd).ToStr();
        Assert.StartsWith("HTTP/1.1 200", headText);

        var cl = headText.Split("\r\n").First(e => e.StartsWith("Content-Length", StringComparison.OrdinalIgnoreCase)).Split(':')[1].Trim().ToInt();
        Assert.Equal(payload.Length, cl);

        var need = headEnd + cl;
        while (all.Length < need)
        {
            var n = await ns.ReadAsync(buf.AsMemory()).AsTask().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(n > 0, "连接提前结束");
            all.Write(buf, 0, n);
        }

        Assert.True(payload.AsSpan().SequenceEqual(all.GetBuffer().AsSpan(headEnd, cl)), "响应体不完整");
    }

    /// <summary>查找响应体起始位置（头与体之间的空行之后）；未找到返回 -1</summary>
    /// <param name="buf">缓冲</param>
    /// <param name="length">有效长度</param>
    private static Int32 IndexOfHeadEnd(Byte[] buf, Int32 length)
    {
        for (var i = 3; i < length; i++)
        {
            if (buf[i - 3] == 13 && buf[i - 2] == 10 && buf[i - 1] == 13 && buf[i] == 10) return i + 1;
        }

        return -1;
    }

    /// <summary>流式响应测试处理器。NonSeekable 时长度未知，触发分块传输</summary>
    class StreamTestHandler : IHttpHandler
    {
        public Byte[] Payload { get; set; } = [];

        public Boolean NonSeekable { get; set; }

        public Task ProcessRequestAsync(IHttpContext context)
        {
            Stream stream = new MemoryStream(Payload);
            if (NonSeekable) stream = new NonSeekableStream(stream);

            context.Response.ContentType = "application/octet-stream";
            context.Response.BodyStream = stream;

            return TaskEx.CompletedTask;
        }
    }

    /// <summary>不可寻址流包装：长度未知，触发分块传输</summary>
    class NonSeekableStream(Stream inner) : Stream
    {
        public override Boolean CanRead => inner.CanRead;
        public override Boolean CanSeek => false;
        public override Boolean CanWrite => false;
        public override Int64 Length => throw new NotSupportedException();
        public override Int64 Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count) => inner.Read(buffer, offset, count);
        public override Int64 Seek(Int64 offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(Int64 value) => throw new NotSupportedException();
        public override void Write(Byte[] buffer, Int32 offset, Int32 count) => throw new NotSupportedException();
    }

    /// <summary>文件打开后即删除目录项的流式响应处理器</summary>
    /// <remarks>流本身仍可读，但零拷贝要按路径重开文件——此时路径已不存在，必须降级回读原流</remarks>
    class DeletedFileHandler : IHttpHandler
    {
        public Byte[] Payload { get; set; } = [];

        public Task ProcessRequestAsync(IHttpContext context)
        {
            var path = Path.Combine(Path.GetTempPath(), "nl_delfile_" + Guid.NewGuid().ToString("N"));
            File.WriteAllBytes(path, Payload);

            // 带 FileShare.Delete 打开后立即删除目录项：句柄仍可读，路径重开必然失败
            var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            File.Delete(path);

            context.Response.ContentType = "application/octet-stream";
            context.Response.BodyStream = fs;

            return TaskEx.CompletedTask;
        }
    }

    [Fact]
    public async Task MapMyHttpHandler()
    {
        _server.Map("/my", new MyHttpHandler());

        var client = new HttpClient { BaseAddress = _baseUri };
        var html = await client.GetStringAsync("/my?name=stone");

        Assert.Equal("<h2>你好，<span color=\"red\">stone</span></h2>", html);
    }

    class MyHttpHandler : IHttpHandler
    {
        public Task ProcessRequestAsync(IHttpContext context)
        {
            var name = context.Parameters["name"];
            var html = $"<h2>你好，<span color=\"red\">{name}</span></h2>";
            context.Response.SetResult(html);

            return TaskEx.CompletedTask;
        }
    }

    [Fact]
    public async Task BigPost()
    {
        _server.Map("/my2", new MyHttpHandler2());

        var client = new HttpClient { BaseAddress = _baseUri };

        var buf1 = new Byte[8 * 1024];
        var buf2 = new Byte[8 * 1024];

#if NET462
        for (var i = 0; i < buf1.Length; i++)
        {
            buf1[i] = (Byte)'0';
            buf2[i] = (Byte)'0';
        }
#else
        Array.Fill(buf1, (Byte)'0');
        Array.Fill(buf2, (Byte)'0');
#endif

        buf1[0] = (Byte)'1';
        buf2[0] = (Byte)'2';

        var ms = new MemoryStream();
        ms.Write(buf1);
        ms.Write(buf2);

        var rs = await client.PostAsync("/my2?name=newlife", new ByteArrayContent(ms.ToArray()));

        Assert.Equal(HttpStatusCode.OK, rs.StatusCode);
    }

    class MyHttpHandler2 : IHttpHandler
    {
        public Task ProcessRequestAsync(IHttpContext context)
        {
            Assert.Equal(8 * 1024 * 2, context.Request.ContentLength);

            // 数据部分，链式
            var pk = context.Request.Body;
            Assert.Equal(8 * 1024 * 2, pk.Length);

            var name = context.Parameters["name"];
            var html = $"<h2>你好，<span color=\"red\">{name}</span></h2>";
            context.Response.SetResult(html);

            return TaskEx.CompletedTask;
        }
    }

    [Fact]
    public async Task MapWebSocket()
    {
        _server.Map("/ws", new WebSocketHandler());

        var content = "Hello NewLife".GetBytes();

        var client = new ClientWebSocket();
        await client.ConnectAsync(new Uri($"ws://127.0.0.1:{_server.Port}/ws"), default);
        await client.SendAsync(new ArraySegment<Byte>(content), System.Net.WebSockets.WebSocketMessageType.Text, true, default);

        var buf = new Byte[1024];
        var rs = await client.ReceiveAsync(new ArraySegment<Byte>(buf), default);
        Assert.EndsWith("说，Hello NewLife", new ArrayPacket(buf, 0, rs.Count).ToStr());

        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "通信完成", default);
        XTrace.WriteLine("Close [{0}] {1}", client.CloseStatus, client.CloseStatusDescription);

        Assert.Equal(WebSocketCloseStatus.NormalClosure, client.CloseStatus);
        Assert.Equal("通信完成", client.CloseStatusDescription);
    }

    [Fact]
    public void ParseFormData()
    {
        var data = @"------WebKitFormBoundary3ZXeqQWNjAzojVR7
Content-Disposition: form-data; name=""name""

大石头
------WebKitFormBoundary3ZXeqQWNjAzojVR7
Content-Disposition: form-data; name=""password""

565656
------WebKitFormBoundary3ZXeqQWNjAzojVR7
Content-Disposition: form-data; name=""avatar""; filename=""logo.png""
Content-Type: image/jpeg

";
        var png = File.ReadAllBytes("Http/leaf.png".GetFullPath());
        IPacket pk = (ArrayPacket)data.Crlf().GetBytes();
        var pngPk = (ArrayPacket)png;
        pngPk.Next = (ArrayPacket)"\r\n------WebKitFormBoundary3ZXeqQWNjAzojVR7--\r\n".GetBytes();
        pk.Next = pngPk;

        var req = new HttpRequest
        {
            ContentType = "multipart/form-data;boundary=----WebKitFormBoundary3ZXeqQWNjAzojVR7",
            Body = pk
        };

        var dic = req.ParseFormData();
        Assert.NotNull(dic);

        var rs = dic.TryGetValue("name", out var name);
        Assert.True(rs);
        Assert.NotEmpty((String)name);
        Assert.Equal("大石头", name);

        rs = dic.TryGetValue("password", out var password);
        Assert.True(rs);
        Assert.NotEmpty((String)password);
        Assert.Equal("565656", password);

        rs = dic.TryGetValue("avatar", out var avatar);
        Assert.True(rs);

        var av = avatar as FormFile;
        Assert.NotNull(av);
        Assert.Equal("logo.png", av.FileName);
        Assert.Equal("image/jpeg", av.ContentType);

        var png2 = av.OpenReadStream().ReadBytes(-1);
        Assert.Equal(png.Length, png2.Length);
        Assert.True(png.SequenceEqual(png2));
        Assert.Equal(png, av.Data!.ToArray());
    }

    [Fact(DisplayName = "ParseFormData 链式跨段体：现代链式包聚合扫描")]
    public void ParseFormData_ChainedBody()
    {
        var head = @"------WebKitFormBoundary3ZXeqQWNjAzojVR7
Content-Disposition: form-data; name=""name""

大石头
------WebKitFormBoundary3ZXeqQWNjAzojVR7
Content-Disposition: form-data; name=""avatar""; filename=""logo.bin""
Content-Type: application/octet-stream

";
        var fileData = new Byte[512];
        for (var i = 0; i < fileData.Length; i++) fileData[i] = (Byte)i;
        var tail = "\r\n------WebKitFormBoundary3ZXeqQWNjAzojVR7--\r\n";

        // 头部、文件数据、尾部各成一段；首段从边界标记中切开，文件数据再切两段（现代链式包的首段 GetSpan 只覆盖首个节点）
        var raw = head.Crlf().GetBytes();
        IPacket pk = new ArrayPacket(raw[..30]);
        pk.Append(new ArrayPacket(raw[30..]));
        pk.Append(new ArrayPacket(fileData[..200]));
        pk.Append(new ArrayPacket(fileData[200..]));
        pk.Append(new ArrayPacket(tail.GetBytes()));

        var req = new HttpRequest
        {
            ContentType = "multipart/form-data;boundary=----WebKitFormBoundary3ZXeqQWNjAzojVR7",
            Body = pk
        };

        var dic = req.ParseFormData();
        Assert.Equal("大石头", dic["name"]);

        var av = dic["avatar"] as FormFile;
        Assert.NotNull(av);
        Assert.Equal("logo.bin", av.FileName);
        Assert.Equal(512L, av.Length);
        Assert.Equal(fileData, av.OpenReadStream()!.ReadBytes(-1));
    }

    [Fact(DisplayName = "ParseFormData 引号边界与参数、前导内容：正常解析")]
    public void ParseFormData_QuotedBoundary_WithParamsAndPreamble()
    {
        var bd = "------X";
        var text = $"preamble\r\n{bd}\r\nContent-Disposition: form-data; name=\"name\"\r\n\r\n大石头\r\n{bd}\r\nContent-Disposition: form-data; name=\"avatar\"; filename=\"a.bin\"\r\nContent-Type: application/octet-stream\r\n\r\n";
        var fileData = new Byte[32];
        for (var i = 0; i < fileData.Length; i++) fileData[i] = (Byte)(i + 1);
        var tail = $"\r\n{bd}--\r\n";

        // 主体切到 3 个节点：文本区 + 文件数据 + 结束标记
        IPacket pk = new ArrayPacket(text.GetBytes());
        pk.Append(new ArrayPacket(fileData));
        pk.Append(new ArrayPacket(tail.GetBytes()));

        var req = new HttpRequest
        {
            // 边界带引号且后随其它参数；首个分隔标记之前有前导内容（MIME preamble）
            ContentType = "multipart/form-data; boundary=\"----X\"; charset=utf-8",
            Body = pk
        };

        var dic = req.ParseFormData();
        Assert.Equal("大石头", dic["name"]);

        var av = dic["avatar"] as FormFile;
        Assert.NotNull(av);
        Assert.Equal("a.bin", av.FileName);
        Assert.Equal(32L, av.Length);
        Assert.Equal(fileData, av.OpenReadStream()!.ReadBytes(-1));
    }

    #region 新增覆盖测试
    [Fact]
    public async Task RouteOverridePriority()
    {
        _server.Map("/override", () => "first");
        _server.Map("/override", () => "second");

        var client = new HttpClient { BaseAddress = _baseUri };
        var txt = await client.GetStringAsync("/override");
        Assert.Equal("second", txt);
    }

    [Fact]
    public async Task WildcardVsExactMatch()
    {
        _server.Map("/test/*", () => "wild");
        _server.Map("/test/path", () => "exact");

        var client = new HttpClient { BaseAddress = _baseUri };
        var txt = await client.GetStringAsync("/test/path");
        Assert.Equal("exact", txt);

        txt = await client.GetStringAsync("/test/other");
        Assert.Equal("wild", txt);
    }

    [Fact]
    public async Task ParameterBindingMultiOverloads()
    {
        // 模型绑定
        _server.Map<Person, String>("/person", p => $"{p.Name}:{p.Age}");
        // 2 参数
        _server.Map<String, Int32, String>("/two", (a, b) => $"{a}:{b}");
        // 3 参数
        _server.Map<String, Int32, String, String>("/three", (a, b, c) => $"{a}:{b}:{c}");
        // 4 参数
        _server.Map<String, Int32, String, Int32, String>("/four", (a, b, c, d) => $"{a}:{b}:{c}:{d}");

        var client = new HttpClient { BaseAddress = _baseUri };
        var v1 = await client.GetStringAsync("/person?Name=Al&Age=5");
        Assert.Equal("Al:5", v1);

        var v2 = await client.GetStringAsync("/two?a=hi&b=7");
        Assert.Equal("hi:7", v2);

        var v3 = await client.GetStringAsync("/three?a=x&b=8&c=ok");
        Assert.Equal("x:8:ok", v3);

        var v4 = await client.GetStringAsync("/four?a=A&b=1&c=B&d=2");
        Assert.Equal("A:1:B:2", v4);
    }

    record Person(String Name, Int32 Age);

    [Fact]
    public async Task NotFoundRoute()
    {
        var client = new HttpClient { BaseAddress = _baseUri };
        var rsp = await client.GetAsync("/notfound/abc");
        Assert.Equal(HttpStatusCode.NotFound, rsp.StatusCode);
    }

    [Fact]
    public async Task PathSafetyForbidden()
    {
        var client = new HttpClient { BaseAddress = _baseUri };
        var rsp = await client.GetAsync("/../etc/passwd");
        //Assert.Equal(HttpStatusCode.Forbidden, rsp.StatusCode);
        // 在HttpClient中就被转为请求 /etc/passwd 了
        Assert.Equal(HttpStatusCode.NotFound, rsp.StatusCode);
    }

    [Fact]
    public async Task ServerHeaderInjectionAndPreserve()
    {
        _server.Map("/header", () => "ok");
        _server.Map("/header2", new CustomServerHeaderHandler());

        var client = new HttpClient { BaseAddress = _baseUri };

        var rsp1 = await client.GetAsync("/header");
        Assert.True(rsp1.Headers.TryGetValues("Server", out var sv1));
        Assert.Contains("NewLife-HttpServer", sv1.First());

        var rsp2 = await client.GetAsync("/header2");
        Assert.True(rsp2.Headers.TryGetValues("Server", out var sv2));
        Assert.Equal("CustomServer", sv2.First());
    }

    class CustomServerHeaderHandler : IHttpHandler
    {
        public Task ProcessRequestAsync(IHttpContext context)
        {
            context.Response.Headers["Server"] = "CustomServer";
            context.Response.SetResult("ok");

            return TaskEx.CompletedTask;
        }
    }

    [Fact]
    public async Task WildcardCacheBehavior()
    {
        _server.Map("/wild/*", new MyHttpHandler());

        var client = new HttpClient { BaseAddress = _baseUri };

        // 清理缓存（内部 _maps 私有）。
        var mapsField = typeof(HttpServer).GetField("_pathCache", BindingFlags.Instance | BindingFlags.NonPublic);
        var maps = mapsField.GetValue(_server) as System.Collections.IDictionary;
        maps.Clear();

        // 短路径（应缓存）  => /wild/x  Split('/') => ["", "wild", "x"] 长度=3 <=3
        var txt = await client.GetStringAsync("/wild/x?name=abc");
        Assert.Contains("abc", txt);
        Assert.True(maps.Contains("/wild/x"));

        // 长路径（不缓存） => /wild/a/b/c/d  分段长度>3
        txt = await client.GetStringAsync("/wild/a/b/c/d?name=long");
        Assert.Contains("long", txt);
        Assert.False(maps.Contains("/wild/a/b/c/d"));
    }

    [Fact]
    public async Task KeepAliveFalseAddsCloseHeader()
    {
        // 使用原始 TCP 发送 HTTP/1.0 请求，默认非 KeepAlive
        var req = "GET /keepalive HTTP/1.0\r\nHost: 127.0.0.1\r\n\r\n";

        _server.Map("/keepalive", () => "alive");

        using var tcp = new TcpClient();
        await tcp.ConnectAsync(IPAddress.Loopback, _server.Port);
        using var ns = tcp.GetStream();
        var bytes = Encoding.ASCII.GetBytes(req);
        await ns.WriteAsync(bytes, 0, bytes.Length);
        await ns.FlushAsync();

        using var ms = new MemoryStream();
        var buf = new Byte[4096];
        // 固定 50ms 微等待在并行满载下可能早于响应到达，改为轮询读取到响应体出现为止（上限 5 秒）
        var resp = String.Empty;
        for (var i = 0; i < 500; i++)
        {
            while (ns.DataAvailable)
            {
                var n = await ns.ReadAsync(buf, 0, buf.Length);
                if (n <= 0) break;

                ms.Write(buf, 0, n);
            }

            resp = Encoding.ASCII.GetString(ms.ToArray());
            if (resp.Contains("alive", StringComparison.OrdinalIgnoreCase)) break;

            await Task.Delay(10);
        }
        Assert.Contains("Connection: close", resp, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("alive", resp);
    }

    [Fact]
    public async Task MaxRequestLengthExceeded()
    {
        using var server = new SmallLimitHttpServer { Port = 0, Limit = 16, Log = XTrace.Log, SessionLog = XTrace.Log };
        server.Map("/small", () => "ok");
        server.Start();

        var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{server.Port}") };
        var content = new ByteArrayContent(new Byte[32]);
        var rsp = await client.PostAsync("/small", content);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, rsp.StatusCode);
    }

    class SmallLimitHttpServer : HttpServer
    {
        public Int32 Limit { get; set; } = 16;
        public override INetHandler? CreateHandler(INetSession session)
        {
            return new HttpSession { MaxRequestLength = Limit };
        }
    }

    #region Controller 上下文注入测试
    [Fact]
    public async Task Controller_IHttpContext_ParameterInjection()
    {
        _server.MapController<ContextTestController>("/ctx");

        var client = new HttpClient { BaseAddress = _baseUri };
        var txt = await client.GetStringAsync("/ctx/test?msg=hello");
        // 返回 Path + msg
        Assert.Equal("/ctx/test:hello", txt);
    }

    [Fact]
    public async Task Controller_IHttpContext_InterfaceInjection()
    {
        _server.MapController<InterfaceTestController>("/iface");

        var client = new HttpClient { BaseAddress = _baseUri };
        var txt = await client.GetStringAsync("/iface/info");
        // 控制器通过 IHttpController 接口属性拿到 Context
        Assert.Equal("/iface/info", txt);
    }

    [Fact]
    public async Task Controller_IServiceProvider_Injection()
    {
        _server.MapController<ServiceProviderTestController>("/sp");

        var client = new HttpClient { BaseAddress = _baseUri };
        var txt = await client.GetStringAsync("/sp/info?name=test");
        Assert.Equal("test", txt);
    }

    [Fact]
    public async Task Controller_Constructor_DI()
    {
        _server.MapController<DiTestController>("/di");

        var client = new HttpClient { BaseAddress = _baseUri };
        var txt = await client.GetStringAsync("/di/path");
        Assert.Equal("/di/path", txt);
    }

    class ContextTestController
    {
        public String Test(IHttpContext ctx, String msg)
        {
            return $"{ctx.Path}:{msg}";
        }
    }

    class InterfaceTestController : IHttpController
    {
        public IHttpContext? Context { get; set; }

        public String Info() => Context?.Path ?? "";
    }

    class ServiceProviderTestController
    {
        public String Info(IServiceProvider sp, String name)
        {
            var ctx = sp.GetService(typeof(IHttpContext));
            Assert.NotNull(ctx);
            return name;
        }
    }

    /// <summary>通过构造函数 DI 获取 IHttpContext</summary>
    class DiTestController
    {
        private readonly IHttpContext _ctx;
        public DiTestController(IHttpContext ctx) => _ctx = ctx;

        public String Path() => _ctx.Path;
    }

    [Fact(DisplayName = "控制器_私有方法不可经URL调用")]
    public async Task Controller_PrivateMethod_NotInvokable()
    {
        _server.MapController<PrivateMethodController>("/priv");

        var client = new HttpClient { BaseAddress = _baseUri };

        // 公开方法可正常调用
        var txt = await client.GetStringAsync("/priv/PublicOp");
        Assert.Equal("public-ok", txt);

        // 私有方法应返回 404（找不到操作），绝不能被执行
        // （旧实现方法查找带 BindingFlags.NonPublic，控制器私有方法可被任意 URL 调用）
        var res = await client.GetAsync("/priv/SecretOp");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    class PrivateMethodController
    {
        public String PublicOp() => "public-ok";

        private static String SecretOp() => "secret";
    }

    [Fact(DisplayName = "控制器_多段挂载前缀_正确定位操作方法")]
    public async Task Controller_MultiSegmentPath_ResolvesMethod()
    {
        _server.MapController<MultiSegmentController>("/api/user");

        var client = new HttpClient { BaseAddress = _baseUri };

        // 方法名位于挂载前缀之后。旧实现固定取 Split('/')[2]，
        // 会拿到路径中间那段 "user"，报 Cannot find operation [user]
        var txt = await client.GetStringAsync("/api/user/info");
        Assert.Equal("info:/api/user/info", txt);

        // 参数仍通过查询串绑定，不受方法名定位改动影响
        txt = await client.GetStringAsync("/api/user/echo?name=abc");
        Assert.Equal("abc", txt);
    }

    [Fact(DisplayName = "控制器_多段挂载前缀_不按前缀末段误路由")]
    public async Task Controller_MultiSegmentPath_NotMisroutedByPrefixName()
    {
        _server.MapController<MultiSegmentController>("/api/user");

        var client = new HttpClient { BaseAddress = _baseUri };

        // 控制器恰好存在与挂载前缀末段同名的方法 User。
        // 旧实现取 Split('/')[2] 得到 "user"，任意子路径都会被静默路由到该方法
        var res = await client.GetAsync("/api/user/not-a-method");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    class MultiSegmentController
    {
        public String Info(IHttpContext ctx) => $"info:{ctx.Path}";

        public String Echo(String name) => name;

        /// <summary>与挂载前缀末段同名，用于验证方法名定位不按路径段序号猜测</summary>
        public String User() => "wrong";
    }

    [Fact(DisplayName = "控制器_同步方法抛ApiException_状态码与消息不被反射包装")]
    public async Task Controller_SyncApiException_Mapped()
    {
        _server.MapController<ThrowController>("/throw");

        var client = new HttpClient { BaseAddress = _baseUri };

        // 同步方法：反射调用包装过的异常必须还原，否则 404 会变成 500 + 包装消息
        var res = await client.GetAsync("/throw/Sync");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        Assert.Equal("user 123 not found", await res.Content.ReadAsStringAsync());

        // 异步方法：经 awaiter 本来就是原始异常，作为回归保护
        res = await client.GetAsync("/throw/Async");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        Assert.Equal("user 123 not found", await res.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "控制器_同步方法抛HttpException_按状态码返回")]
    public async Task Controller_SyncHttpException_Mapped()
    {
        _server.MapController<ThrowController>("/throw");

        var client = new HttpClient { BaseAddress = _baseUri };

        var res = await client.GetAsync("/throw/Forbidden");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact(DisplayName = "委托_同步抛ApiException_状态码映射")]
    public async Task Delegate_SyncApiException_Mapped()
    {
        _server.MapGet<String>("/boom-del", () => throw new ApiException(404, "delegate not found"));

        var client = new HttpClient { BaseAddress = _baseUri };

        var res = await client.GetAsync("/boom-del");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        Assert.Equal("delegate not found", await res.Content.ReadAsStringAsync());
    }

    class ThrowController
    {
        public String Sync() => throw new ApiException(404, "user 123 not found");

        public async Task<String> Async()
        {
            await Task.Yield();
            throw new ApiException(404, "user 123 not found");
        }

        public String Forbidden() => throw new HttpException(HttpStatusCode.Forbidden, "forbidden");
    }
    #endregion

    #endregion
}