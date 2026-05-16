#if NETFRAMEWORK || NETSTANDARD2_0
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace System.Threading.Tasks.Sources;

// 注：本文件移植自 Microsoft.Bcl.AsyncInterfaces，针对 .NET Standard 2.0 / .NET Framework 降级适配。
// 相比原版主要差异：
// - ThrowHelper 调用改为直接 throw new 语句
// - ThreadPool.{Unsafe}QueueUserWorkItem 改为 Task.Factory.StartNew
// - ExecutionContext.RunInternal 改为 ExecutionContext.Run

/// <summary>为实现手动重置的 <see cref="IValueTaskSource{TResult}"/> 或 <see cref="IValueTaskSource"/> 提供核心逻辑</summary>
/// <typeparam name="TResult">结果类型</typeparam>
[StructLayout(LayoutKind.Auto)]
public struct ManualResetValueTaskSourceCore<TResult>
{
    /// <summary>
    /// 操作完成前 <see cref="OnCompleted"/> 被调用时存储的后续回调；
    /// 或已设置为 <see cref="ManualResetValueTaskSourceCoreShared.s_sentinel"/> 表示操作先完成；
    /// 或为 null 表示两者均未发生。
    /// </summary>
    private Action<Object?>? _continuation;
    /// <summary>传递给 <see cref="_continuation"/> 的状态对象</summary>
    private Object? _continuationState;
    /// <summary>需要流动到回调的 <see cref="ExecutionContext"/>，不需要流动时为 null</summary>
    private ExecutionContext? _executionContext;
    /// <summary>捕获的 <see cref="SynchronizationContext"/> 或 <see cref="TaskScheduler"/>，不需要时为 null</summary>
    private Object? _capturedContext;
    /// <summary>当前操作是否已完成</summary>
    private Boolean _completed;
    /// <summary>操作成功时的结果，或尚未完成/失败时的默认值</summary>
    private TResult? _result;
    /// <summary>操作失败时的异常，或尚未完成/成功完成时为 null</summary>
    private ExceptionDispatchInfo? _error;
    /// <summary>当前版本号，防止错误使用</summary>
    private Int16 _version;

    /// <summary>是否强制异步执行后续回调。false 时可能同步执行，true 时始终异步执行</summary>
    public Boolean RunContinuationsAsynchronously { get; set; }

    /// <summary>重置以准备下一次操作</summary>
    public void Reset()
    {
        _version++;
        _completed = false;
        _result = default!;
        _error = null;
        _executionContext = null;
        _capturedContext = null;
        _continuation = null;
        _continuationState = null;
    }

    /// <summary>以成功结果完成操作</summary>
    /// <param name="result">结果值</param>
    public void SetResult(TResult result)
    {
        _result = result;
        SignalCompletion();
    }

    /// <summary>以异常完成操作</summary>
    /// <param name="error">异常</param>
    public void SetException(Exception error)
    {
        _error = ExceptionDispatchInfo.Capture(error);
        SignalCompletion();
    }

    /// <summary>获取操作版本号</summary>
    public Int16 Version => _version;

    /// <summary>获取操作状态</summary>
    /// <param name="token">由 <see cref="ValueTask"/> 构造时提供的令牌值</param>
    /// <returns>操作状态</returns>
    public ValueTaskSourceStatus GetStatus(Int16 token)
    {
        ValidateToken(token);
        return _continuation == null || !_completed
            ? ValueTaskSourceStatus.Pending
            : _error == null
                ? ValueTaskSourceStatus.Succeeded
                : _error.SourceException is OperationCanceledException
                    ? ValueTaskSourceStatus.Canceled
                    : ValueTaskSourceStatus.Faulted;
    }

    /// <summary>获取操作结果</summary>
    /// <param name="token">由 <see cref="ValueTask"/> 构造时提供的令牌值</param>
    /// <returns>操作结果</returns>
    public TResult GetResult(Int16 token)
    {
        ValidateToken(token);
        if (!_completed) throw new InvalidOperationException();

        _error?.Throw();
        return _result!;
    }

    /// <summary>为此操作调度后续回调</summary>
    /// <param name="continuation">操作完成时调用的后续委托</param>
    /// <param name="state">传递给后续委托的状态对象</param>
    /// <param name="token">由 <see cref="ValueTask"/> 构造时提供的令牌值</param>
    /// <param name="flags">描述后续行为的标志</param>
    public void OnCompleted(Action<Object?> continuation, Object? state, Int16 token, ValueTaskSourceOnCompletedFlags flags)
    {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        ValidateToken(token);

        if ((flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != 0)
            _executionContext = ExecutionContext.Capture();

        if ((flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != 0)
        {
            var sc = SynchronizationContext.Current;
            if (sc != null && sc.GetType() != typeof(SynchronizationContext))
                _capturedContext = sc;
            else
            {
                var ts = TaskScheduler.Current;
                if (ts != TaskScheduler.Default)
                    _capturedContext = ts;
            }
        }

        // 先设置状态再交换委托，防止 SetResult/SetException 与本方法并发时丢失状态
        var oldContinuation = _continuation;
        if (oldContinuation == null)
        {
            _continuationState = state;
            oldContinuation = Interlocked.CompareExchange(ref _continuation, continuation, null);
        }

        if (oldContinuation != null)
        {
            // 操作已完成，需要立即排队执行回调
            if (!ReferenceEquals(oldContinuation, ManualResetValueTaskSourceCoreShared.s_sentinel))
                throw new InvalidOperationException();

            switch (_capturedContext)
            {
                case null:
                    Task.Factory.StartNew(continuation, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                    break;
                case SynchronizationContext sc:
                    sc.Post(s =>
                    {
                        var tuple = (Tuple<Action<Object?>, Object?>)s!;
                        tuple.Item1(tuple.Item2);
                    }, Tuple.Create(continuation, state));
                    break;
                case TaskScheduler ts:
                    Task.Factory.StartNew(continuation, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, ts);
                    break;
            }
        }
    }

    private void ValidateToken(Int16 token)
    {
        if (token != _version) throw new InvalidOperationException();
    }

    private void SignalCompletion()
    {
        if (_completed) throw new InvalidOperationException();
        _completed = true;

        if (Volatile.Read(ref _continuation) != null ||
            Interlocked.CompareExchange(ref _continuation, ManualResetValueTaskSourceCoreShared.s_sentinel, null) != null)
        {
            if (_executionContext != null)
            {
                ExecutionContext.Run(
                    _executionContext,
                    s => ((ManualResetValueTaskSourceCore<TResult>)s!).InvokeContinuation(),
                    this);
            }
            else
                InvokeContinuation();
        }
    }

    private void InvokeContinuation()
    {
        Debug.Assert(_continuation != null);
        var continuation = _continuation!; // Debug.Assert 已保证非空

        switch (_capturedContext)
        {
            case null:
                if (RunContinuationsAsynchronously)
                    Task.Factory.StartNew(continuation, _continuationState, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                else
                    continuation(_continuationState);
                break;
            case SynchronizationContext sc:
                sc.Post(s =>
                {
                    var state = (Tuple<Action<Object?>, Object?>)s!;
                    state.Item1(state.Item2);
                }, Tuple.Create(continuation, _continuationState));
                break;
            case TaskScheduler ts:
                Task.Factory.StartNew(continuation, _continuationState, CancellationToken.None, TaskCreationOptions.DenyChildAttach, ts);
                break;
        }
    }
}

internal static class ManualResetValueTaskSourceCoreShared
{
    internal static readonly Action<Object?> s_sentinel = CompletionSentinel;

    private static void CompletionSentinel(Object? _)
    {
        Debug.Fail("哨兵委托不应被调用");
        throw new InvalidOperationException();
    }
}
#endif
