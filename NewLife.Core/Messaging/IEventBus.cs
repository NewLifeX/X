#if !NET45
using TaskEx = System.Threading.Tasks.Task;
#endif

using System.Collections.Concurrent;
using NewLife.Collections;
using NewLife.Data;

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
/// 这里采取第一种设计，不同业务领域可以实例化自己的时间总线，互不干扰。
/// </remarks>
/// <typeparam name="TEvent"></typeparam>
public interface IEventBus<TEvent>
{
    /// <summary>发布消息</summary>
    /// <param name="event">事件</param>
    /// <param name="context">上下文</param>
    Task<Int32> PublishAsync(TEvent @event, IEventContext<TEvent>? context = null);

    /// <summary>订阅消息</summary>
    /// <param name="handler">处理器</param>
    /// <param name="clientId">客户标识。每个客户只能订阅一次，重复订阅将会挤掉前一次订阅</param>
    Boolean Subscribe(IEventHandler<TEvent> handler, String clientId = "");

    /// <summary>取消订阅</summary>
    /// <param name="clientId">客户标识。订阅时使用的标识</param>
    Boolean Unsubscribe(String clientId = "");
}

/// <summary>事件处理器</summary>
/// <typeparam name="TEvent"></typeparam>
public interface IEventHandler<TEvent>
{
    /// <summary>处理事件</summary>
    /// <param name="event"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    Task HandleAsync(TEvent @event, IEventContext<TEvent> context);
}

/// <summary>事件总线</summary>
public class EventBus<TEvent> : IEventBus<TEvent>
{
    private ConcurrentDictionary<String, IEventHandler<TEvent>> _handlers = [];

    /// <summary>发布消息</summary>
    /// <param name="event">事件</param>
    /// <param name="context">上下文</param>
    public virtual async Task<Int32> PublishAsync(TEvent @event, IEventContext<TEvent>? context = null)
    {
        var rs = 0;

        // 创建上下文，循环调用处理器
        context ??= new EventContext<TEvent>(this);
        foreach (var item in _handlers)
        {
            var handler = item.Value;
            await handler.HandleAsync(@event, context).ConfigureAwait(false);
            rs++;
        }

        return rs;
    }

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
    /// <param name="bus"></param>
    /// <param name="action"></param>
    public static void Subscribe<TEvent>(this IEventBus<TEvent> bus, Action<TEvent> action) => bus.Subscribe(new DelegateEventHandler<TEvent>(action));

    /// <summary>订阅事件</summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="bus"></param>
    /// <param name="action"></param>
    public static void Subscribe<TEvent>(this IEventBus<TEvent> bus, Action<TEvent, IEventContext<TEvent>> action) => bus.Subscribe(new DelegateEventHandler<TEvent>(action));

    /// <summary>订阅事件</summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="bus"></param>
    /// <param name="action"></param>
    public static void Subscribe<TEvent>(this IEventBus<TEvent> bus, Func<TEvent, Task> action) => bus.Subscribe(new DelegateEventHandler<TEvent>(action));

    /// <summary>订阅事件</summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="bus"></param>
    /// <param name="action"></param>
    public static void Subscribe<TEvent>(this IEventBus<TEvent> bus, Func<TEvent, IEventContext<TEvent>, Task> action) => bus.Subscribe(new DelegateEventHandler<TEvent>(action));
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
public class EventContext<TEvent>(IEventBus<TEvent> bus) : IEventContext<TEvent>, IExtend
{
    /// <summary>事件总线</summary>
    public IEventBus<TEvent> EventBus { get; } = bus;

    /// <summary>数据项</summary>
    public IDictionary<String, Object?> Items { get; } = new NullableDictionary<String, Object?>();

    /// <summary>设置 或 获取 数据项</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Object? this[String key] { get => Items.TryGetValue(key, out var obj) ? obj : null; set => Items[key] = value; }
}

/// <summary>Action事件处理器</summary>
/// <typeparam name="TEvent"></typeparam>
/// <param name="method"></param>
public class DelegateEventHandler<TEvent>(Delegate method) : IEventHandler<TEvent>
{
    /// <summary>处理事件</summary>
    /// <param name="event"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public Task HandleAsync(TEvent @event, IEventContext<TEvent> context)
    {
        if (method is Func<TEvent, Task> func) return func(@event);
        if (method is Func<TEvent, IEventContext<TEvent>, Task> func2) return func2(@event, context);

        if (method is Action<TEvent> act)
            act(@event);
        else if (method is Action<TEvent, IEventContext<TEvent>> act2)
            act2(@event, context);
        else
            throw new NotSupportedException();

        return TaskEx.CompletedTask;
    }
}