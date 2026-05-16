namespace System.Threading.Tasks;

/// <summary>Task 跨版本兼容帮助类。在 net45/netstandard2.0 等低版本框架中补充高版本 API，下游类库可直接使用而无需条件编译。</summary>
/// <remarks>
/// 命名空间与 Task 相同（System.Threading.Tasks），已有 using 语句的文件无需额外引用。
/// 推荐用法：以 TaskEx.CompletedTask / TaskEx.FromResult() 替代带 #if 的条件编译块。
/// </remarks>
public static class TaskEx
{
#if NET45
    private static readonly Task s_preCompletedTask = Task.FromResult(false);
    /// <summary>已完成的任务。net45 使用缓存的 Task{bool} 模拟，其他框架直接委托 Task.CompletedTask。</summary>
    public static Task CompletedTask => s_preCompletedTask;
#else
    /// <summary>已完成的任务。net45 使用缓存的 Task{bool} 模拟，其他框架直接委托 Task.CompletedTask。</summary>
    public static Task CompletedTask => Task.CompletedTask;
#endif

    /// <summary>创建一个携带指定结果的已完成任务。</summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="result">任务结果值</param>
    /// <returns>已完成的 Task{TResult}</returns>
    public static Task<TResult> FromResult<TResult>(TResult result) => Task.FromResult(result);

#if NET45
    /// <summary>创建一个已因指定异常而失败的任务。net45 使用 TaskCompletionSource 模拟。</summary>
    /// <param name="exception">导致任务失败的异常</param>
    /// <returns>已失败的 Task</returns>
    public static Task FromException(Exception exception)
    {
        var tcs = new TaskCompletionSource<Boolean>();
        tcs.SetException(exception);
        return tcs.Task;
    }

    /// <summary>创建一个已因指定异常而失败的任务。net45 使用 TaskCompletionSource 模拟。</summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="exception">导致任务失败的异常</param>
    /// <returns>已失败的 Task{TResult}</returns>
    public static Task<TResult> FromException<TResult>(Exception exception)
    {
        var tcs = new TaskCompletionSource<TResult>();
        tcs.SetException(exception);
        return tcs.Task;
    }

    /// <summary>创建一个已因取消而终止的任务。net45 使用 TaskCompletionSource 模拟。</summary>
    /// <param name="cancellationToken">导致取消的令牌</param>
    /// <returns>已取消的 Task</returns>
    public static Task FromCanceled(CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<Boolean>();
        tcs.SetCanceled();
        return tcs.Task;
    }

    /// <summary>创建一个已因取消而终止的任务。net45 使用 TaskCompletionSource 模拟。</summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="cancellationToken">导致取消的令牌</param>
    /// <returns>已取消的 Task{TResult}</returns>
    public static Task<TResult> FromCanceled<TResult>(CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<TResult>();
        tcs.SetCanceled();
        return tcs.Task;
    }
#else
    /// <summary>创建一个已因指定异常而失败的任务。</summary>
    /// <param name="exception">导致任务失败的异常</param>
    /// <returns>已失败的 Task</returns>
    public static Task FromException(Exception exception) => Task.FromException(exception);

    /// <summary>创建一个已因指定异常而失败的任务。</summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="exception">导致任务失败的异常</param>
    /// <returns>已失败的 Task{TResult}</returns>
    public static Task<TResult> FromException<TResult>(Exception exception) => Task.FromException<TResult>(exception);

    /// <summary>创建一个已因取消而终止的任务。</summary>
    /// <param name="cancellationToken">导致取消的令牌</param>
    /// <returns>已取消的 Task</returns>
    public static Task FromCanceled(CancellationToken cancellationToken) => Task.FromCanceled(cancellationToken);

    /// <summary>创建一个已因取消而终止的任务。</summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="cancellationToken">导致取消的令牌</param>
    /// <returns>已取消的 Task{TResult}</returns>
    public static Task<TResult> FromCanceled<TResult>(CancellationToken cancellationToken) => Task.FromCanceled<TResult>(cancellationToken);
#endif
}
