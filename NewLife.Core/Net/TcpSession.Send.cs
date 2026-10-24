using System.Buffers;
using System.Net.Sockets;
using NewLife.Data;
using NewLife.Log;
using NewLife.Messaging;

namespace NewLife.Net;

/// <summary>增强TCP客户端（发送：写锁串行、直发核心与流式/文件/消息发送）</summary>
/// <remarks>
/// <para>发送出口共用一把写锁（<see cref="_writeLock"/>），任一时刻只有一位写者在写套接字；失败在锁外上报并按失败关闭会话。</para>
/// <para>直发：<see cref="SessionBase.Send(IPacket)"/> 锁内同步直写（0 分配 0 拷贝，链式包散列写一次系统调用提交整条链），
/// <see cref="TcpSession.SendAsync(IPacket, CancellationToken)"/> 锁内异步直写；流式/文件/消息发送见对应章节，出站队列（第二出口）与发送泵见 TcpSession.SendQueue。</para>
/// <para>数据流：入口 → 发送骨架 SendUnderLock/SendUnderLockAsync（等锁 → 写 → 释放 → 锁外上报）→ 写核心（SSL 流 / 散列写）→ 内核；
/// 平台差异（Span/数组、异步写降级、内核零拷贝）经 TcpSession.Platform。</para>
/// </remarks>
public partial class TcpSession
{
    #region 发送
    // 发送出口：默认直发，另有可选的发送队列作第二出口。两者共用一把写锁，任一时刻只有一位写者在写套接字。
    // 例外：SSL 握手写不经写锁（客户端握手发生在打开流程内，此时会话尚未对业务可用、不可能有并发发送）；
    // 服务端握手则由 TcpSession.StartSslAsync 抢占写锁后才发起，握手期间到达的发送会等待，避免明文写进TLS流。
    //   Send(IPacket/byte[]/Span)  锁内同步直写：0 分配 0 拷贝，写完才返回，失败返回 -1
    //   SendAsync(IPacket)         锁内异步直写：0 分配 0 拷贝，等待可写期间不占线程
    //   SendAsync(Stream)          流式：读一块 → 写完（或挂起）再读下一块，内核缓冲即背压，内存有界
    //   SendFileAsync(文件)        锁内让内核零拷贝推文件（SendFile/TransmitFile）
    // 慢对端由内核发送缓冲形成天然背压；需要排队/限流的场合可在上层用 Actor 组合，不进发送核心。
    // 批量场景可选走 TcpSession.SendQueue（见 TcpSession.SendQueue.cs 出站队列与发送泵），由发送泵在锁内批量写出。
    // 直发入口共用发送骨架 SendUnderLock/SendUnderLockAsync：等锁 → 写 → 释放 → 锁外上报（失败上报/关闭必须在锁外，防回调内再 Send 死锁）。

    /// <summary>写锁。四个发送入口共用：任一时刻只有一位写者在写套接字，故多线程/多入口混用不会交错</summary>
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    /// <summary>获取续发总预算的截止时间。返回 0 表示不限制（Timeout 未启用）</summary>
    private Int64 GetSendDeadline() => Timeout > 0 ? Runtime.TickCount64 + Timeout : 0;

    private Int32 _bsize;

    /// <summary>按本次发送量调优内核发送缓冲（_bsize 缓存，读取 SendBufferSize 耗时很大）</summary>
    /// <remarks>
    /// <para>调用方须已持有写锁：套接字参数调整不能与锁内的 Send/Write 并发。</para>
    /// <para>_bsize 只增不减，仅同步直写路径使用；异步直写与流式分块不调整内核缓冲。</para>
    /// </remarks>
    /// <param name="sock">目标套接字</param>
    /// <param name="count">本次发送字节数</param>
    private void TuneSendBufferSize(Socket sock, Int32 count)
    {
        if (_bsize == 0) _bsize = sock.SendBufferSize;
        if (_bsize < count) sock.SendBufferSize = _bsize = count;
    }

    /// <summary>发送一段数据，短计数自动续发。返回时全部字节已交给内核，失败抛异常</summary>
    /// <remarks>同步 Send 在接收方窗口受限时可能只发出一部分；循环续发直到发完，总耗时超过预算按超时失败。异常由调用方统一转为发送失败处理</remarks>
    /// <param name="sock">目标套接字</param>
    /// <param name="buffer">数据缓冲</param>
    /// <param name="offset">起始偏移</param>
    /// <param name="count">字节数</param>
    /// <param name="deadline">续发总预算截止时间（Runtime.TickCount64 毫秒），0 不限制</param>
    /// <returns>已发送字节数，等于 count</returns>
    private static Int32 SendAll(Socket sock, Byte[] buffer, Int32 offset, Int32 count, Int64 deadline)
    {
        var total = 0;
        while (total < count)
        {
            var sent = sock.Send(buffer, offset + total, count - total, SocketFlags.None);
            if (sent <= 0) throw new IOException(L.T("Send failed (result={0}), {1}/{2} bytes sent", sent, total, count));

            total += sent;

            // 续发前检查总预算：对端持续慢读时，单次 SendTimeout 约束不住整包发送的总耗时
            if (deadline > 0 && total < count && Runtime.TickCount64 > deadline)
                throw new TimeoutException(L.T("Send timeout, {0}/{1} bytes sent", total, count));
        }

        return total;
    }

    /// <summary>发送多段数据（scatter-gather），短计数后按段序跳过已发字节逐段续发</summary>
    /// <param name="sock">目标套接字</param>
    /// <param name="segments">数据段列表</param>
    /// <param name="total">总字节数</param>
    /// <param name="deadline">续发总预算截止时间（Runtime.TickCount64 毫秒），0 不限制</param>
    /// <returns>已发送字节数，等于 total</returns>
    private static Int32 SendAll(Socket sock, IList<ArraySegment<Byte>> segments, Int32 total, Int64 deadline)
    {
        // 先尝试一次多段发送（平台可合并系统调用）；正常路径一次发完直接返回
        var sent = sock.Send(segments);
        if (sent <= 0) throw new IOException(L.T("Send failed (result={0}), 0/{1} bytes sent", sent, total));
        if (sent >= total) return sent;

        // 短计数：按段序跳过已发字节，逐段续发（复用原段缓冲，零拷贝）
        var skip = sent;
        foreach (var seg in segments)
        {
            if (skip >= seg.Count)
            {
                skip -= seg.Count;
                continue;
            }

            sent += SendAll(sock, seg.Array!, seg.Offset + skip, seg.Count - skip, deadline);
            skip = 0;
        }

        return sent;
    }

    /// <summary>发送骨架（同步）：等写锁 → 写 → 释放 → 锁外上报/关闭</summary>
    /// <remarks>
    /// <para>失败上报与关闭必须放在锁外：OnError/Close 会触发用户事件回调，回调内再次 Send 会在同一线程二次进入写锁而死锁。</para>
    /// <para>写动作用静态 lambda 传入（state 携带数据包等输入），避免每次发送产生闭包分配；Span 入参无法经闭包传递，见下方重载。</para>
    /// <para>流式/文件/消息发送有额外的失败分类（调用方取消与数据错误不上报不关会话），不走本骨架，见各自方法注释。</para>
    /// </remarks>
    /// <param name="state">写动作的输入（数据包等）</param>
    /// <param name="write">锁内写动作（静态 lambda）</param>
    /// <param name="data">出错的发送数据，便于日志定位</param>
    /// <returns>已发送字节数；失败返回 -1</returns>
    private Int32 SendUnderLock<TState>(TState state, Func<TState, TcpSession, Int32> write, Object? data)
    {
        Exception? error = null;
        var rs = -1;

        _writeLock.Wait();
        try
        {
            rs = write(state, this);
        }
        catch (Exception ex)
        {
            error = ex;
        }
        finally
        {
            _writeLock.Release();
        }

        if (error != null)
        {
            ReportSendError(error, data);

            return -1;
        }

        return rs;
    }

    /// <summary>发送骨架（同步，内存段）。Span 不能被闭包捕获，单独提供重载；其余语义同 <see cref="SendUnderLock{TState}"/></summary>
    /// <param name="data">数据</param>
    /// <param name="write">锁内写动作（静态 lambda）</param>
    /// <returns>已发送字节数；失败返回 -1</returns>
    private Int32 SendUnderLock(ReadOnlySpan<Byte> data, MemoryWriter write)
    {
        Exception? error = null;
        var rs = -1;

        _writeLock.Wait();
        try
        {
            rs = write(data, this);
        }
        catch (Exception ex)
        {
            error = ex;
        }
        finally
        {
            _writeLock.Release();
        }

        if (error != null)
        {
            ReportSendError(error, null);

            return -1;
        }

        return rs;
    }

    /// <summary>内存段写动作（自定义委托：BCL 的 Func 不支持 ref struct 参数）</summary>
    /// <param name="data">数据</param>
    /// <param name="session">会话</param>
    /// <returns>已发送字节数</returns>
    private delegate Int32 MemoryWriter(ReadOnlySpan<Byte> data, TcpSession session);

    /// <summary>发送骨架（异步）：等写锁 → 写 → 释放 → 锁外上报/关闭；等待可写期间不占线程</summary>
    /// <remarks>取消在等锁边界生效（WaitAsync）；其余语义同 <see cref="SendUnderLock{TState}"/></remarks>
    /// <param name="state">写动作的输入（数据包等）</param>
    /// <param name="write">锁内写动作（静态 lambda）</param>
    /// <param name="data">出错的发送数据，便于日志定位</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已发送字节数；失败返回 -1</returns>
    private async ValueTask<Int32> SendUnderLockAsync<TState>(TState state, Func<TState, TcpSession, ValueTask<Int32>> write, Object? data, CancellationToken cancellationToken)
    {
        Exception? error = null;
        var rs = -1;

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            rs = await write(state, this).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            error = ex;
        }
        finally
        {
            _writeLock.Release();
        }

        if (error != null)
        {
            ReportSendError(error, data);

            return -1;
        }

        return rs;
    }

    /// <summary>直发：锁内同步写出整个数据包（直接写数据包缓冲，0 拷贝 0 分配），写完才返回</summary>
    /// <remarks>写锁保证任一时刻只有一位写者；失败已上报并按失败关闭会话</remarks>
    /// <param name="pk">数据包。借用语义：调用方保留句柄，用完自行释放</param>
    /// <returns>已发送字节数；失败返回 -1</returns>
    private Int32 DirectSend(IPacket pk)
    {
        if (pk == null) return -1;

        if (Log != null && Log.Enable && LogSend) WriteLog("Send [{0}]: {1}", pk.Total, pk.ToHex(LogDataLength));

        return SendUnderLock(pk, static (p, s) => s.WritePacket(p), pk);
    }

    /// <summary>直发：锁内同步写出一段数据（直接写调用方缓冲，0 拷贝 0 分配），写完才返回</summary>
    /// <param name="data">数据。调用方需保证返回前不改写该缓冲</param>
    /// <returns>已发送字节数；失败返回 -1</returns>
    private Int32 DirectSend(ReadOnlySpan<Byte> data)
    {
        if (data.IsEmpty) return 0;

        if (Log != null && Log.Enable && LogSend) WriteLog("Send [{0}]: {1}", data.Length, data.ToHex(LogDataLength));

        return SendUnderLock(data, static (d, s) => s.WriteMemory(d));
    }

    /// <summary>直发：锁内异步写出整个数据包（0 拷贝 0 分配），等待可写期间不占线程</summary>
    /// <remarks>失败已上报并按失败关闭会话；同时使用多个发送入口时由写锁串行化</remarks>
    /// <param name="pk">数据包。借用语义：调用方保留句柄，用完自行释放</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已发送字节数；失败返回 -1</returns>
    private async ValueTask<Int32> DirectSendAsync(IPacket pk, CancellationToken cancellationToken = default)
    {
        if (pk == null) return -1;

        cancellationToken.ThrowIfCancellationRequested();

        if (Log != null && Log.Enable && LogSend) WriteLog("Send [{0}]: {1}", pk.Total, pk.ToHex(LogDataLength));

        return await SendUnderLockAsync(pk, static (p, s) => s.WritePacketAsync(p), pk, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>写核心（同步）：把整包（含链）写完才返回；短计数自动续发，失败抛异常</summary>
    /// <param name="pk">数据包</param>
    /// <returns>已发送字节数</returns>
    private Int32 WritePacket(IPacket pk)
    {
        var count = pk.Total;
        if (count == 0) return 0;

        var sock = Client ?? throw new InvalidOperationException(L.T("Session [{0}] is not open.", Name));

        TuneSendBufferSize(sock, count);

        // SSL 流内部处理部分写：整段写完才返回，无需续发循环
        if (_Stream is { } stream)
        {
            pk.CopyTo(stream);

            LastTime = DateTime.Now;

            return count;
        }

        // 一次系统调用写出整包：单节点直接写数据包自己的缓冲（0 拷贝 0 分配）；
        // 链式包用散列写（scatter-gather）一次提交整条链——逐节点多次写会把一条逻辑帧拆成多个 TCP 段，
        // 对端“整帧同窗到达”的零拷贝快路径随之失效（同一次解读出内存视图而非流式体）
        SendPacketData(sock, pk, count, GetSendDeadline());

        LastTime = DateTime.Now;

        return count;
    }

    /// <summary>同步写一块内存（零拷贝：直接写调用方缓冲），短计数自动续发</summary>
    /// <param name="data">数据</param>
    /// <returns>已发送字节数</returns>
    private Int32 WriteMemory(ReadOnlySpan<Byte> data)
    {
        var sock = Client ?? throw new InvalidOperationException(L.T("Session [{0}] is not open.", Name));
        Int32 rs;

        TuneSendBufferSize(sock, data.Length);

        if (_Stream is { } stream)
        {
            // SSL 流内部处理部分写：整段写完才返回，失败直接抛异常，无需续发循环
            WriteSsl(stream, data);
            rs = data.Length;
        }
        else
        {
            // 同步 Send 在接收方窗口受限时可能只发出一部分，续发循环保证整段送出；总预算按会话 Timeout 计时
            rs = SendAllMemory(sock, data, GetSendDeadline());
        }

        LastTime = DateTime.Now;

        return rs;
    }

    /// <summary>发送失败上报：记错误日志并关闭会话</summary>
    /// <remarks>调用方必须已释放写锁：OnError/Close 会触发用户事件回调，回调内再次 Send 会二次进入写锁而死锁</remarks>
    /// <param name="error">发送异常</param>
    /// <param name="data">出错的发送数据，便于日志定位</param>
    private void ReportSendError(Exception error, Object? data)
    {
        if (error.IsDisposed()) return;

        if (Tracer is { } tracer)
        {
            using var span = tracer.NewSpan($"net:{Name}:Send");
            span?.SetError(error, data);
        }

        OnError("Send", error);

        // 发送异常可能是连接出了问题，需要关闭
        Close("SendError");
    }

    /// <summary>发送数据包（同步直发）：写完才返回</summary>
    /// <param name="data">数据包。借用语义：调用方保留句柄，用完自行释放</param>
    /// <returns>已发送字节数；失败返回 -1</returns>
    protected override Int32 OnSend(IPacket data) => DirectSend(data);

    /// <summary>发送数据（同步直发）：写完才返回</summary>
    /// <param name="data">数据</param>
    /// <returns>已发送字节数；失败返回 -1</returns>
    protected override Int32 OnSend(ArraySegment<Byte> data)
        => data.Array == null || data.Count <= 0 ? 0 : DirectSend(new ReadOnlySpan<Byte>(data.Array, data.Offset, data.Count));

    /// <summary>发送数据（同步直发）：写完才返回</summary>
    /// <param name="data">数据</param>
    /// <returns>已发送字节数；失败返回 -1</returns>
    protected override Int32 OnSend(ReadOnlySpan<Byte> data) => DirectSend(data);
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
        if (!Open()) throw new InvalidOperationException(L.T("Session [{0}] is not open.", GetType().Name));

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
                if (rs < 0) throw new IOException(L.T("Send failed on [{0}], {1} bytes sent before failure.", Name, total));

                total += count;
            }

            // 流提前结束：报错，避免调用方误以为已完整发送
            if (length > 0 && total < length) throw new InvalidDataException(L.T("Stream ended early. Expected {0} bytes but got {1}.", length, total));

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
    /// <para>SSL 会话（<c>SslStream</c>）、Unix 域套接字（内核零拷贝在其中既不会推进也不会报错，实测永久挂起）与无零拷贝发送 API 的目标框架，均降级为 <see cref="SendAsync(Stream, Int64, CancellationToken)"/> 的分块读取发送，语义一致但多一次读块搬运。</para>
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
        if (!fi.Exists) throw new FileNotFoundException(L.T("File not found: {0}", filePath), filePath);

        if (!Open()) throw new InvalidOperationException(L.T("Session [{0}] is not open.", GetType().Name));

        var length = fi.Length;

        Exception? error = null;
        var total = -1L;

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // 非 SSL 会话优先尝试内核零拷贝（NET6+；SSL/Unix 域/低版本由桥内部排除并返回 -1 后降级）
            var kernel = await TrySendFileKernelAsync(filePath, length, cancellationToken).ConfigureAwait(false);
            if (kernel >= 0) return kernel;

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
    #endregion

    #region 消息发送
    /// <summary>发送流式消息（协议模式）：先发协议头部（声明体长），再把数据流内容读一块写一块地送出</summary>
    /// <param name="message">消息（头部字段就位）</param>
    /// <param name="body">消息体数据流</param>
    /// <param name="bodyLength">消息体字节数；负数时从可定位流推导（<see cref="Stream.CanSeek"/>）</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>已写出的流内容字节数</returns>
    /// <remarks>
    /// <para>头部与流内容在<strong>一次</strong>写锁持有期间写出（无交错），整条消息保持一条逻辑消息语义；大消息全程只在读块上驻留，不产生整段内存。</para>
    /// <para>流提前结束（不足声明长度）或连接故障时抛出异常，已发出的部分不回退。</para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">未设置协议</exception>
    /// <exception cref="ArgumentException">体长未知且流不可定位</exception>
    public virtual async ValueTask<Int64> SendMessageAsync(IMessage message, Stream body, Int64 bodyLength = -1, CancellationToken cancellationToken = default)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (body == null) throw new ArgumentNullException(nameof(body));

        var codec = Protocol ?? throw new InvalidOperationException(L.T("Protocol not set for session [{0}]", Name));

        // 长度未知：从可定位流推导（协议头须先声明长度）
        if (bodyLength < 0)
        {
            if (!body.CanSeek) throw new ArgumentException(L.T("Cannot determine the stream length. Provide bodyLength or use a seekable stream."), nameof(bodyLength));
            bodyLength = body.Length - body.Position;
        }

        // 头部先行：与流内容在一次写锁内（头体之间不会被其它写入者插入）
        var header = codec.BuildHeader(message, bodyLength);
        try
        {
            var rs = await SendMessageLockedAsync(header, body, bodyLength, cancellationToken).ConfigureAwait(false);

            // 失败返回 -1：底层已记错误日志并按失败关闭会话，这里转成异常，保持本方法“失败即抛”的契约
            if (rs < 0) throw new IOException(L.T("Send message failed on [{0}].", Name));

            return rs;
        }
        finally
        {
            // 写完才返回，此处释放头部构建产物
            header.TryDispose();
        }
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
        if (!Open()) throw new InvalidOperationException(L.T("Session [{0}] is not open.", GetType().Name));

        return DirectSendAsync(data, cancellationToken);
    }
    #endregion
}
