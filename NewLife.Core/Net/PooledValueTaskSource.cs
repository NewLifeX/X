#if NET5_0_OR_GREATER
using System.Threading.Tasks.Sources;
using NewLife.Collections;

namespace NewLife.Net;

/// <summary>池化的异步完成源。基于 ManualResetValueTaskSourceCore 实现，避免每次 SendMessageAsync 分配 TaskCompletionSource</summary>
/// <remarks>
/// 生命周期：Rent → 设置到匹配队列 → SetResult/SetCanceled → 消费者 await 完成 → GetResult 内自动归还到池。
/// 线程安全：通过 CAS 保证 SetResult/SetCanceled 只成功一次。
/// </remarks>
sealed class PooledValueTaskSource : IValueTaskSource<Object>
{
    private ManualResetValueTaskSourceCore<Object> _core;
    private volatile Int32 _completed;

    private static readonly Pool<PooledValueTaskSource> _pool = new();

    /// <summary>从池中借出</summary>
    public static PooledValueTaskSource Rent() => _pool.Get();

    /// <summary>当前版本号</summary>
    public Int16 Version => _core.Version;

    /// <summary>是否已完成</summary>
    public Boolean IsCompleted => _core.GetStatus(_core.Version) != ValueTaskSourceStatus.Pending;

    /// <summary>获取可等待的 ValueTask</summary>
    public ValueTask<Object> ValueTask => new(this, _core.Version);

    /// <summary>尝试设置成功结果（仅首次调用生效）</summary>
    /// <param name="result">结果值</param>
    /// <returns>是否成功设置</returns>
    public Boolean TrySetResult(Object result)
    {
        if (Interlocked.CompareExchange(ref _completed, 1, 0) != 0) return false;
        _core.SetResult(result);
        return true;
    }

    /// <summary>尝试设置取消（仅首次调用生效）</summary>
    /// <returns>是否成功设置</returns>
    public Boolean TrySetCanceled()
    {
        if (Interlocked.CompareExchange(ref _completed, 1, 0) != 0) return false;
        _core.SetException(new TaskCanceledException());
        return true;
    }

    /// <summary>尝试设置异常（仅首次调用生效）</summary>
    /// <param name="exception">异常</param>
    /// <returns>是否成功设置</returns>
    public Boolean TrySetException(Exception exception)
    {
        if (Interlocked.CompareExchange(ref _completed, 1, 0) != 0) return false;
        _core.SetException(exception);
        return true;
    }

    Object IValueTaskSource<Object>.GetResult(Int16 token)
    {
        try
        {
            return _core.GetResult(token);
        }
        finally
        {
            // 消费者读取结果后，自动归还到池
            _completed = 0;
            _core.Reset();
            _pool.Return(this);
        }
    }

    ValueTaskSourceStatus IValueTaskSource<Object>.GetStatus(Int16 token) => _core.GetStatus(token);

    void IValueTaskSource<Object>.OnCompleted(Action<Object?> continuation, Object? state, Int16 token, ValueTaskSourceOnCompletedFlags flags) =>
        _core.OnCompleted(continuation, state, token, flags);
}
#endif
