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

    [Fact(DisplayName = "Build 链式跨段体：响应体全链写入")]
    public void Build_Response_ChainedBody()
    {
        IPacket chain = new ArrayPacket("hello ".GetBytes());
        chain.Append(new ArrayPacket("world".GetBytes()));

        var resp = new HttpResponse { Body = chain };
        var pk = resp.Build();
        var text = pk.ToStr();
        Assert.Contains("Content-Length: 11\r\n", text);
        Assert.EndsWith("hello world", text);
    }

    [Theory(DisplayName = "Build 1xx/204/304 无实体响应：不声明 Content-Length")]
    [InlineData(HttpStatusCode.Continue)]
    [InlineData(HttpStatusCode.SwitchingProtocols)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.NotModified)]
    public void Build_NoEntityStatus_OmitsContentLength(HttpStatusCode code)
    {
        var resp = new HttpResponse { StatusCode = code };
        var pk = resp.Build();
        var text = pk.ToStr();

        // RFC 7230 §3.3.2：无实体状态码不得带 Content-Length
        Assert.DoesNotContain("Content-Length", text);
        Assert.StartsWith($"HTTP/1.1 {(Int32)code} ", text);
    }

    [Fact(DisplayName = "Build 204带主体：实体不写入报文")]
    public void Build_NoEntityStatus_IgnoresBody()
    {
        // 无实体状态码即使被赋了 Body 也不得把实体写进报文：头部省略了 Content-Length，
        // 实体字节会让头部与实体自相矛盾（严格解析的客户端会把这些字节当作下一个响应的开头）
        var resp = new HttpResponse
        {
            StatusCode = HttpStatusCode.NoContent,
            Body = new ArrayPacket("ignored".GetBytes()),
        };

        using var pk = resp.Build();
        var text = pk.ToStr();

        Assert.DoesNotContain("ignored", text);
        Assert.DoesNotContain("Content-Length", text);
        Assert.EndsWith("\r\n\r\n", text);
    }

    [Theory(DisplayName = "Build 204/304带状态描述：不把描述当实体")]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.NotModified)]
    public void Build_NoEntityStatus_DoesNotUseStatusDescriptionAsBody(HttpStatusCode code)
    {
        // 非成功状态码会用状态描述补齐实体，但 204/304 不得携带实体
        var resp = new HttpResponse { StatusCode = code, StatusDescription = "Not Modified" };

        using var pk = resp.Build();
        var text = pk.ToStr();

        Assert.StartsWith($"HTTP/1.1 {(Int32)code} Not Modified\r\n", text);
        Assert.EndsWith("\r\n\r\n", text);
        Assert.DoesNotContain("Content-Length", text);
    }

    [Fact(DisplayName = "Build 200 无体响应：仍声明 Content-Length 0")]
    public void Build_OkWithoutBody_KeepsZeroContentLength()
    {
        // 回归护栏：204/304 的特例不得泛化到普通状态码
        var resp = new HttpResponse();
        var pk = resp.Build();
        var text = pk.ToStr();

        Assert.Contains("Content-Length: 0\r\n", text);
    }

    [Fact(DisplayName = "Build 1xx响应：状态描述不当作实体，报文只剩头部")]
    public void Build_InformationalStatus_UsesHeaderOnly()
    {
        // 非成功状态码会用状态描述补齐实体，但 1xx 属无实体响应（RFC 7230 §3.3.2）
        var resp = new HttpResponse { StatusCode = HttpStatusCode.Continue, StatusDescription = "Continue" };

        using var pk = resp.Build();

        Assert.Equal("HTTP/1.1 100 Continue\r\n\r\n", pk.ToStr());
    }

    [Theory(DisplayName = "BuildHeaderPacket 1xx/204/304 响应：不声明 Content-Length")]
    [InlineData(HttpStatusCode.Continue)]
    [InlineData(HttpStatusCode.SwitchingProtocols)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.NotModified)]
    public void BuildHeaderPacket_NoEntityStatus_OmitsContentLength(HttpStatusCode code)
    {
        // HEAD 与流式响应走 BuildHeaderPacket 而非 Build，两条路径必须同规则
        var resp = new HttpResponse { StatusCode = code };

        using var pk = resp.BuildHeaderPacket(resp.Body?.Total ?? 0);
        var text = pk.ToStr();

        Assert.DoesNotContain("Content-Length", text);
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

    [Fact(DisplayName = "流式响应_整包构建_物化流并释放")]
    public void BodyStream_Build_Materializes()
    {
        var ms = new MemoryStream("stream-body".GetBytes());
        var resp = new HttpResponse { BodyStream = ms };

        using var pk = resp.Build();
        var text = pk.ToStr();
        Assert.Contains("Content-Length: 11\r\n", text);
        Assert.EndsWith("stream-body", text);

        // 物化后流已释放、BodyStream 清空；二次构建可重复
        Assert.Null(resp.BodyStream);
        Assert.Throws<ObjectDisposedException>(() => ms.ReadByte());

        using var pk2 = resp.Build();
        Assert.Equal(pk.Total, pk2.Total);
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
