namespace NewLife.Caching;

/// <summary>分布式缓存架构服务。提供基础缓存及队列服务</summary>
/// <remarks>
/// 默认实现，构造时自动初始化为 MemoryCache，可在运行时替换为其他缓存实现（如 Redis）。
/// </remarks>
public class CacheProvider : ICacheProvider
{
    #region 属性
    /// <summary>全局缓存。各功能模块跨进程共享数据，分布式部署时可用Redis，需要考虑序列化成本。默认单机使用内存缓存</summary>
    public ICache Cache { get; set; }

    /// <summary>应用内本地缓存。默认内存缓存，无需考虑对象序列化成本，缺点是不支持跨进程共享数据</summary>
    public ICache InnerCache { get; set; }
    #endregion

    #region 构造
    /// <summary>使用默认缓存实例化</summary>
    /// <remarks>
    /// 优先使用全局默认缓存实例，如果不存在则创建新的 MemoryCache 实例。
    /// Cache 和 InnerCache 初始时指向同一实例，可根据需要在运行时分别设置。
    /// </remarks>
    public CacheProvider()
    {
        var cache = Caching.Cache.Default ?? new MemoryCache();
        Cache = cache;
        InnerCache = cache;
    }
    #endregion

    #region 队列服务
    /// <summary>获取队列。各功能模块跨进程共用的队列</summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="topic">主题</param>
    /// <param name="group">消费组。未指定时使用简单队列，指定时使用完整队列</param>
    /// <returns>队列实例</returns>
    /// <remarks>
    /// 当前实现忽略消费组参数，统一使用 Cache 的队列实现。
    /// 具体的队列类型选择由底层缓存实现决定。
    /// </remarks>
    public virtual IProducerConsumer<T> GetQueue<T>(String topic, String? group = null) => Cache.GetQueue<T>(topic);

    /// <summary>获取内部队列。默认内存队列</summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="topic">主题</param>
    /// <returns>队列实例</returns>
    public virtual IProducerConsumer<T> GetInnerQueue<T>(String topic) => InnerCache.GetQueue<T>(topic);
    #endregion

    #region 分布式锁
    /// <summary>申请分布式锁</summary>
    /// <param name="lockKey">要锁定的键值。建议加上应用模块等前缀以避免冲突</param>
    /// <param name="msTimeout">遇到冲突时等待的最大时间（毫秒）</param>
    /// <returns>锁对象，使用后需主动释放</returns>
    public virtual IDisposable? AcquireLock(String lockKey, Int32 msTimeout) => Cache.AcquireLock(lockKey, msTimeout);
    #endregion
}