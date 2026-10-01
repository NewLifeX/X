using System.Buffers;
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
    /// <summary>是否活动</summary>
    /// <remarks>
    /// <para>打开成功后置 true；关闭完成后置 false。服务端已接受连接的会话由宿主直接置 true。</para>
    /// <para>关闭流程内可提前置 false（如底层连接已拆除时），用于阻断掉线重连判断。</para>
    /// <para>本属性只表示“传输是否可用”，<b>不代表</b>接收环已经启动，也不代表会话还在服务端集合里；
    /// 服务端会话由宿主在构造时即置 true（表示连接已被接受），因此它的 <see cref="Open()"/> 会幂等短路，不会执行 <see cref="OnOpenAsync(CancellationToken)"/>。</para>
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

    /// <summary>协议编解码器。非空时启用协议模式：数据经数据管道定界，头部到齐即交付消息帧；发送经协议构建整帧</summary>
    /// <remarks>
    /// <para>仅流式会话（TCP 等 <see cref="IStreamSession"/>）支持协议模式，请在打开之前设置。</para>
    /// <para>协议模式：数据经消息泵定界后交付（<see cref="Received"/> 事件）；消息经 <see cref="SendMessage(IMessage)"/> 发送。</para>
    /// </remarks>
    public IMessageCodec? Protocol { get; set; }

    /// <summary>消息泵最大缓存字节数（协议模式下无法定界的残余上限），默认 1M。0 表示不限制</summary>
    /// <remarks>残余达到上限说明对端数据与协议不匹配或已损坏，消息泵随即报错并关闭会话，避免连接僵死</remarks>
    public Int32 MaxCache { get; set; } = 1024 * 1024;

    /// <summary>整帧模式下的单帧长度上限，默认 16M。0 表示不限制</summary>
    /// <remarks>仅 <see cref="RequireFullFrame"/> 为 true 时生效：整帧解析要求整个帧驻留内存，本上限是单连接的内存安全阀</remarks>
    public Int32 MaxFrameSize { get; set; } = 16 * 1024 * 1024;

    /// <summary>消息泵是否要求整帧完整才产出。默认 false（头部到齐即交付，体可为流式）</summary>
    /// <remarks>在消息泵任务上同步消费流式体的实现应重写为 true：否则大帧会在事件内同步等待后续数据，占用线程池线程</remarks>
    protected virtual Boolean RequireFullFrame => false;

    /// <summary>请求-响应匹配队列。协议模式下等待响应时使用，首次等待时自动创建，可注入共享或自定义实现</summary>
    public IMatchQueue? MatchQueue
    {
        get => _matchQueue;
        set => _matchQueue = value;
    }

    private IMatchQueue? _matchQueue;

    /// <summary>请求-响应匹配等待超时（毫秒）。默认30_000</summary>
    public Int32 MatchTimeout { get; set; } = 30_000;

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

        // 只释放、不置空：置空后并发任务的 ??= 会新建“满额”信号量，其 Release 直接抛 SemaphoreFullException。
        // 保留已释放实例，迟到的 Release 得到 ObjectDisposedException，由收尾逻辑按正常时序忽略
        _concurrency?.Dispose();
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

            // 触发打开完成的事件（状态已变更）
            Opened?.Invoke(this, EventArgs.Empty);

            // 协议模式：数据经数据管道定界，启动消息泵（先于接收环，首个数据到达前就绪）
            if (Protocol != null)
            {
                if (AutoReceive) StartMessagePump();
                else WriteLog("协议模式需要自动接收（AutoReceive），拉取模式下消息泵未启动，收到的是原始字节");
            }

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

            // 协议模式：先停消息泵（取消挂起读取），随后关闭处理器链与会话
            StopMessagePump();

            // 取消挂起的请求-响应等待，避免调用方悬挂
            MatchQueue?.Clear();

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
            // 轮末按引用计数裁决：无人持有则句柄回挂接收槽（UserToken）供下轮复用（重设窗口即可，缓冲原地不动，零 Rent/Return），
            // 被下游消费或带出时才解绑换新。归还见 ReleaseRecv 与 OwnerPacket.Detach 协议。
            // “零 Rent/Return”的前提是未启用入站管道（Pipe）：启用管道后每轮均因共享切片换新缓冲，见轮末裁决处注释。
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

    /// <summary>释放一个事件参数。递减接收计数、归还池化缓冲并销毁。幂等：同一个事件参数重复调用只生效一次</summary>
    /// <param name="se">接收事件参数</param>
    /// <param name="reason">释放原因。便于日志分析</param>
    protected void ReleaseRecv(SocketAsyncEventArgs se, String reason)
    {
        // 幂等：同一个事件参数只释放一次。重复释放会少计 _RecvCount（IsReceiving 失真，拉取直读的互斥判定失效），
        // 并对同一个 se 二次 Dispose。StartReceive 发现会话已销毁时会先释放再抛，异常冒泡回 ProcessEvent
        // 自己的 catch 会再次调本方法，这是已知会重复进入的路径
        var slot = se.UserToken as RecvSlot;
        if (slot != null)
        {
            if (slot.Released) return;
            slot.Released = true;
        }

        if (Log != null && Log.Level <= LogLevel.Debug) WriteLog("释放RecvSA {0} {1}", slot?.Index ?? -1, reason);

        if (_RecvCount > 0) Interlocked.Decrement(ref _RecvCount);

        // 缓冲归属：句柄仍被共享切片持有（RefCount 大于 1）或挂着链式后续段时，本句柄不能脱手（Detach 会抛），
        // 也不能把仍被使用的缓冲归还池（回池后会被立即借给别的会话）——只释放本句柄自己的引用，
        // 缓冲留给最后一个共享句柄归还
        var skipReturn = false;
        try
        {
            if (slot?.Packet is { } cached)
            {
                slot.Packet = null;

                if (cached.RefCount > 1 || cached.Next != null)
                {
                    cached.TryDispose();
                    skipReturn = true;
                }
                else
                {
                    // 独占缓冲：先脱手（抑制析构兜底，避免与本次归还将同一缓冲二次放回池）
                    cached.Detach();
                }
            }

            if (!skipReturn)
            {
                var buffer = se.Buffer;
                se.SetBuffer(null, 0, 0);
                if (buffer != null) ArrayPool<Byte>.Shared.Return(buffer);
            }
        }
        catch (Exception ex)
        {
            // 释放路径出错说明缓冲所有权契约已被破坏：记日志后继续收尾，不向外抛
            // （本方法在 ProcessEvent 的 catch 与背压恢复路径上被调用，外抛会替换掉原始异常）
            WriteLog("释放RecvSA {0} 异常：{1}", slot?.Index ?? -1, ex.Message);
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
                    // 接收环复用：优先取回挂在接收槽上的上一轮句柄，把窗口重设到本段数据；无则新建。
                    // 不变量：slot.Packet 非空 ⇒ 它的缓冲就是本槽的 se.Buffer——se.Buffer 只在 StartReceive 借新、
                    // ReleaseRecv 归还、轮末解绑换新三处变更，且都在变更前清空 slot.Packet；因此这里只需重设长度，
                    // 不需要也不可能换缓冲（换缓冲必须换新句柄）。
                    // 代价：不再校验 RefCount——本槽句柄由本槽独占，凡跨轮带出都会先 Slice（轮末即走换新分支、不回挂），
                    // 唯一失守点是下游违规留存原句柄（不经 Slice）时会被静默重设窗口而不是报错。
                    var slot = se.UserToken as RecvSlot;
                    var pk = slot?.Packet;
                    if (pk != null)
                    {
                        slot!.Packet = null;
                        pk.Resize(bytes);
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
                        // 计数为 1（无他人持有）→ 句柄回挂接收槽供下轮重设窗口复用，缓冲留在会话继续接收，零 Rent/Return；
                        // 其余情况（已被下游消费归零，或存在共享切片大于 1）→ 释放本句柄（已释放则空操作），解绑换新；
                        // 不在此归还：计数大于 1 时旧缓冲仍被外部使用，归还它会把正在使用的缓冲交还给池。
                        // 已启用入站管道（Pipe）时必然走换新分支：AppendToPipe 投递的共享切片使本句柄计数恒大于 1，
                        // 故“零 Rent/Return”复用只发生在未启用管道的会话上（换新为池化往返，实测约 8ns/次，代价可忽略）。
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
        try
        {
            LastTime = DateTime.Now;

            // 预处理，得到将要处理该数据包的会话
            var ss = OnPreReceive(pk, local, remote);
            if (ss == null) return;

            if (LogReceive && Log != null && Log.Enable) WriteLog("Recv [{0}]: {1}", total, pk.ToHex(LogDataLength));

            // 协议模式：数据已由 OnPreReceive 投递数据管道，由消息泵定界交付（头部到齐即出消息）
            if (_pumpTask != null) return;

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

    #endregion 接收

    #region 消息泵
    private CancellationTokenSource? _pumpCts;

    /// <summary>泵任务的观察续体。存的是 ContinueWith 续体而非泵本体：仅用于观察失败，以及标记“协议模式已启动”</summary>
    private Task? _pumpTask;

    /// <summary>启动消息泵。协议模式（<see cref="Protocol"/> 非空）下由打开流程与服务端会话启动流程调用</summary>
    /// <remarks>仅流式会话（<see cref="IStreamSession"/>）支持；首次访问数据管道确保其在接收环启动前就绪。调用方须保证时序（每次会话生命周期至多一次）</remarks>
    protected void StartMessagePump()
    {
        if (this is not IStreamSession stream) return;

        var codec = Protocol;
        if (codec == null) return;

        // 触建数据管道（接收环启动前就绪，首个数据到达即可投递）
        var reader = stream.Pipe.Reader;

        var cts = new CancellationTokenSource();
        _pumpCts = cts;

        WriteLog("启动消息泵：{0}", codec);

        var task = PumpAsync(new MessagePump(codec) { MaxCache = MaxCache, RequireFullFrame = RequireFullFrame, MaxFrameSize = MaxFrameSize }, reader, cts.Token);

        // 观察泵任务：泵内异常若无人观察会被静默吞掉，表现为“连接还在但再也收不到消息”，极难定位
        _pumpTask = task.ContinueWith(
            t =>
            {
                try
                {
                    if (t.IsFaulted) OnError("MessagePump", t.Exception!.GetBaseException());
                }
                catch { }
            },
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    /// <summary>停止消息泵。取消挂起读取，泵任务随后自行退出（数据管道完成同样唤醒读取）</summary>
    private void StopMessagePump()
    {
        var cts = Interlocked.Exchange(ref _pumpCts, null);
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    /// <summary>消息泵循环。定界消息帧并逐帧交付；同连接消息串行处理</summary>
    /// <param name="pump">消息帧泵</param>
    /// <param name="reader">数据管道读取器</param>
    /// <param name="cancellationToken">取消通知（会话关闭）</param>
    private async Task PumpAsync(MessagePump pump, PipeReader reader, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            IMessage? message;
            try
            {
                message = await pump.ReadAsync(reader, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                OnError("MessagePump", ex);

                // 数据流不可恢复（协议错误/IO 异常）：关闭会话，避免半开连接僵死
                Close("MessagePumpError");
                break;
            }

            // 数据管道完成（连接关闭）：退出
            if (message == null) break;

            // 有匹配队列时，入站消息都可能被配对交付给等待方：等待方在事件链之后还要异步消费同一份体，
            // 而流式体不能被二次读（泵会继续读同一 PipeReader，导致单读者冲突或把下一帧字节当成体），因此先物化为内存体。
            // 不能依赖 message.Reply——无方向位协议（如 LengthFieldCodec 的恒真 matcher）恒为 false，会让等待方拿到流式体而串包
            if (MatchQueue != null && message.Body is { IsStreaming: true })
            {
                try
                {
                    var all = await message.Body.ReadAllAsync(cancellationToken).ConfigureAwait(false);
                    message.SetBody(all);
                }
                catch (OperationCanceledException)
                {
                    message.TryDispose();
                    continue;
                }
                catch (Exception ex)
                {
                    // 流式体读满失败（对端半途断开/管道故障）：数据流已不可恢复，与帧层读失败同口径关闭会话。
                    // 异常若向上逃出泵任务，续体只记日志不关会话，表现为“连接还在、消息不再到达、也不触发关闭”的半死态
                    message.TryDispose();
                    OnError("MessagePump", ex);
                    Close("MessagePumpError");
                    break;
                }
            }

            // 并行模式：先物化流式体（一次拷贝换并行安全），信号量约束并发后派发；处理顺序不定（SRMP 按序列号配对）
            if (MaxConcurrency > 1)
            {
                if (message.Body is { IsStreaming: true })
                {
                    try
                    {
                        var all = await message.Body.ReadAllAsync(cancellationToken).ConfigureAwait(false);
                        message.SetBody(all);
                    }
                    catch (OperationCanceledException)
                    {
                        message.TryDispose();
                        continue;
                    }
                    catch (Exception ex)
                    {
                        // 同匹配队列分支：流式体读满失败即数据流不可恢复，丢弃消息并关闭会话
                        message.TryDispose();
                        OnError("MessagePump", ex);
                        Close("MessagePumpError");
                        break;
                    }
                }

                try
                {
                    await Concurrency.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    message.TryDispose();
                    break;
                }
                catch (ObjectDisposedException)
                {
                    // 会话销毁：并发信号量已释放，消息无人处理
                    message.TryDispose();
                    break;
                }

                // Task.Run 派发：async 方法首段同步执行，须真正切换线程池，否则同步处理器会阻塞泵循环
                _ = Task.Run(() => ProcessMessageAsync(message, cancellationToken, true));
                continue;
            }

            // 串行模式：同连接依次处理（前一条收尾后才读下一帧）
            await ProcessMessageAsync(message, cancellationToken, false).ConfigureAwait(false);
        }
    }

    /// <summary>处理单个消息：统一进入事件链（可观测）→ 响应尝试匹配交付 → 收尾</summary>
    /// <param name="message">消息</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <param name="releaseSlot">完成后是否释放并发信号量（并行派发为 true）</param>
    /// <remarks>
    /// <para>交付收尾：未交付等待方的消息丢弃未读负载对齐帧尾，随后释放；命中交付的消息由等待方释放。</para>
    /// <para>并行模式（<see cref="MaxConcurrency"/> 大于1）下由独立任务调用，异常统一经 <see cref="OnError"/> 上报，不向任务外部抛出。</para>
    /// </remarks>
    private async Task ProcessMessageAsync(IMessage message, CancellationToken cancellationToken, Boolean releaseSlot)
    {
        try
        {
            var delivered = false;
            try
            {
                await OnMessageAsync(message).ConfigureAwait(false);

                // 事件链（可观测）可能已在处理器内读空消息体；命中配对的等待方还要消费同一份体，
                // 交付前把内存模式体复位到起点（流式体不参与配对交付，不可重放）
                if (message.Body is { IsStreaming: false } body) body.Reset();

                delivered = TryMatchResponse(message);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                OnError("OnMessage", ex);
            }
            finally
            {
                // 交付收尾：未交付等待方的消息丢弃未读负载对齐帧尾，随后释放
                if (!delivered)
                {
                    try
                    {
                        await MessagePump.DiscardAsync(message, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { }
                    finally
                    {
                        message.TryDispose();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // 兕底：防止 fire-and-forget 任务异常未观察
            OnError("ProcessMessage", ex);
            message.TryDispose();
        }
        finally
        {
            // 会话销毁时信号量可能已释放：任务收尾晚于 Dispose 属正常时序，忽略该异常
            if (releaseSlot)
            {
                try { Concurrency.Release(); }
                catch (ObjectDisposedException) { }
            }
        }
    }

    private SemaphoreSlim? _concurrency;

    /// <summary>并发信号量。并行模式（<see cref="MaxConcurrency"/> 大于1）下约束同连接并发处理数，等待时形成背压</summary>
    private SemaphoreSlim Concurrency => _concurrency ??= new SemaphoreSlim(MaxConcurrency, MaxConcurrency);

    /// <summary>取出协议的请求-响应配对能力。装饰协议（压缩/加密等）把配对能力留给内层，需逐层解包</summary>
    /// <param name="codec">协议</param>
    /// <returns>配对器；协议不支持配对时返回 null</returns>
    private static IMessageMatcher? GetMatcher(IMessageCodec? codec) => codec switch
    {
        IMessageMatcher matcher => matcher,
        IMessageCodecDecorator decorator => GetMatcher(decorator.Inner),
        _ => null,
    };

    /// <summary>尝试把响应消息匹配给等待中的请求（协议模式）。命中则交付等待方，跳过收尾</summary>
    /// <param name="message">收到的消息</param>
    /// <returns>是否已匹配交付</returns>
    /// <remarks>
    /// <para>无匹配队列（未发过等待请求）或无配对协议时快速返回，不产生额外开销；
    /// 消息是否可配对由协议 matcher 判定（如 SRMP 要求应答消息+序列号相等；无方向协议可用恒真 matcher）。</para>
    /// <para>流式负载在事件交付前物化为内存模式：事件链可观察读取，等待方在任意时机异步消费（一次拷贝换正确性）。
    /// 未命中时消息按普通流程收尾，负载随消息归还。</para>
    /// </remarks>
    private Boolean TryMatchResponse(IMessage message)
    {
        var queue = MatchQueue;
        if (queue == null) return false;

        // 装饰协议的配对能力在内层，逐层解包后再比对
        var matcher = GetMatcher(Protocol);
        if (matcher == null) return false;

        return queue.Match(this, message, message, (req, resp) =>
            req is IMessage rq && resp is IMessage rs && matcher.Match(rq, rs));
    }

    /// <summary>收到消息（异步）。协议模式（<see cref="Protocol"/> 非空）下由消息泵逐帧调用</summary>
    /// <param name="message">消息（头部字段就位、体已绑定）</param>
    /// <remarks>
    /// <para>默认触发同步 <see cref="Received"/> 事件链。处理器返回后消息进入收尾：未读体被丢弃对齐帧尾、消息释放。</para>
    /// <para>需要异步读取流式主体的场景，继承会话重写本方法，在 await 期间消息与数据窗口保持有效；同步事件处理器内需要流式数据时请先物化（<see cref="LimitedReader.ReadAllAsync"/>，数据未到齐会等待——串行语义下正确），或物化后交给后台异步链处理。</para>
    /// </remarks>
    protected virtual ValueTask OnMessageAsync(IMessage message)
    {
        OnMessage(message);

        return default;
    }

    /// <summary>收到消息。协议模式（<see cref="Protocol"/> 非空）下由消息泵逐帧调用</summary>
    /// <param name="message">消息（头部字段就位、体已绑定）</param>
    /// <remarks>
    /// <para>构造接收事件参数并进入 <see cref="OnReceive"/> 事件链；消息体未读部分由消息泵在交付后丢弃对齐下一帧。</para>
    /// <para>事件参数的 <see cref="ReceivedEventArgs.Packet"/> 为消息负载视图（整帧快路径），流式模式下为 null；与消息同生命周期（处理器返回后失效），需要留存请先物化或切出共享句柄。业务请统一经 <see cref="ReceivedEventArgs.Message"/> 读取头部与流式体。</para>
    /// </remarks>
    protected virtual void OnMessage(IMessage message)
    {
        var e = ReceivedEventArgs.Rent();
        try
        {
            e.Local = Local.Address;
            e.Remote = Remote.EndPoint;
            e.Packet = message.Payload;
            e.Message = message;

            OnReceive(e);
        }
        finally
        {
            ReceivedEventArgs.Return(e);
        }
    }
    #endregion

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
        if (GetMatcher(codec) == null) throw new NotSupportedException($"协议 [{codec.GetType().Name}] 未实现请求-响应配对（IMessageMatcher），无法等待响应");
        if (this is not IStreamSession) throw new NotSupportedException($"会话类型 [{GetType().Name}] 不支持请求-响应等待（响应匹配依赖消息泵，仅流式会话可用）");

        var span = Tracer?.NewSpan($"net:{Name}:SendMessageAsync", request);
        var source = PooledValueTaskSource<IMessage>.Rent();
        source.AttachSpan(span);

        try
        {
            // 并发首用时只能有一个队列胜出：败者丢弃自建实例，否则请求会入队到无人匹配的队列，只能等超时
            var queue = MatchQueue;
            if (queue == null)
            {
                var created = new DefaultMatchQueue();
                queue = Interlocked.CompareExchange(ref _matchQueue, created, null) ?? created;
            }

            queue.Add(this, request, MatchTimeout, source);

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
    /// <exception cref="NotSupportedException">会话类型不支持流式发送（仅流式会话支持）</exception>
    public virtual async ValueTask<Int64> SendMessageAsync(IMessage message, Stream body, Int64 bodyLength = -1, CancellationToken cancellationToken = default)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (body == null) throw new ArgumentNullException(nameof(body));
        if (this is not TcpSession tcp) throw new NotSupportedException($"会话类型 [{GetType().Name}] 不支持流式发送（仅流式会话支持）");

        var codec = Protocol ?? throw new InvalidOperationException($"Protocol not set for session [{Name}]");

        // 长度未知：从可定位流推导（协议头须先声明长度）
        if (bodyLength < 0)
        {
            if (!body.CanSeek) throw new ArgumentException("无法预知流长度：请提供 bodyLength 或使用可定位流", nameof(bodyLength));
            bodyLength = body.Length - body.Position;
        }

        // 头部先行：与流内容在一次写锁内（头体之间不会被其它写入者插入）
        var header = codec.BuildHeader(message, bodyLength);
        try
        {
            var rs = await tcp.SendMessageLockedAsync(header, body, bodyLength, cancellationToken).ConfigureAwait(false);

            // 失败返回 -1：底层已记错误日志并按失败关闭会话，这里转成异常，保持本方法“失败即抛”的契约
            if (rs < 0) throw new IOException($"Send message failed on [{Name}].");

            return rs;
        }
        finally
        {
            // 写完才返回，此处释放头部构建产物
            header.TryDispose();
        }
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

/// <summary>接收槽状态：槽序号 + 轮末回挂复用的数据包句柄</summary>
/// <param name="index">槽序号（第几个接收事件参数）</param>
internal sealed class RecvSlot(Int32 index)
{
    /// <summary>槽序号（第几个接收事件参数）</summary>
    public Int32 Index { get; } = index;

    /// <summary>轮末回挂的数据包句柄，下一轮重设窗口后复用；无外部持有者时非空</summary>
    public OwnerPacket? Packet { get; set; }

    /// <summary>是否已释放。用于释放幂等：同一个接收事件参数只释放一次</summary>
    public Boolean Released { get; set; }
}