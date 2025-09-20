using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using NewLife.Reflection;

namespace NewLife.Collections;

/// <summary>轻量级对象池。数组无锁实现，高性能</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/object_pool
///
/// 设计说明：
/// - 存储结构采用 1 + N：热点槽位 <see cref="_current"/> 放置最热对象，内部数组存其余对象；
/// - 使用 <see cref="Interlocked.CompareExchange{T}(ref T, T, T)"/> 对热点槽位与数组元素进行无锁并发抢占/归还；
/// - 数组查找 O(N)，且为结构体持有引用，避免对象级别的额外分配；
/// - 通过延迟初始化（双检 + 轻量锁）避免冷启动分配；
/// - 可选择在二代 GC 触发时定期清理，降低长时间闲置的内存占用（限速 60 秒一次）。
///
/// 线程安全说明：
/// - <see cref="Get"/> 与 <see cref="Return(T)"/> 对热点槽位与数组元素的读写使用 CAS，无需锁；
/// - <see cref="Init"/> 仅在首次分配时进入锁，后续不再加锁；
/// - <see cref="Clear"/> 与 <see cref="Get"/> / <see cref="Return(T)"/> 可并发执行，依靠局部快照与空值判定避免空引用；
/// - 初始化后修改 <see cref="Max"/> 不会影响已分配的内部数组容量，此为设计约束。
///
/// 兼容性：支持 .NET Framework 4.5 起与 .NET Standard / .NET Core / .NET 5+ 多目标。
/// </remarks>
/// <typeparam name="T">池化的引用类型</typeparam>
public class Pool<T> : IPool<T> where T : class
{
    #region 属性
    /// <summary>对象池大小。默认 CPU*2，初始化后改变无效</summary>
    public Int32 Max { get; set; }

    private Item[]? _items;
    private T? _current;

    // 专用同步对象。避免在 Init 阶段锁定 this，降低被外部误锁的风险
    private readonly Object _sync = new();

    struct Item
    {
        public T? Value;
    }
    #endregion

    #region 构造
    /// <summary>实例化对象池。默认大小 CPU*2，且最小为 8</summary>
    /// <param name="max">最大对象数。小于等于 0 使用 CPU*2；小于 8 则固定为 8</param>
    public Pool(Int32 max = 0)
    {
        if (max <= 0) max = Environment.ProcessorCount * 2;
        if (max < 8) max = 8;

        Max = max;
    }

    /// <summary>实例化对象池，并在 GC 第二代触发时尝试清理</summary>
    /// <param name="max">最大对象数。默认大小 CPU*2</param>
    /// <param name="useGcClear">是否在二代 GC 触发时清理池里对象（限速 60 秒一次）</param>
    protected Pool(Int32 max, Boolean useGcClear) : this(max)
    {
        if (useGcClear) Gen2GcCallback.Register(s => (s as Pool<T>)!.OnGen2(), this);
    }

    private Int64 _next;
    private Boolean OnGen2()
    {
        var now = Runtime.TickCount64;
        if (_next <= 0)
            _next = now + 60000;
        else if (_next < now)
        {
            Clear();
            _next = now + 60000;
        }

        return true;
    }

    [MemberNotNull(nameof(_items))]
    private Item[] Init()
    {
        // 双检锁：仅首次分配进入临界区。
        if (_items == null)
        {
            lock (_sync)
            {
                _items ??= new Item[Max - 1];
            }
        }

        return _items;
    }
    #endregion

    #region 方法
    /// <summary>获取一个实例；若池中为空则创建</summary>
    /// <returns>对象实例</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual T Get()
    {
        // 先尝试获取热点槽位，最快路径
        var val = _current;
        if (val != null && Interlocked.CompareExchange(ref _current, null, val) == val) return val;

        // 扫描内部数组
        var items = Init();
        for (var i = 0; i < items.Length; i++)
        {
            val = items[i].Value;
            if (val != null && Interlocked.CompareExchange(ref items[i].Value, null, val) == val) return val;
        }

        // 未命中，按策略创建新实例
        var rs = OnCreate();
        if (rs == null) throw new InvalidOperationException($"[Pool] Unable to create an instance of [{typeof(T).FullName}]");

        return rs;
    }

    /// <summary>归还</summary>
    /// <param name="value">归还的对象实例</param>
    /// <returns>是否成功放入池中；池满返回 false</returns>
    [Obsolete("Please use Return from 2024-02-01")]
    public virtual Boolean Put(T value) => Return(value);

    /// <summary>归还</summary>
    /// <param name="value">归还的对象实例</param>
    /// <returns>是否成功放入池中；池满返回 false</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual Boolean Return(T value)
    {
        // 先尝试放入热点槽位
        if (_current == null && Interlocked.CompareExchange(ref _current, value, null) == null) return true;

        // 再尝试放入内部数组
        var items = Init();
        for (var i = 0; i < items.Length; ++i)
        {
            if (Interlocked.CompareExchange(ref items[i].Value, value, null) == null) return true;
        }

        // 池已满
        return false;
    }

    /// <summary>清空对象池，返回被清理的对象数量</summary>
    /// <returns>清理的数量</returns>
    public virtual Int32 Clear()
    {
        var count = 0;

        // 清理热点槽位
        if (_current != null)
        {
            _current = null;
            count++;
        }

        // 快照内部数组，避免并发期间替换导致的空引用
        var items = _items;
        if (items == null) return count;

        for (var i = 0; i < items.Length; ++i)
        {
            if (items[i].Value != null)
            {
                items[i].Value = null;
                count++;
            }
        }
        _items = null;

        return count;
    }
    #endregion

    #region 重载
    /// <summary>
    /// 创建实例。默认使用反射创建，可在子类中重写以自定义创建策略。
    /// 建议在高频场景中于子类缓存构造委托以降低反射开销。
    /// </summary>
    /// <returns>新建对象实例；允许返回 null（将抛出异常）</returns>
    protected virtual T? OnCreate() => typeof(T).CreateInstance() as T;
    #endregion
}