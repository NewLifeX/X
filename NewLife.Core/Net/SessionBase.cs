using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using NewLife.Data;
using NewLife.Log;
using NewLife.Messaging;
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
    private volatile Boolean _everActive;
    /// <summary>是否活动</summary>
    /// <remarks>
    /// <para>打开成功后置 true；关闭完成后置 false。服务端已接受连接的会话由宿主直接置 true。</para>
    /// <para>关闭流程内可提前置 false（如底层连接已拆除时），用于阻断掉线重连判断。</para>
    /// <para>本属性只表示“传输是否可用”，<b>不代表</b>接收环已经启动，也不代表会话还在服务端集合里；
    /// 服务端会话由宿主在构造时即置 true（表示连接已被接受），因此它的 <see cref="Open()"/> 会幂等短路，不会执行 <see cref="OnOpenAsync(CancellationToken)"/>。</para>
    /// <para>会话对象一次性：曾置 true 的会话在关闭后不可再次打开（见 <see cref="OpenAsync(CancellationToken)"/>），重连请新建会话对象。</para>
    /// </remarks>
    public Boolean Active
    {
        get => _active;
        set
        {
            // 先置“曾经活动”标记再写活动位，保证任何后续读到“已关闭”的调用者都能看到该标记
            if (value) _everActive = true;
            _active = value;
        }
    }

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

    /// <summary>协议编解码器。非空时启用协议模式：数据经数据管道定界，头部到齐即交付消息帧；发送经协议构建整帧</summary>
    /// <remarks>
    /// <para>仅流式会话（TCP 等 <see cref="IStreamSession"/>）支持协议模式，请在打开之前设置。</para>
    /// <para>协议模式：数据经消息泵定界后交付（<see cref="Received"/> 事件）；消息经 <see cref="SendMessage(IMessage)"/> 发送。</para>
    /// </remarks>
    public IMessageCodec? Protocol { get; set; }

    /// <summary>请求-响应匹配队列。协议模式下等待响应时使用，首次等待时自动创建，可注入共享或自定义实现</summary>
    public IMatchQueue? MatchQueue
    {
        get => Matcher.Queue;
        set => Matcher.Queue = value;
    }

    /// <summary>请求-响应匹配等待超时（毫秒）。默认30_000</summary>
    public Int32 MatchTimeout
    {
        get => Matcher.Timeout;
        set => Matcher.Timeout = value;
    }

    /// <summary>响应匹配器。等待登记、匹配交付与关闭清理与传输形态无关；流式会话的消息泵在帧定界交付时调用</summary>
    internal ResponseMatcher Matcher { get; } = new();

    /// <summary>最大并发处理数。协议模式下消息处理并发度：1=串行（默认，同连接依次处理）；大于1=并行派发（兼作并发上限）</summary>
    /// <remarks>
    /// <para>并行派发前会先物化流式消息体（一次拷贝），使消息脱离数据管道独立可用；因此并行要求业务处理器线程安全。</para>
    /// <para>并行下同一连接的多个消息处理顺序不定（SRMP 按序列号配对，天然无顺序依赖）；客户端多路复用并发请求时，服务端设置大于1可提升吞吐。</para>
    /// <para>请在打开之前设置；信号量在首次需要时创建，运行中修改不追溯。</para>
    /// </remarks>
    public Int32 MaxConcurrency { get; set; } = 1;

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
    /// <para>会话对象一次性：一旦活动过（打开成功或服务端接受连接），关闭后不可重新打开，重连请新建会话对象（NetClient 等宿主会自动重建）。</para>
    /// </remarks>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>是否成功</returns>
    /// <exception cref="InvalidOperationException">会话对象曾经活动过且已关闭，不可重新打开</exception>
    public virtual async Task<Boolean> OpenAsync(CancellationToken cancellationToken = default)
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);
        if (Active) return true;
        if (_everActive) throw new InvalidOperationException(L.T("Session objects are single-use: after closing they cannot be reopened. Create a new session to reconnect (hosts such as NetClient recreate them automatically)."));
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

            // 协议模式：每次打开输出一次所挂载的协议编码器；服务端接入会话不经过本路径，不会逐会话输出
            if (Protocol != null) WriteLog("Protocol codec: {0}", Protocol);

            // 触发打开完成的事件（状态已变更）
            Opened?.Invoke(this, EventArgs.Empty);

            // 接收流程的附加动作：流式会话在协议模式下启动消息泵（先于接收环，首个数据到达前就绪）
            StartReceiveFlow();

            // 最后开始接收，避免事件处理阻塞接收初始化；拉取模式（AutoReceive=false）不启动接收环。
            // 打开过程中可能已被关闭（对端违规帧触发协议失败、握手校验失败等），会话一次性，
            // 关闭后再调用 Open 会抛异常，故先确认仍处于活动状态
            if (AutoReceive && Active) StartReceive();
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

            // 接收流程的收尾动作：流式会话先停消息泵（取消挂起读取并等待在途数据交付完成），随后关闭处理器链与会话
            await StopReceiveFlowAsync().ConfigureAwait(false);

            // 取消挂起的请求-响应等待，避免调用方悬挂
            Matcher.Clear();

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
        if (IsReceiving) throw new InvalidOperationException(NoPullMessage);

        return OnDirectReceive();
    }

    /// <summary>拉取模式提示。接收环已启动时拉取数据将被拒绝</summary>
    private String NoPullMessage => L.T("[{0}] The receive loop is running (AutoReceive=true), pulling data is not allowed. Set AutoReceive=false before opening to use pull mode, or consume the Received event or pipe instead.", Name);

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
            try
            {
                var size = sock.Receive(pk.Buffer, SocketFlags.None);
                span?.Value = size;

                return pk.Resize(size);
            }
            catch
            {
                // 读失败（含套接字关闭）时归还池化缓冲，避免异常路径上缓冲流失（发布版无析构兜底）
                pk.TryDispose();
                throw;
            }
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
        if (IsReceiving) throw new InvalidOperationException(NoPullMessage);

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
            try
            {
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
            catch
            {
                // 读失败或取消时归还池化缓冲，避免异常路径上缓冲流失（发布版无析构兜底）
                pk.TryDispose();
                throw;
            }
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

            // 池化接收缓冲：从 ArrayPool 借出即包装为拥有句柄挂到接收槽（UserToken），句柄自此持有本槽缓冲直到释放。
            // 每轮把句柄交给下游，轮末按引用计数裁决：无人持有则回挂接收槽供下轮复用（重设窗口即可，缓冲原地不动，零 Rent/Return），
            // 被下游消费或带出时才解绑换新。归还只发生在释放槽句柄处（见 ReleaseRecv）：引用归零则回池，仍有共享则减数、由最后一个持有者归还。
            // “零 Rent/Return”的前提是未启用入站管道（Pipe）：启用管道后每轮均因共享切片换新缓冲，见轮末裁决处注释。
            // 加大接收缓冲区，规避SocketError.MessageSize问题
            var pk = new OwnerPacket(BufferSize);
            var se = new SocketAsyncEventArgs();
            se.SetBuffer(pk.Buffer, pk.Offset, pk.Length);
            se.Completed += (s, e) => ProcessEvent(e, -1, _IntoThreadCount);
            se.UserToken = new RecvSlot(count, pk);

            if (Log != null && Log.Level <= LogLevel.Debug) WriteLog("Creating RecvSA {0}", count);

            StartReceive(se, 0);
        }

        return true;
    }

    /// <summary>释放一个事件参数。递减接收计数（归零时释放并置空套接字）、释放接收槽缓冲句柄并销毁。幂等：同一个事件参数重复调用只生效一次</summary>
    /// <param name="se">接收事件参数</param>
    /// <param name="reason">释放原因。便于日志分析</param>
    protected void ReleaseRecv(SocketAsyncEventArgs se, String reason)
    {
        // 接收槽是接收环的固有契约：事件参数一律由 StartReceive 创建并挂槽，槽或槽中句柄缺失即契约被破坏，直接抛错
        var slot = se.UserToken as RecvSlot ?? throw new InvalidOperationException(L.T("Receive event arguments are missing a receive slot: [{0}] {1}", Name, reason));

        // 幂等：同一个事件参数只释放一次。重复释放会少计 _RecvCount（IsReceiving 失真，拉取直读的互斥判定失效），
        // 并对同一个 se 二次 Dispose。StartReceive 发现会话已销毁时会先释放再抛，异常冒泡回 ProcessEvent
        // 自己的 catch 会再次调本方法，这是已知会重复进入的路径
        if (slot.Released) return;

        var cached = slot.Packet ?? throw new InvalidOperationException(L.T("Receive slot handle is missing: [{0}] {1}", Name, reason));

        if (Log != null && Log.Level <= LogLevel.Debug) WriteLog("Releasing RecvSA {0} {1}", slot.Index, reason);

        slot.Released = true;
        slot.Packet = null;

        // 计数归零代表接收环彻底停止：此时收口套接字引用，避免留下“环已停、套接字未释放”的半开状态。
        // 先取快照再置空、最后释放；客户端会话重新 Open 时会重建套接字。
        if (_RecvCount > 0 && Interlocked.Decrement(ref _RecvCount) == 0)
        {
            var socket = Client;
            Client = null;
            socket.TryDispose();
        }

        // 缓冲由接收槽句柄持有：释放句柄即完成归还——引用归零时由 ArrayOwner 归还池，
        // 仍被共享切片持有时只减引用，缓冲留给最后一个持有者归还。本方法不再直接归还缓冲。
        try
        {
            cached.Dispose();
        }
        finally
        {
            se.Dispose();
        }
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

        // 同步返回0数据包：仅面向字节流的传输（TCP/Unix域）表示对端关闭，断开连接。
        // 数据报传输（UDP）的 0 字节是合法报文（UdpSession 约定为结束该远端会话），必须按普通收包派发；
        // 否则监听方（UdpServer）会因任意对端发来的一个空数据报而关闭整个服务。
        if (!rs && se.BytesTransferred == 0 && se.SocketError == SocketError.Success && Client is not { SocketType: SocketType.Dgram })
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

    /// <summary>发起一次异步接收。返回 false 表示本次操作同步完成（se 已持有本次完成的状态）</summary>
    /// <remarks>无法发起接收（已停止、关闭窗口内底层引用已清等）时必须抛出异常，严禁返回 false：
    /// 调用方把 false 当作“同步完成”，会按 se 上的陈旧状态重新投递本事件参数（重复派发与空转）</remarks>
    /// <param name="se">接收事件参数</param>
    /// <returns>是否挂起等待异步完成；false 表示已同步完成</returns>
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

                // 同步执行，直接使用数据，不需要拷贝：缓冲自借出即由槽句柄持有，直接交管道与事件消费。
                // 接收环复用：取回挂在接收槽上的轮句柄，把窗口重设到本段数据。
                // 不变量：轮句柄随 se 创建即挂槽（轮末回挂或换新），槽与句柄缺失属契约破坏，直接抛错；
                // 槽句柄非空 ⇒ 它的缓冲就是本槽的 se.Buffer，因此只需重设长度，不需要也不可能换缓冲（换缓冲必须换新句柄）。
                // 代价：不再校验 RefCount——本槽句柄由本槽独占，凡跨轮带出都会先 Slice（轮末即走换新分支、不回挂），
                // 唯一失守点是下游违规留存原句柄（不经 Slice）时会被静默重设窗口而不是报错。
                var slot = se.UserToken as RecvSlot ?? throw new InvalidOperationException(L.T("Receive event arguments are missing a receive slot: [{0}]", Name));
                var pk = slot.Packet ?? throw new InvalidOperationException(L.T("Receive slot handle is missing: [{0}]", Name));
                slot.Packet = null;
                pk.Resize(bytes);

                try
                {
                    ProcessReceive(se, ep, pk);
                }
                finally
                {
                    // 轮末裁决缓冲归属，正常与异常路径一致：
                    // 计数为 1（无他人持有）→ 句柄回挂接收槽供下轮重设窗口复用，缓冲留在会话继续接收，零 Rent/Return；
                    // 其余情况（已被下游消费归零，或存在共享切片大于 1）→ 释放本句柄（已释放则空操作），解绑换新；
                    // 不在此归还：计数大于 1 时旧缓冲仍被外部使用，归还它会把正在使用的缓冲交还给池。
                    // 已启用入站管道（Pipe）时必然走换新分支：AppendToPipe 投递的共享切片使本句柄计数恒大于 1，
                    // 故“零 Rent/Return”复用只发生在未启用管道的会话上（换新为池化往返，实测约 8ns/次，代价可忽略）。
                    if (pk.RefCount == 1)
                    {
                        slot.Packet = pk;
                    }
                    else
                    {
                        pk.TryDispose();

                        // 先解绑再借新：若借新抛出（如 OOM），本槽句柄缺失，后续释放路径抛错暴露，不会把旧缓冲二次归还。
                        // 新缓冲即刻包装为句柄并挂槽（句柄自此持有缓冲）
                        se.SetBuffer(null, 0, 0);
                        var np = new OwnerPacket(BufferSize);
                        se.SetBuffer(np.Buffer, np.Offset, np.Length);
                        slot.Packet = np;
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
        var local = GetReceiveLocalAddress(se);
        using var span = Tracer?.NewSpan($"net:{Name}:ProcessReceive", new { total, local, remote }, total);
        ReceivedEventArgs? e = null;
        try
        {
            LastTime = DateTime.Now;

            // 预处理，得到将要处理该数据包的会话
            var ss = OnPreReceive(pk, local, remote);
            if (ss == null) return;

            if (LogReceive && Log != null && Log.Enable) WriteLog("Recv [{0}]: {1}", total, pk.ToHex(LogDataLength));

            // 数据已被上层接收流程接管（流式会话的协议模式）：已投递数据管道，由消息泵定界交付（头部到齐即出消息）
            if (RawReceiveTaken) return;

            if (Local.IsTcp) remote = Remote.EndPoint;

            e = ReceivedEventArgs.Rent();
            // 本轮拥有句柄：可直接交给应答/发送链路消费（其消费/释放只影响本包装句柄）；跨轮带出请 Slice 切出共享句柄
            e.Packet = pk;
            e.Local = local;
            e.Remote = remote;

            OnReceive(e);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, pk.ToHex());
            if (!ex.IsDisposed()) OnError("OnReceive", ex);
        }
        finally
        {
            // 无论正常或异常，都归还池化对象，避免泄漏（缓冲归属由 ProcessEvent 轮末裁决）
            if (e != null) ReceivedEventArgs.Return(e);
        }
    }

    /// <summary>预处理</summary>
    /// <param name="pk">数据包</param>
    /// <param name="local">接收数据的本地地址，取自 <see cref="SessionBase.GetReceiveLocalAddress"/>，无包信息时为空</param>
    /// <param name="remote">远程地址</param>
    /// <returns>将要处理该数据包的会话</returns>
    protected internal abstract ISocketSession? OnPreReceive(IPacket pk, IPAddress? local, IPEndPoint remote);

    /// <summary>处理收到的数据。默认匹配同步接收委托</summary>
    /// <param name="e">接收事件参数</param>
    /// <returns>是否已处理，已处理的数据不再向下传递</returns>
    protected abstract Boolean OnReceive(ReceivedEventArgs e);

    /// <summary>数据到达事件</summary>
    public event EventHandler<ReceivedEventArgs>? Received;

    /// <summary>把会话收到的数据/消息升格到本服务器层（内部）。协议模式下由会话内的消息路径调用</summary>
    /// <param name="sender">事件源（会话）</param>
    /// <param name="e">接收事件参数</param>
    internal void RaiseReceiveInternal(Object sender, ReceivedEventArgs e) => RaiseReceive(sender, e);

    /// <summary>触发数据到达事件</summary>
    /// <param name="sender"></param>
    /// <param name="e">接收事件参数</param>
    protected virtual void RaiseReceive(Object sender, ReceivedEventArgs e) => Received?.Invoke(sender, e);

    /// <summary>收到异常时如何处理。默认关闭会话</summary>
    /// <param name="se"></param>
    /// <returns>是否当作异常处理并结束会话。true 会让 ProcessEvent 释放本接收槽并结束该槽的接收环，false 则重投本槽继续收</returns>
    internal virtual Boolean OnReceiveError(SocketAsyncEventArgs se)
    {
        //if (se.SocketError == SocketError.ConnectionReset) Dispose();
        // 面向连接的会话（TCP/Unix 域）任何接收错误都说明连接不可用，必须关闭会话让上层收到 Closed。
        // 只对 ConnectionReset 关闭会让会话半死：Active 仍为 true、不再收数据、也不触发 Closed，
        // 上层（如 NetClient 的自动重连）永远等不到断线信号。
        // 数据报（UDP）不适用：单个数据报出错不代表远端不可达，由 UdpServer 重投接收槽继续收
        if (Client is { SocketType: SocketType.Stream } && !Disposed)
            Close(se.SocketError == SocketError.ConnectionReset ? "ConnectionReset" : "ReceiveError " + se.SocketError);

        // 返回值必须与“会话是否真的结束”一致：接收槽在启动时按 MaxAsync 创建一次、销毁后不重建，
        // 对没关掉的会话返回 true 会变成“连接没关、接收槽没了”（数据报会话、Client 缺失、Close 失败）
        return Disposed || !Active;
    }

    /// <summary>启动接收处理。接收环启动之前调用，供子类做接收流程的附加准备（如流式会话启动消息泵）</summary>
    /// <remarks>调用方须保证时序：先于 <see cref="StartReceive()"/>，且每次会话生命周期至多一次</remarks>
    protected virtual void StartReceiveFlow() { }

    /// <summary>停止接收处理。关闭流程开头调用，供子类做接收流程的收尾（如流式会话停止消息泵）</summary>
    protected virtual void StopReceiveFlow() { }

    /// <summary>停止接收处理（异步）。关闭流程开头调用，供子类做接收流程的收尾</summary>
    /// <remarks>
    /// 默认转发同步 <see cref="StopReceiveFlow"/>。需要先把“已读出但尚未交付”的在途数据交付完成再关闭的流式会话
    /// （如消息泵）重写本方法：关闭流程在它返回后才推进到关闭处理器链与会话，从而保证在途数据先于关闭交付给上层。
    /// </remarks>
    /// <returns>收尾任务</returns>
    protected virtual Task StopReceiveFlowAsync()
    {
        StopReceiveFlow();

        return TaskEx.CompletedTask;
    }

    /// <summary>本轮原始数据是否已被上层接收流程接管。true 时接收环不再把原始字节抛给事件</summary>
    /// <remarks>流式会话的协议模式为 true（数据经数据管道定界后由消息泵交付）；数据报会话与拉取模式为 false</remarks>
    protected virtual Boolean RawReceiveTaken => false;

    /// <summary>本轮数据的本地地址</summary>
    /// <remarks>默认取接收包信息（`ReceiveMessageFromAsync` 路径）。改用 `ReceiveFromAsync` 的接收环没有包信息，由子类提供等价来源</remarks>
    /// <param name="se">接收事件参数</param>
    /// <returns>本地地址；无包信息时返回 null</returns>
    protected virtual IPAddress? GetReceiveLocalAddress(SocketAsyncEventArgs se) => se.ReceiveMessageFromPacketInfo.Address;

    #endregion 接收

    #region 消息处理

    /// <summary>发送消息。经协议（<see cref="Protocol"/>）构建整帧后发送，不等待响应</summary>
    /// <param name="message">消息</param>
    /// <returns>发送字节数</returns>
    /// <exception cref="InvalidOperationException">未设置协议</exception>
    public virtual Int32 SendMessage(IMessage message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        var codec = Protocol ?? throw new InvalidOperationException($"Protocol not set for session [{Name}]");

        using var span = Tracer?.NewSpan($"net:{Name}:SendMessage", message);
        try
        {
            var data = codec.Build(message);
            if (data == null) return 0;

            try
            {
                return Send(data);
            }
            finally
            {
                // 发送为借阅消费语义：直发写完才返回，构建产物由本层归还（拥有句柄）
                data.TryDispose();
            }
        }
        catch (Exception ex)
        {
            span?.SetError(ex, message);
            throw;
        }
    }

    /// <summary>发送消息并等待匹配的响应（协议模式）。请求入匹配队列，响应到达时完成</summary>
    /// <param name="request">请求消息</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>响应消息；调用方负责消费负载并释放（<see cref="IDisposable.Dispose"/> 或读满负载）</returns>
    /// <exception cref="InvalidOperationException">未设置协议</exception>
    /// <exception cref="NotSupportedException">协议未实现请求-响应配对（<see cref="IMessageMatcher"/>）</exception>
    /// <remarks>
    /// <para>所有响应消息先进入 <see cref="Received"/> 事件链（可观测），随后命中配对的交付本等待方；未命中的按普通消息处理。配对语义由协议的 <see cref="IMessageMatcher.Match"/> 决定。</para>
    /// <para>交付的响应消息体为内存模式（流式负载在事件交付前物化），可任意异步消费；等待超时为 <see cref="MatchTimeout"/>。</para>
    /// </remarks>
    public virtual ValueTask<IMessage> SendMessageAsync(IMessage request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var codec = Protocol ?? throw new InvalidOperationException($"Protocol not set for session [{Name}]");
        if (ResponseMatcher.Resolve(codec) == null) throw new NotSupportedException(L.T("Protocol [{0}] does not implement request/response matching (IMessageMatcher); cannot wait for a response.", codec.GetType().Name));
        if (this is not IStreamSession) throw new NotSupportedException(L.T("Session type [{0}] does not support response-matched delivery and cannot wait for a response (use UdpServer/UdpSession for datagram sessions).", GetType().Name));

        var span = Tracer?.NewSpan($"net:{Name}:SendMessageAsync", request);
        var source = Matcher.Register(this, request, span);

        try
        {
            SendMessage(request);
        }
        catch (Exception ex)
        {
            // 请求未能入队或送出：异常交给等待方（await 时抛出），资源随 GetResult 归还
            source.TrySetException(ex);
        }

        source.RegisterCancellation(cancellationToken);
        return source.ValueTask;
    }

    /// <summary>协议模式的响应等待包装（Object 版返回）</summary>
    /// <param name="message">请求消息</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>响应消息</returns>
    private async ValueTask<Object> SendMessageAsyncForObject(IMessage message, CancellationToken cancellationToken)
        => await SendMessageAsync(message, cancellationToken).ConfigureAwait(false);

    /// <summary>发送消息，不等待响应。经协议（<see cref="Protocol"/>）构建整帧后发送</summary>
    /// <param name="message">消息对象（须实现 <see cref="IMessage"/>）</param>
    /// <returns>发送字节数</returns>
    /// <exception cref="InvalidOperationException">未设置协议或消息类型不支持</exception>
    public virtual Int32 SendMessage(Object message)
    {
        // 协议模式：消息经协议构建整帧发送
        if (Protocol != null && message is IMessage msg) return SendMessage(msg);

        throw new InvalidOperationException($"Protocol not set or message is not IMessage for session [{Name}]");
    }

    /// <summary>发送消息并等待响应（协议模式）。经协议构建整帧发送，收到匹配响应后交付消息本体</summary>
    /// <param name="message">请求消息（须实现 <see cref="IMessage"/>）</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>响应消息</returns>
    /// <exception cref="InvalidOperationException">未设置协议或消息类型不支持</exception>
    public virtual ValueTask<Object> SendMessageAsync(Object message, CancellationToken cancellationToken = default)
    {
        // 协议模式：消息经协议构建并等待匹配响应（交付消息本体）
        if (Protocol != null && message is IMessage msg) return SendMessageAsyncForObject(msg, cancellationToken);

        throw new InvalidOperationException($"Protocol not set or message is not IMessage for session [{Name}]");
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
        Log?.Error("{0}{1}Error {2} {3}", LogPrefix, action, this, ex.Message);
        Error?.Invoke(this, new ExceptionEventArgs(action, ex));
    }

    #endregion 异常处理

    #region 扩展接口
    private ConcurrentDictionary<String, Object?>? _items;

    /// <summary>数据项。首次访问时创建</summary>
    /// <remarks>并发首用时以 CAS 保证只保留一份实例：旧实现用 ??= 可能各自新建，败者写入的数据会随之被丢弃</remarks>
    public IDictionary<String, Object?> Items
    {
        get
        {
            var items = _items;
            if (items != null) return items;

            var created = new ConcurrentDictionary<String, Object?>();

            return Interlocked.CompareExchange(ref _items, created, null) ?? created;
        }
    }

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

/// <summary>接收槽状态：槽序号 + 持有本槽缓冲的数据包句柄</summary>
/// <param name="index">槽序号（第几个接收事件参数）</param>
/// <param name="packet">本槽缓冲的拥有句柄，随缓冲借出时创建；释放槽句柄即归还缓冲</param>
internal sealed class RecvSlot(Int32 index, OwnerPacket packet)
{
    /// <summary>槽序号（第几个接收事件参数）</summary>
    public Int32 Index { get; } = index;

    /// <summary>本槽缓冲的拥有句柄。无外部持有者时挂在本槽供下轮重设窗口复用；释放本句柄即归还缓冲</summary>
    public OwnerPacket? Packet { get; set; } = packet;

    /// <summary>是否已释放。用于释放幂等：同一个接收事件参数只释放一次</summary>
    public Boolean Released { get; set; }
}
