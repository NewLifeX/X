namespace NewLife.Messaging;

/// <summary>事件总线扩展。提供同步 Subscribe/Unsubscribe 与委托订阅、ReceiveAsync 等便利方法</summary>
public static class EventBusExtensions
{
    #region 同步订阅（异步签名的同步包装）
    /// <summary>同步订阅事件（异步方法的同步包装）</summary>
    /// <remarks>等价于 <c>bus.SubscribeAsync(handler, clientId).GetAwaiter().GetResult()</c>。
    /// 仅适用于纯内存总线；远程或异步总线请直接使用 <see cref="IEventBus{TEvent}.SubscribeAsync"/>。</remarks>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="bus">事件总线</param>
    /// <param name="handler">事件处理器</param>
    /// <param name="clientId">客户标识</param>
    /// <returns>是否订阅成功</returns>
    public static Boolean Subscribe<TEvent>(this IEventBus<TEvent> bus, IEventHandler<TEvent> handler, String clientId = "") => bus.SubscribeAsync(handler, clientId).ConfigureAwait(false).GetAwaiter().GetResult();

    /// <summary>同步取消订阅（异步方法的同步包装）</summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="bus">事件总线</param>
    /// <param name="clientId">客户标识</param>
    /// <returns>是否成功取消订阅</returns>
    public static Boolean Unsubscribe<TEvent>(this IEventBus<TEvent> bus, String clientId = "") => bus.UnsubscribeAsync(clientId).ConfigureAwait(false).GetAwaiter().GetResult();
    #endregion

    #region 委托订阅
    /// <summary>订阅事件（同步处理，不依赖上下文）</summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="bus">事件总线</param>
    /// <param name="action">事件处理方法</param>
    /// <param name="clientId">客户标识</param>
    public static void Subscribe<TEvent>(this IEventBus<TEvent> bus, Action<TEvent> action, String clientId = "") => bus.Subscribe(new DelegateEventHandler<TEvent>(action), clientId);

    /// <summary>订阅事件（同步处理，依赖上下文）</summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="bus">事件总线</param>
    /// <param name="action">事件处理方法</param>
    /// <param name="clientId">客户标识</param>
    public static void Subscribe<TEvent>(this IEventBus<TEvent> bus, Action<TEvent, IEventContext> action, String clientId = "") => bus.Subscribe(new DelegateEventHandler<TEvent>(action), clientId);

    /// <summary>订阅事件（异步处理，不依赖上下文）</summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="bus">事件总线</param>
    /// <param name="action">事件处理方法</param>
    /// <param name="clientId">客户标识</param>
    public static void Subscribe<TEvent>(this IEventBus<TEvent> bus, Func<TEvent, Task> action, String clientId = "") => bus.Subscribe(new DelegateEventHandler<TEvent>(action), clientId);

    /// <summary>订阅事件（异步处理，依赖上下文与取消）</summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="bus">事件总线</param>
    /// <param name="action">事件处理方法</param>
    /// <param name="clientId">客户标识</param>
    public static void Subscribe<TEvent>(this IEventBus<TEvent> bus, Func<TEvent, IEventContext, CancellationToken, Task> action, String clientId = "") => bus.Subscribe(new DelegateEventHandler<TEvent>(action), clientId);

    /// <summary>异步订阅事件（同步处理，不依赖上下文）</summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="bus">事件总线</param>
    /// <param name="action">事件处理方法</param>
    /// <param name="clientId">客户标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否订阅成功</returns>
    public static Task<Boolean> SubscribeAsync<TEvent>(this IEventBus<TEvent> bus, Action<TEvent> action, String clientId = "", CancellationToken cancellationToken = default) => bus.SubscribeAsync(new DelegateEventHandler<TEvent>(action), clientId, cancellationToken);

    /// <summary>异步订阅事件（同步处理，依赖上下文）</summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="bus">事件总线</param>
    /// <param name="action">事件处理方法</param>
    /// <param name="clientId">客户标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否订阅成功</returns>
    public static Task<Boolean> SubscribeAsync<TEvent>(this IEventBus<TEvent> bus, Action<TEvent, IEventContext> action, String clientId = "", CancellationToken cancellationToken = default) => bus.SubscribeAsync(new DelegateEventHandler<TEvent>(action), clientId, cancellationToken);

    /// <summary>异步订阅事件（异步处理，不依赖上下文）</summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="bus">事件总线</param>
    /// <param name="action">事件处理方法</param>
    /// <param name="clientId">客户标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否订阅成功</returns>
    public static Task<Boolean> SubscribeAsync<TEvent>(this IEventBus<TEvent> bus, Func<TEvent, Task> action, String clientId = "", CancellationToken cancellationToken = default) => bus.SubscribeAsync(new DelegateEventHandler<TEvent>(action), clientId, cancellationToken);

    /// <summary>异步订阅事件（异步处理，依赖上下文与取消）</summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="bus">事件总线</param>
    /// <param name="action">事件处理方法</param>
    /// <param name="clientId">客户标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否订阅成功</returns>
    public static Task<Boolean> SubscribeAsync<TEvent>(this IEventBus<TEvent> bus, Func<TEvent, IEventContext, CancellationToken, Task> action, String clientId = "", CancellationToken cancellationToken = default) => bus.SubscribeAsync(new DelegateEventHandler<TEvent>(action), clientId, cancellationToken);
    #endregion

    #region 等待接收
    private static Int32 _receiveCounter;

    /// <summary>异步阻塞等待一条事件消息（一次性订阅）</summary>
    /// <remarks>
    /// 内部通过 <see cref="TaskCompletionSource{TResult}"/> 与唯一 clientId 实现：
    /// 收到首条消息后立即完成并自动取消订阅。
    /// </remarks>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="bus">事件总线</param>
    /// <param name="cancellationToken">取消令牌。可通过 <see cref="CancellationTokenSource"/> 实现超时</param>
    /// <returns>等待到的第一条事件</returns>
    public static Task<TEvent> ReceiveAsync<TEvent>(this IEventBus<TEvent> bus, CancellationToken cancellationToken = default)
    {
#if NET45
        var tcs = new TaskCompletionSource<TEvent>();
#else
        var tcs = new TaskCompletionSource<TEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
        var clientId = $"__recv_{Interlocked.Increment(ref _receiveCounter)}";

        bus.Subscribe((TEvent evt) =>
        {
            // 先取消订阅，再触发结果，确保 finally 清理时 Handlers.Count 已归零
            bus.Unsubscribe(clientId);
            tcs.TrySetResult(evt);
        }, clientId);

        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() =>
            {
                bus.Unsubscribe(clientId);
#if NET45
                tcs.TrySetCanceled();
#else
                tcs.TrySetCanceled(cancellationToken);
#endif
            });
        }

        return tcs.Task;
    }

    /// <summary>异步阻塞等待一条事件消息，超时后抛出 <see cref="OperationCanceledException"/></summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="bus">事件总线</param>
    /// <param name="timeout">等待超时时间</param>
    /// <returns>等待到的第一条事件</returns>
    public static async Task<TEvent> ReceiveAsync<TEvent>(this IEventBus<TEvent> bus, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        return await ReceiveAsync(bus, cts.Token).ConfigureAwait(false);
    }
    #endregion
}
