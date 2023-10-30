using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NewLife.Caching;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Caching;

public class MemoryQueueTests
{
    [Fact]
    public async void Test1()
    {
        XTrace.WriteLine("MemoryQueueTests.Test1");

        var q = new MemoryQueue<String>();

        Assert.True(q.IsEmpty);
        Assert.Equal(0, q.Count);

        q.Add("test");
        q.Add("newlife", "stone");

        Assert.False(q.IsEmpty);
        Assert.Equal(3, q.Count);

        var s1 = q.TakeOne();
        Assert.Equal("test", s1);

        var ss = q.Take(3).ToArray();
        Assert.Equal(2, ss.Length);

        XTrace.WriteLine("begin TokeOneAsync");
        ThreadPool.QueueUserWorkItem(s =>
        {
            Thread.Sleep(1100);
            XTrace.WriteLine("add message");
            q.Add("delay");
        });

        var s2 = await q.TakeOneAsync(15, default);
        XTrace.WriteLine("end TokeOneAsync");
        Assert.Equal("delay", s2);
    }
}