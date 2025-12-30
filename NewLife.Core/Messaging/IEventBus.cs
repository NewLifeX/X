using System.Collections.Concurrent;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
#if !NET45
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.Messaging;

/// <summary>事件总线基接口。非泛型版本，用于统一访问不同事件类型的总线</summary>
/// <remarks>
/// 该接口主要用于：
/// <list type="bullet">
/// <item><description>在不关心具体事件类型时，统一持有总线引用（如 <see cref="IEventContext.EventBus"/>）。</description></item>
/// <item><description>实现需要兼容多种事件类型的通用框架。</description></item>
/// </list>
/// 若需要订阅能力，请使用泛型版本 <see cref="IEventBus{TEvent}"/>。
/// </remarks>
public interface IEventBus
{
    /// <summary>发布事件</summary>
    /// <remarks>
    /// 默认实现通常为进程内即时分发：调用方发起发布后，会按订阅快照依次调用各处理器。
    /// 若传入 <paramref name="context"/> 则沿用该上下文；若为 <see langword="null"/> 则由总线创建（可能来自对象池）。
    /// </remarks>
    /// <param name="event">事件</param>
    /// <param name="context">事件上下文。用于在发布者、订阅者及中间处理器之间传递协调数据，如 Handler、ClientId 等</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功处理该事件的处理器数量</returns>
    Task<Int32> PublishAsync(Object @event, IEventContext? context = null, CancellationToken cancellationToken = default);
}

/// <summary>事件总线</summary>
/// <remarks>
/// 为什么要使用 EventBus：
/// 1，解耦：发布者和订阅者不需要知道对方的存在，只需要通过事件总线进行通信。
/// 2，扩展：可以方便的增加新的订阅者，不需要修改发布者的代码。
/// 3，性能：事件总线可以根据订阅者的数量和类型，选择合适的方式进行消息分发。
/// 4，可靠性：事件总线可以提供消息重试、消息持久化等功能。
/// 
/// 事件总线一般有两种实现和使用方式：
/// 1，基于泛型接口的事件总线，通过接口约定事件总线的行为，具体实现可以是内存、消息队列、数据库等。
/// 2，基于普通接口的实现，发布和订阅时指定主题Topic。
/// 这里采取第一种设计，不同业务领域可以实例化自己的事件总线，互不干扰。
/// 
/// 关于同步/异步设计：
/// <list type="bullet">
/// <item><description><see cref="PublishAsync"/> 和 <see cref="IEventHandler{TEvent}.HandleAsync"/> 采用异步，因为消息分发和处理通常涉及 I/O 操作。</description></item>
/// <item><description><see cref="Subscribe"/> 和 <see cref="Unsubscribe"/> 采用同步，适用于内存事件总线和点对点网络场景，保持 API 简洁。</description></item>
/// <item><description>大型分布式场景（订阅需网络往返）请使用 <see cref="IAsyncEventBus{TEvent}"/> 接口。</description></item>
/// </list>
/// </remarks>
/// <typeparam name="TEvent">事件类型</typeparam>
public interface IEventBus<TEvent>
{
    /// <summary>发布事件</summary>
    /// <remarks>
    /// 默认实现通常为进程内即时分发：调用方发起发布后，会按订阅快照依次调用各处理器。
    /// 若传入 <paramref name="context"/> 则沿用该上下文；若为 <see langword="null"/> 则由总线创建（可能来自对象池）。
    /// </remarks>
    /// <param name="event">事件</param>
    /// <param name="context">事件上下文。用于在发布者、订阅者及中间处理器之间传递协调数据，如 Handler、ClientId 等</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功处理该事件的处理器数量</returns>
    Task<Int32> PublishAsync(TEvent @event, IEventContext? context = null, CancellationToken cancellationToken = default);

    /// <summary>订阅事件</summary>
    /// <remarks>
    /// <paramref name="clientId"/> 用于识别订阅者，常用于“同一订阅者重连”的场景。
    /// 不同实现可将其用于消息分组（如同一组只投递给其中一个实例）。
    /// </remarks>
    /// <param name="handler">事件处理器</param>
    /// <param name="clientId">客户标识。每个客户只能订阅一次，重复订阅将会挤掉前一次订阅</param>
    /// <returns>是否订阅成功</returns>
    Boolean Subscribe(IEventHandler<TEvent> handler, String clientId = "");

    /// <summary>取消订阅</summary>
    /// <remarks>若未指定 <paramref name="clientId"/>，实现可约定取消默认/匿名订阅。</remarks>
    /// <param name="clientId">客户标识。订阅时使用的标识</param>
    /// <returns>是否成功取消订阅</returns>
    Boolean Unsubscribe(String clientId = "");
}

/// <summary>支持异步订阅的事件总线</summary>
/// <remarks>
/// 扩展自 <see cref="IEventBus{TEvent}"/>，为大型分布式场景提供异步订阅和取消订阅的能力。
/// 
/// 适用场景：
/// <list type="bullet">
/// <item><description>客户端-服务端两级架构：订阅动作需通过网络发送到服务端，存在网络延迟。</description></item>
/// <item><description>基于消息队列的实现：订阅可能涉及队列声明、绑定等异步操作。</description></item>
/// <item><description>需要确认订阅结果的场景：服务端可能拒绝订阅或返回额外信息。</description></item>
/// </list>
/// 
/// 实现建议：
/// <list type="bullet">
/// <item><description>同步方法 <see cref="IEventBus{TEvent}.Subscribe"/> 可委托给 <see cref="SubscribeAsync"/>，内部阻塞等待。</description></item>
/// <item><description>或者同步方法仅做本地注册，异步方法负责网络通信。</description></item>
/// </list>
/// </remarks>
/// <typeparam name="TEvent">事件类型</typeparam>
public interface IAsyncEventBus<TEvent> : IEventBus<TEvent>
{
    /// <summary>异步订阅事件</summary>
    /// <remarks>
    /// 适用于订阅动作需要网络往返或其他异步操作的场景。
    /// 实现应保证订阅的幂等性：相同 <paramref name="clientId"/> 重复订阅时覆盖前一次订阅。
    /// </remarks>
    /// <param name="handler">事件处理器</param>
    /// <param name="clientId">客户标识。每个客户只能订阅一次，重复订阅将会挤掉前一次订阅</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否订阅成功</returns>
    Task<Boolean> SubscribeAsync(IEventHandler<TEvent> handler, String clientId = "", CancellationToken cancellationToken = default);

    /// <summary>异步取消订阅</summary>
    /// <remarks>
    /// 适用于取消订阅需要网络往返或其他异步操作的场景。
    /// 若未指定 <paramref name="clientId"/>，实现可约定取消默认/匿名订阅。
    /// </remarks>
    /// <param name="clientId">客户标识。订阅时使用的标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功取消订阅</returns>
    Task<Boolean> UnsubscribeAsync(String clientId = "", CancellationToken cancellationToken = default);
}

/// <summary>事件上下文接口</summary>
public interface IEventContext
{
    /// <summary>事件总线</summary>
    /// <remarks>处理器可以通过该属性访问总线能力（如再次发布事件）。</remarks>
    IEventBus EventBus { get; }
}

/// <summary>事件处理器</summary>
/// <typeparam name="TEvent">事件类型</typeparam>
public interface IEventHandler<TEvent>
{
    /// <summary>处理事件</summary>
    /// <remarks>
    /// 实现应尽量保持幂等（允许重复投递时不产生副作用），并在耗时操作中尊重 <paramref name="cancellationToken"/>。
    /// </remarks>
    /// <param name="event">事件</param>
    /// <param name="context">事件上下文。用于在发布者、订阅者及中间处理器之间传递协调数据，如 Handler、ClientId 等</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示处理过程的任务</returns>
    Task HandleAsync(TEvent @event, IEventContext? context, CancellationToken cancellationToken);
}

/// <summary>事件总线工厂</summary>
public interface IEventBusFactory
{
    /// <summary>创建事件总线，可发布消息或订阅消息</summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <remarks>
    /// 工厂通常负责按主题隔离不同业务域的事件通道。
    /// 对于支持“消费组”的实现，<paramref name="clientId"/> 常用于表示同组内竞争消费。
    /// </remarks>
    /// <param name="topic">事件主题</param>
    /// <param name="clientId">客户标识/消息分组</param>
    /// <returns>事件总线实例</returns>
    IEventBus<TEvent> CreateEventBus<TEvent>(String topic, String clientId = "");
}

/// <summary>默认事件总线。即时分发消息，不存储</summary>
/// <remarks>
/// 即时分发消息，意味着不在线的订阅者将无法收到消息。
/// 
/// 异常处理策略：
/// 事件总线采用"尽力而为"的分发语义，默认情况下单个订阅者的异常不会影响其他订阅者接收消息。
/// 这种设计保证了订阅者之间的独立性和系统的健壮性。
/// 如果需要严格的事务性保证，可以设置 ThrowOnHandlerError = true，此时任何订阅者异常都会立即中断分发。
/// </remarks>
public class EventBus<TEvent> : DisposeBase, IEventBus, IEventBus<TEvent>, IAsyncEventBus<TEvent>, ILogFeature
{
    #region 属性
    private readonly ConcurrentDictionary<String, IEventHandler<TEvent>> _handlers = [];
    /// <summary>已订阅的事件处理器集合</summary>
    /// <remarks>
    /// Key 为 <c>clientId</c>，Value 为处理器实例。
    /// 返回的是内部字典视图，用于诊断/监控；其中元素的增删可能随订阅变化。
    /// </remarks>
    public IDictionary<String, IEventHandler<TEvent>> Handlers => _handlers;

    /// <summary>处理器异常时是否抛出。默认 false，采用"尽力而为"策略，单个处理器异常不影响其他处理器</summary>
    /// <remarks>
    /// <see langword="false"/>：记录错误日志后继续分发给后续处理器。
    /// <see langword="true"/>：遇到首个处理器异常立即中断并向调用方抛出。
    /// </remarks>
    public Boolean ThrowOnHandlerError { get; set; }

    private readonly Pool<EventContext> _pool = new();
    #endregion

    #region 方法
    /// <summary>发布事件</summary>
    /// <remarks>
    /// 若事件实现了 <see cref="ITraceMessage"/> 且缺少 TraceId，则会自动从当前埋点写入 TraceId。
    /// </remarks>
    /// <param name="event">事件</param>
    /// <param name="context">事件上下文。用于在发布者、订阅者及中间处理器之间传递协调数据，如 Handler、ClientId 等</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功处理该事件的处理器数量</returns>
    public virtual Task<Int32> PublishAsync(TEvent @event, IEventContext? context = null, CancellationToken cancellationToken = default)
    {
        // 待发布消息增加追踪标识
        if (@event is ITraceMessage tm && tm.TraceId.IsNullOrEmpty()) tm.TraceId = DefaultSpan.Current?.ToString();

        return DispatchAsync(@event, context, cancellationToken);
    }

    Task<Int32> IEventBus.PublishAsync(Object @event, IEventContext? context, CancellationToken cancellationToken) => PublishAsync((TEvent)@event, context, cancellationToken);

    /// <summary>分发事件给各个处理器。进程内分发</summary>
    /// <remarks>
    /// 分发采用顺序调用，各处理器之间互不影响；异常策略由 <see cref="ThrowOnHandlerError"/> 控制。
    /// 注意：订阅集合来自 <see cref="ConcurrentDictionary{TKey,TValue}"/> 的枚举快照，分发过程中订阅变化不保证实时可见。
    /// </remarks>
    /// <param name="event">事件</param>
    /// <param name="context">事件上下文。用于在发布者、订阅者及中间处理器之间传递协调数据，如 Handler、ClientId 等。若为 <see langword="null"/>，将从对象池创建并在分发完成后归还</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功处理的处理器数量</returns>
    protected virtual async Task<Int32> DispatchAsync(TEvent @event, IEventContext? context, CancellationToken cancellationToken)
    {
        var rs = 0;

        // 创建上下文，循环调用处理器
        EventContext? ctx = null;
        if (context == null)
        {
            // 从对象池中获取上下文
            ctx = _pool.Get();
            ctx.EventBus = this;
            context = ctx;
        }
        var clientId = (context as EventContext)?.ClientId;
        foreach (var item in _handlers)
        {
            // 不要分发给自己
            if (clientId != null && clientId == item.Key) continue;

            var handler = item.Value;
            try
            {
                await handler.HandleAsync(@event, context, cancellationToken).ConfigureAwait(false);
                rs++;
            }
            catch (Exception ex)
            {
                // 记录异常日志
                Log?.Error("事件处理器 [{0}] 处理事件时发生异常: {1}", item.Key, ex.Message);

                // 根据策略决定是否抛出异常
                if (ThrowOnHandlerError) throw;
            }
        }

        if (ctx != null)
        {
            ctx.Reset();
            _pool.Return(ctx);
        }

        return rs;
    }

    //Task<Int32> IEventDispatcher<TEvent>.DispatchAsync(TEvent @event, CancellationToken cancellationToken) => DispatchAsync(@event, null, cancellationToken);

    /// <summary>订阅消息</summary>
    /// <remarks>
    /// 该实现直接覆盖 <paramref name="clientId"/> 对应的处理器，从而实现幂等订阅。
    /// </remarks>
    /// <param name="handler">处理器</param>
    /// <param name="clientId">客户标识。每个客户只能订阅一次，重复订阅将会挤掉前一次订阅</param>
    /// <returns>是否订阅成功</returns>
    public virtual Boolean Subscribe(IEventHandler<TEvent> handler, String clientId = "")
    {
        _handlers[clientId] = handler;

        return true;
    }

    /// <summary>取消订阅</summary>
    /// <param name="clientId">客户标识。订阅时使用的标识</param>
    /// <returns>是否成功取消订阅</returns>
    public virtual Boolean Unsubscribe(String clientId = "") => _handlers.TryRemove(clientId, out _);

    /// <summary>异步订阅事件</summary>
    /// <remarks>
    /// 适用于订阅动作需要网络往返或其他异步操作的场景。
    /// 实现应保证订阅的幂等性：相同 <paramref name="clientId"/> 重复订阅时覆盖前一次订阅。
    /// </remarks>
    /// <param name="handler">事件处理器</param>
    /// <param name="clientId">客户标识。每个客户只能订阅一次，重复订阅将会挤掉前一次订阅</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否订阅成功</returns>
    public virtual Task<Boolean> SubscribeAsync(IEventHandler<TEvent> handler, String clientId = "", CancellationToken cancellationToken = default) => Task.FromResult(Subscribe(handler, clientId));

    /// <summary>异步取消订阅</summary>
    /// <remarks>
    /// 适用于取消订阅需要网络往返或其他异步操作的场景。
    /// 若未指定 <paramref name="clientId"/>，实现可约定取消默认/匿名订阅。
    /// </remarks>
    /// <param name="clientId">客户标识。订阅时使用的标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功取消订阅</returns>
    public virtual Task<Boolean> UnsubscribeAsync(String clientId = "", CancellationToken cancellationToken = default) => Task.FromResult(Unsubscribe(clientId));
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

/// <summary>事件总线扩展</summary>
public static class EventBusExtensions
{
    /// <summary>订阅事件</summary>
    /// <remarks>适用于同步处理且不依赖上下文的简单订阅。</remarks>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="bus">事件总线</param>
    /// <param name="action">事件处理方法</param>
    /// <param name="clientId">客户标识。每个客户只能订阅一次，重复订阅将会挤掉前一次订阅</param>
    public static void Subscribe<TEvent>(this IEventBus<TEvent> bus, Action<TEvent> action, String clientId = "") => bus.Subscribe(new DelegateEventHandler<TEvent>(action), clientId);

    /// <summary>订阅事件</summary>
    /// <remarks>适用于需要读取/写入上下文数据的订阅。</remarks>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="bus">事件总线</param>
    /// <param name="action">事件处理方法</param>
    /// <param name="clientId">客户标识。每个客户只能订阅一次，重复订阅将会挤掉前一次订阅</param>
    public static void Subscribe<TEvent>(this IEventBus<TEvent> bus, Action<TEvent, IEventContext> action, String clientId = "") => bus.Subscribe(new DelegateEventHandler<TEvent>(action), clientId);

    /// <summary>订阅事件</summary>
    /// <remarks>适用于异步处理且不依赖上下文的订阅。</remarks>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="bus">事件总线</param>
    /// <param name="action">事件处理方法</param>
    /// <param name="clientId">客户标识。每个客户只能订阅一次，重复订阅将会挤掉前一次订阅</param>
    public static void Subscribe<TEvent>(this IEventBus<TEvent> bus, Func<TEvent, Task> action, String clientId = "") => bus.Subscribe(new DelegateEventHandler<TEvent>(action), clientId);

    /// <summary>订阅事件</summary>
    /// <remarks>适用于异步处理，且需要上下文与取消的订阅。</remarks>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="bus">事件总线</param>
    /// <param name="action">事件处理方法</param>
    /// <param name="clientId">客户标识。每个客户只能订阅一次，重复订阅将会挤掉前一次订阅</param>
    public static void Subscribe<TEvent>(this IEventBus<TEvent> bus, Func<TEvent, IEventContext, CancellationToken, Task> action, String clientId = "") => bus.Subscribe(new DelegateEventHandler<TEvent>(action), clientId);

    /// <summary>异步订阅事件</summary>
    /// <remarks>适用于同步处理且不依赖上下文的简单订阅。</remarks>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="bus">异步事件总线</param>
    /// <param name="action">事件处理方法</param>
    /// <param name="clientId">客户标识。每个客户只能订阅一次，重复订阅将会挤掉前一次订阅</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否订阅成功</returns>
    public static Task<Boolean> SubscribeAsync<TEvent>(this IAsyncEventBus<TEvent> bus, Action<TEvent> action, String clientId = "", CancellationToken cancellationToken = default) => bus.SubscribeAsync(new DelegateEventHandler<TEvent>(action), clientId, cancellationToken);

    /// <summary>异步订阅事件</summary>
    /// <remarks>适用于需要读取/写入上下文数据的订阅。</remarks>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="bus">异步事件总线</param>
    /// <param name="action">事件处理方法</param>
    /// <param name="clientId">客户标识。每个客户只能订阅一次，重复订阅将会挤掉前一次订阅</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否订阅成功</returns>
    public static Task<Boolean> SubscribeAsync<TEvent>(this IAsyncEventBus<TEvent> bus, Action<TEvent, IEventContext> action, String clientId = "", CancellationToken cancellationToken = default) => bus.SubscribeAsync(new DelegateEventHandler<TEvent>(action), clientId, cancellationToken);

    /// <summary>异步订阅事件</summary>
    /// <remarks>适用于异步处理且不依赖上下文的订阅。</remarks>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="bus">异步事件总线</param>
    /// <param name="action">事件处理方法</param>
    /// <param name="clientId">客户标识。每个客户只能订阅一次，重复订阅将会挤掉前一次订阅</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否订阅成功</returns>
    public static Task<Boolean> SubscribeAsync<TEvent>(this IAsyncEventBus<TEvent> bus, Func<TEvent, Task> action, String clientId = "", CancellationToken cancellationToken = default) => bus.SubscribeAsync(new DelegateEventHandler<TEvent>(action), clientId, cancellationToken);

    /// <summary>异步订阅事件</summary>
    /// <remarks>适用于异步处理，且需要上下文与取消的订阅。</remarks>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="bus">异步事件总线</param>
    /// <param name="action">事件处理方法</param>
    /// <param name="clientId">客户标识。每个客户只能订阅一次，重复订阅将会挤掉前一次订阅</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否订阅成功</returns>
    public static Task<Boolean> SubscribeAsync<TEvent>(this IAsyncEventBus<TEvent> bus, Func<TEvent, IEventContext, CancellationToken, Task> action, String clientId = "", CancellationToken cancellationToken = default) => bus.SubscribeAsync(new DelegateEventHandler<TEvent>(action), clientId, cancellationToken);
}

/// <summary>事件上下文</summary>
/// <remarks>
/// 用于在事件分发过程中携带上下文数据。
/// 池化使用：总线创建时填充 <see cref="EventBus"/>，分发完成后调用 <see cref="Reset"/> 并归还对象池。
/// </remarks>
public class EventContext : IEventContext, IExtend
{
    /// <summary>事件总线</summary>
    /// <remarks>上下文由总线创建时会填充该属性；放回对象池前会重置。</remarks>
    public IEventBus EventBus { get; set; } = null!;

    /// <summary>客户端标识</summary>
    public String? ClientId { get; set; }

    /// <summary>数据项</summary>
    public IDictionary<String, Object?> Items { get; } = new Dictionary<String, Object?>();

    /// <summary>设置 或 获取 数据项</summary>
    /// <param name="key"></param>
    /// <returns>指定键对应的对象，若不存在则返回 <see langword="null"/></returns>
    public Object? this[String key] { get => Items.TryGetValue(key, out var obj) ? obj : null; set => Items[key] = value; }

    /// <summary>重置上下文，便于放入对象池</summary>
    public void Reset()
    {
        // 清空上下文数据
        EventBus = null!;
        ClientId = null;
        Items.Clear();
    }
}

/// <summary>委托事件处理器。将 Action/Func 委托封装为 <see cref="IEventHandler{TEvent}"/></summary>
/// <typeparam name="TEvent">事件类型</typeparam>
/// <remarks>
/// 实例化委托事件处理器
/// </remarks>
/// <param name="method">委托方法，不可为 null</param>
/// <exception cref="ArgumentNullException">当 <paramref name="method"/> 为 null 时抛出</exception>
public class DelegateEventHandler<TEvent>(Delegate method) : IEventHandler<TEvent>
{
    private readonly Delegate _method = method ?? throw new ArgumentNullException(nameof(method));

    /// <summary>处理事件</summary>
    /// <remarks>
    /// 支持以下委托形态：
    /// <list type="bullet">
    /// <item><description><see cref="Action{T}"/>（TEvent）</description></item>
    /// <item><description><c>Action&lt;TEvent, IEventContext?&gt;</c></description></item>
    /// <item><description><see cref="Func{T, TResult}"/>（TEvent -&gt; Task）</description></item>
    /// <item><description><c>Func&lt;TEvent, IEventContext?, CancellationToken, Task&gt;</c></description></item>
    /// </list>
    /// </remarks>
    /// <param name="event">事件</param>
    /// <param name="context">事件上下文。用于在发布者、订阅者及中间处理器之间传递协调数据，如 Handler、ClientId 等</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示处理过程的任务</returns>
    /// <exception cref="NotSupportedException">当传入委托类型不受支持时抛出</exception>
    public Task HandleAsync(TEvent @event, IEventContext? context, CancellationToken cancellationToken = default)
    {
        if (_method is Func<TEvent, Task> func) return func(@event);
        if (_method is Func<TEvent, IEventContext?, CancellationToken, Task> func2) return func2(@event, context, cancellationToken);

        if (_method is Action<TEvent> act)
            act(@event);
        else if (_method is Action<TEvent, IEventContext?> act2)
            act2(@event, context);
        else
            throw new NotSupportedException($"不支持的委托类型: {_method.GetType().FullName}");

        return TaskEx.CompletedTask;
    }
}