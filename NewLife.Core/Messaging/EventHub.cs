using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Serialization;

namespace NewLife.Messaging;

/// <summary>事件枢纽。按主题把网络消息分发到事件总线或回调</summary>
/// <typeparam name="TEvent">事件类型</typeparam>
/// <remarks>
/// <para>核心职责：收取网络数据（<see cref="IPacket"/> 或 <see cref="String"/>），解析主题标识，并将消息分发给该主题对应的事件总线（<see cref="IEventBus{TEvent}"/>）或回调（<see cref="IEventHandler{TEvent}"/>）。</para>
/// <para>主题注册：通过 <see cref="Add(String, IEventHandler{TEvent})"/> 或 <see cref="Add(String, IEventBus{TEvent})"/> 注册；同一主题仅保留最后一次注册的处理器。也可通过 <see cref="GetEventBus(String, String)"/> 延迟创建并缓存事件总线。</para>
/// <para>消息格式：仅处理以 <c>event#</c> 开头的消息，格式为 <c>event#topic#clientId#message</c>，其中 <c>message</c> 通常为 <typeparamref name="TEvent"/> 的 JSON。</para>
/// <para>客户端场景：按主题注册（<c>Add</c>/<c>GetEventBus</c>）事件总线；当收到服务端下发数据包时，解析主题并分发到对应事件总线/回调。</para>
/// <para>服务端场景：通常单例使用；多个网络客户端订阅同一主题时，会在枢纽内共享同一个事件总线对象，但每个客户端拥有各自的事件处理器。任一客户端发布事件后，其它订阅该主题的客户端会收到，等价于简化版消息队列。</para>
/// <para>订阅控制：当 <c>message</c> 为 <c>subscribe</c> 或 <c>unsubscribe</c> 时执行订阅/取消订阅；若取消订阅后主题无任何订阅者，将注销并移除该主题的事件总线与分发器，避免占用内存。</para>
/// <para>线程安全：内部使用 <see cref="ConcurrentDictionary{TKey, TValue}"/> 保存主题与处理器映射。返回值语义：未匹配、解析失败或非事件消息返回 <c>0</c>；成功分发时返回处理器结果。</para>
/// <para>集群部署：通过 <see cref="Factory"/> 注入集群工厂，使每个主题的总线在 PublishAsync 时同时推送到外部通道（如 Redis Stream 或星尘 StarDust）。各集群节点后台消费任务收到外部消息后，调用本地 <see cref="GetEventBus(String, String)"/> 获取总线并发布，即可唤醒任意节点上通过 <see cref="ReceiveAsync(String, CancellationToken)"/> 等待的调用方。</para>
/// </remarks>
public class EventHub<TEvent> : IEventHandler<IPacket>, IEventHandler<String>, ILogFeature, ITracerFeature
{
    #region 属性
    /// <summary>事件总线工厂</summary>
    /// <remarks>用于按主题创建事件总线</remarks>
    public IEventBusFactory? Factory { get; set; }

    /// <summary>Json主机</summary>
    public IJsonHost JsonHost { get; set; } = JsonHelper.Default;

    /// <summary>链路追踪</summary>
    public ITracer? Tracer { get; set; }

    /// <summary>已创建的主题事件总线</summary>
    /// <remarks>
    /// <para>服务端单例场景下：同一主题通常会被多个网络客户端共享订阅，因此这里按 topic 缓存总线实例。</para>
    /// </remarks>
    private readonly ConcurrentDictionary<String, IEventBus<TEvent>> _eventBuses = new();

    #endregion

    #region 注册
    /// <summary>添加事件总线到指定主题</summary>
    /// <param name="topic">主题名称</param>
    /// <param name="bus">事件总线实例</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="topic"/> 或 <paramref name="bus"/> 为 null 时抛出</exception>
    public void Add(String topic, IEventBus<TEvent> bus)
    {
        if (topic.IsNullOrEmpty()) throw new ArgumentNullException(nameof(topic));
        if (bus == null) throw new ArgumentNullException(nameof(bus));

        _eventBuses[topic] = bus;
    }

    /// <summary>按主题注册事件处理器</summary>
    /// <param name="topic">主题名称</param>
    /// <param name="handler">事件处理器，将通过其 <c>HandleAsync</c> 处理事件</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="topic"/> 或 <paramref name="handler"/> 为 null 时抛出</exception>
    public void Add(String topic, IEventHandler<TEvent> handler)
    {
        if (topic.IsNullOrEmpty()) throw new ArgumentNullException(nameof(topic));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        // 委托给主题总线，以统一路由路径；clientId="" 保持"最后注册者生效"语义
        GetEventBus(topic).Subscribe(handler, "");
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

        //if (Factory == null) throw new ArgumentNullException(nameof(Factory));

        using var span = Tracer?.NewSpan($"event:{topic}:Create", new { clientId });

        // 并发场景下允许多线程同时走到 CreateEventBus，但最终仅有一个实例会进入字典，其它实例会被丢弃。
        WriteLog("注册主题：{0}，客户端：{1}", topic, clientId);
        bus = Factory?.CreateEventBus<TEvent>(topic, clientId) ?? new EventBus<TEvent>();

        bus = _eventBuses.GetOrAdd(topic, bus);

        return bus;
    }

    /// <summary>尝试获取事件总线</summary>
    /// <param name="topic">主题名称</param>
    /// <param name="eventBus">输出的事件总线实例</param>
    public Boolean TryGetBus<T>(String topic, [MaybeNullWhen(false)] out IEventBus<T> eventBus)
    {
        if (_eventBuses.TryGetValue(topic, out var bus) && bus is IEventBus<T> bus2)
        {
            eventBus = bus2;
            return true;
        }

        eventBus = null;
        return false;
    }
    #endregion

    #region 消息解析
    private static readonly Byte[] _eventPrefixBytes = Encoding.ASCII.GetBytes("event#");
    private static readonly Char[] _eventPrefixChars = "event#".ToCharArray();

    private static Boolean TryParseEventHeader(ReadOnlySpan<Byte> data, out String topic, out String clientId, out Int32 headerLength)
    {
        topic = clientId = String.Empty;
        headerLength = 0;
        if (!data.StartsWith(_eventPrefixBytes)) return false;

        var p = data.IndexOf((Byte)'#');
        var rest = data[(p + 1)..];
        var p2 = rest.IndexOf((Byte)'#');
        if (p2 <= 0) return false;
        topic = rest[..p2].ToStr();

        var rest2 = rest[(p2 + 1)..];
        var p3 = rest2.IndexOf((Byte)'#');
        if (p3 <= 0) return false;
        clientId = rest2[..p3].ToStr();

        headerLength = p + 1 + p2 + 1 + p3 + 1;
        return true;
    }

    private static Boolean TryParseEventHeader(ReadOnlySpan<Char> data, out String topic, out String clientId, out Int32 headerLength)
    {
        topic = clientId = String.Empty;
        headerLength = 0;
        if (!data.StartsWith(_eventPrefixChars)) return false;

        var p = data.IndexOf('#');
        var rest = data[(p + 1)..];
        var p2 = rest.IndexOf('#');
        if (p2 <= 0) return false;
        topic = rest[..p2].ToString();

        var rest2 = rest[(p2 + 1)..];
        var p3 = rest2.IndexOf('#');
        if (p3 <= 0) return false;
        clientId = rest2[..p3].ToString();

        headerLength = p + 1 + p2 + 1 + p3 + 1;
        return true;
    }
    #endregion

    #region 消息处理
    /// <summary>处理接收到的消息</summary>
    /// <remarks>
    /// <para>消息格式：<c>event#topic#clientId#message</c>。当匹配前缀 <c>event#</c> 时解析并路由。</para>
    /// <para>解析后会将原始数据包保存到 <c>context["Raw"]</c>，便于订阅者直接转发原始报文实现零拷贝。</para>
    /// </remarks>
    /// <param name="data">消息数据包</param>
    /// <param name="context">事件上下文。用于在发布者、订阅者及中间处理器之间传递协调数据，如 Handler、ClientId 等</param>
    /// <param name="cancellationToken">取消通知标记</param>
    /// <returns>异步任务，结果为已处理事件数量（0 表示未处理）</returns>
    public virtual async Task<Int32> HandleAsync(IPacket data, IEventContext? context = null, CancellationToken cancellationToken = default)
    {
        if (!TryParseEventHeader(data.GetSpan(), out var topic, out var clientId, out var headerLen)) return 0;

        using var span = Tracer?.NewSpan($"event:{topic}:Dispatch", new { clientId }, data.Total - headerLen);
        var msg = data.Slice(headerLen);
        if (msg.Length == 0) return 0;

        // 保存原始数据包到上下文，便于订阅者直接转发原始报文（零拷贝）
        if (context is IExtend ext) ext["Raw"] = data;

        // 若 TEvent 本身是 IPacket，无需字符串化，直接分发原始数据包
        if (msg is TEvent @event)
            return await DispatchAsync(topic, clientId, @event, context, cancellationToken).ConfigureAwait(false);

        var msgStr = msg.ToStr();
        span?.AppendTag(msgStr);
        return await RouteMessageAsync(topic, clientId, msgStr, context, cancellationToken).ConfigureAwait(false);
    }

    Task IEventHandler<IPacket>.HandleAsync(IPacket @event, IEventContext? context, CancellationToken cancellationToken) => HandleAsync(@event, context, cancellationToken);

    /// <summary>处理接收到的消息</summary>
    /// <remarks>
    /// <para>消息格式：<c>event#topic#clientId#message</c>。当匹配前缀 <c>event#</c> 时解析并路由。</para>
    /// <para>解析后会将原始消息字符串保存到 <c>context["Raw"]</c>，便于订阅者直接转发原始报文。</para>
    /// </remarks>
    /// <param name="data">消息字符串</param>
    /// <param name="context">事件上下文。用于在发布者、订阅者及中间处理器之间传递协调数据，如 Handler、ClientId 等</param>
    /// <param name="cancellationToken">取消通知标记</param>
    /// <returns>异步任务，结果为已处理事件数量（0 表示未处理）</returns>
    public virtual async Task<Int32> HandleAsync(String data, IEventContext? context = null, CancellationToken cancellationToken = default)
    {
        if (!TryParseEventHeader(data.AsSpan(), out var topic, out var clientId, out var headerLen)) return 0;

        using var span = Tracer?.NewSpan($"event:{topic}:Dispatch", new { clientId }, data.Length - headerLen);
        // 解析 message：可能是 JSON 事件体，也可能是 subscribe/unsubscribe 控制指令。
        var msg = data[headerLen..];
        if (msg.Length == 0) return 0;

        // 保存原始消息字符串到上下文，便于订阅者直接转发原始报文
        if (context is IExtend ext) ext["Raw"] = data;

        span?.AppendTag(msg);
        return await RouteMessageAsync(topic, clientId, msg, context, cancellationToken).ConfigureAwait(false);
    }

    Task IEventHandler<String>.HandleAsync(String @event, IEventContext? context, CancellationToken cancellationToken) => HandleAsync(@event, context, cancellationToken);
    #endregion

    #region 消息分发
    private async Task<Int32> RouteMessageAsync(String topic, String clientId, String msg, IEventContext? context, CancellationToken cancellationToken)
    {
        // 控制指令：subscribe / unsubscribe（短字符串且不以 { 开头）
        if (msg[0] != '{' && msg.Length < 32)
            if (await DispatchActionAsync(topic, clientId, msg, context, cancellationToken).ConfigureAwait(false)) return 1;

        // 字符串事件（TEvent = String）
        if (msg is TEvent @event)
            return await DispatchAsync(topic, clientId, @event, context, cancellationToken).ConfigureAwait(false);

        // JSON 反序列化后分发
        return await OnDispatchAsync(topic, clientId, msg, context, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>处理动作指令</summary>
    /// <remarks>
    /// 处理 subscribe/unsubscribe 等控制指令。
    /// </remarks>
    /// <param name="topic">主题名称</param>
    /// <param name="clientId">客户端标识</param>
    /// <param name="action">动作指令（subscribe/unsubscribe）</param>
    /// <param name="context">事件上下文。用于在发布者、订阅者及中间处理器之间传递协调数据，如 Handler、ClientId 等</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功处理该动作</returns>
    public virtual Task<Boolean> DispatchActionAsync(String topic, String clientId, String action, IEventContext? context = null, CancellationToken cancellationToken = default)
    {
        if (action.IsNullOrEmpty() || action[0] == '{') return Task.FromResult(false);

        using var span = Tracer?.NewSpan($"event:{topic}:{action}", new { clientId });

        // 订阅和取消订阅动作。event#topic#clientId#subscribe
        switch (action)
        {
            case "subscribe":
                {
                    // 订阅动作时，必须指定事件处理器
                    if ((context as IExtend)?["Handler"] is not IEventHandler<TEvent> handler)
                        throw new ArgumentNullException(nameof(context), "订阅动作时，必须在上下文中指定事件处理器");

                    // 服务端：为该 topic 创建/获取事件总线，并将当前 handler 绑定到 clientId。
                    // clientId 用于区分网络客户端订阅者，便于取消订阅或按连接分组投递。
                    var bus = GetEventBus(topic, clientId);

                    WriteLog("订阅主题：{0}，客户端：{1}", topic, clientId);
                    bus.Subscribe(handler, clientId);
                }
                return Task.FromResult(true);
            case "unsubscribe":
                {
                    WriteLog("取消订阅主题：{0}，客户端：{1}", topic, clientId);

                    if (!TryGetBus<TEvent>(topic, out var bus)) return Task.FromResult(false);

                    bus.Unsubscribe(clientId);

                    // 服务端：如果没有订阅者则注销该 topic 的总线，避免主题长期占用内存。
                    if (bus is EventBus<TEvent> mbus && mbus.Handlers.Count == 0)
                    {
                        _eventBuses.TryRemove(topic, out _);

                        WriteLog("注销主题：{0}，因订阅为空", topic);
                    }
                }
                return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    /// <summary>分发字符串事件给各个处理器</summary>
    /// <remarks>
    /// <para>进程内分发：按 <c>topic</c> 路由到事件总线或回调。</para>
    /// <para>优先级：先查找已缓存的事件总线，再查找直接注册的分发器。</para>
    /// </remarks>
    /// <param name="topic">主题名称</param>
    /// <param name="clientId">发送方客户端标识</param>
    /// <param name="msg">事件实例</param>
    /// <param name="context">事件上下文。用于在发布者、订阅者及中间处理器之间传递协调数据，如 Handler、ClientId 等</param>
    /// <param name="cancellationToken">取消通知标记</param>
    /// <returns>异步任务，结果为已处理事件数量（0 表示未处理）</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="topic"/> 为空时抛出</exception>
    protected virtual Task<Int32> OnDispatchAsync(String topic, String clientId, String msg, IEventContext? context = null, CancellationToken cancellationToken = default)
    {
        var @event = JsonHost.Read<TEvent>(msg, null)!;
        if (@event is ITraceMessage tm && DefaultSpan.Current is ISpan span)
            span.Detach(tm.TraceId);

        return DispatchAsync(topic, clientId, @event, context, cancellationToken);
    }

    /// <summary>分发事件给各个处理器</summary>
    /// <remarks>按 <c>topic</c> 路由到对应的事件总线。</remarks>
    /// <param name="topic">主题名称</param>
    /// <param name="clientId">发送方客户端标识</param>
    /// <param name="event">事件实例</param>
    /// <param name="context">事件上下文。用于在发布者、订阅者及中间处理器之间传递协调数据，如 Handler、ClientId 等</param>
    /// <param name="cancellationToken">取消通知标记</param>
    /// <returns>异步任务，结果为已处理事件数量（0 表示未处理）</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="topic"/> 为空时抛出</exception>
    public virtual async Task<Int32> DispatchAsync(String topic, String clientId, TEvent @event, IEventContext? context = null, CancellationToken cancellationToken = default)
    {
        if (topic.IsNullOrEmpty()) throw new ArgumentNullException(nameof(topic));

        // 设置上下文的 ClientId，用于事件总线在分发时排除发送方自身
        if (context is EventContext ctx)
        {
            ctx.Topic = topic;
            ctx.ClientId = clientId;
        }
        else if (context is IExtend ext)
        {
            ext["Topic"] = topic;
            ext["ClientId"] = clientId;
        }

        if (_eventBuses.TryGetValue(topic, out var bus))
            return await bus.PublishAsync(@event, context, cancellationToken).ConfigureAwait(false);

        return 0;
    }

    /// <summary>异步阻塞等待指定主题的一条事件消息</summary>
    /// <remarks>
    /// <para>接收完成（或超时/取消）后，若该主题已无订阅者，将自动从枢纽移除空闲总线。</para>
    /// <para>典型用法（IoT 指令确认）：
    /// <code>
    /// // 下发指令
    /// websocket.Send(cmdJson);
    /// // 等待设备应答（30 秒超时）
    /// var result = await hub.ReceiveAsync(nodeCode, TimeSpan.FromSeconds(30));
    ///
    /// // 设备上报 HTTP 接口内
    /// await hub.PublishAsync(nodeCode, cmdResult);
    /// </code>
    /// </para>
    /// </remarks>
    /// <param name="topic">事件主题</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>等待到的第一条事件</returns>
    public async Task<TEvent> ReceiveAsync(String topic, CancellationToken cancellationToken = default)
    {
        var bus = GetEventBus(topic);
        try
        {
            return await bus.ReceiveAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // 自动清理：若该主题已无订阅者，从枢纽移除，避免长期积累空闲总线
            if (bus is EventBus<TEvent> eb && eb.Handlers.Count == 0)
                _eventBuses.TryRemove(topic, out _);
        }
    }

    /// <summary>异步阻塞等待指定主题的一条事件消息，超时后抛出 <see cref="OperationCanceledException"/></summary>
    /// <param name="topic">事件主题</param>
    /// <param name="timeout">等待超时时间</param>
    /// <returns>等待到的第一条事件</returns>
    public async Task<TEvent> ReceiveAsync(String topic, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        return await ReceiveAsync(topic, cts.Token).ConfigureAwait(false);
    }

    /// <summary>向指定主题发布事件</summary>
    /// <remarks>
    /// <para>便捷方法，等价于 <c>GetEvent(topic).PublishAsync(@event)</c>，适用于服务端回调接口中直接发布。</para>
    /// <para>若该主题无订阅者，返回 0 且不产生异常。</para>
    /// </remarks>
    /// <param name="topic">事件主题</param>
    /// <param name="event">事件实例</param>
    /// <param name="context">事件上下文，为 null 时由总线自动创建</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功处理该事件的处理器数量</returns>
    public Task<Int32> PublishAsync(String topic, TEvent @event, IEventContext? context = null, CancellationToken cancellationToken = default) => DispatchAsync(topic, "", @event, context, cancellationToken);
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
