using NewLife;
using NewLife.Http;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Http;

/// <summary>DefaultHttpContext.Current 的异步上下文流动测试</summary>
public class HttpContextCurrentTests
{
    [Fact(DisplayName = "Current_随异步上下文流动_TaskRun与await之后均可获取")]
    public async Task Current_FlowsAcrossAsyncBoundaries()
    {
        // 此前是 [ThreadStatic]：异步续体/派生任务切到别的线程后取到 null，
        // 表现为同一请求内"随机"丢失当前上下文（中间件 await 之后 NRE）
        INetSession session = null!;
        var ctx = new DefaultHttpContext(session, new HttpRequest(), "/x", null);
        DefaultHttpContext.Current = ctx;
        try
        {
            // 派生任务（线程池线程）里仍应取到同一实例
            var seen = await Task.Run(() => DefaultHttpContext.Current);
            Assert.Same(ctx, seen);

            await Task.Yield();
            Assert.Same(ctx, DefaultHttpContext.Current);
        }
        finally
        {
            DefaultHttpContext.Current = null;
        }
    }

    [Fact(DisplayName = "Current_默认空_请求结束后被清空")]
    public void Current_ClearedByRequest()
    {
        INetSession session = null!;
        var ctx = new DefaultHttpContext(session, new HttpRequest(), "/x", null);
        DefaultHttpContext.Current = ctx;

        // 模拟 HttpSession 的 finally：请求结束清空，避免泄漏到同一上下文内的后续代码
        DefaultHttpContext.Current = null;

        Assert.Null(DefaultHttpContext.Current);
    }
}
