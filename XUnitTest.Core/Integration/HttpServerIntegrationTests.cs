using System.Diagnostics;
using System.Net;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
using NewLife.Net;
using NewLife.Remoting;
using Xunit;
using ClientWebSocket = System.Net.WebSockets.ClientWebSocket;

namespace XUnitTest.Integration;

/// <summary>HttpServer 集成测试固定装置</summary>
public class HttpServerFixture : IDisposable
{
    /// <summary>HTTP 服务端实例</summary>
    public HttpServer Server { get; }

    /// <summary>服务端基础地址</summary>
    public Uri BaseUri { get; }

    public HttpServerFixture()
    {
        var server = new HttpServer
        {
            Name = "集成测试Http服务器",
            Port = 0,
            Log = XTrace.Log,
#if DEBUG
            SessionLog = XTrace.Log,
#endif
        };

        server.Map("/", () => "<h1>Hello NewLife!</h1></br> " + DateTime.Now.ToFullString());
        server.Map("/user", (String act, Int32 uid) => new { code = 0, data = $"User.{act}({uid}) success!" });
        server.Map("/my", new IntegrationHttpHandler());
        server.Map("/echo", new EchoBodyHandler());
        server.Map("/form", new FormEchoHandler());
        server.Map("/upload", new UploadEchoHandler());

        server.MapController<ApiController>("/api");
        server.Map("/status", new StatusCodeHandler());
        server.Map("/ws", new IntegrationWebSocketHandler());

        server.Start();

        Server = server;
        BaseUri = new Uri($"http://127.0.0.1:{server.Port}");
    }

    public void Dispose() => Server?.Dispose();
}

/// <summary>自定义 HttpHandler</summary>
class IntegrationHttpHandler : IHttpHandler
{
    public void ProcessRequest(IHttpContext context)
    {
        var name = context.Parameters["name"];
        var html = $"<h2>你好，<span color=\"red\">{name}</span></h2>";
        context.Response.SetResult(html);
    }
}

/// <summary>回显请求体处理器</summary>
class EchoBodyHandler : IHttpHandler
{
    public void ProcessRequest(IHttpContext context)
    {
        var body = context.Request.Body?.ToStr() ?? String.Empty;
        context.Response.SetResult(body);
    }
}

/// <summary>表单回显处理器</summary>
class FormEchoHandler : IHttpHandler
{
    public void ProcessRequest(IHttpContext context)
    {
        var body = context.Request.Body?.ToStr() ?? String.Empty;
        context.Response.SetResult(body);
    }
}

/// <summary>上传回显处理器</summary>
class UploadEchoHandler : IHttpHandler
{
    public void ProcessRequest(IHttpContext context)
    {
        var request = context.Request;
        var dic = request.ParseFormData();
        var fileList = dic.Values.OfType<FormFile>().ToArray();
        var count = fileList.Length;
        var name = count > 0 ? fileList[0].FileName : String.Empty;
        var len = request.Body?.Total ?? 0;

        context.Response.SetResult($"files={count};name={name};body={len}");
    }
}

/// <summary>集成测试专用 WebSocket 处理器：文本回显，二进制原样回显</summary>
class IntegrationWebSocketHandler : WebSocketHandler
{
    public override void ProcessMessage(NewLife.Http.WebSocket socket, WebSocketMessage message)
    {
        if (message.Type == WebSocketMessageType.Text)
        {
            var text = message.Payload?.ToStr() ?? String.Empty;
            socket.Send($"echo:{text}");
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

/// <summary>状态码处理器：根据查询參数 code 返回指定 HTTP 状态码</summary>
class StatusCodeHandler : IHttpHandler
{
    public void ProcessRequest(IHttpContext context)
    {
        var code = context.Parameters["code"].ToInt(200);
        context.Response.StatusCode = (HttpStatusCode)code;
        context.Response.SetResult($"status={code}");
    }
}

/// <summary>HttpServer 集成测试，验证 HTTP/WebSocket 功能</summary>
[Collection("Integration")]
[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
public class HttpServerIntegrationTests : IClassFixture<HttpServerFixture>
{
    private readonly HttpServerFixture _fixture;

    public HttpServerIntegrationTests(HttpServerFixture fixture) => _fixture = fixture;

    [Fact(DisplayName = "01-服务端已启动且端口已分配")]
    public void Test01_ServerStarted()
    {
        Assert.True(_fixture.Server.Active, "服务端应处于运行状态");
        Assert.True(_fixture.Server.Port > 0, "端口应已分配");
    }

    [Fact(DisplayName = "02-HttpClient GET / 返回 Hello NewLife")]
    public async Task Test02_HttpClient_GetRoot()
    {
        using var client = new HttpClient { BaseAddress = _fixture.BaseUri };
        var html = await client.GetStringAsync("/");

        Assert.NotEmpty(html);
        Assert.Contains("Hello NewLife", html);
    }

    [Fact(DisplayName = "03-HttpClient GET /user 参数绑定")]
    public async Task Test03_HttpClient_GetUser()
    {
        using var client = new HttpClient { BaseAddress = _fixture.BaseUri };
        var text = await client.GetStringAsync("/user?act=Delete&uid=1234");

        Assert.Contains("User.Delete(1234) success!", text);
    }

    [Fact(DisplayName = "04-HttpClient POST /echo JSON 请求体回显")]
    public async Task Test04_HttpClient_PostJson()
    {
        using var client = new HttpClient { BaseAddress = _fixture.BaseUri };
        var json = "{\"name\":\"stone\",\"age\":18}";
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/echo", content);

        response.EnsureSuccessStatusCode();
        var rs = await response.Content.ReadAsStringAsync();
        Assert.Equal(json, rs);
    }

    [Fact(DisplayName = "05-HttpClient POST /form 表单编码回显")]
    public async Task Test05_HttpClient_PostForm()
    {
        using var client = new HttpClient { BaseAddress = _fixture.BaseUri };
        using var content = new FormUrlEncodedContent(new Dictionary<String, String>
        {
            ["name"] = "stone",
            ["age"] = "18",
        });
        using var response = await client.PostAsync("/form", content);

        response.EnsureSuccessStatusCode();
        var rs = await response.Content.ReadAsStringAsync();
        Assert.Contains("name=stone", rs);
        Assert.Contains("age=18", rs);
    }

    [Fact(DisplayName = "06-HttpClient POST /upload 文件上传")]
    public async Task Test06_HttpClient_FileUpload()
    {
        using var client = new HttpClient { BaseAddress = _fixture.BaseUri };
        using var data = new MultipartFormDataContent();
        var bytes = Encoding.UTF8.GetBytes("HelloUpload");
        var fileContent = new ByteArrayContent(bytes);
        data.Add(fileContent, "file", "demo.txt");

        using var response = await client.PostAsync("/upload", data);
        response.EnsureSuccessStatusCode();
        var rs = await response.Content.ReadAsStringAsync();

        Assert.Contains("body=", rs);
        var bodyLen = rs.Substring("body=", null).ToInt();
        Assert.True(bodyLen > 0, $"上传请求体长度应大于0，实际：{bodyLen}；响应：{rs}");
    }

    [Fact(DisplayName = "07-ApiHttpClient GET /user")]
    public async Task Test07_ApiHttpClient_GetUser()
    {
        var http = new ApiHttpClient(_fixture.BaseUri.ToString()) { Log = XTrace.Log };
        var rs = await http.GetAsync<String>("/user", new { act = "Delete", uid = 1234 });

        Assert.Equal("User.Delete(1234) success!", rs);
    }

    [Fact(DisplayName = "08-ApiHttpClient POST /echo")]
    public async Task Test08_ApiHttpClient_PostEcho()
    {
        var http = new ApiHttpClient(_fixture.BaseUri.ToString()) { Log = XTrace.Log };
        var obj = new { name = "stone", age = 18 };
        var rs = await http.PostAsync<Object>("/echo", obj);
        var json = NewLife.Serialization.JsonHelper.ToJson(rs);

        Assert.Contains("stone", json);
        Assert.Contains("18", json);
    }

    [Fact(DisplayName = "09-ApiHttpClient GET /api/info 控制器")]
    public async Task Test09_ApiInfoRequest()
    {
        var http = new ApiHttpClient(_fixture.BaseUri.ToString()) { Log = XTrace.Log };
        var rs = await http.GetAsync<Object>("/api/info", new { state = "test" });

        Assert.NotNull(rs);
        var json = NewLife.Serialization.JsonHelper.ToJson(rs);
        Assert.Contains("MachineName", json);
    }

    [Fact(DisplayName = "10-TinyHttpClient GET /")]
    public async Task Test10_TinyHttpClient_Get()
    {
        using var client = new TinyHttpClient();
        var rs = await client.GetStringAsync(_fixture.BaseUri.ToString().TrimEnd('/') + "/");

        Assert.NotNull(rs);
        Assert.Contains("Hello NewLife", rs);
    }

    [Fact(DisplayName = "11-TinyHttpClient InvokeAsync GET /user")]
    public async Task Test11_TinyHttpClient_Invoke()
    {
        using var client = new TinyHttpClient(_fixture.BaseUri.ToString());
        var rs = await client.InvokeAsync<String>("GET", "/user", new { act = "Delete", uid = 1234 });

        Assert.NotNull(rs);
        Assert.Contains("User.Delete(1234) success!", rs);
    }

    [Fact(DisplayName = "12-System.ClientWebSocket 连接 /ws 文本回显")]
    public async Task Test12_SystemClientWebSocket()
    {
        var wsUri = new Uri($"ws://127.0.0.1:{_fixture.Server.Port}/ws");
        using var ws = new ClientWebSocket();

        await ws.ConnectAsync(wsUri, default);
        Assert.Equal(System.Net.WebSockets.WebSocketState.Open, ws.State);

        var msg = "Hello NewLife";
        await ws.SendAsync(Encoding.UTF8.GetBytes(msg), System.Net.WebSockets.WebSocketMessageType.Text, true, default);

        var buf = new Byte[2048];
        var result = await ws.ReceiveAsync(buf, default);
        var reply = Encoding.UTF8.GetString(buf, 0, result.Count);

        Assert.Equal($"echo:{msg}", reply);
        await ws.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "测试完成", default);
    }

    [Fact(DisplayName = "13-NewLife.WebSocketClient 连接 /ws 文本回显")]
    public async Task Test13_NewLifeWebSocketClient()
    {
        var ws = new WebSocketClient($"ws://127.0.0.1:{_fixture.Server.Port}/ws")
        {
            Log = XTrace.Log,
        };

        var opened = await ws.OpenAsync();
        Assert.True(opened, "WebSocketClient 打开连接失败");

        var text = "from-newlife-client";
        var wait = new TaskCompletionSource<String>();
        ws.Received += (s, e) =>
        {
            if (e.Message is WebSocketMessage m && m.Type == WebSocketMessageType.Text)
            {
                var str = m.Payload?.ToStr() ?? String.Empty;
                wait.TrySetResult(str);
            }
            else
            {
                wait.TrySetResult(e.Packet?.ToStr() ?? String.Empty);
            }
        };

        await ws.SendTextAsync(text);

        var reply = await wait.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal($"echo:{text}", reply);
        await ws.CloseAsync(1000, "done");
    }

    [Fact(DisplayName = "14-NewLife.WebSocketClient 二进制帧回显 256 字节")]
    public async Task Test14_NewLifeWebSocketClient_Binary()
    {
        var ws = new WebSocketClient($"ws://127.0.0.1:{_fixture.Server.Port}/ws")
        {
            Log = XTrace.Log,
        };

        var opened = await ws.OpenAsync();
        Assert.True(opened, "WebSocketClient 打开连接失败");

        var data = new Byte[256];
        Random.Shared.NextBytes(data);
        // ToPacket 会原地 XOR 修改数组，先保存副本
        var originalData = data.ToArray();

        var wait = new TaskCompletionSource<Byte[]>();
        ws.Received += (s, e) =>
        {
            if (e.Message is WebSocketMessage m && m.Type == WebSocketMessageType.Binary)
                wait.TrySetResult(m.Payload?.ToArray() ?? []);
        };

        await ws.SendBinaryAsync(new ArrayPacket(data));
        var reply = await wait.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await ws.CloseAsync(1000, "done");

        Assert.Equal(originalData, reply);
    }

    [Fact(DisplayName = "15-GET /status?code=404 返回 404 Not Found")]
    public async Task Test15_StatusCode_404()
    {
        using var client = new HttpClient { BaseAddress = _fixture.BaseUri };
        using var response = await client.GetAsync("/status?code=404");

        Assert.Equal(404, (Int32)response.StatusCode);
    }

    [Fact(DisplayName = "16-GET /status?code=500 返回 500 Internal Server Error")]
    public async Task Test16_StatusCode_500()
    {
        using var client = new HttpClient { BaseAddress = _fixture.BaseUri };
        using var response = await client.GetAsync("/status?code=500");

        Assert.Equal(500, (Int32)response.StatusCode);
    }

    [Fact(DisplayName = "17-HTTP GET 并发吞吐：热身+50并发任务×1000请求=50000条，TPS≥10000")]
    public async Task Test17_Http_GET_100K_TPS()
    {
        var baseUri = _fixture.BaseUri;

        // 单一 HttpClient 实例，内部连接池自动复用
        using var client = new HttpClient { BaseAddress = baseUri };

        // 热身：10并发各发100请求，预热 JIT 与 HTTP 连接池
        {
            var warmupTasks = Enumerable.Range(0, 10).Select(async _ =>
            {
                for (var i = 0; i < 100; i++)
                {
                    using var r = await client.GetAsync("/");
                }
            }).ToArray();
            await Task.WhenAll(warmupTasks).WaitAsync(TimeSpan.FromSeconds(30));
        }

        const Int32 clientCount = 50;
        const Int32 perClient = 1_000;
        const Int32 total = clientCount * perClient;

        var sw = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, clientCount).Select(async _ =>
        {
            var count = 0;
            for (var i = 0; i < perClient; i++)
            {
                using var response = await client.GetAsync("/");
                if (response.IsSuccessStatusCode) count++;
            }
            return count;
        }).ToArray();

        var counts = await Task.WhenAll(tasks);
        sw.Stop();

        var completed = counts.Sum();
        var tps = total / sw.Elapsed.TotalSeconds;
        XTrace.WriteLine("HTTP GET 100K TPS（热身后）：{0}条/{1}ms，TPS={2:N0}", total, sw.ElapsedMilliseconds, tps);

        Assert.Equal(total, completed);
        Assert.True(tps >= 10_000, $"TPS={tps:N0}，低于100000，耗时={sw.ElapsedMilliseconds}ms");
    }

    [Fact(DisplayName = "18-ApiHttpClient 50并发 POST /echo，全部响应内容正确")]
    public async Task Test18_ApiHttpClient_ConcurrentPost()
    {
        const Int32 count = 50;
        var http = new ApiHttpClient(_fixture.BaseUri.ToString());

        var tasks = Enumerable.Range(0, count).Select(async i =>
        {
            var obj = new { name = "stone", age = i };
            var rs = await http.PostAsync<Object>("/echo", obj);
            var json = NewLife.Serialization.JsonHelper.ToJson(rs);
            return json.Contains("stone");
        }).ToArray();

        var results = await Task.WhenAll(tasks);
        Assert.All(results, Assert.True);
    }
}