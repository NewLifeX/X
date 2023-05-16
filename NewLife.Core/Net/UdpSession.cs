using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;

namespace NewLife.Net;

/// <summary>Udp会话。仅用于服务端与某一固定远程地址通信</summary>
internal class UdpSession : DisposeBase, ISocketSession, ITransport
{
    #region 属性
    /// <summary>会话编号</summary>
    public Int32 ID { get; set; }

    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>服务器</summary>
    public UdpServer Server { get; set; }

    /// <summary>底层Socket</summary>
    Socket ISocket.Client => Server?.Client;

    ///// <summary>数据流</summary>
    //public Stream Stream { get; set; }

    private NetUri _Local;
    /// <summary>本地地址</summary>
    public NetUri Local
    {
        get => _Local ??= Server?.Local;
        set => Server.Local = _Local = value;
    }

    /// <summary>端口</summary>
    public Int32 Port { get => Local.Port; set => Local.Port = value; }

    /// <summary>远程地址</summary>
    public NetUri Remote { get; set; }

    private Int32 _timeout;
    /// <summary>超时。默认3000ms</summary>
    public Int32 Timeout
    {
        get => _timeout;
        set
        {
            _timeout = value;
            if (Server != null)
                Server.Client.ReceiveTimeout = _timeout;
        }
    }

    /// <summary>消息管道。收发消息都经过管道处理器，进行协议编码解码</summary>
    /// <remarks>
    /// 1，接收数据解码时，从前向后通过管道处理器；
    /// 2，发送数据编码时，从后向前通过管道处理器；
    /// </remarks>
    public IPipeline Pipeline { get; set; }

    /// <summary>Socket服务器。当前通讯所在的Socket服务器，其实是TcpServer/UdpServer</summary>
    ISocketServer ISocketSession.Server => Server;

    /// <summary>最后一次通信时间，主要表示活跃时间，包括收发</summary>
    public DateTime LastTime { get; private set; } = DateTime.Now;

    /// <summary>APM性能追踪器</summary>
    public ITracer Tracer { get; set; }
    #endregion

    #region 构造
    public UdpSession(UdpServer server, IPEndPoint remote)
    {
        Name = server.Name;

        Server = server;
        Remote = new NetUri(NetType.Udp, remote);
        Tracer = server.Tracer;

        // 检查并开启广播
        server.Client.CheckBroadcast(remote.Address);
    }

    public void Start()
    {
        Pipeline = Server.Pipeline;

        //Server.ReceiveAsync();
        Server.Open();

        WriteLog("New {0}", Remote.EndPoint);

        // 管道
        Pipeline?.Open(Server.CreateContext(this));
    }

    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        WriteLog("Close {0}", Remote.EndPoint);

        // 管道
        var ctx = Server?.CreateContext(this);
        if (ctx != null)
            Pipeline?.Close(ctx, disposing ? "Dispose" : "GC");

        // 释放对服务对象的引用，如果没有其它引用，服务对象将会被回收
        Server = null;
    }
    #endregion

    #region 发送
    public Int32 Send(Packet data)
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);

        return Server.OnSend(data, Remote.EndPoint);
    }

    /// <summary>发送消息，不等待响应</summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public virtual Int32 SendMessage(Object message)
    {
        using var span = Tracer?.NewSpan($"net:{Name}:SendMessage", message);
        try
        {
            var ctx = Server.CreateContext(this);
            return (Int32)Pipeline.Write(ctx, message);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, message);
            throw;
        }
    }

    /// <summary>发送消息并等待响应</summary>
    /// <param name="message">消息</param>
    /// <returns></returns>
    public virtual Task<Object> SendMessageAsync(Object message)
    {
        using var span = Tracer?.NewSpan($"net:{Name}:SendMessageAsync", message);
        try
        {
            var ctx = Server.CreateContext(this);
            var source = new TaskCompletionSource<Object>();
            ctx["TaskSource"] = source;

            var rs = (Int32)Pipeline.Write(ctx, message);
            if (rs < 0) return Task.FromResult((Object)null);

            return source.Task;
        }
        catch (Exception ex)
        {
            span?.SetError(ex, message);
            throw;
        }
    }

    /// <summary>发送消息并等待响应</summary>
    /// <param name="message">消息</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public virtual Task<Object> SendMessageAsync(Object message, CancellationToken cancellationToken)
    {
        using var span = Tracer?.NewSpan($"net:{Name}:SendMessageAsync", message);
        try
        {
            var ctx = Server.CreateContext(this);
            var source = new TaskCompletionSource<Object>();
            ctx["TaskSource"] = source;

            var rs = (Int32)Pipeline.Write(ctx, message);
            if (rs < 0) return Task.FromResult((Object)null);

            // 注册取消时的处理，如果没有收到响应，取消发送等待
            cancellationToken.Register(() => { if (!source.Task.IsCompleted) source.TrySetCanceled(); });

            return source.Task;
        }
        catch (Exception ex)
        {
            span?.SetError(ex, message);
            throw;
        }
    }
    #endregion

    #region 接收
    /// <summary>接收数据</summary>
    /// <returns></returns>
    public Packet Receive()
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);

        using var span = Tracer?.NewSpan($"net:{Name}:Receive", Server.BufferSize + "");
        try
        {
            var ep = Remote.EndPoint as EndPoint;
            var buf = new Byte[Server.BufferSize];
            var size = Server.Client.ReceiveFrom(buf, ref ep);

            return new Packet(buf, 0, size);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    public event EventHandler<ReceivedEventArgs> Received;

    internal void OnReceive(ReceivedEventArgs e)
    {
        LastTime = DateTime.Now;

        if (e != null) Received?.Invoke(this, e);
    }

    /// <summary>处理数据帧</summary>
    /// <param name="data">数据帧</param>
    void ISocketRemote.Process(IData data) => (Server as ISocketRemote).Process(data);
    #endregion

    #region 异常处理
    /// <summary>错误发生/断开连接时</summary>
    public event EventHandler<ExceptionEventArgs> Error;

    /// <summary>触发异常</summary>
    /// <param name="action">动作</param>
    /// <param name="ex">异常</param>
    protected virtual void OnError(String action, Exception ex)
    {
        Log?.Error(LogPrefix + "{0}Error {1} {2}", action, this, ex?.Message);
        Error?.Invoke(this, new ExceptionEventArgs { Exception = ex });
    }
    #endregion

    #region 辅助
    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString()
    {
        if (Remote != null && !Remote.EndPoint.IsAny())
            return $"{Local}=>{Remote.EndPoint}";
        else
            return Local.ToString();
    }
    #endregion

    #region ITransport接口
    Boolean ITransport.Open() => true;

    Boolean ITransport.Close() => true;
    #endregion

    #region 扩展接口
    private readonly ConcurrentDictionary<String, Object> _Items = new();
    /// <summary>数据项</summary>
    public IDictionary<String, Object> Items => _Items;

    /// <summary>设置 或 获取 数据项</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Object this[String key] { get => _Items.TryGetValue(key, out var obj) ? obj : null; set => _Items[key] = value; }
    #endregion

    #region 日志
    /// <summary>日志提供者</summary>
    public ILog Log { get; set; }

    /// <summary>是否输出发送日志。默认false</summary>
    public Boolean LogSend { get; set; }

    /// <summary>是否输出接收日志。默认false</summary>
    public Boolean LogReceive { get; set; }

    private String _LogPrefix;
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
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object[] args)
    {
        Log?.Info(LogPrefix + format, args);
    }

    /// <summary>输出日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    [Conditional("DEBUG")]
    public void WriteDebugLog(String format, params Object[] args)
    {
        Log?.Debug(LogPrefix + format, args);
    }
    #endregion
}