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
        XTrace.WriteLine("root: {0}", "http/".GetFullPath());
        _server.MapStaticFiles("/logos", "http/");

        var client = new HttpClient { BaseAddress = _baseUri };
        var rs = await client.GetStreamAsync("/logos/leaf.png");

        Assert.NotNull(rs);
        Assert.Equal(93917, rs.ReadBytes(-1).Length);
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

    /// <summary>流式响应测试处理器。NonSeekable 时长度未知，触发分块传输</summary>
    class StreamTestHandler : IHttpHandler
    {
        public Byte[] Payload { get; set; } = [];

        public Boolean NonSeekable { get; set; }

        public void ProcessRequest(IHttpContext context)
        {
            Stream stream = new MemoryStream(Payload);
            if (NonSeekable) stream = new NonSeekableStream(stream);

            context.Response.ContentType = "application/octet-stream";
            context.Response.BodyStream = stream;
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
        public void ProcessRequest(IHttpContext context)
        {
            var name = context.Parameters["name"];
            var html = $"<h2>你好，<span color=\"red\">{name}</span></h2>";
            context.Response.SetResult(html);
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
        public void ProcessRequest(IHttpContext context)
        {
            Assert.Equal(8 * 1024 * 2, context.Request.ContentLength);

            // 数据部分，链式
            var pk = context.Request.Body;
            Assert.Equal(8 * 1024 * 2, pk.Length);

            var name = context.Parameters["name"];
            var html = $"<h2>你好，<span color=\"red\">{name}</span></h2>";
            context.Response.SetResult(html);
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
        var png = File.ReadAllBytes("http/leaf.png".GetFullPath());
        IPacket pk = (ArrayPacket)data.GetBytes();
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
        var raw = head.GetBytes();
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
        public void ProcessRequest(IHttpContext context)
        {
            context.Response.Headers["Server"] = "CustomServer";
            context.Response.SetResult("ok");
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
        // 读取一点足够解析头
        await Task.Delay(50); // 微等待服务端响应
        while (ns.DataAvailable)
        {
            var n = await ns.ReadAsync(buf, 0, buf.Length);
            if (n <= 0) break;
            ms.Write(buf, 0, n);
        }
        var resp = Encoding.ASCII.GetString(ms.ToArray());
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
    #endregion

    #endregion
}