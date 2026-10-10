using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using NewLife.Http;
using NewLife.Remoting;
using NewLife.Web;
using Xunit;

namespace XUnitTest.Http;

public class TokenHttpFilterTests
{
    [Fact(DisplayName = "令牌端点请求被识别并放行，不触发取令牌")]
    public async Task SelfTokenRequest_Skipped()
    {
        var handler = new FakeHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:1") };

        // 默认 Action 不带前导斜杠，请求路径也不带
        var filter = new TokenHttpFilter { UserName = "u", Password = "p" };
        var request = new HttpRequestMessage(HttpMethod.Post, "OAuth/Token");
        await filter.OnRequest(client, request, null, default);

        Assert.Equal(0, handler.TokenCalls);
        Assert.Null(filter.Token);
        Assert.Null(request.Headers.Authorization);

        // Action 带前导斜杠
        var filter2 = new TokenHttpFilter { UserName = "u", Password = "p", Action = "/OAuth/Token" };
        var request2 = new HttpRequestMessage(HttpMethod.Post, "/OAuth/Token");
        await filter2.OnRequest(client, request2, null, default);

        Assert.Equal(0, handler.TokenCalls);
        Assert.Null(filter2.Token);

        // 不带 BaseAddress 时以绝对 Url 匹配
        var filter3 = new TokenHttpFilter { UserName = "u", Password = "p" };
        using var client3 = new HttpClient(handler);
        var request3 = new HttpRequestMessage(HttpMethod.Post, "http://127.0.0.1:9/OAuth/Token");
        await filter3.OnRequest(client3, request3, null, default);

        Assert.Equal(0, handler.TokenCalls);
        Assert.Null(filter3.Token);
    }

    [Fact(DisplayName = "取令牌过程中的重入请求直接放行，不再互等3秒")]
    public async Task NestedTokenRequest_NoWaiting()
    {
        var handler = new FakeHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:1") };

        var filter = new ReentryFilter { UserName = "u", Password = "p" };
        var request = new HttpRequestMessage(HttpMethod.Get, "api/info");
        await filter.OnRequest(client, request, null, default);

        Assert.Equal(1, filter.NestedCalls);
        Assert.True(filter.NestedElapsedMs < 1000, $"重入请求耗时 {filter.NestedElapsedMs}ms，应远小于3秒");
        Assert.False(filter.NestedSawToken);

        // 外层正常取到令牌并附加
        Assert.NotNull(filter.Token);
        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
    }

    [Fact(DisplayName = "普通请求照常取令牌并附加授权头")]
    public async Task NormalRequest_AcquiresAndAttaches()
    {
        var handler = new FakeHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:1") };

        var filter = new TokenHttpFilter { UserName = "u", Password = "p" };
        var request = new HttpRequestMessage(HttpMethod.Get, "api/info");
        await filter.OnRequest(client, request, null, default);

        Assert.Equal(1, handler.TokenCalls);
        Assert.NotNull(filter.Token);
        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("tok1", request.Headers.Authorization?.Parameter);
    }

    /// <summary>申请令牌时再次进入过滤器（模拟 SendAuth 内部请求走同一过滤器）</summary>
    private sealed class ReentryFilter : TokenHttpFilter
    {
        public Int32 NestedCalls;
        public Int64 NestedElapsedMs;
        public Boolean NestedSawToken;

        protected override async Task<IToken?> SendAuth(HttpClient client, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/info");
            var sw = Stopwatch.StartNew();
            await OnRequest(client, request, null, cancellationToken);
            sw.Stop();

            NestedElapsedMs = sw.ElapsedMilliseconds;
            NestedSawToken = request.Headers.Authorization != null;
            NestedCalls++;

            return new TokenModel { AccessToken = "tok1", ExpireIn = 3600 };
        }
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        public Int32 TokenCalls;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.ToString() ?? "";
            if (path.Contains("OAuth/Token", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref TokenCalls);
                var json = "{\"access_token\":\"tok1\",\"token_type\":\"Bearer\",\"expire_in\":3600}";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"code\":0,\"data\":{\"ok\":true}}", Encoding.UTF8, "application/json") });
        }
    }
}
