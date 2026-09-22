using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;

namespace NewLife.Net;

/// <summary>会话基类。TCP/UDP 共用的报文端点核心：连接生命周期、收发原语、SAEA 接收环与每轮缓冲所有权、消息管道</summary>
/// <remarks>
/// <para>封装了Socket客户端和服务端会话的基础功能，包括连接管理、数据收发、消息处理等。</para>
/// <para>设计理念：</para>
/// <list type="bullet">
/// <item>异步优先 - 所有IO操作优先使用异步方式</item>
/// <item>事件驱动 - 数据接收通过事件通知</item>
/// <item>消息管道 - 支持灵活的消息编解码管道</item>
/// <item>对象池化 - 上下文对象池化减少GC压力</item>
/// </list>
/// </remarks>
public abstract class SessionBase : DisposeBase, ISocketClient, ITransport, ILogFeature
{
    #region 属性

    /// <summary>会话标识</summary>
    /// <remarks>用于在多会话环境中唯一标识当前会话</remarks>
    public Int32 ID { get; internal set; }

    /// <summary>名称</summary>
    /// <remarks>主要用于日志输出，默认为类名</remarks>
    public String Name { get; set; }

    /// <summary>本地绑定信息</summary>
    /// <remarks>指定Socket绑定的本地网络地址</remarks>
    public NetUri Local { get; set; } = new NetUri();

    /// <summary>端口</summary>
    /// <remarks>本地监听或绑定的端口号</remarks>
    public Int32 Port { get => Local.Port; set => Local.Port = value; }

    /// <summary>远程结点地址</summary>
    /// <remarks>发送数据的目标地址</remarks>
    public NetUri Remote { get; set; } = new NetUri();

    /// <summary>超时时间（毫秒）</summary>
    /// <remarks>连接、发送、接收操作的超时时间，默认3000ms</remarks>
    public Int32 Timeout { get; set; } = 3_000;

    private volatile Boolean _active;
    /// <summary>是否活动</summary>
    /// <remarks>
    /// <para>打开成功后置 true；关闭完成后置 false。服务端已接受连接的会话由宿主直接置 true。</para>
    /// <para>关闭流程内可提前置 false（如底层连接已拆除时），用于阻断掉线重连判断。</para>
    /// </remarks>
    public Boolean Active { get => _active; set => _active = value; }

    /// <summary>底层Socket</summary>
    /// <remarks>底层的Socket实例，可用于高级操作</remarks>
    public Socket? Client { get; protected set; }

    /// <summary>最后一次通信时间</summary>
    /// <remarks>主要表示活跃时间，包括收发操作</remarks>
    public DateTime LastTime { get; internal protected set; } = DateTime.Now;

    /// <summary>自动接收。为 true 时打开后自动启动接收环进入事件模式（不允许拉取数据）；为 false 时只允许同步/异步拉取</summary>
    /// <remarks>
    /// <para>默认 true，请在打开之前设置，打开后修改不影响已启动的接收环。</para>
    /// <para>接收环运行期间调用 <see cref="Receive()"/> 或 <see cref="ReceiveAsync(CancellationToken)"/> 将抛出异常。</para>
    /// </remarks>
    public Boolean AutoReceive { get; set; } = true;

    /// <summary>最大并行接收数。接收环并发待收数量，默认1</summary>
    internal Int32 MaxReceiveCount { get; set; } = 1;

    /// <summary>缓冲区大小</summary>
    /// <remarks>接收缓冲区大小，默认使用全局配置</remarks>
    public Int32 BufferSize { get; set; }

    /// <summary>连接关闭原因</summary>
    /// <remarks>记录连接关闭的原因，便于日志分析</remarks>
    public String? CloseReason { get; set; }

    /// <summary>APM性能追踪器</summary>
    /// <remarks>用于记录关键操作的性能追踪</remarks>
    public ITracer? Tracer { get; set; }

    #endregion 属性

    #region 构造

    /// <summary>实例化会话基类</summary>
    /// <remarks>初始化默认名称、缓冲区大小和日志数据长度</remarks>
    public SessionBase()
    {
        Name = GetType().Name;

        BufferSize = SocketSetting.Current.BufferSize;
        LogDataLength = SocketSetting.Current.LogDataLength;
    }

    /// <summary>销毁资源</summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        var reason = GetType().Name + (disposing ? "Dispose" : "GC");

        try
        {
            Close(reason);
        }
        catch (Exception ex)
        {
            OnError("Dispose", ex);
        }
    }

    /// <summary>已重载。返回本地地址字符串</summary>
    /// <returns>本地地址字符串</returns>
    public override String ToString() => Local + "";

    #endregion 构造

    #region 打开关闭

    /// <summary>打开。同步桥接</summary>
    /// <remarks>转发 <see cref="OpenAsync(CancellationToken)"/> 同步阻塞等待；并发调用不做排队，已打开直接成功</remarks>
    /// <returns>是否成功</returns>
    public Boolean Open() => OpenAsync().GetAwaiter().GetResult();

    /// <summary>打开</summary>
    /// <remarks>
    /// <para>已打开直接成功；并发调用不做排队，入口检查与置位之间到达的调用可能重复执行打开过程（调用方应避免并发打开）。</para>
    /// <para>打开过程由 <paramref name="cancellationToken"/> 约束，异常原样抛出。</para>
    /// </remarks>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>是否成功</returns>
    public virtual async Task<Boolean> OpenAsync(CancellationToken cancellationToken = default)
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);
        if (Active) return true;
        if (cancellationToken.IsCancellationRequested) return false;

        using var span = Tracer?.NewSpan($"net:{Name}:Open", Remote?.ToString());
        try
        {
            _RecvCount = 0;

            var rs = await OnOpenAsync(cancellationToken).ConfigureAwait(false);
            if (!rs) return false;

            // 打开完成瞬间恰逢销毁：释放刚建立的连接，避免留下僵尸会话与句柄泄漏
            if (Disposed)
            {
                Client.TryDispose();
                Client = null;
                return false;
            }

            var timeout = Timeout;
            if (timeout > 0 && Client is { } sock)
            {
                sock.SendTimeout = timeout;
                sock.ReceiveTimeout = timeout;
            }

            Active = true;

            if (Pipeline is Pipeline pipe && pipe.Handlers.Count > 0)
            {
                WriteLog("初始化管道：");
                foreach (var handler in pipe.Handlers)
                {
                    WriteLog("    {0}", handler);
                }
            }

            if (Pipeline != null)
            {
                // 使用上下文池调用Open
                var ctx = CreateContext(this);
                Pipeline.Open(ctx);
                ReturnContext(ctx);
            }

            // 触发打开完成的事件（状态已变更，管道已打开）
            Opened?.Invoke(this, EventArgs.Empty);

            // 最后开始接收，避免事件处理阻塞接收初始化；拉取模式（AutoReceive=false）不启动接收环
            if (AutoReceive) StartReceive();
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }

        return true;
    }

    /// <summary>打开</summary>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    [MemberNotNullWhen(true, nameof(Client))]
    protected abstract Task<Boolean> OnOpenAsync(CancellationToken cancellationToken);

    /// <summary>关闭。同步桥接</summary>
    /// <remarks>转发 <see cref="CloseAsync(String, CancellationToken)"/> 同步阻塞等待；未活动时直接成功</remarks>
    /// <param name="reason">关闭原因。便于日志分析</param>
    /// <returns>是否成功</returns>
    public Boolean Close(String reason) => CloseAsync(reason).GetAwaiter().GetResult();

    /// <summary>关闭</summary>
    /// <remarks>
    /// <para>未活动（无连接）时直接成功；并发关闭不做排队（调用方应避免并发关闭）。</para>
    /// <para>关闭过程由 <paramref name="cancellationToken"/> 约束，异常原样抛出。</para>
    /// </remarks>
    /// <param name="reason">关闭原因。便于日志分析</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>是否成功</returns>
    public virtual async Task<Boolean> CloseAsync(String reason, CancellationToken cancellationToken = default)
    {
        // 无连接：无需关闭
        if (!Active) return true;
        if (cancellationToken.IsCancellationRequested) return false;

        using var span = Tracer?.NewSpan($"net:{Name}:Close", Remote?.ToString());
        try
        {
            CloseReason = reason;

            if (Pipeline != null)
            {
                // 使用上下文池调用Close
                var ctx = CreateContext(this);
                Pipeline.Close(ctx, reason);
                ReturnContext(ctx);
            }

            var rs = await OnCloseAsync(reason ?? (GetType().Name + "Close"), cancellationToken).ConfigureAwait(false);

            _RecvCount = 0;

            // 关闭成功后更新状态，然后再触发关闭事件，确保事件观察到最终状态
            if (rs) Active = false;

            // 触发关闭完成的事件
            Closed?.Invoke(this, EventArgs.Empty);

            return rs;
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    /// <summary>关闭</summary>
    /// <param name="reason">关闭原因。便于日志分析</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    protected abstract Task<Boolean> OnCloseAsync(String reason, CancellationToken cancellationToken);

    Boolean ITransport.Close() => Close("TransportClose");

    /// <summary>检查连接是否已关闭，并返回关闭原因，主要检测FIN/RST</summary>
    /// <returns></returns>
    protected String? CheckClosed()
    {
        var sock = Client;
        if (sock == null || !sock.Connected) return "Disconnected";

        try
        {
            if (sock.Poll(10, SelectMode.SelectRead))
            {
                try
                {
                    // 收到FIN标记
#if NETFRAMEWORK || NETSTANDARD2_0
                    var buffer = new Byte[1];
#else
                    Span<Byte> buffer = stackalloc Byte[1];
#endif
                    if (sock.Receive(buffer, SocketFlags.Peek) == 0) return "Finish";
                }
                catch (SocketException ex)
                {
                    return ex.SocketErrorCode.ToString();
                }
            }
        }
        catch (SocketException ex)
        {
            return ex.SocketErrorCode.ToString();
        }
        catch
        {
            // 其它异常不视为关闭
        }

        return null;
    }

    /// <summary>打开后触发。</summary>
    public event EventHandler? Opened;

    /// <summary>关闭后触发。可实现掉线重连</summary>
    public event EventHandler? Closed;

    #endregion 打开关闭

    #region 发送
    /// <summary>直接发送数据包 Byte[]/Packet</summary>
    /// <remarks>目标地址由<seealso cref="Remote"/>决定</remarks>
    /// <param name="data">数据包</param>
    /// <returns>是否成功</returns>
    public Int32 Send(IPacket data)
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);
        if (!Open()) return -1;

        return OnSend(data);
    }

    /// <summary>发送数据</summary>
    /// <remarks>目标地址由<seealso cref="Remote"/>决定</remarks>
    /// <param name="data">数据包</param>
    /// <returns>是否成功</returns>
    protected abstract Int32 OnSend(IPacket data);

    /// <summary>直接发送数据包 Byte[]/Packet</summary>
    /// <remarks>目标地址由<seealso cref="Remote"/>决定</remarks>
    /// <param name="data">字节数组</param>
    /// <param name="offset">偏移</param>
    /// <param name="count">字节数</param>
    /// <returns>是否成功</returns>
    public Int32 Send(Byte[] data, Int32 offset = 0, Int32 count = -1)
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);
        if (!Open()) return -1;

        // 全部发送
        if (count < 0) count = data.Length - offset;

#if NET6_0_OR_GREATER
        return OnSend(new ReadOnlySpan<Byte>(data, offset, count));
#else
        return OnSend(new ArraySegment<Byte>(data, offset, count));
#endif
    }

    /// <summary>直接发送数据包 Byte[]/Packet</summary>
    /// <remarks>目标地址由<seealso cref="Remote"/>决定</remarks>
    /// <param name="data">数据包</param>
    /// <returns>是否成功</returns>
    public Int32 Send(ArraySegment<Byte> data)
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);
        if (!Open()) return -1;

        return OnSend(data);
    }

    /// <summary>发送数据</summary>
    /// <remarks>目标地址由<seealso cref="Remote"/>决定</remarks>
    /// <param name="data">数据包</param>
    /// <returns>是否成功</returns>
    protected abstract Int32 OnSend(ArraySegment<Byte> data);

    /// <summary>直接发送数据包 Byte[]/Packet</summary>
    /// <remarks>目标地址由<seealso cref="Remote"/>决定</remarks>
    /// <param name="data">数据包</param>
    /// <returns>是否成功</returns>
    public Int32 Send(ReadOnlySpan<Byte> data)
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);
        if (!Open()) return -1;

        return OnSend(data);
    }

    /// <summary>发送数据</summary>
    /// <remarks>目标地址由<seealso cref="Remote"/>决定</remarks>
    /// <param name="data">数据包</param>
    /// <returns>是否成功</returns>
    protected abstract Int32 OnSend(ReadOnlySpan<Byte> data);

    #endregion 发送

    #region 接收

    /// <summary>同步拉取数据。直接读取Socket，仅在接收环未运行时可用</summary>
    /// <remarks>
    /// <para>拉取模式（<see cref="AutoReceive"/> = false）下独占直读；事件模式下若接收环已运行，将抛出异常。</para>
    /// <para>该方法会阻塞当前线程直到有数据到达或连接关闭。</para>
    /// </remarks>
    /// <returns></returns>
    public virtual IOwnerPacket? Receive()
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);

        if (!Open() || Client == null) return null;

        // 接收环运行时禁止拉取：两条读路径会争抢同一链路，数据被分流且不可预期
        if (_RecvCount > 0) throw new InvalidOperationException(NoPullMessage);

        return OnDirectReceive();
    }

    /// <summary>拉取模式提示。接收环已启动时拉取数据将被拒绝</summary>
    private String NoPullMessage => $"[{Name}] 接收环已启动（AutoReceive=true），不允许拉取数据；请在打开前设置 AutoReceive=false 使用拉取模式，或改用 Received 事件/管道接收数据";

    /// <summary>直读数据。子类可重写以适配特殊链路（如SSL流）</summary>
    /// <returns></returns>
    protected virtual IOwnerPacket? OnDirectReceive()
    {
        using var span = Tracer?.NewSpan($"net:{Name}:Receive");
        try
        {
            var sock = Client;
            if (sock == null) return null;

            var pk = new OwnerPacket(BufferSize);
            var size = sock.Receive(pk.Buffer, SocketFlags.None);
            span?.Value = size;

            return pk.Resize(size);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    /// <summary>异步拉取数据。直接读取Socket，仅在接收环未运行时可用</summary>
    /// <remarks>拉取模式（<see cref="AutoReceive"/> = false）下独占直读；事件模式下若接收环已运行，将抛出异常。</remarks>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public virtual async Task<IOwnerPacket?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);

        if (!Open() || Client == null) return null;

        // 接收环运行时禁止拉取：两条读路径会争抢同一链路，数据被分流且不可预期
        if (_RecvCount > 0) throw new InvalidOperationException(NoPullMessage);

        return await OnDirectReceiveAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>异步直读数据。子类可重写以适配特殊链路（如SSL流）</summary>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    protected virtual async Task<IOwnerPacket?> OnDirectReceiveAsync(CancellationToken cancellationToken = default)
    {
        using var span = Tracer?.NewSpan($"net:{Name}:ReceiveAsync", BufferSize + "");
        try
        {
            // 快照 Client：关闭路径会并发置空该属性，Begin/EndReceive 分支的 EndReceive 在 await 续体里执行，重读可能得到 null
            var sock = Client;
            if (sock == null) return null;

            var pk = new OwnerPacket(BufferSize);
#if NETFRAMEWORK || NETSTANDARD2_0
            var ar = sock.BeginReceive(pk.Buffer, 0, pk.Length, SocketFlags.None, null, sock);
            var size = ar.IsCompleted ?
                sock.EndReceive(ar) :
                await Task.Factory.FromAsync(ar, sock.EndReceive).ConfigureAwait(false);
#else
            var size = await sock.ReceiveAsync(pk.GetMemory(), SocketFlags.None, cancellationToken).ConfigureAwait(false);
#endif
            span?.Value = size;

            return pk.Resize(size);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    /// <summary>当前异步接收个数</summary>
    private volatile Int32 _RecvCount;

    /// <summary>接收环是否运行中。拉取模式判据：接收环运行时不允许多路径直读同一Socket</summary>
    internal Boolean IsReceiving => _RecvCount > 0;

    /// <summary>开始接收环。确保异步接收运行，数据在事件中返回</summary>
    /// <remarks>调用后进入事件模式；此后 <see cref="Receive()"/> 与 <see cref="ReceiveAsync(CancellationToken)"/> 将抛出异常</remarks>
    /// <returns>是否成功</returns>
    public virtual Boolean StartReceive()
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);

        if (!Open()) return false;

        var count = _RecvCount;
        var max = MaxReceiveCount;
        if (count >= max) return false;

        // 按照最大并发创建异步委托
        for (var i = count; i < max; i++)
        {
            count = Interlocked.Increment(ref _RecvCount);
            if (count > max)
            {
                Interlocked.Decrement(ref _RecvCount);
                return false;
            }

            // 池化接收缓冲：从 ArrayPool 借出，归本会话持有。每轮把整块缓冲包装为本轮拥有句柄上抛，
            // 轮末按引用计数裁决：无人持有则句柄回挂接收槽（UserToken）供下轮重绑复用，缓冲继续接收（零 Rent/Return），
            // 被下游消费或带出时才解绑换新。归还见 ReleaseRecv 与 OwnerPacket.Detach/Rebind 协议。
            // 加大接收缓冲区，规避SocketError.MessageSize问题
            var buf = ArrayPool<Byte>.Shared.Rent(BufferSize);
            var se = new SocketAsyncEventArgs();
            se.SetBuffer(buf, 0, BufferSize);
            se.Completed += (s, e) => ProcessEvent(e, -1, _IntoThreadCount);
            se.UserToken = new RecvSlot(count);

            if (Log != null && Log.Level <= LogLevel.Debug) WriteLog("创建RecvSA {0}", count);

            StartReceive(se, 0);
        }

        return true;
    }

    /// <summary>释放一个事件参数。递减接收计数、归还池化缓冲并销毁</summary>
    /// <param name="se">接收事件参数</param>
    /// <param name="reason">释放原因。便于日志分析</param>
    protected void ReleaseRecv(SocketAsyncEventArgs se, String reason)
    {
        var idx = (se.UserToken as RecvSlot)?.Index ?? -1;

        if (Log != null && Log.Level <= LogLevel.Debug) WriteLog("释放RecvSA {0} {1}", idx, reason);

        if (_RecvCount > 0) Interlocked.Decrement(ref _RecvCount);
        try
        {
            // 接收槽回挂的复用句柄先脱手（不归还），抑制析构兜底，避免与本次归还将同一缓冲二次放回池
            if (se.UserToken is RecvSlot slot && slot.Packet != null)
            {
                var cached = slot.Packet;
                slot.Packet = null;
                cached.Detach();
            }

            // 归还池化接收缓冲。缓冲要么无人带出（轮末已回挂复用，仍在 se.Buffer 上），
            // 要么本轮被消费/带出时已解绑并换了新缓冲；因此这里归还的一定是本会话独有、
            // 无外部持有者的缓冲，恰好一次；被带出的缓冲由最后释放的共享句柄归还。
            var buffer = se.Buffer;
            se.SetBuffer(null, 0, 0);
            if (buffer != null) ArrayPool<Byte>.Shared.Return(buffer);
        }
        catch { }
        se.Dispose();
    }

    /// <summary>当前进入线程递归数量，超过10就另外起线程</summary>
    protected readonly static Int32 _IntoThreadCount = 10;

    /// <summary>用一个事件参数来开始异步接收</summary>
    /// <param name="se">事件参数</param>
    /// <param name="ioThread">是否在线程池调用,小于等于0不是，大于0是</param>
    /// <returns></returns>
    protected Boolean StartReceive(SocketAsyncEventArgs se, Int32 ioThread)
    {
        if (Disposed)
        {
            ReleaseRecv(se, "Disposed " + se.SocketError);

            throw new ObjectDisposedException(GetType().Name);
        }

        var rs = false;
        try
        {
            // 开始新的监听
            rs = OnReceiveAsync(se);
        }
        catch (Exception ex)
        {
            ReleaseRecv(se, "ReceiveAsyncError " + ex.Message);

            if (!ex.IsDisposed())
            {
                OnError("ReceiveAsync", ex);

                // 异常一般是网络错误，UDP不需要关闭
                //if (!io && ThrowException) throw;
            }
            return false;
        }

        // 同步返回0数据包，断开连接
        if (!rs && se.BytesTransferred == 0 && se.SocketError == SocketError.Success)
        {
            var reason = CheckClosed() ?? "EmptyData";
            Close(reason);
            // 本事件参数不再用于接收，立即归还缓冲
            ReleaseRecv(se, reason);
            Dispose();

            return false;
        }

        // 如果当前就是异步线程，直接处理，否则需要开任务处理，不要占用主线程
        if (!rs)
        {
            if (ioThread-- > 0)
            {
                ProcessEvent(se, -1, ioThread);
            }
            else
            {
                ThreadPool.UnsafeQueueUserWorkItem(s =>
                {
                    try
                    {
                        if (s is SocketAsyncEventArgs ee) ProcessEvent(ee, -1, _IntoThreadCount);
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteException(ex);
                    }
                }, se);
            }
        }

        return true;
    }

    internal abstract Boolean OnReceiveAsync(SocketAsyncEventArgs se);

    /// <summary>同步或异步收到数据</summary>
    /// <remarks>
    /// ioThread:
    /// 如果在StartReceive的时候线程池调用ProcessEvent，则处于worker线程；
    /// 如果在IOCP的时候调用ProcessEvent，则处于completionPort线程。
    /// </remarks>
    /// <param name="se"></param>
    /// <param name="bytes"></param>
    /// <param name="ioThread">是否在IO线程池里面</param>
    protected internal void ProcessEvent(SocketAsyncEventArgs se, Int32 bytes, Int32 ioThread)
    {
        try
        {
            if (!Active)
            {
                ReleaseRecv(se, "!Active " + se.SocketError);
                return;
            }

            // 判断成功失败
            if (se.SocketError != SocketError.Success)
            {
                // 未被关闭Socket时，可以继续使用
                if (OnReceiveError(se))
                {
                    var ex = se.GetException();
                    if (ex != null) OnError("ReceiveAsync", ex);

                    ReleaseRecv(se, "SocketError " + se.SocketError);

                    return;
                }
            }
            else
            {
                var ep = se.RemoteEndPoint as IPEndPoint ?? Remote.EndPoint;
                if (bytes < 0) bytes = se.BytesTransferred;
                if (se.Buffer != null)
                {
                    // 同步执行，直接使用数据，不需要拷贝：整块缓冲包装为本轮拥有句柄，交由管道与事件消费。
                    // 接收环复用：优先取回挂在接收槽上的上一轮句柄，重绑到本段数据；无则新建。
                    var slot = se.UserToken as RecvSlot;
                    var pk = slot?.Packet;
                    if (pk != null)
                    {
                        slot!.Packet = null;
                        pk.Rebind(se.Buffer, se.Offset, bytes);
                    }
                    else
                    {
                        pk = new OwnerPacket(se.Buffer, se.Offset, bytes, true);
                    }

                    try
                    {
                        ProcessReceive(se, ep, pk);
                    }
                    finally
                    {
                        // 轮末裁决缓冲归属，正常与异常路径一致：
                        // 计数为 1（无他人持有）→ 句柄回挂接收槽供下轮重绑复用，缓冲留在会话继续接收，零 Rent/Return；
                        // 其余情况（已被下游消费归零，或存在共享切片大于 1）→ 释放本句柄（已释放则空操作），解绑换新；
                        // 不在此归还：计数大于 1 时旧缓冲仍被外部使用，归还它会把正在使用的缓冲交还给池。
                        if (pk.RefCount == 1)
                        {
                            // 槽缺失（理论不发生）时按旧语义脱手，防止句柄被弃后析构兜底误归还缓冲
                            if (slot != null)
                                slot.Packet = pk;
                            else
                                pk.Detach();
                        }
                        else
                        {
                            pk.TryDispose();

                            // 先解绑再借新：若 Rent 抛出（如 OOM），se.Buffer 保持 null，ReleaseRecv 不会归还，
                            // 避免把已归还池或仍被外部持有的旧缓冲二次归还
                            se.SetBuffer(null, 0, 0);
                            var buf = ArrayPool<Byte>.Shared.Rent(BufferSize);
                            se.SetBuffer(buf, 0, BufferSize);
                        }
                    }
                }
            }

            // 开始新的监听
            if (Active && !Disposed)
                StartReceive(se, ioThread);
            else
                ReleaseRecv(se, "!Active || Disposed");
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);

            try
            {
                // 如果数据处理异常，并且Error处理也抛出异常，则这里可能出错，导致整个接收链毁掉。
                // 但是这个可能性极低
                ReleaseRecv(se, "ProcessEventError " + ex.Message);
                Close("ProcessEventError");
            }
            catch { }

            Dispose();
        }
    }

    /// <summary>接收预处理，粘包拆包</summary>
    /// <remarks>
    /// 本轮数据以拥有句柄（每轮包装）上抛，零拷贝；下游可读、可交给应答/发送链路消费（其消费/释放只影响本包装句柄），
    /// 跨线程/跨 await 带出请在事件内经 <see cref="IPacket.Slice(Int32, Int32)"/> 切出共享句柄（用后 Dispose）。
    /// 缓冲归属由 <see cref="ProcessEvent"/> 在轮末按引用计数统一裁决。
    /// </remarks>
    /// <param name="se">socket异步事件</param>
    /// <param name="remote">远程地址</param>
    /// <param name="pk">本轮数据包装句柄</param>
    private void ProcessReceive(SocketAsyncEventArgs se, IPEndPoint remote, OwnerPacket pk)
    {
        // 打断上下文调用链，这里必须是起点
        DefaultSpan.Current = null;

        var total = pk.Length;
        var local = se.ReceiveMessageFromPacketInfo.Address;
        using var span = Tracer?.NewSpan($"net:{Name}:ProcessReceive", new { total, local, remote }, total);
        ReceivedEventArgs? e = null;
        NetHandlerContext? ctx = null;
        try
        {
            LastTime = DateTime.Now;

            // 预处理，得到将要处理该数据包的会话
            var ss = OnPreReceive(pk, local, remote);
            if (ss == null) return;

            if (LogReceive && Log != null && Log.Enable) WriteLog("Recv [{0}]: {1}", total, pk.ToHex(LogDataLength));

            if (Local.IsTcp) remote = Remote.EndPoint;

            e = ReceivedEventArgs.Rent();
            // 本轮拥有句柄：可直接交给应答/发送链路消费（其消费/释放只影响本包装句柄）；跨轮带出请 Slice 切出共享句柄
            e.Packet = pk;
            e.Local = local;
            e.Remote = remote;

            // 不管Tcp/Udp，都在这使用管道
            var pp = Pipeline;
            if (pp == null)
                OnReceive(e);
            else
            {
                ctx = CreateContext(ss);
                ctx.Data = e;
                ctx.EventArgs = se;

                // 进入管道处理（整轮数据包入口），如果有一个或多个结果通过Finish来处理
                pp.Read(ctx, pk);
            }
        }
        catch (Exception ex)
        {
            span?.SetError(ex, pk.ToHex());
            if (!ex.IsDisposed()) OnError("OnReceive", ex);
        }
        finally
        {
            // 无论正常或异常，都归还池化对象，避免泄漏（缓冲归属由 ProcessEvent 轮末裁决）
            if (ctx != null) ReturnContext(ctx);
            if (e != null) ReceivedEventArgs.Return(e);
        }
    }

    /// <summary>预处理</summary>
    /// <param name="pk">数据包</param>
    /// <param name="local">接收数据的本地地址</param>
    /// <param name="remote">远程地址</param>
    /// <returns>将要处理该数据包的会话</returns>
    protected internal abstract ISocketSession? OnPreReceive(IPacket pk, IPAddress local, IPEndPoint remote);

    /// <summary>处理收到的数据。默认匹配同步接收委托</summary>
    /// <param name="e">接收事件参数</param>
    /// <returns>是否已处理，已处理的数据不再向下传递</returns>
    protected abstract Boolean OnReceive(ReceivedEventArgs e);

    /// <summary>数据到达事件</summary>
    public event EventHandler<ReceivedEventArgs>? Received;

    /// <summary>触发数据到达事件</summary>
    /// <param name="sender"></param>
    /// <param name="e">接收事件参数</param>
    protected virtual void RaiseReceive(Object sender, ReceivedEventArgs e) => Received?.Invoke(sender, e);

    /// <summary>收到异常时如何处理。默认关闭会话</summary>
    /// <param name="se"></param>
    /// <returns>是否当作异常处理并结束会话</returns>
    internal virtual Boolean OnReceiveError(SocketAsyncEventArgs se)
    {
        //if (se.SocketError == SocketError.ConnectionReset) Dispose();
        if (se.SocketError == SocketError.ConnectionReset) Close("ConnectionReset");

        return true;
    }

    #endregion 接收

    #region 消息处理

    /// <summary>消息管道。收发消息都经过管道处理器，进行协议编码解码</summary>
    /// <remarks>
    /// 1，接收数据解码时，从前向后通过管道处理器；
    /// 2，发送数据编码时，从后向前通过管道处理器；
    /// </remarks>
    public IPipeline? Pipeline { get; set; }

    /// <summary>创建上下文</summary>
    /// <param name="session">远程会话</param>
    /// <returns></returns>
    protected internal virtual NetHandlerContext CreateContext(ISocketRemote session)
    {
        // 从池中借用上下文
        var context = NetHandlerContext.Rent();
        context.Pipeline = Pipeline;
        context.Session = session;
        context.Owner = session;

        return context;
    }

    /// <summary>归还上下文到对象池</summary>
    /// <param name="context">上下文</param>
    protected internal virtual void ReturnContext(IHandlerContext? context)
    {
        if (context is NetHandlerContext nhc)
        {
            nhc.Reset();
            NetHandlerContext.Return(nhc);
        }
    }

    /// <summary>通过管道发送消息，不等待响应。管道内对消息进行报文封装处理，最终得到二进制数据进入网卡</summary>
    /// <param name="message">消息</param>
    /// <param name="context">处理器上下文。用于在收包处理链路中复用解码上下文携带的数据</param>
    /// <returns></returns>
    public virtual Int32 SendMessage(Object message, IHandlerContext? context)
    {
        if (context == null) return SendMessage(message);

        if (Pipeline == null) throw new ArgumentNullException(nameof(Pipeline), "No pipes are set");

        using var span = Tracer?.NewSpan($"net:{Name}:SendMessage", message);
        try
        {
            if (span != null && message is ITraceMessage tm && tm.TraceId.IsNullOrEmpty()) tm.TraceId = span.ToString();

            // 复用外部上下文。该上下文通常来自接收链路（池化对象），仅建议在当前同步调用栈内使用。
            // 不覆盖 Owner，避免把接收链路上下文错误绑定到其它会话。
            context.Pipeline ??= Pipeline;

            return (Int32)(Pipeline.Write(context, message) ?? 0);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, message);
            throw;
        }
    }

    /// <summary>通过管道发送消息，不等待响应</summary>
    /// <param name="message">消息</param>
    /// <returns>发送字节数</returns>
    public virtual Int32 SendMessage(Object message)
    {
        if (Pipeline == null) throw new ArgumentNullException(nameof(Pipeline), "No pipes are set");

        using var span = Tracer?.NewSpan($"net:{Name}:SendMessage", message);
        var ctx = CreateContext(this);
        try
        {
            if (span != null && message is ITraceMessage tm && tm.TraceId.IsNullOrEmpty()) tm.TraceId = span.ToString();

            return (Int32)(Pipeline.Write(ctx, message) ?? 0);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, message);
            throw;
        }
        finally
        {
            // 写入完成后归还上下文
            ReturnContext(ctx);
        }
    }

    /// <summary>通过管道发送消息并等待响应</summary>
    /// <param name="message">消息</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>响应消息</returns>
    public virtual ValueTask<Object> SendMessageAsync(Object message, CancellationToken cancellationToken = default)
    {
        if (Pipeline == null) throw new ArgumentNullException(nameof(Pipeline), "No pipes are set");

        // 非异步实现：消除 async 状态机分配，Span 和 CancellationTokenRegistration 由 PooledValueTaskSource.GetResult 自动释放
        var span = Tracer?.NewSpan($"net:{Name}:SendMessageAsync", message);
        var ctx = CreateContext(this);
        try
        {
            if (span != null && message is ITraceMessage tm && tm.TraceId.IsNullOrEmpty()) tm.TraceId = span.ToString();

            var source = PooledValueTaskSource.Rent();
            source.AttachSpan(span);
            ctx["TaskSource"] = source;
            ctx["Span"] = span;

            var rs = (Int32)(Pipeline.Write(ctx, message) ?? 0);

            // 写入完成后立即归还上下文，source已加入匹配队列，不再需要上下文
            ReturnContext(ctx);
            ctx = null;

            if (rs < 0)
            {
                source.TrySetResult(TaskEx.CompletedTask);
                return source.ValueTask;
            }

            // 注册取消令牌，GetResult 中自动释放注册
            source.RegisterCancellation(cancellationToken);

            // 直接返回 ValueTask，零额外分配
            return source.ValueTask;
        }
        catch (Exception ex)
        {
            if (ex is TaskCanceledException)
                span?.AppendTag(ex.Message);
            else
                span?.SetError(ex, null);
            span?.Dispose();

            if (ctx != null) ReturnContext(ctx);
            throw;
        }
    }

    /// <summary>处理数据帧</summary>
    /// <param name="data">数据帧</param>
    void ISocketRemote.Process(IData data)
    {
        // 合并调用链。把当前接收处理消息调用链和消息发送方调用链合并
        var span = DefaultSpan.Current;
        if (span != null && data != null && data.Message is ITraceMessage tm) span.Detach(tm.TraceId);

        if (data is ReceivedEventArgs e) OnReceive(e);
    }

    #endregion 消息处理

    #region 异常处理

    /// <summary>错误发生/断开连接时</summary>
    public event EventHandler<ExceptionEventArgs>? Error;

    /// <summary>触发异常</summary>
    /// <param name="action">动作</param>
    /// <param name="ex">异常</param>
    protected internal virtual void OnError(String action, Exception ex)
    {
        if (Pipeline != null)
        {
            var ctx = CreateContext(this);
            Pipeline.Error(ctx, ex);
            ReturnContext(ctx);
        }

        Log?.Error("{0}{1}Error {2} {3}", LogPrefix, action, this, ex.Message);
        Error?.Invoke(this, new ExceptionEventArgs(action, ex));
    }

    #endregion 异常处理

    #region 扩展接口
    private ConcurrentDictionary<String, Object?>? _items;
    /// <summary>数据项</summary>
    public IDictionary<String, Object?> Items => _items ??= new();

    /// <summary>设置 或 获取 数据项</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Object? this[String key] { get => _items != null && _items.TryGetValue(key, out var obj) ? obj : null; set => Items[key] = value; }
    #endregion

    #region 日志

    /// <summary>日志前缀</summary>
    public virtual String? LogPrefix { get; set; }

    /// <summary>日志对象。禁止设为空对象</summary>
    public ILog Log { get; set; } = Logger.Null;

    /// <summary>是否输出发送日志。默认false</summary>
    public Boolean LogSend { get; set; }

    /// <summary>是否输出接收日志。默认false</summary>
    public Boolean LogReceive { get; set; }

    /// <summary>收发日志数据体长度。默认64</summary>
    public Int32 LogDataLength { get; set; } = 64;

    /// <summary>输出日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object?[] args)
    {
        LogPrefix ??= Name.TrimSuffix("Server", "Session", "Client");
        if (Log != null && Log.Enable) Log.Info($"[{LogPrefix}]{format}", args);
    }

    #endregion 日志
}

/// <summary>接收槽状态：槽序号 + 轮末回挂复用的数据包句柄</summary>
/// <param name="index">槽序号（第几个接收事件参数）</param>
internal sealed class RecvSlot(Int32 index)
{
    /// <summary>槽序号（第几个接收事件参数）</summary>
    public Int32 Index { get; } = index;

    /// <summary>轮末回挂的数据包句柄，下一轮重绑复用；无外部持有者时非空</summary>
    public OwnerPacket? Packet { get; set; }
}