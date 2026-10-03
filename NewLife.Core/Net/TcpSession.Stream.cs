using System.Buffers;
using System.Net.Sockets;
using NewLife.Data;

namespace NewLife.Net;

/// <summary>TCP 会话的流式收发：入站字节流管道（含背压暂停接收）、出站可选发送队列（发送泵）与流式/文件发送</summary>
partial class TcpSession
{
    #region 入站管道
    /// <summary>数据管道。按需创建：首次访问后，接收数据同时投递到管道供流式消费</summary>
    /// <remarks>
    /// <para>数据管道提供字节流视角，配合消息泵（<see cref="NewLife.Messaging.MessagePump"/>）支持头部先行与负载流式读取。</para>
    /// <para>投递采用共享切片（引用计数），不影响既有处理链；消费方负责消费后推进窗口，达到 <see cref="Pipe.PauseThreshold"/> 时自动暂停接收、消费恢复后自动继续。</para>
    /// </remarks>
    public Pipe Pipe
    {
        get
        {
            var pipe = _pipe;
            if (pipe != null) return pipe;

            // 使用独立锁对象，禁止 lock(this)：同步壳等待异步期间会阻塞续体线程，锁同步壳不会持有的对象才能避免死锁
            lock (_pipeLock)
            {
                return _pipe ??= CreatePipe();
            }
        }
    }

    private readonly Object _pipeLock = new();

    /// <summary>数据管道。双检锁创建；volatile 保证无锁快照读（GetPipe/AppendToPipe 等）的跨线程可见性</summary>
    private volatile Pipe? _pipe;

    /// <summary>获取已创建的数据管道。未访问过 <see cref="Pipe"/> 时返回 null（不触发创建）</summary>
    /// <remarks>会话关闭时复位为 null，重开后再访问 <see cref="Pipe"/> 即重建新管道</remarks>
    /// <returns>数据管道；未创建时为 null</returns>
    public Pipe? GetPipe() => _pipe;

    /// <summary>创建数据管道。子类可重写以自定义水位等参数</summary>
    /// <returns>数据管道</returns>
    protected virtual Pipe CreatePipe()
    {
        // 入站水位保持 Pipe 默认 1M/512K：与 Kestrel 入站缓冲上限 MaxRequestBufferSize（1MB）同量级，作为慢消费场景的内存安全阀；
        // 读侧挂起等待（整帧/最小长度未凑齐）时自动“饥饿让位”解除暂停放行数据，大帧不受水位阻挡（见《背压水位定档与内存测算报告》）。
        var pipe = new Pipe();
        pipe.Resumed += (s, e) => ResumeParkedReceive();

        return pipe;
    }

    /// <summary>本轮数据投递给入站管道。共享切片（引用计数），不改变轮句柄；轮末裁决因引用计数大于1自动换新缓冲，消费方经 Pipe.Reader 流式读取</summary>
    /// <remarks>启用管道即放弃接收槽的“轮末零 Rent/Return”复用快路径：本共享切片使轮句柄计数恒大于 1，接收环每轮都要解绑换新（池化往返实测约 8ns/次，代价可忽略）。</remarks>
    /// <param name="pk">本轮数据包装句柄</param>
    private void AppendToPipe(IPacket pk)
    {
        var pipe = _pipe;
        if (pipe == null) return;

        try
        {
            pipe.Writer.Append(pk.Slice(0, -1));
        }
        catch (Exception ex)
        {
            OnError("PipeAppend", ex);
        }
    }

    /// <summary>接收环暂停时暂存的接收事件参数。TCP 单 SAEA 串行接收，至多一个</summary>
    private SocketAsyncEventArgs? _parkedRecv;

    /// <summary>暂存接收。数据管道暂停（背压）时暂不发起新接收，待消费恢复</summary>
    /// <param name="se">接收事件参数</param>
    /// <remarks>存入后复查暂停态：消费线程可能在“读取暂停态”与“存入暂存”之间完成恢复（其恢复路径取到空暂存），
    /// 此时必须立即取回重启，否则本条接收无人唤醒、接收环永久停摆（丢唤醒）。取回统一走 <see cref="ResumeParkedReceive"/>，两路以原子交换互斥、恰好一次。</remarks>
    private void ParkReceive(SocketAsyncEventArgs se)
    {
        var old = Interlocked.Exchange(ref _parkedRecv, se);
        if (old != null && old != se)
        {
            // 理论不可达：单 SAEA 串行接收至多一个暂存。防御性释放，避免泄漏
            ReleaseRecv(old, "ParkReceive");
        }

        // 丢唤醒防护：若存入瞬间暂停已解除（消费恢复路径未取到本暂存），立即重启接收
        if (_pipe?.IsPaused != true) ResumeParkedReceive();
    }

    /// <summary>消费恢复后重启暂存的接收（仍暂停则经 OnReceiveAsync 再次暂存）</summary>
    private void ResumeParkedReceive()
    {
        var se = Interlocked.Exchange(ref _parkedRecv, null);
        if (se == null) return;

        // 会话已关闭或销毁：不再重启接收，直接释放接收参数
        if (Active && !Disposed)
            StartReceive(se, _IntoThreadCount);
        else
            ReleaseRecv(se, "ResumeParkedReceive");
    }

    /// <summary>释放暂存的接收。关闭收尾时调用，避免泄漏</summary>
    private void ReleaseParkedReceive()
    {
        var se = Interlocked.Exchange(ref _parkedRecv, null);
        if (se != null) ReleaseRecv(se, "ReleaseParkedReceive");
    }
    #endregion

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
    /// <remarks>会话关闭时复位为 null，重开后再访问 <see cref="SendQueue"/> 即重建新队列与发送泵</remarks>
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

        // 低版本框架没有 Task.WaitAsync，手工用 WhenAny 附加取消
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

            // 复位入站管道，交由懒创建在重开时重建。客户端会话关闭会把 Client 置空、同一实例可再次 Open，
            // 若继续持有已完成管道：协议模式接收泵在已结束的读取器上读取抛异常并立即关闭会话，
            // 外显为“连得上、发不出、收不到”
            _pipe = null;

            ReleaseParkedReceive();
        }
    }
    #endregion

    #region 流式发送
    /// <summary>流式发送的读块大小</summary>
    /// <remarks>64KB：基准实测 16KB 分块在回环下的吞吐只有 64KB 分块的一半（小分块导致两端 syscall/唤醒次数成倍），64KB 与整段发送持平且仍远低于 LOH 阈值。</remarks>
    private const Int32 StreamChunkSize = 64 * 1024;

    /// <summary>流式发送：分块读入数据流并逐块写出，读一块写一块</summary>
    /// <remarks>
    /// <para>形态对齐主流网络框架的 SendAsync(Stream)：默认 64KB 分块，每块由池化读块直接写出（0 拷贝），全程只在读块上驻留，不产生整段内存。</para>
    /// <para><b>背压</b>：每块写完（或在写锁/内核上挂起）才读下一块，慢速对端不会导致应用层积压——内存占用恒为一块读缓冲，水位由内核发送缓冲充当。</para>
    /// <para>与 <see cref="SessionBase.Send(IPacket)"/> 共用同一把写锁：并发调用不会交错。但两者是<b>各自独立加锁</b>，
    /// “头 + 流式体”先发头部再调用本方法只在单写者场景下保持一条逻辑消息；多写者并发时头体之间可能被插入其它数据，
    /// 需要整条消息原子时改用 <see cref="SendMessageLockedAsync"/>。</para>
    /// <para><b>打开时序</b>：本方法在取写锁之前先确保会话已打开。并发首访同一未打开会话时，打开流程（含 SSL 握手）可能被两个调用者同时进入；
    /// 写锁只串行化“写套接字”，不覆盖打开流程，调用方应在发送前先完成打开。</para>
    /// <para><b>超时</b>：异步写核心按“块”重设计时预算（每块 <see cref="SessionBase.Timeout"/>），故大流的总耗时上限约等于 块数 × Timeout；
    /// 与同步直写路径的“整段总预算”（<see cref="SessionBase.Send(IPacket)"/> 链式包续发）口径不同。</para>
    /// <para>流提前结束（不足 <paramref name="length"/>）或连接故障时抛出异常，已发出的部分不会回退。</para>
    /// </remarks>
    /// <param name="source">数据流</param>
    /// <param name="length">期望发送的字节数；负数表示读到流尾</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已发送的总字节数</returns>
    public async ValueTask<Int64> SendAsync(Stream source, Int64 length = -1, CancellationToken cancellationToken = default)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (Disposed) throw new ObjectDisposedException(GetType().Name);
        if (!Open()) throw new InvalidOperationException($"Session [{GetType().Name}] is not open.");

        Exception? error = null;
        var total = 0L;

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            total = await SendStreamCoreAsync(source, length, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 调用方主动取消：不是发送故障，不上报、不关会话（finally 会释放写锁）
            throw;
        }
        catch (InvalidDataException)
        {
            // 流提前结束属调用方数据问题，不是发送故障：不上报、不关会话，直接抛（finally 会释放写锁）
            throw;
        }
        catch (Exception ex)
        {
            error = ex;
        }
        finally
        {
            _writeLock.Release();
        }

        // 上报与关闭放锁外：OnError/Close 会触发用户事件回调，回调内再次 Send 会在同一把写锁上二次等待而死锁
        if (error != null)
        {
            ReportSendError(error, null);

            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
        }

        return total;
    }

    /// <summary>分块读流并写出。读一块写一块，内存恒为一块读缓冲（调用方须已持有写锁）</summary>
    /// <remarks>每块写完（或在写锁/内核上挂起）才读下一块，慢速对端不会导致应用层积压；流提前结束时报错，已发出的部分不回退。</remarks>
    /// <param name="source">数据流</param>
    /// <param name="length">期望发送的字节数；负数表示读到流尾</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已发送的总字节数</returns>
    private async ValueTask<Int64> SendStreamCoreAsync(Stream source, Int64 length, CancellationToken cancellationToken)
    {
        var chunk = StreamChunkSize;
        var buffer = ArrayPool<Byte>.Shared.Rent(chunk);
        try
        {
            var total = 0L;
            while (length < 0 || total < length)
            {
                var size = chunk;
                if (length > 0 && length - total < size) size = (Int32)(length - total);

                var count = await source.ReadAsync(buffer, 0, size, cancellationToken).ConfigureAwait(false);
                if (count <= 0) break;

                // 写完这一块才读下一块：内存恒为一块，慢对端由内核缓冲形成背压
                var rs = await WriteMemoryAsync(buffer.AsMemory(0, count)).ConfigureAwait(false);
                if (rs < 0) throw new IOException($"Send failed on [{Name}], {total} bytes sent before failure.");

                total += count;
            }

            // 流提前结束：报错，避免调用方误以为已完整发送
            if (length > 0 && total < length) throw new InvalidDataException($"Stream ended early. Expected {length} bytes but got {total}.");

            return total;
        }
        finally
        {
            ArrayPool<Byte>.Shared.Return(buffer);
        }
    }
    #endregion

    #region 文件发送
    /// <summary>发送文件：交给内核零拷贝推送（Linux sendfile / Windows TransmitFile），应用层不经过读块搬运</summary>
    /// <remarks>
    /// <para><b>内存</b>：文件内容全程不进入应用层缓冲，也没有 64KB 读块往返，大文件发送的 CPU 与内存开销都低于流式分块。</para>
    /// <para><b>背压</b>：等待可写期间不占线程；慢速对端由内核发送缓冲形成背压，应用层无积压。</para>
    /// <para>与 <see cref="SessionBase.Send(IPacket)"/> 共用同一把写锁：并发调用不会交错。但两者是<b>各自独立加锁</b>，
    /// “响应头 + 文件体”先发头部再调用本方法只在单写者场景下保持一条逻辑消息；多写者并发时头体之间可能被插入其它数据，
    /// 需要整条消息原子时改用 <see cref="SendMessageLockedAsync"/>。</para>
    /// <para><b>打开时序</b>：本方法在取写锁之前先确保会话已打开（与 <see cref="SendAsync(Stream, Int64, CancellationToken)"/> 同构）。并发首访同一未打开会话时，
    /// 打开流程（含 SSL 握手）可能被两个调用者同时进入；写锁只串行化“写套接字”，不覆盖打开流程，调用方应在发送前先完成打开。</para>
    /// <para>SSL 会话（<c>SslStream</c>）与无零拷贝发送 API 的目标框架降级为 <see cref="SendAsync(Stream, Int64, CancellationToken)"/> 的分块读取发送，语义一致但多一次读块搬运。</para>
    /// <para>不支持偏移与长度：需要发送文件的某一段（如 HTTP Range）时，用 <see cref="SendAsync(Stream, Int64, CancellationToken)"/> 定位后分块发送。</para>
    /// </remarks>
    /// <param name="filePath">文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已发送字节数；发送失败返回 -1（已上报并按失败关闭会话）</returns>
    /// <exception cref="ArgumentNullException">路径为空</exception>
    /// <exception cref="FileNotFoundException">文件不存在</exception>
    /// <exception cref="InvalidOperationException">会话未打开</exception>
    public async ValueTask<Int64> SendFileAsync(String filePath, CancellationToken cancellationToken = default)
    {
        if (filePath.IsNullOrEmpty()) throw new ArgumentNullException(nameof(filePath));
        if (Disposed) throw new ObjectDisposedException(GetType().Name);

        var fi = filePath.AsFile();
        if (!fi.Exists) throw new FileNotFoundException($"File not found: {filePath}", filePath);

        if (!Open()) throw new InvalidOperationException($"Session [{GetType().Name}] is not open.");

        var length = fi.Length;

        Exception? error = null;
        var total = -1L;

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
#if NET6_0_OR_GREATER
            // 非 SSL 会话：整个文件交给内核零拷贝推送，等待可写期间不占线程
            if (_Stream == null)
            {
                var sock = Client;
                if (sock == null) return -1;

                try
                {
                    // 内核 SendFileAsync(string) 无返回值（推完整个文件即成功），以文件长度作为发送量
                    await sock.SendFileAsync(filePath, cancellationToken).ConfigureAwait(false);

                    LastTime = DateTime.Now;

                    return length;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // 调用方主动取消：不是发送故障，向上抛出
                    throw;
                }
                catch (Exception ex) when (ex is NotSupportedException or FileNotFoundException)
                {
                    // 只有「根本没发出任何数据」的失败才能安全降级：平台/套接字类型不支持零拷贝、或路径在存在性检查后被删除。
                    // 网络中断、对端复位一类失败可能已送出部分文件，重发会让对端收到重复内容，必须交由外层按发送失败处理
                    WriteLog("内核零拷贝不可用，降级为分块发送 {0}：{1}", filePath, ex.Message);
                }
            }
#endif
            // SSL 会话或无零拷贝发送 API 的框架：分块读取发送（语义相同，多一次读块搬运）。
            // 显式启用异步 IO 与顺序扫描：默认 OpenRead 得到的 FileStream 未开异步，ReadAsync 会同步阻塞线程
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);

            total = await SendStreamCoreAsync(fs, length, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 调用方主动取消：不是连接故障，向上抛出（finally 会释放写锁）
            throw;
        }
        catch (InvalidDataException)
        {
            // 流提前结束属调用方数据问题，不是发送故障：不上报、不关会话
            throw;
        }
        catch (Exception ex)
        {
            error = ex;
        }
        finally
        {
            _writeLock.Release();
        }

        // 上报与关闭放锁外：OnError/Close 会触发用户事件回调，回调内再次 Send 会在同一把写锁上二次等待而死锁
        if (error != null)
        {
            ReportSendError(error, filePath);

            return -1;
        }

        return total;
    }

    /// <summary>锁内发送「头部 + 流内容」整条消息：一次加锁贯穿，头体之间不会被其它写入者插入</summary>
    /// <remarks>
    /// <para>与分别调用 <see cref="SessionBase.Send(IPacket)"/> 与 <see cref="SendAsync(Stream, Int64, CancellationToken)"/> 的区别：那两条各自加锁，
    /// 多写者并发时头体之间可能被插入其它数据；本方法一次持锁贯穿整条消息，保持“一条逻辑消息”语义。</para>
    /// <para>头部句柄借用（调用方释放）；失败上报在释放写锁之后进行。</para>
    /// </remarks>
    /// <param name="header">协议头部数据包。借用语义：调用方保留句柄，用完自行释放</param>
    /// <param name="body">消息体数据流</param>
    /// <param name="length">消息体字节数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已写出的流内容字节数；失败返回 -1（已上报）</returns>
    internal async ValueTask<Int64> SendMessageLockedAsync(IPacket header, Stream body, Int64 length, CancellationToken cancellationToken = default)
    {
        if (header == null) throw new ArgumentNullException(nameof(header));
        if (body == null) throw new ArgumentNullException(nameof(body));

        Exception? error = null;
        var total = -1L;

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Log != null && Log.Enable && LogSend) WriteLog("Send [{0}]: {1}", header.Total, header.ToHex(LogDataLength));

            // 头部先行：与流内容在同一把写锁内，整条消息不会与其它写入者交错
            WritePacket(header);

            total = await SendStreamCoreAsync(body, length, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 调用方主动取消：不是发送故障，不上报、不关会话（finally 会释放写锁）
            throw;
        }
        catch (InvalidDataException)
        {
            // 流提前结束属调用方数据问题，不是发送故障：不上报、不关会话
            throw;
        }
        catch (Exception ex)
        {
            error = ex;
        }
        finally
        {
            _writeLock.Release();
        }

        // 上报与关闭放锁外：OnError/Close 会触发用户事件回调，回调内再次 Send 会在同一把写锁上二次等待而死锁
        if (error != null)
        {
            ReportSendError(error, null);

            return -1;
        }

        return total;
    }
    #endregion

    #region 异步发送
    /// <summary>异步发送数据包：写完才返回，等待可写期间不占线程（与同步 Send 共用写锁）</summary>
    /// <remarks>
    /// <para>与 <see cref="SessionBase.Send(IPacket)"/> 同义，只是异步执行：不排队、不拷贝，失败记录错误并关闭会话（返回 -1）。</para>
    /// <para>借用语义：调用方保留句柄，返回后即可释放自己的引用</para>
    /// </remarks>
    /// <param name="data">数据包</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已发送字节数；失败返回 -1</returns>
    public ValueTask<Int32> SendAsync(IPacket data, CancellationToken cancellationToken = default)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (Disposed) throw new ObjectDisposedException(GetType().Name);
        if (!Open()) throw new InvalidOperationException($"Session [{GetType().Name}] is not open.");

        return DirectSendAsync(data, cancellationToken);
    }
    #endregion
}
