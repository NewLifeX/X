using System.Collections.Concurrent;
using System.Text;
using NewLife.Data;
using NewLife.Log;
using NewLife.Serialization;

namespace NewLife.Messaging;

/// <summary>事件分发枢纽。按主题路由网络消息或本地事件到各事件总线</summary>
/// <remarks>
/// <para><b>核心职责</b>：</para>
/// <list type="number">
/// <item><description><b>主题路由</b>：维护 <c>topic → IEventBus&lt;TEvent&gt;</c> 映射，提供 <see cref="GetEventBus"/>/<see cref="RegisterBus"/>。</description></item>
/// <item><description><b>协议适配</b>：默认解析 <c>event#topic#clientId#message</c> 文本协议；
/// 派生类可重写 <see cref="TryDecode(IPacket,out EventEnvelope)"/> / <see cref="TryDecode(String,out EventEnvelope)"/> 替换为二进制、MQTT 等协议。</description></item>
/// <item><description><b>控制面 + 数据面</b>：解析后区分动作（订阅/取消订阅）与数据消息，分别走 <see cref="SubscribeAsync"/>/<see cref="UnsubscribeAsync"/> 或 <see cref="PublishAsync"/>。</description></item>
/// </list>
/// <para><b>典型应用</b>：作为 IoT 网关或 MQ Bridge，将网络层（如 WebSocket/TCP）收到的消息按 topic 投递到进程内订阅者。</para>
/// </remarks>
/// <typeparam name="TEvent">事件类型</typeparam>
public class EventHub<TEvent> : IEventHandler<IPacket>, IEventHandler<String>, ILogFeature, ITracerFeature
{
    #region 嵌套：事件信封
    /// <summary>协议解码的中间结果，承载主题/客户端/动作或事件三种语义</summary>
    protected readonly struct EventEnvelope
    {
        /// <summary>主题</summary>
        public String Topic { get; }

        /// <summary>客户端标识。发送方，路由分发时用于排除回环</summary>
        public String ClientId { get; }

        /// <summary>动作。订阅/取消订阅控制指令；为空表示数据消息</summary>
        public String? Action { get; }

        /// <summary>已解码的事件实例</summary>
        public TEvent? Event { get; }

        /// <summary>是否动作信封</summary>
        public Boolean IsAction => !Action.IsNullOrEmpty();

        /// <summary>构造动作信封</summary>
        /// <param name="topic">主题</param>
        /// <param name="clientId">客户端标识</param>
        /// <param name="action">动作指令</param>
        public static EventEnvelope ForAction(String topic, String clientId, String action) => new(topic, clientId, action, default);

        /// <summary>构造事件信封</summary>
        /// <param name="topic">主题</param>
        /// <param name="clientId">客户端标识</param>
        /// <param name="event">事件实例</param>
        public static EventEnvelope ForEvent(String topic, String clientId, TEvent @event) => new(topic, clientId, null, @event);

        private EventEnvelope(String topic, String clientId, String? action, TEvent? @event)
        {
            Topic = topic;
            ClientId = clientId;
            Action = action;
            Event = @event;
        }
    }
    #endregion

    #region 属性
    /// <summary>事件总线工厂。用于按需创建各 topic 对应的事件总线</summary>
    public IEventBusFactory? Factory { get; set; }

    /// <summary>JSON 主机。用于编解码事件体</summary>
    public IJsonHost JsonHost { get; set; } = JsonHelper.Default;

    /// <summary>动作判定阈值。消息体短于该长度且不以 <c>{</c> 开头时视为控制动作指令</summary>
    public Int32 ActionMaxLength { get; set; } = 32;

    /// <summary>链路追踪</summary>
    public ITracer? Tracer { get; set; }

    private readonly ConcurrentDictionary<String, IEventBus<TEvent>> _eventBuses = new();
    /// <summary>已注册的事件总线集合。Key 为 topic</summary>
    public IDictionary<String, IEventBus<TEvent>> EventBuses => _eventBuses;

    private static readonly Byte[] _prefixBytes = Encoding.ASCII.GetBytes("event#");
    private static readonly Char[] _prefixChars = "event#".ToCharArray();
    #endregion

    #region 注册与获取
    /// <summary>注册主题对应的事件总线</summary>
    /// <param name="topic">事件主题</param>
    /// <param name="eventBus">事件总线实例</param>
    public void RegisterBus(String topic, IEventBus<TEvent> eventBus) => _eventBuses[topic] = eventBus;

    /// <summary>尝试获取主题对应的事件总线（不会创建新的）</summary>
    /// <param name="topic">事件主题</param>
    /// <param name="eventBus">事件总线</param>
    /// <returns>是否存在</returns>
    public Boolean TryGetBus(String topic, out IEventBus<TEvent> eventBus) => _eventBuses.TryGetValue(topic, out eventBus!);

    /// <summary>获取或创建主题对应的事件总线</summary>
    /// <param name="topic">事件主题</param>
    /// <param name="clientId">客户标识，仅在使用 <see cref="Factory"/> 创建时传递</param>
    /// <returns>事件总线实例</returns>
    public virtual IEventBus<TEvent> GetEventBus(String topic, String clientId = "")
    {
        if (_eventBuses.TryGetValue(topic, out var bus)) return bus;
        bus = Factory?.CreateEventBus<TEvent>(topic, clientId) ?? new EventBus<TEvent>();
        return _eventBuses.GetOrAdd(topic, bus);
    }
    #endregion

    #region 发布与订阅
    /// <summary>向指定主题发布事件</summary>
    /// <remarks>
    /// 发送方信息（用于排除回环）通过 <see cref="EventContext.ClientId"/> 传递。
    /// 无需排除回环时可传入 <see langword="null"/>，由总线自动创建上下文。
    /// </remarks>
    /// <param name="topic">主题</param>
    /// <param name="event">事件</param>
    /// <param name="context">事件上下文；为空时由总线自行创建</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功处理该事件的处理器数量</returns>
    public virtual Task<Int32> PublishAsync(String topic, TEvent @event, IEventContext? context = null, CancellationToken cancellationToken = default)
    {
        var bus = GetEventBus(topic);

        // 自动注入 Topic 到上下文，便于订阅者读取
        if (context is EventContext ctx) ctx.Topic ??= topic;

        return bus.PublishAsync(@event, context, cancellationToken);
    }

    /// <summary>向指定主题订阅事件</summary>
    /// <param name="topic">主题</param>
    /// <param name="clientId">客户标识</param>
    /// <param name="handler">事件处理器</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否订阅成功</returns>
    public virtual Task<Boolean> SubscribeAsync(String topic, String clientId, IEventHandler<TEvent> handler, CancellationToken cancellationToken = default) => GetEventBus(topic, clientId).SubscribeAsync(handler, clientId, cancellationToken);

    /// <summary>取消指定主题/客户端的订阅</summary>
    /// <param name="topic">主题</param>
    /// <param name="clientId">客户标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功取消订阅</returns>
    public virtual async Task<Boolean> UnsubscribeAsync(String topic, String clientId, CancellationToken cancellationToken = default)
    {
        if (!_eventBuses.TryGetValue(topic, out var bus)) return false;

        var ok = await bus.UnsubscribeAsync(clientId, cancellationToken).ConfigureAwait(false);

        // 取消后若已无订阅者且为默认实现，移除总线避免泄漏
        if (bus is EventBus<TEvent> eb && eb.Handlers.Count == 0) _eventBuses.TryRemove(topic, out _);

        return ok;
    }
    #endregion

    #region 协议解码（可重载）
    /// <summary>尝试从二进制数据包解码事件信封。派生类可重写以替换协议实现</summary>
    /// <param name="data">网络数据包</param>
    /// <param name="envelope">解码出的事件信封</param>
    /// <returns>是否解码成功</returns>
    protected virtual Boolean TryDecode(IPacket data, out EventEnvelope envelope)
    {
        envelope = default;
        if (data == null) return false;
        if (!TryParseHeader(data.GetSpan(), out var topic, out var clientId, out var headerLen)) return false;

        var msg = data.Slice(headerLen);
        if (msg.Length == 0) return false;

        // TEvent 本身是 IPacket，直接零拷贝构造事件信封
        if (msg is TEvent evt) { envelope = EventEnvelope.ForEvent(topic, clientId, evt); return true; }

        var msgStr = msg.ToStr();
        return BuildEnvelopeFromString(topic, clientId, msgStr, out envelope);
    }

    /// <summary>尝试从字符串解码事件信封。派生类可重写以替换协议实现</summary>
    /// <param name="data">字符串消息</param>
    /// <param name="envelope">解码出的事件信封</param>
    /// <returns>是否解码成功</returns>
    protected virtual Boolean TryDecode(String data, out EventEnvelope envelope)
    {
        envelope = default;
        if (data.IsNullOrEmpty()) return false;
        if (!TryParseHeader(data.AsSpan(), out var topic, out var clientId, out var headerLen)) return false;

        var msg = data[headerLen..];
        if (msg.Length == 0) return false;

        return BuildEnvelopeFromString(topic, clientId, msg, out envelope);
    }

    /// <summary>编码事件为字符串（便于通过文本协议发送）</summary>
    /// <param name="topic">主题</param>
    /// <param name="clientId">客户端标识</param>
    /// <param name="event">事件实例</param>
    /// <returns>编码后的字符串</returns>
    protected virtual String EncodeEvent(String topic, String clientId, TEvent @event)
    {
        var body = @event is String s ? s : JsonHost.Write(@event!);
        return $"event#{topic}#{clientId}#{body}";
    }

    /// <summary>编码控制动作为字符串（订阅/取消订阅等）</summary>
    /// <param name="topic">主题</param>
    /// <param name="clientId">客户端标识</param>
    /// <param name="action">动作指令</param>
    /// <returns>编码后的字符串</returns>
    protected virtual String EncodeAction(String topic, String clientId, String action) => $"event#{topic}#{clientId}#{action}";

    /// <summary>根据消息体字符串构造事件信封：动作 / 字符串事件 / JSON 解码</summary>
    private Boolean BuildEnvelopeFromString(String topic, String clientId, String msg, out EventEnvelope envelope)
    {
        // TEvent = String，整条消息体始终视为事件，不识别动作（避免短消息被误判为控制指令）
        if (msg is TEvent strEvt)
        {
            envelope = EventEnvelope.ForEvent(topic, clientId, strEvt);
            return true;
        }

        // 短字符串且不以 { 开头 → 视为动作指令
        if (msg[0] != '{' && msg.Length < ActionMaxLength)
        {
            envelope = EventEnvelope.ForAction(topic, clientId, msg);
            return true;
        }

        // JSON 反序列化
        var evt = JsonHost.Read<TEvent>(msg, null);
        if (evt == null)
        {
            envelope = default;
            return false;
        }
        envelope = EventEnvelope.ForEvent(topic, clientId, evt);
        return true;
    }

    /// <summary>解析 <c>event#topic#clientId#</c> 二进制头部</summary>
    /// <param name="data">输入数据</param>
    /// <param name="topic">输出主题</param>
    /// <param name="clientId">输出客户端标识</param>
    /// <param name="headerLength">头部字节数（含末尾 #）</param>
    /// <returns>是否解析成功</returns>
    public static Boolean TryParseHeader(ReadOnlySpan<Byte> data, out String topic, out String clientId, out Int32 headerLength)
    {
        topic = clientId = String.Empty;
        headerLength = 0;
        if (!data.StartsWith(_prefixBytes)) return false;

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

    /// <summary>解析 <c>event#topic#clientId#</c> 字符串头部</summary>
    /// <param name="data">输入数据</param>
    /// <param name="topic">输出主题</param>
    /// <param name="clientId">输出客户端标识</param>
    /// <param name="headerLength">头部字符数（含末尾 #）</param>
    /// <returns>是否解析成功</returns>
    public static Boolean TryParseHeader(ReadOnlySpan<Char> data, out String topic, out String clientId, out Int32 headerLength)
    {
        topic = clientId = String.Empty;
        headerLength = 0;
        if (!data.StartsWith(_prefixChars)) return false;

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

    #region 网络消息接收
    /// <summary>接收网络字节流消息并按协议解码后路由</summary>
    /// <param name="data">网络数据包</param>
    /// <param name="context">事件上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分发到本地订阅者的处理器数量</returns>
    public virtual async Task<Int32> OnReceiveAsync(IPacket data, IEventContext? context = null, CancellationToken cancellationToken = default)
    {
        if (data == null) return 0;
        if (!TryDecode(data, out var envelope)) return 0;

        return await DispatchEnvelopeAsync(envelope, data, context, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>接收字符串消息并按协议解码后路由</summary>
    /// <param name="data">字符串消息</param>
    /// <param name="context">事件上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分发到本地订阅者的处理器数量</returns>
    public virtual async Task<Int32> OnReceiveAsync(String data, IEventContext? context = null, CancellationToken cancellationToken = default)
    {
        if (data.IsNullOrEmpty()) return 0;
        if (!TryDecode(data, out var envelope)) return 0;

        return await DispatchEnvelopeAsync(envelope, data, context, cancellationToken).ConfigureAwait(false);
    }

    Task IEventHandler<IPacket>.HandleAsync(IPacket @event, IEventContext? context, CancellationToken cancellationToken) => OnReceiveAsync(@event, context, cancellationToken);
    Task IEventHandler<String>.HandleAsync(String @event, IEventContext? context, CancellationToken cancellationToken) => OnReceiveAsync(@event, context, cancellationToken);

    /// <summary>把解码后的事件信封路由到控制面或数据面</summary>
    private async Task<Int32> DispatchEnvelopeAsync(EventEnvelope envelope, Object raw, IEventContext? context, CancellationToken cancellationToken)
    {
        // 把原始数据透传到扩展项，便于网络层做后续路由（如转发到其他客户端）
        if (context is IExtend ext) ext["Raw"] = raw;

        // 控制面：订阅/取消订阅
        if (envelope.IsAction)
        {
            var action = envelope.Action!;
            if (action.EqualIgnoreCase("subscribe"))
            {
                if ((context as IExtend)?["Handler"] is IEventHandler<TEvent> handler)
                {
                    await SubscribeAsync(envelope.Topic, envelope.ClientId, handler, cancellationToken).ConfigureAwait(false);
                    return 1;
                }
                return 0;
            }
            if (action.EqualIgnoreCase("unsubscribe"))
            {
                await UnsubscribeAsync(envelope.Topic, envelope.ClientId, cancellationToken).ConfigureAwait(false);
                return 1;
            }
            return 0;
        }

        // 数据面：发布事件
        if (envelope.Event is null) return 0;

        // 将发送方 ClientId 注入上下文，便于本地总线排除回环
        if (context is EventContext ec)
        {
            ec.ClientId ??= envelope.ClientId;
        }
        else if (context == null && !envelope.ClientId.IsNullOrEmpty())
        {
            context = new EventContext { Topic = envelope.Topic, ClientId = envelope.ClientId };
        }

        return await PublishAsync(envelope.Topic, envelope.Event, context, cancellationToken).ConfigureAwait(false);
    }
    #endregion

    #region 等待接收
    /// <summary>异步等待指定主题的第一条事件</summary>
    /// <param name="topic">主题</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>事件实例</returns>
    public async Task<TEvent> ReceiveAsync(String topic, CancellationToken cancellationToken = default)
    {
        var bus = GetEventBus(topic);
        try
        {
            return await bus.ReceiveAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // 等待完成后若无其他订阅者，移除总线避免泄漏
            if (bus is EventBus<TEvent> eb && eb.Handlers.Count == 0) _eventBuses.TryRemove(topic, out _);
        }
    }

    /// <summary>异步等待指定主题的第一条事件，超时后抛出 <see cref="OperationCanceledException"/></summary>
    /// <param name="topic">主题</param>
    /// <param name="timeout">超时时间</param>
    /// <returns>事件实例</returns>
    public async Task<TEvent> ReceiveAsync(String topic, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        return await ReceiveAsync(topic, cts.Token).ConfigureAwait(false);
    }
    #endregion

    #region 兼容层（旧 API，已标记废弃）
    /// <summary>分发事件到指定主题与发送方（旧 API）</summary>
    /// <remarks>请改用 <see cref="PublishAsync"/>，把 <c>clientId</c> 放入 <see cref="EventContext.ClientId"/></remarks>
    /// <param name="topic">主题</param>
    /// <param name="clientId">发送方客户端标识</param>
    /// <param name="event">事件</param>
    /// <param name="context">事件上下文；为 IExtend 时会写入 Topic/ClientId 数据项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功处理该事件的处理器数量；主题未注册时返回 0</returns>
    [Obsolete("请改用 PublishAsync(topic,event,context,ct)，将 clientId 放入 EventContext.ClientId。将在后续版本移除")]
    public Task<Int32> DispatchAsync(String topic, String clientId, TEvent @event, IEventContext? context = null, CancellationToken cancellationToken = default)
    {
        if (topic.IsNullOrEmpty()) throw new ArgumentNullException(nameof(topic));

        // 上下文字段需要先于注册检查写入，保持旧版可观察行为
        if (context is EventContext ec)
        {
            ec.Topic ??= topic;
            ec.ClientId ??= clientId;
        }
        if (context is IExtend ext)
        {
            ext["Topic"] ??= topic;
            ext["ClientId"] ??= clientId;
        }

        // 主题未注册时不创建总线，直接返回 0（保持旧语义）
        if (!_eventBuses.TryGetValue(topic, out var bus)) return TaskEx.FromResult(0);

        return bus.PublishAsync(@event, context, cancellationToken);
    }

    /// <summary>分发控制动作（subscribe/unsubscribe）（旧 API）</summary>
    /// <remarks>请改用 <see cref="SubscribeAsync"/> / <see cref="UnsubscribeAsync"/></remarks>
    /// <param name="topic">主题</param>
    /// <param name="clientId">客户标识</param>
    /// <param name="action">动作；非 subscribe/unsubscribe 返回 false</param>
    /// <param name="context">事件上下文；subscribe 需要在其中提供 <c>Handler</c> 数据项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>动作是否被识别并执行</returns>
    [Obsolete("请改用 SubscribeAsync/UnsubscribeAsync。将在后续版本移除")]
    public async Task<Boolean> DispatchActionAsync(String topic, String clientId, String action, IEventContext? context = null, CancellationToken cancellationToken = default)
    {
        if (action.IsNullOrEmpty() || action[0] == '{') return false;

        if (action.EqualIgnoreCase("subscribe"))
        {
            var handler = (context as IExtend)?["Handler"] as IEventHandler<TEvent>
                ?? throw new ArgumentNullException(nameof(context), "subscribe 动作需要在上下文中提供 Handler");
            await SubscribeAsync(topic, clientId, handler, cancellationToken).ConfigureAwait(false);
            return true;
        }
        if (action.EqualIgnoreCase("unsubscribe"))
        {
            // 旧语义：总线不存在时返回 false
            if (!_eventBuses.ContainsKey(topic)) return false;
            return await UnsubscribeAsync(topic, clientId, cancellationToken).ConfigureAwait(false);
        }
        return false;
    }

    /// <summary>注册主题与事件总线的映射（旧 API）</summary>
    /// <remarks>请改用 <see cref="RegisterBus"/></remarks>
    /// <param name="topic">主题</param>
    /// <param name="eventBus">事件总线</param>
    [Obsolete("请改用 RegisterBus。将在后续版本移除")]
    public void Add(String topic, IEventBus<TEvent> eventBus) => RegisterBus(topic, eventBus);

    /// <summary>注册主题对应的事件处理器（旧 API）</summary>
    /// <remarks>请改用 <see cref="SubscribeAsync"/></remarks>
    /// <param name="topic">主题</param>
    /// <param name="handler">事件处理器</param>
    [Obsolete("请改用 SubscribeAsync(topic, clientId, handler)。将在后续版本移除")]
    public void Add(String topic, IEventHandler<TEvent> handler) => SubscribeAsync(topic, "", handler).ConfigureAwait(false).GetAwaiter().GetResult();

    /// <summary>尝试获取指定泛型类型的事件总线（旧 API）</summary>
    /// <remarks>请改用非泛型 <see cref="TryGetBus"/></remarks>
    /// <typeparam name="T">事件类型；与 <typeparamref name="TEvent"/> 不一致时返回 false</typeparam>
    /// <param name="topic">主题</param>
    /// <param name="eventBus">事件总线</param>
    /// <returns>是否找到</returns>
    [Obsolete("请改用非泛型 TryGetBus(topic, out bus)。将在后续版本移除")]
    public Boolean TryGetBus<T>(String topic, out IEventBus<T>? eventBus)
    {
        if (typeof(T) == typeof(TEvent) && _eventBuses.TryGetValue(topic, out var bus))
        {
            eventBus = (IEventBus<T>)bus;
            return true;
        }
        eventBus = null;
        return false;
    }
    #endregion

    #region 日志
    /// <summary>日志</summary>
    public ILog Log { get; set; } = Logger.Null;

    /// <summary>写日志</summary>
    /// <param name="format">格式串</param>
    /// <param name="args">参数</param>
    public void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
    #endregion
}
