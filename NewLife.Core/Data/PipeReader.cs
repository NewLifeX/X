using System.Buffers;

namespace NewLife.Data;

/// <summary>数据包管道读侧句柄。从管道读取未消费数据</summary>
/// <remarks>
/// <para><b>对齐 BCL</b>：命名与形态对齐 System.IO.Pipelines 的 <c>PipeReader</c>（同名不同命名空间；两者需同时引用时用别名，如 <c>using NlPipeReader = NewLife.Data.PipeReader;</c>）。BCL 为抽象类 + 内部实现，本库为具体类；读取结果 <see cref="ReadResult"/> 对应 BCL 的 ReadResult。</para>
/// <para><b>职责</b>：管道的未消费数据窗口（段链）由本类持有——读取挂起与唤醒、窗口推进、帧切出、背压恢复判定都在读侧完成；写侧经内部入口 <see cref="AppendInternal(IPacket)"/> 追加数据、完成时经 <see cref="OnWriterCompletedLocked"/> 唤醒读侧。</para>
/// <para>消费推进以字节计数 <see cref="AdvanceTo(Int64)"/> 为主——追加数据会重建窗口序列，位置在跨追加场景不稳定；字节计数对跨段、跨轮场景简单可靠，且可在全部目标框架实现（含 net45），另提供 SequencePosition 重载（取自最近一次读取窗口，形态对齐）。</para>
/// <para>单读者：同一时刻只允许一个挂起读取；结束读取后追加的数据直接释放。</para>
/// </remarks>
public sealed class PipeReader
{
    #region 属性
    /// <summary>当前未消费窗口（不等待）。无数据时为空序列</summary>
    public ReadOnlySequence<Byte> Buffer { get { lock (_pipe.SyncRoot) return BuildWindowLocked(); } }

    /// <summary>写侧是否已完成</summary>
    public Boolean IsCompleted => _pipe.WriterCompleted;

    /// <summary>未消费数据长度。管道级视图见 <see cref="Pipe.UnconsumedLength"/></summary>
    internal Int64 UnconsumedLength { get { lock (_pipe.SyncRoot) return _length; } }
    #endregion

    #region 内部状态
    private readonly Pipe _pipe;

    /// <summary>段链首。每段同时承载序列内存与数据句柄</summary>
    private PacketHelper.PacketSequenceSegment? _segFirst;

    /// <summary>段链尾，追加 O(1)</summary>
    private PacketHelper.PacketSequenceSegment? _segLast;

    /// <summary>链首段内已消费偏移</summary>
    private Int32 _skip;

    /// <summary>未消费长度</summary>
    private Int64 _length;

    /// <summary>已检查长度。窗口前部已解析但不足成帧、等待更多数据的字节数（examined 语义）</summary>
    private Int64 _examined;

    /// <summary>读侧结束</summary>
    private Boolean _readerCompleted;

    /// <summary>取消标志。无挂起读取时置位，下一次读取立即返回取消结果</summary>
    private Boolean _cancelPending;

    /// <summary>挂起的读取</summary>
    private TaskCompletionSource<ReadResult>? _waiting;

    /// <summary>挂起读取的取消注册，完成时释放避免长期令牌累积</summary>
    private CancellationTokenRegistration _waitingReg;

    /// <summary>空帧占位（共享空数组）</summary>
    private static readonly Byte[] _empty = [];
    #endregion

    #region 构造
    internal PipeReader(Pipe pipe) => _pipe = pipe;
    #endregion

    #region 读取
    /// <summary>读取数据。有数据立即返回；无数据挂起直到追加、完成或取消</summary>
    /// <param name="cancellationToken">取消通知。取消时抛出 <see cref="OperationCanceledException"/></param>
    /// <returns>读取结果</returns>
    public ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
    {
        lock (_pipe.SyncRoot)
        {
            // 取消、结束、已完成或已有新数据：结果立即可得
            if (TryTakeResultLocked(out var result)) return new ValueTask<ReadResult>(result);

            // 无数据：挂起等待
            var waiting = _waiting;
            if (waiting != null)
            {
                // 挂起已被取消令牌定案（TrySetCanceled）但尚未取走：清理残留挂起，允许读取器继续使用
                if (!waiting.Task.IsCompleted) throw new InvalidOperationException("Existing pending read; Pipe supports single reader only.");

                _waitingReg.Dispose();
                _waitingReg = default;
                _waiting = null;
            }

#if NET45
            // net45 没有 RunContinuationsAsynchronously，接受同步续体
            var tcs = new TaskCompletionSource<ReadResult>();
#else
            var tcs = new TaskCompletionSource<ReadResult>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
            _waiting = tcs;

            if (cancellationToken.CanBeCanceled)
                _waitingReg = cancellationToken.Register(static state => ((TaskCompletionSource<ReadResult>)state!).TrySetCanceled(), tcs);

            return new ValueTask<ReadResult>(tcs.Task);
        }
    }

    /// <summary>尝试同步读取（不等待）。有数据、已取消或管道已结束时返回 true</summary>
    /// <param name="result">读取结果</param>
    /// <returns>是否立即可读；无数据且未结束时返回 false</returns>
    public Boolean TryRead(out ReadResult result)
    {
        lock (_pipe.SyncRoot)
        {
            return TryTakeResultLocked(out result);
        }
    }

    /// <summary>读取数据，等待窗口至少达到指定字节数。有数据但不足且未结束时标记已检查并继续等待</summary>
    /// <param name="minimumSize">期望的最小字节数</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>读取结果；返回时窗口长度不小于 minimumSize，或管道已结束/取消</returns>
    /// <remarks>对齐 System.IO.Pipelines 的 PipeReader.ReadAtLeastAsync；本方法不消费数据，调用方仍需按常规推进窗口。</remarks>
    /// <exception cref="ArgumentOutOfRangeException">minimumSize 为负数</exception>
    public ValueTask<ReadResult> ReadAtLeastAsync(Int32 minimumSize, CancellationToken cancellationToken = default)
    {
        if (minimumSize < 0) throw new ArgumentOutOfRangeException(nameof(minimumSize));

        return ReadAtLeastAsyncCore(minimumSize, cancellationToken);
    }

    /// <summary>读取直到至少 minimumSize 字节（异步核心）</summary>
    /// <param name="minimumSize">期望的最小字节数</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>读取结果</returns>
    private async ValueTask<ReadResult> ReadAtLeastAsyncCore(Int32 minimumSize, CancellationToken cancellationToken)
    {
        while (true)
        {
            var result = await ReadAsync(cancellationToken).ConfigureAwait(false);
            if (result.IsCanceled || result.IsCompleted || result.Buffer.Length >= minimumSize) return result;

            // 不足：标记窗口前部已检查，等追加更多数据后继续（不消费）
            AdvanceTo(0, result.Buffer.Length);
        }
    }
    #endregion

    #region 消费推进
    /// <summary>消费推进。只能推进当前窗口内已读取的字节数，跨段自动记账，整节点消费后随即归还内存池</summary>
    /// <param name="consumedBytes">已消费字节数，不得超过当前窗口长度</param>
    /// <remarks>单参推进会把全部已消费字节视为已检查；仅想“看看再等更多数据”时请用两参重载。</remarks>
    public void AdvanceTo(Int64 consumedBytes) => AdvanceTo(consumedBytes, consumedBytes);

    /// <summary>消费推进（含已检查长度）。窗口前部已检查部分在无新数据时不再唤醒，等追加后继续</summary>
    /// <param name="consumedBytes">已消费字节数</param>
    /// <param name="examinedBytes">已检查字节数，不得小于已消费字节数；标记已解析但不完整的字节范围</param>
    public void AdvanceTo(Int64 consumedBytes, Int64 examinedBytes)
    {
        Boolean resumed;

        lock (_pipe.SyncRoot)
        {
            resumed = AdvanceLocked(consumedBytes, examinedBytes);
        }

        if (resumed) _pipe.RaiseResumed();
    }

    /// <summary>消费推进（SequencePosition 版，形态对齐 System.IO.Pipelines）</summary>
    /// <param name="consumed">已消费位置，必须取自最近一次读取结果的窗口</param>
    /// <exception cref="InvalidOperationException">位置不属于当前读取窗口（含已过期的旧窗口位置）</exception>
    public void AdvanceTo(SequencePosition consumed) => AdvanceTo(consumed, consumed);

    /// <summary>消费推进（含已检查位置，SequencePosition 版）</summary>
    /// <param name="consumed">已消费位置，必须取自最近一次读取结果的窗口</param>
    /// <param name="examined">已检查位置，不得位于已消费位置之前</param>
    /// <exception cref="InvalidOperationException">位置不属于当前读取窗口（含已过期的旧窗口位置）</exception>
    public void AdvanceTo(SequencePosition consumed, SequencePosition examined)
    {
        var buffer = Buffer;
        AdvanceTo(GetOffset(buffer, consumed), GetOffset(buffer, examined));
    }

    /// <summary>把窗口内位置换算为相对未消费起点的字节偏移</summary>
    private static Int64 GetOffset(in ReadOnlySequence<Byte> buffer, SequencePosition position)
    {
        Int64 offset;
        try
        {
            offset = buffer.Slice(buffer.Start, position).Length;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("The position does not belong to the current read buffer. Use a position from the latest read result.", ex);
        }

        if (offset < 0 || offset > buffer.Length) throw new InvalidOperationException("The position does not belong to the current read buffer. Use a position from the latest read result.");

        return offset;
    }
    #endregion

    #region 切帧与限长
    /// <summary>切出当前窗口前 count 字节为拥有句柄（零拷贝共享切片）并推进消费窗口</summary>
    /// <param name="count">帧长度（字节），不得超过当前窗口长度</param>
    /// <returns>拥有句柄帧（多段时由共享切片组成）；可跨轮持有，用后 Dispose</returns>
    /// <remarks>切出与窗口前移在同一锁内完成：帧自带引用计数，管道推进后帧数据仍有效。切帧为零拷贝共享切片、成本与帧大小无关（管道包级通路 Append→Read 回环基准实测恒定约 85ns）。</remarks>
    public IPacket TakeFrame(Int64 count)
    {
        Boolean resumed;
        IPacket frame;

        lock (_pipe.SyncRoot)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (count > _length) throw new ArgumentOutOfRangeException(nameof(count), $"Frame length {count} exceeds buffered length {_length}");

            frame = SliceLocked(count);
            resumed = AdvanceLocked(count, count);
        }

        if (resumed) _pipe.RaiseResumed();

        return frame;
    }

    /// <summary>限定读取窗口，用于按帧长读取负载（body）</summary>
    /// <param name="count">限定字节数</param>
    /// <returns>限长读取器；读满或释放对齐后主读取器恰好停在帧尾</returns>
    public LimitedReader Limit(Int64 count) => new(this, count);
    #endregion

    #region 流形态
    /// <summary>以流形态读取管道数据。对齐 System.IO.Pipelines 的 PipeReader.AsStream</summary>
    /// <param name="leaveOpen">释放流时是否保留读取器；false 则流 Dispose 时结束读取</param>
    /// <returns>只读流；读取映射到 ReadAsync + 窗口推进</returns>
    public Stream AsStream(Boolean leaveOpen = false) => new PipeReaderStream(this, leaveOpen);

    /// <summary>管道读取器流包装。同步方法以阻塞等待实现（与 BCL 同形）</summary>
    private sealed class PipeReaderStream : Stream
    {
        private readonly PipeReader _reader;
        private readonly Boolean _leaveOpen;

        public PipeReaderStream(PipeReader reader, Boolean leaveOpen)
        {
            _reader = reader;
            _leaveOpen = leaveOpen;
        }

        public override Boolean CanRead => true;
        public override Boolean CanSeek => false;
        public override Boolean CanWrite => false;
        public override Int64 Length => throw new NotSupportedException();
        public override Int64 Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush() { }

        public override Int64 Seek(Int64 offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(Int64 value) => throw new NotSupportedException();

        public override void Write(Byte[] buffer, Int32 offset, Int32 count) => throw new NotSupportedException();

        public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count)
        {
            if (count == 0) return 0;

            return ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
        }

        public override async Task<Int32> ReadAsync(Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken)
        {
            var result = await _reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (result.IsCanceled) throw new OperationCanceledException();
            if (result.Buffer.IsEmpty) return 0;

            // 本次可取部分拷出后即推进窗口（流语义：边读边消费）
            var n = (Int32)Math.Min(count, result.Buffer.Length);
            var take = result.Buffer.Slice(0, n);
            var pos = offset;
            foreach (var seg in take)
            {
                seg.Span.CopyTo(buffer.AsSpan(pos));
                pos += seg.Length;
            }
            _reader.AdvanceTo(n);

            return n;
        }

        protected override void Dispose(Boolean disposing)
        {
            if (disposing && !_leaveOpen) _reader.Complete();

            base.Dispose(disposing);
        }
    }
    #endregion

    #region 取消与结束
    /// <summary>取消挂起的读取。无挂起读取时，下一次读取立即返回取消结果</summary>
    public void CancelPendingRead()
    {
        TaskCompletionSource<ReadResult>? waiter;
        CancellationTokenRegistration reg;
        ReadResult result = default;

        lock (_pipe.SyncRoot)
        {
            waiter = TakeWaiterLocked(out reg);
            if (waiter != null)
                result = new ReadResult(ReadOnlySequence<Byte>.Empty, false, true);
            else
                _cancelPending = true;
        }

        NotifyWaiter(waiter, reg, result);
    }

    /// <summary>结束读取。释放全部未消费数据，此后追加的数据直接被释放</summary>
    public void Complete()
    {
        TaskCompletionSource<ReadResult>? waiter;
        CancellationTokenRegistration reg;
        ReadResult result = default;
        TaskCompletionSource<FlushResult>? flushWaiter;
        CancellationTokenRegistration flushReg;

        lock (_pipe.SyncRoot)
        {
            if (_readerCompleted) return;

            _readerCompleted = true;
            _pipe.ReaderCompleted = true;
            _pipe.WriterCompleted = true;

            ReleaseAllLocked();
            _pipe.ResetPauseLocked();
            _pipe.Writer.ReleaseWriteLocked();

            waiter = TakeWaiterLocked(out reg);
            if (waiter != null) result = new ReadResult(ReadOnlySequence<Byte>.Empty, true, false);

            // 唤醒写侧挂起提交（读侧结束即管道结束）
            flushWaiter = _pipe.Writer.TakeFlushWaiterLocked(out flushReg);
        }

        NotifyWaiter(waiter, reg, result);
        PipeWriter.NotifyFlushWaiter(flushWaiter, flushReg, new FlushResult(true, false));
    }

    /// <summary>结束读取（异步形态，与 <see cref="Complete"/> 等价）</summary>
    /// <param name="error">结束原因（异常）。可为空</param>
    public ValueTask CompleteAsync(Exception? error = null)
    {
        Complete();
        return default;
    }
    #endregion

    #region 写侧入口
    /// <summary>追加数据（所有权转移：无条件接管入参句柄；管道已关闭时由管道释放）。公开面见 <see cref="PipeWriter.Append(IPacket)"/></summary>
    /// <param name="pk">追加的数据包（单段或链式）</param>
    internal void AppendInternal(IPacket pk)
    {
        if (pk == null) return;

        TaskCompletionSource<ReadResult>? waiter;
        CancellationTokenRegistration reg;
        ReadResult result = default;
        Boolean resumed;

        lock (_pipe.SyncRoot)
        {
            if (_pipe.WriterCompleted || _pipe.ReaderCompleted)
            {
                // 已关闭：由管道负责释放（所有权已转移），整链归还
                pk.TryDispose();
                return;
            }

            // 段链即数据链：每段同时承载序列内存与数据句柄（消费推进时同步前移与归还）
            var node = pk;
            var segLast = _segLast;
            while (node != null)
            {
                var seg = segLast == null
                    ? new PacketHelper.PacketSequenceSegment(node.GetMemory())
                    : segLast.Append(node.GetMemory());
                seg.Packet = node;
                _segFirst ??= seg;
                segLast = seg;

                node = node.Next;
            }
            _segLast = segLast;
            _length += pk.Total;

            // 维护暂停态（追加通常只会推高长度；动态调高恢复水位等极端配置下也可能在此解除）
            resumed = _pipe.UpdatePauseLocked(_length);

            waiter = TakeWaiterLocked(out reg);
            if (waiter != null) result = new ReadResult(BuildWindowLocked(), false, false);
        }

        NotifyWaiter(waiter, reg, result);

        if (resumed) _pipe.RaiseResumed();
    }

    /// <summary>写侧完成时的读侧收尾：取出挂起读取并构建完成结果（调用方持锁），返回句柄供锁外唤醒</summary>
    /// <param name="reg">取消注册，需在锁外释放</param>
    /// <param name="result">唤醒携带的读取结果</param>
    /// <returns>挂起的读取；无挂起时为 null</returns>
    internal TaskCompletionSource<ReadResult>? OnWriterCompletedLocked(out CancellationTokenRegistration reg, out ReadResult result)
    {
        var waiter = TakeWaiterLocked(out reg);
        result = waiter != null ? new ReadResult(BuildWindowLocked(), true, false) : default;

        return waiter;
    }
    #endregion

    #region 内部实现
    /// <summary>尝试立即取读取结果（调用方持锁）。取消、结束、已完成与有未检查数据时立即可得</summary>
    /// <param name="result">读取结果</param>
    /// <returns>是否立即可得</returns>
    private Boolean TryTakeResultLocked(out ReadResult result)
    {
        // 取消优先：取消挂起的读取未被消费时，下一次读取立即返回取消结果
        if (_cancelPending)
        {
            _cancelPending = false;
            result = new ReadResult(ReadOnlySequence<Byte>.Empty, false, true);
            return true;
        }

        if (_readerCompleted)
        {
            result = new ReadResult(ReadOnlySequence<Byte>.Empty, true, false);
            return true;
        }

        // 已完成：返回残余窗口，让消费方处理收尾
        if (_pipe.WriterCompleted)
        {
            result = new ReadResult(BuildWindowLocked(), true, false);
            return true;
        }

        // 有未检查的新数据
        if (_length > _examined)
        {
            result = new ReadResult(BuildWindowLocked(), false, false);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>取出挂起的读取句柄（调用方持锁），供锁外唤醒</summary>
    /// <param name="reg">取消注册，需在锁外释放</param>
    /// <returns>挂起的读取；无挂起时为 null</returns>
    internal TaskCompletionSource<ReadResult>? TakeWaiterLocked(out CancellationTokenRegistration reg)
    {
        var waiter = _waiting;
        reg = _waitingReg;
        _waiting = null;
        _waitingReg = default;

        return waiter;
    }

    /// <summary>锁外唤醒：释放取消注册并完成任务</summary>
    /// <param name="waiter">挂起的读取</param>
    /// <param name="reg">取消注册</param>
    /// <param name="result">读取结果</param>
    internal static void NotifyWaiter(TaskCompletionSource<ReadResult>? waiter, CancellationTokenRegistration reg, ReadResult result)
    {
        reg.Dispose();
        waiter?.TrySetResult(result);
    }

    /// <summary>复位读侧状态供管道复用（帧层专用）。复位前管道须已完成</summary>
    internal void ResetForReuse()
    {
        _readerCompleted = false;
        _cancelPending = false;
    }

    /// <summary>消费推进（调用方持锁）。返回是否需要触发恢复事件</summary>
    /// <param name="consumedBytes">已消费字节数</param>
    /// <param name="examinedBytes">已检查字节数</param>
    private Boolean AdvanceLocked(Int64 consumedBytes, Int64 examinedBytes)
    {
        if (consumedBytes < 0) throw new ArgumentOutOfRangeException(nameof(consumedBytes));
        if (consumedBytes > _length) throw new ArgumentOutOfRangeException(nameof(consumedBytes), $"Consumed {consumedBytes} exceeds buffered length {_length}");
        if (examinedBytes < consumedBytes) examinedBytes = consumedBytes;

        var n = consumedBytes;
        while (n > 0 && _segFirst != null)
        {
            var seg = _segFirst;
            var avail = seg.Memory.Length - _skip;
            if (n < avail)
            {
                _skip += (Int32)n;
                break;
            }

            n -= avail;
            _skip = 0;

            // 整段消费：段链前移，数据句柄单独归还（先摘链防级联）
            _segFirst = seg.Next as PacketHelper.PacketSequenceSegment;
            if (_segFirst == null) _segLast = null;

            ReleaseSegmentPacket(seg);
        }

        _length -= consumedBytes;

        // 窗口前移后重算已检查长度：旧检查量前移，再叠加本次声明
        var examinedOld = Math.Max(0, _examined - consumedBytes);
        _examined = Math.Max(examinedOld, examinedBytes - consumedBytes);
        if (_examined > _length) _examined = _length;

        // 全部消费：重置窗口
        if (_segFirst == null)
        {
            _skip = 0;
            _examined = 0;
        }

        // 背压状态：转入暂停/解除暂停（迟滞），解除时报告恢复
        return _pipe.UpdatePauseLocked(_length);
    }

    /// <summary>切出窗口前 count 字节为拥有切片链（调用方持锁，不改变窗口）</summary>
    /// <param name="count">字节数</param>
    /// <returns>拥有切片链；窗口覆盖段均已递增引用计数</returns>
    private IPacket SliceLocked(Int64 count)
    {
        if (count <= 0) return new ArrayPacket(_empty);

        IPacket? head = null;
        IPacket? tail = null;
        var skip = _skip;
        var remain = count;
        for (var seg = _segFirst; seg != null && remain > 0; seg = seg.Next as PacketHelper.PacketSequenceSegment)
        {
            var packet = seg.Packet;
            if (packet == null) continue;

            var avail = seg.Memory.Length - skip;
            var take = (Int32)Math.Min(avail, remain);
            if (take <= 0)
            {
                skip = 0;
                continue;
            }

            var slice = packet.Slice(skip, take);

            if (head == null)
                head = slice;
            else
                tail!.Next = slice;

            tail = slice;
            remain -= take;
            skip = 0;
        }

        return head ?? new ArrayPacket(_empty);
    }

    /// <summary>归还段上的数据句柄（先摘链防级联，调用方持锁）</summary>
    /// <param name="seg">段</param>
    private static void ReleaseSegmentPacket(PacketHelper.PacketSequenceSegment seg)
    {
        var packet = seg.Packet;
        seg.Packet = null;
        if (packet != null)
        {
            packet.Next = null;
            packet.TryDispose();
        }
    }

    /// <summary>归还整条未消费链并清空状态（调用方持有锁）</summary>
    private void ReleaseAllLocked()
    {
        var seg = _segFirst;
        while (seg != null)
        {
            var next = seg.Next as PacketHelper.PacketSequenceSegment;

            ReleaseSegmentPacket(seg);

            seg = next;
        }

        _segFirst = null;
        _segLast = null;
        _skip = 0;
        _length = 0;
    }

    /// <summary>构建当前未消费窗口（调用方持有锁）</summary>
    /// <returns>只读字节序列；无数据时为空序列</returns>
    private ReadOnlySequence<Byte> BuildWindowLocked()
    {
        if (_segFirst == null || _segLast == null || _length <= 0) return ReadOnlySequence<Byte>.Empty;

        return new ReadOnlySequence<Byte>(_segFirst, _skip, _segLast, _segLast.Memory.Length);
    }
    #endregion
}

/// <summary>数据包管道读取结果。对标 BCL 的 System.IO.Pipelines.ReadResult</summary>
public readonly struct ReadResult(ReadOnlySequence<Byte> buffer, Boolean isCompleted, Boolean isCanceled)
{
    /// <summary>未消费数据窗口（只读序列）。空表示无数据、已取消或已结束</summary>
    public ReadOnlySequence<Byte> Buffer { get; } = buffer;

    /// <summary>写侧是否已完成。为 true 时窗口内仍可能有未消费数据，取完后不再有新数据</summary>
    public Boolean IsCompleted { get; } = isCompleted;

    /// <summary>本次读取是否被取消</summary>
    public Boolean IsCanceled { get; } = isCanceled;
}
