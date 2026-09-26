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
    public void ContentLength_ParsedOrMarkedInvalid(String raw, Int32 expectLength, Boolean expectInvalid)
    {
        var req = new HttpRequest();
        Assert.True(req.Parse(new ArrayPacket(raw.GetBytes())));

        // 非法值不能退化成“无体”（ContentLength=-1 且未标记），否则请求边界失步
        Assert.Equal(expectLength, req.ContentLength);
        Assert.Equal(expectInvalid, req.InvalidContentLength);
    }
}
