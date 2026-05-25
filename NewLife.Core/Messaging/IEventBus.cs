using NewLife.Data;

namespace NewLife.Messaging;

/// <summary>事件上下文</summary>
/// <remarks>
/// <para>事件分发过程中携带的协调对象，可由订阅者通过 <see cref="IExtend"/> 接口读写自定义数据。</para>
/// <para>新代码建议关注 <see cref="EventContext"/> 的 Topic/ClientId 数据项；
/// 非泛型 <see cref="IEventBus"/> 引用仅为兼容性保留，可视为类型标记。</para>
/// </remarks>
public interface IEventContext
{
    /// <summary>事件总线</summary>
    /// <remarks>
    /// <para>由总线创建上下文时填充，便于处理器访问总线能力（如再次发布事件）。</para>
    /// <para>注意：当上下文来自对象池时，该属性在分发完成、上下文归还时会被重置为 <see langword="null"/>；
    /// 处理器内若需保留总线引用，应在回调内立即捕获到局部变量。</para>
    /// </remarks>
    IEventBus? EventBus { get; }
}

/// <summary>事件处理器</summary>
/// <remarks>
/// 实现应尽量保持幂等（允许重复投递时不产生副作用），并在耗时操作中尊重 <c>cancellationToken</c>。
/// </remarks>
/// <typeparam name="TEvent">事件类型</typeparam>
public interface IEventHandler<TEvent>
{
    /// <summary>处理事件</summary>
    /// <param name="event">事件</param>
    /// <param name="context">事件上下文。用于在发布者、订阅者及中间处理器之间传递协调数据，如 Topic、ClientId 等</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示处理过程的任务</returns>
    Task HandleAsync(TEvent @event, IEventContext? context, CancellationToken cancellationToken);
}

/// <summary>事件总线工厂</summary>
/// <remarks>
/// 通过实现本接口，可为每个主题创建自定义的事件总线实例：
/// <list type="bullet">
/// <item><description><b>进程内总线（默认）</b>：不设置工厂时，<see cref="EventHub{TEvent}.GetEventBus"/> 自动创建 <see cref="EventBus{TEvent}"/>。</description></item>
/// <item><description><b>集群总线（扩展点）</b>：实现集群工厂，在 PublishAsync 时先向本地 <see cref="EventBus{TEvent}"/> 分发（唤醒同进程等待者），
/// 再推送到外部消息通道（Redis Stream / 星尘 StarDust 等）。各集群节点后台消费任务收到外部消息后，
/// 调用本地 <see cref="EventHub{TEvent}.GetEventBus"/> 发布，即可唤醒任意节点上通过 ReceiveAsync 等待的调用方。</description></item>
/// </list>
/// </remarks>
public interface IEventBusFactory
{
    /// <summary>创建事件总线，可发布消息或订阅消息</summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="topic">事件主题</param>
    /// <param name="clientId">客户标识/消息分组</param>
    /// <returns>事件总线实例</returns>
    IEventBus<TEvent> CreateEventBus<TEvent>(String topic, String clientId = "");
}

/// <summary>事件总线。强类型的事件发布/订阅，统一异步 API</summary>
/// <remarks>
/// <para><b>设计理念</b>：以异步为一等公民。三大基础操作均为异步签名，方便桥接到远程总线、Redis、消息队列等异步实现；
/// 同步调用方可通过 <see cref="EventBusExtensions"/> 提供的扩展方法或直接 <c>.Result</c> 退化使用。</para>
/// <para><b>实现约束</b>：</para>
/// <list type="bullet">
/// <item><description><b>幂等订阅</b>：相同 <c>clientId</c> 重复订阅应覆盖前一次订阅，不抛异常。</description></item>
/// <item><description><b>排除回环</b>：发布时若 <see cref="IEventContext"/> 携带 <c>ClientId</c>，应跳过同名订阅者。</description></item>
/// <item><description><b>错误隔离</b>：默认实现不应让单个订阅者异常中断其他订阅者（严格模式由具体实现自行开关）。</description></item>
/// </list>
/// </remarks>
/// <typeparam name="TEvent">事件类型</typeparam>
public interface IEventBus<TEvent>
{
    /// <summary>发布事件</summary>
    /// <param name="event">事件</param>
    /// <param name="context">事件上下文。可携带主题、客户端标识等元数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功处理该事件的处理器数量</returns>
    Task<Int32> PublishAsync(TEvent @event, IEventContext? context = null, CancellationToken cancellationToken = default);

    /// <summary>订阅事件</summary>
    /// <param name="handler">事件处理器</param>
    /// <param name="clientId">客户标识。同一标识重复订阅时覆盖前一次</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否订阅成功</returns>
    Task<Boolean> SubscribeAsync(IEventHandler<TEvent> handler, String clientId = "", CancellationToken cancellationToken = default);

    /// <summary>取消订阅</summary>
    /// <param name="clientId">客户标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功取消订阅</returns>
    Task<Boolean> UnsubscribeAsync(String clientId = "", CancellationToken cancellationToken = default);
}

/// <summary>非泛型事件总线标记接口。仅为兼容旧版 <see cref="IEventContext.EventBus"/> 字段保留</summary>
/// <remarks>
/// <para>新代码请使用强类型 <see cref="IEventBus{TEvent}"/>。本接口在新架构中仅作类型标记，不再承载方法成员。</para>
/// <para>历史上本接口提供 <c>PublishAsync(Object)</c>；该方法由具体实现 <see cref="EventBus{TEvent}"/> 通过显式接口实现继续提供。</para>
/// </remarks>
public interface IEventBus
{
    /// <summary>发布事件（弱类型）</summary>
    /// <param name="event">事件对象</param>
    /// <param name="context">事件上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功处理该事件的处理器数量</returns>
    Task<Int32> PublishAsync(Object @event, IEventContext? context = null, CancellationToken cancellationToken = default);
}

/// <summary>异步事件总线。历史接口，等价于 <see cref="IEventBus{TEvent}"/></summary>
/// <remarks>
/// <para>在新架构中 <see cref="IEventBus{TEvent}"/> 已直接提供 <c>SubscribeAsync</c>/<c>UnsubscribeAsync</c>，
/// 因此本接口退化为空别名。仅为兼容旧引用而保留。</para>
/// </remarks>
/// <typeparam name="TEvent">事件类型</typeparam>
public interface IAsyncEventBus<TEvent> : IEventBus<TEvent>
{
}
