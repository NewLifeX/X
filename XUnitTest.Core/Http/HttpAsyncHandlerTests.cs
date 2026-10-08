using System.ComponentModel;
using System.Net;
using NewLife.Http;
using Xunit;

namespace XUnitTest.Http;

/// <summary>HTTP 处理器异步契约（IHttpHandler.ProcessRequestAsync）实网测试</summary>
public class HttpAsyncHandlerTests : IDisposable
{
    private readonly HttpServer _server;
    private readonly Uri _baseUri;

    public HttpAsyncHandlerTests()
    {
        var server = new HttpServer { Port = 0 };
        server.Start();

        _server = server;
        _baseUri = new Uri($"http://127.0.0.1:{server.Port}");
    }

    public void Dispose() => _server?.Dispose();

    /// <summary>异步处理器：等待后写响应，记录调用次数</summary>
    private sealed class AsyncHandler : IHttpHandler
    {
        public Int32 Calls;

        public async Task ProcessRequestAsync(IHttpContext context)
        {
            Interlocked.Increment(ref Calls);

            await Task.Delay(10).ConfigureAwait(false);

            context.Response.SetResult("async-ok");
        }
    }

    [Fact]
    [DisplayName("HTTP异步处理器_等待后写响应_响应正常")]
    public async Task AsyncHandler_Awaited()
    {
        var handler = new AsyncHandler();
        _server.Map("/async", handler);

        var client = new HttpClient { BaseAddress = _baseUri };
        var text = await client.GetStringAsync("/async");

        Assert.Equal("async-ok", text);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    [DisplayName("HTTP异步委托_返回Task泛型结果_取回响应体")]
    public async Task AsyncDelegate_TaskResult()
    {
        _server.Map("/delegate", new DelegateHandler
        {
            Callback = (Func<IHttpContext, Task<String>>)(async ctx =>
            {
                await Task.Delay(10).ConfigureAwait(false);

                return "delegate-ok";
            })
        });

        var client = new HttpClient { BaseAddress = _baseUri };
        var text = await client.GetStringAsync("/delegate");

        Assert.Equal("delegate-ok", text);
    }

    [Fact]
    [DisplayName("HTTP异步处理器_抛业务异常_按同步口径映射状态码")]
    public async Task AsyncHandler_Throws_MapsStatusCode()
    {
        _server.Map("/boom", new DelegateHandler
        {
            Callback = (HttpProcessAsyncDelegate)(async ctx =>
            {
                await Task.Yield();

                throw new HttpException(HttpStatusCode.BadGateway, "async-fail");
            })
        });

        var client = new HttpClient { BaseAddress = _baseUri };
        var res = await client.GetAsync("/boom");

        Assert.Equal(HttpStatusCode.BadGateway, res.StatusCode);
    }

    [Fact]
    [DisplayName("HTTP异步处理器_经中间件管道_顺序与同步口径一致")]
    public async Task AsyncHandler_ThroughMiddleware()
    {
        var log = new List<String>();
        _server.Use(async (ctx, next) =>
        {
            log.Add("before");

            await next();

            log.Add("after");
        });

        _server.Map("/pipeline", new DelegateHandler
        {
            Callback = (HttpProcessAsyncDelegate)(async ctx =>
            {
                await Task.Delay(5).ConfigureAwait(false);

                log.Add("handler");
                ctx.Response.SetResult("mw-ok");
            })
        });

        var client = new HttpClient { BaseAddress = _baseUri };
        var text = await client.GetStringAsync("/pipeline");

        Assert.Equal("mw-ok", text);
        Assert.Equal(["before", "handler", "after"], log);
    }

    [Fact]
    [DisplayName("HTTP同步处理器_仍走同步入口_行为不变")]
    public async Task SyncHandler_Unchanged()
    {
        var called = 0;
        _server.Map("/sync", new DelegateHandler
        {
            Callback = (HttpProcessDelegate)(ctx =>
            {
                called++;
                ctx.Response.SetResult("sync-ok");
            })
        });

        var client = new HttpClient { BaseAddress = _baseUri };
        var text = await client.GetStringAsync("/sync");

        Assert.Equal("sync-ok", text);
        Assert.Equal(1, called);
    }
}
