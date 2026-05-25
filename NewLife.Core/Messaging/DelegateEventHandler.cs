namespace NewLife.Messaging;

/// <summary>委托事件处理器。将 Action/Func 委托封装为 <see cref="IEventHandler{TEvent}"/></summary>
/// <typeparam name="TEvent">事件类型</typeparam>
/// <remarks>
/// 支持四种委托形态：
/// <list type="bullet">
/// <item><description><see cref="Action{T}"/>（TEvent）</description></item>
/// <item><description><c>Action&lt;TEvent, IEventContext?&gt;</c></description></item>
/// <item><description><see cref="Func{T, TResult}"/>（TEvent -&gt; Task）</description></item>
/// <item><description><c>Func&lt;TEvent, IEventContext?, CancellationToken, Task&gt;</c></description></item>
/// </list>
/// </remarks>
/// <param name="method">委托方法，不可为 null</param>
/// <exception cref="ArgumentNullException">当 <paramref name="method"/> 为 null 时抛出</exception>
public class DelegateEventHandler<TEvent>(Delegate method) : IEventHandler<TEvent>
{
    private readonly Delegate _method = method ?? throw new ArgumentNullException(nameof(method));

    /// <summary>处理事件</summary>
    /// <param name="event">事件</param>
    /// <param name="context">事件上下文</param>
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
