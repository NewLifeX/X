using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;
using NewLife.Threading;

namespace NewLife.Net;

/// <summary>网络服务器。可同时支持多个Socket服务器，同时支持IPv4和IPv6，同时支持Tcp和Udp</summary>
/// <remarks>
/// <para>网络服务器是应用层的核心组件，封装了底层Socket服务器的管理和网络会话的生命周期。</para>
/// <para>设计理念：</para>
/// <list type="bullet">
/// <item>多协议支持 - 单个NetServer可同时监听TCP和UDP</item>
/// <item>双栈支持 - 自动创建IPv4和IPv6监听</item>
/// <item>会话管理 - 自动维护客户端会话集合</item>
/// <item>管道处理 - 支持消息编解码管道</item>
/// </list>
/// <para>收到请求 <see cref="Server_NewSession"/> 后，会建立 <see cref="CreateSession"/> 会话，并加入到会话集合 <see cref="Sessions"/> 中，然后启动 <see cref="Start"/> 会话处理。</para>
/// <para>标准用法：指定端口后直接 <see cref="Start"/>，NetServer将同时监听Tcp/Udp和IPv4/IPv6（会检查是否支持）四个端口。</para>
/// <para>高级用法：重载方法 <see cref="EnsureCreateServer"/> 来创建一个SocketServer并赋值给 <see cref="Server"/> 属性，<see cref="EnsureCreateServer"/> 将会在 <see cref="OnStart"/> 时首先被调用。</para>
/// <para>超级用法：使用 <see cref="AttachServer"/> 方法向网络服务器添加Socket服务，其中第一个将作为默认Socket服务 <see cref="Server"/>。</para>
/// <para>创建规则：</para>
/// <list type="bullet">
/// <item>如果Socket服务集合 <see cref="Servers"/> 为空，将依据地址 <see cref="Local"/>、端口 <see cref="Port"/>、地址族 <see cref="AddressFamily"/>、协议 <see cref="ProtocolType"/> 创建默认Socket服务</item>
/// <item>如果地址族 <see cref="AddressFamily"/> 指定为IPv4和IPv6以外的值，将同时创建IPv4和IPv6两个Socket服务</item>
/// <item>如果协议 <see cref="ProtocolType"/> 指定为Tcp和Udp以外的值，将同时创建Tcp和Udp两个Socket服务</item>
/// <item>默认情况下，地址族 <see cref="AddressFamily"/> 和协议 <see cref="ProtocolType"/> 都是其它值，所以一共将会创建四个Socket服务（Tcp、Tcpv6、Udp、Udpv6）</item>
/// </list>
/// <para>典型用法：</para>
/// <code>
/// var server = new NetServer
/// {
///     Port = 8080,
///     ProtocolType = NetType.Tcp
/// };
/// server.Received += (s, e) =>
/// {
///     var session = s as INetSession;
///     session.Send(e.Packet); // Echo
/// };
/// server.Start();
/// </code>
/// </remarks>
public class NetServer : DisposeBase, IServer, IExtend, ILogFeature
{
    #region 属性
    /// <summary>服务名</summary>
    /// <remarks>默认为类名去掉Server后缀，用于日志输出和会话命名</remarks>
    public String Name { get; set; }

    private NetUri _Local = new();
    /// <summary>本地绑定地址</summary>
    /// <remarks>
    /// <para>指定服务器监听的网络地址，包含协议类型、IP地址和端口。</para>
    /// <para>设置时会自动推断地址族，除非Host为"*"表示监听所有地址。</para>
    /// </remarks>
    public NetUri Local
    {
        get => _Local;
        set
        {
            _Local = value;
            if (AddressFamily <= AddressFamily.Unspecified && value.Host != "*")
                AddressFamily = value.Address.AddressFamily;
        }
    }

    /// <summary>监听端口</summary>
    /// <remarks>设置为0时，启动后系统自动分配可用端口</remarks>
    public Int32 Port { get => _Local.Port; set => _Local.Port = value; }

    /// <summary>协议类型</summary>
    /// <remarks>
    /// <para>指定监听的网络协议：</para>
    /// <list type="bullet">
    /// <item>Tcp - 仅监听TCP</item>
    /// <item>Udp - 仅监听UDP</item>
    /// <item>Unknown - 同时监听TCP和UDP（默认）</item>
    /// <item>Http/WebSocket - 监听HTTP协议</item>
    /// </list>
    /// </remarks>
    public NetType ProtocolType { get => _Local.Type; set => _Local.Type = value; }

    /// <summary>地址族</summary>
    /// <remarks>
    /// <para>指定监听的IP版本：</para>
    /// <list type="bullet">
    /// <item>InterNetwork - 仅IPv4</item>
    /// <item>InterNetworkV6 - 仅IPv6</item>
    /// <item>Unspecified - 同时监听IPv4和IPv6（默认）</item>
    /// </list>
    /// </remarks>
    public AddressFamily AddressFamily { get; set; }

    /// <summary>底层Socket服务器集合</summary>
    /// <remarks>包含所有创建的TcpServer和UdpServer实例</remarks>
    public IList<ISocketServer> Servers { get; private set; } = [];

    /// <summary>默认Socket服务器</summary>
    /// <remarks>
    /// <para>返回服务器集合中的第一个服务器。</para>
    /// <para>如果集合为空，会自动调用 <see cref="EnsureCreateServer"/> 创建服务器。</para>
    /// </remarks>
    public ISocketServer? Server
    {
        get
        {
            var ss = Servers;
            if (ss.Count <= 0) EnsureCreateServer();

            return ss.Count > 0 ? ss[0] : null;
        }
        set
        {
            if (value == null)
                Servers.Clear();
            else
            {
                var ss = Servers;
                if (!ss.Contains(value)) ss.Insert(0, value);
            }
        }
    }

    /// <summary>是否活动</summary>
    /// <remarks>当服务器集合非空且默认服务器已启动时返回true</remarks>
    public Boolean Active => Servers.Count > 0 && Server != null && Server.Active;

    /// <summary>会话超时时间（秒）</summary>
    /// <remarks>
    /// <para>对于每一个会话连接，如果超过该时间仍然没有收到任何数据，则断开会话连接。</para>
    /// <para>默认0秒，使用SocketServer默认值（通常为20分钟）。</para>
    /// </remarks>
    public Int32 SessionTimeout { get; set; }

    /// <summary>消息管道</summary>
    /// <remarks>
    /// <para>收发消息都经过管道处理器，进行协议编码解码。</para>
    /// <para>处理顺序：</para>
    /// <list type="number">
    /// <item>接收数据解码时，从前向后通过管道处理器</item>
    /// <item>发送数据编码时，从后向前通过管道处理器</item>
    /// </list>
    /// </remarks>
    public IPipeline? Pipeline { get; set; }

    /// <summary>是否使用会话集合</summary>
    /// <remarks>
    /// <para>允许遍历会话，支持群发消息等功能。默认true。</para>
    /// <para>禁用后不维护会话集合，可减少内存占用。</para>
    /// </remarks>
    public Boolean UseSession { get; set; } = true;

    /// <summary>地址重用</summary>
    /// <remarks>
    /// <para>主要应用于网络服务器重启交替。默认false。</para>
    /// <para>一个端口释放后会等待两分钟之后才能再被使用，SO_REUSEADDR是让端口释放后立即就可以被再次使用。</para>
    /// <para>SO_REUSEADDR用于对TCP套接字处于TIME_WAIT状态下的socket(TCP连接中, 先调用close()的一方会进入TIME_WAIT状态)，才可以重复绑定使用。</para>
    /// </remarks>
    public Boolean ReuseAddress { get; set; }

    /// <summary>SSL协议版本</summary>
    /// <remarks>默认None表示不启用SSL，设置为Tls12或Tls13启用安全传输</remarks>
    public SslProtocols SslProtocol { get; set; } = SslProtocols.None;

    /// <summary>X509证书</summary>
    /// <remarks>
    /// <para>用于SSL连接时验证证书指纹，可以直接加载pem证书文件，未指定时不验证证书。</para>
    /// <para>可以使用pfx证书文件，也可以使用pem证书文件。服务端必须指定证书。</para>
    /// <code>var cert = new X509Certificate2("file", "pass");</code>
    /// </remarks>
    public X509Certificate? Certificate { get; set; }

    /// <summary>APM性能追踪器</summary>
    /// <remarks>用于应用层的性能追踪，记录会话连接、数据收发等关键操作</remarks>
    public ITracer? Tracer { get; set; }

    /// <summary>Socket层性能追踪器</summary>
    /// <remarks>用于内部Socket服务器的APM性能追踪，便于调试底层网络问题</remarks>
    public ITracer? SocketTracer { get; set; }

    /// <summary>统计信息显示周期（秒）</summary>
    /// <remarks>默认600秒，设置为0表示不显示统计信息</remarks>
    public Int32 StatPeriod { get; set; } = 600;

    /// <summary>是否输出发送日志</summary>
    /// <remarks>默认false，启用后会在日志中输出发送的数据内容</remarks>
    public Boolean LogSend { get; set; }

    /// <summary>是否输出接收日志</summary>
    /// <remarks>默认false，启用后会在日志中输出接收的数据内容</remarks>
    public Boolean LogReceive { get; set; }

    /// <summary>服务提供者</summary>
    /// <remarks>
    /// <para>用于网络服务器内部解析各种服务，可以直接赋值或者依赖注入。</para>
    /// <para>网络会话默认使用该提供者，应用系统可以在网络会话中创建Scope版服务提供者。</para>
    /// </remarks>
    public IServiceProvider? ServiceProvider { get; set; }

    private ConcurrentDictionary<String, Object?>? _items;
    /// <summary>扩展数据字典</summary>
    /// <remarks>用于存储服务器级别的自定义数据</remarks>
    public IDictionary<String, Object?> Items => _items ??= new();

    /// <summary>获取/设置扩展数据</summary>
    /// <param name="key">数据键名</param>
    /// <returns>数据值，不存在时返回null</returns>
    public Object? this[String key] { get => _items != null && _items.TryGetValue(key, out var obj) ? obj : null; set => Items[key] = value; }
    #endregion

    #region 构造
    /// <summary>实例化一个网络服务器</summary>
    public NetServer()
    {
        Name = GetType().Name.TrimEnd("Server");

        if (SocketSetting.Current.Debug) Log = XTrace.Log;
    }

    /// <summary>通过指定端口实例化一个网络服务器</summary>
    /// <param name="port">监听端口</param>
    public NetServer(Int32 port) : this(IPAddress.Any, port) { }

    /// <summary>通过指定监听地址和端口实例化一个网络服务器</summary>
    /// <param name="address">监听地址</param>
    /// <param name="port">监听端口</param>
    public NetServer(IPAddress address, Int32 port) : this(address, port, NetType.Unknown) { }

    /// <summary>通过指定监听地址、端口和协议实例化一个网络服务器</summary>
    /// <remarks>默认支持Tcp协议和Udp协议</remarks>
    /// <param name="address">监听地址</param>
    /// <param name="port">监听端口</param>
    /// <param name="protocolType">协议类型</param>
    public NetServer(IPAddress address, Int32 port, NetType protocolType) : this()
    {
        Local.Address = address;
        Port = port;
        Local.Type = protocolType;
    }

    /// <summary>释放资源</summary>
    /// <remarks>释放会话集合和底层Socket服务器</remarks>
    /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        if (Active) Stop(GetType().Name + (disposing ? "Dispose" : "GC"));
    }
    #endregion

    #region 创建
    /// <summary>添加Socket服务器</summary>
    /// <remarks>
    /// <para>将Socket服务器添加到服务器集合中，并配置相关属性。</para>
    /// <para>自动设置服务器名称、会话超时、管道、日志等属性。</para>
    /// </remarks>
    /// <param name="server">要添加的Socket服务器</param>
    /// <returns>添加是否成功，如果已存在则返回false</returns>
    public virtual Boolean AttachServer(ISocketServer server)
    {
        if (Servers.Contains(server)) return false;

        server.Name = $"{Name}{(server.Local.IsTcp ? "Tcp" : "Udp")}{(server.Local.Address.IsIPv4() ? "" : "6")}";
        server.NewSession += Server_NewSession;

        if (SessionTimeout > 0) server.SessionTimeout = SessionTimeout;
        if (Pipeline != null) server.Pipeline = Pipeline;

        // 内部服务器日志更多是为了方便网络库调试，而网络服务器日志用于应用开发
        if (SocketLog != null) server.Log = SocketLog;
        if (SocketTracer != null) server.Tracer = SocketTracer;
        server.LogSend = LogSend;
        server.LogReceive = LogReceive;

        server.Error += OnError;

        if (server is TcpServer tcpServer)
        {
            tcpServer.ReuseAddress = ReuseAddress;
            tcpServer.SslProtocol = SslProtocol;
            if (Certificate != null) tcpServer.Certificate = Certificate;
        }
        else if (server is UdpServer udpServer)
        {
            udpServer.ReuseAddress = ReuseAddress;
        }

        Servers.Add(server);

        return true;
    }

    /// <summary>添加服务器监听</summary>
    /// <remarks>
    /// <para>同时添加指定端口的IPv4和IPv6服务器。</para>
    /// <para>如果协议不是指定的Tcp或Udp，则同时添加Tcp和Udp服务器。</para>
    /// </remarks>
    /// <param name="address">监听地址</param>
    /// <param name="port">监听端口</param>
    /// <param name="protocol">协议类型</param>
    /// <param name="family">地址族</param>
    /// <returns>添加的服务器数量</returns>
    public virtual Int32 AddServer(IPAddress address, Int32 port, NetType protocol = NetType.Unknown, AddressFamily family = AddressFamily.Unspecified)
    {
        var list = CreateServer(address, port, protocol, family);
        var count = 0;
        foreach (var item in list)
        {
            AttachServer(item);

            count++;
        }
        return count;
    }

    /// <summary>确保创建服务器</summary>
    /// <remarks>如果服务器集合为空，根据Local配置自动创建服务器</remarks>
    public virtual void EnsureCreateServer()
    {
        if (Servers.Count <= 0)
        {
            var uri = Local;
            var family = AddressFamily;
            if (family <= AddressFamily.Unspecified && uri.Host != "*" && !uri.Address.IsAny())
                family = uri.Address.AddressFamily;
            var list = CreateServer(uri.Address, uri.Port, uri.Type, family);
            foreach (var item in list)
            {
                AttachServer(item);
            }
        }
    }

    /// <summary>添加管道处理器</summary>
    /// <typeparam name="THandler">处理器类型</typeparam>
    public void Add<THandler>() where THandler : IPipelineHandler, new() => GetPipe().Add(new THandler());

    /// <summary>添加管道处理器</summary>
    /// <param name="handler">处理器实例</param>
    public void Add(IPipelineHandler handler) => GetPipe().Add(handler);

    /// <summary>获取或创建管道</summary>
    /// <returns>管道实例</returns>
    private IPipeline GetPipe() => Pipeline ??= new Pipeline();
    #endregion

    #region 方法
    /// <summary>开始服务</summary>
    /// <remarks>
    /// <para>启动所有Socket服务器，开始监听端口。</para>
    /// <para>如果端口为0，启动后会自动分配可用端口并更新 <see cref="Port"/> 属性。</para>
    /// </remarks>
    public void Start()
    {
        if (Active) return;

        OnStart();

        if (Server == null)
        {
            WriteLog("没有可用Socket服务器！");

            return;
        }

        Local.Type = Server.Local.Type;

        WriteLog("准备就绪！");
    }

    /// <summary>开始时调用的方法</summary>
    /// <remarks>
    /// <para>子类可重载此方法添加自定义启动逻辑。</para>
    /// <para>执行流程：</para>
    /// <list type="number">
    /// <item>调用 <see cref="EnsureCreateServer"/> 确保服务器已创建</item>
    /// <item>启动所有Socket服务器</item>
    /// <item>处理随机端口分配</item>
    /// <item>输出管道处理器信息</item>
    /// <item>启动统计定时器</item>
    /// </list>
    /// </remarks>
    protected virtual void OnStart()
    {
        EnsureCreateServer();

        if (Servers.Count == 0) throw new Exception($"Failed to listen to all ports! Port=[{Port}]");

        var snapshot = Servers.ToArray();
        WriteLog("准备开始监听{0}个服务器", snapshot.Length);

        foreach (var item in snapshot)
        {
            item.Start();

            // 如果是随机端口，反写回来，并且修改其它服务器的端口
            if (Port == 0)
            {
                Port = item.Port;

                foreach (var elm in Servers)
                {
                    if (elm != item && elm.Port == 0) elm.Port = Port;
                }
            }
            WriteLog("开始监听 {0}", item);
        }

        if (Pipeline is Pipeline pipe && pipe.Handlers.Count > 0)
        {
            WriteLog("初始化管道：");
            foreach (var handler in pipe.Handlers)
            {
                WriteLog("    {0}", handler);
            }
        }

        if (StatPeriod > 0) _Timer = new TimerX(ShowStat, null, 10_000, StatPeriod * 1000);
    }

    /// <summary>停止服务</summary>
    /// <remarks>停止所有Socket服务器，释放会话和资源</remarks>
    /// <param name="reason">关闭原因，便于日志分析</param>
    public void Stop(String? reason)
    {
        _Timer.TryDispose();
        _Timer = null;

        var ss = Servers.Where(e => e.Active).ToArray();
        if (ss == null || ss.Length == 0) return;

        OnStop(reason);

        WriteLog("已停止！");
    }

    /// <summary>停止时调用的方法</summary>
    /// <remarks>
    /// <para>子类可重载此方法添加自定义停止逻辑。</para>
    /// <para>执行流程：</para>
    /// <list type="number">
    /// <item>停止所有活动的Socket服务器</item>
    /// <item>释放所有网络会话</item>
    /// <item>释放所有服务器实例</item>
    /// </list>
    /// </remarks>
    /// <param name="reason">关闭原因，便于日志分析</param>
    protected virtual void OnStop(String? reason)
    {
        var ss = Servers.Where(e => e.Active).ToArray();
        WriteLog("准备停止监听{0}个服务器 {1}", ss.Length, reason);

        if (reason.IsNullOrEmpty()) reason = GetType().Name + "Stop";
        foreach (var item in ss)
        {
            WriteLog("停止监听 {0}", item);
            item.Stop(reason);
        }

        var sessions = _Sessions;
        if (sessions != null && sessions.Count > 0)
        {
            WriteLog("准备释放网络会话{0}个！", sessions.Count);
            foreach (var item in sessions.Values.ToArray())
            {
                item.TryDispose();
            }
            sessions.Clear();
        }

        var severs = Servers;
        if (severs != null && severs.Count > 0)
        {
            WriteLog("准备释放服务{0}个！", severs.Count);
            foreach (var item in severs)
            {
                item.TryDispose();
            }
            severs.Clear();
        }
    }
    #endregion

    #region 业务
    /// <summary>新会话事件</summary>
    /// <remarks>对于TCP是新连接，对于UDP是新客户端</remarks>
    public event EventHandler<NetSessionEventArgs>? NewSession;

    /// <summary>数据接收事件</summary>
    /// <remarks>某个会话的数据到达时触发，sender是INetSession</remarks>
    public event EventHandler<ReceivedEventArgs>? Received;

    /// <summary>处理Socket服务器的新会话事件</summary>
    /// <remarks>接受连接时触发，对于Udp是收到数据时（同时触发OnReceived）</remarks>
    /// <param name="sender">事件源</param>
    /// <param name="e">会话事件参数</param>
    void Server_NewSession(Object? sender, SessionEventArgs e)
    {
        var session = e.Session;

        var ns = OnNewSession(session);

        NewSession?.Invoke(sender, new NetSessionEventArgs { Session = ns });
    }

    private Int32 _sessionID = 0;
    /// <summary>收到连接时，建立会话</summary>
    /// <remarks>
    /// <para>创建网络会话实例，挂接数据接收和错误处理事件。</para>
    /// <para>自动分配会话ID，配置日志和服务提供者。</para>
    /// </remarks>
    /// <param name="session">底层Socket会话</param>
    /// <returns>创建的网络会话</returns>
    protected virtual INetSession OnNewSession(ISocketSession session)
    {
        var count = Interlocked.Increment(ref _SessionCount);
        session.OnDisposed += (s, e2) => Interlocked.Decrement(ref _SessionCount);

        // 使用原子操作更新最高会话数
        var max = _maxSessionCount;
        while (count > max)
        {
            if (Interlocked.CompareExchange(ref _maxSessionCount, count, max) == max) break;
            max = _maxSessionCount;
        }

        var ns = CreateSession(session);
        // sessionID变大后，可能达到最大值，然后变为-1，再变为0，所以不用担心
        //ns.ID = ++sessionID;
        // 网络会话改为原子操作，避免多线程冲突
        if (ns is NetSession ns2)
        {
            ns2.ID = Interlocked.Increment(ref _sessionID);
            //ns2.ServiceProvider = ServiceProvider;
            ns2.Log = SessionLog;
        }
        ns.Host = this;
        ns.Server = session.Server;
        ns.Session = session;

        if (UseSession) AddSession(ns);

        ns.Received += OnReceived;

        // 开始会话处理
        ns.Start();

        return ns;
    }

    /// <summary>处理会话数据接收事件</summary>
    /// <param name="sender">事件源（INetSession）</param>
    /// <param name="e">接收事件参数</param>
    void OnReceived(Object? sender, ReceivedEventArgs e)
    {
        if (sender is INetSession session)
        {
            if (e.Packet != null) OnReceive(session, e.Packet);
            OnReceive(session, e);
        }

        Received?.Invoke(sender, e);
    }

    /// <summary>收到数据时的处理</summary>
    /// <remarks>最原始的数据处理，但不影响会话内部的数据处理。子类可重载添加自定义逻辑</remarks>
    /// <param name="session">网络会话</param>
    /// <param name="pk">数据包</param>
    protected virtual void OnReceive(INetSession session, IPacket pk) { }

    /// <summary>收到数据时的处理</summary>
    /// <remarks>最原始的数据处理，但不影响会话内部的数据处理。子类可重载添加自定义逻辑</remarks>
    /// <param name="session">网络会话</param>
    /// <param name="e">接收事件参数</param>
    protected virtual void OnReceive(INetSession session, ReceivedEventArgs e) { }

    /// <summary>错误事件</summary>
    /// <remarks>错误发生或断开连接时触发，sender是ISocketSession</remarks>
    public event EventHandler<ExceptionEventArgs>? Error;

    /// <summary>触发异常</summary>
    /// <param name="sender">事件源</param>
    /// <param name="e">异常事件参数</param>
    protected virtual void OnError(Object? sender, ExceptionEventArgs e)
    {
        if (Log.Enable) Log.Error("{0} Error {1}", sender, e.Exception);

        Error?.Invoke(sender, e);
    }
    #endregion

    #region 会话
    private readonly ConcurrentDictionary<Int32, INetSession> _Sessions = new();
    /// <summary>会话集合</summary>
    /// <remarks>用自增的数字ID作为标识，业务应用自己维持ID与业务主键的对应关系</remarks>
    public IDictionary<Int32, INetSession> Sessions => _Sessions;

    private Int32 _SessionCount;
    /// <summary>当前会话数</summary>
    public Int32 SessionCount { get => _SessionCount; set => _SessionCount = value; }

    private volatile Int32 _maxSessionCount;
    /// <summary>最高会话数</summary>
    /// <remarks>记录服务器运行期间的最大并发会话数</remarks>
    public Int32 MaxSessionCount => _maxSessionCount;

    /// <summary>添加会话</summary>
    /// <remarks>
    /// <para>子类可以在添加会话前对会话进行一些处理。</para>
    /// <para>自动注册会话释放事件，从集合中移除。</para>
    /// </remarks>
    /// <param name="session">要添加的会话</param>
    protected virtual void AddSession(INetSession session)
    {
        session.Host ??= this;

        if (_Sessions.TryAdd(session.ID, session))
        {
            session.OnDisposed += (s, e) =>
            {
                if (s is INetSession ns) _Sessions.TryRemove(ns.ID, out _);
            };
        }
        else
        {
            // 并发或重复添加时仅记录日志，避免抛异常打断连接流程
            if (Log.Enable) Log.Warn("会话已存在，忽略重复添加。ID={0}", session.ID);
        }
    }

    /// <summary>创建会话</summary>
    /// <remarks>子类可重载此方法返回自定义会话类型</remarks>
    /// <param name="session">底层Socket会话</param>
    /// <returns>创建的网络会话实例</returns>
    protected virtual INetSession CreateSession(ISocketSession session)
    {
        var ns = new NetSession
        {
            Server = session.Server,
            Session = session
        };
        (ns as INetSession).Host = this;

        return ns;
    }

    /// <summary>根据会话ID查找会话</summary>
    /// <param name="sessionid">会话ID</param>
    /// <returns>找到的会话，不存在时返回null</returns>
    public INetSession? GetSession(Int32 sessionid)
    {
        if (sessionid == 0) return null;

        return _Sessions.TryGetValue(sessionid, out var ns) ? ns : null;
    }

    /// <summary>为会话创建网络数据处理器</summary>
    /// <remarks>可作为业务处理实现，也可以作为前置协议解析。子类可重载返回自定义处理器</remarks>
    /// <param name="session">网络会话</param>
    /// <returns>处理器实例，默认返回null</returns>
    public virtual INetHandler? CreateHandler(INetSession session) => null;
    #endregion

    #region 群发
    /// <summary>异步群发数据给所有客户端</summary>
    /// <param name="data">要发送的数据包</param>
    /// <returns>已群发客户端总数</returns>
    public virtual Task<Int32> SendAllAsync(IPacket data) => SendAllAsync(data, null);

    /// <summary>异步群发数据给所有客户端</summary>
    /// <param name="data">要发送的数据包</param>
    /// <param name="predicate">过滤器，判断指定会话是否需要发送，null表示发送给所有会话</param>
    /// <returns>已群发客户端总数</returns>
    public virtual Task<Int32> SendAllAsync(IPacket data, Func<INetSession, Boolean>? predicate = null)
    {
        if (!UseSession) throw new ArgumentOutOfRangeException(nameof(UseSession), true, "Mass posting requires the use of session collections");

        var count = 0;
        // 直接遍历Values，避免KeyValuePair的额外开销
        foreach (var session in _Sessions.Values)
        {
            if (predicate == null || predicate(session))
            {
                try
                {
                    session.Send(data);
                    count++;
                }
                catch { }
            }
        }

        return Task.FromResult(count);
    }

    /// <summary>群发管道消息给所有客户端</summary>
    /// <remarks>不等待响应，支持协议编码</remarks>
    /// <param name="message">应用消息，底层对其进行协议编码</param>
    /// <param name="predicate">过滤器，判断指定会话是否需要发送，null表示发送给所有会话</param>
    /// <returns>已群发客户端总数</returns>
    public virtual Int32 SendAllMessage(Object message, Func<INetSession, Boolean>? predicate = null)
    {
        if (!UseSession) throw new ArgumentOutOfRangeException(nameof(UseSession), true, "Mass posting requires the use of session collections");

        var count = 0;
        // 直接遍历Values，避免KeyValuePair的额外开销
        foreach (var session in _Sessions.Values)
        {
            if (predicate == null || predicate(session))
            {
                try
                {
                    session.SendMessage(message);
                    count++;
                }
                catch { }
            }
        }

        return count;
    }
    #endregion

    #region 创建Tcp/Udp、IPv4/IPv6服务
    /// <summary>创建Tcp/Udp、IPv4/IPv6服务</summary>
    /// <param name="address">监听地址</param>
    /// <param name="port">监听端口</param>
    /// <param name="protocol">协议类型</param>
    /// <param name="family">地址族</param>
    /// <returns>创建的服务器数组</returns>
    protected ISocketServer[] CreateServer(IPAddress address, Int32 port, NetType protocol, AddressFamily family)
    {
        switch (protocol)
        {
            case NetType.Tcp:
                return CreateServer<TcpServer>(address, port, family);
            case NetType.Http:
            case NetType.WebSocket:
                var ss = CreateServer<TcpServer>(address, port, family);
                foreach (var item in ss)
                {
                    if (item is TcpServer tcp) tcp.EnableHttp = true;
                }
                return ss;
            case NetType.Udp:
                return CreateServer<UdpServer>(address, port, family);
            case NetType.Unknown:
            default:
                var list = new List<ISocketServer>();

                // 其它未知协议，同时用Tcp和Udp
                list.AddRange(CreateServer<TcpServer>(address, port, family));
                list.AddRange(CreateServer<UdpServer>(address, port, family));

                return list.ToArray();
        }
    }

    /// <summary>创建指定类型的服务器</summary>
    /// <typeparam name="TServer">服务器类型</typeparam>
    /// <param name="address">监听地址</param>
    /// <param name="port">监听端口</param>
    /// <param name="family">地址族</param>
    /// <returns>创建的服务器数组</returns>
    ISocketServer[] CreateServer<TServer>(IPAddress address, Int32 port, AddressFamily family) where TServer : ISocketServer, new()
    {
        var list = new List<ISocketServer>();
        switch (family)
        {
            case AddressFamily.InterNetwork:
            case AddressFamily.InterNetworkV6:
                var addr = address.GetRightAny(family);
                if (addr != null)
                {
                    var svr = new TServer();
                    svr.Local.Address = addr;
                    svr.Local.Port = port;
                    //svr.AddressFamily = family;
                    //svr.Tracer = SocketTracer;

                    // 协议端口不能是已经被占用
                    if (ReuseAddress || !svr.Local.CheckPort()) list.Add(svr);
                }
                break;
            default:
                // 其它情况表示同时支持IPv4和IPv6
                // 兼容Linux
                //if (Socket.OSSupportsIPv4)
                list.AddRange(CreateServer<TServer>(address, port, AddressFamily.InterNetwork));
                if (Socket.OSSupportsIPv6 && !Runtime.Mono)
                    list.AddRange(CreateServer<TServer>(address, port, AddressFamily.InterNetworkV6));
                break;
        }

        return list.ToArray();
    }
    #endregion

    #region 统计
    private TimerX? _Timer;
    private String? _Last;

    /// <summary>显示统计信息</summary>
    /// <param name="state">定时器状态</param>
    private void ShowStat(Object? state)
    {
        var msg = GetStat();
        if (msg.IsNullOrEmpty() || msg == _Last) return;
        _Last = msg;

        WriteLog(msg);
    }

    /// <summary>获取统计信息</summary>
    /// <returns>统计信息字符串</returns>
    public String GetStat()
    {
        var max = _maxSessionCount;
        if (max <= 0) return String.Empty;

        var sb = Pool.StringBuilder.Get();
        SessionCount = _Sessions.Count;
        sb.AppendFormat("在线：{0:n0}/{1:n0} ", SessionCount, max);

        return sb.Return(true);
    }
    #endregion

    #region 日志
    /// <summary>日志提供者</summary>
    public ILog Log { get; set; } = Logger.Null;

    /// <summary>Socket层日志提供者</summary>
    /// <remarks>用于内部Socket服务器的日志输出，便于调试底层网络问题</remarks>
    public ILog? SocketLog { get; set; }

    /// <summary>会话日志提供者</summary>
    /// <remarks>用于网络会话的日志输出</remarks>
    public ILog? SessionLog { get; set; }

    private String? _LogPrefix;
    /// <summary>日志前缀</summary>
    public virtual String LogPrefix
    {
        get
        {
            _LogPrefix ??= Name;
            return _LogPrefix;
        }
        set => _LogPrefix = value;
    }

    /// <summary>写日志</summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">参数</param>
    public virtual void WriteLog(String format, params Object?[] args)
    {
        if (!LogPrefix.EndsWith(" ") && !format.StartsWith(" ")) format = " " + format;
        Log.Info(LogPrefix + format, args);
    }

    /// <summary>输出错误日志</summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">参数</param>
    public virtual void WriteError(String format, params Object?[] args) => Log.Error(LogPrefix + format, args);
    #endregion

    #region 辅助
    /// <summary>已重载。返回服务器的字符串表示</summary>
    /// <returns>格式为 "服务名 [监听地址列表]"</returns>
    public override String ToString()
    {
        var servers = Servers;
        if (servers == null || servers.Count <= 0) return Name;

        if (servers.Count == 1) return Name + " " + servers[0].ToString();

        var sb = Pool.StringBuilder.Get();
        foreach (var item in servers)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(item);
        }
        return Name + " " + sb.Return(true);
    }
    #endregion
}

/// <summary>泛型网络服务器，支持指定会话类型</summary>
/// <typeparam name="TSession">会话类型，必须继承自 <see cref="INetSession"/> 且有无参构造函数</typeparam>
/// <remarks>
/// <para>使用泛型服务器可以自动创建自定义会话类型，无需重载 CreateSession 方法。</para>
/// <code>
/// public class MyServer : NetServer&lt;MySession&gt; { }
/// 
/// public class MySession : NetSession
/// {
///     protected override void OnReceive(ReceivedEventArgs e)
///     {
///         base.OnReceive(e);
///         // 自定义处理逻辑
///     }
/// }
/// </code>
/// </remarks>
public class NetServer<TSession> : NetServer where TSession : class, INetSession, new()
{
    /// <summary>创建会话</summary>
    /// <param name="session">底层Socket会话</param>
    /// <returns>创建的自定义会话实例</returns>
    protected override INetSession CreateSession(ISocketSession session)
    {
        var ns = new TSession
        {
            Host = this,
            Server = session.Server,
            Session = session
        };

        return ns;
    }

    /// <summary>获取指定标识的会话</summary>
    /// <remarks>返回强类型的自定义会话实例</remarks>
    /// <param name="id">会话ID</param>
    /// <returns>找到的会话，不存在时返回null</returns>
    public new TSession? GetSession(Int32 id)
    {
        if (id <= 0) return null;

        return base.GetSession(id) as TSession;
    }
}