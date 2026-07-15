using System.ComponentModel;
using System.Net;
using System.Net.Http;
using NewLife.Http;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Http;

public class DnsHttpHandlerTests
{
    private class MockDnsResolver : IDnsResolver
    {
        public IPAddress[]? Resolve(String host)
        {
            return host switch
            {
                "test.local" => [IPAddress.Parse("10.0.0.1"), IPAddress.Parse("10.0.0.2")],
                "single.local" => [IPAddress.Parse("192.168.1.1")],
                _ => null,
            };
        }
    }

    /// <summary>捕获最后一个请求的 Handler，用于验证 URI 修改</summary>
    private class CaptureHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    [Fact]
    [DisplayName("DnsHttpHandler_自定义解析器_替换Host为IP")]
    public async Task CustomResolver_ReplacesHostWithIP()
    {
        var resolver = new MockDnsResolver();
        var capture = new CaptureHandler();

        var handler = new DnsHttpHandler(capture)
        {
            Resolver = resolver,
        };
        using var client = new HttpClient(handler);

        var response = await client.GetAsync("http://test.local/hello");

        Assert.NotNull(capture.LastRequest);
        // Host 头应保留原始域名
        Assert.Equal("test.local", capture.LastRequest.Headers.Host);
        // URI 应被替换为 IP
        Assert.Equal("10.0.0.1", capture.LastRequest.RequestUri?.Host);
        Assert.Equal("/hello", capture.LastRequest.RequestUri?.AbsolutePath);
    }

    [Fact]
    [DisplayName("DnsHttpHandler_多IP_取第一个地址")]
    public async Task MultipleIPs_UsesFirstAddress()
    {
        var resolver = new MockDnsResolver();
        var capture = new CaptureHandler();

        var handler = new DnsHttpHandler(capture)
        {
            Resolver = resolver,
        };
        using var client = new HttpClient(handler);

        var response = await client.GetAsync("http://test.local/path");
        var host = capture.LastRequest?.RequestUri?.Host;
        Assert.NotNull(host);
        // 第一次请求使用第一个 IP
        Assert.Equal("10.0.0.1", host);
    }

    [Fact]
    [DisplayName("DnsHttpHandler_已是IP地址_不进行解析")]
    public async Task AlreadyIPAddress_SkipsResolution()
    {
        var resolver = new MockDnsResolver();
        var capture = new CaptureHandler();

        var handler = new DnsHttpHandler(capture)
        {
            Resolver = resolver,
        };
        using var client = new HttpClient(handler);

        var response = await client.GetAsync("http://127.0.0.1/ip");

        Assert.NotNull(capture.LastRequest);
        // IP 地址应保持不变
        Assert.Equal("127.0.0.1", capture.LastRequest.RequestUri?.Host);
        Assert.Equal("/ip", capture.LastRequest.RequestUri?.AbsolutePath);
        // Host 头不应被设置（127.0.0.1 是 IP）
        Assert.Null(capture.LastRequest.Headers.Host);
    }

    [Fact]
    [DisplayName("DnsHttpHandler_解析为空_不替换URI")]
    public async Task ResolveReturnsNull_KeepsOriginalUri()
    {
        var resolver = new MockDnsResolver();
        var capture = new CaptureHandler();

        var handler = new DnsHttpHandler(capture)
        {
            Resolver = resolver,
        };
        using var client = new HttpClient(handler);

        // unknown.local 没有在 MockDnsResolver 中定义，返回 null
        var response = await client.GetAsync("http://unknown.local/null");

        Assert.NotNull(capture.LastRequest);
        // 解析返回 null，URI 应保持不变
        Assert.Equal("unknown.local", capture.LastRequest.RequestUri?.Host);
    }

    [Fact]
    [DisplayName("DnsHttpHandler_单IP_不轮询")]
    public async Task SingleIP_NoRoundRobin()
    {
        var resolver = new MockDnsResolver();
        var capture = new CaptureHandler();

        var handler = new DnsHttpHandler(capture)
        {
            Resolver = resolver,
        };
        using var client = new HttpClient(handler);

        var response1 = await client.GetAsync("http://single.local/");
        var ip1 = capture.LastRequest?.RequestUri?.Host;
        Assert.Equal("192.168.1.1", ip1);

        var response2 = await client.GetAsync("http://single.local/");
        var ip2 = capture.LastRequest?.RequestUri?.Host;
        // 只有一个 IP，轮询后仍返回相同的 IP
        Assert.Equal("192.168.1.1", ip2);
        Assert.Equal(ip1, ip2);
    }
}
