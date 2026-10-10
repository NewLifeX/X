using System.Buffers;

namespace NewLife.Data;

/// <summary>限长读取器。在管道主读取器上限定字节预算（流式模式），或对内存数据包限定窗口（内存模式），用于按帧长读取负载（body）</summary>
/// <remarks>
/// <para><b>两种模式</b>：流式模式由 <see cref="PipeReader.Limit(Int64)"/> 创建，数据随管道到达；内存模式由消息整帧解析创建，数据已在内存（读取立即完成）。</para>
/// <para><b>视图与消费</b>：<see cref="AsPacket"/> 取剩余体视图（不消费）；<see cref="ReadAllAsync"/> 读满并推进预算（消费）；需要长驻内容时请使用 ReadAllAsync。</para>
/// <para><b>短读不静默</b>：流式模式 <see cref="ReadAllAsync"/> 未读满就遇到取消或流结束（对端关闭、管道故障）时抛异常，不返回半截消息体——半截体除了长度对不上之外没有任何可判定的信号，静默返回等于把截断伪装成正常消息。</para>
/// <para>读取窗口裁剪到预算内；<see cref="AdvanceTo(Int64, Int64)"/> 透传主读取器并扣减预算。</para>
/// <para><see cref="DrainAsync"/> 丢弃未读余量（等待数据到达逐步跳过），使主读取器对齐到帧尾；未读满就进入下一帧前必须 Drain，否则窗口错位。</para>
/// <para>预算耗尽后的读取返回 <c>IsCompleted=true</c> 的空结果（体结束语义）。</para>
/// <para>单消费者使用；内存模式的底层数据包所有权归消息持有，本读取器只提供窗口视图。</para>
/// </remarks>
public sealed class LimitedReader
{
    #region 属性
    private readonly PipeReader? _reader;
    private readonly IPacket? _packet;
    private readonly ReadOnlySequence<Byte> _sequence;

    /// <summary>内存模式起点偏移（用于复位）</summary>
    private readonly Int64 _start;

    /// <summary>内存模式初始预算（用于复位）</summary>
    private readonly Int64 _initial;

    private Int64 _offset;
    private Int64 _remaining;

    /// <summary>是否流式模式。false 为内存模式（整帧解析，读取立即完成）</summary>
    public Boolean IsStreaming => _reader != null;

    /// <summary>剩余预算（字节）</summary>
    public Int64 Remaining => _remaining;

    /// <summary>当前窗口（裁剪到剩余预算内）。无数据时为空序列</summary>
    public ReadOnlySequence<Byte> Buffer
    {
        get
        {
            // 内存模式：直接对预建序列切片
            if (_reader == null) return _sequence.Slice(_offset, _remaining);

            var buffer = _reader.Buffer;
            return _remaining >= buffer.Length ? buffer : buffer.Slice(0, _remaining);
        }
    }
    #endregion

    #region 构造
    /// <summary>在主读取器上限定字节预算（流式模式）</summary>
    /// <param name="reader">主读取器</param>
    /// <param name="length">预算字节数</param>
    internal LimitedReader(PipeReader reader, Int64 length)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));

        _reader = reader;
        _remaining = length;
    }

    /// <summary>在内存数据包上限定窗口（内存模式）。数据包所有权归调用方（消息），本读取器只提供视图</summary>
    /// <param name="packet">底层数据包</param>
    /// <param name="offset">起始偏移</param>
    /// <param name="length">窗口字节数</param>
    internal LimitedReader(IPacket packet, Int32 offset, Int64 length)
    {
        if (packet == null) throw new ArgumentNullException(nameof(packet));
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));

        _packet = packet;
        _sequence = packet.AsReadOnlySequence();
        _start = offset;
        _initial = length;
        _offset = offset;
        _remaining = length;
    }
    #endregion

    #region 方法
    /// <summary>复位到起点，使内容可被重新读取。仅内存模式可用</summary>
    /// <remarks>用于事件链（可观测）已消费消息体后、把消息交付给等待方前恢复其可读性。
    /// 流式体的数据由管道承载、不可重放，调用将抛异常。</remarks>
    /// <exception cref="InvalidOperationException">流式模式不可复位</exception>
    internal void Reset()
    {
        if (_reader != null) throw new InvalidOperationException("流式模式不支持复位");

        _offset = _start;
        _remaining = _initial;
    }
    /// <summary>读取数据。预算耗尽时返回已结束的空结果；其余语义与主读取器一致</summary>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>读取结果（窗口已裁剪到预算内）</returns>
    public async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
    {
        if (_remaining <= 0) return new ReadResult(ReadOnlySequence<Byte>.Empty, true, false);

        // 内存模式：数据已在内存，读取立即完成
        if (_reader == null) return new ReadResult(Buffer, true, false);

        // 主读取器已随管道结束完成：按“体结束”语义返回，不再读取（结束后再读会抛异常）
        if (_reader.IsReaderCompleted) return new ReadResult(ReadOnlySequence<Byte>.Empty, true, false);

        var rr = await _reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (rr.IsCanceled) return rr;

        var buffer = rr.Buffer;
        if (buffer.Length > _remaining) buffer = buffer.Slice(0, _remaining);

        return new ReadResult(buffer, rr.IsCompleted, false);
    }

    /// <summary>尝试同步读取（不等待）。有数据、已取消、预算耗尽或流结束时返回 true</summary>
    /// <param name="result">读取结果（窗口已裁剪到预算内）</param>
    /// <returns>是否立即可读；无数据且未结束时返回 false</returns>
    public Boolean TryRead(out ReadResult result)
    {
        // 预算耗尽：体结束语义
        if (_remaining <= 0)
        {
            result = new ReadResult(ReadOnlySequence<Byte>.Empty, true, false);
            return true;
        }

        // 内存模式：数据已在内存，立即返回
        if (_reader == null)
        {
            result = new ReadResult(Buffer, true, false);
            return true;
        }

        // 主读取器已随管道结束完成：按“体结束”语义返回，不再读取（结束后再读会抛异常）
        if (_reader.IsReaderCompleted)
        {
            result = new ReadResult(ReadOnlySequence<Byte>.Empty, true, false);
            return true;
        }

        if (!_reader.TryRead(out var rr))
        {
            result = default;
            return false;
        }

        if (rr.IsCanceled)
        {
            result = rr;
            return true;
        }

        var buffer = rr.Buffer;
        if (buffer.Length > _remaining) buffer = buffer.Slice(0, _remaining);

        result = new ReadResult(buffer, rr.IsCompleted, false);
        return true;
    }

    /// <summary>消费推进，不得超过剩余预算</summary>
    /// <param name="consumedBytes">已消费字节数</param>
    public void AdvanceTo(Int64 consumedBytes) => AdvanceTo(consumedBytes, consumedBytes);

    /// <summary>消费推进（含已检查长度），不得超过剩余预算</summary>
    /// <param name="consumedBytes">已消费字节数</param>
    /// <param name="examinedBytes">已检查字节数</param>
    public void AdvanceTo(Int64 consumedBytes, Int64 examinedBytes)
    {
        if (consumedBytes < 0 || consumedBytes > _remaining) throw new ArgumentOutOfRangeException(nameof(consumedBytes));
        if (examinedBytes < consumedBytes) examinedBytes = consumedBytes;
        if (examinedBytes > _remaining) examinedBytes = _remaining;

        if (_reader == null)
            _offset += consumedBytes;
        else
            _reader.AdvanceTo(consumedBytes, examinedBytes);

        _remaining -= consumedBytes;
    }

    /// <summary>丢弃未读余量，使主读取器对齐到帧尾</summary>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>未读余量清零；流结束时可能保留余量（无法对齐）</returns>
    /// <remarks>等待数据到达并逐步跳过；取消时不作处理，由调用方决定</remarks>
    public async ValueTask DrainAsync(CancellationToken cancellationToken = default)
    {
        // 内存模式：直接丢弃余量
        if (_reader == null)
        {
            _offset += _remaining;
            _remaining = 0;
            return;
        }

        while (_remaining > 0)
        {
            var rr = await _reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (rr.IsCanceled) return;

            var take = (Int64)Math.Min(rr.Buffer.Length, _remaining);
            if (take > 0)
            {
                _reader.AdvanceTo(take, take);
                _remaining -= take;
            }
            else if (rr.IsCompleted)
            {
                // 数据未到齐但流已结束：无法对齐，保留余量由调用方感知
                return;
            }
        }
    }

    /// <summary>取剩余体的数据包视图（不消费）。仅内存模式可用；流式模式请用 <see cref="ReadAllAsync"/></summary>
    /// <returns>剩余体视图；预算耗尽时返回 null。拥有句柄为共享切片（各自 Dispose），借阅视图仅在底层数据有效期内可用</returns>
    /// <exception cref="InvalidOperationException">流式模式不允许直接取包</exception>
    public IPacket? AsPacket()
    {
        if (_reader != null) throw new InvalidOperationException("流式模式不允许直接取包，请使用 ReadAllAsync 读取或 DrainAsync 丢弃");
        if (_remaining <= 0) return null;

        return _packet!.Slice((Int32)_offset, (Int32)_remaining);
    }

    /// <summary>读满剩余数据（读取完成语义）</summary>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>剩余体的数据包；内存模式为立即完成的视图，流式模式为读满后的池化包</returns>
    /// <remarks>流式模式未读满就遇取消或流结束时抛异常，不返回半截消息体；内存模式数据已在内存，不会短读。</remarks>
    /// <exception cref="OperationCanceledException">流式模式读取被取消，消息体未读满</exception>
    /// <exception cref="EndOfStreamException">流式模式数据流提前结束，消息体未读满</exception>
    public async ValueTask<IPacket> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        if (_remaining <= 0) return new OwnerPacket(0);

        // 内存模式：取视图并推进至末尾
        if (_reader == null)
        {
            var view = _packet!.Slice((Int32)_offset, (Int32)_remaining);
            _offset += _remaining;
            _remaining = 0;
            return view;
        }

        // 流式模式：预算已知，一次分配读满
        if (_remaining > Int32.MaxValue) throw new NotSupportedException($"消息体过大（{_remaining} 字节），无法一次性物化");

        var buffer = new OwnerPacket((Int32)_remaining);
        try
        {
            var total = _remaining;
            var memory = buffer.GetMemory();
            var offset = 0;
            var canceled = false;
            while (_remaining > 0)
            {
                var rr = await _reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                if (rr.IsCanceled)
                {
                    canceled = true;
                    break;
                }

                var data = rr.Buffer;
                if (!data.IsEmpty)
                {
                    var take = (Int32)Math.Min(data.Length, _remaining);
                    data.Slice(0, take).CopyTo(memory.Span[offset..]);
                    offset += take;
                    AdvanceTo(take);
                }
                else if (rr.IsCompleted) break;
            }

            // 未读满就退出：半截消息体比异常危险得多（上层只能从长度对不上猜出问题），按原因抛给调用方
            if (_remaining > 0)
            {
                if (canceled) throw new OperationCanceledException($"消息体读取被取消，期望 {total} 字节，实读 {offset} 字节");

                // 管道带异常结束时透传原始故障（与帧泵的口径一致），否则说明对端提前关闭
                if (_reader.Error is { } error) throw error;

                throw new EndOfStreamException($"消息体未读满：期望 {total} 字节，实读 {offset} 字节");
            }

            return buffer.Resize(offset);
        }
        catch
        {
            // 失败路径必须归还池缓冲：留着只能靠析构兜底（打漏释放警告，且归还时机不定）
            buffer.TryDispose();
            throw;
        }
    }
    #endregion
}
