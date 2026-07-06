using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Threading;

namespace NewLife.Collections;

/// <summary>资源池。支持空闲释放、生命周期强制回收、池满阻塞等待，主要用于数据库连接池和网络连接池</summary>
/// <remarks>
/// <para>唯一权威：SemaphoreSlim（容量=Max）是限流的唯一机制，借出时 Wait、归还时 Release。因创建新连接必先持有信号量，且空闲缓存只存放曾借出过的连接，故总连接数（Busy+Free）恒不超过 Max。FreeCount/BusyCount 仅用于观测，从不参与放行判断。</para>
/// <para>空闲存储：双集合仅是缓存组织方式，不参与限流。ConcurrentStack（热栈，LIFO 复用，保留 Min 个最热连接） + ConcurrentQueue（冷队列，FIFO，存放 Min 以外的溢出连接便于清理）。</para>
/// <para>清理：TimerX 定时扫描冷队列（IdleTime + MaxLifetime）和热栈（Pop-All→过滤→Push-Back）。空闲连接不持有信号量，销毁它们不释放槽位。</para>
/// <para>钩子：OnCreate/OnGet/OnReturn/OnDispose 虚方法可重写，支持异步（OnCreateAsync/OnGetAsync）。钩子抛出异常时信号量会被安全释放，不会泄漏槽位。</para>
/// <para>注意：Max 在首次借出时按当前值确定信号量容量，之后修改无效，请在使用前设定。</para>
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

    /// <summary>最大个数。默认100，限制总连接数（Busy+Free≤Max），由容量信号量唯一保证。小于等于0表示不限制。须在首次借出前设定。</summary>
    public Int32 Max { get; set; } = 100;

    /// <summary>最小个数。默认1，维持的最少空闲连接数，IdleTime 不清理但 MaxLifetime 可淘汰</summary>
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

    /// <summary>容量信号量。唯一限流机制，容量=Max。借出时 Wait、归还时 Release，保证总连接数 Busy+Free≤Max，池满时阻塞等待 WaitTimeout。Max≤0 时返回 null 表示不限流</summary>
    private SemaphoreSlim? _slot;

    private SemaphoreSlim? GetSlot()
    {
        if (Max <= 0) return null;

        var s = Volatile.Read(ref _slot);
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

        Clear();

        _slot?.Dispose();
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
        // 获取槽位。借出持有、归还释放，信号量容量=Max 保证总连接数 Busy+Free≤Max
        var slot = GetSlot();
        if (slot?.Wait(WaitTimeout) == false)
        {
            using var span = DefaultTracer.Instance?.NewSpan($"pool:{Name}:Full", new { Name, BusyCount, Max, WaitTimeout });
            throw new PoolFullException(Name, BusyCount, Max);
        }

        // 已持有槽位。循环内 OnCreate/OnGet 若抛异常，catch 释放槽位避免泄漏；成功 return 不释放，归还时才还
        try
        {
            while (true)
            {
                // 获取一个 Item（优先从空闲池复用，否则新建）
                if (!TryAcquireFree(out var pi))
                    pi = new Item { Value = OnCreate(), CreatedTime = TimerX.Now };

                if (pi.Value == null) continue;

                // 生命周期检查（新建项 CreatedTime=now，不会误淘汰）
                if (MaxLifetime > 0 && pi.CreatedTime.AddSeconds(MaxLifetime) < TimerX.Now)
                {
                    OnDispose(pi.Value);
                    continue;
                }

                if (OnGet(pi.Value))
                    return FinishAcquire(pi);

                OnDispose(pi.Value);
            }
        }
        catch
        {
            slot?.Release();
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
        // 获取槽位。借出持有、归还释放，信号量容量=Max 保证总连接数 Busy+Free≤Max
        var slot = GetSlot();
        if (slot != null && !await slot.WaitAsync(WaitTimeout, cancellationToken).ConfigureAwait(false))
        {
            using var span = DefaultTracer.Instance?.NewSpan($"pool:{Name}:Full", new { Name, BusyCount, Max, WaitTimeout });
            throw new PoolFullException(Name, BusyCount, Max);
        }

        // 已持有槽位。循环内取消或 OnCreateAsync/OnGetAsync 抛异常时，catch 释放槽位避免泄漏；成功 return 不释放
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 获取一个 Item（优先从空闲池复用，否则新建）
                if (!TryAcquireFree(out var pi))
                    pi = new Item { Value = await OnCreateAsync(cancellationToken).ConfigureAwait(false), CreatedTime = TimerX.Now };

                if (pi.Value == null) continue;

                // 生命周期检查（新建项 CreatedTime=now，不会误淘汰）
                if (MaxLifetime > 0 && pi.CreatedTime.AddSeconds(MaxLifetime) < TimerX.Now)
                {
                    OnDispose(pi.Value);
                    continue;
                }

                if (await OnGetAsync(pi.Value, cancellationToken).ConfigureAwait(false))
                    return FinishAcquire(pi);

                OnDispose(pi.Value);
            }
        }
        catch
        {
            slot?.Release();
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

        // 借出时持有的槽位，无论成功入池、销毁还是钩子抛异常，都在 finally 释放一次并唤醒等待者
        try
        {
            if (!OnReturn(value) || value is DisposeBase db && db.Disposed)
            {
                OnDispose(value);
                return false;
            }

            // 如果空闲数不足最小值，则返回到基础空闲集合（热栈），否则进扩展集合（冷队列）
            if (_FreeCount < Min)
                _free.Push(pi);
            else
                _free2.Enqueue(pi);

            // 最后时间
            pi.LastTime = TimerX.Now;

            Interlocked.Increment(ref _FreeCount);

            // 启动定期清理的定时器（仅在需要清理时）
            if (IdleTime > 0) StartTimer();

            return true;
        }
        finally
        {
            // 释放槽位，唤醒等待者（Max≤0 时无信号量，GetSlot 返回 null 跳过）
            GetSlot()?.Release();
        }
    }

    /// <summary>归还时是否可用</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    protected virtual Boolean OnReturn(T value) => true;

    /// <summary>清空已有对象</summary>
    public virtual Int32 Clear()
    {
        var count = _FreeCount + _BusyCount;

        // 空闲项已归还（槽位已释放），直接销毁
        while (_free.TryPop(out var pi)) OnDispose(pi.Value);
        while (_free2.TryDequeue(out var pi)) OnDispose(pi.Value);
        Interlocked.Exchange(ref _FreeCount, 0);

        // 繁忙项尚未归还（仍持有槽位），销毁后释放槽位；空闲项不持有槽位，不释放
        foreach (var item in _busy)
        {
            OnDispose(item.Key);
            GetSlot()?.Release();
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
            var times = 100;
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

        // 清理热栈 _free 中的过期项
        // Min 连接受 IdleTime 保护（空闲不淘汰），但不受 MaxLifetime 保护（超龄必淘汰）
        if (!_free.IsEmpty)
        {
            var buffer = new List<Item>();
            while (_free.TryPop(out var pi)) buffer.Add(pi);

            var total = FreeCount + BusyCount;
            foreach (var pi in buffer)
            {
                var expired = (IdleTime > 0 && pi.LastTime < idleExp && total > Min)
                    || (MaxLifetime > 0 && pi.CreatedTime.AddSeconds(MaxLifetime) < now);
                if (expired)
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