using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Log;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Data;

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
        //Assert.Equal(f.Sequence, seq);

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
    public void NewIdForUTC()
    {
        var ids = new Int64[2];
        {
            var snow = new Snowflake();

            var id = snow.NewId();
            ids[0] = id;

            Assert.Equal(DateTimeKind.Local, snow.StartTimestamp.Kind);

            var rs = snow.TryParse(id, out var t, out var w, out var s);
            Assert.True(rs);
            Assert.Equal(DateTimeKind.Local, t.Kind);
        }
        {
            var snow = new Snowflake();
            snow.StartTimestamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var id = snow.NewId();
            ids[1] = id;

            Assert.Equal(DateTimeKind.Utc, snow.StartTimestamp.Kind);

            var rs = snow.TryParse(id, out var t, out var w, out var s);
            Assert.True(rs);
            Assert.Equal(DateTimeKind.Utc, t.Kind);
        }

        // 两个Id的时间应该相差不多
        var diff = (ids[1] - ids[0]) >> 22;
        //Assert.Equal(0, diff);
        Assert.True(Math.Abs(diff) < 100);

        {
            var snow = new Snowflake();

            var time = DateTime.Now;
            var id1 = snow.NewId(time);
            var id2 = snow.NewId(time.ToUniversalTime());

            diff = Math.Abs((id2 - id1) >> 22);
            Assert.True(diff < 100);

            time = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local);
            id1 = snow.NewId(time);
            id2 = snow.NewId(time.ToUniversalTime());

            diff = Math.Abs((id2 - id1) >> 22);
            Assert.True(diff < 100);
        }
    }

    [Fact]
    public void NewIdByUid()
    {
        var time = DateTime.Now;
        var uid = Rand.Next(1_000_000);

        XTrace.WriteLine("time: {0} uid: {1}/{1:X8}", time, uid);

        var f = new Snowflake();
        var id1 = f.NewId(time, uid);
        XTrace.WriteLine("id1: {0}", id1);

        var id2 = f.NewId(time, uid);
        Assert.NotEqual(id1, id2);
        XTrace.WriteLine("id2: {0}", id2);

        var id3 = f.NewId(time, uid);
        Assert.NotEqual(id1, id3);
        Assert.NotEqual(id2, id3);
        XTrace.WriteLine("id3: {0}", id3);

        var id4 = f.NewId(time, uid);
        Assert.NotEqual(id1, id4);
        Assert.NotEqual(id2, id4);
        Assert.NotEqual(id3, id4);
        XTrace.WriteLine("id4: {0}", id4);

        var id5 = f.NewId(time, uid);
        Assert.NotEqual(id1, id5);
        XTrace.WriteLine("id5: {0}", id5);
    }

    [Fact]
    public void ValidRepeat()
    {
        var sw = Stopwatch.StartNew();

        //var ws = new ConcurrentBag<Int32>();
        var ws = new ConcurrentDictionary<Int32, Snowflake>();
        //var repeat = new ConcurrentBag<Int64>();
        var hash = new ConcurrentDictionary<Int64, Snowflake>();

        var ts = new List<Task>();
        var ss = new Int64[24];
        var rs = new List<Int64>[ss.Length];
        for (var k = 0; k < ss.Length; k++)
        {
            // 提前计算workerId到本地变量，避免匿名函数闭包里面产生重复
            var wid = (k + 1) & 0x3FF;
            var idx = k;
            ts.Add(Task.Run(() =>
            {
                var repeat = new List<Int64>();
                var f = new Snowflake { StartTimestamp = new DateTime(2020, 1, 1), WorkerId = wid };
                //ws.Add(f.WorkerId);
                Assert.True(ws.TryAdd(f.WorkerId, f));
                //if (!ws.TryAdd(f.WorkerId, f)) Assert.True(false);

                //for (var i = 0; i < 1_000_000; i++)
                //{
                //    var id = f.NewId();
                //    if (!hash.TryAdd(id, f))
                //    {
                //        hash.TryGetValue(id, out var f2);
                //        repeat.Add(id);
                //    }

                //    ss[wid - 1]++;
                //}
                Parallel.For(0, 1_000_000, i =>
                {
                    var id = f.NewId();
                    if (!hash.TryAdd(id, f))
                    {
                        hash.TryGetValue(id, out var f2);
                        repeat.Add(id);
                    }

                    ss[wid - 1]++;
                });

                rs[wid - 1] = repeat;
            }));
        }
        Task.WaitAll(ts.ToArray());

        sw.Stop();

        //Assert.True(sw.ElapsedMilliseconds < 10_000);
        //var count = repeat.Count;
        //Assert.Equal(0, count);
        for (var i = 0; i < ss.Length; i++)
        {
            Assert.Equal(0, rs[i].Count);
            Assert.Empty(rs[i]);
        }
    }

    [Fact]
    public void ValidRepeatForSingleThread()
    {
        XTrace.WriteLine(nameof(ValidRepeatForSingleThread));

        var count = 1_000_000;
        var hash = new ConcurrentDictionary<Int64, Snowflake>();
        var ds = new Int64[count];
        var result = new List<Int64>();
        var repeat = new List<Int64>();
        var f = new Snowflake { StartTimestamp = new DateTime(2020, 1, 1) };

        // 生成雪花Id，用最短的代码，避免耗时
        var sw = Stopwatch.StartNew();
        Parallel.For(0, count, i =>
        {
            ds[i] = f.NewId();
        });

        sw.Stop();

        for (var i = 0; i < count; i++)
        {
            var id = ds[i];
            if (hash.TryAdd(id, f))
                result.Add(id);
            else
                repeat.Add(id);
        }

        XTrace.WriteLine("生成 {0:n0}，耗时 {1}，速度 {2:n0}tps", count, sw.Elapsed, count * 1000 / sw.ElapsedMilliseconds);

        // 按毫秒时间分解
        var dic = result.Select(e =>
        {
            f.TryParse(e, out var time, out var wid, out var seq);
            return new MyId { Id = e, Time = time, WorkerId = wid, Sequence = seq };
        }).GroupBy(e => e.Time).OrderBy(e => e.Key).ToDictionary(e => e.Key, e => e.ToList());
        var dic2 = repeat.Select(e =>
        {
            f.TryParse(e, out var time, out var wid, out var seq);
            return new MyId { Id = e, Time = time, WorkerId = wid, Sequence = seq };
        }).GroupBy(e => e.Time).OrderBy(e => e.Key).ToDictionary(e => e.Key, e => e.ToList());

        XTrace.WriteLine("SnowId，生成 {0:n0}，得到 {1:n0}，重复 {2:n0} ({3:p4})，时间 {4:n0}", count, result.Count, repeat.Count, (Double)repeat.Count / count, dic.Count);

        //// 输出每一毫秒的Id数据分布
        //var last = DateTime.MinValue;
        //foreach (var item in dic)
        //{
        //    var ids = item.Value;
        //    var min = ids.Min(e => e.Sequence);
        //    var max = ids.Max(e => e.Sequence);

        //    if (dic2.TryGetValue(item.Key, out var rs))
        //        XTrace.WriteLine("{0} {1:n0} ({2}, {3}) {4:n0} ({5}, {6})", item.Key, ids.Count, min, max, rs.Count, rs.Min(e => e.Sequence), rs.Max(e => e.Sequence));
        //    else
        //        XTrace.WriteLine("{0} {1:n0} ({2}, {3})", item.Key, ids.Count, min, max);

        //    if (item.Key > last.AddMilliseconds(1) && last != DateTime.MinValue)
        //        XTrace.WriteLine("时间差 {0:n0}ms", (item.Key - last).TotalMilliseconds);
        //    last = item.Key;

        //    if (min != 0)
        //        XTrace.WriteLine("最小值非0");
        //}

        //Assert.Equal(0, repeat.Count);
        Assert.Empty(repeat);
    }

    class MyId
    {
        public Int64 Id { get; set; }

        public DateTime Time { get; set; }

        public Int32 WorkerId { get; set; }

        public Int32 Sequence { get; set; }
    }

    [Fact]
    public void Benchmark()
    {
        var sw = Stopwatch.StartNew();

        var cpu = Environment.ProcessorCount * 4;
        var count = 10_000_000L;

        var ts = new List<Task>();
        for (var i = 0; i < cpu; i++)
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

        Assert.True(sw.ElapsedMilliseconds < 20_000);
    }

    [Fact]
    public void GlobalWorkerId()
    {
        {
            var n = Rand.Next(0x400);
            Snowflake.GlobalWorkerId = n;

            var sn = new Snowflake();
            sn.NewId();
            Assert.Equal(n, sn.WorkerId);
        }
        {
            var n = Rand.Next(0x400, Int32.MaxValue);
            Snowflake.GlobalWorkerId = n;

            var sn = new Snowflake();
            sn.NewId();
            Assert.Equal(n & 0x3FF, sn.WorkerId);
        }
    }
}