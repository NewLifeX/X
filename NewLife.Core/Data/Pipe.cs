namespace NewLife.Data;

/// <summary>数据包管道。连接接收方与消费方的字节流缓冲，写侧追加数据、读侧按序列消费</summary>
/// <remarks>
/// <para><b>对齐 BCL</b>：命名与形态对齐 System.IO.Pipelines 的 <c>Pipe</c>（同名不同命名空间；两者需同时引用时用别名，如 <c>using NlPipe = NewLife.Data.Pipe;</c>）。本类承载“管道级”关注点——双端句柄装配、共享同步锁与关闭状态、水位背压与恢复事件；读写行为分别在 <see cref="PipeReader"/> 与 <see cref="PipeWriter"/> 上实现。自研动机：BCL 的 System.IO.Pipelines 最低 netstandard2.0（net45 无法引用），本库需要同一套管道形态覆盖含 net45 在内的全部目标框架。</para>
/// <para><b>模型</b>：未消费数据以段链持有在 <see cref="Reader"/>——每段同时承载序列内存（对外暴露 <see cref="System.Buffers.ReadOnlySequence{T}"/> 零拷贝窗口）与拥有句柄（消费推进时同步归还）；任意时刻追加数据即可唤醒挂起的读取。</para>
/// <para><b>背压</b>：未消费数据达到 <see cref="PauseThreshold"/> 时 <see cref="IsPaused"/> 为 true，接收方应暂停继续接收；写侧提交（<see cref="PipeWriter.FlushAsync(CancellationToken)"/> 达到暂停水位即挂起，对齐 BCL）亦可在此挂起等待，形成双向背压。消费推进到 <see cref="ResumeThreshold"/> 以下时触发 <see cref="Resumed"/> 并唤醒挂起提交，恢复接收。缓冲有界，消费者不取数则接收方停止拉动（TCP 窗口自然回压）。背压近乎零开销：未触发暂停时的提交快路径基准实测 28ns、零分配——只有真正积压到暂停水位才付出挂起/唤醒。</para>
/// <para><b>线程模型</b>：单写（<see cref="Writer"/> 的方法）单读（<see cref="Reader"/> 的方法），读写可来自不同线程。</para>
/// <para><b>所有权</b>：<see cref="PipeWriter.Append(IPacket)"/> 无条件接管入参句柄（管道已关闭时由管道负责释放）；消费方读取的序列仅在对应数据被消费前有效。</para>
/// <para><b>与 BCL 的差异</b>：消费推进以字节计数 <see cref="PipeReader.AdvanceTo(Int64)"/> 为主——追加数据会重建窗口序列，位置在跨追加场景不稳定；字节计数对跨段、跨轮场景简单可靠，且可在全部目标框架实现（含 net45），另提供 SequencePosition 重载（取自最近一次读取窗口，形态对齐）。<see cref="PipeWriter.Append(IPacket)"/>、<see cref="PipeReader.TakeFrame(Int64)"/>、<see cref="PipeReader.Limit(Int64)"/> 与字节计数推进为本库扩展，差异清单见《数据管道Pipe》。</para>
/// </remarks>
/// <example>
/// <code>
/// var pipe = new Pipe();
/// pipe.Writer.Append(received);             // 接收线程投递（所有权转移）
/// var rr = await pipe.Reader.ReadAsync();   // 消费线程读取
/// if (!rr.Buffer.IsEmpty) { /* 解析窗口 */ pipe.Reader.AdvanceTo(consumedBytes); }
/// pipe.Writer.Complete();                   // 写入结束（唤醒挂起读）
/// </code>
/// </example>
public sealed class Pipe : IDisposable
{
    #region 属性
    /// <summary>读侧句柄。消费方从这里读取数据（对标 BCL 的 PipeReader）</summary>
    public PipeReader Reader { get; }

    /// <summary>写侧句柄。生产方从这里写入数据（对标 BCL 的 PipeWriter）</summary>
    public PipeWriter Writer { get; }

    /// <summary>暂停水位（字节）。未检查数据达到该值时 <see cref="IsPaused"/> 为 true，接收方应暂停接收、写侧提交可挂起等待；0或负数不启用背压。默认1M</summary>
    /// <remarks>记账量为“未检查数据 = <see cref="UnconsumedLength"/> − 已检查字节数”，口径对齐 System.IO.Pipelines：
    /// 读侧 <see cref="PipeReader.AdvanceTo(Int64, Int64)"/> 声明为已检查的字节即解除计入，即使尚未消费——背压约束的是消费者还没看过的积压。
    /// 单参 <see cref="PipeReader.AdvanceTo(Int64)"/> 等价 examined=consumed，故常规消费推进的暂停/恢复行为与“按未消费记账”完全一致。</remarks>
    public Int64 PauseThreshold { get; set; } = 1024 * 1024;

    /// <summary>恢复水位（字节）。未检查数据降到该值以下时解除暂停并触发 <see cref="Resumed"/>。默认512K</summary>
    /// <remarks>记账口径见 <see cref="PauseThreshold"/>。</remarks>
    public Int64 ResumeThreshold { get; set; } = 512 * 1024;

    /// <summary>未消费数据长度。来自读侧段链</summary>
    public Int64 UnconsumedLength => Reader.UnconsumedLength;

    /// <summary>是否已达暂停水位。达到后保持，直到消费降到恢复水位以下才解除</summary>
    public Boolean IsPaused => PauseThreshold > 0 && _paused;

    /// <summary>写侧是否已完成</summary>
    public Boolean IsCompleted => WriterCompleted;

    /// <summary>完成时的异常。读写两侧的 Complete(error) 均可携带，先到先得（已有异常时不被空值覆盖）</summary>
    public Exception? Error { get; internal set; }

    /// <summary>消费推进使未消费数据降到恢复水位以下、或读侧挂起等待触发饥饿让位时触发，接收方可恢复接收</summary>
    public event EventHandler? Resumed;
    #endregion

    #region 共享状态（读写两侧共用；状态写入一律在 SyncRoot 保护下）

    /// <summary>共享同步锁。读写两侧的状态写入共用这一把锁</summary>
    internal readonly Object SyncRoot = new();

    /// <summary>写侧是否已完成。写入在 SyncRoot 保护下；volatile 保证无锁快照读（IsCompleted）的跨线程可见性</summary>
    internal volatile Boolean WriterCompleted;

    /// <summary>读侧是否已结束。写入在 SyncRoot 保护下</summary>
    internal volatile Boolean ReaderCompleted;

    /// <summary>暂停态。达到暂停水位后置位，消费降到恢复水位以下才解除（迟滞）；写入在 SyncRoot 保护下，volatile 保证无锁快照读（IsPaused）的跨线程可见性</summary>
    private volatile Boolean _paused;
    #endregion

    #region 构造
    /// <summary>创建数据包管道</summary>
    public Pipe()
    {
        Reader = new PipeReader(this);
        Writer = new PipeWriter(this);
    }
    #endregion

    #region 方法
    /// <summary>释放管道。等价于完成写入并结束读取，全部未消费数据归还内存池</summary>
    public void Dispose()
    {
        Writer.Complete();
        Reader.Complete();
    }

    /// <summary>复位管道，供对象复用。要求读写两端均已 <see cref="PipeWriter.Complete(Exception?)"/></summary>
    /// <remarks>对齐 System.IO.Pipelines 的 Pipe.Reset；复位后读写句柄保持有效、完成状态与水位清零、错误清除。</remarks>
    /// <exception cref="InvalidOperationException">读侧或写侧尚未完成</exception>
    public void Reset()
    {
        lock (SyncRoot)
        {
            if (!WriterCompleted || !ReaderCompleted) throw new InvalidOperationException("Pipe must be completed on both sides before reset.");

            WriterCompleted = false;
            ReaderCompleted = false;
            _paused = false;
            Error = null;

            Reader.ResetForReuse();
        }
    }

    /// <summary>刷新暂停状态（调用方持锁）。返回本次是否需要触发恢复事件</summary>
    /// <param name="pending">当前未检查数据长度（未消费 − 已检查）</param>
    /// <remarks>迟滞：达到暂停水位转入暂停态后保持，直到降回恢复水位以下才解除。
    /// 不能按瞬时长度判断——多次小步消费时，跨过恢复水位的那一次推进的起始长度已低于暂停水位，瞬时判断会漏报恢复。</remarks>
    internal Boolean UpdatePauseLocked(Int64 pending)
    {
        if (PauseThreshold <= 0)
        {
            _paused = false;
            return false;
        }

        if (_paused)
        {
            // 已暂停：降到恢复水位以下时解除并报告
            if (pending < ResumeThreshold)
            {
                _paused = false;
                return true;
            }
        }
        else if (pending >= PauseThreshold)
        {
            _paused = true;
        }

        return false;
    }

    /// <summary>重置暂停态（调用方持锁）。读侧结束时调用，不再触发恢复事件</summary>
    internal void ResetPauseLocked() => _paused = false;

    /// <summary>读饥饿让位（调用方持锁）。返回本次是否需要触发恢复事件</summary>
    /// <remarks>读侧在“无新数据可交付”时才会挂起等待：此时不会再有消费、暂停已不可能按常规路径（消费降压）解除，
    /// 若继续持有，接收方将停摆，读者永远等不到后续字节（整帧/最小长度读取死锁）。故挂起前解除暂停，由调用方触发 <see cref="Resumed"/> 放行接收。
    /// 按“未检查”记账后，读者进入等待时未检查量必然不大于 0、暂停已自行解除，这里作为兜底保留：
    /// 运行期调低 <see cref="PauseThreshold"/> 等配置使暂停在读者等待期间重新成立时，仍据此放行接收。</remarks>
    internal Boolean ReleasePauseForReaderLocked()
    {
        if (!_paused) return false;

        _paused = false;

        return true;
    }

    /// <summary>触发恢复（锁外调用）：唤醒挂起的写侧提交并触发恢复事件，避免用户代码进入锁内</summary>
    internal void RaiseResumed()
    {
        TaskCompletionSource<FlushResult>? waiter;
        CancellationTokenRegistration reg;
        lock (SyncRoot)
        {
            waiter = Writer.TakeFlushWaiterLocked(out reg);
        }

        PipeWriter.NotifyFlushWaiter(waiter, reg, new FlushResult(IsCompleted, false));

        Resumed?.Invoke(this, EventArgs.Empty);
    }
    #endregion
}
