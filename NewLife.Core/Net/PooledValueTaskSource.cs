using System.Threading.Tasks.Sources;
using NewLife.Collections;
using NewLife.Log;

namespace NewLife.Net;

/// <summary>池化任务源的弱类型完成接口。供匹配队列等组件在不同结果类型（Object/Message）上统一设置结果或取消</summary>
/// <remarks>完成方法均要求版本一致：匹配队列可能在等待方取消后才拿到迟到响应，
/// 而本实例此时已被归还池并可能被新请求借出复用，版本不符即丢弃，避免误完成新请求。</remarks>
interface IPooledSource
{
    /// <summary>当前版本号。匹配队列在入队时记录，完成时回传校验</summary>
    Int16 Version { get; }

    /// <summary>尝试设置成功结果（仅首次调用生效，且要求版本一致）</summary>
    /// <param name="result">结果值</param>
    /// <param name="version">入队时记录的版本号</param>
    /// <returns>是否成功设置</returns>
    Boolean TrySetResult(Object result, Int16 version);

    /// <summary>尝试设置取消（仅首次调用生效，且要求版本一致）</summary>
    /// <param name="version">入队时记录的版本号</param>
    /// <returns>是否成功设置</returns>
    Boolean TrySetCanceled(Int16 version);
}

/// <summary>池化的异步完成源。基于 ManualResetValueTaskSourceCore 实现，避免每次 SendMessageAsync 分配 TaskCompletionSource</summary>
/// <remarks>
/// 生命周期：Rent → AttachSpan/RegisterCancellation → 设置到匹配队列 → SetResult/SetCanceled → 消费者 await 完成 → GetResult 内自动释放资源并归还到池。
/// 线程安全：通过 CAS 保证 SetResult/SetCanceled 只成功一次。
/// 非异步模式：SendMessageAsync 无需 async/await，直接返回 ValueTask，消除状态机分配。数据支撑（基准实测）：替代每次调用的 TaskCompletionSource 分配，并配合非异步化与 ValueTask 返回，逐项消除编译器状态机（约 200B）与 AsTask 包装（约 56B）（见《网络库编解码器Echo性能测试报告》优化项）。
/// </remarks>
sealed class PooledValueTaskSource<T> : IValueTaskSource<T>, IPooledSource
{
    private ManualResetValueTaskSourceCore<T> _core;
    private volatile Int32 _completed;

    /// <summary>尝试设置成功结果（弱类型版，结果类型不符时失败）</summary>
    /// <param name="result">结果值</param>
    /// <param name="version">入队时记录的版本号</param>
    /// <returns>是否成功设置</returns>
    Boolean IPooledSource.TrySetResult(Object result, Int16 version) => version == _core.Version && result is T value && TrySetResult(value);

    /// <summary>尝试设置取消（弱类型版）</summary>
    /// <param name="version">入队时记录的版本号</param>
    /// <returns>是否成功设置</returns>
    Boolean IPooledSource.TrySetCanceled(Int16 version) => version == _core.Version && TrySetCanceled();

    /// <summary>关联的性能追踪 Span，在 GetResult 中自动释放</summary>
    private ISpan? _span;

    /// <summary>取消令牌注册，在 GetResult 中自动释放</summary>
    private CancellationTokenRegistration _registration;

    private static readonly Pool<PooledValueTaskSource<T>> _pool = new();

    /// <summary>从池中借出</summary>
    public static PooledValueTaskSource<T> Rent()
    {
        var source = _pool.Get();
        // 在借出时重置完成标志，而非 GetResult 中重置：本实例要能被下一次请求重新使用。
        // 复位会一并“解除”上一轮的完成定案，所以完成方必须带版本号（见 IPooledSource），
        // 残留的匹配队列项持有旧版本，无法完成后续借出的新请求
        source._completed = 0;
        // 对齐旧 TCS（TaskCreationOptions.RunContinuationsAsynchronously）行为，避免续体在接收线程同步执行
        source._core.RunContinuationsAsynchronously = true;
        return source;
    }

    /// <summary>当前版本号</summary>
    public Int16 Version => _core.Version;

    /// <summary>是否已完成</summary>
    public Boolean IsCompleted => _core.GetStatus(_core.Version) != ValueTaskSourceStatus.Pending;

    /// <summary>获取可等待的 ValueTask</summary>
    public ValueTask<T> ValueTask => new(this, _core.Version);

    /// <summary>关联性能追踪 Span，将在 GetResult 中自动 Dispose</summary>
    /// <param name="span">追踪 Span</param>
    public void AttachSpan(ISpan? span) => _span = span;

    /// <summary>注册取消令牌，将在 GetResult 中自动 Dispose 注册</summary>
    /// <param name="cancellationToken">取消令牌</param>
    public void RegisterCancellation(CancellationToken cancellationToken)
    {
        if (cancellationToken.CanBeCanceled)
            _registration = cancellationToken.Register(static s => ((PooledValueTaskSource<T>)s!).TrySetCanceled(), this);
    }

    /// <summary>尝试设置成功结果（仅首次调用生效）</summary>
    /// <param name="result">结果值</param>
    /// <returns>是否成功设置</returns>
    public Boolean TrySetResult(T result)
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

    T IValueTaskSource<T>.GetResult(Int16 token)
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
            // 不重置 _completed，保留为 1，防止匹配队列残留引用对已回收源重复调用 TrySetCanceled；
            // Reset 会递增版本号，残留项因此彻底失效（版本不符）
            _span = null;
            _registration = default;
            _core.Reset();
            _pool.Return(this);
        }
    }

    ValueTaskSourceStatus IValueTaskSource<T>.GetStatus(Int16 token) => _core.GetStatus(token);

    void IValueTaskSource<T>.OnCompleted(Action<Object?> continuation, Object? state, Int16 token, ValueTaskSourceOnCompletedFlags flags) =>
        _core.OnCompleted(continuation, state, token, flags);
}
