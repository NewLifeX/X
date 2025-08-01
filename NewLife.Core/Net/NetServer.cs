﻿using System.Collections.Concurrent;
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
/// 网络服务器模型，所有网络应用服务器可以通过继承该类实现。
/// 该类仅实现了业务应用对网络流的操作，与具体网络协议无关。
/// 
/// 收到请求<see cref="Server_NewSession"/>后，会建立<see cref="CreateSession"/>会话，并加入到会话集合<see cref="Sessions"/>中，然后启动<see cref="Start"/>会话处理；
/// 
/// 标准用法：
/// 指定端口后直接<see cref="Start"/>，NetServer将同时监听Tcp/Udp和IPv4/IPv6（会检查是否支持）四个端口。
/// 
/// 高级用法：
/// 重载方法<see cref="EnsureCreateServer"/>来创建一个SocketServer并赋值给<see cref="Server"/>属性，<see cref="EnsureCreateServer"/>将会在<see cref="OnStart"/>时首先被调用。
/// 
/// 超级用法：
/// 使用<see cref="AttachServer"/>方法向网络服务器添加Socket服务，其中第一个将作为默认Socket服务<see cref="Server"/>。
/// 如果Socket服务集合<see cref="Servers"/>为空，将依据地址<see cref="Local"/>、端口<see cref="Port"/>、地址族<see cref="AddressFamily"/>、协议<see cref="ProtocolType"/>创建默认Socket服务。
/// 如果地址族<see cref="AddressFamily"/>指定为IPv4和IPv6以外的值，将同时创建IPv4和IPv6两个Socket服务；
/// 如果协议<see cref="ProtocolType"/>指定为Tcp和Udp以外的值，将同时创建Tcp和Udp两个Socket服务；
/// 默认情况下，地址族<see cref="AddressFamily"/>和协议<see cref="ProtocolType"/>都是其它值，所以一共将会创建四个Socket服务（Tcp、Tcpv6、Udp、Udpv6）。
/// </remarks>
public class NetServer : DisposeBase, IServer, IExtend, ILogFeature
{
    #region 属性
    /// <summary>服务名</summary>
    public String Name { get; set; }

    private NetUri _Local = new();
    /// <summary>本地结点</summary>
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

    /// <summary>端口</summary>
    public Int32 Port { get => _Local.Port; set => _Local.Port = value; }

    /// <summary>协议类型</summary>
    public NetType ProtocolType { get => _Local.Type; set => _Local.Type = value; }

    /// <summary>寻址方案</summary>
    public AddressFamily AddressFamily { get; set; }

    /// <summary>服务器集合</summary>
    public IList<ISocketServer> Servers { get; private set; } = [];

    /// <summary>服务器。返回服务器集合中的第一个服务器</summary>
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
    public Boolean Active => Servers.Count > 0 && Server != null && Server.Active;

    /// <summary>会话超时时间。默认0秒，使用SocketServer默认值</summary>
    /// <remarks>
    /// 对于每一个会话连接，如果超过该时间仍然没有收到任何数据，则断开会话连接。
    /// </remarks>
    public Int32 SessionTimeout { get; set; }

    /// <summary>消息管道。收发消息都经过管道处理器，进行协议编码解码</summary>
    /// <remarks>
    /// 1，接收数据解码时，从前向后通过管道处理器；
    /// 2，发送数据编码时，从后向前通过管道处理器；
    /// </remarks>
    public IPipeline? Pipeline { get; set; }

    /// <summary>使用会话集合，允许遍历会话。默认true</summary>
    public Boolean UseSession { get; set; } = true;

    /// <summary>地址重用，主要应用于网络服务器重启交替。默认false</summary>
    /// <remarks>
    /// 一个端口释放后会等待两分钟之后才能再被使用，SO_REUSEADDR是让端口释放后立即就可以被再次使用。
    /// SO_REUSEADDR用于对TCP套接字处于TIME_WAIT状态下的socket(TCP连接中, 先调用close() 的一方会进入TIME_WAIT状态)，才可以重复绑定使用。
    /// </remarks>
    public Boolean ReuseAddress { get; set; }

    /// <summary>SSL协议。默认None</summary>
    public SslProtocols SslProtocol { get; set; } = SslProtocols.None;

    /// <summary>X509证书。用于SSL连接时验证证书指纹，可以直接加载pem证书文件，未指定时不验证证书</summary>
    /// <remarks>
    /// 可以使用pfx证书文件，也可以使用pem证书文件。
    /// 服务端必须指定证书。
    /// </remarks>
    /// <example>
    /// var cert = new X509Certificate2("file", "pass");
    /// </example>
    public X509Certificate? Certificate { get; set; }

    /// <summary>APM性能追踪器</summary>
    public ITracer? Tracer { get; set; }

    /// <summary>用于内部Socket服务器的APM性能追踪器</summary>
    public ITracer? SocketTracer { get; set; }

    /// <summary>显示统计信息的周期。默认600秒，0表示不显示统计信息</summary>
    public Int32 StatPeriod { get; set; } = 600;

    /// <summary>是否输出发送日志。默认false</summary>
    public Boolean LogSend { get; set; }

    /// <summary>是否输出接收日志。默认false</summary>
    public Boolean LogReceive { get; set; }

    /// <summary>服务提供者</summary>
    /// <remarks>
    /// 用于网络服务器内部解析各种服务，可以直接赋值或者依赖注入。
    /// 网络会话默认使用该提供者，应用系统可以在网络会话中创建Scope版服务提供者。
    /// </remarks>
    public IServiceProvider? ServiceProvider { get; set; }

    private ConcurrentDictionary<String, Object?>? _items;
    /// <summary>数据项</summary>
    public IDictionary<String, Object?> Items => _items ??= new();

    /// <summary>获取/设置 用户会话数据</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Object? this[String key] { get => _items != null && _items.TryGetValue(key, out var obj) ? obj : null; set => Items[key] = value; }
    #endregion

    #region 构造
    /// <summary>实例化一个网络服务器</summary>
    public NetServer()
    {
        Name = GetType().Name.TrimEnd("Server");

        //Servers = new List<ISocketServer>();

        if (SocketSetting.Current.Debug) Log = XTrace.Log;
    }

    /// <summary>通过指定监听地址和端口实例化一个网络服务器</summary>
    /// <param name="port"></param>
    public NetServer(Int32 port) : this(IPAddress.Any, port) { }

    /// <summary>通过指定监听地址和端口实例化一个网络服务器</summary>
    /// <param name="address"></param>
    /// <param name="port"></param>
    public NetServer(IPAddress address, Int32 port) : this(address, port, NetType.Unknown) { }

    /// <summary>通过指定监听地址和端口，还有协议，实例化一个网络服务器，默认支持Tcp协议和Udp协议</summary>
    /// <param name="address"></param>
    /// <param name="port"></param>
    /// <param name="protocolType"></param>
    public NetServer(IPAddress address, Int32 port, NetType protocolType) : this()
    {
        Local.Address = address;
        Port = port;
        Local.Type = protocolType;
    }

    /// <summary>已重载。释放会话集合等资源</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        if (Active) Stop(GetType().Name + (disposing ? "Dispose" : "GC"));
    }
    #endregion

    #region 创建
    /// <summary>添加Socket服务器</summary>
    /// <param name="server"></param>
    /// <returns>添加是否成功</returns>
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
        //server.Tracer = Tracer;

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

    /// <summary>同时添加指定端口的IPv4和IPv6服务器，如果协议不是指定的Tcp或Udp，则同时添加Tcp和Udp服务器</summary>
    /// <param name="address"></param>
    /// <param name="port"></param>
    /// <param name="protocol"></param>
    /// <param name="family"></param>
    /// <returns></returns>
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

    /// <summary>确保建立服务器</summary>
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

    /// <summary>添加处理器</summary>
    /// <typeparam name="THandler"></typeparam>
    public void Add<THandler>() where THandler : IHandler, new() => GetPipe().Add(new THandler());

    /// <summary>添加处理器</summary>
    /// <param name="handler">处理器</param>
    public void Add(IHandler handler) => GetPipe().Add(handler);

    private IPipeline GetPipe() => Pipeline ??= new Pipeline();
    #endregion

    #region 方法
    /// <summary>开始服务</summary>
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
    protected virtual void OnStart()
    {
        EnsureCreateServer();

        if (Servers.Count == 0) throw new Exception($"Failed to listen to all ports! Port=[{Port}]");

        WriteLog("准备开始监听{0}个服务器", Servers.Count);

        foreach (var item in Servers)
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
    /// <param name="reason">关闭原因。便于日志分析</param>
    public void Stop(String? reason)
    {
        _Timer.TryDispose();

        var ss = Servers.Where(e => e.Active).ToArray();
        if (ss == null || ss.Length == 0) return;

        OnStop(reason);

        WriteLog("已停止！");
    }

    /// <summary>停止时调用的方法</summary>
    /// <param name="reason">关闭原因。便于日志分析</param>
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
        if (sessions != null)
        {
            WriteLog("准备释放网络会话{0}个！", sessions.Count);
            foreach (var item in sessions.Values.ToArray())
            {
                item.TryDispose();
            }
            sessions.Clear();
        }

        var severs = Servers;
        if (severs != null)
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
    /// <summary>新会话，对于TCP是新连接，对于UDP是新客户端</summary>
    public event EventHandler<NetSessionEventArgs>? NewSession;

    /// <summary>某个会话的数据到达。sender是INetSession</summary>
    public event EventHandler<ReceivedEventArgs>? Received;

    /// <summary>接受连接时，对于Udp是收到数据时（同时触发OnReceived）。</summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void Server_NewSession(Object? sender, SessionEventArgs e)
    {
        var session = e.Session;

        var ns = OnNewSession(session);

        NewSession?.Invoke(sender, new NetSessionEventArgs { Session = ns });
    }

    private Int32 _sessionID = 0;
    /// <summary>收到连接时，建立会话，并挂接数据接收和错误处理事件</summary>
    /// <param name="session"></param>
    protected virtual INetSession OnNewSession(ISocketSession session)
    {
        Interlocked.Increment(ref _SessionCount);
        session.OnDisposed += (s, e2) => Interlocked.Decrement(ref _SessionCount);
        if (_SessionCount > MaxSessionCount) MaxSessionCount = _SessionCount;

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

    /// <summary>收到数据时</summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void OnReceived(Object? sender, ReceivedEventArgs e)
    {
        if (sender is INetSession session)
        {
            if (e.Packet != null) OnReceive(session, e.Packet);
            OnReceive(session, e);
        }

        Received?.Invoke(sender, e);
    }

    /// <summary>收到数据时，最原始的数据处理，但不影响会话内部的数据处理</summary>
    /// <param name="session"></param>
    /// <param name="pk"></param>
    protected virtual void OnReceive(INetSession session, IPacket pk) { }

    /// <summary>收到数据时，最原始的数据处理，但不影响会话内部的数据处理</summary>
    /// <param name="session"></param>
    /// <param name="e"></param>
    protected virtual void OnReceive(INetSession session, ReceivedEventArgs e) { }

    /// <summary>错误发生/断开连接时。sender是ISocketSession</summary>
    public event EventHandler<ExceptionEventArgs>? Error;

    /// <summary>触发异常</summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected virtual void OnError(Object? sender, ExceptionEventArgs e)
    {
        if (Log.Enable) Log.Error("{0} Error {1}", sender, e.Exception);

        Error?.Invoke(sender, e);
    }
    #endregion

    #region 会话
    private readonly ConcurrentDictionary<Int32, INetSession> _Sessions = new();
    /// <summary>会话集合。用自增的数字ID作为标识，业务应用自己维持ID与业务主键的对应关系。</summary>
    public IDictionary<Int32, INetSession> Sessions => _Sessions;

    private Int32 _SessionCount;
    /// <summary>会话数</summary>
    public Int32 SessionCount { get => _SessionCount; set => _SessionCount = value; }

    /// <summary>最高会话数</summary>
    public Int32 MaxSessionCount { get; private set; }

    /// <summary>添加会话。子类可以在添加会话前对会话进行一些处理</summary>
    /// <param name="session"></param>
    protected virtual void AddSession(INetSession session)
    {
        session.Host ??= this;

        if (_Sessions.TryAdd(session.ID, session))
        {
            session.OnDisposed += (s, e) =>
            {
                if (s is INetSession ns) _Sessions.Remove(ns.ID);
            };
        }
    }

    /// <summary>创建会话</summary>
    /// <param name="session"></param>
    /// <returns></returns>
    protected virtual INetSession CreateSession(ISocketSession session)
    {
        var ns = new NetSession();
        (ns as INetSession).Host = this;
        ns.Server = session.Server;
        ns.Session = session;

        return ns;
    }

    /// <summary>根据会话ID查找会话</summary>
    /// <param name="sessionid"></param>
    /// <returns></returns>
    public INetSession? GetSession(Int32 sessionid)
    {
        if (sessionid == 0) return null;

        return Sessions.TryGetValue(sessionid, out var ns) ? ns : null;
    }

    /// <summary>为会话创建网络数据处理器。可作为业务处理实现，也可以作为前置协议解析</summary>
    /// <param name="session"></param>
    /// <returns></returns>
    public virtual INetHandler? CreateHandler(INetSession session) => null;
    #endregion

    #region 群发
    /// <summary>异步群发数据给所有客户端</summary>
    /// <param name="data"></param>
    /// <returns>已群发客户端总数</returns>
    public virtual async Task<Int32> SendAllAsync(IPacket data)
    {
        if (!UseSession) throw new ArgumentOutOfRangeException(nameof(UseSession), true, "Mass posting requires the use of session collections");

        var ts = new List<Task>();
        foreach (var item in Sessions)
        {
            ts.Add(Task.Run(() => item.Value.Send(data)));
        }

        await Task.WhenAll(ts).ConfigureAwait(false);

        return Sessions.Count;
    }

    /// <summary>异步群发数据给所有客户端</summary>
    /// <param name="data"></param>
    /// <param name="predicate">过滤器，判断指定会话是否需要发送</param>
    /// <returns>已群发客户端总数</returns>
    public virtual async Task<Int32> SendAllAsync(IPacket data, Func<INetSession, Boolean>? predicate = null)
    {
        if (!UseSession) throw new ArgumentOutOfRangeException(nameof(UseSession), true, "Mass posting requires the use of session collections");

        var ts = new List<Task>();
        foreach (var item in Sessions)
        {
            if (predicate == null || predicate(item.Value))
                ts.Add(Task.Run(() => item.Value.Send(data)));
        }

        await Task.WhenAll(ts).ConfigureAwait(false);

        return Sessions.Count;
    }

    /// <summary>群发管道消息给所有客户端。不等待，支持协议编码</summary>
    /// <param name="message">应用消息，底层对其进行协议编码</param>
    /// <param name="predicate">过滤器，判断指定会话是否需要发送</param>
    /// <returns>已群发客户端总数</returns>
    public virtual Int32 SendAllMessage(Object message, Func<INetSession, Boolean>? predicate = null)
    {
        if (!UseSession) throw new ArgumentOutOfRangeException(nameof(UseSession), true, "Mass posting requires the use of session collections");

        var count = 0;
        foreach (var item in Sessions)
        {
            if (predicate == null || predicate(item.Value))
            {
                try
                {
                    item.Value.SendMessage(message);
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
    /// <param name="address"></param>
    /// <param name="port"></param>
    /// <param name="protocol"></param>
    /// <param name="family"></param>
    /// <returns></returns>
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

    private void ShowStat(Object? state)
    {
        var msg = GetStat();
        if (msg.IsNullOrEmpty() || msg == _Last) return;
        _Last = msg;

        WriteLog(msg);
    }

    /// <summary>获取统计信息</summary>
    /// <returns></returns>
    public String GetStat()
    {
        var sb = Pool.StringBuilder.Get();
        if (MaxSessionCount > 0)
        {
            SessionCount = _Sessions.Count;
            sb.AppendFormat("在线：{0:n0}/{1:n0} ", SessionCount, MaxSessionCount);
        }

        return sb.Return(true);
    }
    #endregion

    #region 日志
    /// <summary>日志提供者</summary>
    public ILog Log { get; set; } = Logger.Null;

    /// <summary>用于内部Socket服务器的日志提供者</summary>
    public ILog? SocketLog { get; set; }

    /// <summary>用于网络会话的日志提供者</summary>
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
    /// <param name="format"></param>
    /// <param name="args"></param>
    public virtual void WriteLog(String format, params Object?[] args)
    {
        if (!LogPrefix.EndsWith(" ") && !format.StartsWith(" ")) format = " " + format;
        Log.Info(LogPrefix + format, args);
    }

    /// <summary>输出错误日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public virtual void WriteError(String format, params Object?[] args) => Log.Error(LogPrefix + format, args);
    #endregion

    #region 辅助
    /// <summary>已重载。</summary>
    /// <returns></returns>
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

/// <summary>网络服务器</summary>
/// <typeparam name="TSession"></typeparam>
public class NetServer<TSession> : NetServer where TSession : class, INetSession, new()
{
    /// <summary>创建会话</summary>
    /// <param name="session"></param>
    /// <returns></returns>
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
    /// <param name="id"></param>
    /// <returns></returns>
    public new TSession? GetSession(Int32 id)
    {
        if (id <= 0) return null;

        return base.GetSession(id) as TSession;
    }
}