using System.Diagnostics;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Model;
using Xunit;

namespace XUnitTest.Model
{
    public class ActorTests
    {
        [Fact]
        public async void Test1()
        {
            var sw = Stopwatch.StartNew();

            var actor = new BuildExcelActor();

            for (var i = 0; i < 6; i++)
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
            protected override async Task ReceiveAsync(ActorContext context)
            {
                XTrace.WriteLine("生成Excel数据：{0}", context.Message);

                await Task.Delay(500);
            }
        }
    }
}