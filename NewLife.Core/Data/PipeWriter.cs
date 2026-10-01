using System.Buffers;

namespace NewLife.Data;

/// <summary>数据包管道写侧句柄。向管道写入数据</summary>
/// <remarks>
/// <para><b>对齐 BCL</b>：命名与形态对齐 System.IO.Pipelines 的 <c>PipeWriter</c>（同名不同命名空间；两者需同时引用时用别名，如 <c>using NlPipeWriter = NewLife.Data.PipeWriter;</c>）。BCL 为抽象类 + 内部实现，本库为具体类；实现 <see cref="IBufferWriter{T}"/> 全部成员，提交结果 <see cref="FlushResult"/> 对应 BCL 的 FlushResult。</para>
/// <para><b>职责</b>：写缓冲（GetSpan/GetMemory 租借、Advance 推进、WriteAsync/FlushAsync 提交）由本类持有；包级 <see cref="Append(IPacket)"/> 直接投递到读侧未消费窗口（所有权转移、零拷贝），跨度写入则先进入池化写缓冲、<see cref="WriteAsync(ReadOnlyMemory{Byte}, CancellationToken)"/><see cref="FlushAsync(CancellationToken)"/> 之前对读取方不可见。</para>
/// <para>完成语义：<see cref="Complete"/> 会先把已推进未提交的数据提交给读侧再结束（对齐 BCL）；未推进的写入缓冲在 <see cref="Complete"/> 或管道释放时丢弃（池缓冲归还）。读侧结束后仍可取窗口写入，由提交结果告知停止；写侧自己结束后再写抛异常。单写：写侧操作同一时刻只允许一个线程。</para>
/// </remarks>
public sealed class PipeWriter : IBufferWriter<Byte>
{
    #region 属性
    /// <summary>尚未提交（未 Flush）的字节数</summary>
    public Int64 UnflushedBytes { get { lock (_pipe.SyncRoot) return _writeSealed + _writeLength; } }
    #endregion

    #region 内部状态
    private readonly Pipe _pipe;

    /// <summary>写入缓冲（ArrayPool 租借；GetSpan/GetMemory 跨度写入路径）</summary>
    private Byte[]? _writeBuffer;

    /// <summary>写入缓冲已推进长度</summary>
    private Int32 _writeLength;

    /// <summary>最近一次返回窗口的剩余可推进量</summary>
    private Int32 _writeAllowance;

    /// <summary>已封口待提交的写段链</summary>
    private IPacket? _writeHead;

    /// <summary>待提交写段链尾</summary>
    private IPacket? _writeTail;

    /// <summary>已封口待提交字节数</summary>
    private Int64 _writeSealed;

    /// <summary>默认写入段大小</summary>
    private const Int32 DefaultWriteSize = 4096;

    /// <summary>挂起的提交（写侧回压）。单写：至多一个</summary>
    private TaskCompletionSource<FlushResult>? _flushWaiter;

    /// <summary>挂起提交的取消注册，完成时释放避免长期令牌累积</summary>
    private CancellationTokenRegistration _flushReg;

    /// <summary>取消暂存：无挂起提交时调用 CancelPendingFlush，由下一次提交消费（对齐 BCL，只生效一次）</summary>
    private Boolean _cancelPendingFlush;
    #endregion

    #region 构造
    internal PipeWriter(Pipe pipe) => _pipe = pipe;
    #endregion

    #region 方法
    /// <summary>追加数据（所有权转移：无条件接管入参句柄；管道已关闭时由管道释放）</summary>
    /// <param name="pk">追加的数据包（单段或链式）</param>
    /// <remarks>追加后数据进入未消费窗口，并立即唤醒挂起的读取；调用后不得再使用或释放入参句柄，由管道在消费后归还。</remarks>
    public void Append(IPacket pk) => _pipe.Reader.AppendInternal(pk);

    /// <summary>获取可写入内存窗口（至少 sizeHint 字节）。推进用 <see cref="Advance"/>，提交用 <see cref="FlushAsync(CancellationToken)"/></summary>
    /// <param name="sizeHint">期望最小字节数，0 表示任意（至少 1）</param>
    /// <returns>可写入内存；FlushAsync 之前对读取方不可见</returns>
    /// <exception cref="InvalidOperationException">写侧已结束</exception>
    /// <remarks>读侧已结束时不再抛异常（对齐 BCL）：仍需调用 <see cref="FlushAsync(CancellationToken)"/> 取回 <c>IsCompleted</c> 以停止写入。</remarks>
    public Memory<Byte> GetMemory(Int32 sizeHint = 0)
    {
        if (sizeHint < 0) throw new ArgumentOutOfRangeException(nameof(sizeHint));

        lock (_pipe.SyncRoot)
        {
            // 只在“写侧自己已结束且读侧仍在”时拒绝：读侧结束（对端关闭／接收侧收尾）后按 BCL 习惯仍允许取窗口，
            // 数据在提交时直接释放，调用方据 FlushAsync 返回的 IsCompleted 停止写入。
            // 读侧结束后 ReaderCompleted 为真（它同时把 WriterCompleted 也置真），故不能用 WriterCompleted 单独判读侧状态
            if (_pipe.WriterCompleted && !_pipe.ReaderCompleted) throw new InvalidOperationException("Pipe has been completed.");

            EnsureWriteSpaceLocked(sizeHint);

            _writeAllowance = _writeBuffer!.Length - _writeLength;

            return _writeBuffer.AsMemory(_writeLength);
        }
    }

    /// <summary>获取可写入跨度窗口（至少 sizeHint 字节）</summary>
    /// <param name="sizeHint">期望最小字节数，0 表示任意</param>
    /// <returns>可写入跨度；FlushAsync 之前对读取方不可见</returns>
    public Span<Byte> GetSpan(Int32 sizeHint = 0) => GetMemory(sizeHint).Span;

    /// <summary>推进写入位置。累计推进不得超过最近一次 GetMemory/GetSpan 返回窗口的长度</summary>
    /// <param name="count">已写入字节数</param>
    /// <exception cref="ArgumentOutOfRangeException">超过最近一次返回窗口的长度</exception>
    public void Advance(Int32 count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (count == 0) return;

        lock (_pipe.SyncRoot)
        {
            if (_writeBuffer == null || count > _writeAllowance || _writeLength + count > _writeBuffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count), "Advance exceeds the memory returned by the last GetMemory/GetSpan call.");

            _writeLength += count;
            _writeAllowance -= count;
        }
    }

    /// <summary>写入并提交数据。数据复制进管道缓冲，提交后对读取方可见</summary>
    /// <param name="data">待写入数据</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>提交结果；管道已结束时 IsCompleted 为 true（写入方应停止）</returns>
    /// <remarks>形态对齐 System.IO.Pipelines 的 PipeWriter.WriteAsync：写入即提交（等价 GetSpan + 拷贝 + Advance + FlushAsync 的组合）；已提交数据来自管道缓冲副本，调用方可立即复用入参缓冲。写侧自己结束时抛 <see cref="InvalidOperationException"/>（对齐 BCL）；仅读侧结束时不再抛，由返回的 <c>IsCompleted</c> 告知停止写入。</remarks>
    public ValueTask<FlushResult> WriteAsync(ReadOnlyMemory<Byte> data, CancellationToken cancellationToken = default)
    {
        if (!data.IsEmpty)
        {
            var span = GetSpan(data.Length);
            data.Span.CopyTo(span);
            Advance(data.Length);
        }

        return FlushAsync(cancellationToken);
    }

    /// <summary>提交写入。已推进数据进入未消费窗口并唤醒挂起读取；达到暂停水位时挂起等待消费恢复（写侧回压），语义对齐 BCL PipeWriter.FlushAsync</summary>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>提交结果；管道已结束时 IsCompleted 为 true（写入方应停止）</returns>
    /// <exception cref="OperationCanceledException">取消令牌已请求取消，或在挂起期间被取消</exception>
    /// <remarks>写侧回压：未消费数据达到 <see cref="Pipe.PauseThreshold"/> 时提交挂起，直到消费降至 <see cref="Pipe.ResumeThreshold"/> 以下、管道结束或取消。不需要等待水位恢复时使用 <see cref="FlushAsync(Boolean, CancellationToken)"/>(false, ...)。成本（基准实测）：未处于暂停态时提交约 28ns、零分配——背压是零成本保险，只有真正积压才付出挂起/唤醒。</remarks>
    public ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default) => FlushAsync(true, cancellationToken);

    /// <summary>提交写入（可选写侧回压）。waitForResume 为 true 且读侧未消费量达到暂停水位时挂起，直到消费降至恢复水位以下、管道结束或取消；false 仅提交不等待</summary>
    /// <param name="waitForResume">是否等待水位恢复。true 时提交在管道暂停期间挂起；当前无出站调用方，属预留能力（见《数据管道Pipe》出站一节）</param>
    /// <param name="cancellationToken">取消通知。挂起期间触发则任务以取消结束</param>
    /// <returns>提交结果；管道已结束时 IsCompleted 为 true；被 <see cref="CancelPendingFlush"/> 取消时 IsCanceled 为 true</returns>
    /// <exception cref="OperationCanceledException">取消令牌已请求取消，或在挂起期间被取消</exception>
    /// <exception cref="InvalidOperationException">已有挂起提交（单写：至多一个挂起提交）</exception>
    public ValueTask<FlushResult> FlushAsync(Boolean waitForResume, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IPacket? head;
        Boolean canceled;
        lock (_pipe.SyncRoot)
        {
            // 封口当前缓冲并入待提交链
            SealWriteLocked();

            head = _writeHead;
            _writeHead = null;
            _writeTail = null;
            _writeSealed = 0;

            if (head != null && (_pipe.WriterCompleted || _pipe.ReaderCompleted))
            {
                // 管道已结束：提交的数据无人消费，直接释放
                head.TryDispose();
                head = null;
            }

            // 取消暂存只生效一次：数据照常提交（对齐 BCL），只是不再等待水位
            canceled = _cancelPendingFlush;
            _cancelPendingFlush = false;
        }

        if (head != null) _pipe.Reader.AppendInternal(head);

        // 默认不挂起，或本次提交已被取消：立即完成
        if (!waitForResume || canceled) return new ValueTask<FlushResult>(new FlushResult(_pipe.IsCompleted, canceled));

        TaskCompletionSource<FlushResult> tcs;
        lock (_pipe.SyncRoot)
        {
            // 未处于暂停态或管道已结束：无需要等待的水位恢复，立即完成
            if (!_pipe.IsPaused || _pipe.WriterCompleted || _pipe.ReaderCompleted)
                return new ValueTask<FlushResult>(new FlushResult(_pipe.IsCompleted, false));

            var waiting = _flushWaiter;
            if (waiting != null)
            {
                // 挂起已被取消定案（TrySetCanceled）但尚未取走：清理残留挂起，允许继续使用
                if (!waiting.Task.IsCompleted) throw new InvalidOperationException("Existing pending flush; Pipe supports single writer only.");

                _flushReg.Dispose();
                _flushReg = default;
                _flushWaiter = null;
            }

#if NET45
            // net45 没有 RunContinuationsAsynchronously，接受同步续体
            tcs = new TaskCompletionSource<FlushResult>();
#else
            tcs = new TaskCompletionSource<FlushResult>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
            _flushWaiter = tcs;

            if (cancellationToken.CanBeCanceled)
                _flushReg = cancellationToken.Register(static state => ((TaskCompletionSource<FlushResult>)state!).TrySetCanceled(), tcs);
        }

        return new ValueTask<FlushResult>(tcs.Task);
    }

    /// <summary>取消挂起的提交。有挂起提交时立即以 IsCanceled 唤醒；无挂起提交时暂存，由下一次提交标记取消一次（数据照常提交）</summary>
    public void CancelPendingFlush()
    {
        TaskCompletionSource<FlushResult>? waiter;
        CancellationTokenRegistration reg;

        lock (_pipe.SyncRoot)
        {
            waiter = TakeFlushWaiterLocked(out reg);

            // 无挂起提交：暂存取消，由下一次提交消费（对齐 BCL，只生效一次）
            if (waiter == null) _cancelPendingFlush = true;
        }

        if (waiter == null) return;

        // 挂起可能已被取消令牌抢先定案：取消未交付给任何一方，补置暂存让下一次提交生效
        if (!NotifyFlushWaiter(waiter, reg, new FlushResult(_pipe.IsCompleted, true)))
        {
            lock (_pipe.SyncRoot)
            {
                _cancelPendingFlush = true;
            }
        }
    }

    /// <summary>取出挂起的提交句柄（调用方持锁），供锁外唤醒</summary>
    /// <param name="reg">取消注册，需在锁外释放</param>
    /// <returns>挂起的提交；无挂起时为 null</returns>
    internal TaskCompletionSource<FlushResult>? TakeFlushWaiterLocked(out CancellationTokenRegistration reg)
    {
        var waiter = _flushWaiter;
        reg = _flushReg;
        _flushWaiter = null;
        _flushReg = default;

        return waiter;
    }

    /// <summary>复位写侧状态供管道复用（调用方持锁，复位前管道须已完成）</summary>
    internal void ResetForReuse()
    {
        // 残留的取消暂存会让复用后的第一次提交凭空标记取消，必须清掉
        _cancelPendingFlush = false;
    }

    /// <summary>锁外唤醒：释放取消注册并完成任务</summary>
    /// <param name="waiter">挂起的提交</param>
    /// <param name="reg">取消注册</param>
    /// <param name="result">提交结果</param>
    /// <returns>结果是否真正交付；false 表示等待者已被取消令牌抢先定案</returns>
    internal static Boolean NotifyFlushWaiter(TaskCompletionSource<FlushResult>? waiter, CancellationTokenRegistration reg, FlushResult result)
    {
        reg.Dispose();
        return waiter?.TrySetResult(result) ?? false;
    }

    /// <summary>完成写入。未提交的已推进数据先提交给读侧（对齐 BCL），再唤醒挂起读取与提交；此后追加的数据直接释放</summary>
    /// <param name="error">结束原因（异常）。可为空；首个带异常的完成方胜出（读侧已带异常时不覆盖）</param>
    public void Complete(Exception? error = null)
    {
        IPacket? head;
        TaskCompletionSource<ReadResult>? waiter;
        CancellationTokenRegistration reg;
        ReadResult result = default;
        TaskCompletionSource<FlushResult>? flushWaiter;
        CancellationTokenRegistration flushReg;

        // 先提交后完成：Complete 不得静默丢弃已 Advance 的数据（对齐 BCL）。
        // 投递必须在标记完成之前——否则 AppendInternal 会把数据当“已关闭”直接释放
        lock (_pipe.SyncRoot)
        {
            if (_pipe.WriterCompleted) return;

            SealWriteLocked();
            head = _writeHead;
            _writeHead = null;
            _writeTail = null;
            _writeSealed = 0;

            // 归还写缓冲（已取走的待提交链不在此内）
            ReleaseWriteLocked();
        }

        if (head != null) _pipe.Reader.AppendInternal(head);

        lock (_pipe.SyncRoot)
        {
            if (_pipe.WriterCompleted) return;

            _pipe.WriterCompleted = true;
            if (error != null) _pipe.Error ??= error;

            // 唤醒读侧挂起读取（唤醒句柄由读侧构建）
            waiter = _pipe.Reader.OnWriterCompletedLocked(out reg, out result);

            // 唤醒写侧挂起提交（如关闭路径由其它线程触发）
            flushWaiter = TakeFlushWaiterLocked(out flushReg);
        }

        PipeReader.NotifyWaiter(waiter, reg, result);
        NotifyFlushWaiter(flushWaiter, flushReg, new FlushResult(true, false));
    }

    /// <summary>完成写入（异步形态，与 <see cref="Complete(Exception?)"/> 等价）</summary>
    /// <param name="error">结束原因（异常）。可为空</param>
    public ValueTask CompleteAsync(Exception? error = null)
    {
        Complete(error);
        return default;
    }
    #endregion

    #region 流形态
    /// <summary>以流形态写入管道数据。对齐 System.IO.Pipelines 的 PipeWriter.AsStream</summary>
    /// <param name="leaveOpen">释放流时是否保留写入器；false 则流 Dispose 时完成写入</param>
    /// <returns>只写流；写入映射到 WriteAsync（写入即提交）</returns>
    public Stream AsStream(Boolean leaveOpen = false) => new PipeWriterStream(this, leaveOpen);

    /// <summary>管道写入器流包装。同步方法以阻塞等待实现（与 BCL 同形）</summary>
    private sealed class PipeWriterStream : Stream
    {
        private readonly PipeWriter _writer;
        private readonly Boolean _leaveOpen;

        public PipeWriterStream(PipeWriter writer, Boolean leaveOpen)
        {
            _writer = writer;
            _leaveOpen = leaveOpen;
        }

        public override Boolean CanRead => false;
        public override Boolean CanSeek => false;
        public override Boolean CanWrite => true;
        public override Int64 Length => throw new NotSupportedException();
        public override Int64 Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush() => _writer.FlushAsync().GetAwaiter().GetResult();

        public override Int64 Seek(Int64 offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(Int64 value) => throw new NotSupportedException();

        public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count) => throw new NotSupportedException();

        public override void Write(Byte[] buffer, Int32 offset, Int32 count)
        {
            if (count == 0) return;

            WriteAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
        }

        public override async Task WriteAsync(Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken)
        {
            // 写入即提交；流形态按序多次写入
            await _writer.WriteAsync(new ReadOnlyMemory<Byte>(buffer, offset, count), cancellationToken).ConfigureAwait(false);
        }

        protected override void Dispose(Boolean disposing)
        {
            if (disposing && !_leaveOpen) _writer.Complete();

            base.Dispose(disposing);
        }
    }
    #endregion

    #region 写入缓冲内部
    /// <summary>确保当前写入缓冲有足够空间（调用方持锁）</summary>
    private void EnsureWriteSpaceLocked(Int32 sizeHint)
    {
        var need = sizeHint > 0 ? sizeHint : 1;
        var buffer = _writeBuffer;
        if (buffer != null && buffer.Length - _writeLength >= need) return;

        SealWriteLocked();

        // 未携带数据的小缓冲直接归还，避免泄漏
        if (_writeBuffer != null)
        {
            ArrayPool<Byte>.Shared.Return(_writeBuffer);
            _writeBuffer = null;
        }

        _writeBuffer = ArrayPool<Byte>.Shared.Rent(sizeHint > 0 ? sizeHint : DefaultWriteSize);
    }

    /// <summary>封口当前写入缓冲，挂入待提交链（调用方持锁）</summary>
    private void SealWriteLocked()
    {
        if (_writeBuffer == null || _writeLength <= 0) return;

        var node = new OwnerPacket(_writeBuffer, 0, _writeLength, true);
        if (_writeHead == null)
            _writeHead = node;
        else
            _writeTail!.Next = node;

        _writeTail = node;
        _writeSealed += _writeLength;

        _writeBuffer = null;
        _writeLength = 0;
        _writeAllowance = 0;
    }

    /// <summary>释放写入缓冲与待提交链，未提交数据丢弃（调用方持锁；本侧完成与读侧结束均会触发）</summary>
    /// <remarks>调用前应先用 <see cref="SealWriteLocked"/> 取走已推进数据（若需提交），本方法只负责归还池缓冲与丢弃残余。</remarks>
    internal void ReleaseWriteLocked()
    {
        if (_writeBuffer != null)
        {
            ArrayPool<Byte>.Shared.Return(_writeBuffer);
            _writeBuffer = null;
        }

        _writeLength = 0;
        _writeAllowance = 0;
        _writeSealed = 0;

        var node = _writeHead;
        while (node != null)
        {
            var next = node.Next;
            node.Next = null;
            node.TryDispose();
            node = next;
        }

        _writeHead = null;
        _writeTail = null;
    }
    #endregion
}   

/// <summary>数据包管道写入提交结果（<see cref="PipeWriter.FlushAsync(CancellationToken)"/> 返回）。对标 BCL 的 System.IO.Pipelines.FlushResult</summary>
public readonly struct FlushResult(Boolean isCompleted, Boolean isCanceled)
{
    /// <summary>管道是否已结束。为 true 时写入方应停止继续写入</summary>
    public Boolean IsCompleted { get; } = isCompleted;

    /// <summary>本次提交是否被取消</summary>
    public Boolean IsCanceled { get; } = isCanceled;
}
