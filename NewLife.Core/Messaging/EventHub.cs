using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using NewLife;
using NewLife.Caching;
using NewLife.Data;
using NewLife.Log;
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

/// <summary>事件枢纽。按主题把网络消息分发到事件总线或回调</summary>
/// <typeparam name="TEvent">事件类型</typeparam>
/// <remarks>
/// <para>核心职责：收取网络数据（<see cref="IPacket"/> 或 <see cref="String"/>），解析主题标识，并将消息分发给该主题对应的事件总线（<see cref="IEventBus{TEvent}"/>）或回调（<see cref="DispatchCallback{TEvent}"/>）。</para>
/// <para>主题注册：通过 <see cref="Add(String, DispatchCallback{TEvent})"/> 或 <see cref="Add(String, IEventDispatcher{TEvent})"/> 注册；同一主题仅保留最后一次注册的处理器。也可通过 <see cref="GetEventBus(String, String)"/> 延迟创建并缓存事件总线。</para>
/// <para>消息格式：仅处理以 <c>event#</c> 开头的消息，格式为 <c>event#topic#clientId#message</c>，其中 <c>message</c> 通常为 <typeparamref name="TEvent"/> 的 JSON。</para>
/// <para>客户端场景：按主题注册（<c>Add</c>/<c>GetEventBus</c>）事件总线；当收到服务端下发数据包时，解析主题并分发到对应事件总线/回调。</para>
/// <para>服务端场景：通常单例使用；多个网络客户端订阅同一主题时，会在枢纽内共享同一个事件总线对象，但每个客户端拥有各自的事件处理器。任一客户端发布事件后，其它订阅该主题的客户端会收到，等价于简化版消息队列。</para>
/// <para>订阅控制：当 <c>message</c> 为 <c>subscribe</c> 或 <c>unsubscribe</c> 时执行订阅/取消订阅；若取消订阅后主题无任何订阅者，将注销并移除该主题的事件总线与分发器，避免占用内存。</para>
/// <para>线程安全：内部使用 <see cref="ConcurrentDictionary{TKey, TValue}"/> 保存主题与处理器映射。返回值语义：未匹配、解析失败或非事件消息返回 <c>0</c>；成功分发时返回处理器结果。</para>
/// </remarks>
public class EventHub<TEvent> : IEventDispatcher<IPacket>, IEventDispatcher<String>, IEventHandler<TEvent>
{
    #region 属性
    /// <summary>事件总线工厂</summary>
    /// <remarks>用于按主题创建事件总线。默认使用 <see cref="MemoryCache"/> 作为工厂实现。</remarks>
    public IEventBusFactory Factory { get; set; } = MemoryCache.Instance;

    /// <summary>已创建的主题事件总线</summary>
    /// <remarks>
    /// <para>服务端单例场景下：同一主题通常会被多个网络客户端共享订阅，因此这里按 topic 缓存总线实例。</para>
    /// </remarks>
    private readonly ConcurrentDictionary<String, IEventBus<TEvent>> _eventBuses = new();

    /// <summary>主题分发器</summary>
    /// <remarks>
    /// <para>存放按 topic 路由后的最终执行入口：可能是事件总线（其 <c>DispatchAsync</c>），也可能是用户直接注册的回调。</para>
    /// <para>同一主题仅保留最后一次注册的处理器。</para>
    /// </remarks>
    private readonly ConcurrentDictionary<String, DispatchCallback<TEvent>> _dispatchers = new();
    #endregion

    #region 方法
    /// <summary>添加订阅</summary>
    /// <param name="topic">主题名称</param>
    /// <param name="action">处理该主题事件的回调</param>
    public void Add(String topic, DispatchCallback<TEvent> action)
    {
        // 仅建立 topic -> 回调 的映射；该主题不一定具备事件总线。
        _dispatchers[topic] = action;
    }

    /// <summary>按主题注册事件分发器</summary>
    /// <param name="topic">主题名称</param>
    /// <param name="dispatcher">事件分发器，将通过其 <c>DispatchAsync</c> 处理事件</param>
    public void Add(String topic, IEventDispatcher<TEvent> dispatcher)
    {
        // 将分发器封装为统一委托，消息体按 TEvent 传递
        _dispatchers[topic] = dispatcher.DispatchAsync;

        // 若该分发器本身就是事件总线，则同时缓存总线对象，后续发布走总线语义。
        if (dispatcher is IEventBus<TEvent> bus)
            _eventBuses[topic] = bus;
    }

    /// <summary>获取指定主题的事件总线</summary>
    /// <remarks>
    /// <para>说明：不存在时将通过 <see cref="Factory"/> 创建并缓存；并发下可能多次创建，但最终仅保留一份实例。</para>
    /// </remarks>
    /// <param name="topic">事件主题</param>
    /// <param name="clientId">客户标识/消息分组</param>
    /// <exception cref="ArgumentNullException">当 <see cref="Factory"/> 为空时抛出</exception>
    public IEventBus<TEvent> GetEventBus(String topic, String clientId = "")
    {
        if (_eventBuses.TryGetValue(topic, out var bus)) return bus;

        if (Factory == null) throw new ArgumentNullException(nameof(Factory));

        // 并发场景下允许多线程同时走到 CreateEventBus，但最终仅有一个实例会进入字典，其它实例会被丢弃。
        WriteLog("注册主题：{0}，客户端：{1}", topic, clientId);
        bus = Factory.CreateEventBus<TEvent>(topic, clientId);

        //_eventBuses[topic] = bus;
        bus = _eventBuses.GetOrAdd(topic, bus);

        // 若事件总线同时提供事件分发能力，则这里将其注册到分发器表，供 DispatchAsync(topic, ...) 路由使用。
        if (bus is IEventDispatcher<TEvent> dispatcher)
            _dispatchers[topic] = dispatcher.DispatchAsync;

        return bus;
    }

    /// <summary>尝试获取分发器</summary>
    /// <param name="topic">主题名称</param>
    /// <param name="action">输出的分发委托</param>
    public Boolean TryGetValue(String topic, [MaybeNullWhen(false)] out DispatchCallback<TEvent> action) => _dispatchers.TryGetValue(topic, out action);

    /// <summary>尝试获取事件总线</summary>
    /// <param name="topic">主题名称</param>
    /// <param name="eventBus">输出的事件总线实例</param>
    public Boolean TryGetBus<T>(String topic, [MaybeNullWhen(false)] out IEventBus<T> eventBus)
    {
        // 优先从已创建的事件总线字典中获取。
        if (_eventBuses.TryGetValue(topic, out var bus) && bus is IEventBus<T> bus2)
        {
            eventBus = bus2;
            return true;
        }

        // 兼容：若调用方只通过 Add(topic, dispatcher) 注册过分发器，这里尝试从委托 Target 回溯总线对象。
        if (_dispatchers.TryGetValue(topic, out var action))
        {
            eventBus = action.Target as IEventBus<T>;
            if (eventBus != null) return true;
        }

        eventBus = null;
        return false;
    }

    private static readonly Byte[] _eventPrefix = Encoding.ASCII.GetBytes("event#");
    private static readonly Char[] _eventPrefix2 = "event#".ToCharArray();
    /// <summary>处理接收到的消息</summary>
    /// <remarks>
    /// <para>消息格式：<c>event#topic#clientId#message</c>。当匹配前缀 <c>event#</c> 时解析并路由。</para>
    /// </remarks>
    /// <param name="data">消息数据包</param>
    /// <param name="cancellationToken">取消通知标记</param>
    /// <returns>异步任务，结果为已处理事件数量（0 表示未处理）</returns>
    public virtual async Task<Int32> DispatchAsync(IPacket data, CancellationToken cancellationToken = default)
    {
        // 处理事件消息。event#topic#clientid#message
        // 先用 Span 前缀判断，尽量避免非事件消息的字符串分配。
        var header = data.GetSpan();
        if (!header.StartsWith(_eventPrefix)) return 0;

        // 解析 topic
        var p = header.IndexOf((Byte)'#');
        var header2 = header[(p + 1)..];
        var p2 = header2.IndexOf((Byte)'#');
        if (p2 <= 0) return 0;

        var topic = header2[..p2].ToStr();

        // 解析 clientId（发送方标识/订阅分组）
        var header3 = header2[(p2 + 1)..];
        var p3 = header3.IndexOf((Byte)'#');
        if (p3 <= 0) return 0;

        var clientid = header3[..p3].ToStr();

        var msg = data.Slice(p + 1 + p2 + 1 + p3 + 1);
        if (msg.Length == 0) return 0;

        if (msg[0] != '{' && msg.Total < 16)
        {
            if (await DispatchActionAsync(topic, clientid, msg.ToStr()).ConfigureAwait(false)) return 1;
        }

        // 普通事件：优先尝试直接转换（消息本身可能已是 TEvent），否则按 JSON 解析。
        if (msg is TEvent @event)
        {
            return await DispatchAsync(topic, clientid, @event, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            @event = msg.ToStr().ToJsonEntity<TEvent>()!;
            return await DispatchAsync(topic, clientid, @event, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>处理接收到的消息</summary>
    /// <remarks>
    /// <para>消息格式：<c>event#topic#clientId#message</c>。当匹配前缀 <c>event#</c> 时解析并路由。</para>
    /// </remarks>
    /// <param name="data">消息字符串</param>
    /// <param name="cancellationToken">取消通知标记</param>
    /// <returns>异步任务，结果为已处理事件数量（0 表示未处理）</returns>
    public virtual async Task<Int32> DispatchAsync(String data, CancellationToken cancellationToken = default)
    {
        // 处理事件消息。event#topic#clientid#message
        // 返回 0 表示未消费该消息（可能是其它业务协议数据）。
        var header = data.AsSpan();
        if (!header.StartsWith(_eventPrefix2)) return 0;

        // 解析 topic
        var p = header.IndexOf('#');
        var header2 = header[(p + 1)..];
        var p2 = header2.IndexOf('#');
        if (p2 <= 0) return 0;

        var topic = header2[..p2].ToString();

        // 解析 clientId（发送方标识/订阅分组）
        var header3 = header2[(p2 + 1)..];
        var p3 = header3.IndexOf('#');
        if (p3 <= 0) return 0;

        var clientid = header3[..p3].ToString();

        // 解析 message：可能是 JSON 事件体，也可能是 subscribe/unsubscribe 控制指令。
        var msg = data[(p + 1 + p2 + 1 + p3 + 1)..];
        if (msg.Length == 0) return 0;

        if (msg[0] != '{' && msg.Length < 16)
        {
            if (await DispatchActionAsync(topic, clientid, msg).ConfigureAwait(false)) return 1;
        }

        // 普通事件：优先尝试直接转换（消息本身可能已是 TEvent），否则按 JSON 解析。
        if (msg is TEvent @event)
        {
            return await DispatchAsync(topic, clientid, @event, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            @event = msg.ToJsonEntity<TEvent>()!;
            return await DispatchAsync(topic, clientid, @event, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>处理动作</summary>
    /// <param name="topic"></param>
    /// <param name="clientid"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public virtual Task<Boolean> DispatchActionAsync(String topic, String clientid, String action)
    {
        if (action[0] == '{') return Task.FromResult(false);

        // 订阅和取消订阅动作。event#topic#clientid#subscribe
        switch (action)
        {
            case "subscribe":
                {
                    // 服务端：为该 topic 创建/获取事件总线，并将当前 handler 绑定到 clientid。
                    // clientid 用于区分网络客户端订阅者，便于取消订阅或按连接分组投递。
                    var bus = GetEventBus(topic, clientid);

                    WriteLog("订阅主题：{0}，客户端：{1}", topic, clientid);
                    bus.Subscribe(this, clientid);
                }
                return Task.FromResult(true);
            case "unsubscribe":
                {
                    WriteLog("取消订阅主题：{0}，客户端：{1}", topic, clientid);

                    if (!TryGetBus<TEvent>(topic, out var bus)) return Task.FromResult(false);

                    bus.Unsubscribe(clientid);

                    // 服务端：如果没有订阅者则注销该 topic 的总线与分发器，避免主题长期占用内存。
                    if (bus is EventBus<TEvent> mbus && mbus.Handlers.Count == 0)
                    {
                        _eventBuses.TryRemove(topic, out _);
                        _dispatchers.TryRemove(topic, out _);

                        WriteLog("注销主题：{0}，因订阅为空", topic);
                    }
                }
                return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    /// <summary>分发事件给各个处理器</summary>
    /// <remarks>
    /// <para>进程内分发：按 <c>topic</c> 路由到事件总线或回调。</para>
    /// </remarks>
    /// <param name="topic">主题名称</param>
    /// <param name="clientId">发送方客户端标识</param>
    /// <param name="event">事件实例</param>
    /// <param name="cancellationToken">取消通知标记</param>
    /// <returns>异步任务，结果为已处理事件数量（0 表示未处理）</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="topic"/> 为空时抛出</exception>
    public virtual async Task<Int32> DispatchAsync(String topic, String clientId, TEvent @event, CancellationToken cancellationToken = default)
    {
        if (topic.IsNullOrEmpty()) throw new ArgumentNullException(nameof(topic));

        // 进程内分发：优先路由到事件总线（支持多订阅者发布/投递），否则路由到直接注册的回调。
        var rs = 0;
        if (_eventBuses.TryGetValue(topic, out var bus))
            rs += await bus.PublishAsync(@event, null, cancellationToken).ConfigureAwait(false);
        else if (_dispatchers.TryGetValue(topic, out var action))
            rs += await action(@event, cancellationToken).ConfigureAwait(false);

        return rs;
    }

    Task IEventHandler<TEvent>.HandleAsync(TEvent @event, IEventContext<TEvent>? context, CancellationToken cancellationToken) => throw new NotImplementedException();
    #endregion

    #region 日志
    /// <summary>日志</summary>
    public ILog Log { get; set; } = Logger.Null;

    /// <summary>写日志。同步到当前埋点</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object[] args)
    {
        var span = DefaultSpan.Current;
        span?.AppendTag(String.Format(format, args));

        Log?.Info(format, args);
    }
    #endregion
}
