using System;
using NewLife;
using NewLife.Web;
using Xunit;

namespace XUnitTest.Web;

public class UriInfoTests
{
    [Theory]
    [InlineData("http://localhost:8080/cube/info", "http", "localhost", 8080, "/cube/info")]
    [InlineData("http://localhost:8080/", "http", "localhost", 8080, "/")]
    [InlineData("Http://localhost/", "Http", "localhost", 0, "/")]
    [InlineData("Http://localhost", "Http", "localhost", 0, "/")]
    [InlineData("https://localhost:8080/cube/info", "https", "localhost", 8080, "/cube/info")]
    [InlineData("https://localhost:8080/", "https", "localhost", 8080, "/")]
    [InlineData("Https://localhost/", "Https", "localhost", 0, "/")]
    [InlineData("Https://localhost", "Https", "localhost", 0, "/")]
    [InlineData("wss://localhost:8080/cube/info", "wss", "localhost", 8080, "/cube/info")]
    [InlineData("wss://localhost:8080/", "wss", "localhost", 8080, "/")]
    [InlineData("wss://localhost/", "wss", "localhost", 0, "/")]
    [InlineData("wss://localhost", "wss", "localhost", 0, "/")]
    [InlineData("localhost:8080/cube/info", null, "localhost", 8080, "/cube/info")]
    [InlineData("localhost:8080/", null, "localhost", 8080, "/")]
    [InlineData("localhost/", null, "localhost", 0, "/")]
    [InlineData("localhost", null, "localhost", 0, "/")]
    [InlineData("/dotnet-sdk-6.0.100-preview.6.21355.2-win-x64.exe", null, null, 0, "/dotnet-sdk-6.0.100-preview.6.21355.2-win-x64.exe")]
    [InlineData("dotNet/dotnet-sdk-6.0.100-preview.6.21355.2-win-x64.exe", null, "dotNet", 0, "/dotnet-sdk-6.0.100-preview.6.21355.2-win-x64.exe")]
    public void Parse(String url, String schema, String host, Int32 port, String path)
    {
        {
            var uri = new UriInfo(url);
            Assert.Equal(schema, uri.Scheme);
            Assert.Equal(host, uri.Host);
            Assert.Equal(port, uri.Port);
            Assert.Equal(path, uri.PathAndQuery);

            if (port == 0)
            {
                if (host.IsNullOrEmpty())
                {
                    Assert.Null(uri.Authority);
                    if (schema.IsNullOrEmpty())
                        Assert.Equal(path, uri.ToString());
                    else
                        Assert.Equal($"{schema}://{path}", uri.ToString());
                }
                else
                {
                    Assert.Equal(host, uri.Authority);
                    if (schema.IsNullOrEmpty())
                        Assert.Equal($"{host}{path}", uri.ToString());
                    else
                        Assert.Equal($"{schema}://{host}{path}", uri.ToString());
                }
            }
            else
            {
                if (host.IsNullOrEmpty())
                {
                    Assert.Equal($"{host}:{port}", uri.Authority);
                    if (schema.IsNullOrEmpty())
                        Assert.Equal(path, uri.ToString());
                    else
                        Assert.Equal($"{schema}://{host}:{port}{path}", uri.ToString());
                }
                else
                {
                    Assert.Equal($"{host}:{port}", uri.Authority);
                    if (schema.IsNullOrEmpty())
                        Assert.Equal($"{host}:{port}{path}", uri.ToString());
                    else
                        Assert.Equal($"{schema}://{host}:{port}{path}", uri.ToString());
                }
            }
        }
        if (!url.StartsWith("/"))
        {
            if (!url.StartsWithIgnoreCase("http://", "https://", "ws://", "wss://"))
                url = "http://" + url;

            if (schema.IsNullOrEmpty()) schema = "http";
            if (path.IsNullOrEmpty()) path = "/";
            //schema = schema?.ToLower();

            if (port == 0 && schema.EqualIgnoreCase("http", "ws"))
                port = 80;
            else if (port == 0 && schema.EqualIgnoreCase("https", "wss"))
                port = 443;

            var uri = new Uri(url);
            Assert.Equal(schema, uri.Scheme, true);
            Assert.Equal(host, uri.Host, true);
            Assert.Equal(port, uri.Port);
            Assert.Equal(path, uri.PathAndQuery);

            if (port == 0 ||
                port == 80 && schema.EqualIgnoreCase("http", "ws") ||
                port == 443 && schema.EqualIgnoreCase("https", "wss"))
            {
                Assert.Equal($"{host}", uri.Authority, true);
                Assert.Equal($"{schema}://{host}{path}", uri.ToString(), true);
            }
            else
            {
                Assert.Equal($"{host}:{port}", uri.Authority, true);
                Assert.Equal($"{schema}://{host}:{port}{path}", uri.ToString(), true);
            }
        }
    }

    [Theory]
    [InlineData("http://localhost:8080/cube/info?name=newlife", "http", "localhost", 8080, "/cube/info", "?name=newlife")]
    [InlineData("http://localhost:8080/?name=newlife", "http", "localhost", 8080, "/", "?name=newlife")]
    [InlineData("http://localhost:8080?name=newlife", "http", "localhost", 8080, "/", "?name=newlife")]
    [InlineData("Http://localhost/?name=newlife", "Http", "localhost", 0, "/", "?name=newlife")]
    [InlineData("Http://localhost?name=newlife", "Http", "localhost", 0, "/", "?name=newlife")]
    public void Parse2(String url, String schema, String host, Int32 port, String path, String query)
    {
        {
            var uri = new UriInfo(url);
            Assert.Equal(schema, uri.Scheme);
            Assert.Equal(host, uri.Host);
            Assert.Equal(port, uri.Port);
            Assert.Equal(path, uri.AbsolutePath);
            Assert.Equal(query, uri.Query);

            if (port == 0)
            {
                if (host.IsNullOrEmpty())
                {
                    Assert.Null(uri.Authority);
                    if (schema.IsNullOrEmpty())
                        Assert.Equal($"{path}{query}", uri.ToString());
                    else
                        Assert.Equal($"{schema}://{path}{query}", uri.ToString());
                }
                else
                {
                    Assert.Equal(host, uri.Authority);
                    if (schema.IsNullOrEmpty())
                        Assert.Equal($"{host}{path}{query}", uri.ToString());
                    else
                        Assert.Equal($"{schema}://{host}{path}{query}", uri.ToString());
                }
            }
            else
            {
                if (host.IsNullOrEmpty())
                {
                    Assert.Equal($"{host}:{port}", uri.Authority);
                    if (schema.IsNullOrEmpty())
                        Assert.Equal($"{path}{query}", uri.ToString());
                    else
                        Assert.Equal($"{schema}://{host}:{port}{path}{query}", uri.ToString());
                }
                else
                {
                    Assert.Equal($"{host}:{port}", uri.Authority);
                    if (schema.IsNullOrEmpty())
                        Assert.Equal($"{host}:{port}{path}{query}", uri.ToString());
                    else
                        Assert.Equal($"{schema}://{host}:{port}{path}{query}", uri.ToString());
                }
            }
        }
        if (!url.StartsWith("/"))
        {
            if (!url.StartsWithIgnoreCase("http://", "https://", "ws://", "wss://"))
                url = "http://" + url;

            if (schema.IsNullOrEmpty()) schema = "http";
            if (path.IsNullOrEmpty()) path = "/";
            //schema = schema?.ToLower();

            if (port == 0 && schema.EqualIgnoreCase("http", "ws"))
                port = 80;
            else if (port == 0 && schema.EqualIgnoreCase("https", "wss"))
                port = 443;

            var uri = new Uri(url);
            Assert.Equal(schema, uri.Scheme, true);
            Assert.Equal(host, uri.Host, true);
            Assert.Equal(port, uri.Port);
            Assert.Equal(path, uri.AbsolutePath);
            Assert.Equal(query, uri.Query);

            if (port == 0 ||
                port == 80 && schema.EqualIgnoreCase("http", "ws") ||
                port == 443 && schema.EqualIgnoreCase("https", "wss"))
            {
                Assert.Equal($"{host}", uri.Authority, true);
                Assert.Equal($"{schema}://{host}{path}{query}", uri.ToString(), true);
            }
            else
            {
                Assert.Equal($"{host}:{port}", uri.Authority, true);
                Assert.Equal($"{schema}://{host}:{port}{path}{query}", uri.ToString(), true);
            }
        }
    }

    [Fact]
    public void Test()
    {
        var url = "http://localhost:8080/cube/info";
        var uri = new UriInfo(url);

        // 故意没有问号开头，转字符串后自己补上
        uri.Query = "name=newlife";

        Assert.Equal(url + "?" + uri.Query, uri.ToString());

        var rs = UriInfo.TryParse(url, out var uri2);
        Assert.True(rs);
        Assert.NotNull(uri2);
    }

    [Fact]
    public void Append()
    {
        var uri = new UriInfo();
        Assert.Null(uri.ToString());

        uri = new UriInfo("/");
        Assert.Equal("/", uri.ToString());

        uri.Append("age", 18);
        Assert.Equal("/?age=18", uri.ToString());

        uri = new UriInfo("/cube/info?name=newlife");
        Assert.Equal("/cube/info?name=newlife", uri.ToString());

        uri.Append("age", 18);
        Assert.Equal("/cube/info?name=newlife&age=18", uri.ToString());
    }

    [Fact]
    public void AppendNotEmpty()
    {
        var uri = new UriInfo("/");
        Assert.Equal("/", uri.ToString());

        uri.AppendNotEmpty("age", 18);
        Assert.Equal("/?age=18", uri.ToString());

        uri = new UriInfo("/cube/info?name=newlife");
        Assert.Equal("/cube/info?name=newlife", uri.ToString());

        uri.AppendNotEmpty("age", null);
        Assert.Equal("/cube/info?name=newlife", uri.ToString());
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("http://localhost", true)]
    [InlineData("/path", true)]
    [InlineData("host", true)]
    public void TryParse(String? url, Boolean expected)
    {
        var result = UriInfo.TryParse(url, out var uri);
        Assert.Equal(expected, result);

        if (expected)
            Assert.NotNull(uri);
        else
            Assert.Null(uri);
    }

    [Theory]
    [InlineData("http://localhost:80/path", "localhost")]
    [InlineData("https://localhost:443/path", "localhost")]
    [InlineData("http://localhost:8080/path", "localhost:8080")]
    [InlineData("https://localhost:8443/path", "localhost:8443")]
    [InlineData("ws://localhost:80/path", "localhost")]
    [InlineData("wss://localhost:443/path", "localhost")]
    [InlineData("ws://localhost:8080/path", "localhost:8080")]
    [InlineData("wss://localhost:8443/path", "localhost:8443")]
    [InlineData("ftp://localhost:21/path", "localhost:21")]
    public void Authority(String url, String expected)
    {
        var uri = new UriInfo(url);
        Assert.Equal(expected, uri.Authority);
    }

    [Theory]
    [InlineData("http://localhost/path?query=1", "/path?query=1")]
    [InlineData("http://localhost/path?query=1&name=test", "/path?query=1&name=test")]
    [InlineData("http://localhost/path", "/path")]
    [InlineData("http://localhost", "/")]
    [InlineData("http://localhost?query=1", "/?query=1")]
    public void PathAndQuery(String url, String expected)
    {
        var uri = new UriInfo(url);
        Assert.Equal(expected, uri.PathAndQuery);
    }

    [Theory]
    [InlineData("http://192.168.1.1:8080/path", "192.168.1.1", 8080)]
    [InlineData("https://10.0.0.1/path", "10.0.0.1", 0)]
    [InlineData("http://127.0.0.1:3000/api", "127.0.0.1", 3000)]
    public void ParseIPAddress(String url, String expectedHost, Int32 expectedPort)
    {
        var uri = new UriInfo(url);
        Assert.Equal(expectedHost, uri.Host);
        Assert.Equal(expectedPort, uri.Port);
    }

    [Theory]
    [InlineData("http://[::1]:8080/path", "::1", 8080)]
    [InlineData("http://[2001:db8::1]/path", "2001:db8::1", 0)]
    [InlineData("https://[fe80::1]:443/api", "fe80::1", 443)]
    [InlineData("http://[::ffff:192.0.2.1]:9000/api", "::ffff:192.0.2.1", 9000)]
    [InlineData("http://[2001:0db8:0000:0000:0000:ff00:0042:8329]/path", "2001:0db8:0000:0000:0000:ff00:0042:8329", 0)]
    [InlineData("ws://[::]:8080/socket", "::", 8080)]
    [InlineData("wss://[fe80::1%eth0]:443/ws", "fe80::1%eth0", 443)]
    public void ParseIPv6Address(String url, String expectedHost, Int32 expectedPort)
    {
        var uri = new UriInfo(url);
        Assert.Equal(expectedHost, uri.Host);
        Assert.Equal(expectedPort, uri.Port);
    }

    [Theory]
    [InlineData("http://example.com/path%20with%20spaces", "/path%20with%20spaces")]
    [InlineData("http://example.com/path?query=%E4%B8%AD%E6%96%87", "?query=%E4%B8%AD%E6%96%87")]
    [InlineData("http://example.com/path?a=1&b=2&c=3", "?a=1&b=2&c=3")]
    public void ParseSpecialCharacters(String url, String expectedPathAndQuery)
    {
        var uri = new UriInfo(url);
        Assert.EndsWith(expectedPathAndQuery, uri.PathAndQuery);
    }

    [Fact]
    public void ParseEmptyPathWithQuery()
    {
        var uri = new UriInfo("http://localhost?query=value");
        Assert.Equal("http", uri.Scheme);
        Assert.Equal("localhost", uri.Host);
        Assert.Equal(0, uri.Port);
        Assert.Equal("/", uri.AbsolutePath);
        Assert.Equal("?query=value", uri.Query);
        Assert.Equal("/?query=value", uri.PathAndQuery);
    }

    [Fact]
    public void ParseComplexUrl()
    {
        var url = "https://user:pass@example.com:8443/api/v1/users?filter=active&sort=name#section";
        var uri = new UriInfo(url);

        // UriInfo 不处理认证信息和片段，但应正确解析其他部分
        Assert.Equal("https", uri.Scheme);
        Assert.True(uri.Host?.Contains("example.com") ?? false);
    }

    [Theory]
    [InlineData("ftp://ftp.example.com:21/files", "ftp", "ftp.example.com", 21)]
    public void ParseNonHttpSchemes(String url, String scheme, String host, Int32 port)
    {
        var uri = new UriInfo(url);
        Assert.Equal(scheme, uri.Scheme);
        Assert.Equal(host, uri.Host);
        Assert.Equal(port, uri.Port);
    }

    [Fact]
    public void AppendMultipleParameters()
    {
        var uri = new UriInfo("http://localhost/api");
        uri.Append("page", 1)
           .Append("size", 20)
           .Append("sort", "name");

        Assert.Equal("http://localhost/api?page=1&size=20&sort=name", uri.ToString());
    }

    [Fact]
    public void AppendNotEmptyMultipleParameters()
    {
        var uri = new UriInfo("http://localhost/api");
        uri.AppendNotEmpty("page", 1)
           .AppendNotEmpty("size", null)
           .AppendNotEmpty("sort", "")
           .AppendNotEmpty("filter", "active");

        Assert.Equal("http://localhost/api?page=1&filter=active", uri.ToString());
    }

    [Theory]
    [InlineData("http://EXAMPLE.COM/Path", "http", "EXAMPLE.COM")]
    [InlineData("HTTP://example.com/path", "HTTP", "example.com")]
    [InlineData("HtTp://ExAmPlE.CoM/path", "HtTp", "ExAmPlE.CoM")]
    public void ParsePreserveCase(String url, String scheme, String host)
    {
        var uri = new UriInfo(url);
        Assert.Equal(scheme, uri.Scheme);
        Assert.Equal(host, uri.Host);
    }

    [Fact]
    public void ToStringWithoutScheme()
    {
        var uri = new UriInfo
        {
            Host = "localhost",
            Port = 8080,
            AbsolutePath = "/api",
            Query = "?test=1"
        };

        Assert.Equal("localhost:8080/api?test=1", uri.ToString());
    }

    [Fact]
    public void ToStringOnlyPath()
    {
        var uri = new UriInfo
        {
            AbsolutePath = "/api/users",
            Query = "?filter=active"
        };

        Assert.Equal("/api/users?filter=active", uri.ToString());
    }

    [Theory]
    [InlineData("localhost:0/path", 0)]
    [InlineData("localhost:65535/path", 65535)]
    [InlineData("localhost:abc/path", 0)]
    public void ParsePortEdgeCases(String url, Int32 expectedPort)
    {
        var uri = new UriInfo(url);
        Assert.Equal(expectedPort, uri.Port);
    }

    [Theory]
    [InlineData("http://[::1]/path", "::1", 0, "/path")]
    [InlineData("http://[::1]:8080/api", "::1", 8080, "/api")]
    [InlineData("http://[::1]:8080/", "::1", 8080, "/")]
    [InlineData("http://[2001:db8::1]:443/api/v1", "2001:db8::1", 443, "/api/v1")]
    [InlineData("https://[fe80::1]/", "fe80::1", 0, "/")]
    public void ParseIPv6WithPath(String url, String expectedHost, Int32 expectedPort, String expectedPath)
    {
        var uri = new UriInfo(url);
        Assert.Equal(expectedHost, uri.Host);
        Assert.Equal(expectedPort, uri.Port);
        Assert.Equal(expectedPath, uri.AbsolutePath);
    }

    [Theory]
    [InlineData("http://[::1]?query=value", "::1", "?query=value")]
    [InlineData("http://[::1]:8080?a=1&b=2", "::1", "?a=1&b=2")]
    [InlineData("http://[2001:db8::1]/path?name=test", "2001:db8::1", "?name=test")]
    public void ParseIPv6WithQuery(String url, String expectedHost, String expectedQuery)
    {
        var uri = new UriInfo(url);
        Assert.Equal(expectedHost, uri.Host);
        Assert.Equal(expectedQuery, uri.Query);
    }

    [Theory]
    [InlineData("http://[::1]:80/path", "[::1]")]
    [InlineData("https://[::1]:443/path", "[::1]")]
    [InlineData("http://[::1]:8080/path", "[::1]:8080")]
    [InlineData("https://[fe80::1]:8443/path", "[fe80::1]:8443")]
    [InlineData("ws://[::1]:80/ws", "[::1]")]
    [InlineData("wss://[::1]:443/ws", "[::1]")]
    public void IPv6Authority(String url, String expectedAuthority)
    {
        var uri = new UriInfo(url);
        Assert.Equal(expectedAuthority, uri.Authority);
    }

    [Theory]
    [InlineData("http://[::1]/path", "http://[::1]/path")]
    [InlineData("http://[::1]:8080/path", "http://[::1]:8080/path")]
    [InlineData("https://[2001:db8::1]:443/api", "https://[2001:db8::1]/api")]
    [InlineData("http://[fe80::1]:80/", "http://[fe80::1]/")]
    public void IPv6ToString(String url, String expected)
    {
        var uri = new UriInfo(url);
        Assert.Equal(expected, uri.ToString());
    }

    [Theory]
    [InlineData("http://[/path", "")]
    [InlineData("http://[::1/path", "::1")]
    [InlineData("http://[:8080/path", ":8080")]
    public void ParseMalformedIPv6(String url, String expectedHost)
    {
        var uri = new UriInfo(url);
        Assert.Equal(expectedHost, uri.Host);
    }

    [Fact]
    public void ParseIPv6WithoutBrackets()
    {
        // 没有方括号的IPv6会被当作普通主机名，最后一个冒号后面会被当作端口
        var uri = new UriInfo("http://2001:db8::1:8080/path");
        Assert.Equal("2001:db8::1", uri.Host);
        Assert.Equal(8080, uri.Port);
    }

    [Fact]
    public void AppendToIPv6Url()
    {
        var uri = new UriInfo("http://[::1]:8080/api");
        uri.Append("key", "value")
           .Append("id", 123);
        
        Assert.Equal("http://[::1]:8080/api?key=value&id=123", uri.ToString());
    }

    [Theory]
    [InlineData("[::1]:8080/path", "::1", 8080)]
    [InlineData("[2001:db8::1]/path", "2001:db8::1", 0)]
    [InlineData("[::1]", "::1", 0)]
    public void ParseIPv6WithoutScheme(String url, String expectedHost, Int32 expectedPort)
    {
        var uri = new UriInfo(url);
        Assert.Equal(expectedHost, uri.Host);
        Assert.Equal(expectedPort, uri.Port);
    }

    [Theory]
    [InlineData("http://localhost:8080/cube/info", "http://localhost:8080/cube/info")]
    [InlineData("https://example.com/api/v1", "https://example.com/api/v1")]
    [InlineData("http://localhost/path", "http://localhost/path")]
    [InlineData("https://example.com:8443/api", "https://example.com:8443/api")]
    [InlineData("http://192.168.1.1:8080/path", "http://192.168.1.1:8080/path")]
    [InlineData("http://[::1]:8080/path", "http://[::1]:8080/path")]
    [InlineData("https://[2001:db8::1]/api", "https://[2001:db8::1]/api")]
    [InlineData("http://localhost:8080/path?query=1", "http://localhost:8080/path?query=1")]
    [InlineData("https://example.com/path?a=1&b=2", "https://example.com/path?a=1&b=2")]
    public void ToUri_Success(String url, String expected)
    {
        var uriInfo = new UriInfo(url);
        var uri = uriInfo.ToUri();

        Assert.NotNull(uri);
        Assert.Equal(expected, uri.ToString());
        Assert.Equal(uriInfo.Scheme, uri.Scheme);
        // 注意：标准 Uri 的 Host 属性对 IPv6 会保留方括号，而 UriInfo 去掉了方括号
        if (uriInfo.Host.Contains(':'))
        {
            // IPv6 地址：标准 Uri 的 Host 包含方括号
            Assert.Equal($"[{uriInfo.Host}]", uri.Host);
        }
        else
        {
            Assert.Equal(uriInfo.Host, uri.Host);
        }
        Assert.Equal(uriInfo.Port == 0 ? (uriInfo.Scheme.EqualIgnoreCase("http", "ws") ? 80 : 443) : uriInfo.Port, uri.Port);
        Assert.Equal(uriInfo.AbsolutePath, uri.AbsolutePath);
    }

    [Theory]
    [InlineData("/path/to/resource")]
    [InlineData("localhost:8080/path")]
    [InlineData("localhost/path")]
    public void ToUri_NoScheme_ReturnsNull(String url)
    {
        var uriInfo = new UriInfo(url);
        var uri = uriInfo.ToUri();

        Assert.Null(uri);
    }

    [Fact]
    public void ToUri_NoHost_ReturnsNull()
    {
        var uriInfo = new UriInfo
        {
            Scheme = "http",
            AbsolutePath = "/path"
        };
        var uri = uriInfo.ToUri();

        Assert.Null(uri);
    }

    [Fact]
    public void ToUri_WithQueryParameters()
    {
        var uriInfo = new UriInfo("http://localhost/api");
        uriInfo.Append("page", 1)
               .Append("size", 20)
               .Append("name", "test");

        var uri = uriInfo.ToUri();
        
        Assert.NotNull(uri);
        Assert.Equal("http://localhost/api?page=1&size=20&name=test", uri.ToString());
        Assert.Equal("?page=1&size=20&name=test", uri.Query);
    }

    [Theory]
    [InlineData("http://localhost:80/path", "http://localhost/path")]
    [InlineData("https://localhost:443/path", "https://localhost/path")]
    [InlineData("http://localhost:8080/path", "http://localhost:8080/path")]
    public void ToUri_DefaultPort(String url, String expected)
    {
        var uriInfo = new UriInfo(url);
        var uri = uriInfo.ToUri();

        Assert.NotNull(uri);
        Assert.Equal(expected, uri.ToString());
    }

    [Fact]
    public void ToUri_IPv6Address()
    {
        var uriInfo = new UriInfo("http://[::1]:8080/api");
        var uri = uriInfo.ToUri();

        Assert.NotNull(uri);
        Assert.Equal("http://[::1]:8080/api", uri.ToString());
        // 标准 Uri 的 Host 属性对 IPv6 保留方括号
        Assert.Equal("[::1]", uri.Host);
        Assert.Equal(8080, uri.Port);
    }

    [Fact]
    public void ToUri_ConsistentWithSystemUri()
    {
        var testUrls = new[]
        {
            "http://localhost:8080/path",
            "https://example.com/api/v1",
            "http://192.168.1.1:3000/test",
            "https://[::1]:8443/ws",
            "http://example.com/path?query=value&id=123"
        };

        foreach (var url in testUrls)
        {
            var uriInfo = new UriInfo(url);
            var uriFromInfo = uriInfo.ToUri();
            var systemUri = new Uri(url);

            Assert.NotNull(uriFromInfo);
            Assert.Equal(systemUri.ToString(), uriFromInfo.ToString());
            Assert.Equal(systemUri.Scheme, uriFromInfo.Scheme);
            Assert.Equal(systemUri.Host, uriFromInfo.Host);
            Assert.Equal(systemUri.Port, uriFromInfo.Port);
            Assert.Equal(systemUri.AbsolutePath, uriFromInfo.AbsolutePath);
            Assert.Equal(systemUri.PathAndQuery, uriFromInfo.PathAndQuery);
        }
    }
}

