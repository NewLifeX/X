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

        Thread.Sleep(3050);

        //Assert.Equal(10, count);
        //Assert.Equal(10, timer.Timers);
    }

    [Fact]
    public void SyncTest()
    {
        XTrace.WriteLine("SyncTest");
        using var timer = new TimerX(DoSyncTest, "SyncStone", 100, 200);

        var ms = Runtime.TickCount64;
        ms = ms + 100 - timer.NextTick;
        Assert.InRange(ms, 0, 5);

        Thread.Sleep(1000);
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
        using var timer = new TimerX(DoAsyncTest, "AsyncStone", 100, 200) { Async = true };

        var ms = Runtime.TickCount64;
        ms = ms + 100 - timer.NextTick;
        Assert.InRange(ms, 0, 5);

        Thread.Sleep(1000);
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
        using var timer = new TimerX(DoAbsolutelyTest, "Stone2", DateTime.Today, 100);

        var ms = timer.NextTick - Runtime.TickCount64;
        Assert.InRange(ms, 0, 99);

        Thread.Sleep(1000);
    }

    private static async Task DoAbsolutelyTest(Object state)
    {
        var key = Rand.NextString(8);
        XTrace.WriteLine("Begin {0} {1}", state, key);

        await Task.Delay(110);

        XTrace.WriteLine("End {0} {1}", state, key);
    }

    [Fact]
    public void CronTest()
    {
        XTrace.WriteLine("CronTest");
        using var timer = new TimerX(DoCronTest, "CronTest", "1/4 * * * *;3/20");

        Assert.NotNull(timer.Crons);
        Assert.Equal(2, timer.Crons.Length);

        var ms = timer.NextTick - Runtime.TickCount64;
        Assert.InRange(ms, 0, 3999 + 1000);

#if DEBUG
        Thread.Sleep(5500);
#endif
    }

    private static async Task DoCronTest(Object state)
    {
        var key = Rand.NextString(8);
        XTrace.WriteLine("Begin {0} {1}", state, key);

        await Task.Delay(110);

        XTrace.WriteLine("End {0} {1}", state, key);
    }
}