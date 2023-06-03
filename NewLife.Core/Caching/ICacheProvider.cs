namespace NewLife.Caching;

/// <summary>分布式缓存架构服务。提供基础缓存及队列服务</summary>
public interface ICacheProvider
{
    /// <summary>全局缓存。各功能模块跨进程共享数据，分布式部署时可用Redis，需要考虑序列化成本。默认单机使用内存缓存</summary>
    ICache Cache { get; set; }

    /// <summary>应用内本地缓存。默认内存缓存，无需考虑对象序列化成本，缺点是不支持跨进程共享数据</summary>
    ICache InnerCache { get; set; }

    /// <summary>获取队列。各功能模块跨进程共用的队列</summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="topic">主题</param>
    /// <param name="group">消费组</param>
    /// <returns></returns>
    IProducerConsumer<T> GetQueue<T>(String topic, String group = null);

    /// <summary>获取内部队列。默认内存队列</summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="topic">主题</param>
    /// <returns></returns>
    IProducerConsumer<T> GetInnerQueue<T>(String topic);

    /// <summary>申请分布式锁</summary>
    /// <param name="lockKey"></param>
    /// <param name="msTimeout"></param>
    /// <returns></returns>
    IDisposable AcquireLock(String lockKey, Int32 msTimeout);
}