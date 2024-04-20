using System.Text;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;

namespace NewLife.Net;

/// <summary>网络服务的会话，每个Tcp/Udp连接作为一个会话</summary>
/// <typeparam name="TServer">网络服务类型</typeparam>
/// <remarks>
/// 每当收到一个Tcp连接时，创建一个INetSession会话，用于处理该连接的业务。
/// 使用Udp服务端时，收到远程节点的第一个数据包时，也会创建一个会话，处理该节点的业务。
/// 
/// 所有应用服务器以会话<see cref="INetSession"/>作为业务处理核心。
/// 应用服务器收到新会话请求后，通过Start启动一个会话处理。
/// 会话进行业务处理的过程中，可以通过多个Send方法向客户端发送数据。
/// </remarks>
public class NetSession<TServer> : NetSession where TServer : NetServer
{
    /// <summary>主服务</summary>
    public virtual TServer Host { get => ((this as INetSession).Host as TServer)!; set => (this as INetSession).Host = value; }
}

/// <summary>网络服务的会话，每个Tcp/Udp连接作为一个会话</summary>
/// <remarks>
/// 每当收到一个Tcp连接时，创建一个INetSession会话，用于处理该连接的业务。
/// 使用Udp服务端时，收到远程节点的第一个数据包时，也会创建一个会话，处理该节点的业务。
/// 
/// 所有应用服务器以会话<see cref="INetSession"/>作为业务处理核心。
/// 应用服务器收到新会话请求后，通过<see cref="Start"/>启动一个会话处理。
/// 会话进行业务处理的过程中，可以通过多个Send方法向客户端发送数据。
/// 
/// 实际应用可通过重载OnReceive实现收到数据时的业务逻辑。
/// </remarks>
public class NetSession : DisposeBase, INetSession, IServiceProvider, IExtend
{
    #region 属性
    /// <summary>唯一会话标识。在主服务中唯一标识当前会话，原子自增</summary>
    public virtual Int32 ID { get; internal set; }

    /// <summary>主服务。负责管理当前会话的主服务器NetServer</summary>
    NetServer INetSession.Host { get; set; } = null!;

    /// <summary>客户端。跟客户端通讯的那个Socket，其实是服务端TcpSession/UdpServer</summary>
    public ISocketSession Session { get; set; } = null!;

    /// <summary>服务端</summary>
    public ISocketServer Server { get; set; } = null!;

    /// <summary>客户端地址</summary>
    public NetUri Remote => Session.Remote;

    /// <summary>网络数据处理器。可作为业务处理实现，也可以作为前置协议解析</summary>
    public INetHandler? Handler { get; set; }

    /// <summary>用户会话数据</summary>
    public IDictionary<String, Object?> Items => Session.Items;

    /// <summary>获取/设置 用户会话数据</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public virtual Object? this[String key] { get => Session[key]; set => Session[key] = value; }

    /// <summary>服务提供者</summary>
    /// <remarks>
    /// 根据会话创建Scoped范围服务，以使得各服务解析在本会话中唯一。
    /// 基类使用内置ObjectContainer的Scope，在WebApi/Worker项目中，使用者需要自己创建Scope并赋值服务提供者。
    /// </remarks>
    public IServiceProvider? ServiceProvider { get; set; }

    /// <summary>连接创建事件。创建会话之后</summary>
    public event EventHandler<EventArgs>? Connected;

    /// <summary>连接断开事件。包括客户端主动断开、服务端主动断开以及服务端超时下线</summary>
    public event EventHandler<EventArgs>? Disconnected;

    /// <summary>数据到达事件</summary>
    public event EventHandler<ReceivedEventArgs>? Received;

    private Int32 _running;
    private IServiceScope? _scope;
    #endregion

    #region 方法
    /// <summary>开始会话处理。</summary>
    public virtual void Start()
    {
        _running = 1;
        WriteLog("Connected {0}", Session);

        var ns = (this as INetSession).Host;
        // 服务提供者，用于创建Scoped范围服务，以使得各服务解析在本会话中唯一
        if (ServiceProvider == null)
        {
            _scope = ns.ServiceProvider?.CreateScope();
            ServiceProvider = _scope?.ServiceProvider ?? ns.ServiceProvider;
        }

        using var span = ns.Tracer?.NewSpan($"net:{ns.Name}:Connect", Remote?.ToString());
        try
        {
            // 网络处理器，独立的业务处理器
            Handler = ns.CreateHandler(this);
            Handler?.Init(this);

            OnConnected();

            var ss = Session;
            if (ss != null)
            {
                //// 网络会话和Socket会话共用用户会话数据
                //Items = ss.Items;

                ss.Received += Ss_Received;
                ss.OnDisposed += (s, e2) =>
                {
                    try
                    {
                        if (s is SessionBase session && !session.CloseReason.IsNullOrEmpty())
                            Close(session.CloseReason);
                        else
                            Close("Disconnect");
                    }
                    catch { }

                    Dispose();
                };
                ss.Error += OnError;
            }
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    private void Ss_Received(Object? sender, ReceivedEventArgs e)
    {
        var ns = (this as INetSession).Host;
        using var span = ns?.Tracer?.NewSpan($"net:{ns?.Name}:Receive", e.Message, e.Packet?.Total ?? 0);

        try
        {
            // 网络处理器先行，还有数据再往下执行
            Handler?.Process(e);

            // 前面逻辑可能关闭连接，也可能清空数据不允许继续
            if (!Disposed && (e.Packet != null || e.Message != null))
                OnReceive(e);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, e.Message ?? e.Packet);
            throw;
        }
    }

    /// <summary>子类重载实现资源释放逻辑时必须首先调用基类方法</summary>
    /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        // 停止网络处理器
        Handler.TryDispose();

        var reason = GetType().Name + (disposing ? "Dispose" : "GC");

        try
        {
            Close(reason);
        }
        catch { }

        //Session.Dispose();//去掉这句话，因为在释放的时候Session有的时候为null，会出异常报错，导致整个程序退出。去掉后正常。
        Session.TryDispose();

        //Server = null;
        //Session = null;

        _scope.TryDispose();
    }

    /// <summary>关闭跟客户端的网络连接</summary>
    /// <param name="reason">断开原因。包括 SendError/RemoveNotAlive/Dispose/GC 等，其中 ConnectionReset 为网络被动断开或对方断开</param>
    public void Close(String reason)
    {
        if (Interlocked.CompareExchange(ref _running, 0, 1) != 1) return;

        var ns = (this as INetSession).Host;
        using var span = ns?.Tracer?.NewSpan($"net:{ns.Name}:Disconnect");
        try
        {
            WriteLog("Disconnect [{0}] {1}", Session, reason);

#pragma warning disable CS0618 // 类型或成员已过时
            OnDisconnected();
#pragma warning restore CS0618 // 类型或成员已过时
            OnDisconnected(reason);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }
    #endregion

    #region 业务核心
    /// <summary>新的客户端连接。基类负责触发Connected事件</summary>
    protected virtual void OnConnected() => Connected?.Invoke(this, EventArgs.Empty);

    /// <summary>客户端连接已断开。基类负责触发Disconnected事件</summary>
    /// <param name="reason">断开原因。包括 SendError/RemoveNotAlive/Dispose/GC 等，其中 ConnectionReset 为网络被动断开或对方断开</param>
    protected virtual void OnDisconnected(String reason) => Disconnected?.Invoke(this, new EventArgs<String>(reason));

    /// <summary>客户端连接已断开</summary>
    [Obsolete("=>OnDisconnected(String reason)")]
    protected virtual void OnDisconnected() { }

    /// <summary>收到客户端发来的数据。基类负责触发Received事件</summary>
    /// <param name="e"></param>
    protected virtual void OnReceive(ReceivedEventArgs e) => Received?.Invoke(this, e);

    /// <summary>错误发生，可能是连接断开</summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected virtual void OnError(Object? sender, ExceptionEventArgs e) => WriteError(e.Exception.Message);
    #endregion

    #region 发送数据
    /// <summary>发送数据，直达网卡</summary>
    /// <param name="data">数据包</param>
    public virtual INetSession Send(Packet data)
    {
        var ns = (this as INetSession).Host;
        using var span = ns?.Tracer?.NewSpan($"net:{ns.Name}:Send", data);

        Session.Send(data);

        return this;
    }

    /// <summary>发送数据流，直达网卡</summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public virtual INetSession Send(Stream stream)
    {
        var ns = (this as INetSession).Host;
        using var span = ns?.Tracer?.NewSpan($"net:{ns.Name}:Send");

        Session.Send(stream);

        return this;
    }

    /// <summary>发送字符串，直达网卡</summary>
    /// <param name="msg"></param>
    /// <param name="encoding"></param>
    public virtual INetSession Send(String msg, Encoding? encoding = null)
    {
        var ns = (this as INetSession).Host;
        using var span = ns?.Tracer?.NewSpan($"net:{ns.Name}:Send", msg);

        Session.Send(msg, encoding);

        return this;
    }

    /// <summary>通过管道发送消息，不等待响应。管道内对消息进行报文封装处理，最终得到二进制数据进入网卡</summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public virtual Int32 SendMessage(Object message) => Session.SendMessage(message);

    /// <summary>异步发送消息并等待响应。管道内对消息进行报文封装处理，最终得到二进制数据进入网卡</summary>
    /// <param name="message">消息</param>
    /// <returns></returns>
    public virtual Task<Object> SendMessageAsync(Object message) => Session.SendMessageAsync(message);

    /// <summary>异步发送消息并等待响应。管道内对消息进行报文封装处理，最终得到二进制数据进入网卡</summary>
    /// <param name="message">消息</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public virtual Task<Object> SendMessageAsync(Object message, CancellationToken cancellationToken) => Session.SendMessageAsync(message, cancellationToken);
    #endregion

    #region 日志
    /// <summary>日志提供者</summary>
    public ILog? Log { get; set; }

    private String? _LogPrefix;
    /// <summary>日志前缀</summary>
    public virtual String LogPrefix
    {
        get
        {
            if (_LogPrefix == null)
            {
                var host = (this as INetSession).Host;
                var name = host == null ? "" : host.Name;
                _LogPrefix = $"{name}[{ID}] ";
            }
            return _LogPrefix;
        }
        set => _LogPrefix = value;
    }

    /// <summary>写日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public virtual void WriteLog(String format, params Object?[] args) => Log?.Info(LogPrefix + format, args);

    /// <summary>输出错误日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public virtual void WriteError(String format, params Object?[] args) => Log?.Error(LogPrefix + format, args);
    #endregion

    #region 辅助
    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => $"{(this as INetSession).Host?.Name}[{ID}] {Session}";

    /// <summary>获取服务</summary>
    /// <param name="serviceType"></param>
    /// <returns></returns>
    public virtual Object GetService(Type serviceType)
    {
        if (serviceType == typeof(IServiceProvider)) return this;
        if (serviceType == typeof(NetSession)) return this;
        if (serviceType == typeof(INetSession)) return this;
        if (serviceType == typeof(NetServer)) return (this as INetSession).Host;
        if (serviceType == typeof(ISocketSession)) return Session;
        if (serviceType == typeof(ISocketServer)) return Server;

        return ServiceProvider!.GetService(serviceType)!;
    }
    #endregion
}