using NewLife.Http;
using Xunit;

namespace XUnitTest.Http;

/// <summary>参数化路由器测试：{param} / {param?} / {*path} 的段对齐与参数提取</summary>
public class HttpRouterTests
{
    private sealed class StubHandler : IHttpHandler
    {
        public void ProcessRequest(IHttpContext context) { }
    }

    [Fact(DisplayName = "路由器_{id}参数段_命中并提取参数")]
    public void ParameterizedRoute_Matches()
    {
        var handler = new StubHandler();
        var router = new HttpRouter();
        router.Register("/api/users/{id}", handler);

        var ps = new Dictionary<String, Object?>();
        Assert.Same(handler, router.Match("/api/users/123", ps));
        Assert.Equal("123", ps["id"]);
    }

    [Fact(DisplayName = "路由器_多参数段_{controller}{action}_逐段提取")]
    public void MultipleParameters_Extracted()
    {
        var router = new HttpRouter();
        router.Register("/api/{controller}/{action}", new StubHandler());

        var ps = new Dictionary<String, Object?>();
        Assert.NotNull(router.Match("/api/user/info", ps));
        Assert.Equal("user", ps["controller"]);
        Assert.Equal("info", ps["action"]);
    }

    [Fact(DisplayName = "路由器_{id?}可选参数段_省略时命中且参数为空")]
    public void OptionalRoute_MatchesWithoutSegment()
    {
        var router = new HttpRouter();
        router.Register("/api/users/{id?}", new StubHandler());

        var ps = new Dictionary<String, Object?>();
        Assert.NotNull(router.Match("/api/users", ps));
        Assert.Null(ps["id"]);

        Assert.NotNull(router.Match("/api/users/5", ps));
        Assert.Equal("5", ps["id"]);
    }

    [Fact(DisplayName = "路由器_{*path}通配段_捕获剩余多段路径")]
    public void WildcardRoute_CapturesRemainingSegments()
    {
        var router = new HttpRouter();
        router.Register("/api/files/{*path}", new StubHandler());

        var ps = new Dictionary<String, Object?>();
        Assert.NotNull(router.Match("/api/files/a/b/c.txt", ps));
        Assert.Equal("a/b/c.txt", ps["path"]);
    }

    [Fact(DisplayName = "路由器_字面段不同或段数不符_不命中")]
    public void LiteralMismatch_ReturnsNull()
    {
        var router = new HttpRouter();
        router.Register("/api/users/{id}", new StubHandler());

        var ps = new Dictionary<String, Object?>();
        Assert.Null(router.Match("/api/orders/123", ps));
        Assert.Null(router.Match("/api/users/123/extra", ps));
        Assert.Null(router.Match("/api", ps));
    }
}
