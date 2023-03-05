using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using NewLife.Log;
using NewLife.Model;

namespace NewLife.Net;

/// <summary>TCP服务器</summary>
/// <remarks>
/// 核心工作：启动服务<see cref="Start"/>时，监听端口，并启用多个（逻辑处理器数的10倍）异步接受操作<see cref="StartAccept"/>。
/// 
/// 服务器完全处于异步工作状态，任何操作都不可能被阻塞。
/// 
/// 注意：服务器接受连接请求后，不会开始处理数据，而是由<see cref="NewSession"/>事件订阅者决定何时开始处理数据。
/// </remarks>
public class TcpServer : DisposeBase, ISocketServer, ILogFeature
{
    #region 属性
    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>本地绑定信息</summary>
    public NetUri Local { get; set; }

    /// <summary>端口</summary>
    public Int32 Port { get { return Local.Port; } set { Local.Port = value; } }

    /// <summary>会话超时时间</summary>
    /// <remarks>
    /// 对于每一个会话连接，如果超过该时间仍然没有收到任何数据，则断开会话连接。
    /// </remarks>
    public Int32 SessionTimeout { get; set; }

    /// <summary>底层Socket</summary>
    public Socket Client { get; private set; }

    /// <summary>是否活动</summary>
    public Boolean Active { get; set; }

    /// <summary>最大并行接收连接数。默认CPU*1.6</summary>
    public Int32 MaxAsync { get; set; }

    /// <summary>不延迟直接发送。Tcp为了合并小包而设计，客户端默认false，服务端默认true</summary>
    public Boolean NoDelay { get; set; } = true;

    /// <summary>地址重用，主要应用于网络服务器重启交替。默认false</summary>
    /// <remarks>
    /// 一个端口释放后会等待两分钟之后才能再被使用，SO_REUSEADDR是让端口释放后立即就可以被再次使用。
    /// SO_REUSEADDR用于对TCP套接字处于TIME_WAIT状态下的socket(TCP连接中, 先调用close() 的一方会进入TIME_WAIT状态)，才可以重复绑定使用。
    /// </remarks>
    public Boolean ReuseAddress { get; set; }

    /// <summary>启用Http，数据处理时截去请求响应头，默认false</summary>
    public Boolean EnableHttp { get; set; }

    /// <summary>消息管道。收发消息都经过管道处理器，进行协议编码解码</summary>
    /// <remarks>
    /// 1，接收数据解码时，从前向后通过管道处理器；
    /// 2，发送数据编码时，从后向前通过管道处理器；
    /// </remarks>
    public IPipeline Pipeline { get; set; }

    /// <summary>SSL协议。默认None，服务端Default，客户端不启用</summary>
    public SslProtocols SslProtocol { get; set; } = SslProtocols.None;

    /// <summary>SSL证书。服务端使用</summary>
    /// <remarks>var cert = new X509Certificate2("file", "pass");</remarks>
    public X509Certificate Certificate { get; set; }

    /// <summary>APM性能追踪器</summary>
    public ITracer Tracer { get; set; }
    #endregion

    #region 构造
    /// <summary>构造TCP服务器对象</summary>
    public TcpServer()
    {
        Name = GetType().Name;

        Local = new NetUri(NetType.Tcp, IPAddress.Any, 0);
        SessionTimeout = Setting.Current.SessionTimeout;
        MaxAsync = Environment.ProcessorCount * 16 / 10;
        _Sessions = new SessionCollection(this);

        if (Setting.Current.Debug) Log = XTrace.Log;
    }

    /// <summary>构造TCP服务器对象</summary>
    /// <param name="port"></param>
    public TcpServer(Int32 port) : this() => Port = port;

    /// <summary>已重载。释放会话集合等资源</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        if (Active) Stop(GetType().Name + (disposing ? "Dispose" : "GC"));
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

            // 开始监听
            //if (Server == null) Server = new TcpListener(Local.EndPoint);
            if (sock == null) Client = sock = NetHelper.CreateTcp(Local.EndPoint.Address.IsIPv4());

            // 地址重用，主要应用于网络服务器重启交替。前一个进程关闭时，端口在短时间内处于TIME_WAIT，导致新进程无法监听。
            // 启用地址重用后，即使旧进程未退出，新进程也可以监听，但只有旧进程退出后，新进程才能接受对该端口的连接请求
            if (ReuseAddress) sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            WriteLog("Start {0}", this);

            // 三次握手之后，Accept之前的总连接个数，队列满之后，新连接将得到主动拒绝ConnectionRefused错误
            // 在我（大石头）的开发机器上，实际上这里的最大值只能是200，大于200跟200一个样
            //Server.Start();
            sock.Bind(Local.EndPoint);
            //sock.Listen(Int32.MaxValue);
            sock.Listen(65535);

            if (Runtime.Windows)
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
    public virtual void Stop(String reason)
    {
        if (!Active) return;

        WriteLog("Stop {0} {1}", reason, this);

        using var span = Tracer?.NewSpan($"net:{Name}:Stop");
        try
        {
            // 关闭的时候会除非一系列异步回调，提前清空Client
            Active = false;

            CloseAllSession();

            Client.Shutdown();
            Client = null;
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }
    #endregion

    #region 连接处理
    /// <summary>新会话时触发</summary>
    public event EventHandler<SessionEventArgs> NewSession;

    /// <summary>开启异步接受新连接</summary>
    /// <param name="se"></param>
    /// <param name="io">是否IO线程</param>
    /// <returns>开启异步是否成功</returns>
    Boolean StartAccept(SocketAsyncEventArgs se, Boolean io)
    {
        if (!Active || Client == null)
        {
            se.TryDispose();
            return false;
        }

        using var span = Tracer?.NewSpan($"net:{Name}:StartAccept");
        var rs = false;
        try
        {
            se.AcceptSocket = null;
            rs = Client.AcceptAsync(se);
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
                Task.Factory.StartNew(() => ProcessAccept(se));
        }

        return true;
    }

    void ProcessAccept(SocketAsyncEventArgs se)
    {
        if (!Active || Client == null)
        {
            se.TryDispose();
            return;
        }

        using var span = Tracer?.NewSpan($"net:{Name}:ProcessAccept");

        // 判断成功失败
        if (se.SocketError != SocketError.Success)
        {
            // 未被关闭Socket时，可以继续使用
            //if (!se.IsNotClosed())
            {
                var ex = se.GetException();
                if (ex != null) OnError("AcceptAsync", ex);

                se.TryDispose();
                return;
            }
        }
        else
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

        //// 设置心跳时间，默认10秒
        //client.SetTcpKeepAlive(true);

        if (_Sessions.Add(session))
        {
            // 会话改为原子操作，避免多线程冲突
            session.ID = Interlocked.Increment(ref g_ID);
            session.WriteLog("New {0}", session.Remote.EndPoint);

            NewSession?.Invoke(this, new SessionEventArgs { Session = session });

            // 自动开始异步接收处理
            session.SslProtocol = SslProtocol;
            session.Certificate = Certificate;
            session.Tracer = Tracer;
            session.Start();
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
            Log = Log,
            LogSend = LogSend,
            LogReceive = LogReceive,
            Pipeline = Pipeline,
            Tracer = Tracer,
        };

        // 为了降低延迟，服务端不要合并小包
        client.NoDelay = NoDelay;

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
                sessions.TryDispose();
                sessions.Clear();
            }
        }
    }
    #endregion

    #region 异常处理
    /// <summary>错误发生/断开连接时</summary>
    public event EventHandler<ExceptionEventArgs> Error;

    /// <summary>触发异常</summary>
    /// <param name="action">动作</param>
    /// <param name="ex">异常</param>
    protected virtual void OnError(String action, Exception ex)
    {
        if (Log != null) Log.Error("{0}{1}Error {2} {3}", LogPrefix, action, this, ex?.Message);
        Error?.Invoke(this, new ExceptionEventArgs { Action = action, Exception = ex });
    }
    #endregion

    #region 日志
    private String _LogPrefix;
    /// <summary>日志前缀</summary>
    public virtual String LogPrefix
    {
        get
        {
            if (_LogPrefix == null)
            {
                var name = Name == null ? "" : Name.TrimEnd("Server", "Session", "Client");
                _LogPrefix = $"{name}.";
            }
            return _LogPrefix;
        }
        set { _LogPrefix = value; }
    }

    /// <summary>日志对象</summary>
    public ILog Log { get; set; }

    /// <summary>是否输出发送日志。默认false</summary>
    public Boolean LogSend { get; set; }

    /// <summary>是否输出接收日志。默认false</summary>
    public Boolean LogReceive { get; set; }

    /// <summary>输出日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object[] args)
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