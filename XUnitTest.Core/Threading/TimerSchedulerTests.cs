using NewLife.Threading;
using Xunit;

namespace XUnitTest.Threading;

/// <summary>定时器调度器测试</summary>
public class TimerSchedulerTests
{
    [Fact(DisplayName = "Default调度器不为null")]
    public void DefaultIsNotNull()
    {
        Assert.NotNull(TimerScheduler.Default);
    }

    [Fact(DisplayName = "Create创建命名调度器")]
    public void CreateNamedScheduler()
    {
        var name = "TestScheduler_" + Guid.NewGuid().ToString("N")[..8];
        var scheduler = TimerScheduler.Create(name);

        Assert.NotNull(scheduler);
        Assert.Equal(name, scheduler.ToString());
    }

    [Fact(DisplayName = "Create同名返回相同实例")]
    public void CreateSameNameReturnsSameInstance()
    {
        var name = "SharedScheduler_" + Guid.NewGuid().ToString("N")[..8];
        var s1 = TimerScheduler.Create(name);
        var s2 = TimerScheduler.Create(name);

        Assert.Same(s1, s2);
    }

    [Fact(DisplayName = "Dispose可以安全调用")]
    public void DisposeIsSafe()
    {
        var name = "DisposeTest_" + Guid.NewGuid().ToString("N")[..8];
        var scheduler = TimerScheduler.Create(name);
        scheduler.Dispose();
        // 不应抛异常
    }
}
