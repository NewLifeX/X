using System.Collections.Concurrent;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
#if !NET45
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.Messaging;

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
/// </remarks>
/// <typeparam name="TEvent"></typeparam>
public interface IEventBus<TEvent>
{
    /// <summary>发布事件</summary>
    /// <param name="event">事件</param>
    /// <param name="context">上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<Int32> PublishAsync(TEvent @event, IEventContext<TEvent>? context = null, CancellationToken cancellationToken = default);

    /// <summary>订阅事件</summary>
    /// <param name="handler">事件处理器</param>
    /// <param name="clientId">客户标识。每个客户只能订阅一次，重复订阅将会挤掉前一次订阅</param>
    Boolean Subscribe(IEventHandler<TEvent> handler, String clientId = "");

    /// <summary>取消订阅</summary>
    /// <param name="clientId">客户标识。订阅时使用的标识</param>
    Boolean Unsubscribe(String clientId = "");
}

/// <summary>事件分发器（抽象分发接口）。供路由器按主题转发到具体总线</summary>
/// <typeparam name="TEvent"></typeparam>
public interface IEventDispatcher<TEvent>
{
    /// <summary>分发事件给各个处理器。进程内分发</summary>
    /// <param name="event">事件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功处理的处理器数量</returns>
    Task<Int32> DispatchAsync(TEvent @event, CancellationToken cancellationToken);
}

/// <summary>事件处理器</summary>
/// <typeparam name="TEvent"></typeparam>
public interface IEventHandler<TEvent>
{
    /// <summary>处理事件</summary>
    /// <param name="event">事件</param>
    /// <param name="context">上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task HandleAsync(TEvent @event, IEventContext<TEvent>? context, CancellationToken cancellationToken);
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
public class EventBus<TEvent> : DisposeBase, IEventBus<TEvent>, IEventDispatcher<TEvent>
{
    private readonly ConcurrentDictionary<String, IEventHandler<TEvent>> _handlers = [];
    /// <summary>已订阅的事件处理器集合</summary>
    public IDictionary<String, IEventHandler<TEvent>> Handlers => _handlers;

    private readonly Pool<EventContext<TEvent>> _pool = new();

    /// <summary>处理器异常时是否抛出。默认 false，采用"尽力而为"策略，单个处理器异常不影响其他处理器</summary>
    public Boolean ThrowOnHandlerError { get; set; }

    /// <summary>日志</summary>
    public ILog? Log { get; set; }

    /// <summary>发布事件</summary>
    /// <param name="event">事件</param>
    /// <param name="context">上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    public virtual Task<Int32> PublishAsync(TEvent @event, IEventContext<TEvent>? context = null, CancellationToken cancellationToken = default)
    {
        // 待发布消息增加追踪标识
        if (@event is ITraceMessage tm && tm.TraceId.IsNullOrEmpty()) tm.TraceId = DefaultSpan.Current?.ToString();

        return DispatchAsync(@event, context, cancellationToken);
    }

    /// <summary>分发事件给各个处理器。进程内分发</summary>
    /// <param name="event"></param>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected virtual async Task<Int32> DispatchAsync(TEvent @event, IEventContext<TEvent>? context, CancellationToken cancellationToken)
    {
        var rs = 0;

        // 创建上下文，循环调用处理器
        EventContext<TEvent>? ctx = null;
        if (context == null)
        {
            // 从对象池中获取上下文
            ctx = _pool.Get();
            ctx.EventBus = this;
            context = ctx;
        }
        foreach (var item in _handlers)
        {
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

    Task<Int32> IEventDispatcher<TEvent>.DispatchAsync(TEvent @event, CancellationToken cancellationToken) => DispatchAsync(@event, null, cancellationToken);

    /// <summary>订阅消息</summary>
    /// <param name="handler">处理器</param>
    /// <param name="clientId">客户标识。每个客户只能订阅一次，重复订阅将会挤掉前一次订阅</param>
    public virtual Boolean Subscribe(IEventHandler<TEvent> handler, String clientId = "")
    {
        _handlers[clientId] = handler;

        return true;
    }

    /// <summary>取消订阅</summary>
    /// <param name="clientId">客户标识。订阅时使用的标识</param>
    public virtual Boolean Unsubscribe(String clientId = "") => _handlers.TryRemove(clientId, out _);
}

/// <summary>事件总线扩展</summary>
public static class EventBusExtensions
{
    /// <summary>订阅事件</summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="bus">事件总线</param>
    /// <param name="action">事件处理方法</param>
    /// <param name="clientId">客户标识。每个客户只能订阅一次，重复订阅将会挤掉前一次订阅</param>
    public static void Subscribe<TEvent>(this IEventBus<TEvent> bus, Action<TEvent> action, String clientId = "") => bus.Subscribe(new DelegateEventHandler<TEvent>(action), clientId);

    /// <summary>订阅事件</summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="bus">事件总线</param>
    /// <param name="action">事件处理方法</param>
    /// <param name="clientId">客户标识。每个客户只能订阅一次，重复订阅将会挤掉前一次订阅</param>
    public static void Subscribe<TEvent>(this IEventBus<TEvent> bus, Action<TEvent, IEventContext<TEvent>> action, String clientId = "") => bus.Subscribe(new DelegateEventHandler<TEvent>(action), clientId);

    /// <summary>订阅事件</summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="bus">事件总线</param>
    /// <param name="action">事件处理方法</param>
    /// <param name="clientId">客户标识。每个客户只能订阅一次，重复订阅将会挤掉前一次订阅</param>
    public static void Subscribe<TEvent>(this IEventBus<TEvent> bus, Func<TEvent, Task> action, String clientId = "") => bus.Subscribe(new DelegateEventHandler<TEvent>(action), clientId);

    /// <summary>订阅事件</summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="bus">事件总线</param>
    /// <param name="action">事件处理方法</param>
    /// <param name="clientId">客户标识。每个客户只能订阅一次，重复订阅将会挤掉前一次订阅</param>
    public static void Subscribe<TEvent>(this IEventBus<TEvent> bus, Func<TEvent, IEventContext<TEvent>, CancellationToken, Task> action, String clientId = "") => bus.Subscribe(new DelegateEventHandler<TEvent>(action), clientId);
}

/// <summary>事件上下文接口</summary>
/// <typeparam name="TEvent"></typeparam>
public interface IEventContext<TEvent>
{
    /// <summary>事件总线</summary>
    IEventBus<TEvent> EventBus { get; }
}

/// <summary>事件上下文</summary>
/// <typeparam name="TEvent"></typeparam>
public class EventContext<TEvent> : IEventContext<TEvent>, IExtend
{
    /// <summary>事件总线</summary>
    public IEventBus<TEvent> EventBus { get; set; } = null!;

    /// <summary>数据项</summary>
    public IDictionary<String, Object?> Items { get; } = new NullableDictionary<String, Object?>();

    /// <summary>设置 或 获取 数据项</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Object? this[String key] { get => Items.TryGetValue(key, out var obj) ? obj : null; set => Items[key] = value; }

    /// <summary>重置上下文，便于放入对象池</summary>
    public void Reset()
    {
        // 清空上下文数据
        EventBus = null!;
        Items.Clear();
    }
}

/// <summary>Action事件处理器</summary>
/// <typeparam name="TEvent"></typeparam>
/// <param name="method"></param>
public class DelegateEventHandler<TEvent>(Delegate method) : IEventHandler<TEvent>
{
    /// <summary>处理事件</summary>
    /// <param name="event">事件</param>
    /// <param name="context">上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public Task HandleAsync(TEvent @event, IEventContext<TEvent>? context, CancellationToken cancellationToken = default)
    {
        if (method is Func<TEvent, Task> func) return func(@event);
        if (method is Func<TEvent, IEventContext<TEvent>?, CancellationToken, Task> func2) return func2(@event, context, cancellationToken);

        if (method is Action<TEvent> act)
            act(@event);
        else if (method is Action<TEvent, IEventContext<TEvent>?> act2)
            act2(@event, context);
        else
            throw new NotSupportedException();

        return TaskEx.CompletedTask;
    }
}