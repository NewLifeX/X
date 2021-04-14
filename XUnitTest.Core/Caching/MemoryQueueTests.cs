using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Caching;
using NewLife.Threading;
using Xunit;

namespace XUnitTest.Caching
{
    public class MemoryQueueTests
    {
        [Fact]
        public async void Test1()
        {
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

            ThreadPoolX.QueueUserWorkItem(() =>
            {
                Thread.Sleep(1100);
                q.Add("delay");
            });

            var s2 = await q.TakeOneAsync(1500);
            Assert.Equal("delay", s2);
        }
    }
}