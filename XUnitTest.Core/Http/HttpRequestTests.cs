using NewLife;
using NewLife.Data;
using NewLife.Http;
using Xunit;

namespace XUnitTest.Http;

/// <summary>HttpRequest 解析与构建测试</summary>
public class HttpRequestTests
{
    [Fact]
    public void Parse_Request_Get_WithHeadersAndBody()
    {
        var body = "name=Stone&age=30".GetBytes();
        var header = """
            GET /api/user?id=123 HTTP/1.1
            Host: example.com
            Connection: keep-alive
            Content-Length: 
            """ + body.Length + """

            Content-Type: application/x-www-form-urlencoded
            Custom: Value-1


            """;
        var raw = header.GetBytes();
        var pk = new ArrayPacket(raw) { Next = (ArrayPacket)body };

        var req = new HttpRequest();
        var ok = req.Parse(pk);
        Assert.True(ok);
        Assert.Equal("1.1", req.Version);
        Assert.Equal("GET", req.Method);
        Assert.Equal("/api/user?id=123", req.RequestUri + "");
        Assert.Equal("example.com", req.Host);
        Assert.True(req.KeepAlive);
        Assert.Equal(body.Length, req.ContentLength);
        Assert.Equal("application/x-www-form-urlencoded", req.ContentType);
        Assert.Equal(body.Length, req.BodyLength);
        Assert.True(req.IsCompleted);
        Assert.Equal("Value-1", req.Headers["Custom"]);
    }

    [Fact]
    public void Parse_Request_Post_NoContentLength()
    {
        var header = """
            POST /submit HTTP/1.0
            Host: test.com
            Connection: keep-alive
            Content-Type: text/plain


            """;
        var body = "Hello".GetBytes();
        var pk = new ArrayPacket(header.GetBytes()) { Next = (ArrayPacket)body };

        var req = new HttpRequest();
        var ok = req.Parse(pk);
        Assert.True(ok);
        Assert.Equal("1.0", req.Version);
        Assert.Equal("POST", req.Method);
        Assert.True(req.KeepAlive); // HTTP/1.0 keep-alive 由头部指定
        Assert.Equal(-1, req.ContentLength); // 未提供 Content-Length
        Assert.Equal(body.Length, req.BodyLength);
        Assert.True(req.IsCompleted); // 未指定长度视为完成
    }

    [Fact]
    public void FastParse_Request_OnlyFirstLine()
    {
        var data = "GET /ping HTTP/1.1\r\n".GetBytes();
        var pk = new ArrayPacket(data);
        var req = new HttpRequest();
        var ok = req.FastParse(pk);
        Assert.True(ok);
        Assert.Equal("GET", req.Method);
        Assert.Equal("/ping", req.RequestUri + "");
        Assert.Equal("1.1", req.Version);
        Assert.Null(req.Headers["Host"]);
    }

    [Fact(DisplayName = "Parse 链式跨节点头部：头部横跨多个节点仍可解析")]
    public void Parse_Chained_SplitHeader()
    {
        var raw = "POST /api/user HTTP/1.1\r\nHost: example.com\r\nContent-Length: 7\r\nContent-Type: text/plain\r\n\r\npayload".GetBytes();

        // 头部切到 3 个节点上（跨接收段组链）
        IPacket pk = new ArrayPacket(raw[..5]);
        pk.Append(new ArrayPacket(raw[5..30]));
        pk.Append(new ArrayPacket(raw[30..]));

        var req = new HttpRequest();
        Assert.True(req.Parse(pk));
        Assert.Equal("POST", req.Method);
        Assert.Equal("/api/user", req.RequestUri + "");
        Assert.Equal("example.com", req.Host);
        Assert.Equal(7, req.ContentLength);
        Assert.Equal("payload", req.Body?.ToStr());
    }

    [Fact(DisplayName = "FastParse 链式跨节点头部：首行横跨节点仍可解析")]
    public void FastParse_Chained_SplitFirstLine()
    {
        var raw = "GET /very/long/path HTTP/1.1\r\nHost: a\r\n\r\n".GetBytes();

        // 首行切到 3 个节点上
        IPacket pk = new ArrayPacket(raw[..7]);
        pk.Append(new ArrayPacket(raw[7..15]));
        pk.Append(new ArrayPacket(raw[15..]));

        var req = new HttpRequest();
        Assert.True(req.FastParse(pk));
        Assert.Equal("GET", req.Method);
        Assert.Equal("/very/long/path", req.RequestUri + "");
        Assert.Equal("1.1", req.Version);
    }

    [Fact]
    public void Build_Request_AutoMethodAndHost()
    {
        var req = new HttpRequest
        {
            RequestUri = new Uri("http://example.com:80/api/info?x=1"),
            KeepAlive = true,
            ContentType = "application/json",
            Body = (ArrayPacket)"{\"name\":\"Stone\"}".GetBytes()
        };
        var pk = req.Build();
        var text = pk.ToStr();
        Assert.StartsWith("POST /api/info?x=1 HTTP/1.1\r\n", text); // 有主体自动 POST
        Assert.Contains("Host: example.com\r\n", text);
        Assert.Contains("Content-Length: 16\r\n", text);
        Assert.Contains("Content-Type: application/json\r\n", text);
        Assert.Contains("Connection: keep-alive\r\n", text);
        Assert.EndsWith("\r\n{\"name\":\"Stone\"}", text);
    }

    [Fact]
    public void Parse_InvalidHeader_ReturnsFalse()
    {
        var raw = """
            INVALID_HEADER
            Key:Value


            """.GetBytes();
        var pk = new ArrayPacket(raw);
        var req = new HttpRequest();
        var ok = req.Parse(pk);
        Assert.False(ok);
    }
}
