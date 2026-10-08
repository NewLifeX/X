using NewLife.Http;
using Xunit;

namespace XUnitTest.Http;

/// <summary>参数化路由器测试：{param} / {param?} / {*path} 的段对齐与参数提取</summary>
public class HttpRouterTests
{
    private sealed class StubHandler : IHttpHandler
    {
        public Task ProcessRequestAsync(IHttpContext context) => TaskEx.CompletedTask;
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

    [Fact(DisplayName = "路由器_字面段多的模式优先_宽泛模式不遮蔽具体模式")]
    public void MoreLiterals_Wins()
    {
        // 同类模式（参数化路由）之间此前按注册顺序命中：先注册的宽泛模式会遮住后注册的具体模式
        var wide = new StubHandler();
        var specific = new StubHandler();
        var router = new HttpRouter();
        router.Register("/api/{controller}/{id}", wide);
        router.Register("/api/users/{id}", specific);

        var ps = new Dictionary<String, Object?>();
        Assert.Same(specific, router.Match("/api/users/123", ps));
        Assert.Equal("123", ps["id"]);

        // 不匹配具体模式时仍由宽泛模式兜底
        var ps2 = new Dictionary<String, Object?>();
        Assert.Same(wide, router.Match("/api/orders/9", ps2));
        Assert.Equal("orders", ps2["controller"]);
    }

    [Fact(DisplayName = "路由器_路径参数百分号解码_与查询串口径一致")]
    public void ParameterValue_UrlDecoded()
    {
        var router = new HttpRouter();
        router.Register("/api/files/{name}", new StubHandler());

        var ps = new Dictionary<String, Object?>();
        Assert.NotNull(router.Match("/api/files/a%20b%2Fc", ps));
        Assert.Equal("a b/c", ps["name"]);
    }

    [Fact(DisplayName = "路由器_通配参数逐段解码_段内%2F不改段边界")]
    public void WildcardValue_EachSegmentDecoded()
    {
        var router = new HttpRouter();
        router.Register("/files/{*path}", new StubHandler());

        var ps = new Dictionary<String, Object?>();
        Assert.NotNull(router.Match("/files/a%2Fb/c%20d", ps));
        Assert.Equal("a/b/c d", ps["path"]);
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

    [Fact(DisplayName = "路由器_失败候选参数不残留_不污染命中路由")]
    public void FailedCandidate_NoStaleParameters()
    {
        var hit = new StubHandler();
        var router = new HttpRouter();
        // 先注册的候选会先写入 id=5，再因字面段 detail 不匹配 info 而失败
        router.Register("/api/{id}/detail", new StubHandler());
        router.Register("/api/{ver}/info", hit);

        var ps = new Dictionary<String, Object?>();
        Assert.Same(hit, router.Match("/api/5/info", ps));

        // 失败候选残留的 {id} 不得出现在参数字典：它会在路由参数最后合并时覆盖同名查询参数
        Assert.False(ps.ContainsKey("id"));
        Assert.Equal("5", ps["ver"]);
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
