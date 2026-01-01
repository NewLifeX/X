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

namespace XUnitTest.Http;

public class HttpServerTests
{
    private static HttpServer _server;
    private static Uri _baseUri;
    static HttpServerTests()
    {
        var server = new HttpServer
        {
            Port = 18080,
            Log = XTrace.Log,
            SessionLog = XTrace.Log
        };
        server.Start();

        _server = server;
        _baseUri = new Uri("http://127.0.0.1:18080");
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
        await client.ConnectAsync(new Uri("ws://127.0.0.1:18080/ws"), default);
        await client.SendAsync(new ArraySegment<Byte>(content), System.Net.WebSockets.WebSocketMessageType.Text, true, default);

        var buf = new Byte[1024];
        var rs = await client.ReceiveAsync(new ArraySegment<Byte>(buf), default);
        Assert.EndsWith("说，Hello NewLife", new Packet(buf, 0, rs.Count).ToStr());

        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "通信完成", default);
        XTrace.WriteLine("Close [{0}] {1}", client.CloseStatus, client.CloseStatusDescription);

        Assert.Equal(WebSocketCloseStatus.NormalClosure, client.CloseStatus);
        Assert.Equal("Finished", client.CloseStatusDescription);
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
        var pk = new Packet(data.GetBytes());
        pk.Next = png;
        pk.Append("\r\n------WebKitFormBoundary3ZXeqQWNjAzojVR7--\r\n".GetBytes());

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
        Assert.Equal(png, (av.Data as Packet)?.Data);
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
        await tcp.ConnectAsync(IPAddress.Loopback, 18080);
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
        using var server = new SmallLimitHttpServer { Port = 18082, Limit = 16, Log = XTrace.Log, SessionLog = XTrace.Log };
        server.Map("/small", () => "ok");
        server.Start();

        var client = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:18082") };
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
    #endregion
}