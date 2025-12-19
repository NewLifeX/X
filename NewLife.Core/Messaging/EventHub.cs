using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using NewLife;
using NewLife.Caching;
using NewLife.Data;
using NewLife.Serialization;
#if !NET45
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.Messaging;

/// <summary>事件分发回调</summary>
/// <typeparam name="TEvent">事件类型</typeparam>
/// <param name="event">事件实例</param>
/// <param name="cancellationToken">取消通知标记</param>
/// <returns>异步任务，结果为已处理事件数量（0 表示未处理）</returns>
public delegate Task<Int32> DispatchCallback<TEvent>(TEvent @event, CancellationToken cancellationToken);

/// <summary>事件枢纽。把从网络收到的数据包分发给不同的事件总线</summary>
/// <typeparam name="TEvent">事件类型</typeparam>
/// <remarks>
/// <para>进程内按主题路由并分发事件。通过 <see cref="Add(String, DispatchCallback{TEvent})"/> 或 <see cref="Add(String, IEventDispatcher{TEvent})"/> 注册处理器，
/// 同一主题仅保留最后一次注册的处理器。</para>
/// <para>实现了 <see cref="IEventDispatcher{T}"/>（<see cref="IPacket"/> 与 <see cref="String"/> 两种输入），当收到以 <c>event#</c> 开头的消息时，
/// 按 <c>event#topic#clientId#message</c> 解析并路由，其中 <c>message</c> 为 <typeparamref name="TEvent"/> 的 JSON 表示。</para>
/// <para>线程安全：内部使用 <see cref="ConcurrentDictionary{TKey, TValue}"/> 保存订阅。</para>
/// <para>返回值语义：未匹配到订阅、解析失败或非事件消息返回 <c>0</c>；成功分发时返回处理器的结果。</para>
/// </remarks>
public class EventHub<TEvent> : IEventDispatcher<IPacket>, IEventDispatcher<String>, IEventHandler<TEvent>
{
    #region 属性
    /// <summary>事件总线工厂。用于创建事件总线</summary>
    public IEventBusFactory Factory { get; set; } = MemoryCache.Instance;

    private readonly ConcurrentDictionary<String, IEventBus<TEvent>> _eventBuses = new();
    private readonly ConcurrentDictionary<String, DispatchCallback<TEvent>> _dispatchers = new();
    #endregion

    #region 方法
    /// <summary>添加订阅</summary>
    /// <param name="topic">主题名称</param>
    /// <param name="action">处理该主题事件的回调</param>
    public void Add(String topic, DispatchCallback<TEvent> action)
    {
        _dispatchers[topic] = action;
    }

    /// <summary>按主题注册事件总线。路由至分发器的 <c>DispatchAsync</c></summary>
    /// <param name="topic">主题名称</param>
    /// <param name="dispatcher">事件分发器，将通过其 <c>DispatchAsync</c> 处理事件</param>
    public void Add(String topic, IEventDispatcher<TEvent> dispatcher)
    {
        // 将分发器封装为统一委托，消息体按 TEvent 传递
        _dispatchers[topic] = dispatcher.DispatchAsync;

        if (dispatcher is IEventBus<TEvent> bus)
            _eventBuses[topic] = bus;
    }

    /// <summary>获取指定主题的事件总线，不存在时创建</summary>
    /// <param name="topic">事件主题</param>
    /// <param name="clientId">客户标识/消息分组</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public IEventBus<TEvent> GetEventBus(String topic, String clientId = "")
    {
        if (_eventBuses.TryGetValue(topic, out var bus)) return bus;

        if (Factory == null) throw new ArgumentNullException(nameof(Factory));

        bus = Factory.CreateEventBus<TEvent>(topic, clientId);

        //_eventBuses[topic] = bus;
        bus = _eventBuses.GetOrAdd(topic, bus);

        if (bus is IEventDispatcher<TEvent> dispatcher)
            _dispatchers[topic] = dispatcher.DispatchAsync;

        return bus;
    }

    /// <summary>尝试获取分发器</summary>
    /// <param name="topic">主题名称</param>
    /// <param name="action">处理委托</param>
    /// <returns></returns>
    public Boolean TryGetValue(String topic, [MaybeNullWhen(false)] out DispatchCallback<TEvent> action) => _dispatchers.TryGetValue(topic, out action);

    /// <summary>尝试获取事件总线</summary>
    /// <param name="topic">主题名称</param>
    /// <param name="eventBus">事件总线</param>
    /// <returns></returns>
    public Boolean TryGetBus<T>(String topic, [MaybeNullWhen(false)] out IEventBus<T> eventBus)
    {
        if (_eventBuses.TryGetValue(topic, out var bus) && bus is IEventBus<T> bus2)
        {
            eventBus = bus2;
            return true;
        }

        if (_dispatchers.TryGetValue(topic, out var action))
        {
            eventBus = action.Target as IEventBus<T>;
            if (eventBus != null) return true;
        }

        eventBus = null;
        return false;
    }

    private static readonly Byte[] _eventPrefix = Encoding.ASCII.GetBytes("event#");
    /// <summary>处理接收到的消息</summary>
    /// <remarks>消息格式：<c>event#topic#clientId#message</c>。当匹配前缀 <c>event#</c> 时解析并路由。</remarks>
    /// <param name="data">消息数据包</param>
    /// <param name="cancellationToken">取消通知标记</param>
    /// <returns>异步任务，结果为已处理事件数量（0 表示未处理）</returns>
    public virtual Task<Int32> DispatchAsync(IPacket data, CancellationToken cancellationToken = default)
    {
        // 处理事件消息。event#topic#clientid#message
        if (data.GetSpan().StartsWith(_eventPrefix))
        {
            var str = data.ToStr();
            return DispatchAsync(str, cancellationToken);
        }

        return Task.FromResult(0);
    }

    /// <summary>处理接收到的消息</summary>
    /// <remarks>消息格式：<c>event#topic#clientId#message</c>。当匹配前缀 <c>event#</c> 时解析并路由。</remarks>
    /// <param name="data">消息字符串</param>
    /// <param name="cancellationToken">取消通知标记</param>
    /// <returns>异步任务，结果为已处理事件数量（0 表示未处理）</returns>
    public virtual Task<Int32> DispatchAsync(String data, CancellationToken cancellationToken = default)
    {
        // 处理事件消息。event#topic#clientid#message
        if (data.StartsWith("event#"))
        {
            var p = data.IndexOf('#');
            var p2 = data.IndexOf('#', p + 1);
            if (p2 > 0)
            {
                var topic = data.Substring(p + 1, p2 - p - 1);
                var p3 = data.IndexOf('#', p2 + 1);
                if (p3 > 0)
                {
                    var clientid = data.Substring(p2 + 1, p3 - p2 - 1);
                    var msg = data[(p3 + 1)..];
                    if (msg[0] != '{')
                    {
                        // 订阅和取消订阅动作。event#topic#clientid#subscribe
                        switch (msg)
                        {
                            case "subscribe":
                                var bus = GetEventBus(topic);
                                bus.Subscribe(this, clientid);
                                break;
                            case "unsubscribe":
                                break;
                            default:
                                break;
                        }
                    }
                    if (msg is TEvent @event)
                    {
                        return DispatchAsync(topic, clientid, @event, cancellationToken);
                    }
                    else
                    {
                        var message = msg.ToJsonEntity<TEvent>()!;
                        return DispatchAsync(topic, clientid, message, cancellationToken);
                    }
                }
            }
        }

        return Task.FromResult(0);
    }

    /// <summary>分发事件给各个处理器。进程内分发</summary>
    /// <param name="topic">主题名称</param>
    /// <param name="clientId">发送方客户端标识</param>
    /// <param name="event">事件实例</param>
    /// <param name="cancellationToken">取消通知标记</param>
    /// <returns>异步任务，结果为已处理事件数量（0 表示未处理）</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="topic"/> 为空时抛出</exception>
    public virtual async Task<Int32> DispatchAsync(String topic, String clientId, TEvent @event, CancellationToken cancellationToken = default)
    {
        if (topic.IsNullOrEmpty()) throw new ArgumentNullException(nameof(topic));

        var rs = 0;
        if (_dispatchers.TryGetValue(topic, out var action))
            rs += await action(@event, cancellationToken).ConfigureAwait(false);

        if (_eventBuses.TryGetValue(topic, out var bus))
            rs += await bus.PublishAsync(@event, null, cancellationToken).ConfigureAwait(false);

        return rs;
    }

    Task IEventHandler<TEvent>.HandleAsync(TEvent @event, IEventContext<TEvent>? context, CancellationToken cancellationToken) => throw new NotImplementedException();
    #endregion
}
