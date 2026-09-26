using System.ComponentModel;
using NewLife.Http;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Http;

/// <summary>WebSocket 服务端握手校验测试。校验不通过必须返回 null，否则连接会被静默接管</summary>
public class WebSocketTests
{
    private static DefaultHttpContext NewContext()
    {
        // 两个构造重载只差首个参数类型，显式声明类型消歧；握手校验不依赖会话
        INetSession session = null!;
        var ctx = new DefaultHttpContext(session, new HttpRequest(), "/ws", null);
        ctx.Request.Headers["Sec-WebSocket-Key"] = "dGhlIHNhbXBsZSBub25jZQ==";

        return ctx;
    }

    [Fact]
    [DisplayName("WS握手_四要素齐全_返回101并建立连接")]
    public void Handshake_ValidUpgrade_ReturnsInstance()
    {
        var ctx = NewContext();
        ctx.Request.Headers["Upgrade"] = "websocket";
        ctx.Request.Headers["Connection"] = "keep-alive, Upgrade";
        ctx.Request.Headers["Sec-WebSocket-Version"] = "13";

        var ws = WebSocket.Handshake(ctx);

        Assert.NotNull(ws);
        Assert.Equal(101, (Int32)ctx.Response.StatusCode);
        Assert.True(ws!.Connected);
    }

    [Fact]
    [DisplayName("WS握手_缺少Upgrade头_返回空不接管连接")]
    public void Handshake_MissingUpgrade_ReturnsNull()
    {
        // 只有 Sec-WebSocket-Key 的普通请求不得升级：否则连接会被静默当 WebSocket 接管，
        // 客户端收不到任何握手响应，且该连接永远脱离 HTTP 解析
        var ctx = NewContext();

        Assert.Null(WebSocket.Handshake(ctx));
    }

    [Fact]
    [DisplayName("WS握手_版本非13_返回空不接管连接")]
    public void Handshake_WrongVersion_ReturnsNull()
    {
        var ctx = NewContext();
        ctx.Request.Headers["Upgrade"] = "websocket";
        ctx.Request.Headers["Connection"] = "Upgrade";
        ctx.Request.Headers["Sec-WebSocket-Version"] = "8";

        Assert.Null(WebSocket.Handshake(ctx));
    }
}
