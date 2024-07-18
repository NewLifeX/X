namespace System.Threading;

#if NETFRAMEWORK || NETSTANDARD || NETCOREAPP3_1 || NET5_0 || NET6_0 || NET7_0
/// <summary>表示可以更改其到期时间和时间段的计时器。</summary>
public interface ITimer : IDisposable
{
    /// <summary>更改计时器的启动时间和方法调用之间的时间间隔，使用 TimeSpan 值度量时间间隔。</summary>
    /// <param name="dueTime">一个 TimeSpan，表示在调用构造 ITimer 时指定的回调方法之前的延迟时间量。 指定 InfiniteTimeSpan 可防止重新启动计时器。 指定 Zero 可立即重新启动计时器。</param>
    /// <param name="period">构造 Timer 时指定的回调方法调用之间的时间间隔。 指定 InfiniteTimeSpan 可以禁用定期终止。</param>
    /// <returns></returns>
    Boolean Change(TimeSpan dueTime, TimeSpan period);
}
#endif