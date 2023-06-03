using System.Text;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;

namespace NewLife.Net;

/// <summary>网络服务的会话，每个连接一个会话</summary>
/// <typeparam name="TServer">网络服务类型</typeparam>
public class NetSession<TServer> : NetSession where TServer : NetServer
{
    /// <summary>主服务</summary>
    public virtual TServer Host { get => (this as INetSession).Host as TServer; set => (this as INetSession).Host = value; }
}

/// <summary>网络服务的会话，每个连接一个会话</summary>
/// <remarks>
/// 实际应用可通过重载OnReceive实现收到数据时的业务逻辑。
/// </remarks>
public class NetSession : DisposeBase, INetSession, IExtend
{
    #region 属性
    /// <summary>唯一会话标识</summary>
    public virtual Int32 ID { get; internal set; }

    /// <summary>主服务</summary>
    NetServer INetSession.Host { get; set; }

    /// <summary>客户端。跟客户端通讯的那个Socket，其实是服务端TcpSession/UdpServer</summary>
    public ISocketSession Session { get; set; }

    /// <summary>服务端</summary>
    public ISocketServer Server { get; set; }

    /// <summary>客户端地址</summary>
    public NetUri Remote => Session?.Remote;

    /// <summary>用户会话数据</summary>
    public IDictionary<String, Object> Items { get; set; } = new NullableDictionary<String, Object>();

    /// <summary>获取/设置 用户会话数据</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public virtual Object this[String key] { get => Items[key]; set => Items[key] = value; }

    /// <summary>服务提供者</summary>
    /// <remarks>
    /// 根据会话创建Scoped范围服务，以使得各服务解析在本会话中唯一。
    /// 基类使用内置ObjectContainer的Scope，在WebApi/Worker项目中，使用者需要自己创建Scope并赋值服务提供者。
    /// </remarks>
    public IServiceProvider ServiceProvider { get; set; }

    /// <summary>数据到达事件</summary>
    public event EventHandler<ReceivedEventArgs> Received;

    private Int32 _running;
    private IServiceScope _scope;
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

        using var span = ns?.Tracer?.NewSpan($"net:{ns.Name}:Connect", Remote?.ToString());
        try
        {
            OnConnected();

            var ss = Session;
            if (ss != null)
            {
                // 网络会话和Socket会话共用用户会话数据
                Items = ss.Items;

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

    private void Ss_Received(Object sender, ReceivedEventArgs e)
    {
        var ns = (this as INetSession).Host;
        var tracer = ns?.Tracer;
        using var span = tracer?.NewSpan($"net:{ns.Name}:Receive", e.Message);

        try
        {
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
    /// <summary>新的客户端连接</summary>
    protected virtual void OnConnected() { }

    /// <summary>客户端连接已断开</summary>
    /// <param name="reason">断开原因。包括 SendError/RemoveNotAlive/Dispose/GC 等，其中 ConnectionReset 为网络被动断开或对方断开</param>
    protected virtual void OnDisconnected(String reason) { }

    /// <summary>客户端连接已断开</summary>
    [Obsolete("=>OnDisconnected(String reason)")]
    protected virtual void OnDisconnected() { }

    /// <summary>收到客户端发来的数据</summary>
    /// <param name="e"></param>
    protected virtual void OnReceive(ReceivedEventArgs e) => Received?.Invoke(this, e);

    /// <summary>错误发生，可能是连接断开</summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected virtual void OnError(Object sender, ExceptionEventArgs e) { }
    #endregion

    #region 发送数据
    /// <summary>发送数据</summary>
    /// <param name="data">数据包</param>
    public virtual INetSession Send(Packet data)
    {
        var ns = (this as INetSession).Host;
        using var span = ns?.Tracer?.NewSpan($"net:{ns.Name}:Send", data);

        Session.Send(data);

        return this;
    }

    /// <summary>发送数据流</summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public virtual INetSession Send(Stream stream)
    {
        var ns = (this as INetSession).Host;
        using var span = ns?.Tracer?.NewSpan($"net:{ns.Name}:Send");

        Session.Send(stream);

        return this;
    }

    /// <summary>发送字符串</summary>
    /// <param name="msg"></param>
    /// <param name="encoding"></param>
    public virtual INetSession Send(String msg, Encoding encoding = null)
    {
        var ns = (this as INetSession).Host;
        using var span = ns?.Tracer?.NewSpan($"net:{ns.Name}:Send", msg);

        Session.Send(msg, encoding);

        return this;
    }

    /// <summary>通过管道发送消息，不等待响应</summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public virtual Int32 SendMessage(Object message) => Session.SendMessage(message);

    /// <summary>异步发送并等待响应</summary>
    /// <param name="message">消息</param>
    /// <returns></returns>
    public virtual Task<Object> SendMessageAsync(Object message) => Session.SendMessageAsync(message);

    /// <summary>异步发送并等待响应</summary>
    /// <param name="message">消息</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public virtual Task<Object> SendMessageAsync(Object message, CancellationToken cancellationToken) => Session.SendMessageAsync(message, cancellationToken);
    #endregion

    #region 日志
    /// <summary>日志提供者</summary>
    public ILog Log { get; set; }

    private String _LogPrefix;
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
    public virtual void WriteLog(String format, params Object[] args) => Log?.Info(LogPrefix + format, args);

    /// <summary>输出错误日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public virtual void WriteError(String format, params Object[] args) => Log?.Error(LogPrefix + format, args);
    #endregion

    #region 辅助
    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => $"{(this as INetSession).Host?.Name}[{ID}] {Session}";
    #endregion
}