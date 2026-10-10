using System;
using System.Threading;
using System.Threading.Tasks;
using NewLife;
using NewLife.Log;
using NewLife.Security;
using NewLife.Threading;
using Xunit;

namespace XUnitTest.Threading;

public class TimerXTests
{
    static TimerXTests()
    {
        TimerScheduler.Default.Log = XTrace.Log;
    }

    [Fact]
    public void NormalTest()
    {
        var now = DateTime.Now;
        var count = 0;
        using var timer = new TimerX(s =>
        {
            Interlocked.Increment(ref count);

            //Assert.Equal(s, TimerX.Current);
        }, "NewLife", 1000, 3000, "Test");

        Assert.True(timer.Id > 0);
        Assert.Equal("Test", timer.Scheduler.Name);
        //Assert.NotNull(timer.Callback);
        Assert.Equal("NewLife", timer.State);
        Assert.True(timer.NextTick > Runtime.TickCount64);
        Assert.True(timer.NextTick <= Runtime.TickCount64 + 1000);
        Assert.True(timer.NextTime > now.AddMilliseconds(100));
        //Assert.True(timer.NextTime < now.AddMilliseconds(20));
        Assert.Equal(0, timer.Timers);
        Assert.Equal(3000, timer.Period);
        Assert.False(timer.Async);
        Assert.False(timer.Absolutely);

        //Assert.Equal(10, count);
        //Assert.Equal(10, timer.Timers);
    }

    [Fact]
    public void SyncTest()
    {
        XTrace.WriteLine("SyncTest");

        // 以构造前后的系统节拍为窗口校验首次触发时刻：NextTick = 构造时读数 + 100，
        // 这样既不受 TickCount64 在 Windows 上 15.6ms 量化影响，也不受线程被抢占导致的读数延迟影响
        var before = Runtime.TickCount64;
        using var timer = new TimerX(DoSyncTest, "SyncStone", 100, 200);
        var after = Runtime.TickCount64;

        Assert.InRange(timer.NextTick, before + 100, after + 100);

        // 留一点时间让定时器至少触发一次
        Thread.Sleep(300);
    }

    private static void DoSyncTest(Object state)
    {
        var key = Rand.NextString(8);
        XTrace.WriteLine("Begin {0} {1}", state, key);

        Thread.Sleep(100);

        XTrace.WriteLine("End {0} {1}", state, key);
    }

    [Fact]
    public void AsyncTest()
    {
        XTrace.WriteLine("AsyncTest");

        // 同 SyncTest，用构造前后的节拍窗口消除 TickCount64 量化与线程调度带来的测量误差
        var before = Runtime.TickCount64;
        using var timer = new TimerX(DoAsyncTest, "AsyncStone", 100, 200) { Async = true };
        var after = Runtime.TickCount64;

        Assert.InRange(timer.NextTick, before + 100, after + 100);

        // 留一点时间让定时器至少触发一次
        Thread.Sleep(300);
    }

    private static async Task DoAsyncTest(Object state)
    {
        var key = Rand.NextString(8);
        XTrace.WriteLine("Begin {0} {1}", state, key);

        await Task.Delay(100);

        XTrace.WriteLine("End {0} {1}", state, key);
    }

    [Fact]
    public void AbsolutelyTest()
    {
        XTrace.WriteLine("AbsolutelyTest");

        var before = Runtime.TickCount64;
        using var timer = new TimerX(DoAbsolutelyTest, "Stone2", DateTime.Today, 100);
        var after = Runtime.TickCount64;

        // 绝对定时器把 00:00 起的 100ms 网格推后到当前时刻之后，首次触发落在 (now, now+100] 区间内
        Assert.InRange(timer.NextTick, before + 1, after + 100);

        // 留一点时间让定时器至少触发一次
        Thread.Sleep(300);
    }

    private static async Task DoAbsolutelyTest(Object state)
    {
        var key = Rand.NextString(8);
        XTrace.WriteLine("Begin {0} {1}", state, key);

        await Task.Delay(110);

        XTrace.WriteLine("End {0} {1}", state, key);
    }

    [Fact]
    public void AbsolutelyBoundaryTest()
    {
        XTrace.WriteLine("AbsolutelyBoundaryTest");

        // 构造时刻距绝对网格点不足 1ms 时，间隔截断为 0 会提前触发。
        // 显式把网格点摆在当前时刻之后 0.3ms，覆盖该窗口，验证首个触发严格晚于当前时刻。
        // 奇偶轮换同步与异步构造，两条路径都覆盖
        for (var i = 0; i < 100; i++)
        {
            var start = DateTime.Now.AddMilliseconds(-99.7);
            var before = Runtime.TickCount64;
            using var timer = i % 2 == 0
                ? new TimerX(_ => { }, "Stone3", start, 100)
                : new TimerX(DoAbsolutelyTest, "Stone2", start, 100);
            var after = Runtime.TickCount64;

            Assert.InRange(timer.NextTick, before + 1, after + 100);
        }
    }

    [Fact]
    public void CronTest()
    {
        XTrace.WriteLine("CronTest");

        var before = Runtime.TickCount64;
        using var timer = new TimerX(DoCronTest, "CronTest", "1/4 * * * *;3/20");
        var after = Runtime.TickCount64;

        Assert.NotNull(timer.Crons);
        Assert.Equal(2, timer.Crons.Length);

        // 首次触发不晚于 4 秒（1/4 表示每分钟内从第 1 秒起每 4 秒一次），
        // 下界用构造时读数、上界留 1 秒余量以容纳秒级对齐
        Assert.InRange(timer.NextTick, before + 1, after + 3999 + 1000);
    }

    private static async Task DoCronTest(Object state)
    {
        var key = Rand.NextString(8);
        XTrace.WriteLine("Begin {0} {1}", state, key);

        await Task.Delay(110);

        XTrace.WriteLine("End {0} {1}", state, key);
    }
}