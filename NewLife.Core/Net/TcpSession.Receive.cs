using System.Net;
using System.Net.Sockets;
using NewLife.Data;
using NewLife.Log;

namespace NewLife.Net;

/// <summary>增强TCP客户端（接收：读回调、背压暂存与入站管道投递）</summary>
/// <remarks>
/// <para>数据流：Socket/SSL 读回调 → <see cref="TcpSession.OnReceiveAsync"/>（背压暂停时暂存，消费恢复后重启）→ <see cref="TcpSession.OnPreReceive"/>（空数据判闭、投递入站管道）
/// → 协议模式由消息泵从管道定界交付（见 TcpSession.Pump），非协议模式沿事件链 <see cref="TcpSession.OnReceive"/> 派发原始字节。</para>
/// </remarks>
public partial class TcpSession
{
    #region 接收
    /// <summary>同步直读数据。重写以支持SSL</summary>
    /// <returns></returns>
    protected override IOwnerPacket? OnDirectReceive()
    {
        var ss = _Stream;
        if (ss == null) return base.OnDirectReceive();

        using var span = Tracer?.NewSpan($"net:{Name}:Receive");
        try
        {
            var pk = new OwnerPacket(BufferSize);
            var size = ss.Read(pk.Buffer, 0, pk.Length);
            span?.Value = size;

            return pk.Resize(size);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    /// <summary>异步直读数据。重写以支持SSL</summary>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    protected override async Task<IOwnerPacket?> OnDirectReceiveAsync(CancellationToken cancellationToken = default)
    {
        var ss = _Stream;
        if (ss == null) return await base.OnDirectReceiveAsync(cancellationToken).ConfigureAwait(false);

        using var span = Tracer?.NewSpan($"net:{Name}:ReceiveAsync", BufferSize + "");
        try
        {
            var pk = new OwnerPacket(BufferSize);
            var size = await ss.ReadAsync(pk.Buffer, 0, pk.Length, cancellationToken).ConfigureAwait(false);
            span?.Value = size;

            return pk.Resize(size);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    internal override Boolean OnReceiveAsync(SocketAsyncEventArgs se)
    {
        var sock = Client;
        if (sock == null || !Active || Disposed) throw new ObjectDisposedException(GetType().Name);

        // 背压：数据管道达到暂停水位时暂存接收参数，消费恢复后经 Resumed 事件重启（仍暂停则再次暂存）
        if (_pipe?.IsPaused == true)
        {
            ParkReceive(se);

            return true;
        }

        var ss = _Stream;
        if (ss != null)
        {
            ss.BeginRead(se.Buffer!, se.Offset, se.Count, OnEndRead, se);

            return true;
        }

        return sock.ReceiveAsync(se);
    }

    /// <summary>异步读取数据流，仅用于SSL</summary>
    /// <param name="ar"></param>
    private void OnEndRead(IAsyncResult ar)
    {
        Int32 bytes;
        try
        {
            bytes = _Stream!.EndRead(ar);
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);

            // 读失败统一按对端已关闭（0字节）处理，触发会话断开链路。
            // 仅记日志会让本接收参数既不重投也不释放，SSL 会话无感知悬挂直到超时清理。
            if (ar.AsyncState is SocketAsyncEventArgs args)
            {
                args.SocketError = SocketError.Success;
                ProcessEvent(args, 0, 1);
            }

            return;
        }
        if (ar.AsyncState is SocketAsyncEventArgs se) ProcessEvent(se, bytes, 1);
    }

    //private Int32 _empty;

    /// <summary>预处理</summary>
    /// <param name="pk">数据包</param>
    /// <param name="local">接收数据的本地地址，取自 <see cref="SessionBase.GetReceiveLocalAddress"/>，无包信息时为空</param>
    /// <param name="remote">远程地址</param>
    /// <returns>将要处理该数据包的会话</returns>
    protected internal override ISocketSession? OnPreReceive(IPacket pk, IPAddress? local, IPEndPoint remote)
    {
        if (pk.Length == 0)
        {
            using var span = Tracer?.NewSpan($"net:{Name}:EmptyData", remote?.ToString());

            // 连续多次空数据，则断开
            //if (DisconnectWhenEmptyData && ++_empty >= 3)
            {
                var reason = CheckClosed();
                if (reason != null)
                {
                    Close(reason);
                    Dispose();

                    return null;
                }
            }
        }
        //else
        //    _empty = 0;

        // 流式视图：本轮数据投递共享切片到入站管道（引用计数），不影响轮句柄与后续传统管道处理链
        AppendToPipe(pk);

        return this;
    }

    /// <summary>处理收到的数据</summary>
    /// <param name="e">接收事件参数</param>
    protected override Boolean OnReceive(ReceivedEventArgs e)
    {
        //var pk = e.Packet;
        //if ((pk == null || pk.Count == 0) && e.Message == null && !MatchEmpty) return true;

        // 分析处理
        RaiseReceive(this, e);

        return true;
    }

    #endregion

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
    /// <remarks>会话关闭时复位为 null，再次访问 <see cref="Pipe"/> 会重建新管道</remarks>
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
}
