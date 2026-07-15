using System.ComponentModel;
using System.Net;
using System.Net.Http;
using NewLife.Http;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Http;

public class HttpTraceHandlerTests
{
    /// <summary>模拟内部处理器，直接返回固定响应</summary>
    private class OkHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    /// <summary>模拟内部处理器，抛出异常</summary>
    private class ErrorHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Simulated error");
        }
    }

    [Fact]
    [DisplayName("HttpTraceHandler_无Tracer_直接透传")]
    public async Task NoTracer_Passthrough()
    {
        var inner = new OkHandler();
        var handler = new HttpTraceHandler(inner)
        {
            Tracer = null,
        };
        using var client = new HttpClient(handler);

        var response = await client.GetAsync("http://localhost/passthrough");

        Assert.NotNull(inner.LastRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/passthrough", inner.LastRequest.RequestUri?.AbsolutePath);
    }

    [Fact]
    [DisplayName("HttpTraceHandler_有Tracer_请求通过埋点")]
    public async Task WithTracer_RequestTraced()
    {
        var tracer = new DefaultTracer
        {
            Period = 60,
            MaxSamples = 100,
        };

        var inner = new OkHandler();
        var handler = new HttpTraceHandler(inner)
        {
            Tracer = tracer,
        };
        using var client = new HttpClient(handler);

        var response = await client.GetAsync("http://localhost/traced");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // 验证有埋点数据生成
        var spans = tracer.TakeAll();
        Assert.NotEmpty(spans);
    }

    [Fact]
    [DisplayName("HttpTraceHandler_异常路径_记录埋点")]
    public async Task WithTracer_ExceptionPath()
    {
        var tracer = new DefaultTracer
        {
            Period = 60,
            MaxSamples = 100,
        };

        var handler = new HttpTraceHandler(new ErrorHandler())
        {
            Tracer = tracer,
        };
        using var client = new HttpClient(handler);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(async () =>
            await client.GetAsync("http://localhost/error"));

        Assert.Equal("Simulated error", ex.Message);

        // 验证有异常埋点
        var spans = tracer.TakeAll();
        Assert.NotEmpty(spans);
    }

    [Fact]
    [DisplayName("HttpTraceHandler_异常过滤器_仅记录匹配的异常")]
    public async Task ExceptionFilter_FiltersExceptions()
    {
        var tracer = new DefaultTracer
        {
            Period = 60,
            MaxSamples = 100,
        };

        var handler = new HttpTraceHandler(new ErrorHandler())
        {
            Tracer = tracer,
            // 仅记录 HttpRequestException 异常
            ExceptionFilter = ex => ex is HttpRequestException,
        };
        using var client = new HttpClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(async () =>
            await client.GetAsync("http://localhost/filter"));

        // 验证有埋点数据
        var spans = tracer.TakeAll();
        Assert.NotEmpty(spans);
    }
}
