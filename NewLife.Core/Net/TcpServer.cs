using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Model;

namespace NewLife.Net;

/// <summary>TCP服务器</summary>
/// <remarks>
/// <para>核心工作：启动服务 <see cref="Start"/> 时，监听端口，并启用多个（逻辑处理器数的10倍）异步接受操作 <see cref="StartAccept"/>。</para>
/// <para>功能特性：</para>
/// <list type="bullet">
/// <item>完全异步工作模式，任何操作都不会被阻塞</item>
/// <item>支持SSL/TLS加密传输</item>
/// <item>支持TCP KeepAlive</item>
/// <item>支持地址重用（快速重启）</item>
/// <item>自动管理客户端会话</item>
/// </list>
/// <para>注意：服务器接受连接请求后，不会开始处理数据，而是由 <see cref="NewSession"/> 事件订阅者决定何时开始处理数据。</para>
/// </remarks>
public class TcpServer : DisposeBase, ISocketServer, ILogFeature
{
    #region 属性
    /// <summary>名称</summary>
    /// <remarks>主要用于日志输出，默认为类名</remarks>
    public String Name { get; set; }

    /// <summary>本地绑定信息</summary>
    /// <remarks>指定服务器监听的网络地址</remarks>
    public NetUri Local { get; set; }

    /// <summary>端口</summary>
    /// <remarks>本地监听端口，0表示由系统自动分配</remarks>
    public Int32 Port { get => Local.Port; set => Local.Port = value; }

    /// <summary>会话超时时间（秒）</summary>
    /// <remarks>
    /// 对于每一个会话连接，如果超过该时间仍然没有收到任何数据，则断开会话连接。
    /// </remarks>
    public Int32 SessionTimeout { get; set; }

    /// <summary>底层Socket</summary>
    /// <remarks>监听Socket实例</remarks>
    public Socket? Client { get; private set; }

    /// <summary>是否活动</summary>
    /// <remarks>表示服务器是否正在运行</remarks>
    public Boolean Active { get; set; }

    /// <summary>最大并行接收连接数</summary>
    /// <remarks>默认CPU*1.6，控制同时等待Accept的数量</remarks>
    public Int32 MaxAsync { get; set; }

    /// <summary>不延迟直接发送</summary>
    /// <remarks>Tcp为了合并小包而设计，客户端默认false，服务端默认true</remarks>
    public Boolean NoDelay { get; set; } = true;

    /// <summary>地址重用</summary>
    /// <remarks>
    /// <para>主要应用于网络服务器重启交替。默认false。</para>
    /// <para>一个端口释放后会等待两分钟之后才能再被使用，SO_REUSEADDR是让端口释放后立即就可以被再次使用。</para>
    /// <para>SO_REUSEADDR用于对TCP套接字处于TIME_WAIT状态下的socket，才可以重复绑定使用。</para>
    /// </remarks>
    public Boolean ReuseAddress { get; set; }

    /// <summary>KeepAlive间隔（秒）</summary>
    /// <remarks>默认0秒不启用。启用后可及时检测连接断开</remarks>
    public Int32 KeepAliveInterval { get; set; }

    /// <summary>启用Http</summary>
    /// <remarks>数据处理时截去请求响应头，默认false</remarks>
    public Boolean EnableHttp { get; set; }

    /// <summary>协议编解码器。非空时启用协议模式（创建会话时下发），会话数据经数据管道定界，头部到齐即交付消息帧</summary>
    public IMessageCodec? Protocol { get; set; }

    /// <summary>消息泵最大缓存字节数（协议模式无法定界的残余上限），创建会话时下发，默认 1M。0 表示不限制</summary>
    public Int32 MaxCache { get; set; } = 1024 * 1024;

    /// <summary>最大并发处理数。协议模式下会话消息处理并发度，创建会话时下发：1=串行（默认），大于1=并行派发（兼作并发上限）</summary>
    public Int32 MaxConcurrency { get; set; } = 1;

    /// <summary>SSL协议版本</summary>
    /// <remarks>默认None不启用SSL</remarks>
    public SslProtocols SslProtocol { get; set; } = SslProtocols.None;

    /// <summary>X509证书</summary>
    /// <remarks>
    /// <para>用于SSL连接时验证证书指纹，可以直接加载pem证书文件，未指定时不验证证书。</para>
    /// <para>可以使用pfx证书文件，也可以使用pem证书文件。</para>
    /// <para>服务端必须指定证书。</para>
    /// </remarks>
    /// <example>
    /// var cert = new X509Certificate2("file", "pass");
    /// </example>
    public X509Certificate? Certificate { get; set; }

    /// <summary>APM性能追踪器</summary>
    /// <remarks>用于记录关键操作的性能追踪</remarks>
    public ITracer? Tracer { get; set; }
    #endregion

    #region 构造
    /// <summary>实例化TCP服务器</summary>
    public TcpServer()
    {
        Name = GetType().Name;

        Local = new NetUri(NetType.Tcp, IPAddress.Any, 0);
        SessionTimeout = SocketSetting.Current.SessionTimeout;
        MaxAsync = Environment.ProcessorCount * 16 / 10;
        _Sessions = new SessionCollection(this);

        if (SocketSetting.Current.Debug) Log = XTrace.Log;
    }

    /// <summary>使用端口实例化TCP服务器</summary>
    /// <param name="port">监听端口</param>
    public TcpServer(Int32 port) : this() => Port = port;

    /// <summary>释放会话集合等资源</summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        if (Active) Stop(GetType().Name + (disposing ? "Dispose" : "GC"));

        _Sessions?.Dispose();
    }
    #endregion

    #region 开始停止
    /// <summary>开始</summary>
    public virtual void Start()
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);

        if (Active || Disposed) return;

        using var span = Tracer?.NewSpan($"net:{Name}:Start");
        try
        {
            var sock = Client;
            var isUnix = Local.IsUnix;

            // 开始监听
            //if (Server == null) Server = new TcpListener(Local.EndPoint);
            if (sock == null) Client = sock = isUnix ? NetHelper.CreateUnix() : NetHelper.CreateTcp(Local.Address.IsIPv4());

            try
            {
                // 地址重用，主要应用于网络服务器重启交替。前一个进程关闭时，端口在短时间内处于TIME_WAIT，导致新进程无法监听。
                // 启用地址重用后，即使旧进程未退出，新进程也可以监听，但只有旧进程退出后，新进程才能接受对该端口的连接请求
                // Unix域套接字以文件路径为地址，没有端口重用的概念
                if (ReuseAddress && !isUnix) sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            }
            catch (Exception ex)
            {
                // 有些平台不支持地址重用，比如旧版A2上的Ubuntu16，但是云服务器的Linux都支持
                XTrace.WriteLine(ex.Message);
            }

            WriteLog("Start {0}", this);

            // 三次握手之后，Accept之前的总连接个数，队列满之后，新连接将得到主动拒绝ConnectionRefused错误
            // 在我（大石头）的开发机器上，实际上这里的最大值只能是200，大于200跟200一个样
            //Server.Start();
            if (isUnix)
                BindUnix(sock, Local.Path);
            else
                sock.Bind(Local.EndPoint);
            //sock.Listen(Int32.MaxValue);
            sock.Listen(65535);

            if (!isUnix && Local.Port == 0 && sock.LocalEndPoint is IPEndPoint ep)
                Local.Port = ep.Port;

            // Unix域套接字不支持TCP选项
            if (Runtime.Windows && !isUnix)
            {
                sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            }

            Active = true;

            for (var i = 0; i < MaxAsync; i++)
            {
                var se = new SocketAsyncEventArgs();
                se.Completed += (s, e) => ProcessAccept(e);

                StartAccept(se, false);
            }
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    /// <summary>停止</summary>
    /// <param name="reason">关闭原因。便于日志分析</param>
    public virtual void Stop(String? reason)
    {
        if (!Active) return;

        WriteLog("Stop {0} {1}", reason, this);

        using var span = Tracer?.NewSpan($"net:{Name}:Stop");
        try
        {
            // 关闭的时候会除非一系列异步回调，提前清空Client
            Active = false;

            CloseAllSession();

            Client?.Shutdown();
            Client = null;

            // Unix域套接字需要删除文件，否则残留文件会导致下次启动拒绝服务
            if (Local.IsUnix && !Local.Path.IsNullOrEmpty())
            {
                try
                {
                    File.Delete(Local.Path!);
                }
                catch (Exception ex)
                {
                    XTrace.WriteLine(ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }
    #endregion

    #region Unix域套接字
    /// <summary>绑定Unix域套接字，自动清理残留文件</summary>
    /// <param name="sock">监听套接字</param>
    /// <param name="path">套接字文件路径</param>
    private static void BindUnix(Socket sock, String? path)
    {
#if NETFRAMEWORK || NETSTANDARD2_0
        throw new PlatformNotSupportedException("Unix Domain Socket 需要 .NET Standard 2.1 或更高版本的目标框架");
#else
        if (path.IsNullOrEmpty()) throw new ArgumentNullException(nameof(path), "Unix域套接字路径不能为空");

        // 已有服务在监听时拒绝启动，避免两套服务各自服务一部分连接
        if (File.Exists(path) && UnixSocketAlive(path!))
            throw new InvalidOperationException($"Unix域套接字[{path}]已被其它服务监听");

        // 清理进程异常退出后残留的套接字文件
        if (File.Exists(path))
        {
            XTrace.WriteLine("清理残留的Unix域套接字文件 {0}", path);
            File.Delete(path!);
        }

        sock.Bind(new UnixDomainSocketEndPoint(path!));
#endif
    }

#if !NETFRAMEWORK && !NETSTANDARD2_0
    /// <summary>探测Unix域套接字上是否存在活动服务</summary>
    /// <param name="path">套接字文件路径</param>
    /// <returns></returns>
    private static Boolean UnixSocketAlive(String path)
    {
        using var probe = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try
        {
            probe.Connect(new UnixDomainSocketEndPoint(path));
            return true;
        }
        catch (SocketException) { return false; }
    }
#endif
    #endregion

    #region 连接处理
    /// <summary>新会话时触发</summary>
    public event EventHandler<SessionEventArgs>? NewSession;

    /// <summary>开启异步接受新连接</summary>
    /// <param name="se"></param>
    /// <param name="io">是否IO线程</param>
    /// <returns>开启异步是否成功</returns>
    Boolean StartAccept(SocketAsyncEventArgs se, Boolean io)
    {
        if (!Active || Client is not { } sock)
        {
            se?.Dispose();
            return false;
        }

        using var span = Tracer?.NewSpan($"net:{Name}:StartAccept");
        var rs = false;
        try
        {
            se.AcceptSocket = null;
            rs = sock.AcceptAsync(se);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);

            if (!ex.IsDisposed()) OnError("AcceptAsync", ex);

            if (!io) throw;

            return false;
        }

        if (!rs)
        {
            if (io)
                ProcessAccept(se);
            else
                Task.Factory.StartNew(() => ProcessAccept(se), TaskCreationOptions.LongRunning);
        }

        return true;
    }

    void ProcessAccept(SocketAsyncEventArgs se)
    {
        if (!Active || Client == null)
        {
            se?.Dispose();
            return;
        }

        using var span = Tracer?.NewSpan($"net:{Name}:ProcessAccept");

        // 判断成功失败
        if (se.SocketError != SocketError.Success)
        {
            var ex = se.GetException();
            if (ex != null) OnError("AcceptAsync", ex);

            // 未被关闭Socket时，可以继续使用
            //if (!se.IsNotClosed())
            // 历史：该判断曾被整体注释掉，导致任何接受错误都会释放事件参数并直接返回。
            // 接受槽位只在启动时按 MaxAsync 创建一次，逐个消耗后服务器将不再接受新连接。
            // 现按“监听套接字是否已中止”区分：已中止（服务正在关闭）才释放返回，其余错误继续投递。
            if (se.IsAborted())
            {
                se?.Dispose();
                return;
            }
        }
        else if (se.AcceptSocket != null)
        {
            // 直接在IO线程调用业务逻辑
            try
            {
                OnAccept(se.AcceptSocket);
            }
            catch (Exception ex)
            {
                span?.SetError(ex, null);

                if (!ex.IsDisposed()) OnError("EndAccept", ex);
            }
        }

        // 开始新的征程
        StartAccept(se, true);
    }

    Int32 g_ID = 0;
    /// <summary>收到新连接时处理</summary>
    /// <param name="client"></param>
    protected virtual void OnAccept(Socket client)
    {
        var session = CreateSession(client);

        // 设置心跳时间。Unix域套接字不支持TCP选项
        if (KeepAliveInterval > 0 && !Local.IsUnix) client.SetTcpKeepAlive(true, KeepAliveInterval, KeepAliveInterval);

        if (_Sessions.Add(session))
        {
            // 会话改为原子操作，避免多线程冲突
            session.ID = Interlocked.Increment(ref g_ID);
            session.WriteLog("New {0}", session.Remote.EndPoint);

            NewSession?.Invoke(this, new SessionEventArgs(session));

            // 自动开始异步接收处理
            session.SslProtocol = SslProtocol;
            session.Certificate = Certificate;
            session.Tracer = Tracer;
            session.Start();
        }
        else
        {
            // 会话集合拒绝（端点重复或并发竞态）：会话已建立但无人接管，立即释放，避免 socket 与接收缓冲泄漏
            session.TryDispose();
        }
    }
    #endregion

    #region 会话
    private readonly SessionCollection _Sessions;
    /// <summary>会话集合。用地址端口作为标识，业务应用自己维持地址端口与业务主键的对应关系。</summary>
    public IDictionary<String, ISocketSession> Sessions => _Sessions;

    /// <summary>创建会话</summary>
    /// <param name="client"></param>
    /// <returns></returns>
    protected virtual TcpSession CreateSession(Socket client)
    {
        //var session = EnableHttp ? new HttpSession(this, client) : new TcpSession(this, client);
        var session = new TcpSession(this, client)
        {
            //// 服务端不支持掉线重连
            //AutoReconnect = 0,
            NoDelay = NoDelay,
            KeepAliveInterval = KeepAliveInterval,
            Protocol = Protocol,
            MaxConcurrency = MaxConcurrency,
            MaxCache = MaxCache,
            //DisconnectWhenEmptyData = false,

            Log = Log,
            LogSend = LogSend,
            LogReceive = LogReceive,
            Tracer = Tracer,
        };

        // 为了降低延迟，服务端不要合并小包。Unix域套接字不支持TCP选项
        if (!Local.IsUnix) client.NoDelay = NoDelay;

        return session;
    }

    private void CloseAllSession()
    {
        var sessions = _Sessions;
        if (sessions != null)
        {
            if (sessions.Count > 0)
            {
                WriteLog("准备释放会话{0}个！", sessions.Count);
                sessions.CloseAll(nameof(CloseAllSession));
                sessions.Dispose();
                sessions.Clear();
            }
        }
    }
    #endregion

    #region 异常处理
    /// <summary>错误发生/断开连接时</summary>
    public event EventHandler<ExceptionEventArgs>? Error;

    /// <summary>触发异常</summary>
    /// <param name="action">动作</param>
    /// <param name="ex">异常</param>
    protected virtual void OnError(String action, Exception ex)
    {
        Log?.Error("{0}{1}Error {2} {3}", LogPrefix, action, this, ex.Message);
        Error?.Invoke(this, new ExceptionEventArgs(action, ex));
    }
    #endregion

    #region 日志
    private String? _LogPrefix;
    /// <summary>日志前缀</summary>
    public virtual String LogPrefix
    {
        get
        {
            if (_LogPrefix == null)
            {
                var name = Name == null ? "" : Name.TrimSuffix("Server", "Session", "Client");
                _LogPrefix = $"{name}.";
            }
            return _LogPrefix;
        }
        set { _LogPrefix = value; }
    }

    /// <summary>日志对象</summary>
    public ILog Log { get; set; } = Logger.Null;

    /// <summary>是否输出发送日志。默认false</summary>
    public Boolean LogSend { get; set; }

    /// <summary>是否输出接收日志。默认false</summary>
    public Boolean LogReceive { get; set; }

    /// <summary>输出日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object?[] args)
    {
        if (Log != null && Log.Enable) Log.Info(LogPrefix + format, args);
    }
    #endregion

    #region 辅助
    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString()
    {
        var ss = Sessions;
        var count = ss != null ? ss.Count : 0;
        if (count > 0)
            return $"{Local} [{count}]";
        else
            return Local.ToString();
    }
    #endregion
}