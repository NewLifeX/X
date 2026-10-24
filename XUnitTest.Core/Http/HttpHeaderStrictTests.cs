using System.ComponentModel;
using NewLife;
using NewLife.Data;
using NewLife.Http;
using Xunit;

namespace XUnitTest.Http;

/// <summary>HTTP 头部行的严格校验测试（请求拒绝、响应宽容）</summary>
/// <remarks>
/// <para>RFC 7230 §3.2.4：请求中出现「字段名与冒号之间的空白」或「以空白开头的续行折叠（obs-fold）」，
/// 服务端必须拒绝或替换为空格；而对响应，客户端必须替换、不得拒绝。</para>
/// <para>若不拒绝请求侧的畸形行，本端与前置代理会对同一段字节得出不同长度理解，构成请求走私面：
/// 代理按 OWS（含制表符）剥去空白后认出 <c>Content-Length</c>，本端却把它当成未知头部。</para>
/// </remarks>
public class HttpHeaderStrictTests
{
    [Fact]
    [DisplayName("HTTP请求_字段名含制表符_解析失败")]
    public void Request_TabBeforeColon_Rejected()
    {
        // 字段名与冒号之间的制表符：Trim(' ') 去不掉它，旧实现把名字解析成「Content-Length\t」而认不出长度头
        var req = new HttpRequest();
        var data = "POST /x HTTP/1.1\r\nHost: a\r\nContent-Length\t: 5\r\n\r\nHELLO".GetBytes();

        Assert.False(req.Parse(new ArrayPacket(data)));
    }

    [Fact]
    [DisplayName("HTTP请求_字段名后有空格再接冒号_解析失败")]
    public void Request_SpaceBeforeColon_Rejected()
    {
        // 字段名与冒号之间的空格会被 Trim(' ') 抹掉，解析结果与前置代理「拒绝该请求」的理解不一致
        var req = new HttpRequest();
        var data = "POST /x HTTP/1.1\r\nHost: a\r\nContent-Length : 5\r\n\r\nHELLO".GetBytes();

        Assert.False(req.Parse(new ArrayPacket(data)));
    }

    [Fact]
    [DisplayName("HTTP请求_头部续行折叠_解析失败")]
    public void Request_ObsFold_Rejected()
    {
        // 续行折叠：Trim 前导空格后名字正好命中 Content-Length，本端会据此等待实体，
        // 而前置代理把该行视为上一头部的续行（不产生长度头）
        var req = new HttpRequest();
        var data = "GET /x HTTP/1.1\r\nHost: a\r\n Content-Length: 5\r\n\r\n".GetBytes();

        Assert.False(req.Parse(new ArrayPacket(data)));
    }

    [Fact]
    [DisplayName("HTTP请求_冒号后制表符_仍按长度头识别")]
    public void Request_TabAfterColon_Accepted()
    {
        // 冒号之后的空白属于 RFC 允许的 OWS（SP/HTAB），不得误伤：值侧去空白后应正常识别长度
        var req = new HttpRequest();
        var data = "POST /x HTTP/1.1\r\nHost: a\r\nContent-Length:\t5\r\n\r\nHELLO".GetBytes();

        Assert.True(req.Parse(new ArrayPacket(data)));
        Assert.Equal(5, req.ContentLength);
        Assert.False(req.InvalidContentLength);
        Assert.Equal("HELLO", req.Body.ToStr());
    }

    [Fact]
    [DisplayName("HTTP请求_常规头部_正常解析")]
    public void Request_NormalHeader_Parsed()
    {
        var req = new HttpRequest();
        var data = "POST /x HTTP/1.1\r\nHost: example.com\r\nContent-Type: text/plain\r\nContent-Length: 3\r\n\r\nabc".GetBytes();

        Assert.True(req.Parse(new ArrayPacket(data)));
        Assert.Equal("POST", req.Method);
        Assert.Equal("example.com", req.Headers["Host"]);
        Assert.Equal("text/plain", req.ContentType);
        Assert.Equal(3, req.ContentLength);
        Assert.Equal("abc", req.Body.ToStr());
    }

    [Fact]
    [DisplayName("HTTP响应_同类畸形行_保持宽容不影响客户端解析")]
    public void Response_MalformedHeader_Tolerated()
    {
        // 响应侧不强制拒绝：RFC 要求客户端替换而非拒绝，避免解析不了历史服务器
        var res = new HttpResponse();
        var data = "HTTP/1.1 200 OK\r\nContent-Length: 0\r\nX-a: 1\r\n X-b: 2\r\n\r\n".GetBytes();

        Assert.True(res.Parse(new ArrayPacket(data)));
        Assert.Equal("1", res.Headers["X-a"]);
    }
}
