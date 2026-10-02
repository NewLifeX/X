using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using NewLife.Data;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Model;

namespace NewLife.Net;

/// <summary>Udp会话</summary>
/// <remarks>
/// <para>仅用于服务端与某一固定远程地址通信。</para>
/// <para>特性：</para>
/// <list type="bullet">
/// <item>绑定到固定的远程地址</item>
/// <item>共享UdpServer的底层Socket</item>
/// <item>收到空数据包时自动结束会话。判定以收到的原始数据报为准，在进入事件链之前定下；业务在事件内释放或置空 <see cref="ReceivedEventArgs.Packet"/> 不影响判定，要主动结束会话请 <see cref="DisposeBase.Dispose()"/></item>
/// </list>
/// </remarks>
public class UdpSession : DisposeBase, ISocketSession, ITransport, ILogFeature
{
    #region 属性
    /// <summary>会话编号</summary>
    /// <remarks>用于在多会话环境中唯一标识当前会话</remarks>
    public Int32 ID { get; set; }

    /// <summary>名称</summary>
    /// <remarks>主要用于日志输出，默认继承自服务器名称</remarks>
    public String Name { get; set; }

    /// <summary>服务器</summary>
    /// <remarks>所属的UdpServer实例</remarks>
    public UdpServer Server { get; set; }

    /// <summary>底层Socket</summary>
    /// <remarks>返回UdpServer的Socket</remarks>
    Socket? ISocket.Client => Server?.Client;

    /// <summary>本地地址</summary>
    /// <remarks>接收数据的本地网络地址</remarks>
    public NetUri Local { get; set; }

    /// <summary>端口</summary>
    /// <remarks>本地端口号</remarks>
    public Int32 Port { get => Local.Port; set => Local.Port = value; }

    /// <summary>远程地址</summary>
    /// <remarks>通信的目标远程地址</remarks>
    public NetUri Remote { get; set; }

    private Int32 _timeout;
    /// <summary>超时时间（毫秒）</summary>
    /// <remarks>接收操作的超时时间，默认3000ms</remarks>
    public Int32 Timeout
    {
        get => _timeout;
        set
        {
            _timeout = value;
            if (Server?.Client is { } sock)
                sock.ReceiveTimeout = _timeout;
        }
    }

    /// <summary>协议编解码器。非空时为协议模式：数据报按完整帧定界并构造消息分发（无粘包/半包处理，无需泵任务）</summary>
    public IMessageCodec? Protocol { get; set; }

    /// <summary>最大并发处理数。协议模式下消息处理并发度（随服务器下发）：1=串行（默认），大于1=并行派发（兼作并发上限）</summary>
    /// <remarks>并行下同一会话的多个消息处理顺序不定（SRMP 按序列号配对）；并行要求业务处理器线程安全。</remarks>
    public Int32 MaxConcurrency { get; set; } = 1;

    /// <summary>Socket服务器</summary>
    /// <remarks>当前通讯所在的Socket服务器</remarks>
    ISocketServer ISocketSession.Server => Server;

    /// <summary>最后一次通信时间</summary>
    /// <remarks>主要表示活跃时间，包括收发操作</remarks>
    public DateTime LastTime { get; private set; } = DateTime.Now;

    /// <summary>APM性能追踪器</summary>
    /// <remarks>用于记录关键操作的性能追踪</remarks>
    public ITracer? Tracer { get; set; }
    #endregion

    #region 构造
    /// <summary>实例化Udp会话</summary>
    /// <param name="server">所属服务器</param>
    /// <param name="local">接收数据的本地地址</param>
    /// <param name="remote">远程终结点</param>
    public UdpSession(UdpServer server, IPAddress? local, IPEndPoint remote)
    {
        Name = server.Name;

        Server = server;
        Remote = new NetUri(NetType.Udp, remote);
        Tracer = server.Tracer;

        Local = server.Local.Clone();
        if (local != null) Local.Address = local;

        // 检查并开启广播
        server.Client?.CheckBroadcast(remote.Address);
    }

    /// <summary>开始数据交换</summary>
    public void Start()
    {
        if (Disposed || Server == null) return;

        Protocol = Server.Protocol;
        MaxConcurrency = Server.MaxConcurrency;

        Server.Open();

        WriteLog("New {0}", Remote.EndPoint);
    }

    private void Stop(String reason)
    {
        if (Server == null) return;

        WriteLog("Close {0} {1}", Remote.EndPoint, reason);

        Server = null!;
    }

    /// <summary>销毁资源</summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        Stop(disposing ? "Dispose" : "GC");

        // 只释放、不置空：置空后 ??= 会为后续派发新建“满额”信号量，即便会话已销毁也放行消息进入处理链。
        // 已派发的消息持有自己等待到的实例，其 Release 命中已释放实例抛 ObjectDisposedException，由调用点忽略（基类同样只释放不置空）
        _concurrency?.Dispose();

        //// 释放对服务对象的引用，如果没有其它引用，服务对象将会被回收
        //Server = null;
    }
    #endregion

    #region 发送
    /// <summary>发送数据</summary>
    /// <param name="data">数据包</param>
    /// <returns>实际发送的字节数</returns>
    /// <exception cref="ObjectDisposedException">会话已销毁</exception>
    public Int32 Send(IPacket data)
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);

        return Server.OnSend(data, Remote.EndPoint);
    }

    /// <summary>发送数据</summary>
    /// <param name="data">字节数组</param>
    /// <param name="offset">偏移</param>
    /// <param name="count">字节数</param>
    /// <returns>实际发送的字节数</returns>
    /// <exception cref="ObjectDisposedException">会话已销毁</exception>
    public Int32 Send(Byte[] data, Int32 offset = 0, Int32 count = -1)
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);

        // 全部发送
        if (count < 0) count = data.Length - offset;

#if NET6_0_OR_GREATER
        return Server.OnSend(new ReadOnlySpan<Byte>(data, offset, count), Remote.EndPoint);
#else
        return Server.OnSend(new ArraySegment<Byte>(data, offset, count), Remote.EndPoint);
#endif
    }

    /// <summary>发送数据</summary>
    /// <param name="data">数组段</param>
    /// <returns>实际发送的字节数</returns>
    /// <exception cref="ObjectDisposedException">会话已销毁</exception>
    public Int32 Send(ArraySegment<Byte> data)
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);

        return Server.OnSend(data, Remote.EndPoint);
    }

    /// <summary>发送数据</summary>
    /// <param name="data">只读内存段</param>
    /// <returns>实际发送的字节数</returns>
    /// <exception cref="ObjectDisposedException">会话已销毁</exception>
    public Int32 Send(ReadOnlySpan<Byte> data)
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);

        return Server.OnSend(data, Remote.EndPoint);
    }

    /// <summary>发送消息，不等待响应。经协议构建整帧后发送</summary>
    /// <param name="message">消息对象（须实现 <see cref="IMessage"/>）</param>
    /// <returns>实际发送的字节数</returns>
    /// <exception cref="InvalidOperationException">未设置协议或消息类型不支持</exception>
    public virtual Int32 SendMessage(Object message)
    {
        // 协议模式：消息经协议构建整帧后发送
        if (Protocol is { } codec && message is IMessage msg)
        {
            var data = codec.Build(msg);
            if (data == null) return 0;

            try
            {
                return Send(data);
            }
            finally
            {
                data.TryDispose();
            }
        }

        throw new InvalidOperationException($"Protocol not set or message is not IMessage for session [{Name}]");
    }

    /// <summary>发送消息并等待响应（数据报不支持）</summary>
    /// <param name="message">消息对象</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>响应消息</returns>
    /// <exception cref="NotSupportedException">协议模式下数据报不支持请求-响应等待</exception>
    public virtual ValueTask<Object> SendMessageAsync(Object message, CancellationToken cancellationToken = default)
    {
        // 协议模式：暂不支持数据报请求-响应等待（响应匹配依赖消息泵）
        if (Protocol != null && message is IMessage)
            throw new NotSupportedException("UDP 协议模式暂不支持请求-响应等待，请使用单向 SendMessage。");

        throw new InvalidOperationException($"Protocol not set or message is not IMessage for session [{Name}]");
    }
    #endregion

    #region 接收
    /// <summary>同步接收数据</summary>
    /// <returns>接收到的数据包</returns>
    /// <exception cref="ObjectDisposedException">会话已销毁</exception>
    /// <exception cref="InvalidOperationException">服务器未设置</exception>
    public IOwnerPacket Receive()
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);
        var server = Server;
        if (server?.Client is not { } sock) throw new InvalidOperationException(nameof(Server));

        // 服务器接收环运行时禁止会话拉取：两条读路径会争抢同一Socket，数据被分流且不可预期
        if (server.IsReceiving) throw new InvalidOperationException(NoPullMessage);

        using var span = Tracer?.NewSpan($"net:{Name}:Receive");
        try
        {
            var remote = Remote.EndPoint;
            var ep = remote as EndPoint;
            var pk = new OwnerPacket(server.BufferSize);
            while (true)
            {
                var size = sock.ReceiveFrom(pk.Buffer, ref ep);
                // 共享Socket可能收到其他对端的数据报，丢弃非本会话来源的数据报继续等待（契约：Udp只接受来自所属远方的数据）
                if (!IsFromRemote(ep, remote)) continue;

                span?.Value = size;
                return pk.Resize(size);
            }
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    /// <summary>异步接收数据</summary>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>接收到的数据包</returns>
    /// <exception cref="ObjectDisposedException">会话已销毁</exception>
    /// <exception cref="InvalidOperationException">服务器未设置</exception>
    public virtual async Task<IOwnerPacket?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);
        var server = Server;
        if (server?.Client is not { } socket) throw new InvalidOperationException(nameof(Server));

        // 服务器接收环运行时禁止会话拉取：两条读路径会争抢同一Socket，数据被分流且不可预期
        if (server.IsReceiving) throw new InvalidOperationException(NoPullMessage);

        using var span = Tracer?.NewSpan($"net:{Name}:Receive");
        try
        {
            var remote = Remote.EndPoint;
            var ep = remote as EndPoint;
            var pk = new OwnerPacket(server.BufferSize);
            while (true)
            {
#if NETFRAMEWORK || NETSTANDARD2_0
                var ar = socket.BeginReceiveFrom(pk.Buffer, 0, pk.Length, SocketFlags.None, ref ep, null, socket);
                var size = ar.IsCompleted ?
                    socket.EndReceiveFrom(ar, ref ep) :
                    await Task.Factory.FromAsync(ar, e => socket.EndReceiveFrom(e, ref ep)).ConfigureAwait(false);
                // 共享Socket可能收到其他对端的数据报，丢弃非本会话来源的数据报继续等待
                if (!IsFromRemote(ep, remote)) continue;
#elif NET7_0_OR_GREATER
                var result = await socket.ReceiveFromAsync(pk.GetMemory(), ep, cancellationToken).ConfigureAwait(false);
                // 共享Socket可能收到其他对端的数据报，丢弃非本会话来源的数据报继续等待
                if (!IsFromRemote(result.RemoteEndPoint, remote)) continue;
                var size = result.ReceivedBytes;
#else
                var result = await socket.ReceiveFromAsync(pk.Buffer, SocketFlags.None, ep).ConfigureAwait(false);
                // 共享Socket可能收到其他对端的数据报，丢弃非本会话来源的数据报继续等待
                if (!IsFromRemote(result.RemoteEndPoint, remote)) continue;
                var size = result.ReceivedBytes;
#endif
                span?.Value = size;
                return pk.Resize(size);
            }
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    /// <summary>数据接收事件</summary>
    public event EventHandler<ReceivedEventArgs>? Received;

    internal Boolean OnReceive(ReceivedEventArgs e)
    {
        LastTime = DateTime.Now;

        // 协议模式：数据报即完整帧，定界后逐条分发；空数据报仍按停会话约定处理
        if (Protocol is { } codec && e?.Packet is { Length: > 0 } pk)
        {
            ProcessDatagram(codec, pk);
            return true;
        }

        // 我们约定，UDP收到空数据包时，结束会话。判定以收到的原始数据报为准，在进入事件链之前定下：
        // 业务按契约在事件内消费/释放本轮句柄（交发送链路、或置空做标记）后 Length 已归零，
        // 事件之后再判会把正常数据报当成空包，误停会话并清掉会话级状态。要主动结束会话，请 Dispose 会话。
        var empty = e != null && e.Packet is not { Length: > 0 };

        if (e != null) Received?.Invoke(this, e);

        // 我们约定，UDP收到空数据包时，结束会话
        if (empty)
        {
            Stop("Finish");
            Dispose();
        }

        return false;
    }

    /// <summary>处理协议数据报。一个数据报可能包含多个消息帧，逐条定界分发；坏帧截断丢弃</summary>
    /// <param name="codec">协议编解码器</param>
    /// <param name="pk">数据报（接收环轮内有效，消息体为零拷贝共享切片）</param>
    private void ProcessDatagram(IMessageCodec codec, IPacket pk)
    {
        var seq = pk.AsReadOnlySequence();
        var pos = 0L;
        while (pos < seq.Length)
        {
            var rs = codec.TryParse(seq.Slice(pos));
            if (rs == null) break;

            // 损坏帧：数据报内已无法继续定界，丢弃本报文等下一包；
            // 不能按异常处理，否则一个坏报文就会下线整个 UDP 服务
            if (rs.Value.Invalid) break;

            // 已定界但需整帧（装饰协议，如压缩）：数据报是原子单位，无法在包内补齐，丢弃本报文等下一包
            if (rs.Value.NeedFullFrame) break;

            // 无消息帧（心跳/空行/分隔符）：跳过字节后继续解析
            if (rs.Value.Message == null)
            {
                var skip = rs.Value.HeaderSize;
                if (skip <= 0) break;
                pos += skip;
                continue;
            }

            var message = rs.Value.Message;
            var headerSize = rs.Value.HeaderSize;
            var bodyLength = rs.Value.BodyLength;

            // 声明长度超出数据报窗口：坏帧截断丢弃
            if (headerSize + bodyLength > seq.Length - pos)
            {
                message.Dispose();
                break;
            }

            // 体绑定：协议未预绑定体时才从数据报窗口切零拷贝共享切片。
            // 预绑定体（如压缩协议解压后重绑）必须保留，否则会把解压结果替换成线上压缩字节（静默交出错误数据）——与帧泵分支保持一致
            if (message.Payload == null && bodyLength > 0)
                message.SetBody(pk.Slice((Int32)pos + headerSize, (Int32)bodyLength));

            // 并行模式：数据报体为零拷贝共享切片（天然独立），信号量约束后派发；处理顺序不定（SRMP 按序列号配对）
            if (MaxConcurrency > 1)
            {
                // 等待信号量放到线程池上：接收环运行在 IOCP/线程池线程，就地阻塞会拖慢整条接收链并挤爆内核缓冲
                var concurrency = Concurrency;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await concurrency.WaitAsync().ConfigureAwait(false);
                    }
                    catch (ObjectDisposedException)
                    {
                        // 会话销毁：信号量已释放，消息无人处理，就地释放
                        message.TryDispose();
                        return;
                    }

                    ProcessMessage(message, concurrency);
                });

                pos += headerSize + bodyLength;
                continue;
            }

            try
            {
                OnMessage(message);
            }
            catch (Exception ex)
            {
                OnError("OnMessage", ex);
            }
            finally
            {
                message.TryDispose();
            }

            pos += headerSize + bodyLength;
        }
    }

    /// <summary>并行处理单个消息：完成后释放并发信号量</summary>
    /// <param name="message">消息（体为数据报内零拷贝共享切片，天然独立）</param>
    /// <param name="concurrency">派发前等待到的信号量实例。归还必须用同一实例：会话首访并发时可能各建一个，重读属性会把槽位还到另一个满额实例并抛 <see cref="SemaphoreFullException"/></param>
    private void ProcessMessage(IMessage message, SemaphoreSlim concurrency)
    {
        try
        {
            OnMessage(message);
        }
        catch (Exception ex)
        {
            OnError("OnMessage", ex);
        }
        finally
        {
            message.TryDispose();

            // 释放自己等待到的那个并发槽位（背压）；会话销毁时该实例已被释放，忽略该异常
            try { concurrency.Release(); }
            catch (ObjectDisposedException) { }
        }
    }

    private SemaphoreSlim? _concurrency;

    /// <summary>并发信号量。并行模式（<see cref="MaxConcurrency"/> 大于1）下约束同会话并发处理数，等待时形成背压</summary>
    /// <remarks>
    /// 多条接收槽线程并发首访时可能各建一个满额实例，字段只留最后一个。该竞态无害：每个消息只归还自己等待到的实例，
    /// 不存在“等待在A、归还到B”的错位，最坏仅首访瞬间并发上限短暂放大。刻意不加 CAS/锁，不为极窄窗口引入原子操作。
    /// </remarks>
    private SemaphoreSlim Concurrency => _concurrency ??= new SemaphoreSlim(MaxConcurrency, MaxConcurrency);

    /// <summary>收到消息。构造接收事件参数并进入事件链（协议模式）</summary>
    /// <param name="message">消息（头部字段就位、体已绑定）</param>
    /// <remarks>事件参数的 <see cref="ReceivedEventArgs.Packet"/> 为消息负载视图；与消息同生命周期（处理器返回后失效）</remarks>
    private void OnMessage(IMessage message)
    {
        var e = ReceivedEventArgs.Rent();
        try
        {
            e.Local = Local.Address;
            e.Remote = Remote.EndPoint;
            e.Packet = message.Payload;
            e.Message = message;

            LastTime = DateTime.Now;
            Received?.Invoke(this, e);

            // 升格到服务器层：以服务器为接入点的场景（如 NetClient）经此收到消息
            Server?.RaiseReceiveInternal(this, e);
        }
        finally
        {
            ReceivedEventArgs.Return(e);
        }
    }

    /// <summary>处理数据帧</summary>
    /// <param name="data">数据帧</param>
    void ISocketRemote.Process(IData data) => (Server as ISocketRemote)?.Process(data);
    #endregion

    #region 异常处理
    /// <summary>错误发生/断开连接时</summary>
    public event EventHandler<ExceptionEventArgs>? Error;

    /// <summary>触发异常</summary>
    /// <param name="action">动作</param>
    /// <param name="ex">异常</param>
    protected virtual void OnError(String action, Exception ex)
    {
        Log?.Error(LogPrefix + "{0}Error {1} {2}", action, this, ex.Message);
        Error?.Invoke(this, new ExceptionEventArgs(action, ex));
    }
    #endregion

    #region 辅助
    /// <summary>已重载。返回会话的字符串表示</summary>
    /// <returns>本地地址和远程地址的组合</returns>
    public override String ToString()
    {
        if (Remote != null && !Remote.EndPoint.IsAny())
            return $"{Local}<={Remote.EndPoint}";
        else
            return Local.ToString();
    }

    /// <summary>拉取模式提示。服务器接收环已启动时拉取数据将被拒绝</summary>
    private String NoPullMessage => $"[{Name}] 服务器接收环已启动（AutoReceive=true），不允许从会话拉取数据；请改用 Received 事件接收，或在服务器打开前设置 AutoReceive=false 使用拉取模式";

    /// <summary>判断数据报是否来自本会话的远端</summary>
    /// <param name="source">数据报的实际来源地址</param>
    /// <param name="remote">本会话的远端地址</param>
    /// <returns></returns>
    private static Boolean IsFromRemote(EndPoint? source, IPEndPoint remote)
    {
        // 远端为通配地址时不做过滤（防御）
        if (remote.Address.IsAny()) return true;

        return source is IPEndPoint ep && ep.Port == remote.Port && ep.Address.Equals(remote.Address);
    }
    #endregion

    #region ITransport接口
    Boolean ITransport.Open() => true;

    Boolean ITransport.Close() => true;
    #endregion

    #region 扩展接口
    private ConcurrentDictionary<String, Object?>? _items;

    /// <summary>数据项。首次访问时创建</summary>
    /// <remarks>并发首用时以 CAS 保证只保留一份实例：直接 ??= 会各自新建，败者刚写入的数据随之不可达而丢失</remarks>
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
    /// <param name="key">键</param>
    /// <returns>值</returns>
    public Object? this[String key] { get => _items != null && _items.TryGetValue(key, out var obj) ? obj : null; set => Items[key] = value; }
    #endregion

    #region 日志
    /// <summary>日志提供者</summary>
    public ILog Log { get; set; } = Logger.Null;

    /// <summary>是否输出发送日志</summary>
    /// <remarks>默认false</remarks>
    public Boolean LogSend { get; set; }

    /// <summary>是否输出接收日志</summary>
    /// <remarks>默认false</remarks>
    public Boolean LogReceive { get; set; }

    private String? _LogPrefix;
    /// <summary>日志前缀</summary>
    public virtual String LogPrefix
    {
        get
        {
            if (_LogPrefix == null)
            {
                var name = Server == null ? "" : Server.Name;
                _LogPrefix = $"{name}[{ID}].";
            }
            return _LogPrefix;
        }
        set => _LogPrefix = value;
    }

    /// <summary>输出日志</summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">参数</param>
    public void WriteLog(String format, params Object?[] args) => Log.Info(LogPrefix + format, args);
    #endregion
}