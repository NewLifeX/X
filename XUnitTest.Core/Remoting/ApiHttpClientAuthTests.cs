using NewLife.Remoting;
using Xunit;

namespace XUnitTest.Remoting;

/// <summary>ApiHttpClient 令牌只在服务地址上附带</summary>
public class ApiHttpClientAuthTests
{
    private sealed class TestClient : ApiHttpClient
    {
        public System.Net.Http.HttpRequestMessage Build(String action) => BuildRequest(System.Net.Http.HttpMethod.Get, action, null, null);
    }

    [Fact(DisplayName = "令牌只发给服务地址，不发给第三方绝对地址")]
    public void TokenOnlySentToServiceTarget()
    {
        using var client = new TestClient();
        client.Add("svc", "http://127.0.0.1:1234");
        client.Token = "secret-token";

        // 相对路径：正常附带
        var relative = client.Build("api/info");
        Assert.NotNull(relative.Headers.Authorization);
        Assert.Equal("Bearer", relative.Headers.Authorization!.Scheme);

        // 同主机同端口：正常附带
        var same = client.Build("http://127.0.0.1:1234/api/info");
        Assert.NotNull(same.Headers.Authorization);

        // 第三方绝对地址（如 CDN 下载）：不能带令牌
        var thirdParty = client.Build("https://example.com/pkg.zip");
        Assert.Null(thirdParty.Headers.Authorization);
    }

    [Fact(DisplayName = "协议相对地址会被补全成跨主机请求，不能带令牌")]
    public void ProtocolRelativeAddressDoesNotCarryToken()
    {
        using var client = new TestClient();
        client.Add("svc", "http://127.0.0.1:1234");
        client.Token = "secret-token";

        // //host/path 与 \\host\share 都不是相对路径：HttpClient 会把它们按 BaseAddress 补全到别的站点
        Assert.Null(client.Build("//example.com/pkg.zip").Headers.Authorization);
        Assert.Null(client.Build("\\\\example.com\\share\\pkg.zip").Headers.Authorization);
    }
}
