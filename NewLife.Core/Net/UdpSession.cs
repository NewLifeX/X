using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;

namespace NewLife.Net;

/// <summary>Udp会话。仅用于服务端与某一固定远程地址通信</summary>
public class UdpSession : DisposeBase, ISocketSession, ITransport, ILogFeature
{
    #region 属性
    /// <summary>会话编号</summary>
    public Int32 ID { get; set; }

    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>服务器</summary>
    public UdpServer Server { get; set; }

    /// <summary>底层Socket</summary>
    Socket? ISocket.Client => Server?.Client;

    /// <summary>本地地址</summary>
    public NetUri Local { get; set; }

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
            if (Server?.Client != null)
                Server.Client.ReceiveTimeout = _timeout;
        }
    }

    /// <summary>消息管道。收发消息都经过管道处理器，进行协议编码解码</summary>
    /// <remarks>
    /// 1，接收数据解码时，从前向后通过管道处理器；
    /// 2，发送数据编码时，从后向前通过管道处理器；
    /// </remarks>
    public IPipeline? Pipeline { get; set; }

    /// <summary>Socket服务器。当前通讯所在的Socket服务器，其实是TcpServer/UdpServer</summary>
    ISocketServer ISocketSession.Server => Server;

    /// <summary>最后一次通信时间，主要表示活跃时间，包括收发</summary>
    public DateTime LastTime { get; private set; } = DateTime.Now;

    /// <summary>APM性能追踪器</summary>
    public ITracer? Tracer { get; set; }
    #endregion

    #region 构造
    /// <summary>实例化Udp会话</summary>
    /// <param name="server"></param>
    /// <param name="local">接收数据的本地地址</param>
    /// <param name="remote"></param>
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
        Pipeline = Server.Pipeline;

        //Server.ReceiveAsync();
        Server.Open();

        WriteLog("New {0}", Remote.EndPoint);

        // 管道
        Pipeline?.Open(Server.CreateContext(this));
    }

    private void Stop(String reason)
    {
        if (Server == null) return;

        WriteLog("Close {0} {1}", Remote.EndPoint, reason);

        // 管道
        var ctx = Server?.CreateContext(this);
        if (ctx != null)
            Pipeline?.Close(ctx, reason);

        Server = null!;
    }

    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        Stop(disposing ? "Dispose" : "GC");

        //// 释放对服务对象的引用，如果没有其它引用，服务对象将会被回收
        //Server = null;
    }
    #endregion

    #region 发送
    /// <summary>发送数据</summary>
    /// <param name="data"></param>
    /// <returns></returns>
    /// <exception cref="ObjectDisposedException"></exception>
    public Int32 Send(IPacket data)
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);

        return Server.OnSend(data, Remote.EndPoint);
    }

    /// <summary>发送消息，不等待响应</summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public virtual Int32 SendMessage(Object message)
    {
        if (Pipeline == null) throw new InvalidOperationException(nameof(Pipeline));

        using var span = Tracer?.NewSpan($"net:{Name}:SendMessage", message);
        try
        {
            var ctx = Server.CreateContext(this);
            return (Int32)(Pipeline.Write(ctx, message) ?? -1);
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
    public virtual async Task<Object> SendMessageAsync(Object message)
    {
        if (Server == null) throw new InvalidOperationException(nameof(Server));
        if (Pipeline == null) throw new InvalidOperationException(nameof(Pipeline));

        using var span = Tracer?.NewSpan($"net:{Name}:SendMessageAsync", message);
        try
        {
            var ctx = Server.CreateContext(this);
            var source = new TaskCompletionSource<Object>();
            ctx["TaskSource"] = source;
            ctx["Span"] = span;

            var rs = (Int32)(Pipeline.Write(ctx, message) ?? -1);
#if NET45
            if (rs < 0) return Task.FromResult(0);
#else
            if (rs < 0) return Task.CompletedTask;
#endif

            return await source.Task;
        }
        catch (Exception ex)
        {
            if (ex is TaskCanceledException)
                span?.AppendTag(ex.Message);
            else
                span?.SetError(ex, message);
            throw;
        }
    }

    /// <summary>发送消息并等待响应</summary>
    /// <param name="message">消息</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public virtual async Task<Object> SendMessageAsync(Object message, CancellationToken cancellationToken)
    {
        if (Server == null) throw new InvalidOperationException(nameof(Server));
        if (Pipeline == null) throw new InvalidOperationException(nameof(Pipeline));

        using var span = Tracer?.NewSpan($"net:{Name}:SendMessageAsync", message);
        try
        {
            var ctx = Server.CreateContext(this);
            var source = new TaskCompletionSource<Object>();
            ctx["TaskSource"] = source;
            ctx["Span"] = span;

            var rs = (Int32)(Pipeline.Write(ctx, message) ?? -1);
#if NET45
            if (rs < 0) return Task.FromResult(0);
#else
            if (rs < 0) return Task.CompletedTask;
#endif

            // 注册取消时的处理，如果没有收到响应，取消发送等待
            using (cancellationToken.Register(TrySetCanceled, source))
            {
                return await source.Task;
            }
        }
        catch (Exception ex)
        {
            if (ex is TaskCanceledException)
                span?.AppendTag(ex.Message);
            else
                span?.SetError(ex, message);
            throw;
        }
    }

    void TrySetCanceled(Object? state)
    {
        var source = state as TaskCompletionSource<Object>;
        if (source != null && !source.Task.IsCompleted) source.TrySetCanceled();
    }
    #endregion

    #region 接收
    /// <summary>接收数据</summary>
    /// <returns></returns>
    public IOwnerPacket Receive()
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);
        if (Server?.Client == null) throw new InvalidOperationException(nameof(Server));

        using var span = Tracer?.NewSpan($"net:{Name}:Receive", Server.BufferSize + "");
        try
        {
            var ep = Remote.EndPoint as EndPoint;
            var pk = new OwnerPacket(Server.BufferSize);
            var size = Server.Client.ReceiveFrom(pk.Buffer, ref ep);
            if (span != null) span.Value = size;

            return pk.Resize(size);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    /// <summary>异步接收数据</summary>
    /// <returns></returns>
    public virtual async Task<IOwnerPacket?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);
        if (Server?.Client == null) throw new InvalidOperationException(nameof(Server));

        using var span = Tracer?.NewSpan($"net:{Name}:Receive", Server.BufferSize + "");
        try
        {
            var ep = Remote.EndPoint as EndPoint;
            var pk = new OwnerPacket(Server.BufferSize);
            var socket = Server.Client;
#if NETFRAMEWORK || NETSTANDARD2_0
            var ar = socket.BeginReceiveFrom(pk.Buffer, 0, pk.Length, SocketFlags.None, ref ep, null, socket);
            var size = ar.IsCompleted ?
                socket.EndReceive(ar) :
                await Task.Factory.FromAsync(ar, e => socket.EndReceiveFrom(e, ref ep));
#elif NET7_0_OR_GREATER
            var result = await socket.ReceiveFromAsync(pk.GetMemory(), ep, cancellationToken);
            var size = result.ReceivedBytes;
#else
            var result = await socket.ReceiveFromAsync(pk.Buffer, SocketFlags.None, ep);
            var size = result.ReceivedBytes;
#endif
            if (span != null) span.Value = size;

            return pk.Resize(size);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    /// <summary>数据接收事件</summary>
    public event EventHandler<ReceivedEventArgs>? Received;

    internal void OnReceive(ReceivedEventArgs e)
    {
        LastTime = DateTime.Now;

        if (e != null) Received?.Invoke(this, e);

        // 我们约定，UDP收到空数据包时，结束会话
        if (e != null && (e.Packet == null || e.Packet.Length == 0))
        {
            Stop("Finish");
            Dispose();
        }
    }

    /// <summary>处理数据帧</summary>
    /// <param name="data">数据帧</param>
    void ISocketRemote.Process(IData data) => (Server as ISocketRemote).Process(data);
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
    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString()
    {
        if (Remote != null && !Remote.EndPoint.IsAny())
            return $"{Local}<={Remote.EndPoint}";
        else
            return Local.ToString();
    }
    #endregion

    #region ITransport接口
    Boolean ITransport.Open() => true;

    Boolean ITransport.Close() => true;
    #endregion

    #region 扩展接口
    private ConcurrentDictionary<String, Object?>? _items;
    /// <summary>数据项</summary>
    public IDictionary<String, Object?> Items => _items ??= new();

    /// <summary>设置 或 获取 数据项</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Object? this[String key] { get => _items != null && _items.TryGetValue(key, out var obj) ? obj : null; set => Items[key] = value; }
    #endregion

    #region 日志
    /// <summary>日志提供者</summary>
    public ILog Log { get; set; } = Logger.Null;

    /// <summary>是否输出发送日志。默认false</summary>
    public Boolean LogSend { get; set; }

    /// <summary>是否输出接收日志。默认false</summary>
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
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object?[] args) => Log.Info(LogPrefix + format, args);
    #endregion
}