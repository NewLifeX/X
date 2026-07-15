using System.Net;
using NewLife.Http;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Http;

/// <summary>HTTP中间件测试</summary>
public class HttpMiddlewareTests
{
    private static HttpServer CreateServer(Action<HttpServer> configure)
    {
        var server = new HttpServer
        {
            Port = 0,
            Log = XTrace.Log,
            SessionLog = XTrace.Log,
        };
        configure(server);
        server.Start();
        return server;
    }

    [Fact(DisplayName = "CORS设置响应头")]
    public async Task CorsSetsHeaders()
    {
        using var server = CreateServer(s =>
        {
            s.UseCors();
            s.Map("/test", () => "OK");
        });

        var url = $"http://127.0.0.1:{server.Port}/test";
        using var http = new HttpClient();
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("Origin", "http://example.com");
        var res = await http.SendAsync(req);

        Assert.True(res.IsSuccessStatusCode);
        Assert.Equal("*", res.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault());
        Assert.NotNull(res.Headers.GetValues("Access-Control-Allow-Methods").FirstOrDefault());
        Assert.NotNull(res.Headers.GetValues("Access-Control-Allow-Headers").FirstOrDefault());
    }

    [Fact(DisplayName = "CORS OPTIONS返回204")]
    public async Task CorsOptionsReturns204()
    {
        using var server = CreateServer(s =>
        {
            s.UseCors();
            s.Map("/test", () => "OK");
        });

        var url = $"http://127.0.0.1:{server.Port}/test";
        using var http = new HttpClient();
        var req = new HttpRequestMessage(HttpMethod.Options, url);
        req.Headers.Add("Origin", "http://example.com");
        var res = await http.SendAsync(req);

        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    [Fact(DisplayName = "CORS自定义AllowOrigin")]
    public async Task CorsCustomAllowOrigin()
    {
        var cors = new CorsMiddleware { AllowOrigin = "https://myapp.com" };
        using var server = CreateServer(s =>
        {
            s.Use(cors.Invoke);
            s.Map("/test", () => "OK");
        });

        var url = $"http://127.0.0.1:{server.Port}/test";
        using var http = new HttpClient();
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("Origin", "https://myapp.com");
        var res = await http.SendAsync(req);

        Assert.True(res.IsSuccessStatusCode);
        Assert.Equal("https://myapp.com", res.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault());
    }

    [Fact(DisplayName = "ErrorHandler正常请求通过")]
    public async Task ErrorHandlerPassesThrough()
    {
        using var server = CreateServer(s =>
        {
            s.UseErrorHandler(false);
            s.Map("/ok", () => "All good");
        });

        var url = $"http://127.0.0.1:{server.Port}/ok";
        using var http = new HttpClient();
        var res = await http.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Equal("All good", body);
    }

    [Fact(DisplayName = "ErrorHandler捕获异常返回500")]
    public async Task ErrorHandlerCatchesException()
    {
        using var server = CreateServer(s =>
        {
            s.UseErrorHandler(false);
            s.Map("/error", (HttpProcessDelegate)(ctx => { throw new InvalidOperationException("测试异常"); }));
        });

        var url = $"http://127.0.0.1:{server.Port}/error";
        using var http = new HttpClient();
        var res = await http.GetAsync(url);

        Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);
    }

    [Fact(DisplayName = "ErrorHandler捕获HttpException带状态码")]
    public async Task ErrorHandlerCatchesHttpException()
    {
        using var server = CreateServer(s =>
        {
            s.UseErrorHandler(false);
            s.Map("/badrequest", (HttpProcessDelegate)(ctx => { throw new HttpException(HttpStatusCode.BadRequest, "参数错误"); }));
            s.Map("/forbidden", (HttpProcessDelegate)(ctx => { throw new HttpException(HttpStatusCode.Forbidden, "禁止访问"); }));
        });

        using var http = new HttpClient();

        var url1 = $"http://127.0.0.1:{server.Port}/badrequest";
        var res1 = await http.GetAsync(url1);
        Assert.Equal(HttpStatusCode.BadRequest, res1.StatusCode);

        var url2 = $"http://127.0.0.1:{server.Port}/forbidden";
        var res2 = await http.GetAsync(url2);
        Assert.Equal(HttpStatusCode.Forbidden, res2.StatusCode);
    }

    [Fact(DisplayName = "ErrorHandler IncludeDetails返回JSON错误")]
    public async Task ErrorHandlerIncludeDetails()
    {
        using var server = CreateServer(s =>
        {
            s.UseErrorHandler(includeDetails: true);
            s.Map("/error", (HttpProcessDelegate)(ctx => { throw new InvalidOperationException("详细异常"); }));
        });

        var url = $"http://127.0.0.1:{server.Port}/error";
        using var http = new HttpClient();
        var res = await http.GetAsync(url);

        Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("详细异常", body);
        Assert.Contains("InvalidOperationException", body);
    }

    [Fact(DisplayName = "多个中间件串联")]
    public async Task MultipleMiddlewareChain()
    {
        using var server = CreateServer(s =>
        {
            s.UseErrorHandler(false);
            s.UseCors();
            s.Map("/test", () => "Chained");
        });

        var url = $"http://127.0.0.1:{server.Port}/test";
        using var http = new HttpClient();
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("Origin", "http://example.com");
        var res = await http.SendAsync(req);

        Assert.True(res.IsSuccessStatusCode);
        Assert.Equal("*", res.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault());
        var body = await res.Content.ReadAsStringAsync();
        Assert.Equal("Chained", body);
    }
}
