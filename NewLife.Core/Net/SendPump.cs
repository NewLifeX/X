using System.Buffers;
using NewLife.Data;

namespace NewLife.Net;

/// <summary>段发送委托</summary>
/// <param name="data">段数据</param>
/// <returns>已发送字节数；小于等于 0 视为失败</returns>
internal delegate ValueTask<Int32> SendSegmentDelegate(ReadOnlyMemory<Byte> data);

/// <summary>发送泵。出站管道的唯一消费方：循环取出管道窗口逐段发送（一次唤醒批处理整窗），写侧完成且残余发完后退出；发送失败中止管道</summary>
/// <remarks>
/// <para>与发送管道成对使用：<see cref="Append(IPacket)"/> 追加数据（借用语义：调用方保留句柄所有权与释放责任，管道自取一份引用），<see cref="FlushAsync(Int32)"/> 关闭前排空，<see cref="Abort(Exception?)"/> 中止。</para>
/// <para>借用口径与直发路径一致：入队后调用方即可释放自己的句柄，缓冲由管道持有的引用保活至发送完成。</para>
/// <para>发送动作经构造传入的委托异步执行，本组件不感知 Socket 细节，可脱离会话独立测试。</para>
/// </remarks>
internal sealed class SendPump
{
    #region 属性
    /// <summary>数据发送管道</summary>
    public Pipe Pipe { get; }

    /// <summary>管道是否已完成。已完成时追加数据将直接返回 -1</summary>
    public Boolean IsCompleted => Pipe.IsCompleted;

    /// <summary>待发送字节数</summary>
    public Int64 PendingLength => Pipe.UnconsumedLength;
    #endregion

    #region 构造
    /// <summary>实例化发送泵并启动泵任务</summary>
    /// <param name="pipe">数据发送管道。读侧由本泵独占</param>
    /// <param name="send">段发送委托。返回小于等于 0 视为失败并中止管道</param>
    /// <param name="onError">错误回调，参数为动作名与异常</param>
    /// <param name="log">日志回调，参数为格式与实参</param>
    public SendPump(Pipe pipe, SendSegmentDelegate send, Action<String, Exception> onError, Action<String, Object?[]> log)
    {
        Pipe = pipe;
        _send = send;
        _onError = onError;
        _log = log;

        // 启动发送泵（管道唯一消费方）
        _task = Task.Run(PumpAsync);
    }

    private readonly SendSegmentDelegate _send;
    private readonly Action<String, Exception> _onError;
    private readonly Action<String, Object?[]> _log;
    private Task? _task;
    #endregion

    #region 追加
    /// <summary>追加数据包（借用语义；头节点为借阅视图时自动转自有拷贝）。返回已接收字节数</summary>
    /// <param name="pk">数据包</param>
    /// <returns>已接收字节数；管道已完成时返回 -1</returns>
    /// <remarks>
    /// <para><b>借用语义</b>：调用方保留自己的引用，发送完成后自行释放；管道额外持有一份引用，消费后释放。
    /// 因此可以放心地把接收轮句柄、构建产物交给发送，再立即释放自己的引用。</para>
    /// <para>仅检查<b>头节点</b>是否拥有句柄：头节点为拥有句柄时零拷贝入管道，否则整链克隆为自有拷贝。</para>
    /// <para><b>时效</b>：链中若含借阅视图节点（如帧头链 <c>new OwnerPacket(size) { Next = view }</c>），该视图不会被克隆——入管道后仍引用调用方缓冲，数据真正发出前不得复用或改写该缓冲。</para>
    /// </remarks>
    public Int32 Append(IPacket pk)
    {
        var pipe = Pipe;
        if (pipe.IsCompleted) return -1;

        var count = pk.Total;

        // 拥有句柄：切出共享句柄（引用计数 +1）交给管道，管道消费时释放该引用；调用方自己的句柄不变，用完自行释放。
        // 这份计数不能省：接收轮末按“RefCount==1 即无人持有”判定并复用缓冲，
        // 零拷贝交接若不计数，轮末会把仍在发送中的缓冲交给下一轮接收数据覆盖。
        if (pk is OwnerPacket op && op.RefCount > 0)
            pk = op.Slice(0, -1);
        else
            pk = pk.Clone();

        pipe.Writer.Append(pk);

        return count;
    }

    /// <summary>追加字节跨度（按副本入管道，所有权随副本转移，调用方可立即复用原缓冲）。返回已接收字节数</summary>
    /// <param name="data">字节数据</param>
    /// <returns>已接收字节数；管道已完成时返回 -1</returns>
    public Int32 Append(ReadOnlySpan<Byte> data)
    {
        if (data.IsEmpty) return 0;
        if (Pipe.IsCompleted) return -1;

        var pk = new OwnerPacket(data.Length);
        data.CopyTo(pk.GetSpan());

        var count = pk.Total;

        // 副本为自有句柄：所有权直接交给管道，不再额外计数（否则多出的引用没人释放）
        Pipe.Writer.Append(pk);

        return count;
    }
    #endregion

    #region 流式发送
    /// <summary>流式发送。从数据流分块读入发送管道，由发送泵顺序送出</summary>
    /// <remarks>
    /// <para>分块读取（默认 64KB），每块零拷贝包装入 <see cref="Pipe"/>，大文件全程只在读块上驻留，不产生整段内存。回环基准实测：1MB 用时 510µs、8MB 4.08ms，与手工 64KB 分块持平（管道自身开销约 1~2%），16KB→64KB 分块优化带来 2.3× 提升。</para>
    /// <para>写侧回压：管道未发送数据达到 <see cref="Pipe.PauseThreshold"/> 时挂起等待网络消化，内存占用有界，慢速对端不会导致应用层无限积压。</para>
    /// <para>与 <see cref="Append(IPacket)"/> 共用同一管道出口（无交错）；"头 + 流式体"组合消息先追加头部数据包再调用本方法即可，整条消息保持一条逻辑消息语义。</para>
    /// <para>流提前结束（不足 <paramref name="length"/>）或发送管道中途中止（连接故障/关闭）时抛出异常，已入管道部分仍会尽力送出。</para>
    /// </remarks>
    /// <param name="source">数据流</param>
    /// <param name="length">期望发送的字节数；负数表示读到流尾</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已写入发送管道的总字节数</returns>
    public async ValueTask<Int64> SendAsync(Stream source, Int64 length = -1, CancellationToken cancellationToken = default)
    {
        var pipe = Pipe;
        var total = 0L;

        while (length < 0 || total < length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (pipe.IsCompleted) throw new InvalidOperationException("Send pipe has been completed.", pipe.Error);

            // 期望长度剩余部分不足一块时按剩余读
            var size = StreamChunkSize;
            if (length > 0 && length - total < size) size = (Int32)(length - total);

            // 池化缓冲读块：读满后包装为拥有句柄零拷贝入管道，最终释放归还内存池
            var buffer = ArrayPool<Byte>.Shared.Rent(size);
            Int32 count;
            try
            {
                count = await source.ReadAsync(buffer, 0, size, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                ArrayPool<Byte>.Shared.Return(buffer);
                throw;
            }

            if (count <= 0)
            {
                ArrayPool<Byte>.Shared.Return(buffer);
                break;
            }

            pipe.Writer.Append(new OwnerPacket(buffer, 0, count, true));
            total += count;

            // 写侧回压：排队过深时挂起，等待网络消化到恢复水位（默认提交即挂起，对齐 BCL）
            await pipe.Writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        // 流提前结束：报错，避免调用方误以为已完整发送
        if (length > 0 && total < length) throw new InvalidDataException($"Stream ended early. Expected {length} bytes but got {total}.");

        return total;
    }

    /// <summary>流式发送的读块大小</summary>
    /// <remarks>64KB：基准实测 16KB 分块在回环下的吞吐只有 64KB 分块的一半（小分块导致两端 syscall/唤醒次数成倍），64KB 与整段发送持平且仍远低于 LOH 阈值。</remarks>
    private const Int32 StreamChunkSize = 64 * 1024;
    #endregion

    #region 关闭
    /// <summary>关闭前收尾：完成写入并限时等待泵发完已排队数据</summary>
    /// <param name="timeout">等待超时（毫秒），超时不再等待；底层连接关闭后泵自行退出</param>
    public async Task FlushAsync(Int32 timeout)
    {
        Pipe.Writer.Complete();

        var pump = _task;
        if (pump == null) return;

        var done = await Task.WhenAny(pump, Task.Delay(timeout)).ConfigureAwait(false);
        if (done != pump) _log("发送管道排空超时，待发 {0:n0} 字节", [Pipe.UnconsumedLength]);
    }

    /// <summary>中止管道。幂等：完成写入（唤醒挂起提交）并释放读侧残余</summary>
    /// <param name="error">中止原因</param>
    public void Abort(Exception? error)
    {
        var pipe = Pipe;

        try { pipe.Writer.Complete(error); } catch { }
        try { pipe.Reader.Complete(); } catch { }
    }
    #endregion

    #region 泵
    /// <summary>发送泵循环。唯一消费方：循环取出管道窗口逐段发送（一次唤醒批处理整窗），写侧完成且残余发完后退出；发送失败中止管道</summary>
    private async Task PumpAsync()
    {
        var reader = Pipe.Reader;
        try
        {
            while (true)
            {
                // 读取器已被完成（中止/关闭与发送竞态）：正常收尾，结束后再读会抛异常
                if (reader.IsReaderCompleted) return;

                var rr = await reader.ReadAsync().ConfigureAwait(false);
                if (rr.IsCanceled) continue;

                // 批处理：一次唤醒发完窗口内全部段（追加的每个数据包占一至多个段）
                var buffer = rr.Buffer;
                if (!buffer.IsEmpty)
                {
                    foreach (var memory in buffer)
                    {
                        if (memory.IsEmpty) continue;

                        if (!await SendSegmentAsync(memory).ConfigureAwait(false)) return;
                    }

                    reader.AdvanceTo(buffer.Length);
                }

                // 写侧完成且残余已发完：退出
                if (rr.IsCompleted) return;
            }
        }
        catch (Exception ex)
        {
            _onError("SendPump", ex);
            Abort(ex);
        }
        finally
        {
            // 释放读侧残余与状态（幂等）
            reader.Complete();
        }
    }

    /// <summary>发送一个段，部分发送自动续发。返回是否全部发出（失败时已中止管道）</summary>
    /// <param name="data">段数据</param>
    private async ValueTask<Boolean> SendSegmentAsync(ReadOnlyMemory<Byte> data)
    {
        var offset = 0;
        while (offset < data.Length)
        {
            var rs = await _send(data[offset..]).ConfigureAwait(false);
            if (rs <= 0)
            {
                // 发送失败：中止管道（唤醒挂起提交、释放排队数据），泵退出
                Abort(new IOException($"Send failed (result={rs}), pending {Pipe.UnconsumedLength} bytes"));

                return false;
            }

            offset += rs;
        }

        return true;
    }
    #endregion
}
