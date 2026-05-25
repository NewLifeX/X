using System.Collections.Concurrent;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;

namespace NewLife.Messaging;

#region 事件上下文实现

/// <summary>事件上下文。携带主题、客户端标识与扩展数据项</summary>
/// <remarks>
/// <para>支持对象池：总线创建时填充 <see cref="EventBus{TEvent}"/>，分发完成后调用 <see cref="Reset"/> 并归还对象池。</para>
/// <para>处理器不得在异步流外保存该对象的引用，需要保存时应提前复制所需字段。</para>
/// </remarks>
public class EventContext : IEventContext, IExtend
{
    /// <summary>事件总线</summary>
    /// <remarks>由总线创建时填充；放回对象池前会重置为 <see langword="null"/>。</remarks>
    public IEventBus? EventBus { get; set; }

    /// <summary>消息主题</summary>
    public String? Topic { get; set; }

    /// <summary>客户端标识。用于事件总线在分发时排除发送方自身</summary>
    public String? ClientId { get; set; }

    /// <summary>数据项</summary>
    public IDictionary<String, Object?> Items { get; } = new Dictionary<String, Object?>();

    /// <summary>设置 或 获取 数据项</summary>
    /// <param name="key">键</param>
    /// <returns>指定键对应的对象，若不存在则返回 <see langword="null"/></returns>
    public Object? this[String key] { get => Items.TryGetValue(key, out var obj) ? obj : null; set => Items[key] = value; }

    /// <summary>重置上下文，便于放入对象池</summary>
    public void Reset()
    {
        EventBus = null;
        Topic = null;
        ClientId = null;
        Items.Clear();
    }
}

#endregion

/// <summary>默认事件总线。即时分发消息，不存储</summary>
/// <remarks>
/// <para>即时分发：不在线的订阅者将无法收到消息。</para>
/// <para>异常处理策略：默认"尽力而为"，单个订阅者的异常不会影响其他订阅者接收消息。
/// 若需严格的事务性保证，可设置 <see cref="ThrowOnHandlerError"/> = true，任何订阅者异常都会立即中断分发。</para>
/// <para>线程安全：订阅集合基于 <see cref="ConcurrentDictionary{TKey,TValue}"/>；分发期间的订阅变化不保证本轮可见。</para>
/// </remarks>
public class EventBus<TEvent> : DisposeBase, IEventBus, IEventBus<TEvent>, IAsyncEventBus<TEvent>, ILogFeature
{
    #region 属性
    private readonly ConcurrentDictionary<String, IEventHandler<TEvent>> _handlers = [];
    /// <summary>已订阅的事件处理器集合</summary>
    /// <remarks>Key 为 <c>clientId</c>，Value 为处理器实例。返回的是内部字典视图，用于诊断/监控。</remarks>
    public IDictionary<String, IEventHandler<TEvent>> Handlers => _handlers;

    /// <summary>处理器异常时是否抛出。默认 false，采用"尽力而为"策略</summary>
    public Boolean ThrowOnHandlerError { get; set; }

    private readonly Pool<EventContext> _pool = new();
    #endregion

    #region 发布
    /// <summary>发布事件</summary>
    /// <remarks>
    /// <para>默认实现直接调用 <see cref="DispatchAsync"/> 进行本地分发。</para>
    /// <para>派生总线（如基于消息队列的实现）通常会在 <see cref="PublishAsync"/> 中改写为"先入队、由消费循环再调用 <see cref="DispatchAsync"/>"，
    /// 以确保从队列收到的消息只走本地分发、不再循环入队。</para>
    /// </remarks>
    /// <param name="event">事件</param>
    /// <param name="context">事件上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功处理该事件的处理器数量</returns>
    public virtual Task<Int32> PublishAsync(TEvent @event, IEventContext? context = null, CancellationToken cancellationToken = default) => DispatchAsync(@event, context, cancellationToken);

    /// <summary>本地分发事件到所有订阅者</summary>
    /// <remarks>若事件实现了 <see cref="ITraceMessage"/> 且缺少 TraceId，将自动从当前埋点写入 TraceId。</remarks>
    /// <param name="event">事件</param>
    /// <param name="context">事件上下文。若为 <see langword="null"/>，将从对象池创建并在分发完成后归还</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功处理该事件的处理器数量</returns>
    protected virtual async Task<Int32> DispatchAsync(TEvent @event, IEventContext? context = null, CancellationToken cancellationToken = default)
    {
        // 待发布消息增加追踪标识
        if (@event is ITraceMessage tm && tm.TraceId.IsNullOrEmpty()) tm.TraceId = DefaultSpan.Current?.ToString();

        var rs = 0;

        // 创建上下文，循环调用处理器
        EventContext? ctx = null;
        if (context == null)
        {
            ctx = _pool.Get();
            ctx.EventBus = this;
            context = ctx;
        }
        var clientId = (context as EventContext)?.ClientId;
        foreach (var item in _handlers)
        {
            // 不要分发给自己
            if (clientId != null && clientId == item.Key) continue;

            try
            {
                await item.Value.HandleAsync(@event, context, cancellationToken).ConfigureAwait(false);
                rs++;
            }
            catch (Exception ex)
            {
                Log?.Error("事件处理器 [{0}] 处理事件时发生异常: {1}", item.Key, ex.Message);
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

    Task<Int32> IEventBus.PublishAsync(Object @event, IEventContext? context, CancellationToken cancellationToken) => PublishAsync((TEvent)@event, context, cancellationToken);
    #endregion

    #region 订阅
    /// <summary>异步订阅事件</summary>
    /// <remarks>幂等订阅：相同 <paramref name="clientId"/> 重复订阅时覆盖前一次订阅。</remarks>
    /// <param name="handler">事件处理器</param>
    /// <param name="clientId">客户标识。每个客户只能订阅一次，重复订阅将会挤掉前一次订阅</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否订阅成功</returns>
    public virtual Task<Boolean> SubscribeAsync(IEventHandler<TEvent> handler, String clientId = "", CancellationToken cancellationToken = default)
    {
        _handlers[clientId] = handler;
        return TaskEx.FromResult(true);
    }

    /// <summary>异步取消订阅</summary>
    /// <param name="clientId">客户标识。订阅时使用的标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功取消订阅</returns>
    public virtual Task<Boolean> UnsubscribeAsync(String clientId = "", CancellationToken cancellationToken = default) => TaskEx.FromResult(_handlers.TryRemove(clientId, out _));
    #endregion

    #region 日志
    /// <summary>日志</summary>
    public ILog Log { get; set; } = Logger.Null;

    /// <summary>写日志。同步到当前埋点</summary>
    /// <param name="format">格式串</param>
    /// <param name="args">参数</param>
    public void WriteLog(String format, params Object[] args)
    {
        var span = DefaultSpan.Current;
        span?.AppendTag(String.Format(format, args));

        Log?.Info(format, args);
    }
    #endregion
}
