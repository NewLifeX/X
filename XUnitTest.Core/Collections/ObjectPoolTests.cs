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

    [Fact(DisplayName = "GetAsync异步借出和归还")]
    public async Task GetAsync_BasicGetReturn()
    {
        using var pool = new ObjectPool<TestResource>();

        var obj = await pool.GetAsync();
        Assert.NotNull(obj);
        Assert.Equal(1, pool.BusyCount);

        pool.Return(obj);
        Assert.Equal(0, pool.BusyCount);
    }

    [Fact(DisplayName = "GetItemAsync异步包装借出和自动归还")]
    public async Task GetItemAsync_AutoReturn()
    {
        using var pool = new ObjectPool<TestResource>();

        using (var item = await pool.GetItemAsync())
        {
            Assert.NotNull(item.Value);
            Assert.Equal(1, pool.BusyCount);
        }

        Assert.Equal(0, pool.BusyCount);
    }

    [Fact(DisplayName = "GetAsync超过最大值抛异常")]
    public async Task GetAsync_ExceedsMax_Throws()
    {
        using var pool = new ObjectPool<TestResource> { Max = 2 };

        await pool.GetAsync();
        await pool.GetAsync();

        await Assert.ThrowsAsync<Exception>(() => pool.GetAsync());
    }

    [Fact(DisplayName = "GetAsync支持CancellationToken取消")]
    public async Task GetAsync_CancelledToken_Throws()
    {
        using var pool = new ObjectPool<AsyncResource> { Max = 1 };
        using var cts = new System.Threading.CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pool.GetAsync(cts.Token));
    }

    [Fact(DisplayName = "OnCreateAsync异步创建被调用")]
    public async Task OnCreateAsync_AsyncCreation()
    {
        using var pool = new AsyncResourcePool();

        var obj = await pool.GetAsync();
        Assert.NotNull(obj);
        Assert.True(pool.CreateAsyncCalled);

        pool.Return(obj);
    }

    [Fact(DisplayName = "OnGetAsync异步校验被调用")]
    public async Task OnGetAsync_AsyncValidation()
    {
        using var pool = new AsyncValidationPool();

        var obj = await pool.GetAsync();
        Assert.NotNull(obj);

        pool.Return(obj);

        // 归还后再借，触发 OnGetAsync 验证
        var obj2 = await pool.GetAsync();
        Assert.NotNull(obj2);
        Assert.True(pool.GetAsyncCalled);

        pool.Return(obj2);
    }

    [Fact(DisplayName = "GetAsync并发安全")]
    public async Task GetAsync_ConcurrentSafe()
    {
        using var pool = new ObjectPool<TestResource> { Max = 100, Min = 5 };

        var tasks = Enumerable.Range(0, 50).Select(async _ =>
        {
            var item = await pool.GetAsync();
            await Task.Delay(1);
            pool.Return(item);
        });

        await Task.WhenAll(tasks);

        Assert.Equal(0, pool.BusyCount);
    }

    #region 辅助类
    class TestResource : IDisposable
    {
        public Boolean Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }

    class AsyncResource { }

    class AsyncResourcePool : ObjectPool<AsyncResource>
    {
        public Boolean CreateAsyncCalled { get; private set; }

        protected override async Task<AsyncResource?> OnCreateAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            CreateAsyncCalled = true;
            return new AsyncResource();
        }
    }

    class AsyncValidationPool : ObjectPool<TestResource>
    {
        public Boolean GetAsyncCalled { get; private set; }

        protected override async Task<Boolean> OnGetAsync(TestResource value, System.Threading.CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            GetAsyncCalled = true;
            return !value.Disposed;
        }
    }
    #endregion
}
