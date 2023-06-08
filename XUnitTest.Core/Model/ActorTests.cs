using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Model;
using Xunit;

namespace XUnitTest.Model
{
    public class ActorTests
    {
        [Fact(DisplayName = "1,基础Actor生成Excel数据")]
        public async void Test1()
        {
            var sw = Stopwatch.StartNew();

            var actor = new BuildExcelActor();
            var count = 6;
            for (var i = 0; i < count; i++)
            {
                // 模拟查询数据，耗时500ms
                XTrace.WriteLine("读取数据……");
                await Task.Delay(500);

                // 通知另一个处理器
                actor.Tell($"数据行_{i + 1}");
            }

            var sw2 = Stopwatch.StartNew();

            // 等待最终完成
            actor.Stop();

            sw.Stop();
            sw2.Stop();

            Assert.True(sw.ElapsedMilliseconds > 6 * 500);
            Assert.True(sw2.ElapsedMilliseconds <= 500);
        }

        private class BuildExcelActor : Actor
        {
            protected override async Task ReceiveAsync(ActorContext context, CancellationToken cancellationToken)
            {
                XTrace.WriteLine("生成Excel数据：{0}", context.Message);

                await Task.Delay(500);
            }
        }

        [Fact(DisplayName = "2,累加计数Actor")]
        public void TestInc()
        {
            using var actor = new TestActor();
            actor.BoundedCapacity = 200;
            actor.BatchSize = 100;

            XTrace.WriteLine("TestCount Start");
            for (var i = 0; i < 1000; i++)
            {
                actor.Tell(i);
            }

            XTrace.WriteLine("TestCount Finishing");

            actor.Stop();
            Assert.True(actor.Total < 1000);

            actor.Stop(-1);
            Assert.Equal(1000, actor.Total);

            XTrace.WriteLine("TestCount End");

            Thread.Sleep(5000);
            XTrace.WriteLine("End");
        }

        private class TestActor : Actor
        {
            public Int32 Total { get; set; }

            protected override async Task ReceiveAsync(ActorContext[] contexts, CancellationToken cancellationToken)
            {
                Total += contexts.Length;
                XTrace.WriteLine("Total={0}", Total);

                await Task.Delay(500);
            }
        }
    }
}