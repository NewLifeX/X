using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Threading;

namespace NewLife.Collections;

/// <summary>资源池。支持空闲释放、生命周期强制回收、池满阻塞等待，主要用于数据库连接池和网络连接池</summary>
/// <remarks>
/// <para>双空闲集合：ConcurrentStack（热栈，LIFO 复用，Min 保护） + ConcurrentQueue（冷队列，FIFO 清理）。</para>
/// <para>借出：SemaphoreSlim 控制并发数 ≤ Max，WaitTimeout 阻塞等待池满。</para>
/// <para>清理：TimerX 定时扫描冷队列（IdleTime + MaxLifetime）和热栈（Pop-All→过滤→Push-Back）。</para>
/// <para>钩子：OnCreate/OnGet/OnReturn/OnDispose 虚方法可重写，支持异步（OnCreateAsync/OnGetAsync）。</para>
/// <para>文档：https://newlifex.com/core/object_pool</para>
/// </remarks>
/// <typeparam name="T"></typeparam>
public class ObjectPool<T> : DisposeBase, IPool<T> where T : notnull
{
    #region 属性
    /// <summary>名称</summary>
    public String Name { get; set; }

    private volatile Int32 _FreeCount;
    /// <summary>空闲个数</summary>
    public Int32 FreeCount => _FreeCount;

    private volatile Int32 _BusyCount;
    /// <summary>繁忙个数</summary>
    public Int32 BusyCount => _BusyCount;

    /// <summary>最大个数。默认100，始终大于0</summary>
    public Int32 Max { get; set; } = 100;

    /// <summary>最小个数。默认1</summary>
    public Int32 Min { get; set; } = 1;

    /// <summary>空闲清理时间。最小个数之上的资源超过空闲时间时被清理，默认60s</summary>
    public Int32 IdleTime { get; set; } = 60;

    /// <summary>
    /// 完全空闲清理时间。已废弃，不再支持。后续版本将移除。
    /// </summary>
    [Obsolete("不再支持，后续版本将移除。请使用 IdleTime 控制空闲清理。")]
    public Int32 AllIdleTime { get; set; }

    /// <summary>借出等待超时。默认15s，池满时阻塞等待。TimeSpan.Zero表示不等待，池满时立即抛出PoolFullException。</summary>
    public TimeSpan WaitTimeout { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>最大生命周期。从创建时刻算起的绝对存活时间，超过后强制回收。默认0s表示不启用。适用于数据库主从切换、连接代理滚动更新等场景。</summary>
    public Int32 MaxLifetime { get; set; }

    /// <summary>容量信号量。控制并发借出数不超过 Max，池满时阻塞等待</summary>
    private SemaphoreSlim? _slot;

    private SemaphoreSlim GetSlot()
    {
        var s = _slot;
        if (s != null) return s;

        lock (_sync)
        {
            if (_slot != null) return _slot;

            return _slot = new SemaphoreSlim(Max, Max);
        }
    }

    /// <summary>基础空闲集合。只保存最小个数，最热部分</summary>
    private readonly ConcurrentStack<Item> _free = new();

    /// <summary>扩展空闲集合。保存最小个数以外部分</summary>
    private readonly ConcurrentQueue<Item> _free2 = new();

    /// <summary>借出去的放在这</summary>
    private readonly ConcurrentDictionary<T, Item> _busy = new();

    /// <summary>内部同步对象。避免锁定this降低外部误锁风险</summary>
    private readonly Object _sync = new();
    #endregion

    #region 构造
    /// <summary>实例化一个资源池</summary>
    public ObjectPool()
    {
        var str = GetType().Name;
        if (str.Contains('`')) str = str.Substring(null, "`");
        if (str != "Pool")
            Name = str;
        else
            Name = $"Pool<{typeof(T).Name}>";
    }

    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        _timer?.Dispose();
        _timer = null;
        _slot?.Dispose();

        Clear();
    }
    #endregion

    #region 内嵌
    class Item
    {
        /// <summary>数值</summary>
        public T? Value { get; set; }

        /// <summary>最后操作时间（借出或归还）。用于过期清理</summary>
        public DateTime LastTime { get; set; }

        /// <summary>创建时间。用于 MaxLifetime 强制回收</summary>
        public DateTime CreatedTime { get; set; }
    }
    #endregion

    #region 主方法
    /// <summary>尝试从空闲集合获取一个缓存项</summary>
    /// <param name="item">获取到的缓存项</param>
    /// <returns>是否成功获取</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Boolean TryAcquireFree([MaybeNullWhen(false)] out Item item)
    {
        if (_free.TryPop(out item) || _free2.TryDequeue(out item))
        {
            Interlocked.Decrement(ref _FreeCount);
            return true;
        }

        item = null;
        return false;
    }

    /// <summary>完成借出，记录到繁忙集合</summary>
    /// <param name="pi">缓存项</param>
    /// <returns></returns>
    private T FinishAcquire(Item pi)
    {
        pi.LastTime = TimerX.Now;
        _busy.TryAdd(pi.Value!, pi);
        Interlocked.Increment(ref _BusyCount);

        return pi.Value!;
    }

    /// <summary>借出</summary>
    /// <returns></returns>
    public virtual T Get()
    {
        // 获取容量槽位，池满时阻塞等待
        if (Max > 0 && !GetSlot()!.Wait(WaitTimeout))
        {
            using var span = DefaultTracer.Instance?.NewSpan($"pool:{Name}:Full", new { Name, BusyCount, Max, WaitTimeout });
            throw new PoolFullException(Name, BusyCount, Max);
        }

        try
        {
            while (true)
            {
                // 从空闲集合借一个。借出时惰性检查 MaxLifetime（Work 定时清理作兜底）
                if (TryAcquireFree(out var pi))
                {
                    if (MaxLifetime > 0 && pi.CreatedTime.AddSeconds(MaxLifetime) < TimerX.Now)
                    {
                        if (pi.Value != null) OnDispose(pi.Value);
                        continue;
                    }
                }
                else
                {
                    // 借不到，增加
                    pi = new Item
                    {
                        Value = OnCreate(),
                        CreatedTime = TimerX.Now,
                    };
                }

                // 借出时如果不可用，销毁并重试
                if (pi.Value == null || !OnGet(pi.Value))
                {
                    if (pi.Value != null) OnDispose(pi.Value);
                    continue;
                }

                return FinishAcquire(pi);
            }
        }
        catch
        {
            if (Max > 0) GetSlot()!.Release();
            throw;
        }
    }

    /// <summary>借出时是否可用</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    protected virtual Boolean OnGet(T value) => true;

    /// <summary>异步借出；内部使用 <see cref="OnCreateAsync"/> 和 <see cref="OnGetAsync"/> ，适用于需要异步初始化的连接池场景</summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public virtual async Task<T> GetAsync(CancellationToken cancellationToken = default)
    {
        // 获取容量槽位，池满时异步阻塞等待
        if (Max > 0 && !await GetSlot()!.WaitAsync(WaitTimeout, cancellationToken).ConfigureAwait(false))
        {
            using var span = DefaultTracer.Instance?.NewSpan($"pool:{Name}:Full", new { Name, BusyCount, Max, WaitTimeout });
            throw new PoolFullException(Name, BusyCount, Max);
        }

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 从空闲集合借一个。借出时惰性检查 MaxLifetime（Work 定时清理作兜底）
                if (TryAcquireFree(out var pi))
                {
                    if (MaxLifetime > 0 && pi.CreatedTime.AddSeconds(MaxLifetime) < TimerX.Now)
                    {
                        if (pi.Value != null) OnDispose(pi.Value);
                        continue;
                    }
                }
                else
                {
                    // 借不到，异步创建
                    pi = new Item
                    {
                        Value = await OnCreateAsync(cancellationToken).ConfigureAwait(false),
                        CreatedTime = TimerX.Now,
                    };
                }

                // 借出时如果不可用，销毁并重试
                if (pi.Value == null || !await OnGetAsync(pi.Value, cancellationToken).ConfigureAwait(false))
                {
                    if (pi.Value != null) OnDispose(pi.Value);
                    continue;
                }

                return FinishAcquire(pi);
            }
        }
        catch
        {
            if (Max > 0) GetSlot()!.Release();
            throw;
        }
    }

    /// <summary>异步检查借出时资源是否可用；默认调用同步 <see cref="OnGet"/>，子类可重写以支持异步检查（如 Ping 连接存活性）</summary>
    /// <param name="value"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected virtual Task<Boolean> OnGetAsync(T value, CancellationToken cancellationToken = default) => Task.FromResult(OnGet(value));

    /// <summary>申请资源包装项，Dispose时自动归还到池中</summary>
    /// <returns></returns>
    public PoolItem<T> GetItem() => new(this, Get());

    /// <summary>异步申请资源包装项，Dispose时自动归还到池中</summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<PoolItem<T>> GetItemAsync(CancellationToken cancellationToken = default) => new(this, await GetAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>归还</summary>
    /// <param name="value"></param>
    [Obsolete("Please use Return from 2024-02-01")]
    public virtual Boolean Put(T value) => Return(value);

    /// <summary>归还</summary>
    /// <param name="value"></param>
    public virtual Boolean Return(T value)
    {
        if (value == null) return false;

        // 从繁忙队列找到并移除缓存项
        if (!_busy.TryRemove(value, out var pi)) return false;

        Interlocked.Decrement(ref _BusyCount);

        // 是否可用。不可用时自动销毁，避免子类手动 TryDispose 的 workaround
        var slot = GetSlot();
        if (!OnReturn(value) || value is DisposeBase db && db.Disposed)
        {
            OnDispose(value);
            slot.Release();
            return false;
        }

        // 如果空闲数不足最小值，则返回到基础空闲集合
        if (_FreeCount < Min)
            _free.Push(pi);
        else
            _free2.Enqueue(pi);

        // 最后时间
        pi.LastTime = TimerX.Now;

        Interlocked.Increment(ref _FreeCount);

        // 启动定期清理的定时器（仅在需要清理时）
        if (IdleTime > 0) StartTimer();

        // 释放容量槽位，唤醒等待者
        slot.Release();

        return true;
    }

    /// <summary>归还时是否可用</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    protected virtual Boolean OnReturn(T value) => true;

    /// <summary>清空已有对象</summary>
    public virtual Int32 Clear()
    {
        var count = _FreeCount + _BusyCount;

        while (_free.TryPop(out var pi)) OnDispose(pi.Value);
        while (_free2.TryDequeue(out var pi)) OnDispose(pi.Value);
        Interlocked.Exchange(ref _FreeCount, 0);

        foreach (var item in _busy)
        {
            OnDispose(item.Key);
        }
        _busy.Clear();
        Interlocked.Exchange(ref _BusyCount, 0);

        return count;
    }

    /// <summary>销毁</summary>
    /// <param name="value"></param>
    protected virtual void OnDispose(T? value) => value.TryDispose();
    #endregion

    #region 重载
    /// <summary>创建实例</summary>
    /// <returns></returns>
    protected virtual T? OnCreate() => (T?)typeof(T).CreateInstance();

    /// <summary>异步创建实例；默认调用同步 <see cref="OnCreate"/>，子类可重写以支持异步初始化</summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected virtual Task<T?> OnCreateAsync(CancellationToken cancellationToken = default) => Task.FromResult(OnCreate());
    #endregion

    #region 定期清理
    private Boolean IsExpired(Item pi, DateTime idleExp, DateTime now)
    {
        if (IdleTime > 0 && pi.LastTime < idleExp) return true;
        if (MaxLifetime > 0 && pi.CreatedTime.AddSeconds(MaxLifetime) < now) return true;
        return false;
    }

    private TimerX? _timer;
    private void StartTimer()
    {
        if (_timer != null) return;
        lock (_sync)
        {
            _timer ??= new TimerX(Work, null, 15000, 15000) { Async = true };
        }
    }

    private void Work(Object? state)
    {
        var count = 0;

        var now = TimerX.Now;
        var idleExp = IdleTime > 0 ? now.AddSeconds(-IdleTime) : DateTime.MinValue;

        // 仅清理扩展空闲集合 _free2 中的超时项。热栈 _free 受 Min 保护，不清理
        if (FreeCount + BusyCount > Min && !_free2.IsEmpty)
        {
            // 移除过期项：空闲超时 或 超过最大生命周期
            var times = 10;
            while (times-- > 0 && _free2.TryPeek(out var pi) && IsExpired(pi, idleExp, now))
            {
                // 取出来销毁。在并行操作中，此时返回可能是另一个对象
                if (_free2.TryDequeue(out var pi2))
                {
                    if (IsExpired(pi2, idleExp, now))
                    {
                        pi2.Value.TryDispose();

                        count++;
                        Interlocked.Decrement(ref _FreeCount);
                    }
                    else
                    {
                        // 可能是另一个对象，放回去
                        _free2.Enqueue(pi2);
                    }
                }
            }
        }

        // 清理热栈 _free 中的过期项（空闲超时或超过最大生命周期）
        // ConcurrentStack 只能访问栈顶，Pop-All → 过滤 → Push-Back 是唯一的正确清理方式
        if (FreeCount + BusyCount > Min && !_free.IsEmpty)
        {
            var buffer = new List<Item>();
            while (_free.TryPop(out var pi)) buffer.Add(pi);

            foreach (var pi in buffer)
            {
                if (IsExpired(pi, idleExp, now))
                {
                    pi.Value.TryDispose();

                    count++;
                    Interlocked.Decrement(ref _FreeCount);
                }
                else
                {
                    _free.Push(pi);
                }
            }
        }

        // 如果没有需要清理的资源，停止定时器避免空转
        if (_free.IsEmpty && _free2.IsEmpty)
        {
            lock (_sync)
            {
                if (_free.IsEmpty && _free2.IsEmpty)
                {
                    _timer?.Dispose();
                    _timer = null;
                }
            }
        }
    }
    #endregion
}

/// <summary>池满异常。当借出时池中繁忙数已达最大值且等待超时时抛出</summary>
/// <remarks>实例化池满异常</remarks>
/// <param name="poolName">池名称</param>
/// <param name="busyCount">当前繁忙数</param>
/// <param name="max">最大容量</param>
public class PoolFullException(String poolName, Int32 busyCount, Int32 max) : InvalidOperationException($"{poolName} 申请失败，已有 {busyCount:n0} 达到或超过最大值 {max:n0}")
{
    /// <summary>当前繁忙数</summary>
    public Int32 BusyCount { get; } = busyCount;

    /// <summary>最大容量</summary>
    public Int32 Max { get; } = max;
}

/// <summary>资源池包装项，自动归还资源到池中</summary>
/// <typeparam name="T"></typeparam>
/// <remarks>包装项</remarks>
/// <param name="pool"></param>
/// <param name="value"></param>
public class PoolItem<T>(IPool<T> pool, T value) : DisposeBase
{
    #region 属性
    /// <summary>数值</summary>
    public T Value { get; } = value;

    /// <summary>池</summary>
    public IPool<T> Pool { get; } = pool;
    #endregion

    #region 销毁
    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        Pool.Return(Value);
    }
    #endregion
}