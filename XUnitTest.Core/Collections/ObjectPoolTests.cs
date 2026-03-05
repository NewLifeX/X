using NewLife.Collections;
using Xunit;

namespace XUnitTest.Collections;

public class ObjectPoolTests
{
    [Fact(DisplayName = "基本借出和归还")]
    public void BasicGetReturn()
    {
        using var pool = new ObjectPool<TestResource>();

        var obj = pool.Get();
        Assert.NotNull(obj);
        Assert.Equal(1, pool.BusyCount);
        Assert.Equal(0, pool.FreeCount);

        pool.Return(obj);
        Assert.Equal(0, pool.BusyCount);
        Assert.Equal(1, pool.FreeCount);
    }

    [Fact(DisplayName = "归还null返回false")]
    public void Return_Null()
    {
        using var pool = new ObjectPool<TestResource>();
        Assert.False(pool.Return(null!));
    }

    [Fact(DisplayName = "归还不存在的对象返回false")]
    public void Return_NotFromPool()
    {
        using var pool = new ObjectPool<TestResource>();
        var obj = new TestResource();
        Assert.False(pool.Return(obj));
    }

    [Fact(DisplayName = "多次借出归还")]
    public void MultipleGetReturn()
    {
        using var pool = new ObjectPool<TestResource> { Max = 10, Min = 1 };

        var list = new List<TestResource>();
        for (var i = 0; i < 5; i++)
        {
            list.Add(pool.Get());
        }
        Assert.Equal(5, pool.BusyCount);

        foreach (var item in list)
        {
            pool.Return(item);
        }
        Assert.Equal(0, pool.BusyCount);
        Assert.Equal(5, pool.FreeCount);
    }

    [Fact(DisplayName = "超过最大值抛异常")]
    public void ExceedsMax_Throws()
    {
        using var pool = new ObjectPool<TestResource> { Max = 2 };

        pool.Get();
        pool.Get();

        Assert.Throws<Exception>(() => pool.Get());
    }

    [Fact(DisplayName = "GetItem包装借出和归还")]
    public void GetItemTest()
    {
        using var pool = new ObjectPool<TestResource>();

        using (var item = pool.GetItem())
        {
            Assert.NotNull(item.Value);
            Assert.Equal(1, pool.BusyCount);
        }

        // Dispose后自动归还
        Assert.Equal(0, pool.BusyCount);
    }

    [Fact(DisplayName = "Clear清空")]
    public void ClearTest()
    {
        using var pool = new ObjectPool<TestResource>();

        var obj1 = pool.Get();
        var obj2 = pool.Get();
        pool.Return(obj1);
        pool.Return(obj2);

        var count = pool.Clear();
        Assert.True(count >= 2);
        Assert.Equal(0, pool.FreeCount);
    }

    [Fact(DisplayName = "Name默认值")]
    public void DefaultName()
    {
        using var pool = new ObjectPool<TestResource>();
        Assert.False(String.IsNullOrEmpty(pool.Name));
    }

    [Fact(DisplayName = "并发借出归还安全")]
    public void ConcurrentGetReturn()
    {
        using var pool = new ObjectPool<TestResource> { Max = 100, Min = 5 };

        Parallel.For(0, 50, _ =>
        {
            var item = pool.Get();
            Thread.Sleep(1); // 模拟使用
            pool.Return(item);
        });

        Assert.Equal(0, pool.BusyCount);
    }

    #region 辅助类
    class TestResource : IDisposable
    {
        public Boolean Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }
    #endregion
}
