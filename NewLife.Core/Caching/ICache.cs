using System.Diagnostics.CodeAnalysis;

namespace NewLife.Caching;

/// <summary>缓存接口</summary>
/// <remarks>
/// <para>文档：https://newlifex.com/core/icache</para>
/// 
/// <para><strong>通用语义约定：</strong></para>
/// <list type="bullet">
/// <item><description><strong>过期时间：</strong>单位为秒（除非另有 TimeSpan 参数）。<c>expire &lt; 0</c> 表示采用 <see cref="Expire"/> 默认值，<c>expire == 0</c> 表示永不过期，<c>expire &gt; 0</c> 表示相对过期（从现在起）。</description></item>
/// <item><description><strong>过期处理：</strong>已过期的键应视为不存在；实现可在访问时惰性清理。</description></item>
/// <item><description><strong>性能注意：</strong>远端缓存（如 Redis）上 <see cref="Keys"/>、<see cref="Count"/>、<see cref="Search(string, int, int)"/> 可能是高耗时/高复杂度操作，仅用于诊断或小规模数据。</description></item>
/// </list>
/// </remarks>
public interface ICache
{
    #region 基本属性
    /// <summary>缓存名称</summary>
    String Name { get; }

    /// <summary>默认过期时间（秒）</summary>
    /// <remarks>
    /// 作为 <c>expire &lt; 0</c> 时的回退值；实现应使用"相对过期"（TTL，从现在起计时）。默认 0 秒表示不过期。
    /// </remarks>
    Int32 Expire { get; set; }

    /// <summary>缓存项总数</summary>
    /// <remarks>
    /// 对远端/分布式实现，该操作可能需要全量扫描，务必谨慎使用。
    /// </remarks>
    Int32 Count { get; }

    /// <summary>所有缓存键集合</summary>
    /// <remarks>
    /// 仅用于诊断或小数据量。对海量键的远端实现可能非常昂贵。
    /// </remarks>
    ICollection<String> Keys { get; }

    /// <summary>获取或设置缓存项（永不过期）</summary>
    /// <param name="key">键</param>
    /// <returns>返回对象实例；不存在返回 <c>null</c></returns>
    /// <remarks>
    /// 通过索引器设置的项应视为"永不过期"（等价 <c>expire == 0</c>）。
    /// </remarks>
    Object? this[String key] { get; set; }
    #endregion

    #region 基础操作
    /// <summary>检查缓存项是否存在</summary>
    /// <param name="key">键</param>
    /// <returns>是否存在。已过期的键视为不存在</returns>
    Boolean ContainsKey(String key);

    /// <summary>设置缓存项</summary>
    /// <param name="key">键</param>
    /// <param name="value">值。引用类型可为 <c>null</c></param>
    /// <param name="expire">过期时间（秒）。小于0时采用默认缓存时间 <see cref="Expire"/>；0 表示永不过期；大于0表示从现在起相对过期</param>
    /// <returns>是否成功设置</returns>
    Boolean Set<T>(String key, T value, Int32 expire = -1);

    /// <summary>设置缓存项</summary>
    /// <param name="key">键</param>
    /// <param name="value">值。引用类型可为 <c>null</c></param>
    /// <param name="expire">过期时间，相对过期（从现在起）。传入 <see cref="TimeSpan.Zero"/> 表示永不过期</param>
    /// <returns>是否成功设置</returns>
    Boolean Set<T>(String key, T value, TimeSpan expire);

    /// <summary>获取缓存项</summary>
    /// <param name="key">键</param>
    /// <returns>返回值；不存在或反序列化失败时返回默认值</returns>
    /// <remarks>
    /// 若需区分"键不存在/反序列化失败/值恰好为默认值"的情况，请使用 <see cref="TryGetValue{T}(string, out T)"/>。
    /// </remarks>
    [return: MaybeNull]
    T Get<T>(String key);

    /// <summary>尝试获取缓存项</summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="value">输出值。即使有值也不一定能够返回，可能缓存项刚好是默认值，或者只是反序列化失败</param>
    /// <returns>返回是否包含值，即使反序列化失败</returns>
    /// <remarks>
    /// 返回 <c>true</c> 表示键存在，但不保证反序列化成功；反序列化失败时 <paramref name="value"/> 通常为默认值。
    /// 解决缓存穿透问题的重要方法。
    /// </remarks>
    Boolean TryGetValue<T>(String key, [MaybeNullWhen(false)] out T value);

    /// <summary>移除缓存项</summary>
    /// <param name="key">键。支持 * 模糊匹配</param>
    /// <returns>受影响的键个数</returns>
    /// <remarks>
    /// 模式匹配规则：至少支持 <c>*</c>（通配任意长度）。大小写敏感性由实现决定；远端实现大范围匹配可能较慢。
    /// </remarks>
    Int32 Remove(String key);

    /// <summary>批量移除缓存项</summary>
    /// <param name="keys">键集合。支持 * 模糊匹配</param>
    /// <returns>受影响的键个数</returns>
    Int32 Remove(params String[] keys);

    /// <summary>清空所有缓存项</summary>
    /// <remarks>
    /// 对远端实现可能是危险/高代价操作，请谨慎调用。
    /// </remarks>
    void Clear();
    #endregion

    #region 过期时间管理
    /// <summary>设置缓存项有效期</summary>
    /// <param name="key">键</param>
    /// <param name="expire">过期时间。表示从现在起剩余存活期（TTL）；传入 <see cref="TimeSpan.Zero"/> 表示永不过期</param>
    /// <returns>是否设置成功（不存在的键应返回 <c>false</c>）</returns>
    Boolean SetExpire(String key, TimeSpan expire);

    /// <summary>获取缓存项有效期</summary>
    /// <param name="key">键</param>
    /// <returns>剩余存活期（TTL）。建议语义：永不过期返回 <see cref="TimeSpan.Zero"/>；不存在或已过期返回负值（例如 <c>TimeSpan.FromSeconds(-1)</c>）</returns>
    /// <remarks>
    /// 若需要无歧义区分不存在与永不过期，建议优先调用 <see cref="ContainsKey(string)"/> 或 <see cref="TryGetValue{T}(string, out T)"/>。
    /// </remarks>
    TimeSpan GetExpire(String key);
    #endregion

    #region 批量操作
    /// <summary>批量获取缓存项</summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="keys">键集合</param>
    /// <returns>返回键-值字典。通常仅包含存在的键；不存在的键可省略或映射为默认值，取决于实现</returns>
    IDictionary<String, T?> GetAll<T>(IEnumerable<String> keys);

    /// <summary>批量设置缓存项</summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="values">键-值字典</param>
    /// <param name="expire">过期时间（秒）。小于0时采用默认缓存时间 <see cref="Expire"/>；0 表示永不过期</param>
    void SetAll<T>(IDictionary<String, T> values, Int32 expire = -1);
    #endregion

    #region 高级操作
    /// <summary>添加缓存项（已存在时不更新）</summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <param name="expire">过期时间（秒）。小于0时采用默认缓存时间 <see cref="Expire"/>；0 表示永不过期</param>
    /// <returns>是否成功添加。若键已存在返回 <c>false</c></returns>
    Boolean Add<T>(String key, T value, Int32 expire = -1);

    /// <summary>替换并返回旧值（原子操作）</summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="value">新值</param>
    /// <returns>旧值（不存在时为默认值）</returns>
    /// <remarks>
    /// 常用于累加到一定数后重置归零，配合 <see cref="Increment(string, long)"/> 使用，避免多线程冲突。
    /// 若键不存在，返回默认值并设置新值。
    /// </remarks>
    [return: MaybeNull]
    T Replace<T>(String key, T value);

    /// <summary>获取或添加缓存项</summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="callback">缺失时的工厂回调。实现层可选择提供并发合并（single-flight）以避免惊群</param>
    /// <param name="expire">过期时间（秒）。小于0时采用默认缓存时间 <see cref="Expire"/></param>
    /// <returns>返回缓存值；工厂回调可能返回默认值</returns>
    /// <remarks>
    /// 在数据不存在时执行委托请求数据，是缓存穿透保护的重要模式。
    /// </remarks>
    [return: MaybeNull]
    T GetOrAdd<T>(String key, Func<String, T> callback, Int32 expire = -1);

    /// <summary>原子递增</summary>
    /// <param name="key">键</param>
    /// <param name="value">变化量</param>
    /// <returns>更新后的值</returns>
    /// <remarks>
    /// 不存在的键应按 <c>0</c> 起步；允许负数以实现递减。
    /// </remarks>
    Int64 Increment(String key, Int64 value);

    /// <summary>原子递增（浮点）</summary>
    /// <param name="key">键</param>
    /// <param name="value">变化量</param>
    /// <returns>更新后的值</returns>
    /// <remarks>
    /// 不存在的键应按 <c>0</c> 起步；允许负数以实现递减，注意浮点精度差异。
    /// </remarks>
    Double Increment(String key, Double value);

    /// <summary>原子递减</summary>
    /// <param name="key">键</param>
    /// <param name="value">变化量</param>
    /// <returns>更新后的值</returns>
    Int64 Decrement(String key, Int64 value);

    /// <summary>原子递减（浮点）</summary>
    /// <param name="key">键</param>
    /// <param name="value">变化量</param>
    /// <returns>更新后的值</returns>
    Double Decrement(String key, Double value);

    /// <summary>搜索匹配的键</summary>
    /// <param name="pattern">匹配字符串。一般支持 *，Redis 还支持 ?；在远端实现上请优先分页扫描</param>
    /// <param name="offset">开始偏移量。默认从0开始，Redis 对海量 key 搜索时需要分批</param>
    /// <param name="count">搜索个数。默认-1表示全部，Redis 对海量 key 搜索时需要分批</param>
    /// <returns>匹配的键集合</returns>
    /// <remarks>
    /// 对海量键空间，建议调用方分页遍历（结合 <paramref name="offset"/> 与 <paramref name="count"/>）。
    /// </remarks>
    IEnumerable<String> Search(String pattern, Int32 offset = 0, Int32 count = -1);
    #endregion

    #region 集合操作
    /// <summary>获取列表</summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="key">键</param>
    /// <returns>与缓存后端绑定的列表视图</returns>
    /// <remarks>
    /// 该集合通常直接操作后端数据结构（如内存集合/Redis List），线程安全性与可见性由实现决定。
    /// </remarks>
    IList<T> GetList<T>(String key);

    /// <summary>获取字典</summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="key">键</param>
    /// <returns>与缓存后端绑定的字典视图</returns>
    IDictionary<String, T> GetDictionary<T>(String key);

    /// <summary>获取队列</summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="key">键</param>
    /// <returns>生产者消费者队列视图，具体能力由实现决定</returns>
    IProducerConsumer<T> GetQueue<T>(String key);

    /// <summary>获取栈</summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="key">键</param>
    /// <returns>生产者消费者栈视图，具体能力由实现决定</returns>
    IProducerConsumer<T> GetStack<T>(String key);

    /// <summary>获取集合</summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="key">键</param>
    /// <returns>与缓存后端绑定的集合（Set）视图</returns>
    ICollection<T> GetSet<T>(String key);
    #endregion

    #region 分布式锁
    /// <summary>申请分布式锁（简化版）</summary>
    /// <param name="key">锁键</param>
    /// <param name="msTimeout">锁等待时间（毫秒）</param>
    /// <returns>锁对象，使用后需主动释放</returns>
    /// <remarks>
    /// <para>需要注意，使用完锁后需调用 Dispose 方法以释放锁，申请与释放一定是成对出现。</para>
    /// 
    /// <para>线程A申请得到锁key以后，在A主动释放锁或者到达超时时间msTimeout之前，其它线程B申请这个锁都会被阻塞。
    /// 线程B申请锁key的最大阻塞时间为msTimeout，在到期之前，如果线程A主动释放锁或者A锁定key的超时时间msTimeout已到，那么线程B会抢到锁。</para>
    /// 
    /// <example>
    /// <code>
    /// using var rlock = cache.AcquireLock("myKey", 5000);
    /// // todo 需要分布式锁保护的代码
    /// rlock.Dispose(); // 释放锁。也可以在using语句结束时自动释放
    /// </code>
    /// </example>
    /// </remarks>
    IDisposable? AcquireLock(String key, Int32 msTimeout);

    /// <summary>申请分布式锁（完整版）</summary>
    /// <param name="key">锁键</param>
    /// <param name="msTimeout">锁等待时间，申请加锁时如果遇到冲突则等待的最大时间（毫秒）</param>
    /// <param name="msExpire">锁过期时间，超过该时间如果没有主动释放则自动释放锁，必须整数秒（毫秒）</param>
    /// <param name="throwOnFailure">失败时是否抛出异常，如果不抛出异常，可通过返回null得知申请锁失败</param>
    /// <returns>锁对象，使用后需主动释放</returns>
    /// <remarks>
    /// <para>需要注意，使用完锁后需调用 Dispose 方法以释放锁，申请与释放一定是成对出现。</para>
    /// 
    /// <para>线程A申请得到锁key以后，在A主动释放锁或者到达超时时间msExpire之前，其它线程B申请这个锁都会被阻塞。
    /// 线程B申请锁key的最大阻塞时间为msTimeout，在到期之前，如果线程A主动释放锁或者A锁定key的超时时间msExpire已到，那么线程B会抢到锁。</para>
    /// 
    /// <example>
    /// <code>
    /// using var rlock = cache.AcquireLock("myKey", 5000, 15000, false);
    /// if (rlock == null) throw new Exception("申请锁失败！");
    /// // todo 需要分布式锁保护的代码
    /// rlock.Dispose(); // 释放锁。也可以在using语句结束时自动释放
    /// </code>
    /// </example>
    /// </remarks>
    IDisposable? AcquireLock(String key, Int32 msTimeout, Int32 msExpire, Boolean throwOnFailure);
    #endregion

    #region 事务与性能
    /// <summary>提交变更</summary>
    /// <returns>受影响项数量或实现定义的状态码</returns>
    /// <remarks>
    /// 纯内存实现通常可返回 <c>0</c> 或直接忽略；持久化实现可用于批量提交。部分提供者需要刷盘操作。
    /// </remarks>
    Int32 Commit();

    /// <summary>多线程性能测试</summary>
    /// <param name="rand">随机读写。顺序：每个线程多次操作一个key；随机：每个线程每次操作不同key</param>
    /// <param name="batch">批量操作。默认0不分批，分批仅针对随机读写，对顺序读写的单key操作没有意义</param>
    /// <returns>总耗时或实现定义的计时值（单位通常为毫秒）</returns>
    /// <remarks>
    /// 仅用于诊断/评估，不建议在生产路径调用。
    /// </remarks>
    Int64 Bench(Boolean rand = false, Int32 batch = 0);
    #endregion
}