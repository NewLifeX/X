using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NewLife.Log;
using Xunit;
using NewLife.Reflection;
using System.Diagnostics;

namespace XUnitTest.Log
{
    public class PerfCounterTests
    {
        [Fact]
        public void Test()
        {
            var counter = new PerfCounter();

            XTrace.WriteLine("IsHighResolution={0}", Stopwatch.IsHighResolution);
            XTrace.WriteLine("Frequency={0:n0}", Stopwatch.Frequency);
            var tickFrequency = typeof(CounterHelper).GetValue("tickFrequency");
            XTrace.WriteLine("tickFrequency={0:n0}", tickFrequency);

            var ts = counter.StartCount();
            var count = 10000;
            Thread.SpinWait(count);

            var usCost = counter.StopCount(ts);
            XTrace.WriteLine("Thread.SpinWait({0}) = {1}us", count, usCost);

            //Assert.True(usCost >= 1000);
        }
    }
}
