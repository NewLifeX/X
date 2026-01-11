using System.Text;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;

namespace NewLife.Net;

/// <summary>泛型网络服务会话，支持指定服务器类型</summary>
/// <typeparam name="TServer">网络服务器类型，必须继承自 <see cref="NetServer"/></typeparam>
/// <remarks>
/// <para>提供强类型的 Host 属性访问，避免类型转换。</para>
/// <para>适用于需要访问自定义服务器属性或方法的场景。</para>
/// <code>
/// public class MySession : NetSession&lt;MyServer&gt;
/// {
///     protected override void OnReceive(ReceivedEventArgs e)
///     {
///         // 可直接访问 Host.CustomProperty
///         var value = Host.CustomProperty;
///     }
/// }
/// </code>
/// </remarks>
public class NetSession<TServer> : NetSession where TServer : NetServer
{
    /// <summary>主服务</summary>
    /// <remarks>强类型访问主服务器实例，避免类型转换</remarks>
    public virtual TServer Host { get => ((this as INetSession).Host as TServer)!; set => (this as INetSession).Host = value; }
}

/// <summary>网络服务的会话，每个Tcp/Udp连接作为一个会话</summary>
/// <remarks>
/// <para>网络会话是应用层业务处理的核心，封装了单个客户端连接的完整生命周期。</para>
/// <para>设计模式：</para>
/// <list type="bullet">
/// <item>模板方法模式 - 通过重载 OnConnected/OnDisconnected/OnReceive 实现业务逻辑</item>
/// <item>事件驱动模式 - 通过订阅 Connected/Disconnected/Received 事件处理业务</item>
/// <item>管道处理模式 - 通过 Pipeline 实现协议编解码</item>
/// </list>
/// <para>生命周期：</para>
/// <list type="number">
/// <item>TCP连接建立或UDP首包到达时，服务器调用 CreateSession 创建会话</item>
/// <item>调用 <see cref="Start"/> 启动会话，初始化网络处理器，触发 <see cref="OnConnected"/></item>
/// <item>数据到达时触发 <see cref="OnReceive"/>，可通过 Send 系列方法发送响应</item>
/// <item>连接断开时触发 <see cref="OnDisconnected(String)"/>，会话被释放</item>
/// </list>
/// <para>典型用法：</para>
/// <code>
/// public class MySession : NetSession
/// {
///     protected override void OnConnected()
///     {
///         base.OnConnected();
///         WriteLog("客户端已连接");
///         Send("Welcome!");
///     }
///     
///     protected override void OnReceive(ReceivedEventArgs e)
///     {
///         base.OnReceive(e);
///         // 处理接收到的数据
///         var msg = e.Packet?.ToStr();
///         Send($"Echo: {msg}");
///     }
///     
///     protected override void OnDisconnected(String reason)
///     {
///         base.OnDisconnected(reason);
///         WriteLog($"客户端断开: {reason}");
///     }
/// }
/// </code>
/// </remarks>
public class NetSession : DisposeBase, INetSession, IServiceProvider, IExtend
{
    #region 属性
    /// <summary>唯一会话标识</summary>
    /// <remarks>在主服务中唯一标识当前会话，由服务器原子自增分配</remarks>
    public virtual Int32 ID { get; internal set; }

    /// <summary>主服务</summary>
    /// <remarks>负责管理当前会话的主服务器NetServer</remarks>
    NetServer INetSession.Host { get; set; } = null!;

    /// <summary>客户端Socket会话</summary>
    /// <remarks>跟客户端通讯的Socket会话，实际类型为服务端TcpSession/UdpSession</remarks>
    public ISocketSession Session { get; set; } = null!;

    /// <summary>Socket服务器</summary>
    /// <remarks>当前通讯所在的Socket服务器，实际类型为TcpServer/UdpServer</remarks>
    public ISocketServer Server { get; set; } = null!;

    /// <summary>客户端远程地址</summary>
    /// <remarks>获取客户端的网络地址信息</remarks>
    public NetUri Remote => Session.Remote;

    /// <summary>网络数据处理器</summary>
    /// <remarks>可作为业务处理实现，也可以作为前置协议解析，由服务器的 CreateHandler 方法创建</remarks>
    public INetHandler? Handler { get; set; }

    /// <summary>用户会话数据</summary>
    /// <remarks>与底层Socket会话共享的扩展数据字典，用于存储会话级别的自定义数据</remarks>
    public IDictionary<String, Object?> Items => Session.Items;

    /// <summary>获取/设置用户会话数据</summary>
    /// <param name="key">数据键名</param>
    /// <returns>数据值，不存在时返回null</returns>
    public virtual Object? this[String key] { get => Session[key]; set => Session[key] = value; }

    /// <summary>会话级作用域服务提供者</summary>
    /// <remarks>
    /// <para>根据会话创建Scoped范围服务，以使得各服务解析在本会话中唯一。</para>
    /// <para>基类使用内置ObjectContainer的Scope，在WebApi/Worker项目中，使用者需要自己创建Scope并赋值服务提供者。</para>
    /// </remarks>
    public IServiceProvider? ServiceProvider { get; set; }

    /// <summary>连接创建事件</summary>
    /// <remarks>创建会话并调用 <see cref="Start"/> 后触发</remarks>
    public event EventHandler<EventArgs>? Connected;

    /// <summary>连接断开事件</summary>
    /// <remarks>包括客户端主动断开、服务端主动断开以及服务端超时下线</remarks>
    public event EventHandler<EventArgs>? Disconnected;

    /// <summary>数据到达事件</summary>
    /// <remarks>当有新数据到达时触发，事件参数包含原始数据包和经过管道处理后的消息对象</remarks>
    public event EventHandler<ReceivedEventArgs>? Received;

    private Int32 _running;
    private IServiceScope? _scope;
    #endregion

    #region 方法
    /// <summary>开始会话处理</summary>
    /// <remarks>
    /// <para>启动会话的数据接收和事件处理流程，由服务器在创建会话后自动调用。</para>
    /// <para>执行流程：</para>
    /// <list type="number">
    /// <item>创建会话级服务提供者作用域</item>
    /// <item>创建并初始化网络处理器</item>
    /// <item>触发 <see cref="OnConnected"/> 和 <see cref="Connected"/> 事件</item>
    /// <item>注册Socket会话的数据接收和断开事件</item>
    /// </list>
    /// </remarks>
    public virtual void Start()
    {
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0) return;

        WriteLog("Connected {0}", Session);

        var host = (this as INetSession).Host;
        // 服务提供者，用于创建Scoped范围服务，以使得各服务解析在本会话中唯一
        if (ServiceProvider == null)
        {
            _scope = host.ServiceProvider?.CreateScope();
            ServiceProvider = _scope?.ServiceProvider ?? host.ServiceProvider;
        }

        using var span = host.Tracer?.NewSpan($"net:{host.Name}:Connect", Remote?.ToString());
        try
        {
            // 网络处理器，独立的业务处理器
            Handler = host.CreateHandler(this);
            Handler?.Init(this);

            OnConnected();

            var ss = Session;
            if (ss != null)
            {
                //// 网络会话和Socket会话共用用户会话数据
                //Items = ss.Items;

                ss.Received += Ss_Received;
                ss.OnDisposed += Ss_OnDisposed;
                ss.Error += OnError;
            }
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    /// <summary>处理Socket会话销毁事件</summary>
    /// <param name="sender">事件源</param>
    /// <param name="e">事件参数</param>
    private void Ss_OnDisposed(Object? sender, EventArgs e)
    {
        try
        {
            var reason = sender is SessionBase session && !session.CloseReason.IsNullOrEmpty()
                ? session.CloseReason
                : "Disconnect";
            Close(reason);
        }
        catch { }

        Dispose();
    }

    /// <summary>处理Socket会话数据接收事件</summary>
    /// <param name="sender">事件源</param>
    /// <param name="e">接收事件参数</param>
    private void Ss_Received(Object? sender, ReceivedEventArgs e)
    {
        var host = (this as INetSession).Host;
        var tracer = host?.Tracer;
        using var span = tracer?.NewSpan($"net:{host?.Name}:Receive", e.Message, e.Packet?.Total ?? 0);

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

    /// <summary>释放资源</summary>
    /// <remarks>子类重载实现资源释放逻辑时必须首先调用基类方法</remarks>
    /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        // 停止网络处理器
        Handler.TryDispose();
        Handler = null;

        var reason = GetType().Name + (disposing ? "Dispose" : "GC");

        try
        {
            Close(reason);
        }
        catch { }

        //Session.Dispose();//去掉这句话，因为在释放的时候Session有的时候为null，会出异常报错，导致整个程序退出。去掉后正常。
        Session?.Dispose();

        //Server = null;
        //Session = null;

        _scope?.Dispose();
        _scope = null;
    }

    /// <summary>关闭跟客户端的网络连接</summary>
    /// <remarks>
    /// <para>主动断开与客户端的连接。</para>
    /// <para>该方法保证 OnDisconnected 只被调用一次，即使多次调用 Close 也是安全的。</para>
    /// </remarks>
    /// <param name="reason">断开原因。包括 SendError/RemoveNotAlive/Dispose/GC 等，其中 ConnectionReset 为网络被动断开或对方断开</param>
    public void Close(String reason)
    {
        if (Interlocked.CompareExchange(ref _running, 0, 1) != 1) return;

        var host = (this as INetSession).Host;
        var remoteStr = Remote?.ToString();
        using var span = host?.Tracer?.NewSpan($"net:{host.Name}:Disconnect", new { remote = remoteStr, reason });
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
    /// <remarks>基类负责触发 <see cref="Connected"/> 事件，子类可重载添加自定义逻辑</remarks>
    protected virtual void OnConnected() => Connected?.Invoke(this, EventArgs.Empty);

    /// <summary>客户端连接已断开</summary>
    /// <remarks>基类负责触发 <see cref="Disconnected"/> 事件，子类可重载添加自定义逻辑</remarks>
    /// <param name="reason">断开原因。包括 SendError/RemoveNotAlive/Dispose/GC 等，其中 ConnectionReset 为网络被动断开或对方断开</param>
    protected virtual void OnDisconnected(String reason) => Disconnected?.Invoke(this, new EventArgs<String>(reason));

    /// <summary>客户端连接已断开</summary>
    /// <remarks>已过时，请使用 <see cref="OnDisconnected(String)"/></remarks>
    [Obsolete("=>OnDisconnected(String reason)")]
    protected virtual void OnDisconnected() => Disconnected?.Invoke(this, EventArgs.Empty);

    /// <summary>收到客户端发来的数据</summary>
    /// <remarks>
    /// <para>基类负责触发 <see cref="Received"/> 事件，子类可重载添加自定义处理逻辑。</para>
    /// <para>事件参数包含原始数据包 Packet 和经过管道处理后的消息对象 Message。</para>
    /// </remarks>
    /// <param name="e">接收事件参数</param>
    protected virtual void OnReceive(ReceivedEventArgs e) => Received?.Invoke(this, e);

    /// <summary>发生错误</summary>
    /// <remarks>可能是连接断开或其他网络异常</remarks>
    /// <param name="sender">事件源</param>
    /// <param name="e">异常事件参数</param>
    protected virtual void OnError(Object? sender, ExceptionEventArgs e) => WriteError(e.Exception.Message);
    #endregion

    #region 发送数据
    /// <summary>发送数据包</summary>
    /// <remarks>直达网卡，不经过管道处理</remarks>
    /// <param name="data">数据包</param>
    /// <returns>当前会话实例，支持链式调用</returns>
    public virtual INetSession Send(IPacket data)
    {
        var host = (this as INetSession).Host;
        using var span = host?.Tracer?.NewSpan($"net:{host.Name}:Send", data, data.Total);

        Session.Send(data);

        return this;
    }

    /// <summary>发送字节数组</summary>
    /// <remarks>直达网卡，不经过管道处理</remarks>
    /// <param name="data">字节数组</param>
    /// <param name="offset">数据起始偏移量</param>
    /// <param name="count">发送字节数，-1表示发送从偏移量开始的所有数据</param>
    /// <returns>当前会话实例，支持链式调用</returns>
    public virtual INetSession Send(Byte[] data, Int32 offset = 0, Int32 count = -1)
    {
        var host = (this as INetSession).Host;
        var len = count > 0 ? count : data.Length - offset;
        using var span = host?.Tracer?.NewSpan($"net:{host.Name}:Send", data.ToHex(offset, len > 64 ? 64 : len), len);

        Session.Send(data, offset, count);

        return this;
    }

    /// <summary>发送只读内存段</summary>
    /// <remarks>直达网卡，高性能API，避免不必要的内存拷贝</remarks>
    /// <param name="data">只读内存段</param>
    /// <returns>当前会话实例，支持链式调用</returns>
    public virtual INetSession Send(ReadOnlySpan<Byte> data)
    {
        var host = (this as INetSession).Host;
        using var span = host?.Tracer?.NewSpan($"net:{host.Name}:Send", null, data.Length);

        Session.Send(data);

        return this;
    }

    /// <summary>发送数据流</summary>
    /// <remarks>直达网卡，适用于大数据量流式传输场景</remarks>
    /// <param name="stream">数据流</param>
    /// <returns>当前会话实例，支持链式调用</returns>
    public virtual INetSession Send(Stream stream)
    {
        var host = (this as INetSession).Host;
        using var span = host?.Tracer?.NewSpan($"net:{host.Name}:Send");

        Session.Send(stream);

        return this;
    }

    /// <summary>发送字符串</summary>
    /// <remarks>直达网卡，使用指定编码将字符串转换为字节后发送</remarks>
    /// <param name="msg">要发送的字符串</param>
    /// <param name="encoding">字符编码，默认UTF-8</param>
    /// <returns>当前会话实例，支持链式调用</returns>
    public virtual INetSession Send(String msg, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        var host = (this as INetSession).Host;
        using var span = host?.Tracer?.NewSpan($"net:{host.Name}:Send", msg, encoding.GetByteCount(msg));

        Session.Send(msg, encoding);

        return this;
    }

    /// <summary>通过管道发送消息，不等待响应</summary>
    /// <remarks>管道内对消息进行报文封装处理，最终得到二进制数据进入网卡</remarks>
    /// <param name="message">应用层消息对象</param>
    /// <returns>实际发送的字节数</returns>
    public virtual Int32 SendMessage(Object message) => Session.SendMessage(message);

    /// <summary>通过管道发送响应消息</summary>
    /// <remarks>管道内对消息进行报文封装处理，与请求关联</remarks>
    /// <param name="message">响应消息对象</param>
    /// <param name="eventArgs">接收到请求的事件参数，用于关联请求上下文</param>
    /// <returns>实际发送的字节数</returns>
    public virtual Int32 SendReply(Object message, ReceivedEventArgs eventArgs) => (Session as SessionBase)!.SendMessage(message, eventArgs.Context);

    /// <summary>异步发送消息并等待响应</summary>
    /// <remarks>管道内对消息进行报文封装处理，最终得到二进制数据进入网卡</remarks>
    /// <param name="message">请求消息对象</param>
    /// <returns>响应消息对象</returns>
    public virtual Task<Object> SendMessageAsync(Object message) => Session.SendMessageAsync(message);

    /// <summary>异步发送消息并等待响应</summary>
    /// <remarks>管道内对消息进行报文封装处理，支持超时取消</remarks>
    /// <param name="message">请求消息对象</param>
    /// <param name="cancellationToken">取消令牌，用于超时控制</param>
    /// <returns>响应消息对象</returns>
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
    /// <param name="format">格式化字符串</param>
    /// <param name="args">参数</param>
    public virtual void WriteLog(String format, params Object?[] args) => Log?.Info(LogPrefix + format, args);

    /// <summary>输出错误日志</summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">参数</param>
    public virtual void WriteError(String format, params Object?[] args) => Log?.Error(LogPrefix + format, args);
    #endregion

    #region 辅助
    /// <summary>已重载。返回会话的字符串表示</summary>
    /// <returns>格式为 "服务名[会话ID] Socket会话信息"</returns>
    public override String ToString() => $"{(this as INetSession).Host?.Name}[{ID}] {Session}";

    /// <summary>获取服务</summary>
    /// <remarks>
    /// <para>实现 <see cref="IServiceProvider"/> 接口，支持依赖注入。</para>
    /// <para>优先返回内置类型：IServiceProvider、NetSession、INetSession、NetServer、ISocketSession、ISocketServer。</para>
    /// <para>其他类型从 <see cref="ServiceProvider"/> 获取。</para>
    /// </remarks>
    /// <param name="serviceType">服务类型</param>
    /// <returns>服务实例</returns>
    public virtual Object GetService(Type serviceType)
    {
        if (serviceType == typeof(IServiceProvider)) return this;
        if (serviceType == typeof(NetSession)) return this;
        if (serviceType == typeof(INetSession)) return this;
        if (serviceType == typeof(NetServer)) return (this as INetSession).Host;
        if (serviceType == typeof(ISocketSession)) return Session;
        if (serviceType == typeof(ISocketServer)) return Server;

        return ServiceProvider?.GetService(serviceType)!;
    }
    #endregion
}