using System.Net;
using System.Net.Http;
using NewLife.Http;
using Xunit;

namespace XUnitTest.Http;

/// <summary>GetString 的附加头部只作用于本次请求</summary>
public class HttpHelperHeaderIsolationTests
{
    private sealed class CaptureHandler : HttpMessageHandler
    {
        public List<String?> Auths { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Auths.Add(request.Headers.Authorization?.ToString());

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok"),
            });
        }
    }

    [Fact(DisplayName = "附加头部不会残留到后续请求")]
    public void HeadersDoNotLeakToNextRequest()
    {
        var handler = new CaptureHandler();
        using var client = new HttpClient(handler);

        var headers = new Dictionary<String, String> { ["Authorization"] = "Bearer secret" };
        var r1 = client.GetString("http://127.0.0.1/api/a", headers);
        var r2 = client.GetString("http://127.0.0.1/api/b");

        Assert.Equal("ok", r1);
        Assert.Equal("ok", r2);
        Assert.Equal("Bearer secret", handler.Auths[0]);
        Assert.Null(handler.Auths[1]);
        Assert.False(client.DefaultRequestHeaders.Contains("Authorization"));
    }

    [Fact(DisplayName = "附带头部含 CRLF 时拒绝发送")]
    public void RejectHeaderWithNewLine()
    {
        var handler = new CaptureHandler();
        using var client = new HttpClient(handler);

        var headers = new Dictionary<String, String> { ["X-Test"] = "a\r\nInjected: yes" };

        Assert.Throws<ArgumentException>(() => client.GetString("http://127.0.0.1/api/a", headers));
        Assert.Empty(handler.Auths);
    }
}
