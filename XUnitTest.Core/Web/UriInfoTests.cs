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
    [InlineData("Http://localhost", "Http", "localhost", 0, null)]
    [InlineData("https://localhost:8080/cube/info", "https", "localhost", 8080, "/cube/info")]
    [InlineData("https://localhost:8080/", "https", "localhost", 8080, "/")]
    [InlineData("Https://localhost/", "Https", "localhost", 0, "/")]
    [InlineData("Https://localhost", "Https", "localhost", 0, null)]
    [InlineData("wss://localhost:8080/cube/info", "wss", "localhost", 8080, "/cube/info")]
    [InlineData("wss://localhost:8080/", "wss", "localhost", 8080, "/")]
    [InlineData("wss://localhost/", "wss", "localhost", 0, "/")]
    [InlineData("wss://localhost", "wss", "localhost", 0, null)]
    [InlineData("localhost:8080/cube/info", null, "localhost", 8080, "/cube/info")]
    [InlineData("localhost:8080/", null, "localhost", 8080, "/")]
    [InlineData("localhost/", null, "localhost", 0, "/")]
    [InlineData("localhost", null, "localhost", 0, null)]
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
                Assert.Equal($"{host}", uri.Authority);
                Assert.Equal($"{schema}://{host}{path}", uri.ToString());
            }
            else
            {
                Assert.Equal($"{host}:{port}", uri.Authority);
                Assert.Equal($"{schema}://{host}:{port}{path}", uri.ToString());
            }
        }
        {
            if (!url.StartsWithIgnoreCase("http://", "https://", "ws://", "wss://"))
                url = "http://" + url;
            if (schema.IsNullOrEmpty()) schema = "http";
            if (path.IsNullOrEmpty()) path = "/";
            schema = schema?.ToLower();

            if (port == 0 && schema.EqualIgnoreCase("http", "ws"))
                port = 80;
            else if (port == 0 && schema.EqualIgnoreCase("https", "wss"))
                port = 443;

            var uri = new Uri(url);
            Assert.Equal(schema, uri.Scheme);
            Assert.Equal(host, uri.Host);
            Assert.Equal(port, uri.Port);
            Assert.Equal(path, uri.PathAndQuery);

            if (port == 0 ||
                port == 80 && schema.EqualIgnoreCase("http", "ws") ||
                port == 443 && schema.EqualIgnoreCase("https", "wss"))
            {
                Assert.Equal($"{host}", uri.Authority);
                Assert.Equal($"{schema}://{host}{path}", uri.ToString());
            }
            else
            {
                Assert.Equal($"{host}:{port}", uri.Authority);
                Assert.Equal($"{schema}://{host}:{port}{path}", uri.ToString());
            }
        }
    }
}
