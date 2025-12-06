using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Security;

namespace NewLife.Caching;

/// <summary>缓存</summary>
/// <remarks>
/// 基础抽象，定义通用缓存语义：
/// <list type="bullet">
/// <item><description><see cref="Expire"/> 作为默认 TTL（秒），当各 Set 方法传入 <c>expire &lt; 0</c> 时使用。</description></item>
/// <item><description><c>expire == 0</c> 表示永不过期；<c>&gt; 0</c> 表示从当前起的相对过期（TTL）。</description></item>
/// <item><description>实现可采用惰性过期策略，已过期键在访问时才清理。</description></item>
/// <item><description>远端 / 分布式实现中，<see cref="Keys"/>、<see cref="Count"/>、<see cref="Search(String, Int32, Int32)"/> 可能是高复杂度操作，应仅用于诊断。</description></item>
/// </list>
/// </remarks>
public abstract class Cache : DisposeBase, ICache
{
    #region 静态默认实现
    /// <summary>默认缓存</summary>
    public static ICache Default { get; set; } = new MemoryCache();
    #endregion

    #region 属性
    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>默认过期时间。避免 Set 操作时没有设置过期时间，默认 0 秒表示不过期</summary>
    /// <remarks>当 Set 相关 API 传入 <c>expire &lt; 0</c> 时使用该值。</remarks>
    public Int32 Expire { get; set; }

    /// <summary>获取和设置缓存，使用默认过期时间</summary>
    /// <param name="key">键</param>
    /// <remarks>
    /// 通过索引器设置的键等价于 Set(key, value, -1)，即使用 <see cref="Expire"/>；若未设置默认过期时间（为 0）则表示永不过期。
    /// </remarks>
    public virtual Object? this[String key] { get => Get<Object>(key); set => Set(key, value); }

    /// <summary>缓存个数</summary>
    public abstract Int32 Count { get; }

    /// <summary>所有键</summary>
    public abstract ICollection<String> Keys { get; }
    #endregion

    #region 构造
    /// <summary>构造函数</summary>
    protected Cache() => Name = GetType().Name.TrimEnd("Cache");

    /// <summary>销毁。释放资源</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        _keys = null;
        //_keys2 = null;
    }
    #endregion

    #region 基础操作
    /// <summary>使用连接字符串初始化配置</summary>
    /// <param name="config">连接/配置字符串；不同实现自行解析</param>
    public virtual void Init(String config) { }

    /// <summary>是否包含缓存项</summary>
    /// <param name="key">键</param>
    public abstract Boolean ContainsKey(String key);

    /// <summary>设置缓存项</summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <param name="expire">过期时间，秒。&lt;0 使用 <see cref="Expire"/>；0 永不过期；&gt;0 相对过期</param>
    public abstract Boolean Set<T>(String key, T value, Int32 expire = -1);

    /// <summary>设置缓存项</summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <param name="expire">过期时间，相对过期。<see cref="TimeSpan.Zero"/> 永不过期</param>
    public virtual Boolean Set<T>(String key, T value, TimeSpan expire) => Set(key, value, (Int32)expire.TotalSeconds);

    /// <summary>获取缓存项</summary>
    /// <param name="key">键</param>
    /// <returns></returns>
    [return: MaybeNull]
    public abstract T Get<T>(String key);

    /// <summary>移除缓存项。支持 * 与 ? 模式匹配</summary>
    /// <param name="key">键或模式</param>
    public abstract Int32 Remove(String key);

    /// <summary>批量移除缓存项。支持 * 与 ? 模式匹配</summary>
    /// <param name="keys">键或模式集合</param>
    public abstract Int32 Remove(params String[] keys);

    /// <summary>清空所有缓存项</summary>
    public virtual void Clear() => throw new NotSupportedException();

    /// <summary>设置缓存项有效期</summary>
    /// <param name="key">键</param>
    /// <param name="expire">过期时间，TTL。<see cref="TimeSpan.Zero"/> 永不过期</param>
    public abstract Boolean SetExpire(String key, TimeSpan expire);

    /// <summary>获取缓存项有效期</summary>
    /// <param name="key">键</param>
    /// <returns>剩余 TTL；建议：永不过期返回 <see cref="TimeSpan.Zero"/>；不存在或已过期返回负值</returns>
    public abstract TimeSpan GetExpire(String key);
    #endregion

    #region 集合操作
    /// <summary>批量获取缓存项</summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="keys">键集合</param>
    public virtual IDictionary<String, T?> GetAll<T>(IEnumerable<String> keys)
    {
        var dic = new Dictionary<String, T?>();
        foreach (var key in keys)
        {
            dic[key] = Get<T>(key);
        }

        return dic;
    }

    /// <summary>批量设置缓存项</summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="values">键值对集合</param>
    /// <param name="expire">过期时间，秒。&lt;0 使用默认，0 永不过期</param>
    public virtual void SetAll<T>(IDictionary<String, T> values, Int32 expire = -1)
    {
        foreach (var item in values)
        {
            Set(item.Key, item.Value, expire);
        }
    }

    /// <summary>获取列表</summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="key">键</param>
    /// <returns></returns>
    public virtual IList<T> GetList<T>(String key) => throw new NotSupportedException();

    /// <summary>获取哈希</summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="key">键</param>
    /// <returns></returns>
    public virtual IDictionary<String, T> GetDictionary<T>(String key) => throw new NotSupportedException();

    /// <summary>获取队列</summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="key">键</param>
    /// <returns></returns>
    public virtual IProducerConsumer<T> GetQueue<T>(String key) => throw new NotSupportedException();

    /// <summary>获取栈</summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="key">键</param>
    /// <returns></returns>
    public virtual IProducerConsumer<T> GetStack<T>(String key) => throw new NotSupportedException();

    /// <summary>获取 Set</summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="key">键</param>
    public virtual ICollection<T> GetSet<T>(String key) => throw new NotSupportedException();

    /// <summary>获取事件总线，可发布消息或订阅消息</summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="topic">事件主题</param>
    /// <param name="clientId">客户标识/消息分组</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public virtual IEventBus<T> GetEventBus<T>(String topic, String clientId = "") => throw new NotSupportedException();
    #endregion

    #region 高级操作
    /// <summary>添加，已存在时不更新</summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <param name="expire">过期时间，秒。&lt;0 使用默认</param>
    public virtual Boolean Add<T>(String key, T value, Int32 expire = -1)
    {
        if (ContainsKey(key)) return false;

        return Set(key, value, expire);
    }

    /// <summary>设置新值并获取旧值，原子操作</summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <returns></returns>
    [return: MaybeNull]
    public virtual T Replace<T>(String key, T value)
    {
        var rs = Get<T>(key);
        Set(key, value);
        return rs;
    }

    /// <summary>尝试获取指定键，返回是否包含值</summary>
    /// <remarks>
    /// 当返回 <c>false</c> 时一定不存在；返回 <c>true</c> 时，<paramref name="value"/> 可能为默认值（例如存储的是 0 / null）。
    /// 默认实现：先 Get，若返回非默认值直接成功；否则再调用 ContainsKey 区分"默认值"与"不存在"。
    /// </remarks>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="value">输出值（可能为默认值）</param>
    public virtual Boolean TryGetValue<T>(String key, [MaybeNullWhen(false)] out T value)
    {
        value = Get<T>(key)!;
        if (!Equals(value, default(T))) return true;

        return ContainsKey(key);
    }

    /// <summary>获取 或 添加 缓存数据，在数据不存在时执行委托请求数据</summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="callback">缺失时构造函数</param>
    /// <param name="expire">过期时间，秒。&lt;0 使用默认</param>
    [return: MaybeNull]
    public virtual T GetOrAdd<T>(String key, Func<String, T> callback, Int32 expire = -1)
    {
        var value = Get<T>(key);
        if (!Equals(value, default(T))) return value;

        if (ContainsKey(key)) return value;

        value = callback(key);

        if (expire < 0) expire = Expire;
        if (Add(key, value, expire)) return value;

        return Get<T>(key);
    }

    /// <summary>累加，原子操作</summary>
    /// <param name="key">键</param>
    /// <param name="value">变化量</param>
    /// <returns></returns>
    public virtual Int64 Increment(String key, Int64 value)
    {
        lock (this)
        {
            var v = Get<Int64>(key);
            v += value;
            Set(key, v);

            return v;
        }
    }

    /// <summary>累加，原子操作</summary>
    /// <param name="key">键</param>
    /// <param name="value">变化量</param>
    /// <returns></returns>
    public virtual Double Increment(String key, Double value)
    {
        lock (this)
        {
            var v = Get<Double>(key);
            v += value;
            Set(key, v);

            return v;
        }
    }

    /// <summary>递减，原子操作</summary>
    /// <param name="key">键</param>
    /// <param name="value">变化量</param>
    /// <returns></returns>
    public virtual Int64 Decrement(String key, Int64 value)
    {
        lock (this)
        {
            var v = Get<Int64>(key);
            v -= value;
            Set(key, v);

            return v;
        }
    }

    /// <summary>递减，原子操作</summary>
    /// <param name="key">键</param>
    /// <param name="value">变化量</param>
    /// <returns></returns>
    public virtual Double Decrement(String key, Double value)
    {
        lock (this)
        {
            var v = Get<Double>(key);
            v -= value;
            Set(key, v);

            return v;
        }
    }

    /// <summary>累加并获取过期时间，原子操作</summary>
    /// <remarks>
    /// 适用于需要同时获取累加结果和剩余过期时间的场景，如限流计数器。
    /// 远端实现（如 Redis）可重写此方法使用 Pipeline 实现单次往返。
    /// </remarks>
    /// <param name="key">键</param>
    /// <param name="value">变化量</param>
    /// <returns>元组，Item1为累加后的值，Item2为剩余过期时间（秒），-1表示永不过期，-2表示键不存在</returns>
    public virtual (Int64 Value, Int32 Ttl) IncrementWithTtl(String key, Int64 value = 1)
    {
        var result = Increment(key, value);
        var expire = GetExpire(key);

        // 转换过期时间：TimeSpan.Zero 表示永不过期（-1），负值表示不存在（-2）
        var ttl = expire < TimeSpan.Zero ? -2 : (expire == TimeSpan.Zero ? -1 : (Int32)expire.TotalSeconds);

        return (result, ttl);
    }

    /// <summary>累加并获取过期时间，原子操作（浮点）</summary>
    /// <remarks>
    /// 适用于需要同时获取累加结果和剩余过期时间的场景。
    /// 远端实现（如 Redis）可重写此方法使用 Pipeline 实现单次往返。
    /// </remarks>
    /// <param name="key">键</param>
    /// <param name="value">变化量</param>
    /// <returns>元组，Item1为累加后的值，Item2为剩余过期时间（秒），-1表示永不过期，-2表示键不存在</returns>
    public virtual (Double Value, Int32 Ttl) IncrementWithTtl(String key, Double value)
    {
        var result = Increment(key, value);
        var expire = GetExpire(key);

        // 转换过期时间：TimeSpan.Zero 表示永不过期（-1），负值表示不存在（-2）
        var ttl = expire < TimeSpan.Zero ? -2 : (expire == TimeSpan.Zero ? -1 : (Int32)expire.TotalSeconds);

        return (result, ttl);
    }

    /// <summary>递减并获取过期时间，原子操作</summary>
    /// <remarks>
    /// 适用于需要同时获取递减结果和剩余过期时间的场景。
    /// 远端实现（如 Redis）可重写此方法使用 Pipeline 实现单次往返。
    /// </remarks>
    /// <param name="key">键</param>
    /// <param name="value">变化量</param>
    /// <returns>元组，Item1为递减后的值，Item2为剩余过期时间（秒），-1表示永不过期，-2表示键不存在</returns>
    public virtual (Int64 Value, Int32 Ttl) DecrementWithTtl(String key, Int64 value = 1)
    {
        var result = Decrement(key, value);
        var expire = GetExpire(key);

        // 转换过期时间：TimeSpan.Zero 表示永不过期（-1），负值表示不存在（-2）
        var ttl = expire < TimeSpan.Zero ? -2 : (expire == TimeSpan.Zero ? -1 : (Int32)expire.TotalSeconds);

        return (result, ttl);
    }

    /// <summary>递减并获取过期时间，原子操作（浮点）</summary>
    /// <remarks>
    /// 适用于需要同时获取递减结果和剩余过期时间的场景。
    /// 远端实现（如 Redis）可重写此方法使用 Pipeline 实现单次往返。
    /// </remarks>
    /// <param name="key">键</param>
    /// <param name="value">变化量</param>
    /// <returns>元组，Item1为递减后的值，Item2为剩余过期时间（秒），-1表示永不过期，-2表示键不存在</returns>
    public virtual (Double Value, Int32 Ttl) DecrementWithTtl(String key, Double value) => IncrementWithTtl(key, -value);

    /// <summary>搜索键</summary>
    /// <param name="pattern">匹配字符串。一般支持 *，远端实现可支持 ?</param>
    /// <param name="offset">开始行（分页偏移）</param>
    /// <param name="count">搜索个数。默认 -1 表示全部</param>
    public virtual IEnumerable<String> Search(String pattern, Int32 offset = 0, Int32 count = -1) => [];
    #endregion

    #region 事务
    /// <summary>提交变更（部分提供者需要刷盘）</summary>
    public virtual Int32 Commit() => 0;

    /// <summary>申请分布式锁（简易版）。使用完以后需要主动释放</summary>
    /// <remarks>
    /// 需要注意，使用完锁后需调用Dispose方法以释放锁，申请与释放一定是成对出现。
    /// 
    /// 线程A申请得到锁key以后，在A主动释放锁或者到达超时时间msTimeout之前，其它线程B申请这个锁都会被阻塞。
    /// 线程B申请锁key的最大阻塞时间为msTimeout，在到期之前，如果线程A主动释放锁或者A锁定key的超时时间msTimeout已到，那么线程B会抢到锁。
    /// 
    /// <code>
    /// using var rlock = cache.AcquireLock("myKey", 5000);
    /// //todo 需要分布式锁保护的代码
    /// rlock.Dispose(); //释放锁。也可以在using语句结束时自动释放
    /// </code>
    /// 简化语义：等待时间 = 锁持有时间（<paramref name="msTimeout"/>）。相当于 <see cref="AcquireLock(String, Int32, Int32, Boolean)"/> 中 <c>msExpire = msTimeout</c>。
    /// </remarks>
    /// <param name="key">锁键</param>
    /// <param name="msTimeout">等待及过期时间（毫秒）</param>
    public virtual IDisposable? AcquireLock(String key, Int32 msTimeout)
    {
        var rlock = new CacheLock(this, key);
        if (!rlock.Acquire(msTimeout, msTimeout)) throw new InvalidOperationException($"Lock [{key}] failed! msTimeout={msTimeout}");

        return rlock;
    }

    /// <summary>申请分布式锁。使用完以后需要主动释放</summary>
    /// <remarks>
    /// 需要注意，使用完锁后需调用Dispose方法以释放锁，申请与释放一定是成对出现。
    /// 
    /// 线程A申请得到锁key以后，在A主动释放锁或者到达超时时间msExpire之前，其它线程B申请这个锁都会被阻塞。
    /// 线程B申请锁key的最大阻塞时间为msTimeout，在到期之前，如果线程A主动释放锁或者A锁定key的超时时间msExpire已到，那么线程B会抢到锁。
    /// 
    /// <code>
    /// using var rlock = cache.AcquireLock("myKey", 5000, 15000, false);
    /// if (rlock ==null) throw new Exception("申请锁失败！");
    /// //todo 需要分布式锁保护的代码
    /// rlock.Dispose(); //释放锁。也可以在using语句结束时自动释放
    /// </code>
    /// </remarks>
    /// <param name="key">要锁定的key</param>
    /// <param name="msTimeout">锁等待时间，申请加锁时如果遇到冲突则等待的最大时间，单位毫秒</param>
    /// <param name="msExpire">锁过期时间，超过该时间如果没有主动释放则自动释放锁，必须整数秒，单位毫秒</param>
    /// <param name="throwOnFailure">失败时是否抛出异常，如果不抛出异常，可通过返回null得知申请锁失败</param>
    /// <returns></returns>
    public virtual IDisposable? AcquireLock(String key, Int32 msTimeout, Int32 msExpire, Boolean throwOnFailure)
    {
        var rlock = new CacheLock(this, key);
        if (!rlock.Acquire(msTimeout, msExpire))
        {
            if (throwOnFailure) throw new InvalidOperationException($"Lock [{key}] failed! msTimeout={msTimeout}");

            return null;
        }

        return rlock;
    }
    #endregion

    #region 性能测试
    /// <summary>多线程性能测试</summary>
    /// <param name="rand">随机读写。顺序，每个线程多次操作一个key；随机，每个线程每次操作不同key</param>
    /// <param name="batch">批量操作。默认0不分批，分批仅针对随机读写，对顺序读写的单key操作没有意义</param>
    /// <remarks>
    /// Memory性能测试[顺序]，逻辑处理器 32 个 2,000MHz Intel(R) Xeon(R) CPU E5-2640 v2 @ 2.00GHz
    /// 
    /// 测试 10,000,000 项，  1 线程
    /// 赋值 10,000,000 项，  1 线程，耗时   3,764ms 速度 2,656,748 ops
    /// 读取 10,000,000 项，  1 线程，耗时   1,296ms 速度 7,716,049 ops
    /// 删除 10,000,000 项，  1 线程，耗时   1,230ms 速度 8,130,081 ops
    /// 
    /// 测试 20,000,000 项，  2 线程
    /// 赋值 20,000,000 项，  2 线程，耗时   3,088ms 速度 6,476,683 ops
    /// 读取 20,000,000 项，  2 线程，耗时   1,051ms 速度 19,029,495 ops
    /// 删除 20,000,000 项，  2 线程，耗时   1,011ms 速度 19,782,393 ops
    /// 
    /// 测试 40,000,000 项，  4 线程
    /// 赋值 40,000,000 项，  4 线程，耗时   3,060ms 速度 13,071,895 ops
    /// 读取 40,000,000 项，  4 线程，耗时   1,023ms 速度 39,100,684 ops
    /// 删除 40,000,000 项，  4 线程，耗时     994ms 速度 40,241,448 ops
    /// 
    /// 测试 80,000,000 项，  8 线程
    /// 赋值 80,000,000 项，  8 线程，耗时   3,124ms 速度 25,608,194 ops
    /// 读取 80,000,000 项，  8 线程，耗时   1,171ms 速度 68,317,677 ops
    /// 删除 80,000,000 项，  8 线程，耗时   1,199ms 速度 66,722,268 ops
    /// 
    /// 测试 320,000,000 项， 32 线程
    /// 赋值 320,000,000 项， 32 线程，耗时  13,857ms 速度 23,093,021 ops
    /// 读取 320,000,000 项， 32 线程，耗时   1,950ms 速度 164,102,564 ops
    /// 删除 320,000,000 项， 32 线程，耗时   3,359ms 速度 95,266,448 ops
    /// 
    /// 测试 320,000,000 项， 64 线程
    /// 赋值 320,000,000 项， 64 线程，耗时   9,648ms 速度 33,167,495 ops
    /// 读取 320,000,000 项， 64 线程，耗时   1,974ms 速度 162,107,396 ops
    /// 删除 320,000,000 项， 64 线程，耗时   1,907ms 速度 167,802,831 ops
    /// 
    /// 测试 320,000,000 项，256 线程
    /// 赋值 320,000,000 项，256 线程，耗时  12,429ms 速度 25,746,238 ops
    /// 读取 320,000,000 项，256 线程，耗时   1,907ms 速度 167,802,831 ops
    /// 删除 320,000,000 项，256 线程，耗时   2,350ms 速度 136,170,212 ops
    /// </remarks>
    public virtual Int64 Bench(Boolean rand = false, Int32 batch = 0)
    {
        var cpu = Environment.ProcessorCount;
        XTrace.WriteLine($"{Name}性能测试[{(rand ? "随机" : "顺序")}]，批大小[{batch}]，逻辑处理器 {cpu:n0} 个");

        var rs = 0L;
        var times = GetTimesPerThread(rand, batch);

        // 提前准备Keys，减少性能测试中的干扰
        var key = "b_";
        var max = cpu > 64 ? cpu : 64;
        var maxTimes = times * max;
        if (!rand) maxTimes = max;
        _keys = new String[maxTimes];

        var sb = new StringBuilder();
        for (var i = 0; i < _keys.Length; i++)
        {
            sb.Clear();
            sb.Append(key);
            sb.Append(i);
            _keys[i] = sb.ToString();
        }

        // 单线程
        rs += BenchOne(_keys, times, 1, rand, batch);

        // 多线程
        if (cpu != 2) rs += BenchOne(_keys, times * 2, 2, rand, batch);
        if (cpu != 4) rs += BenchOne(_keys, times * 4, 4, rand, batch);
        if (cpu != 8) rs += BenchOne(_keys, times * 8, 8, rand, batch);

        // CPU个数
        rs += BenchOne(_keys, times * cpu, cpu, rand, batch);

        //// 1.5倍
        //var cpu2 = cpu * 3 / 2;
        //if (!(new[] { 2, 4, 8, 64, 256 }).Contains(cpu2)) BenchOne(times * cpu2, cpu2, rand);

        // 最大
        if (cpu < 64) rs += BenchOne(_keys, times * cpu, 64, rand, batch);
        //if (cpu * 8 >= 256) BenchOne(times * cpu, cpu * 8, rand);

        return rs;
    }

    /// <summary>获取每个线程测试次数</summary>
    /// <param name="rand"></param>
    /// <param name="batch"></param>
    /// <returns></returns>
    protected virtual Int32 GetTimesPerThread(Boolean rand, Int32 batch) => 10_000;

    private String[]? _keys;
    /// <summary>使用指定线程测试指定次数</summary>
    /// <param name="keys"></param>
    /// <param name="times">次数</param>
    /// <param name="threads">线程</param>
    /// <param name="rand">随机读写</param>
    /// <param name="batch">批量操作</param>
    public virtual Int64 BenchOne(String[] keys, Int64 times, Int32 threads, Boolean rand, Int32 batch)
    {
        if (threads <= 0) threads = Environment.ProcessorCount;
        if (times <= 0) times = threads * 1_000;

        XTrace.WriteLine($"测试 {times:n0} 项，{threads,3:n0} 线程");

        var rs = 3L;

        // 提前执行一次网络操作，预热链路
        var key = keys[0];
        Set(key, Rand.NextString(32));
        _ = Get<String>(key);
        Remove(key);

        // 赋值测试
        rs += BenchSet(keys, times, threads, rand, batch);

        // 读取测试
        rs += BenchGet(keys, times, threads, rand, batch);

        // 删除测试
        rs += BenchRemove(keys, times, threads, rand, batch);

        // 累加测试
        rs += BenchInc(keys, times, threads, rand, batch);

        return rs;
    }

    /// <summary>读取测试</summary>
    /// <param name="keys">键</param>
    /// <param name="times">次数</param>
    /// <param name="threads">线程</param>
    /// <param name="rand">随机读写</param>
    /// <param name="batch">批量操作</param>
    protected virtual Int64 BenchGet(String[] keys, Int64 times, Int32 threads, Boolean rand, Int32 batch)
    {
        // 提前执行一次网络操作，预热链路
        var v = Get<String>(keys[0]);

        var sw = Stopwatch.StartNew();
        if (rand)
        {
            // 随机操作，每个线程每次操作不同key，跳跃式
            Parallel.For(0, threads, k =>
            {
                if (batch == 0)
                {
                    for (var i = k; i < times; i += threads)
                    {
                        var val = Get<String>(keys[i % keys.Length]);
                    }
                }
                else
                {
                    var n = 0;
                    var keys2 = new String[batch];
                    for (var i = k; i < times; i += threads)
                    {
                        keys2[n++] = keys[i % keys.Length];

                        if (n >= batch)
                        {
                            var vals = GetAll<String>(keys2);
                            n = 0;
                        }
                    }
                    if (n > 0)
                    {
                        var vals = GetAll<String>(keys2.Take(n));
                    }
                }
            });
        }
        else
        {
            // 顺序操作，每个线程多次操作同一个key
            Parallel.For(0, threads, k =>
            {
                var mykey = keys[k];
                var count = times / threads;
                for (var i = 0; i < count; i++)
                {
                    var val = Get<String>(mykey);
                }
            });
        }
        sw.Stop();

        var speed = times * 1000 / sw.ElapsedMilliseconds;
        XTrace.WriteLine($"读取 耗时 {sw.ElapsedMilliseconds,7:n0}ms 速度 {speed,11:n0} ops");

        return times + 1;
    }

    /// <summary>赋值测试</summary>
    /// <param name="keys">键</param>
    /// <param name="times">次数</param>
    /// <param name="threads">线程</param>
    /// <param name="rand">随机读写</param>
    /// <param name="batch">批量操作</param>
    protected virtual Int64 BenchSet(String[] keys, Int64 times, Int32 threads, Boolean rand, Int32 batch)
    {
        Set(keys[0], Rand.NextString(32));

        var sw = Stopwatch.StartNew();
        if (rand)
        {
            // 随机操作，每个线程每次操作不同key，跳跃式
            Parallel.For(0, threads, k =>
            {
                var val = Rand.NextString(8);
                if (batch == 0)
                {
                    for (var i = k; i < times; i += threads)
                    {
                        Set(keys[i % keys.Length], val);
                    }
                }
                else
                {
                    var n = 0;
                    var dic = new Dictionary<String, String>();
                    for (var i = k; i < times; i += threads)
                    {
                        dic[keys[i % keys.Length]] = val;
                        n++;

                        if (n >= batch)
                        {
                            SetAll(dic);
                            dic.Clear();
                            n = 0;
                        }
                    }
                    if (n > 0)
                    {
                        SetAll(dic);
                    }
                }

                // 提交变更
                Commit();
            });
        }
        else
        {
            // 顺序操作，每个线程多次操作同一个key
            Parallel.For(0, threads, k =>
            {
                var mykey = keys[k];
                var val = Rand.NextString(8);
                var count = times / threads;
                for (var i = 0; i < count; i++)
                {
                    Set(mykey, val);
                }

                // 提交变更
                Commit();
            });
        }
        sw.Stop();

        var speed = times * 1000 / sw.ElapsedMilliseconds;
        XTrace.WriteLine($"赋值 耗时 {sw.ElapsedMilliseconds,7:n0}ms 速度 {speed,11:n0} ops");

        return times + 1;
    }

    /// <summary>删除测试</summary>
    /// <param name="keys">键</param>
    /// <param name="times">次数</param>
    /// <param name="threads">线程</param>
    /// <param name="rand">随机读写</param>
    /// <param name="batch">批量操作</param>
    protected virtual Int64 BenchRemove(String[] keys, Int64 times, Int32 threads, Boolean rand, Int32 batch)
    {
        // 提前执行一次网络操作，预热链路
        Remove(keys[0]);

        var sw = Stopwatch.StartNew();
        if (rand)
        {
            // 随机操作，每个线程每次操作不同key，跳跃式
            Parallel.For(0, threads, k =>
            {
                if (batch == 0)
                {
                    for (var i = k; i < times; i += threads)
                    {
                        Remove(keys[i % keys.Length]);
                    }
                }
                else
                {
                    var n = 0;
                    var keys2 = new String[batch];
                    for (var i = k; i < times; i += threads)
                    {
                        keys2[n++] = keys[i % keys.Length];

                        if (n >= batch)
                        {
                            Remove(keys2);
                            n = 0;
                        }
                    }
                    if (n > 0)
                    {
                        Remove(keys2.Take(n).ToArray());
                    }
                }

                // 提交变更
                Commit();
            });
        }
        else
        {
            // 顺序操作，每个线程多次操作同一个key
            Parallel.For(0, threads, k =>
            {
                var mykey = keys[k];
                var count = times / threads;
                for (var i = 0; i < count; i++)
                {
                    Remove(mykey);
                }

                // 提交变更
                Commit();
            });
        }
        sw.Stop();

        var speed = times * 1000 / sw.ElapsedMilliseconds;
        XTrace.WriteLine($"删除 耗时 {sw.ElapsedMilliseconds,7:n0}ms 速度 {speed,11:n0} ops");

        return times + 1;
    }

    /// <summary>累加测试</summary>
    /// <param name="keys">键</param>
    /// <param name="times">次数</param>
    /// <param name="threads">线程</param>
    /// <param name="rand">随机读写</param>
    /// <param name="batch">批量操作</param>
    protected virtual Int64 BenchInc(String[] keys, Int64 times, Int32 threads, Boolean rand, Int32 batch)
    {
        // 提前执行一次网络操作，预热链路
        Increment(keys[0], 1);

        var sw = Stopwatch.StartNew();
        if (rand)
        {
            // 随机操作，每个线程每次操作不同key，跳跃式
            Parallel.For(0, threads, k =>
            {
                var val = Rand.Next(100);
                for (var i = k; i < times; i += threads)
                {
                    Increment(keys[i % keys.Length], val);
                }

                // 提交变更
                Commit();
            });
        }
        else
        {
            // 顺序操作，每个线程多次操作同一个key
            Parallel.For(0, threads, k =>
            {
                var mykey = keys[k];
                var val = Rand.Next(100);
                var count = times / threads;
                for (var i = 0; i < count; i++)
                {
                    Increment(mykey, val);
                }

                // 提交变更
                Commit();
            });
        }
        sw.Stop();

        var speed = times * 1000 / sw.ElapsedMilliseconds;
        XTrace.WriteLine($"累加 耗时 {sw.ElapsedMilliseconds,7:n0}ms 速度 {speed,11:n0} ops");

        return times + 1;
    }
    #endregion

    #region 辅助
    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => Name;
    #endregion
}