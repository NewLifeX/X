using System.Diagnostics;
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
        using var pool = new ObjectPool<TestResource> { Max = 2, WaitTimeout = TimeSpan.Zero };

        pool.Get();
        pool.Get();

        var ex = Assert.Throws<PoolFullException>(() => pool.Get());
        Assert.Equal(2, ex.BusyCount);
        Assert.Equal(2, ex.Max);
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
        using var pool = new ObjectPool<TestResource> { Max = 2, WaitTimeout = TimeSpan.Zero };

        await pool.GetAsync();
        await pool.GetAsync();

        var ex = await Assert.ThrowsAsync<PoolFullException>(() => pool.GetAsync());
        Assert.Equal(2, ex.BusyCount);
        Assert.Equal(2, ex.Max);
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

    [Fact(DisplayName = "GetAsync_WaitTimeout等待后成功获取")]
    public async Task GetAsync_WaitTimeout_Success()
    {
        using var pool = new ObjectPool<TestResource> { Max = 2, WaitTimeout = TimeSpan.FromSeconds(5) };

        // 借出 2 个占满池
        var obj1 = pool.Get();
        var obj2 = pool.Get();

        // 异步归还一个，让等待的 GetAsync 能获取到
        _ = Task.Run(async () =>
        {
            await Task.Delay(200);
            pool.Return(obj2);
        });

        // GetAsync 应该等待后成功获取
        var obj3 = await pool.GetAsync();
        Assert.NotNull(obj3);

        pool.Return(obj1);
        pool.Return(obj3);
    }

    [Fact(DisplayName = "GetAsync_WaitTimeout为0时立即抛异常")]
    public async Task GetAsync_WaitTimeout_Throws()
    {
        using var pool = new ObjectPool<TestResource> { Max = 1, WaitTimeout = TimeSpan.Zero };

        // 借出唯一的连接占满池
        var obj = pool.Get();

        // GetAsync 应 WaitTimeout=0 立即抛 PoolFullException
        var sw = Stopwatch.StartNew();
        var ex = await Assert.ThrowsAsync<PoolFullException>(() => pool.GetAsync());
        sw.Stop();

        Assert.Equal(1, ex.BusyCount);
        Assert.Equal(1, ex.Max);
        // 并行满载时异步获取可能被线程池调度推迟数十毫秒，只断言"没有进入等待"：
        // 若误用默认 15s 超时，这里会达到秒级
        Assert.True(sw.ElapsedMilliseconds < 2000, $"WaitTimeout=0 应立即抛异常，实际等待 {sw.ElapsedMilliseconds}ms");

        pool.Return(obj);
    }

    [Fact(DisplayName = "GetAsync_WaitTimeout默认15s阻塞等待")]
    public async Task GetAsync_WaitTimeout_DefaultBlocks()
    {
        using var pool = new ObjectPool<TestResource> { Max = 1 };

        var obj = pool.Get();

        // 异步归还一个，让等待的 GetAsync 能获取到
        _ = Task.Run(async () =>
        {
            await Task.Delay(200);
            pool.Return(obj);
        });

        // GetAsync 默认 WaitTimeout=15s，应阻塞等待后成功获取
        var sw = Stopwatch.StartNew();
        var obj2 = await pool.GetAsync();
        sw.Stop();

        Assert.NotNull(obj2);
        Assert.True(sw.ElapsedMilliseconds >= 150, $"等待 {sw.ElapsedMilliseconds}ms，应 >= 150ms（阻塞等待归还）");

        pool.Return(obj2);
    }

    [Fact(DisplayName = "MaxLifetime过期连接被惰性回收")]
    public void MaxLifetime_ExpiredConnection_Recycled()
    {
        using var pool = new ObjectPool<TestResource> { MaxLifetime = 1, WaitTimeout = TimeSpan.Zero };

        // 借出并立即归还，让 CreatedTime 距今很近
        var obj1 = pool.Get();
        pool.Return(obj1);
        Assert.Equal(1, pool.FreeCount);

        // 等待超过 MaxLifetime（1s），给足余量
        Thread.Sleep(1500);

        // 过期回收由后台定时器执行，多目标框架并行满载时定时器回调可能被推迟，轮询等待其完成
        var obj2 = pool.Get();
        var end = Environment.TickCount64 + 10_000;
        while (ReferenceEquals(obj1, obj2) && Environment.TickCount64 < end)
        {
            pool.Return(obj2);
            Thread.Sleep(50);
            obj2 = pool.Get();
        }

        // 旧连接因超 MaxLifetime 被回收，应创建新连接
        Assert.NotSame(obj1, obj2);

        pool.Return(obj2);
    }

    [Fact(DisplayName = "OnCreate抛异常不泄漏信号量槽位")]
    public void OnCreate_Throws_NoSlotLeak()
    {
        using var pool = new ThrowingCreatePool { Max = 2, WaitTimeout = TimeSpan.Zero, FailCount = 3 };

        // 前3次 OnCreate 抛异常，槽位必须被释放，否则池会永久锁死
        for (var i = 0; i < 3; i++)
            Assert.Throws<InvalidOperationException>(() => pool.Get());

        // 槽位无泄漏：现在应能正常借满 Max=2
        var a = pool.Get();
        var b = pool.Get();
        Assert.NotNull(a);
        Assert.NotNull(b);

        // 已满，立即抛 PoolFull（证明恰好2个槽位可用，既无泄漏也无多余）
        Assert.Throws<PoolFullException>(() => pool.Get());

        pool.Return(a);
        pool.Return(b);
    }

    [Fact(DisplayName = "OnCreateAsync抛异常不泄漏信号量槽位")]
    public async Task OnCreateAsync_Throws_NoSlotLeak()
    {
        using var pool = new ThrowingCreatePool { Max = 2, WaitTimeout = TimeSpan.Zero, FailCount = 3 };

        for (var i = 0; i < 3; i++)
            await Assert.ThrowsAsync<InvalidOperationException>(() => pool.GetAsync());

        var a = await pool.GetAsync();
        var b = await pool.GetAsync();
        Assert.NotNull(a);
        Assert.NotNull(b);

        await Assert.ThrowsAsync<PoolFullException>(() => pool.GetAsync());

        pool.Return(a);
        pool.Return(b);
    }

    [Fact(DisplayName = "OnReturn抛异常不泄漏信号量槽位")]
    public void OnReturn_Throws_NoSlotLeak()
    {
        using var pool = new ThrowingReturnPool { Max = 1, WaitTimeout = TimeSpan.Zero, ThrowOnReturn = true };

        var a = pool.Get();

        // 归还时 OnReturn 抛异常，finally 必须释放槽位
        Assert.Throws<InvalidOperationException>(() => pool.Return(a));

        // 槽位无泄漏：Max=1，应能再次借出
        pool.ThrowOnReturn = false;
        var b = pool.Get();
        Assert.NotNull(b);
        pool.Return(b);
    }

    [Fact(DisplayName = "Max为0时借还与清空不触碰信号量")]
    public void MaxZero_ReturnClear_NoThrow()
    {
        using var pool = new ObjectPool<TestResource> { Max = 0 };

        var a = pool.Get();
        var b = pool.Get();
        Assert.Equal(2, pool.BusyCount);

        Assert.True(pool.Return(a));
        pool.Return(b);
        Assert.Equal(0, pool.BusyCount);

        pool.Clear();
    }

    [Fact(DisplayName = "OnGet持续返回false时有界失败不挂起")]
    public void OnGet_AlwaysFalse_ThrowsInsteadOfHanging()
    {
        using var pool = new RejectablePool
        {
            Max = 2,
            WaitTimeout = TimeSpan.Zero,
            MaxRetry = 10,
            RejectFree = true,
            FreshEnabled = false,
        };

        var sw = Stopwatch.StartNew();
        Assert.Throws<InvalidOperationException>(() => pool.Get());
        sw.Stop();

        // 新建实例校验失败立即抛，不会反复重建
        Assert.Equal(1, pool.CreateCount);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10), $"有界失败耗时过长：{sw.Elapsed}");
    }

    [Fact(DisplayName = "异步OnGet持续返回false时有界失败不挂起")]
    public async Task OnGetAsync_AlwaysFalse_ThrowsInsteadOfHanging()
    {
        using var pool = new RejectablePool
        {
            Max = 2,
            WaitTimeout = TimeSpan.Zero,
            MaxRetry = 10,
            RejectFree = true,
            FreshEnabled = false,
        };

        var sw = Stopwatch.StartNew();
        await Assert.ThrowsAsync<InvalidOperationException>(() => pool.GetAsync());
        sw.Stop();

        Assert.Equal(1, pool.CreateCount);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10), $"有界失败耗时过长：{sw.Elapsed}");
    }

    [Fact(DisplayName = "旧实例连续失效达到MaxRetry后强制新建")]
    public void OnGet_StaleFreeItems_ForceCreateAfterMaxRetry()
    {
        using var pool = new RejectablePool { Max = 10, WaitTimeout = TimeSpan.Zero, MaxRetry = 3 };

        // 预置5个旧实例（Fresh=false）
        var list = new List<RejectableResource>();
        for (var i = 0; i < 5; i++)
        {
            var obj = pool.Get();
            obj.Fresh = false;
            list.Add(obj);
        }
        foreach (var obj in list) pool.Return(obj);

        pool.RejectFree = true;
        pool.ResetCounters();

        var result = pool.Get();

        // 丢弃3个旧实例后强制新建，新实例可用
        Assert.True(result.Fresh);
        Assert.Equal(3, pool.DisposeCount);
        Assert.Equal(1, pool.CreateCount);
        Assert.True(pool.BusyCount + pool.FreeCount <= pool.Max, $"总连接数超过上限：Busy={pool.BusyCount}, Free={pool.FreeCount}, Max={pool.Max}");

        pool.Return(result);
    }

    [Fact(DisplayName = "MaxRetry为0时旧实例全失效仍能终止")]
    public void OnGet_MaxRetryZero_Terminates()
    {
        using var pool = new RejectablePool { Max = 10, WaitTimeout = TimeSpan.Zero, MaxRetry = 0 };

        var list = new List<RejectableResource>();
        for (var i = 0; i < 3; i++)
        {
            var obj = pool.Get();
            obj.Fresh = false;
            list.Add(obj);
        }
        foreach (var obj in list) pool.Return(obj);

        pool.RejectFree = true;
        pool.ResetCounters();

        var result = pool.Get();

        // 不限制重试次数时仍会耗尽空闲项后新建，不会死循环
        Assert.True(result.Fresh);
        Assert.Equal(3, pool.DisposeCount);
        Assert.Equal(1, pool.CreateCount);

        pool.Return(result);
    }

    [Fact(DisplayName = "新建实例校验失败立即抛异常")]
    public void OnGet_NewItemInvalid_ThrowsImmediately()
    {
        using var pool = new RejectablePool
        {
            Max = 2,
            WaitTimeout = TimeSpan.Zero,
            MaxRetry = 10,
            RejectFree = true,
            FreshEnabled = false,
        };

        Assert.Throws<InvalidOperationException>(() => pool.Get());
        Assert.Equal(1, pool.CreateCount);
        Assert.Equal(1, pool.DisposeCount);
    }

    [Fact(DisplayName = "借出失败后信号量槽位不泄漏")]
    public void Get_Failure_ReleasesSlot()
    {
        using var pool = new RejectablePool
        {
            Max = 1,
            WaitTimeout = TimeSpan.Zero,
            MaxRetry = 10,
            RejectFree = true,
            FreshEnabled = false,
        };

        Assert.Throws<InvalidOperationException>(() => pool.Get());

        // 槽位已释放：改为可成功后应能立即借出，而不是 PoolFullException
        pool.FreshEnabled = true;
        var obj = pool.Get();
        Assert.NotNull(obj);
        pool.Return(obj);
    }

    [Fact(DisplayName = "异步借出失败后信号量槽位不泄漏")]
    public async Task GetAsync_Failure_ReleasesSlot()
    {
        using var pool = new RejectablePool
        {
            Max = 1,
            WaitTimeout = TimeSpan.Zero,
            MaxRetry = 10,
            RejectFree = true,
            FreshEnabled = false,
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => pool.GetAsync());

        pool.FreshEnabled = true;
        var obj = await pool.GetAsync();
        Assert.NotNull(obj);
        pool.Return(obj);
    }

    #region 辅助类
    class TestResource : IDisposable
    {
        public Boolean Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }

    class RejectableResource : IDisposable
    {
        /// <summary>是否为新创建实例。用于区分旧实例失效与新建实例校验失败</summary>
        public Boolean Fresh { get; set; }

        public void Dispose() { }
    }

    class RejectablePool : ObjectPool<RejectableResource>
    {
        /// <summary>为真时旧实例（Fresh=false）校验失败</summary>
        public Boolean RejectFree { get; set; }

        /// <summary>新建实例是否可用</summary>
        public Boolean FreshEnabled { get; set; } = true;

        public Int32 CreateCount { get; private set; }

        public Int32 DisposeCount { get; private set; }

        public void ResetCounters()
        {
            CreateCount = 0;
            DisposeCount = 0;
        }

        protected override RejectableResource? OnCreate()
        {
            CreateCount++;
            return new RejectableResource { Fresh = FreshEnabled };
        }

        protected override Boolean OnGet(RejectableResource value) => !RejectFree || value.Fresh;

        protected override async Task<Boolean> OnGetAsync(RejectableResource value, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return OnGet(value);
        }

        protected override void OnDispose(RejectableResource? value)
        {
            DisposeCount++;
            base.OnDispose(value);
        }
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

    class ThrowingCreatePool : ObjectPool<TestResource>
    {
        /// <summary>大于0时 OnCreate 抛异常，每次递减</summary>
        public Int32 FailCount { get; set; }

        protected override TestResource? OnCreate()
        {
            if (FailCount > 0)
            {
                FailCount--;
                throw new InvalidOperationException("模拟创建失败");
            }

            return new TestResource();
        }
    }

    class ThrowingReturnPool : ObjectPool<TestResource>
    {
        /// <summary>为真时 OnReturn 抛异常</summary>
        public Boolean ThrowOnReturn { get; set; }

        protected override Boolean OnReturn(TestResource value)
        {
            if (ThrowOnReturn) throw new InvalidOperationException("模拟归还校验失败");

            return true;
        }
    }
    #endregion
}
