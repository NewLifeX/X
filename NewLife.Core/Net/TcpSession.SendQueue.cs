using NewLife.Data;

namespace NewLife.Net;

/// <summary>增强TCP客户端（出站队列与发送泵：可选第二出口）</summary>
/// <remarks>
/// <para>与直发并存：<see cref="SessionBase.Send(IPacket)"/> 系列始终直发，本队列是批量场景的第二出口；同一条逻辑消息必须走同一出口，两条出口混用时消息之间不保证先后。</para>
/// <para>数据流：<see cref="TcpSession.SendQueuedAsync"/> 入队（积压达水位异步等待）→ 发送泵（专用线程）取整个读窗口批量写出（共用写锁）→ 套接字。关闭不排空：残余随队列释放。</para>
/// </remarks>
public partial class TcpSession
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

            _queueResume ??= CreateSignalSource();
            task = _queueResume.Task;
        }

        if (!cancellationToken.CanBeCanceled)
        {
            await task.ConfigureAwait(false);

            return;
        }

        // 低版本框架没有 Task.WaitAsync，由桥方法手工用 WhenAny 附加取消；取消即抛 OperationCanceledException
        await WaitSignalAsync(task, cancellationToken).ConfigureAwait(false);
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

    #region 发送泵
    // 发送泵（出站队列）与 SessionBase 的消息泵（入站定界）是两套独立机制，字段用 send 前缀与本类内的泵状态区分
    private CancellationTokenSource? _sendPumpCts;
    private Task? _sendPumpTask;
    private Int32 _sendPumpThreadId;

    /// <summary>启动发送泵。专用线程（LongRunning）：同步泵不能占用线程池线程，否则同步入队方在等待水位时把线程池占满，泵与入队方会互相等待而死锁</summary>
    /// <param name="queue">发送队列</param>
    private void StartSendPump(Pipe queue)
    {
        var cts = new CancellationTokenSource();
        _sendPumpCts = cts;

        _sendPumpTask = Task.Factory.StartNew(() => SendPumpLoop(queue, cts.Token), cts.Token,
            TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    /// <summary>停止发送泵。先结束写侧唤醒挂起读，再限时等泵退出——泵退出后不再触碰队列缓冲，方能安全释放队列</summary>
    /// <remarks>本方法可能由泵线程自身触发（发送失败→上报→关闭），此时等待就是自死锁，按线程判定跳过</remarks>
    /// <param name="queue">发送队列</param>
    private void StopSendPump(Pipe queue)
    {
        // 结束写侧：挂起的 ReadAsync 立即返回完成，入队方的水位等待随之结束
        queue.Writer.Complete();

        var task = _sendPumpTask;
        if (task == null || task.IsCompleted) return;
        if (Environment.CurrentManagedThreadId == _sendPumpThreadId) return;

        // 限时等待：泵可能正阻塞在写锁或慢对端的套接字写上。超时先中止泵再给一小段收尾时间，
        // 仍不退出就放手——残余缓冲随管道回收，泵持有的帧自带引用计数，不会读到已归还的内存。
        // 等待上限刻意压在 2 秒内：Close 是常见操作，不能因为一个慢对端把调用方长时间卡住
        try
        {
            if (!task.Wait(1000))
            {
                _sendPumpCts?.Cancel();
                task.Wait(1000);
            }
        }
        catch (AggregateException) { }
    }

    /// <summary>发送泵：从队列取数据批量写出。一个读窗口内累积的多条消息一次散列写提交，得出批量发送</summary>
    /// <param name="queue">发送队列</param>
    /// <param name="cancellationToken">取消令牌。会话关闭时触发</param>
    private void SendPumpLoop(Pipe queue, CancellationToken cancellationToken)
    {
        _sendPumpThreadId = Environment.CurrentManagedThreadId;

        var reader = queue.Reader;
        try
        {
            // 会话已终结时立即收尾退出。只认两种终态：已释放，或服务端会话已关闭不再活动。
            // 服务端会话关闭即 Dispose、不会重开；客户端会话关闭后保留未释放（会话对象一次性，由调用方保管终态），
            // 故客户端泵不按“已关闭”提前收尾，统一交给关闭流程与最终释放处理。
            //
            // 本泵可能是关闭收尾之后才建出来的——入队方过了 Open 检查、随后关闭完成（见 SendQueue 的建队列路径），
            // 此时队列读侧不会再有写入、泵令牌也无人取消，继续阻塞在读取上会让这条 LongRunning 专用线程
            // 连同它引用的会话永久存活。
            if (Disposed || (_Server != null && !Active))
            {
                // 按关闭收尾的同构动作收尾本队列：完成写侧让等水位的入队方立即退出
                // （完成写侧不触发 Resumed，必须显式广播），读侧由 finally 完成并归还残余句柄
                queue.Writer.Complete();
                WakeQueueWaiters();

                return;
            }

            while (true)
            {
                var result = reader.ReadAsync(cancellationToken).GetAwaiter().GetResult();
                var count = result.Buffer.Length;

                if (count > 0)
                {
                    // 零拷贝切出整窗并推进消费窗口（同时解除写侧水位）：多条消息合并为一次散列写
                    var frame = reader.TakeFrame(count);

                    Exception? error = null;
                    _writeLock.Wait();
                    try
                    {
                        WritePacket(frame);
                        LastTime = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }
                    finally
                    {
                        _writeLock.Release();
                    }

                    frame.TryDispose();

                    // 上报与关闭放锁外：OnError/Close 会触发用户事件回调，回调内再次 Send 会在同一线程二次进入写锁而死锁
                    if (error != null)
                    {
                        ReportSendError(error, null);

                        break;
                    }
                }

                if (result.IsCompleted) break;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            ReportSendError(ex, null);
        }
        finally
        {
            reader.Complete();
        }
    }
    #endregion
}