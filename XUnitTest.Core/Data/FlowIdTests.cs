using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Data;
using Xunit;

namespace XUnitTest.Data
{
    public class FlowIdTests
    {
        [Fact]
        public void NewId()
        {
            var fid = new FlowId();
            var id = fid.NewId();

            var time = id >> 22;
            Assert.True(fid.StartTimestamp.AddMilliseconds(time) <= DateTime.Now);

            var wid = (id >> 12) & 0x3FF;
            Assert.Equal(fid.WorkerId, wid);

            var seq = id & 0x0FFF;
            Assert.Equal(fid.Sequence, seq);
        }

        [Fact]
        public void ValidRepeat()
        {
            var sw = Stopwatch.StartNew();

            var ws = new List<Int32>();
            var hash = new ConcurrentHashSet<Int64>();

            var repeat = 0;
            var ts = new List<Task>();
            for (var k = 0; k < 10; k++)
            {
                ts.Add(Task.Run(() =>
                {
                    var f = new FlowId();
                    Assert.True(!ws.Contains(f.WorkerId));
                    ws.Add(f.WorkerId);

                    for (var i = 0; i < 1_000_000; i++)
                    {
                        var id = f.NewId();
                        if (hash.Contain(id))
                            Interlocked.Increment(ref repeat);
                        else
                            hash.TryAdd(id);
                    }
                }));
            }
            Task.WaitAll(ts.ToArray());

            sw.Stop();

            Assert.True(sw.ElapsedMilliseconds < 10_000);
            Assert.Equal(0, repeat);
        }

        [Fact]
        public void Benchmark()
        {
            var f = new FlowId();

            var sw = Stopwatch.StartNew();

            for (var i = 0; i < 10_000_000; i++)
            {
                var id = f.NewId();
            }

            sw.Stop();

            Assert.True(sw.ElapsedMilliseconds < 10_000);
        }
    }
}