using System.Net.Sockets;
using NewLife.Data;

namespace NewLife.Net;

/// <summary>TCP 会话的流式收发：入站字节流管道（含背压暂停接收）与出站单出口发送队列（发送泵）</summary>
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
    /// <summary>数据发送管道（出站）。按需创建：首次访问后，Send 系列方法把数据追加进管道，由发送泵统一顺序送出</summary>
    /// <remarks>
    /// <para><b>单出口</b>：管道创建后 <see cref="SessionBase.Send(IPacket)"/> 等发送方法全部改为追加进管道（字节数组/跨度输入按副本，借阅视图自动转自有拷贝），与管道内排队数据天然无交错；发送在发送泵上异步完成。</para>
    /// <para><b>背压</b>：未发送数据达到 <see cref="Pipe.PauseThreshold"/> 后 <see cref="Pipe.IsPaused"/> 为 true，生产方的 <see cref="PipeWriter.FlushAsync(CancellationToken)"/> 默认挂起等待（对齐 BCL）；泵推进降到 <see cref="Pipe.ResumeThreshold"/> 以下时唤醒。水位感知发送可用 <see cref="TrySend(IPacket)"/>（暂停时拒绝）或 <see cref="SendAsync(IPacket, CancellationToken)"/>（挂起等待）。</para>
    /// <para><b>生命周期</b>：关闭时完成写入并限时等待泵发完已排队数据；发送失败中止管道（错误随 <see cref="Pipe.Error"/>，后续追加的数据由管道直接释放）。</para>
    /// <para>读侧 <see cref="Pipe.Reader"/> 为发送泵独占，请勿另作它用。</para>
    /// </remarks>
    public Pipe SendPipe => GetOrCreatePump().Pipe;

    private readonly Object _sendPumpLock = new();

    /// <summary>发送泵。双检锁创建；volatile 保证无锁快照读（Send/GetSendPipe）的跨线程可见性</summary>
    private volatile SendPump? _sendPump;

    /// <summary>获取已创建的数据发送管道。未访问过 <see cref="SendPipe"/> 时返回 null（不触发创建）</summary>
    /// <returns>数据发送管道；未创建时为 null</returns>
    public Pipe? GetSendPipe() => _sendPump?.Pipe;

    /// <summary>创建数据发送管道。子类可重写以自定义水位等参数</summary>
    /// <returns>数据发送管道</returns>
    /// <remarks>出站水位默认 64K 暂停 / 32K 恢复：对齐 BCL 管道默认与 Kestrel 出站阻塞阈值（MaxResponseBufferSize=64KB），慢速对端下每连接最坏排队内存为入站侧（1M）的十六分之一；各档位吞吐实测无差异（见《背压水位定档与内存测算报告》）。入站方向保持 1M/512K（Kestrel 入站同量级的内存安全阀；读饥饿时自动让位，不束缚帧尺寸）。</remarks>
    protected virtual Pipe CreateSendPipe() => new()
    {
        PauseThreshold = 64 * 1024,
        ResumeThreshold = 32 * 1024,
    };

    /// <summary>获取或创建发送泵（管道唯一消费方，构造时启动）</summary>
    /// <returns>发送泵</returns>
    private SendPump GetOrCreatePump()
    {
        var pump = _sendPump;
        if (pump != null) return pump;

        lock (_sendPumpLock)
        {
            if (_sendPump == null)
            {
                var created = CreateSendPipe();

                // 发送泵（管道唯一消费方），构造时启动；发送委托指向泵专用发送核心，避免再次进入队列分流
                _sendPump = new SendPump(created, DirectSendAsync, OnError, WriteLog);
            }

            return _sendPump;
        }
    }
    #endregion

    #region 发送核心
    /// <summary>发送核心：发送管道创建后队列优先（单出口），否则直发</summary>
    /// <param name="data">数据包</param>
    /// <returns>已发送或已接收字节数</returns>
    protected override Int32 OnSend(IPacket data)
    {
        var pump = _sendPump;
        if (pump != null) return pump.Append(data);

        return DirectSend(data);
    }

    /// <summary>发送核心：发送管道创建后队列优先（单出口），否则直发</summary>
    /// <param name="data">数据包</param>
    /// <returns>已发送或已接收字节数</returns>
    protected override Int32 OnSend(ArraySegment<Byte> data)
    {
        var pump = _sendPump;
        if (pump != null && data.Array != null && data.Count > 0)
            return pump.Append(new ReadOnlySpan<Byte>(data.Array, data.Offset, data.Count));

        return DirectSend(data);
    }

    /// <summary>发送核心：发送管道创建后队列优先（单出口），否则直发</summary>
    /// <param name="data">数据包</param>
    /// <returns>已发送或已接收字节数</returns>
    protected override Int32 OnSend(ReadOnlySpan<Byte> data)
    {
        var pump = _sendPump;
        if (pump != null && !data.IsEmpty) return pump.Append(data);

        return DirectSend(data);
    }
    #endregion

    #region 关闭收尾
    /// <summary>关闭。重写以收尾流式收发：有连接先排空发送队列，完成后终止入站管道（挂起读立即完成）</summary>
    /// <remarks>发送队列限时（会话超时）等待泵发完已排队数据；无连接时直接中止队列（幂等）</remarks>
    /// <param name="reason">关闭原因。便于日志分析</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>是否成功</returns>
    public override async Task<Boolean> CloseAsync(String reason, CancellationToken cancellationToken = default)
    {
        // 出站队列：有连接时限时等待发送泵发完已排队数据；无连接直接中止（幂等）
        var pump = _sendPump;
        if (pump != null)
        {
            // 批量停机（服务端 Stop/Dispose）跳过排空：逐会话限时等待会让停机时间随会话数线性放大
            if (Active && !FastCloseOnShutdown) await pump.FlushAsync(Timeout > 0 ? Timeout : 3_000).ConfigureAwait(false);
            else pump.Abort(null);
        }

        try
        {
            return await base.CloseAsync(reason, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // 入站流：完成写侧，挂起读立即完成；释放暂存的接收，避免泄漏
            _pipe?.Writer.Complete();
            ReleaseParkedReceive();
        }
    }
    #endregion

    #region 流式发送
    /// <summary>流式发送。从数据流分块读入发送管道，由发送泵顺序送出</summary>
    /// <remarks>
    /// <para>形态对齐主流网络框架的 SendAsync(Stream)：分块读取（默认 64KB），每块零拷贝包装入 <see cref="SendPipe"/>，大文件全程只在读块上驻留，不产生整段内存。回环基准实测：1MB 用时 510µs、8MB 4.08ms，与手工 64KB 分块持平（管道自身开销约 1~2%），16KB→64KB 分块优化带来 2.3× 提升。</para>
    /// <para>写侧回压：管道未发送数据达到 <see cref="Pipe.PauseThreshold"/> 时挂起等待网络消化，内存占用有界，慢速对端不会导致应用层无限积压。</para>
    /// <para>发送与 <see cref="SessionBase.Send(IPacket)"/> 共用 <see cref="SendPipe"/> 单出口（无交错）；"头 + 流式体"组合消息先发头部数据包再调用本方法即可，整条消息保持一条逻辑消息语义。</para>
    /// <para>流提前结束（不足 <paramref name="length"/>）或发送管道中途中止（连接故障/关闭）时抛出异常，已入管道部分仍会尽力送出。</para>
    /// </remarks>
    /// <param name="source">数据流</param>
    /// <param name="length">期望发送的字节数；负数表示读到流尾</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已写入发送管道的总字节数</returns>
    public ValueTask<Int64> SendAsync(Stream source, Int64 length = -1, CancellationToken cancellationToken = default)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (Disposed) throw new ObjectDisposedException(GetType().Name);
        if (!Open()) throw new InvalidOperationException($"Session [{GetType().Name}] is not open.");

        return GetOrCreatePump().SendAsync(source, length, cancellationToken);
    }
    #endregion

    #region 背压发送
    /// <summary>异步发送数据包。入队后等待积压降到恢复水位以下，等待期间不占用线程</summary>
    /// <remarks>
    /// <para>与 <see cref="SendAsync(Stream, Int64, CancellationToken)"/> 共用发送管道单出口；积压达到 <see cref="Pipe.PauseThreshold"/> 时挂起，降至 <see cref="Pipe.ResumeThreshold"/> 以下恢复。</para>
    /// <para>管道已中止时抛出异常（错误随 <see cref="Pipe.Error"/>）。发送管道为单写者：请勿与本方法、<see cref="PipeWriter.FlushAsync(CancellationToken)"/> 并发提交。</para>
    /// </remarks>
    /// <param name="data">数据包。拥有句柄零拷贝入管道；借阅视图自动转自有拷贝</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已入队字节数；管道已中止返回 -1</returns>
    public async ValueTask<Int32> SendAsync(IPacket data, CancellationToken cancellationToken = default)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (Disposed) throw new ObjectDisposedException(GetType().Name);
        if (!Open()) throw new InvalidOperationException($"Session [{GetType().Name}] is not open.");

        var pump = GetOrCreatePump();
        var rs = pump.Append(data);
        if (rs < 0) return rs;

        // 背压等待：积压达到暂停水位时挂起，网络消化到恢复水位后继续
        var flush = await pump.Pipe.Writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        if (flush.IsCompleted) throw new InvalidOperationException("Send pipe has been completed.", pump.Pipe.Error);

        return rs;
    }

    /// <summary>尝试非阻塞发送数据包。管道积压达到暂停水位时拒绝，交由调用方决定丢弃或稍后重试</summary>
    /// <remarks>不主动打开连接：非活动会话直接拒绝。首次调用会创建发送管道，此后该会话的发送统一经泵送出（单出口）</remarks>
    /// <param name="data">数据包。拥有句柄零拷贝入管道；借阅视图自动转自有拷贝</param>
    /// <returns>是否成功入队；暂停或管道已中止返回 false</returns>
    public Boolean TrySend(IPacket data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (Disposed) throw new ObjectDisposedException(GetType().Name);

        // 不主动打开连接：“尝试发送”不应发生阻塞式连接
        if (!Active) return false;

        var pump = GetOrCreatePump();
        if (pump.Pipe.IsPaused) return false;

        return pump.Append(data) >= 0;
    }
    #endregion
}
