using System.Buffers;
using System.Net.Sockets;
using NewLife.Data;
using NewLife.Messaging;

namespace NewLife.Net;

/// <summary>TCP 会话的流式收发：出站可选发送队列（发送泵）与流式/文件发送</summary>
partial class TcpSession
{
    #region 出站队列
    /// <summary>发送队列（出站）。按需创建：首次访问后启动发送泵，入队数据由泵批量写出</summary>
    /// <remarks>
    /// <para>与直发并存：<see cref="SessionBase.Send(IPacket)"/> 系列始终直发，本队列是批量场景的第二出口，不改变直发路径。</para>
    /// <para><b>顺序</b>：同一条逻辑消息必须走同一出口；两条出口混用时，消息之间不保证先后顺序（各自内部有序）。</para>
    /// <para><b>所有权</b>：入队的 <see cref="IPacket"/> 所有权转移给队列，泵写出后由队列释放，调用方不得再释放。</para>
    /// <para><b>背压</b>：未写出数据达到 <see cref="Pipe.PauseThreshold"/> 时入队异步等待，泵写出后自动恢复。</para>
    /// </remarks>
    public Pipe SendQueue
    {
        get
        {
            var queue = _sendQueue;
            if (queue != null) return queue;

            // 使用独立锁对象，禁止 lock(this)：泵与入队方都在锁外等待，锁对象本身不能被长时间持有
            lock (_sendQueueLock)
            {
                queue = _sendQueue;
                if (queue != null) return queue;

                queue = CreateSendQueue();
                queue.Resumed += OnSendQueueResumed;
                _sendQueue = queue;

                StartSendPump(queue);

                return queue;
            }
        }
    }

    private readonly Object _sendQueueLock = new();

    /// <summary>发送队列。双检锁创建；volatile 保证入队方的无锁快照读可见</summary>
    private volatile Pipe? _sendQueue;

    /// <summary>获取已创建的发送队列。未访问过 <see cref="SendQueue"/> 时返回 null（不触发创建）</summary>
    /// <remarks>会话关闭时复位为 null，再次访问 <see cref="SendQueue"/> 会重建新队列与发送泵</remarks>
    /// <returns>发送队列；未创建时为 null</returns>
    public Pipe? GetSendQueue() => _sendQueue;

    /// <summary>创建发送队列。子类可重写以自定义水位等参数</summary>
    /// <returns>发送队列</returns>
    protected virtual Pipe CreateSendQueue()
    {
        // 出站水位 256K 暂停 / 128K 恢复：出站积压是应用层待发数据、按会话占用内存，
        // 取入站（1M/512K）的 1/4 已足够吸收抖动；恢复水位取暂停的一半，迟滞避免频繁抖动
        return new Pipe
        {
            PauseThreshold = 256 * 1024,
            ResumeThreshold = 128 * 1024,
        };
    }

    /// <summary>经发送队列发送（所有权转移）。队列积压达上限时异步等待，泵写出后自动恢复</summary>
    /// <remarks>
    /// <para>与直发系列并存；同一条逻辑消息必须走同一出口，混用只保证各自内部有序。</para>
    /// <para>不提供同步重载：同步等待会把入队方线程挂在水位上，异步等待期间不占线程。</para>
    /// <para><b>失败归属</b>：入队成功后句柄即归队列（含返回 false 的情形，此时队列已代为释放）；
    /// 仅会话已释放/未打开等入队前抛异常时句柄仍在调用方，由调用方负责释放。</para>
    /// </remarks>
    /// <param name="data">数据包。所有权转移：入队后由发送泵负责释放，调用方不得再释放</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>入队并提交成功返回 true；等待水位期间会话关闭、数据被丢弃返回 false</returns>
    public async ValueTask<Boolean> SendQueuedAsync(IPacket data, CancellationToken cancellationToken = default)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (Disposed) throw new ObjectDisposedException(GetType().Name);
        if (!Open()) throw new InvalidOperationException($"Session [{GetType().Name}] is not open.");

        var queue = SendQueue;
        queue.Writer.Append(data);

        // 背压门：管道写侧回压是单写者（并发 FlushAsync(true) 会抛），故自行以 IsPaused + Resumed 广播等待
        while (queue.IsPaused && !queue.IsCompleted)
        {
            await WaitQueueResumeAsync(queue, cancellationToken).ConfigureAwait(false);
        }

        return !queue.IsCompleted;
    }

    private TaskCompletionSource<Boolean>? _queueResume;
    private readonly Object _queueResumeLock = new();

    /// <summary>等待发送队列水位恢复。多生产者共享同一唤醒源，由 <see cref="Pipe.Resumed"/> 广播完成</summary>
    /// <param name="queue">发送队列</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task WaitQueueResumeAsync(Pipe queue, CancellationToken cancellationToken)
    {
        Task<Boolean> task;
        lock (_queueResumeLock)
        {
            // 与事件回调同锁复查：恢复若发生在判定与登记之间，IsPaused 已为 false，直接返回，避免丢唤醒
            if (!queue.IsPaused || queue.IsCompleted) return;

#if NET45
            _queueResume ??= new TaskCompletionSource<Boolean>();
#else
            _queueResume ??= new TaskCompletionSource<Boolean>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
            task = _queueResume.Task;
        }

        if (!cancellationToken.CanBeCanceled)
        {
            await task.ConfigureAwait(false);

            return;
        }

        // 低版本框架没有 Task.WaitAsync，手工用 WhenAny 附加取消；
        // 现代框架直接带取消等待，取消即抛 OperationCanceledException，与下方 ThrowIfCancellationRequested 等价
#if NET6_0_OR_GREATER
        await task.WaitAsync(cancellationToken).ConfigureAwait(false);
#else
#if NET45
        var tcs = new TaskCompletionSource<Boolean>();
#else
        var tcs = new TaskCompletionSource<Boolean>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
        using (cancellationToken.Register(() => tcs.TrySetResult(false)))
        {
            await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
#endif
    }

    /// <summary>发送队列水位恢复：广播唤醒全部等待中的入队方</summary>
    private void OnSendQueueResumed(Object? sender, EventArgs e) => WakeQueueWaiters();

    /// <summary>广播唤醒全部等待中的入队方</summary>
    /// <remarks>
    /// <para>水位恢复与队列结束都走这里：等待者醒来后自行复查 <see cref="Pipe.IsPaused"/> 与 <see cref="Pipe.IsCompleted"/>，
    /// 暂停未解除则继续等待，队列已结束则退出并返回 false。</para>
    /// <para>关闭路径必须显式调用：<see cref="PipeWriter.Complete"/> 只唤醒挂起读与挂起提交，并不触发 <see cref="Pipe.Resumed"/>，
    /// 不唤醒会让等水位的入队方永久挂起（HTTP 同步链上即表现为处理线程卡死）。</para>
    /// </remarks>
    private void WakeQueueWaiters()
    {
        TaskCompletionSource<Boolean>? tcs;
        lock (_queueResumeLock)
        {
            tcs = _queueResume;
            _queueResume = null;
        }

        tcs?.TrySetResult(true);
    }
    #endregion

    #region 关闭收尾
    /// <summary>关闭。重写以收尾流式收发：出站队列残余直接丢弃并归还缓冲（不排空），入站管道终止（挂起读立即完成）</summary>
    /// <param name="reason">关闭原因。便于日志分析</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>是否成功</returns>
    public override async Task<Boolean> CloseAsync(String reason, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.CloseAsync(reason, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // 仅在关闭真正发生（会话已不活动）时执行流式收尾：取消在入口被拦截（会话仍活动）时
            // 不得拆掉管道与队列——否则会话外显“还开着”却收不到数据。
            // 未打开的关闭（Active 亦为假）照常收尾，幂等复位上一轮资源
            if (!Active) CleanupStreamFlows();
        }
    }

    /// <summary>流式收尾：停发送泵并释放出站队列（残余丢弃）、终止入站管道、释放暂存接收槽</summary>
    private void CleanupStreamFlows()
    {
        // 出站队列：先停泵（结束写侧唤醒挂起读，并等泵退出后不再触碰队列缓冲），
        // 入队方的水位等待随之结束，最后释放队列归还残余池缓冲。
        // 不排空——停机场景对端往往已不可达，队列残余本就送不出去，逐会话限时排空会让总耗时随会话数线性放大。
        //
        // 取走队列与 getter 的双检锁互斥，避免关闭期间又新建出一个无人释放的队列与发送泵
        Pipe? queue;
        lock (_sendQueueLock)
        {
            queue = _sendQueue;
            _sendQueue = null;
        }

        if (queue != null)
        {
            queue.Resumed -= OnSendQueueResumed;

            StopSendPump(queue);

            // 唤醒仍在等水位的入队方：完成写侧不会触发 Resumed，不唤醒它们会永久挂起
            WakeQueueWaiters();

            queue.Dispose();
        }

        // 入站流：释放管道（等价于完成写侧唤醒挂起读 + 结束读侧归还残余池缓冲）。
        // 只完成写侧会让读侧链上尚未消费的池缓冲一直挂到管道随对象不可达，由终结器兜底归还；
        // 服务端会话还可能存活到 SessionTimeout，等于长期占用内存池。
        _pipe?.Dispose();

        // 复位入站管道，交由懒创建在再次访问时重建。客户端会话关闭后未释放（会话对象一次性，由调用方保管），
        // 若继续持有已完成管道，再次访问会拿到已结束的读取器，协议模式接收泵在其上读取即抛异常
        _pipe = null;

        ReleaseParkedReceive();
    }
    #endregion

}
