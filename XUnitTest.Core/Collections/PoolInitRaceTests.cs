using NewLife.Collections;
using Xunit;

namespace XUnitTest.Collections;

/// <summary>对象池与清空并发时不崩</summary>
public class PoolInitRaceTests
{
    [Fact(DisplayName = "并发取还对象同时清空池不抛异常")]
    public void GetReturnWhileClear()
    {
        var pool = new Pool<Object>(64);

        // 只做并发压力：窗口很窄，不能保证每次都撞上，但能抓住「返回 null」这类崩溃
        Parallel.For(0, 8, i =>
        {
            for (var n = 0; n < 20_000; n++)
            {
                var value = pool.Get();
                Assert.NotNull(value);
                pool.Return(value);

                if ((n & 1023) == 0) pool.Clear();
            }
        });
    }
}
