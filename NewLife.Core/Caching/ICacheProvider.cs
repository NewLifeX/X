using NewLife.Messaging;

namespace NewLife.Caching;

/// <summary>分布式缓存架构服务。提供基础缓存及队列服务</summary>
/// <remarks>
/// <para>文档：https://newlifex.com/core/icacheprovider</para>
/// 
/// <para><strong>架构设计：</strong></para>
/// <para>根据实际开发经验，即使在分布式系统中，也有大量的数据是不需要跨进程共享的，因此本接口提供了两级缓存。</para>
/// <list type="bullet">
/// <item><description><strong>进程内缓存：</strong>使用 <see cref="InnerCache"/>，可以规避对象序列化成本</description></item>
/// <item><description><strong>跨进程缓存：</strong>使用 <see cref="Cache"/>，借助该缓存架构，可以实现各功能模块跨进程共享数据，分布式部署时可用 Redis，需要考虑序列化成本</description></item>
/// </list>
/// 
/// <para><strong>队列使用策略：</strong></para>
/// <para>可根据是否设置消费组来决定使用简单队列还是完整队列：</para>
/// <list type="bullet">
/// <item><description><strong>简单队列（如RedisQueue）：</strong>可用作命令队列，Topic 很多，但几乎没有消息</description></item>
/// <item><description><strong>完整队列（如RedisStream）：</strong>可用作消息队列，Topic 很少，但消息很多，并且支持多消费组</description></item>
/// </list>
/// </remarks>
public interface ICacheProvider
{
    #region 缓存实例
    /// <summary>全局缓存。各功能模块跨进程共享数据，分布式部署时可用Redis，需要考虑序列化成本。默认单机使用内存缓存</summary>
    /// <remarks>
    /// 适用于需要跨进程/跨服务器共享的数据场景，如用户会话、分布式锁、缓存共享等。
    /// 在集群部署时通常配置为 Redis，单机部署时可使用 MemoryCache。
    /// </remarks>
    ICache Cache { get; set; }

    /// <summary>应用内本地缓存。默认内存缓存，无需考虑对象序列化成本，缺点是不支持跨进程共享数据</summary>
    /// <remarks>
    /// 适用于进程内数据缓存，如配置信息、字典数据、计算结果等。
    /// 优势：无序列化开销，性能更好；劣势：仅限当前进程，不支持分布式。
    /// </remarks>
    ICache InnerCache { get; set; }
    #endregion

    #region 队列服务
    /// <summary>获取队列。各功能模块跨进程共用的队列</summary>
    /// <typeparam name="T">消息类型。用于消息生产者时，可指定为Object</typeparam>
    /// <param name="topic">主题名称</param>
    /// <param name="group">消费组。未指定消费组时使用简单队列（如RedisQueue），指定消费组时使用完整队列（如RedisStream）</param>
    /// <returns>队列实例</returns>
    /// <remarks>
    /// <para><strong>队列选择策略：</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>简单队列（group == null）：</strong>适合命令分发场景，Topic 数量多但消息量少</description></item>
    /// <item><description><strong>完整队列（group != null）：</strong>适合消息传递场景，Topic 数量少但消息量大，支持消费组和消息确认</description></item>
    /// </list>
    /// </remarks>
    IProducerConsumer<T> GetQueue<T>(String topic, String? group = null);

    /// <summary>获取内部队列。默认内存队列</summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="topic">主题名称</param>
    /// <returns>队列实例</returns>
    /// <remarks>
    /// 进程内消息队列，适用于模块间解耦通信，无序列化开销但不支持跨进程。
    /// </remarks>
    IProducerConsumer<T> GetInnerQueue<T>(String topic);
    #endregion

    #region 分布式锁
    /// <summary>申请分布式锁（简化版）。使用完以后需要主动释放</summary>
    /// <param name="lockKey">要锁定的键值。建议加上应用模块等前缀以避免冲突</param>
    /// <param name="msTimeout">遇到冲突时等待的最大时间，同时也是锁维持的时间（毫秒）</param>
    /// <returns>锁对象，使用后需主动释放。申请失败时抛出异常</returns>
    /// <remarks>
    /// <para>需要注意，使用完锁后需调用 Dispose 方法以释放锁，申请与释放一定是成对出现。</para>
    /// 
    /// <para><strong>锁机制说明：</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>申请逻辑：</strong>锁定某个 key，锁维持时间为 msTimeout，遇到冲突时等待 msTimeout 时间</description></item>
    /// <item><description><strong>成功场景：</strong>在等待时间内获得锁，返回 IDisposable 对象，离开 using 代码块时自动释放锁</description></item>
    /// <item><description><strong>失败场景：</strong>在等待时间内没有获得锁，抛出异常，需要自己处理锁冲突的情况</description></item>
    /// </list>
    /// 
    /// <para>如果希望指定不同的维持时间和等待时间，可以使用 <see cref="ICache"/> 接口的 <see cref="ICache.AcquireLock(String, Int32, Int32, Boolean)"/> 方法。</para>
    /// 
    /// <example>
    /// <code>
    /// using var rlock = cacheProvider.AcquireLock("myKey", 5000);
    /// //todo 需要分布式锁保护的代码
    /// rlock.Dispose(); //释放锁。也可以在using语句结束时自动释放
    /// </code>
    /// </example>
    /// </remarks>
    IDisposable? AcquireLock(String lockKey, Int32 msTimeout);
    #endregion
}

/// <summary>缓存提供者助手</summary>
public static class CacheProviderHelper
{
    /// <summary>创建事件总线</summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="provider">缓存提供者</param>
    /// <param name="topic">主题</param>
    /// <param name="clientId">应用标识。作为消费组</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public static IEventBus<TEvent>? CreateEventBus<TEvent>(this ICacheProvider provider, String topic, String? clientId = null)
    {
        if (provider.Cache is not Cache cache)
            //throw new NotSupportedException($"[{provider.Cache.GetType().FullName}]缓存不支持创建事件总线！");
            return null;

        return cache.CreateEventBus<TEvent>(topic, clientId ?? String.Empty);
    }
}