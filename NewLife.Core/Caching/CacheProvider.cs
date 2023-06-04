namespace NewLife.Caching;

/// <summary>分布式缓存架构服务。提供基础缓存及队列服务</summary>
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
    public CacheProvider()
    {
        var cache = Caching.Cache.Default ?? new MemoryCache();
        Cache = cache;
        InnerCache = cache;
    }
    #endregion

    #region 方法
    /// <summary>获取队列。各功能模块跨进程共用的队列</summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="topic">主题</param>
    /// <param name="group">消费组</param>
    /// <returns></returns>
    public virtual IProducerConsumer<T> GetQueue<T>(String topic, String group = null) => Cache?.GetQueue<T>(topic);

    /// <summary>获取内部队列。默认内存队列</summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="topic">主题</param>
    /// <returns></returns>
    public virtual IProducerConsumer<T> GetInnerQueue<T>(String topic) => InnerCache?.GetQueue<T>(topic);

    /// <summary>申请分布式锁</summary>
    /// <param name="lockKey">要锁定的键值。建议加上应用模块等前缀以避免冲突</param>
    /// <param name="msTimeout">遇到冲突时等待的最大时间</param>
    /// <returns></returns>
    public virtual IDisposable AcquireLock(String lockKey, Int32 msTimeout) => Cache?.AcquireLock(lockKey, msTimeout);
    #endregion
}