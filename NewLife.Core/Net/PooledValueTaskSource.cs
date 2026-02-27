#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
using System.Threading.Tasks.Sources;
using NewLife.Collections;
using NewLife.Log;

namespace NewLife.Net;

/// <summary>池化的异步完成源。基于 ManualResetValueTaskSourceCore 实现，避免每次 SendMessageAsync 分配 TaskCompletionSource</summary>
/// <remarks>
/// 生命周期：Rent → AttachSpan/RegisterCancellation → 设置到匹配队列 → SetResult/SetCanceled → 消费者 await 完成 → GetResult 内自动释放资源并归还到池。
/// 线程安全：通过 CAS 保证 SetResult/SetCanceled 只成功一次。
/// 非异步模式：SendMessageAsync 无需 async/await，直接返回 ValueTask，消除状态机分配。
/// </remarks>
sealed class PooledValueTaskSource : IValueTaskSource<Object>
{
    private ManualResetValueTaskSourceCore<Object> _core;
    private volatile Int32 _completed;

    /// <summary>关联的性能追踪 Span，在 GetResult 中自动释放</summary>
    private ISpan? _span;

    /// <summary>取消令牌注册，在 GetResult 中自动释放</summary>
    private CancellationTokenRegistration _registration;

    private static readonly Pool<PooledValueTaskSource> _pool = new();

    /// <summary>从池中借出</summary>
    public static PooledValueTaskSource Rent()
    {
        var source = _pool.Get();
        // 在借出时重置完成标志，而非 GetResult 中重置，避免匹配队列残留引用对已回收源重复操作
        source._completed = 0;
        return source;
    }

    /// <summary>当前版本号</summary>
    public Int16 Version => _core.Version;

    /// <summary>是否已完成</summary>
    public Boolean IsCompleted => _core.GetStatus(_core.Version) != ValueTaskSourceStatus.Pending;

    /// <summary>获取可等待的 ValueTask</summary>
    public ValueTask<Object> ValueTask => new(this, _core.Version);

    /// <summary>关联性能追踪 Span，将在 GetResult 中自动 Dispose</summary>
    /// <param name="span">追踪 Span</param>
    public void AttachSpan(ISpan? span) => _span = span;

    /// <summary>注册取消令牌，将在 GetResult 中自动 Dispose 注册</summary>
    /// <param name="cancellationToken">取消令牌</param>
    public void RegisterCancellation(CancellationToken cancellationToken)
    {
        if (cancellationToken.CanBeCanceled)
            _registration = cancellationToken.Register(static s => ((PooledValueTaskSource)s!).TrySetCanceled(), this);
    }

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
        catch (TaskCanceledException ex)
        {
            _span?.AppendTag(ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _span?.SetError(ex, null);
            throw;
        }
        finally
        {
            // 释放关联资源
            _registration.Dispose();
            _span?.Dispose();

            // 重置状态并归还到池
            // 不重置 _completed，保留为 1，防止匹配队列残留引用对已回收源重复调用 TrySetCanceled
            _span = null;
            _registration = default;
            _core.Reset();
            _pool.Return(this);
        }
    }

    ValueTaskSourceStatus IValueTaskSource<Object>.GetStatus(Int16 token) => _core.GetStatus(token);

    void IValueTaskSource<Object>.OnCompleted(Action<Object?> continuation, Object? state, Int16 token, ValueTaskSourceOnCompletedFlags flags) =>
        _core.OnCompleted(continuation, state, token, flags);
}
#endif
