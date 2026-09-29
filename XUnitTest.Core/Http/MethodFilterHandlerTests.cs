using System.Net;
using NewLife.Http;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Http;

/// <summary>HTTP 方法过滤处理器测试</summary>
public class MethodFilterHandlerTests
{
    private sealed class StubHandler : IHttpHandler
    {
        public Boolean Called { get; private set; }

        public void ProcessRequest(IHttpContext context) => Called = true;
    }

    [Fact(DisplayName = "方法过滤器_请求为空_按不匹配拒绝而非放行")]
    public void NullRequest_Rejects()
    {
        // 旧写法 `!context.Request?.Method.EqualIgnoreCase(Method) == true` 在 Request 为 null 时整体为 null，
        // 条件不成立会直接放行到业务处理器，方法校验被绕过
        var inner = new StubHandler();
        var handler = new MethodFilterHandler("GET", inner);
        var context = new DefaultHttpContext((ISocketRemote)null!, null!, "/x", null);

        handler.ProcessRequest(context);

        Assert.False(inner.Called);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, context.Response.StatusCode);
        Assert.Equal("GET", context.Response.Headers["Allow"]);
    }

    [Fact(DisplayName = "方法过滤器_方法匹配_放行到实际处理器")]
    public void MatchedMethod_Passes()
    {
        var inner = new StubHandler();
        var handler = new MethodFilterHandler("GET", inner);
        var context = new DefaultHttpContext((ISocketRemote)null!, new HttpRequest { Method = "GET" }, "/x", null);

        handler.ProcessRequest(context);

        Assert.True(inner.Called);
    }

    [Fact(DisplayName = "方法过滤器_方法不匹配_回405且不进业务")]
    public void MismatchedMethod_Rejects()
    {
        var inner = new StubHandler();
        var handler = new MethodFilterHandler("GET", inner);
        var context = new DefaultHttpContext((ISocketRemote)null!, new HttpRequest { Method = "POST" }, "/x", null);

        handler.ProcessRequest(context);

        Assert.False(inner.Called);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, context.Response.StatusCode);
    }
}
