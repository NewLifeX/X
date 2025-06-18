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
}
