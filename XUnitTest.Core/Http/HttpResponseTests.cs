using System.Net;
using NewLife;
using NewLife.Data;
using NewLife.Http;
using NewLife.Remoting;
using Xunit;

namespace XUnitTest.Http;

/// <summary>HttpResponse 解析与构建测试</summary>
public class HttpResponseTests
{
    [Fact]
    public void Parse_Response_Ok()
    {
        var body = "OK".GetBytes();
        var header = """
            HTTP/1.1 200 OK
            Content-Length: 2
            Content-Type: text/plain


            """;
        var pk = new ArrayPacket(header.GetBytes()) { Next = (ArrayPacket)body };
        var resp = new HttpResponse();
        var ok = resp.Parse(pk);
        Assert.True(ok);
        Assert.Equal("1.1", resp.Version);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("OK", resp.StatusDescription);
        Assert.Equal(2, resp.ContentLength);
        Assert.Equal("text/plain", resp.ContentType);
        Assert.Equal(2, resp.BodyLength);
        Assert.True(resp.IsCompleted);
    }

    [Fact]
    public void Parse_Response_ErrorWithoutBody_UsesDescriptionAsBodyOnBuild()
    {
        var resp = new HttpResponse
        {
            StatusCode = HttpStatusCode.BadRequest,
            StatusDescription = "Bad Request",
        };
        var pk = resp.Build();
        var text = pk.GetSpan().ToStr();
        Assert.Contains("HTTP/1.1 400 Bad Request\r\n", text);
        Assert.Contains("Content-Length: 11\r\n", text); // Body 自动使用描述
        Assert.EndsWith("\r\nBad Request", text);
    }

    [Fact]
    public void Build_Response_SetsContentLengthZeroWhenNoBody()
    {
        var resp = new HttpResponse();
        var pk = resp.Build();
        var text = pk.ToStr();
        Assert.Contains("HTTP/1.1 200 OK\r\n", text);
        Assert.Contains("Content-Length: 0\r\n", text);
    }

    [Fact]
    public void SetResult_VariousTypes()
    {
        var resp1 = new HttpResponse();
        resp1.SetResult("hello");
        Assert.Equal("text/html", resp1.ContentType);
        Assert.Equal("hello", resp1.Body.ToStr());

        var resp2 = new HttpResponse();
        resp2.SetResult(new { name = "Stone" });
        Assert.Equal("application/json", resp2.ContentType);
        Assert.Contains("\"name\":\"Stone\"", resp2.Body.ToStr());

        var resp3 = new HttpResponse();
        var bytes = new Byte[] { 1, 2, 3 };
        resp3.SetResult(bytes);
        Assert.Equal("application/octet-stream", resp3.ContentType);
        Assert.Equal(bytes, resp3.Body.ReadBytes());

        var resp4 = new HttpResponse();
        var ex = new ApiException(500, "failed");
        resp4.SetResult(ex);
        Assert.Equal(HttpStatusCode.InternalServerError, resp4.StatusCode); // ApiException.Code 500 cast to HttpStatusCode
        Assert.Equal("failed", resp4.StatusDescription);
    }

    [Fact]
    public void Valid_ThrowsOnNonOk()
    {
        var resp = new HttpResponse { StatusCode = HttpStatusCode.NotFound, StatusDescription = "missing" };
        var ex = Assert.Throws<Exception>(() => resp.Valid());
        Assert.Equal("missing", ex.Message);
    }

    [Fact]
    public void Response_Parse_InvalidFirstLine()
    {
        var raw = """
            NOTHTTP 400 ERR
            Content-Length:0


            """.GetBytes();
        var pk = new ArrayPacket(raw);
        var resp = new HttpResponse();
        var ok = resp.Parse(pk);
        Assert.False(ok);
    }
}
