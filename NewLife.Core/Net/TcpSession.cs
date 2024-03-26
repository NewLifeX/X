using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using NewLife.Data;
using NewLife.Log;

namespace NewLife.Net;

/// <summary>增强TCP客户端</summary>
public class TcpSession : SessionBase, ISocketSession
{
    #region 属性

    /// <summary>实际使用的远程地址。Remote配置域名时，可能有多个IP地址</summary>
    public IPAddress? RemoteAddress { get; private set; }

    /// <summary>收到空数据时抛出异常并断开连接。默认true</summary>
    public Boolean DisconnectWhenEmptyData { get; set; } = true;

    internal ISocketServer? _Server;

    /// <summary>Socket服务器。当前通讯所在的Socket服务器，其实是TcpServer/UdpServer。该属性决定本会话是客户端会话还是服务的会话</summary>
    ISocketServer ISocketSession.Server => _Server!;

    ///// <summary>自动重连次数，默认3。发生异常断开连接时，自动重连服务端。</summary>
    //public Int32 AutoReconnect { get; set; } = 3;

    /// <summary>不延迟直接发送。Tcp为了合并小包而设计，客户端默认false，服务端默认true</summary>
    public Boolean NoDelay { get; set; }

    /// <summary>KeepAlive间隔。默认0秒不启用</summary>
    public Int32 KeepAliveInterval { get; set; }

    /// <summary>SSL协议。默认None，服务端Default，客户端不启用</summary>
    public SslProtocols SslProtocol { get; set; } = SslProtocols.None;

    /// <summary>X509证书。用于SSL连接时验证证书指纹，可以直接加载pem证书文件，未指定时不验证证书</summary>
    /// <remarks>
    /// 可以使用pfx证书文件，也可以使用pem证书文件。
    /// 服务端必须指定证书，客户端可以不指定，除非服务端请求客户端证书。
    /// </remarks>
    /// <example>
    /// var cert = new X509Certificate2("file", "pass");
    /// </example>
    public X509Certificate? Certificate { get; set; }

    private SslStream? _Stream;

    #endregion 属性

    #region 构造

    /// <summary>实例化增强TCP</summary>
    public TcpSession()
    {
        Name = GetType().Name;
        Local.Type = NetType.Tcp;
        Remote.Type = NetType.Tcp;
    }

    /// <summary>使用监听口初始化</summary>
    /// <param name="listenPort"></param>
    public TcpSession(Int32 listenPort) : this() => Port = listenPort;

    /// <summary>用TCP客户端初始化</summary>
    /// <param name="client"></param>
    public TcpSession(Socket client) : this()
    {
        if (client == null) return;

        Client = client;
        var socket = client;
        if (socket.LocalEndPoint != null) Local.EndPoint = (IPEndPoint)socket.LocalEndPoint;
        if (socket.RemoteEndPoint != null) Remote.EndPoint = (IPEndPoint)socket.RemoteEndPoint;
    }

    internal TcpSession(ISocketServer server, Socket client)
        : this(client)
    {
        Active = true;
        _Server = server;
        Name = server.Name;
    }

    #endregion 构造

    #region 方法

    internal void Start()
    {
        // 管道
        Pipeline?.Open(CreateContext(this));

        // 设置读写超时
        var sock = Client;
        var timeout = Timeout;
        if (timeout > 0 && sock != null)
        {
            sock.SendTimeout = timeout;
            sock.ReceiveTimeout = timeout;
        }

        // 服务端SSL
        var cert = Certificate;
        if (sock != null && cert != null)
        {
            var ns = new NetworkStream(sock);
            var sslStream = new SslStream(ns, false);

            var sp = SslProtocol;

            WriteLog("服务端SSL认证，SslProtocol={0}，Issuer: {1}", sp, cert.Issuer);

            //var cert = new X509Certificate2("file", "pass");
            sslStream.AuthenticateAsServer(cert, false, sp, false);

            _Stream = sslStream;
        }

        ReceiveAsync();
    }

    /// <summary>打开</summary>
    protected override Boolean OnOpen()
    {
        // 服务端会话没有打开
        if (_Server != null) return false;

        var span = DefaultSpan.Current;
        var timeout = Timeout;
        var uri = Remote;
        var sock = Client;
        if (sock == null || !sock.IsBound)
        {
            span?.AppendTag($"Local={Local}");

            // 根据目标地址适配本地IPv4/IPv6
            if (Local.Address.IsAny() && uri != null && !uri.Address.IsAny())
            {
                Local.Address = Local.Address.GetRightAny(uri.Address.AddressFamily)!;
            }

            sock = Client = NetHelper.CreateTcp(Local.Address!.IsIPv4());
            //sock.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            if (NoDelay) sock.NoDelay = true;
            if (timeout > 0)
            {
                sock.SendTimeout = timeout;
                sock.ReceiveTimeout = timeout;
            }

            sock.Bind(Local.EndPoint);
            if (sock.LocalEndPoint is IPEndPoint ep) Local.EndPoint.Port = ep.Port;
            span?.AppendTag($"LocalEndPoint={sock.LocalEndPoint}");

            WriteLog("Open {0}", this);
        }

        // 打开端口前如果已设定远程地址，则自动连接
        if (uri == null || uri.EndPoint.IsAny()) return false;

        try
        {
            var addrs = uri.GetAddresses();
            span?.AppendTag($"addrs={addrs.Join()} port={uri.Port}");

            if (timeout <= 0)
                sock.Connect(addrs, uri.Port);
            else
            {
                // 采用异步来解决连接超时设置问题
                var ar = sock.BeginConnect(addrs, uri.Port, null, null);
                if (!ar.AsyncWaitHandle.WaitOne(timeout, true))
                {
                    sock.Close();
                    throw new TimeoutException($"The connection to server [{uri}] timed out! [{timeout}ms]");
                }

                sock.EndConnect(ar);
            }

            // 作为客户端，启用KeepAlive，及时释放无效连接
            if (KeepAliveInterval > 0) sock.SetTcpKeepAlive(true, KeepAliveInterval, KeepAliveInterval);

            RemoteAddress = (sock.RemoteEndPoint as IPEndPoint)?.Address;
            span?.AppendTag($"RemoteEndPoint={sock.RemoteEndPoint}");

            // 客户端SSL
            var sp = SslProtocol;
            if (sp != SslProtocols.None)
            {
                var host = uri.Host ?? uri.Address + "";
                WriteLog("客户端SSL认证，SslProtocol={0}，Host={1}", sp, host);

                // 服务端请求客户端证书时，需要传入证书
                var certs = new X509CertificateCollection();
                var cert = Certificate;
                if (cert != null) certs.Add(cert);

                var ns = new NetworkStream(sock);
                var sslStream = new SslStream(ns, false, OnCertificateValidationCallback);
                sslStream.AuthenticateAsClient(host, certs, sp, false);

                _Stream = sslStream;
            }
        }
        catch (Exception ex)
        {
            if (ex is SocketException) sock.Close();

            // 连接失败时，任何错误都放弃当前Socket
            Client = null;
            if (!Disposed && !ex.IsDisposed()) OnError("Connect", ex);

            throw;
        }

        //_Reconnect = 0;

        return true;
    }

    private Boolean OnCertificateValidationCallback(Object? sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        //WriteLog("Valid {0} {1}", certificate.Issuer, sslPolicyErrors);
        //if (chain?.ChainStatus != null)
        //{
        //    foreach (var item in chain.ChainStatus)
        //    {
        //        WriteLog("Chain {0} {1}", item.Status, item.StatusInformation?.Trim());
        //    }
        //}

        // 如果没有证书，全部通过
        if (Certificate is not X509Certificate2 cert) return true;
        if (chain == null) return false;

        return chain.ChainElements
                .Cast<X509ChainElement>()
                .Any(x => x.Certificate.Thumbprint == cert.Thumbprint);
    }

    /// <summary>关闭</summary>
    /// <param name="reason">关闭原因。便于日志分析</param>
    protected override Boolean OnClose(String reason)
    {
        var client = Client;
        if (client != null)
        {
            WriteLog("Close {0} {1}", reason, this);

            // 提前关闭这个标识，否则Close时可能触发自动重连机制
            Active = false;
            try
            {
                // 温和一点关闭连接
                client.Shutdown();
                client.Close();

                // 如果是服务端，这个时候就是销毁
                if (_Server != null) Dispose();
            }
            catch (Exception ex)
            {
                Client = null;
                if (!ex.IsDisposed()) OnError("Close", ex);
                //if (ThrowException) throw;

                return false;
            }
            Client = null;
        }

        return true;
    }

    #endregion 方法

    #region 发送

    private Int32 _bsize;
    private SpinLock _spinLock = new();

    /// <summary>发送数据</summary>
    /// <remarks>
    /// 目标地址由<seealso cref="SessionBase.Remote"/>决定
    /// </remarks>
    /// <param name="pk">数据包</param>
    /// <returns>是否成功</returns>
    protected override Int32 OnSend(Packet pk)
    {
        var count = pk.Total;

        if (Log != null && Log.Enable && LogSend) WriteLog("Send [{0}]: {1}", count, pk.ToHex(LogDataLength));

        using var span = Tracer?.NewSpan($"net:{Name}:Send", pk.Total + "", pk.Total);

        var rs = count;
        var sock = Client;
        if (sock == null) return -1;

        var gotLock = false;
        try
        {
            // 修改发送缓冲区，读取SendBufferSize耗时很大
            if (_bsize == 0) _bsize = sock.SendBufferSize;
            if (_bsize < count) sock.SendBufferSize = _bsize = count;

            // 加锁发送
            _spinLock.Enter(ref gotLock);

            if (_Stream == null)
            {
                if (count == 0)
                    rs = sock.Send(new Byte[0]);
                else if (pk.Next == null)
                    rs = sock.Send(pk.Data, pk.Offset, count, SocketFlags.None);
                else
                    rs = sock.Send(pk.ToSegments());
            }
            else
            {
                if (count == 0)
                    _Stream.Write(new Byte[0]);
                else if (pk.Next == null)
                    _Stream.Write(pk.Data, pk.Offset, count);
                else
                    _Stream.Write(pk.ToArray());
            }

            //// 检查返回值
            //if (rs != count) throw new NetException($"发送[{count:n0}]而成功[{rs:n0}]");
        }
        catch (Exception ex)
        {
            span?.SetError(ex, pk);

            if (!ex.IsDisposed())
            {
                OnError("Send", ex);

                // 发送异常可能是连接出了问题，需要关闭
                Close("SendError");

                //// 异步重连
                //ThreadPoolX.QueueUserWorkItem(Reconnect);

                //if (ThrowException) throw;
            }

            return -1;
        }
        finally
        {
            if (gotLock) _spinLock.Exit();
        }

        LastTime = DateTime.Now;

        return rs;
    }

    #endregion 发送

    #region 接收

    internal override Boolean OnReceiveAsync(SocketAsyncEventArgs se)
    {
        var sock = Client;
        if (sock == null || !Active || Disposed) throw new ObjectDisposedException(GetType().Name);

        var ss = _Stream;
        if (ss != null)
        {
            ss.BeginRead(se.Buffer!, se.Offset, se.Count, OnEndRead, se);

            return true;
        }

        return sock.ReceiveAsync(se);
    }

    /// <summary>异步读取数据流，仅用于SSL</summary>
    /// <param name="ar"></param>
    private void OnEndRead(IAsyncResult ar)
    {
        Int32 bytes;
        try
        {
            bytes = _Stream!.EndRead(ar);
        }
        catch (Exception ex)
        {
            if (ex is IOException ||
            ex is SocketException sex && sex.SocketErrorCode == SocketError.ConnectionReset)
            {
            }
            else
            {
                XTrace.WriteException(ex);
            }

            return;
        }
        if (ar.AsyncState is SocketAsyncEventArgs se) ProcessEvent(se, bytes, 1);
    }

    private Int32 _empty;

    /// <summary>预处理</summary>
    /// <param name="pk">数据包</param>
    /// <param name="remote">远程地址</param>
    /// <returns>将要处理该数据包的会话</returns>
    protected internal override ISocketSession? OnPreReceive(Packet pk, IPEndPoint remote)
    {
        if (pk.Count == 0)
        {
            using var span = Tracer?.NewSpan($"net:{Name}:EmptyData", remote?.ToString());

            // 连续多次空数据，则断开
            if (DisconnectWhenEmptyData && ++_empty >= 3)
            {
                Close("EmptyData");
                Dispose();

                return null;
            }
        }
        else
            _empty = 0;

        return this;
    }

    /// <summary>处理收到的数据</summary>
    /// <param name="e">接收事件参数</param>
    protected override Boolean OnReceive(ReceivedEventArgs e)
    {
        //var pk = e.Packet;
        //if ((pk == null || pk.Count == 0) && e.Message == null && !MatchEmpty) return true;

        // 分析处理
        RaiseReceive(this, e);

        return true;
    }

    #endregion 接收

    #region 自动重连

    ///// <summary>重连次数</summary>
    //private Int32 _Reconnect;
    //void Reconnect()
    //{
    //    if (Disposed) return;
    //    // 如果重连次数达到最大重连次数，则退出
    //    if (Interlocked.Increment(ref _Reconnect) > AutoReconnect) return;

    //    WriteLog("Reconnect {0}", this);

    //    using var span = Tracer?.NewSpan($"net:{Name}:Reconnect", _Reconnect + "");
    //    try
    //    {
    //        Open();
    //    }
    //    catch (Exception ex)
    //    {
    //        span?.SetError(ex, null);
    //    }
    //}

    #endregion 自动重连

    #region 辅助

    private String? _LogPrefix;

    /// <summary>日志前缀</summary>
    public override String LogPrefix
    {
        get
        {
            if (_LogPrefix == null)
            {
                var name = _Server == null ? "" : _Server.Name;
                _LogPrefix = $"{name}[{ID}].";
            }
            return _LogPrefix;
        }
        set { _LogPrefix = value; }
    }

    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString()
    {
        var local = Local;
        var remote = Remote.EndPoint;
        if (remote == null || remote.IsAny())
            return local.ToString();

        return _Server == null ? $"{local}=>{remote}" : $"{local}<={remote}";
    }

    #endregion 辅助
}