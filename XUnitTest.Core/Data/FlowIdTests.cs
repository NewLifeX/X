using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Data
{
    public class FlowIdTests
    {
        [Fact]
        public void NewId()
        {
            var f = new Snowflake();
            var id = f.NewId();

            var time = id >> 22;
            var tt = f.StartTimestamp.AddMilliseconds(time);
            Assert.True(tt <= DateTime.Now);

            var wid = (id >> 12) & 0x3FF;
            Assert.Equal(f.WorkerId, wid);

            var seq = id & 0x0FFF;
            Assert.Equal(f.Sequence, seq);

            // 时间转编号
            var id2 = f.GetId(tt);
            Assert.Equal(id >> 22, id2 >> 22);

            // 分析
            var rs = f.TryParse(id, out var t, out var w, out var s);
            Assert.True(rs);
            Assert.Equal(tt, t);
            Assert.Equal(wid, w);
            Assert.Equal(seq, s);
        }

        [Fact]
        public void ValidRepeat()
        {
            var sw = Stopwatch.StartNew();

            //var ws = new ConcurrentBag<Int32>();
            var ws = new ConcurrentDictionary<Int32, Snowflake>();
            var repeat = new ConcurrentBag<Int64>();
            var hash = new ConcurrentDictionary<Int64, Snowflake>();

            var ts = new List<Task>();
            for (var k = 0; k < 10; k++)
            {
                // 提前计算workerId到本地变量，避免匿名函数闭包里面产生重复
                var wid = (k + 1) & 0x3FF;
                ts.Add(Task.Run(() =>
                {
                    var f = new Snowflake { StartTimestamp = new DateTime(2020, 1, 1), WorkerId = wid };
                    //ws.Add(f.WorkerId);
                    Assert.True(ws.TryAdd(f.WorkerId, f));
                    //if (!ws.TryAdd(f.WorkerId, f)) Assert.True(false);

                    for (var i = 0; i < 100_000; i++)
                    {
                        var id = f.NewId();
                        if (!hash.TryAdd(id, f)) repeat.Add(id);
                    }
                }));
            }
            Task.WaitAll(ts.ToArray());

            sw.Stop();

            Assert.True(sw.ElapsedMilliseconds < 10_000);
            var count = repeat.Count;
            Assert.Equal(0, count);
        }

        [Fact]
        public void Benchmark()
        {
            var sw = Stopwatch.StartNew();

            var count = 10_000_000L;

            var ts = new List<Task>();
            for (var i = 0; i < 1; i++)
            {
                ts.Add(Task.Run(() =>
                {
                    var f = new Snowflake();

                    for (var i = 0; i < count; i++)
                    {
                        var id = f.NewId();
                    }
                }));
            }

            Task.WaitAll(ts.ToArray());

            sw.Stop();

            count *= ts.Count;
            XTrace.WriteLine("生成 {0:n0}，耗时 {1}，速度 {2:n0}tps", count, sw.Elapsed, count * 1000 / sw.ElapsedMilliseconds);

            Assert.True(sw.ElapsedMilliseconds < 10_000);
        }
    }
}