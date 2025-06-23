﻿using System.Collections.Concurrent;
using NewLife.Log;
#if !NET45
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.Model;

/// <summary>无锁并行编程模型</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/actor
/// 
/// 独立线程轮询消息队列，简单设计避免影响默认线程池。
/// 适用于任务颗粒较大的场合，例如IO操作。
/// </remarks>
public interface IActor
{
    /// <summary>添加消息，驱动内部处理</summary>
    /// <param name="message">消息</param>
    /// <param name="sender">发送者</param>
    /// <returns>返回待处理消息数</returns>
    Int32 Tell(Object message, IActor? sender = null);
}

/// <summary>Actor上下文</summary>
public class ActorContext
{
    /// <summary>发送者</summary>
    public IActor? Sender { get; set; }

    /// <summary>消息</summary>
    public Object? Message { get; set; }
}

/// <summary>无锁并行编程模型</summary>
/// <remarks>
/// 独立线程轮询消息队列，简单设计避免影响默认线程池。
/// </remarks>
public abstract class Actor : DisposeBase, IActor
{
    #region 属性
    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>是否启用</summary>
    public Boolean Active { get; private set; }

    /// <summary>受限容量。最大可堆积的消息数，默认Int32.MaxValue</summary>
    public Int32 BoundedCapacity { get; set; } = Int32.MaxValue;

    /// <summary>批大小。每次处理消息数，默认1，大于1表示启用批量处理模式</summary>
    public Int32 BatchSize { get; set; } = 1;

    /// <summary>是否长时间运行。长时间运行任务使用独立线程，默认true</summary>
    public Boolean LongRunning { get; set; } = true;

    /// <summary>存放消息的邮箱。默认FIFO实现，外部可覆盖</summary>
    protected BlockingCollection<ActorContext>? MailBox { get; set; }

    /// <summary>
    /// 性能追踪器
    /// </summary>
    public ITracer? Tracer { get; set; }

    /// <summary>
    /// 父级性能追踪器。用于把内外调用链关联起来
    /// </summary>
    public ISpan? TracerParent { get; set; }

    private Task? _task;
    private Exception? _error;
    private CancellationTokenSource? _source;
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public Actor() => Name = GetType().Name.TrimEnd("Actor");

    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        _error = null;
        Stop(0);

        if (_source != null)
        {
            _source.Cancel();
            _source.TryDispose();
        }

        _task.TryDispose();

        MailBox.TryDispose();
    }

    /// <summary>已重载。显示名称</summary>
    /// <returns></returns>
    public override String ToString() => Name;
    #endregion

    #region 方法
    /// <summary>通知开始处理</summary>
    /// <remarks>
    /// 添加消息时自动触发
    /// </remarks>
    public virtual Task? Start() => Start(default);

    /// <summary>通知开始处理</summary>
    /// <remarks>
    /// 添加消息时自动触发
    /// </remarks>
    /// <param name="cancellationToken">取消令牌。可用于通知内部取消工作</param>
    /// <returns></returns>
    public virtual Task? Start(CancellationToken cancellationToken = default)
    {
        if (Active) return _task;
        lock (this)
        {
            if (Active) return _task;

            if (Tracer == null && TracerParent is DefaultSpan ds) Tracer = ds.Tracer;

            using var span = Tracer?.NewSpan("actor:Start", Name);

            _source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            MailBox ??= new BlockingCollection<ActorContext>(BoundedCapacity);

            // 启动异步
            if (_task == null)
            {
                lock (this)
                {
                    _task ??= OnStart(_source.Token);
                }
            }

            Active = true;

            return _task;
        }
    }

    /// <summary>开始时，返回执行线程包装任务</summary>
    /// <returns></returns>
    protected virtual Task OnStart(CancellationToken cancellationToken)
    {
        var creationOptions = LongRunning ? TaskCreationOptions.LongRunning : TaskCreationOptions.None;
        var scheduler = TaskScheduler.Current ?? TaskScheduler.Default;
        return Task.Factory.StartNew(DoActorWork, cancellationToken, creationOptions, scheduler);
    }

    /// <summary>通知停止添加消息，并等待处理完成</summary>
    /// <param name="msTimeout">等待的毫秒数。0表示不等待，-1表示无限等待</param>
    public virtual Boolean Stop(Int32 msTimeout = 0)
    {
        using var span = Tracer?.NewSpan("actor:Stop", $"{Name} msTimeout={msTimeout}");
        try
        {
            MailBox?.CompleteAdding();

            if (msTimeout > 0 && _source != null && !_source.IsCancellationRequested)
                _source.CancelAfter(msTimeout);

            if (_error != null) throw _error;
            if (msTimeout == 0 || _task == null) return true;

            return _task.Wait(msTimeout);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    /// <summary>添加消息，驱动内部处理</summary>
    /// <param name="message">消息</param>
    /// <param name="sender">发送者</param>
    /// <returns>返回待处理消息数</returns>
    public virtual Int32 Tell(Object message, IActor? sender = null)
    {
        //using var span = Tracer?.NewSpan("actor:Tell", Name);
        if (!Active)
        {
            if (_error != null) throw _error;

            // 自动开始
            Start();

            if (!Active) throw new ObjectDisposedException(nameof(Actor));
        }

        var box = MailBox ?? throw new ArgumentNullException(nameof(MailBox));
        box.Add(new ActorContext { Sender = sender, Message = message });

        return box.Count;
    }

    /// <summary>循环消费消息</summary>
    private void DoActorWork()
    {
        DefaultSpan.Current = TracerParent;

        using var span = Tracer?.NewSpan("actor:Loop", Name);
        try
        {
            Loop();
        }
        catch (OperationCanceledException) { }
        catch (InvalidOperationException) { /*CompleteAdding后Take会抛出IOE异常*/}
        catch (Exception ex)
        {
            span?.SetError(ex, null);

            _error = ex;
            XTrace.WriteException(ex);
        }

        Active = false;
    }

    /// <summary>循环消费消息</summary>
    protected virtual void Loop()
    {
        var box = MailBox;
        if (box == null || _source == null) return;

        var span = DefaultSpan.Current;
        var token = _source.Token;
        while (!_source.IsCancellationRequested && !box.IsCompleted)
        {
            if (BatchSize <= 1)
            {
                var ctx = box.Take(token);
                var task = ReceiveAsync(ctx, token);
                task?.Wait(token);

                if (span != null) span.Value++;
            }
            else
            {
                var list = new List<ActorContext>();

                // 阻塞取一个
                var ctx = box.Take(token);
                list.Add(ctx);

                // 不阻塞取一批
                for (var i = 1; i < BatchSize; i++)
                {
                    if (!box.TryTake(out ctx)) break;

                    list.Add(ctx);
                }
                if (span != null) span.Value += list.Count;

                var task = ReceiveAsync(list.ToArray(), token);
                task?.Wait(token);
            }
        }
    }

    /// <summary>处理消息。批大小为1时使用该方法</summary>
    /// <param name="context">上下文</param>
    /// <param name="cancellationToken">取消通知</param>
    protected virtual Task ReceiveAsync(ActorContext context, CancellationToken cancellationToken) => TaskEx.CompletedTask;

    /// <summary>批量处理消息。批大小大于1时使用该方法</summary>
    /// <param name="contexts">上下文集合</param>
    /// <param name="cancellationToken">取消通知</param>
    protected virtual Task ReceiveAsync(ActorContext[] contexts, CancellationToken cancellationToken) => TaskEx.CompletedTask;
    #endregion
}