using System.Diagnostics.CodeAnalysis;
using NewLife.Log;
using NewLife.Messaging;

namespace NewLife.Caching;

/// <summary>消息队列事件总线。通过消息队列来发布和订阅消息</summary>
/// <remarks>
/// - 使用 <see cref="ICache.GetQueue{T}(String)"/> 获取后端队列；
/// - 订阅时启动后台消费循环，将队列消息分发给本地订阅者；
/// - 支持取消令牌优雅停止后台循环。
/// </remarks>
public class QueueEventBus<TEvent>(ICache cache, String topic) : EventBus<TEvent>, ITracerFeature
{
    #region 属性
    /// <summary>链路追踪</summary>
    public ITracer? Tracer { get; set; }

    private IProducerConsumer<TEvent>? _queue;
    private CancellationTokenSource? _source;
    private Task? _consumerTask;
    #endregion

    /// <summary>销毁。先取消后台任务，再释放资源</summary>
    /// <param name="disposing">是否由 <see cref="DisposeBase.Dispose()"/> 调用</param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        // 取消后台消费循环，并等待其退出后再释放 CTS
        var src = Interlocked.Exchange(ref _source, null);
        if (src != null)
        {
            try
            {
                if (!src.IsCancellationRequested) src.Cancel();
            }
            catch (ObjectDisposedException) { }
        }

        var task = Interlocked.Exchange(ref _consumerTask, null);
        if (task != null)
        {
            try { task.Wait(3_000); }
            catch (AggregateException) { }
        }

        src?.Dispose();
    }

    /// <summary>初始化：按需创建队列实例</summary>
    [MemberNotNull(nameof(_queue))]
    protected virtual void Init()
    {
        Tracer ??= (cache as ITracerFeature)?.Tracer;
        _queue ??= cache.GetQueue<TEvent>(topic);
    }

    /// <summary>发布消息到消息队列</summary>
    /// <param name="event">事件</param>
    /// <param name="context">上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    public override Task<Int32> PublishAsync(TEvent @event, IEventContext<TEvent>? context = null, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
#if NET45
            var tcs = new TaskCompletionSource<Int32>();
            tcs.SetCanceled();
            return tcs.Task;
#else
            return Task.FromCanceled<Int32>(cancellationToken);
#endif
        }

        Init();
        var rs = _queue.Add(@event);

        return Task.FromResult(rs);
    }

    /// <summary>订阅消息。启动后台循环，从消息队列订阅消息并分发到本地订阅者</summary>
    /// <param name="handler">处理器</param>
    /// <param name="clientId">客户标识。每个客户只能订阅一次，重复订阅将会挤掉前一次订阅</param>
    public override Boolean Subscribe(IEventHandler<TEvent> handler, String clientId = "")
    {
        if (_source == null)
        {
            var source = new CancellationTokenSource();
            if (Interlocked.CompareExchange(ref _source, source, null) == null)
            {
                Init();
                // 将固定的 source 传入，避免闭包捕获 _source 产生竞争
                var t = Task.Factory.StartNew(() => ConsumeMessage(source), source.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
                Interlocked.Exchange(ref _consumerTask, t);
            }
            else
            {
                // 如果竞争失败，释放本地创建的 source
                source.Dispose();
            }
        }

        return base.Subscribe(handler, clientId);
    }

    /// <summary>从队列消费消息并通过事件总线分发给本地订阅者</summary>
    /// <param name="source">取消令牌源</param>
    protected virtual async Task ConsumeMessage(CancellationTokenSource source)
    {
        DefaultSpan.Current = null;
        var cancellationToken = source.Token;
        while (!cancellationToken.IsCancellationRequested)
        {
            ISpan? span = null;
            try
            {
                var msg = await _queue!.TakeOneAsync(15, cancellationToken).ConfigureAwait(false);
                if (msg != null)
                {
                    span = Tracer?.NewSpan($"event:{topic}", msg);
                    if (span != null && msg is ITraceMessage tm) span.Detach(tm.TraceId);

                    // 发布到事件总线
                    await DispatchAsync(msg, null, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay(1_000, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (ThreadAbortException) { break; }
            catch (ThreadInterruptedException) { break; }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested) break;

                span?.SetError(ex);
            }
            finally
            {
                span?.Dispose();
            }
        }
    }
}