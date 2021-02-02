using System;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Security;
using NewLife.Threading;
using Xunit;

namespace XUnitTest.Threading
{
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
            }, null, 10, 100, "Test");

            Assert.True(timer.Id > 0);
            Assert.Equal("Test", timer.Scheduler.Name);
            //Assert.NotNull(timer.Callback);
            Assert.Null(timer.State);
            Assert.True(timer.NextTime > now.AddMilliseconds(10));
            Assert.True(timer.NextTime < now.AddMilliseconds(20));
            Assert.Equal(0, timer.Timers);
            Assert.Equal(100, timer.Period);
            Assert.False(timer.Async);
            Assert.False(timer.Absolutely);

            Thread.Sleep(1050);

            //Assert.Equal(10, count);
            //Assert.Equal(10, timer.Timers);
        }

        [Fact]
        public void AsyncTest()
        {
            XTrace.WriteLine("AsyncTest");
            using var timer = new TimerX(DoAsyncTest, "Stone", 10, 100);

            Thread.Sleep(1000);
        }

        private static async Task DoAsyncTest(Object state)
        {
            var key = Rand.NextString(8);
            XTrace.WriteLine("Begin {0} {1}", state, key);

            await Task.Delay(110);

            XTrace.WriteLine("End {0} {1}", state, key);
        }

        [Fact]
        public void AsyncTest2()
        {
            XTrace.WriteLine("AsyncTest2");
            using var timer = new TimerX(DoAsyncTest2, "Stone2", DateTime.Now, 100);

            Thread.Sleep(1000);
        }

        private static async Task DoAsyncTest2(Object state)
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
            using var timer = new TimerX(DoAsyncTest2, "CronTest", "1/2 * * * *");

            Thread.Sleep(5500);
        }
    }
}
