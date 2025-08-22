using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;

namespace NewLife.Net;

/// <summary>增强的UDP</summary>
/// <remarks>
/// 如果已经打开异步接收，还要使用同步接收，则同步Receive内部不再调用底层Socket，而是等待截走异步数据。
/// </remarks>
public class UdpServer : SessionBase, ISocketServer, ILogFeature
{
    #region 属性
    /// <summary>会话超时时间</summary>
    /// <remarks>
    /// 对于每一个会话连接，如果超过该时间仍然没有收到任何数据，则断开会话连接。
    /// </remarks>
    public Int32 SessionTimeout { get; set; }

    /// <summary>是否接收来自自己广播的环回数据。默认false</summary>
    public Boolean Loopback { get; set; }

    /// <summary>地址重用，主要应用于网络服务器重启交替。默认false</summary>
    /// <remarks>
    /// 一个端口释放后会等待两分钟之后才能再被使用，SO_REUSEADDR是让端口释放后立即就可以被再次使用。
    /// SO_REUSEADDR用于对TCP套接字处于TIME_WAIT状态下的socket(TCP连接中, 先调用close() 的一方会进入TIME_WAIT状态)，才可以重复绑定使用。
    /// </remarks>
    public Boolean ReuseAddress { get; set; }
    #endregion

    #region 构造
    /// <summary>实例化增强UDP</summary>
    public UdpServer()
    {
        Local.Type = NetType.Udp;
        Remote.Type = NetType.Udp;
        _Sessions = new SessionCollection(this);

        SessionTimeout = SocketSetting.Current.SessionTimeout;

        // 处理UDP最大并发接收
        MaxAsync = Environment.ProcessorCount * 16 / 10;

        if (SocketSetting.Current.Debug) Log = XTrace.Log;
    }

    /// <summary>使用监听口初始化</summary>
    /// <param name="listenPort"></param>
    public UdpServer(Int32 listenPort) : this() => Port = listenPort;

    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        if (Active) Close(GetType().Name + (disposing ? "Dispose" : "GC"));

        _Sessions.Dispose();
    }
    #endregion

    #region 方法
    /// <summary>打开</summary>
    /// <param name="cancellationToken">取消通知</param>
    protected override Task<Boolean> OnOpenAsync(CancellationToken cancellationToken)
    {
        var sock = Client;
        if (sock == null || !sock.IsBound)
        {
            var uri = Remote;
            // 根据目标地址适配本地IPv4/IPv6
            if (Local.Address.IsAny() && uri != null && !uri.Address.IsAny())
            {
                Local.Address = Local.Address.GetRightAny(uri.Address.AddressFamily);
            }

            Client = sock = NetHelper.CreateUdp(Local.Address.IsIPv4());

            try
            {
                // 地址重用，主要应用于网络服务器重启交替。前一个进程关闭时，端口在短时间内处于TIME_WAIT，导致新进程无法监听。
                // 启用地址重用后，即使旧进程未退出，新进程也可以监听，但只有旧进程退出后，新进程才能接受对该端口的连接请求
                if (ReuseAddress) sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // 无需设置SocketOptionName.PacketInformation，在ReceiveMessageFromAsync时会自动设置
                //if (sock.AddressFamily == AddressFamily.InterNetwork)
                //    sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
            }
            catch (Exception ex)
            {
                // 有些平台不支持地址重用，比如旧版A2上的Ubuntu16，但是云服务器的Linux都支持
                XTrace.WriteLine(ex.Message);
            }

            sock.Bind(Local.EndPoint);

            WriteLog("Open {0}", this);
        }

        return Task.FromResult(true);
    }

    /// <summary>关闭</summary>
    /// <param name="reason">关闭原因。便于日志分析</param>
    /// <param name="cancellationToken">取消通知</param>
    protected override Task<Boolean> OnCloseAsync(String reason, CancellationToken cancellationToken)
    {
        var sock = Client;
        if (sock != null)
        {
            WriteLog("Close {0} {1}", reason, this);

            try
            {
                // 以客户端模式工作时，发空包通知服务端结束会话
                var remote = Remote;
                if (remote != null && !remote.Address.IsAny() && remote.Port != 0)
                {
                    Send(Pool.Empty);
                }

                Client = null;

                CloseAllSession();

                sock.Shutdown();
            }
            catch (Exception ex)
            {
                if (!ex.IsDisposed()) OnError("Close", ex);
                //if (ThrowException) throw;

                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }
    #endregion

    #region 发送
    /// <summary>发送数据</summary>
    /// <remarks>
    /// 目标地址由<seealso cref="SessionBase.Remote"/>决定
    /// </remarks>
    /// <param name="data">数据包</param>
    /// <returns>是否成功</returns>
    protected override Int32 OnSend(IPacket data) => OnSend(data, Remote.EndPoint);

    /// <summary>发送数据</summary>
    /// <remarks>
    /// 目标地址由<seealso cref="SessionBase.Remote"/>决定
    /// </remarks>
    /// <param name="data">数据包</param>
    /// <returns>是否成功</returns>
    protected override Int32 OnSend(ArraySegment<Byte> data) => OnSend(data, Remote.EndPoint);

    /// <summary>发送数据</summary>
    /// <remarks>
    /// 目标地址由<seealso cref="SessionBase.Remote"/>决定
    /// </remarks>
    /// <param name="data">数据包</param>
    /// <returns>是否成功</returns>
    protected override Int32 OnSend(ReadOnlySpan<Byte> data) => OnSend(data, Remote.EndPoint);

    internal Int32 OnSend(IPacket pk, IPEndPoint remote)
    {
        var count = pk.Total;

        using var span = Tracer?.NewSpan($"net:{Name}:Send", count + "", count);

        try
        {
            var rs = 0;
            var sock = Client ?? throw new InvalidOperationException(nameof(OnSend));
            lock (sock)
            {
                if (sock.Connected && !sock.EnableBroadcast)
                {
                    if (Log.Enable && LogSend) WriteLog("Send [{0}]: {1}", count, pk.ToHex(LogDataLength));

                    if (count == 0)
                        rs = sock.Send(Pool.Empty);
                    else if (pk.Next == null && pk.TryGetArray(out var segment))
                        rs = sock.Send(segment.Array!, segment.Offset, segment.Count, SocketFlags.None);
#if NETCOREAPP || NETSTANDARD2_1
                    else if (pk.TryGetSpan(out var data))
                        rs = sock.Send(data);
#endif
                    else
                        rs = sock.Send(pk.ToSegments(), SocketFlags.None);
                }
                else
                {
                    sock.CheckBroadcast(remote.Address);
                    if (Log.Enable && LogSend) WriteLog("Send {2} [{0}]: {1}", count, pk.ToHex(LogDataLength), remote);

                    if (count == 0)
                        rs = sock.SendTo(Pool.Empty, remote);
                    else if (pk.Next == null && pk.TryGetArray(out var segment))
                        rs = sock.SendTo(segment.Array!, segment.Offset, segment.Count, SocketFlags.None, remote);
#if NET6_0_OR_GREATER
                    else if (pk.TryGetSpan(out var data))
                        rs = sock.SendTo(data, remote);
#endif
                    else
                        rs = sock.SendTo(pk.ToArray(), 0, count, SocketFlags.None, remote);
                }
            }

            return rs;
        }
        catch (Exception ex)
        {
            // 发生异常时，全量数据写入埋点
            span?.SetError(ex, pk);

            if (!ex.IsDisposed())
            {
                OnError("Send", ex);
            }
            return -1;
        }
    }

    internal Int32 OnSend(Byte[] data, Int32 offset, Int32 count, IPEndPoint remote)
    {
#if NET6_0_OR_GREATER
        return OnSend(new ReadOnlySpan<Byte>(data, offset, count), remote);
#else
        return OnSend(new ArraySegment<Byte>(data, offset, count), remote);
#endif
    }

    internal Int32 OnSend(ArraySegment<Byte> data, IPEndPoint remote)
    {
        var count = data.Count;
        var logCount = count > LogDataLength ? count : LogDataLength;

        using var span = Tracer?.NewSpan($"net:{Name}:Send", count + "", count);

        try
        {
            var rs = 0;
            var sock = Client ?? throw new InvalidOperationException(nameof(OnSend));
            lock (sock)
            {
                if (sock.Connected && !sock.EnableBroadcast)
                {
                    if (Log.Enable && LogSend) WriteLog("Send [{0}]: {1}", count, data.Array.ToHex(data.Offset, logCount));

                    rs = sock.Send(data.Array!, data.Offset, data.Count, SocketFlags.None);
                }
                else
                {
                    sock.CheckBroadcast(remote.Address);
                    if (Log.Enable && LogSend) WriteLog("Send {2} [{0}]: {1}", count, data.Array.ToHex(data.Offset, logCount), remote);

                    rs = sock.SendTo(data.Array!, data.Offset, data.Count, SocketFlags.None, remote);
                }
            }

            return rs;
        }
        catch (Exception ex)
        {
            // 发生异常时，全量数据写入埋点
            span?.SetError(ex, data.Array.ToHex(data.Offset, data.Count));

            if (!ex.IsDisposed())
            {
                OnError("Send", ex);
            }
            return -1;
        }
    }

    internal Int32 OnSend(ReadOnlySpan<Byte> data, IPEndPoint remote)
    {
        var count = data.Length;

        using var span = Tracer?.NewSpan($"net:{Name}:Send", count + "", count);

        try
        {
            var rs = 0;
            var sock = Client ?? throw new InvalidOperationException(nameof(OnSend));
            lock (sock)
            {
                if (sock.Connected && !sock.EnableBroadcast)
                {
                    if (Log.Enable && LogSend) WriteLog("Send [{0}]: {1}", count, data.ToHex(LogDataLength));

#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
                    rs = sock.Send(data, SocketFlags.None);
#else
                    rs = sock.Send(data.ToArray(), SocketFlags.None);
#endif
                }
                else
                {
                    sock.CheckBroadcast(remote.Address);
                    if (Log.Enable && LogSend) WriteLog("Send {2} [{0}]: {1}", count, data.ToHex(LogDataLength), remote);

#if NET6_0_OR_GREATER
                    rs = sock.SendTo(data, SocketFlags.None, remote);
#else
                    rs = sock.SendTo(data.ToArray(), SocketFlags.None, remote);
#endif
                }
            }

            return rs;
        }
        catch (Exception ex)
        {
            // 发生异常时，全量数据写入埋点
            span?.SetError(ex, data.ToHex());

            if (!ex.IsDisposed())
            {
                OnError("Send", ex);
            }
            return -1;
        }
    }

    /// <summary>发送消息并等待响应。必须调用会话的发送，否则配对会失败</summary>
    /// <param name="message">消息</param>
    /// <returns></returns>
    public override Task<Object> SendMessageAsync(Object message) => CreateSession(null, Remote.EndPoint).SendMessageAsync(message);

    /// <summary>发送消息并等待响应。必须调用会话的发送，否则配对会失败</summary>
    /// <param name="message">消息</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public override Task<Object> SendMessageAsync(Object message, CancellationToken cancellationToken) => CreateSession(null, Remote.EndPoint).SendMessageAsync(message, cancellationToken);
    #endregion

    #region 接收
    internal override Boolean OnReceiveAsync(SocketAsyncEventArgs se)
    {
        if (!Active || Client == null) return false;

        // 每次接收以后，这个会被设置为远程地址，这里重置一下，以防万一
        se.RemoteEndPoint = new IPEndPoint(IPAddress.Any.GetRightAny(Local.EndPoint.AddressFamily), 0);

        // 在StarAgent中，此时可能收到广播包，SocketFlags是Broadcast，需要清空，否则报错“参考的对象类型不支持尝试的操作”
        se.SocketFlags = SocketFlags.None;

        //return Client.ReceiveFromAsync(se);
        // Android 不支持 ReceiveMessageFromAsync 方法
        if (Runtime.Mono)
            return Client.ReceiveFromAsync(se);
        else
            return Client.ReceiveMessageFromAsync(se);
    }

    /// <summary>预处理</summary>
    /// <param name="pk">数据包</param>
    /// <param name="local">接收数据的本地地址</param>
    /// <param name="remote">远程地址</param>
    /// <returns>将要处理该数据包的会话</returns>
    internal protected override ISocketSession? OnPreReceive(IPacket pk, IPAddress local, IPEndPoint remote)
    {
        // 过滤自己广播的环回数据。放在这里，兼容UdpSession
        if (!Loopback && remote.Port == Port)
        {
            if (!Local.Address.IsAny())
            {
                if (remote.Address.Equals(Local.Address)) return null;
            }
            else
            {
                foreach (var item in NetHelper.GetIPsWithCache())
                {
                    if (remote.Address.Equals(item)) return null;
                }
            }
        }

        // 为该连接单独创建一个会话，方便直接通信
        return CreateSession(local, remote);
    }

    /// <summary>处理收到的数据</summary>
    /// <param name="e">接收事件参数</param>
    protected override Boolean OnReceive(ReceivedEventArgs e)
    {
        var pk = e.Packet;

        var remote = e.Remote;

        // 为该连接单独创建一个会话，方便直接通信
        var session = remote == null ? null : CreateSession(e.Local, remote);
        // 数据直接转交给会话，不再经过事件，那样在会话较多时极为浪费资源
        if (session is UdpSession us)
            us.OnReceive(e);
        else
        {
            // 没有匹配到任何会话时，才在这里显示日志。理论上不存在这个可能性
            if (Log.Enable && LogReceive && pk != null) WriteLog("Recv [{0}]: {1}", pk.Length, pk.ToHex(LogDataLength));
        }

        if (session != null) RaiseReceive(session, e);

        return true;
    }

    /// <summary>收到异常时如何处理。Tcp/Udp客户端默认关闭会话，但是Udp服务端不能关闭服务器，仅关闭会话</summary>
    /// <param name="se"></param>
    /// <returns>是否当作异常处理并结束会话</returns>
    internal override Boolean OnReceiveError(SocketAsyncEventArgs se)
    {
        // 缓冲区不足时，加大
        if (se.SocketError == SocketError.MessageSize && BufferSize < 1024 * 1024) BufferSize *= 2;

        // Udp服务器不能关闭自己，但是要关闭会话
        // Udp客户端一般不关闭自己
        if (se.SocketError is not SocketError.ConnectionReset and
            not SocketError.ConnectionAborted
            ) return true;

        // 关闭相应会话
        var sessions = _Sessions;
        if (sessions != null)
        {
            var ep = se.RemoteEndPoint as IPEndPoint;
            var ss = sessions.Get(ep + "");
            ss?.Dispose();
        }
        // 无论如何，Udp都不关闭自己
        return false;
    }
    #endregion

    #region 会话
    /// <summary>新会话时触发</summary>
    public event EventHandler<SessionEventArgs>? NewSession;

    private readonly SessionCollection _Sessions;
    /// <summary>会话集合。用地址端口作为标识，业务应用自己维持地址端口与业务主键的对应关系。</summary>
    public IDictionary<String, ISocketSession> Sessions => _Sessions;

    private readonly Dictionary<Int32, ISocketSession> _broadcasts = [];

    Int32 g_ID = 0;
    /// <summary>创建会话</summary>
    /// <param name="local">接收数据的本地地址</param>
    /// <param name="remoteEP">远程地址</param>
    /// <returns></returns>
    public virtual ISocketSession CreateSession(IPAddress? local, IPEndPoint remoteEP)
    {
        if (Disposed) throw new ObjectDisposedException(GetType().Name);

        var sessions = _Sessions;
        //if (sessions == null) return null;

        // 平均执行耗时260.80ns，其中55%花在sessions.Get上面，Get里面有加锁操作

        if (!Active)
        {
            // 根据目标地址适配本地IPv4/IPv6
            Local.Address = Local.Address.GetRightAny(remoteEP.AddressFamily);

            if (!Open()) throw new InvalidOperationException($"Open {Local} error");
        }

        // 需要查找已有会话，已有会话不存在时才创建新会话
        var session = sessions.Get(remoteEP + "");
        // 是否匹配广播端口
        var port = remoteEP.Port;
        if (session != null || _broadcasts.TryGetValue(port, out session)) return session;

        // 相同远程地址可能同时发来多个数据包，而底层采取多线程方式同时调度，导致创建多个会话
        lock (sessions)
        {
            // 需要查找已有会话，已有会话不存在时才创建新会话
            session = sessions.Get(remoteEP + "");
            if (session != null || _broadcasts.TryGetValue(port, out session)) return session;

            var us = new UdpSession(this, local, remoteEP)
            {
                Log = Log,
                LogSend = LogSend,
                LogReceive = LogReceive,
                Tracer = Tracer,
            };

            session = us;
            if (sessions.Add(session))
            {
                // 广播地址，接受任何地址响应数据
                if (Equals(remoteEP.Address, IPAddress.Broadcast))
                {
                    _broadcasts[port] = session;
                    session.OnDisposed += (s, e) =>
                    {
                        lock (_broadcasts)
                        {
                            if (s is UdpSession ss)
                                _broadcasts.Remove(ss.Remote.Port);
                        }
                    };
                }

                //us.ID = g_ID++;
                // 会话改为原子操作，避免多线程冲突
                us.ID = Interlocked.Increment(ref g_ID);
                us.Tracer = Tracer;
                us.Start();

                // 触发新会话事件
                NewSession?.Invoke(this, new SessionEventArgs(session));
            }
        }

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

    #region IServer接口
    void IServer.Start() => Open();

    void IServer.Stop(String? reason) => Close(reason ?? "Stop");
    #endregion

    #region 辅助
    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString()
    {
        var ss = Sessions;
        if (ss != null && ss.Count > 0)
            return $"{Local} [{ss.Count}]";
        else
            return Local.ToString();
    }
    #endregion
}

/// <summary>Udp扩展</summary>
public static class UdpHelper
{
    /// <summary>发送数据流</summary>
    /// <param name="udp"></param>
    /// <param name="stream"></param>
    /// <param name="remoteEP"></param>
    /// <returns>返回自身，用于链式写法</returns>
    public static UdpClient Send(this UdpClient udp, Stream stream, IPEndPoint? remoteEP = null)
    {
        Int64 total = 0;

        var buffer = Pool.Shared.Rent(1472);
        while (true)
        {
            var n = stream.Read(buffer, 0, buffer.Length);
            if (n <= 0) break;

            udp.Send(buffer, n, remoteEP);
            total += n;

            if (n < buffer.Length) break;
        }
        Pool.Shared.Return(buffer);

        return udp;
    }

    /// <summary>向指定目的地发送信息</summary>
    /// <param name="udp"></param>
    /// <param name="buffer">缓冲区</param>
    /// <param name="remoteEP"></param>
    /// <returns>返回自身，用于链式写法</returns>
    public static UdpClient Send(this UdpClient udp, Byte[] buffer, IPEndPoint? remoteEP = null)
    {
        udp.Send(buffer, buffer.Length, remoteEP);
        return udp;
    }

    /// <summary>向指定目的地发送信息</summary>
    /// <param name="udp"></param>
    /// <param name="message"></param>
    /// <param name="encoding">文本编码，默认null表示UTF-8编码</param>
    /// <param name="remoteEP"></param>
    /// <returns>返回自身，用于链式写法</returns>
    public static UdpClient Send(this UdpClient udp, String message, Encoding? encoding = null, IPEndPoint? remoteEP = null)
    {
        if (encoding == null)
            Send(udp, Encoding.UTF8.GetBytes(message), remoteEP);
        else
            Send(udp, encoding.GetBytes(message), remoteEP);
        return udp;
    }

    /// <summary>广播数据包</summary>
    /// <param name="udp"></param>
    /// <param name="buffer">缓冲区</param>
    /// <param name="port"></param>
    public static UdpClient Broadcast(this UdpClient udp, Byte[] buffer, Int32 port)
    {
        if (udp.Client != null && udp.Client.LocalEndPoint != null)
        {
            if (udp.Client.LocalEndPoint is IPEndPoint ip && !ip.Address.IsIPv4())
                throw new NotSupportedException("IPv6 does not support broadcasting!");
        }

        if (!udp.EnableBroadcast) udp.EnableBroadcast = true;

        udp.Send(buffer, buffer.Length, new IPEndPoint(IPAddress.Broadcast, port));

        return udp;
    }

    /// <summary>广播字符串</summary>
    /// <param name="udp"></param>
    /// <param name="message"></param>
    /// <param name="port"></param>
    public static UdpClient Broadcast(this UdpClient udp, String message, Int32 port)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        return Broadcast(udp, buffer, port);
    }

    /// <summary>接收字符串</summary>
    /// <param name="udp"></param>
    /// <param name="encoding">文本编码，默认null表示UTF-8编码</param>
    /// <returns></returns>
    public static String ReceiveString(this UdpClient udp, Encoding? encoding = null)
    {
        IPEndPoint? ep = null;
        var buffer = udp.Receive(ref ep);
        if (buffer == null || buffer.Length <= 0) return String.Empty;

        encoding ??= Encoding.UTF8;
        return encoding.GetString(buffer);
    }
}