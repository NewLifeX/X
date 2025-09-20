using System.Collections.Concurrent;
using NewLife.Collections;
using Xunit;

namespace XUnitTest.Collections;

public class PoolTests
{
    [Fact(DisplayName = "Pool 并发压力测试")]
    public void ConcurrentGetReturn()
    {
        var pool = new Pool<Object>(64);
        var bag = new ConcurrentBag<Object>();
        var threadCount = 8;
        var loop = 10_000;
        Parallel.For(0, threadCount, _ =>
        {
            for (var i = 0; i < loop; i++)
            {
                var obj = pool.Get();
                bag.Add(obj);
                Assert.NotNull(obj);
            }
        });

        // 归还所有对象
        foreach (var obj in bag) pool.Return(obj);

        // 池容量不超限
        var count = pool.Clear();
        Assert.True(count <= pool.Max);
    }
}
