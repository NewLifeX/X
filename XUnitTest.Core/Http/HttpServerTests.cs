using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
using NewLife.Remoting;
using Xunit;

namespace XUnitTest.Http
{
    public class HttpServerTests
    {
        private static HttpServer _server;
        private static Uri _baseUri;
        static HttpServerTests()
        {
            var server = new HttpServer
            {
                Port = 8080,
                Log = XTrace.Log,
                SessionLog = XTrace.Log
            };
            server.Start();

            _server = server;
            _baseUri = new Uri("http://127.0.0.1:8080");
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
            Assert.Equal(93917, rs.ReadBytes().Length);
        }

        [Fact]
        public async void MapController()
        {
            _server.MapController<ApiController>("/api");

            var client = new HttpClient { BaseAddress = _baseUri };
            var rs = await client.GetAsync<IDictionary<String, Object>>("/api/info", new { state = 1234 });

            Assert.Equal("1234", rs["state"]);
        }

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
        public async void MapWebSocket()
        {
            _server.Map("/ws", new WebSocketHandler());

            var client = new ClientWebSocket();
            await client.ConnectAsync(new Uri("ws://127.0.0.1:8080/ws"), default);
            await client.SendAsync("Hello NewLife".GetBytes(), System.Net.WebSockets.WebSocketMessageType.Text, true, default);

            var buf = new Byte[1024];
            var rs = await client.ReceiveAsync(buf, default);
            Assert.EndsWith("说，Hello NewLife", new Packet(buf, 0, rs.Count).ToStr());

            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "通信完成", default);
            XTrace.WriteLine("Close [{0}] {1}", client.CloseStatus, client.CloseStatusDescription);

            Assert.Equal(WebSocketCloseStatus.NormalClosure, client.CloseStatus);
            Assert.Equal("Finished", client.CloseStatusDescription);
        }
    }
}