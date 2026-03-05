using NewLife.Threading;
using Xunit;

namespace XUnitTest.Threading;

/// <summary>线程池助手测试</summary>
public class ThreadPoolXTests
{
    [Fact(DisplayName = "Init不抛异常")]
    public void InitDoesNotThrow()
    {
        // Init触发静态构造函数，设置最小线程数
        ThreadPoolX.Init();

        ThreadPool.GetMinThreads(out var wt, out var io);
        Assert.True(wt > 0);
        Assert.True(io > 0);
    }

    [Fact(DisplayName = "QueueUserWorkItem执行回调")]
    public void QueueUserWorkItemExecutes()
    {
        var tcs = new TaskCompletionSource<Boolean>();
        ThreadPoolX.QueueUserWorkItem(() => tcs.SetResult(true));

        var result = tcs.Task.Wait(5000);
        Assert.True(result);
        Assert.True(tcs.Task.Result);
    }

    [Fact(DisplayName = "QueueUserWorkItem泛型执行回调")]
    public void QueueUserWorkItemGenericExecutes()
    {
        var tcs = new TaskCompletionSource<Int32>();
        ThreadPoolX.QueueUserWorkItem<Int32>(val => tcs.SetResult(val), 42);

        var result = tcs.Task.Wait(5000);
        Assert.True(result);
        Assert.Equal(42, tcs.Task.Result);
    }

    [Fact(DisplayName = "null回调安全忽略")]
    public void NullCallbackIgnored()
    {
        // 不应抛异常
        ThreadPoolX.QueueUserWorkItem((Action)null!);
        ThreadPoolX.QueueUserWorkItem<Int32>(null!, 0);
    }

    [Fact(DisplayName = "回调异常不抛出")]
    public void CallbackExceptionDoesNotThrow()
    {
        var done = new ManualResetEventSlim(false);
        ThreadPoolX.QueueUserWorkItem(() =>
        {
            try
            {
                throw new InvalidOperationException("test");
            }
            finally
            {
                done.Set();
            }
        });

        Assert.True(done.Wait(5000));
    }
}
