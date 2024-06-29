using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
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
    public async void MapDelegate()
    {
        _server.Map("/", () => "<h1>Hello NewLife!</h1></br> " + DateTime.Now.ToFullString() + "</br><img src=\"logos/leaf.png\" />");

        var client = new HttpClient { BaseAddress = _baseUri };
        var html = await client.GetStringAsync("/");

        Assert.NotEmpty(html);
        Assert.StartsWith("<h1>Hello NewLife!</h1></br>", html);
        Assert.Contains("logos/leaf.png", html);
    }

    [Fact]
    public async void MapApi()
    {
        _server.Map("/user", (String act, Int32 uid) => new { code = 0, data = $"User.{act}({uid}) success!" });

        var client = new HttpClient { BaseAddress = _baseUri };
        var rs = await client.GetAsync<String>("/user", new { act = "edit", uid = 1234 });

        Assert.Equal("User.edit(1234) success!", rs);
    }

    [Fact]
    public async void MapStaticFiles()
    {
        XTrace.WriteLine("root: {0}", "http/".GetFullPath());
        _server.MapStaticFiles("/logos", "http/");

        var client = new HttpClient { BaseAddress = _baseUri };
        var rs = await client.GetStreamAsync("/logos/leaf.png");

        Assert.NotNull(rs);
        Assert.Equal(93917, rs.ReadBytes(-1).Length);
    }

    //[Fact]
    //public async void MapController()
    //{
    //    _server.MapController<ApiController>("/api");

    //    var client = new HttpClient { BaseAddress = _baseUri };
    //    var rs = await client.GetAsync<IDictionary<String, Object>>("/api/info", new { state = 1234 });

    //    Assert.Equal("1234", rs["state"]);
    //}

    [Fact]
    public async void MapMyHttpHandler()
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
    public async void BigPost()
    {
        _server.Map("/my2", new MyHttpHandler2());

        var client = new HttpClient { BaseAddress = _baseUri };

        var buf1 = new Byte[8 * 1024];
        var buf2 = new Byte[8 * 1024];

        Array.Fill(buf1, (Byte)'0');
        Array.Fill(buf2, (Byte)'0');

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
            Assert.Equal(8 * 1024 * 2, pk.Total);

            var name = context.Parameters["name"];
            var html = $"<h2>你好，<span color=\"red\">{name}</span></h2>";
            context.Response.SetResult(html);
        }
    }

    [Fact]
    public async void MapWebSocket()
    {
        _server.Map("/ws", new WebSocketHandler());

        var content = "Hello NewLife".GetBytes();

        var client = new ClientWebSocket();
        await client.ConnectAsync(new Uri("ws://127.0.0.1:18080/ws"), default);
        await client.SendAsync(content, System.Net.WebSockets.WebSocketMessageType.Text, true, default);

        var buf = new Byte[1024];
        var rs = await client.ReceiveAsync(buf, default);
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
    }
}