namespace NewLife.Caching;

/// <summary>分布式缓存架构服务。提供基础缓存及队列服务</summary>
/// <remarks>
/// 根据实际开发经验，即使在分布式系统中，也有大量的数据是不需要跨进程共享的，因此本接口提供了两级缓存。
/// 进程内缓存使用<see cref="InnerCache"/>，可以规避对象序列化成本，跨进程缓存使用<see cref="Cache"/>。
/// 借助该缓存架构，可以实现各功能模块跨进程共享数据，分布式部署时可用Redis，需要考虑序列化成本。
/// 
/// 使用队列时，可根据是否设置消费组来决定使用简单队列还是完整队列。
/// 简单队列（如RedisQueue）可用作命令队列，Topic很多，但几乎没有消息。
/// 完整队列（如RedisStream）可用作消息队列，Topic很少，但消息很多，并且支持多消费组。
/// </remarks>
public interface ICacheProvider
{
    /// <summary>全局缓存。各功能模块跨进程共享数据，分布式部署时可用Redis，需要考虑序列化成本。默认单机使用内存缓存</summary>
    ICache Cache { get; set; }

    /// <summary>应用内本地缓存。默认内存缓存，无需考虑对象序列化成本，缺点是不支持跨进程共享数据</summary>
    ICache InnerCache { get; set; }

    /// <summary>获取队列。各功能模块跨进程共用的队列</summary>
    /// <remarks>
    /// 使用队列时，可根据是否设置消费组来决定使用简单队列还是完整队列。
    /// 简单队列（如RedisQueue）可用作命令队列，Topic很多，但几乎没有消息。
    /// 完整队列（如RedisStream）可用作消息队列，Topic很少，但消息很多，并且支持多消费组。
    /// </remarks>
    /// <typeparam name="T">消息类型。用于消息生产者时，可指定为Object</typeparam>
    /// <param name="topic">主题</param>
    /// <param name="group">消费组。未指定消费组时使用简单队列（如RedisQueue），指定消费组时使用完整队列（如RedisStream）</param>
    /// <returns></returns>
    IProducerConsumer<T> GetQueue<T>(String topic, String group = null);

    /// <summary>获取内部队列。默认内存队列</summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="topic">主题</param>
    /// <returns></returns>
    IProducerConsumer<T> GetInnerQueue<T>(String topic);

    /// <summary>申请分布式锁</summary>
    /// <remarks>
    /// 一般实现为Redis分布式锁，申请锁的具体表现为锁定某个key，锁维持时间为msTimeout，遇到冲突时等待msTimeout时间。
    /// 如果在等待时间内获得锁，则返回一个IDisposable对象，离开using代码块时自动释放锁。
    /// 如果在等待时间内没有获得锁，则抛出异常，需要自己处理锁冲突的情况。
    /// 
    /// 如果希望指定不同的维持时间和等待时间，可以使用<see cref="ICache"/>接口的<see cref="ICache.AcquireLock(String, Int32, Int32, Boolean)"/>方法。
    /// </remarks>
    /// <param name="lockKey">要锁定的键值。建议加上应用模块等前缀以避免冲突</param>
    /// <param name="msTimeout">遇到冲突时等待的最大时间，同时也是锁维持的时间</param>
    /// <returns></returns>
    IDisposable AcquireLock(String lockKey, Int32 msTimeout);
}