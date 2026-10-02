using NewLife;
using NewLife.Data;
using NewLife.Http;
using Xunit;

namespace XUnitTest.Http;

/// <summary>HttpBase 公共逻辑测试</summary>
public class HttpBaseTests
{
    [Theory]
    [InlineData("GET / ")]
    [InlineData("HTTP/1.1 200")]
    [InlineData("POST /api ")]
    public void FastValidHeader_Positive(String first)
    {
        var data = first.GetBytes();
        Assert.True(HttpBase.FastValidHeader(data));
    }

    [Theory(DisplayName = "Content-Length_合法则解析或标记非法")]
    [InlineData("POST / HTTP/1.1\r\nContent-Length: 5\r\n\r\nhello", 5, false)]
    [InlineData("POST / HTTP/1.1\r\nContent-Length: 0\r\n\r\n", 0, false)]
    [InlineData("POST / HTTP/1.1\r\nHost: a\r\n\r\n", -1, false)]
    [InlineData("POST / HTTP/1.1\r\nContent-Length: -1\r\n\r\n", -1, true)]
    [InlineData("POST / HTTP/1.1\r\nContent-Length: abc\r\n\r\n", -1, true)]
    [InlineData("POST / HTTP/1.1\r\nContent-Length: 9999999999\r\n\r\n", -1, true)]
    // 重复（含大小写不同）的 Content-Length 会与前置代理的取值理解不一致，构成 CL.CL 走私面，同样判非法
    [InlineData("POST / HTTP/1.1\r\nContent-Length: 5\r\nContent-Length: 5\r\n\r\nhello", -1, true)]
    [InlineData("POST / HTTP/1.1\r\nContent-Length: 5\r\ncontent-length: 3\r\n\r\nhello", -1, true)]
    public void ContentLength_ParsedOrMarkedInvalid(String raw, Int32 expectLength, Boolean expectInvalid)
    {
        var req = new HttpRequest();
        Assert.True(req.Parse(new ArrayPacket(raw.GetBytes())));

        // 非法值不能退化成“无体”（ContentLength=-1 且未标记），否则请求边界失步
        Assert.Equal(expectLength, req.ContentLength);
        Assert.Equal(expectInvalid, req.InvalidContentLength);
    }

    [Fact(DisplayName = "请求同实例二次解析_上一条头部不残留")]
    public void Reparse_SameInstance_ClearsStaleHeaders()
    {
        // 残留判据比缺省值更危险：上一条的 Transfer-Encoding/Content-Length 会让本条按错误的方式分帧
        var req = new HttpRequest();
        var first = "POST /a HTTP/1.1\r\nHost: a\r\nTransfer-Encoding: gzip\r\nContent-Length: 5\r\nX-Old: 1\r\n\r\nhello";
        Assert.True(req.Parse(new ArrayPacket(first.GetBytes())));
        Assert.Equal("gzip", req.Headers["Transfer-Encoding"]);

        Assert.True(req.Parse(new ArrayPacket("GET /b HTTP/1.1\r\nHost: b\r\n\r\n".GetBytes())));

        Assert.Null(req.Headers["Transfer-Encoding"]);
        Assert.Null(req.Headers["Content-Length"]);
        Assert.Null(req.Headers["X-Old"]);
        Assert.Equal("b", req.Headers["Host"]);
        Assert.Equal(-1, req.ContentLength);
    }

    [Fact(DisplayName = "响应同实例二次解析_上一条头部不残留")]
    public void ResponseReparse_SameInstance_ClearsStaleHeaders()
    {
        // 客户端据响应头部分帧（Transfer-Encoding 优先于 Content-Length），残留会让下一条响应被错误解码
        var res = new HttpResponse();
        var first = "HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked\r\nContent-Length: 5\r\n\r\nhello";
        Assert.True(res.Parse(new ArrayPacket(first.GetBytes())));
        Assert.Equal("chunked", res.Headers["Transfer-Encoding"]);

        Assert.True(res.Parse(new ArrayPacket("HTTP/1.1 204 No Content\r\n\r\n".GetBytes())));

        Assert.Null(res.Headers["Transfer-Encoding"]);
        Assert.Null(res.Headers["Content-Length"]);
    }

    [Fact(DisplayName = "FastParse只分析首行_不残留上一条头部")]
    public void FastParse_ClearsStaleHeaders()
    {
        // FastParse 不解析头部，故也不应残留：否则 Host 会沿用上一条请求的值
        var req = new HttpRequest();
        Assert.True(req.Parse(new ArrayPacket("GET /a HTTP/1.1\r\nHost: a\r\n\r\n".GetBytes())));
        Assert.Equal("a", req.Host);

        Assert.True(req.FastParse(new ArrayPacket("GET /b HTTP/1.1\r\n\r\n".GetBytes())));

        Assert.Null(req.Headers["Host"]);
        Assert.Null(req.Host);
    }

    [Fact(DisplayName = "同实例二次解析_上一条主体句柄已归还")]
    public void Reparse_SameInstance_ReleasesStaleBody()
    {
        // 旧主体是上一次解析的拥有句柄（源包共享切片）：覆盖而不归还，其池缓冲引用计数永不归零
        var bytes = "POST /a HTTP/1.1\r\nHost: a\r\nContent-Length: 3\r\n\r\nabc".GetBytes();
        using var src = new OwnerPacket(bytes.Length);
        bytes.CopyTo(src.GetSpan());

        var req = new HttpRequest();
        Assert.True(req.Parse(src));

        var body = (OwnerPacket)req.Body!;
        Assert.Equal("abc", body.ToStr());
        Assert.Equal(2, src.RefCount);   // 源句柄 + 主体切片

        Assert.True(req.Parse(src));

        // 旧主体已释放（Dispose 置空 _owner，RefCount 读作 0 哨兵值）；源句柄 + 新主体仍为 2，
        // 既证明旧句柄已归还，也证明没有过度释放
        Assert.Equal(0, body.RefCount);
        Assert.Equal(2, src.RefCount);
    }
}
