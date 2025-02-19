using System.Diagnostics.CodeAnalysis;
using NewLife.Log;
using NewLife.Messaging;

namespace NewLife.Caching;

/// <summary>消息队列事件总线。通过消息队列来发布和订阅消息</summary>
public class QueueEventBus<TEvent> : EventBus<TEvent>
{
    private IProducerConsumer<TEvent>? _queue;
    private readonly ICache _cache;
    private readonly String _topic;
    private readonly String _clientId;
    CancellationTokenSource? _source;

    /// <summary>实例化消息队列事件总线</summary>
    public QueueEventBus(ICache cache, String topic, String clientId)
    {
        _cache = cache;
        _topic = topic;
        _clientId = clientId;
    }

    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        _source?.TryDispose();
    }

    /// <summary>初始化</summary>
    [MemberNotNull(nameof(_queue))]
    protected virtual void Init()
    {
        if (_queue != null) return;

        _queue = _cache.GetQueue<TEvent>(_topic);
    }

    /// <summary>发布消息到消息队列</summary>
    /// <param name="event">事件</param>
    /// <param name="context">上下文</param>
    public override Task<Int32> PublishAsync(TEvent @event, IEventContext<TEvent>? context = null)
    {
        Init();
        var rs = _queue.Add(@event);

        return Task.FromResult(rs);
    }

    /// <summary>订阅消息。启动大循环，从消息队列订阅消息，再分发到本地订阅者</summary>
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
                _ = Task.Run(() => ConsumeMessage(_source));
            }
        }

        return base.Subscribe(handler, clientId);
    }

    /// <summary>从队列中消费消息，经事件总线送给设备会话</summary>
    /// <param name="source"></param>
    /// <returns></returns>
    protected virtual async Task ConsumeMessage(CancellationTokenSource source)
    {
        DefaultSpan.Current = null;
        var cancellationToken = source.Token;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var msg = await _queue!.TakeOneAsync(15, cancellationToken).ConfigureAwait(false);
                if (msg != null)
                {
                    // 发布到事件总线
                    await base.PublishAsync(msg).ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay(1_000, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
        }
        finally
        {
            source.Cancel();
        }
    }
}
