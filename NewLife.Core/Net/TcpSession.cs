using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;

namespace NewLife.Net;

/// <summary>增强TCP客户端</summary>
/// <remarks>
/// <para>封装了TCP客户端和服务端会话的功能，支持SSL/TLS安全连接。</para>
/// <para>功能特性：</para>
/// <list type="bullet">
/// <item>支持同步和异步连接</item>
/// <item>支持SSL/TLS加密传输</item>
/// <item>支持客户端证书验证</item>
/// <item>支持TCP KeepAlive</item>
/// <item>线程安全的数据发送</item>
/// </list>
/// </remarks>
public partial class TcpSession : SessionBase, ISocketSession, IStreamSession
{
    #region 属性

    /// <summary>实际使用的远程地址</summary>
    /// <remarks>Remote配置域名时，可能有多个IP地址，此属性记录实际连接的地址</remarks>
    public IPAddress? RemoteAddress { get; private set; }

    ///// <summary>收到空数据时抛出异常并断开连接。默认true</summary>
    //public Boolean DisconnectWhenEmptyData { get; set; } = true;

    internal ISocketServer? _Server;

    /// <summary>Socket服务器</summary>
    /// <remarks>当前通讯所在的Socket服务器，其实是TcpServer/UdpServer。该属性决定本会话是客户端会话还是服务的会话</remarks>
    ISocketServer ISocketSession.Server => _Server!;

    /// <summary>不延迟直接发送</summary>
    /// <remarks>Tcp为了合并小包而设计，客户端默认false，服务端默认true</remarks>
    public Boolean NoDelay { get; set; }

    /// <summary>KeepAlive间隔（秒）</summary>
    /// <remarks>默认0秒不启用。启用后可及时检测连接断开</remarks>
    public Int32 KeepAliveInterval { get; set; }

    /// <summary>SSL协议版本</summary>
    /// <remarks>默认None不启用SSL，服务端使用Default，客户端不启用</remarks>
    public SslProtocols SslProtocol { get; set; } = SslProtocols.None;

    /// <summary>X509证书</summary>
    /// <remarks>
    /// <para>用于SSL连接时验证证书指纹，可以直接加载pem证书文件，未指定时不验证证书。</para>
    /// <para>可以使用pfx证书文件，也可以使用pem证书文件。</para>
    /// <para>服务端必须指定证书，客户端可以不指定，除非服务端请求客户端证书。</para>
    /// </remarks>
    /// <example>
    /// var cert = new X509Certificate2("file", "pass");
    /// </example>
    public X509Certificate? Certificate { get; set; }

    private SslStream? _Stream;

    #endregion 属性

    #region 构造

    /// <summary>实例化增强TCP客户端</summary>
    public TcpSession()
    {
        Name = GetType().Name;
        Local.Type = NetType.Tcp;
        Remote.Type = NetType.Tcp;
    }

    /// <summary>使用监听端口初始化</summary>
    /// <param name="listenPort">监听端口</param>
    public TcpSession(Int32 listenPort) : this() => Port = listenPort;

    /// <summary>用TCP客户端初始化</summary>
    /// <param name="client">已连接的Socket</param>
    public TcpSession(Socket client) : this()
    {
        if (client == null) return;

        Client = client;
        var socket = client;
        if (socket.LocalEndPoint is IPEndPoint localEp) Local.EndPoint = localEp;
        if (socket.RemoteEndPoint is IPEndPoint remoteEp) Remote.EndPoint = remoteEp;

#if !NETFRAMEWORK && !NETSTANDARD2_0
        // Unix域套接字回填路径，便于日志显示
        if (socket.LocalEndPoint is UnixDomainSocketEndPoint localUds && !localUds.ToString().IsNullOrEmpty())
        {
            Local.Type = NetType.Unix;
            Local.Path = localUds.ToString();
        }
        if (socket.RemoteEndPoint is UnixDomainSocketEndPoint remoteUds && !remoteUds.ToString().IsNullOrEmpty())
        {
            Remote.Type = NetType.Unix;
            Remote.Path = remoteUds.ToString();
        }
#endif
    }

    internal TcpSession(ISocketServer server, Socket client)
        : this(client)
    {
        // 服务端会话表示“连接已被接受”，构造时即视为活动：之后服务器的 Start() 只负责启动接收环，
        // 不再走打开流程（Open 因此幂等成功）
        Active = true;
        _Server = server;
        Name = server.Name;
    }

    #endregion 构造

    #region 方法

    internal void Start()
    {
        // 设置读写超时。Unix域套接字不支持TCP选项
        var sock = Client;
        var timeout = Timeout;
        if (timeout > 0 && sock != null && !Local.IsUnix)
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
            if (sp == SslProtocols.None) sp = SslProtocols.Tls12;

            WriteLog("服务端SSL认证，SslProtocol={0}，Issuer: {1}", sp, cert.Issuer);

            //var cert = new X509Certificate2("file", "pass");
            sslStream.AuthenticateAsServer(cert, false, sp, false);

            _Stream = sslStream;
        }

        // 服务端会话由服务器统一接管，必须启动接收环（事件模式），不支持拉取模式。
        // 这里直接置位而不是读取配置：子类若在 CreateSession 里设 AutoReceive=false，
        // 旧的"条件启动"会让属性与实际接收模式不一致（事件照收、拉取却被拒），必须在启动时收敛为 true
        AutoReceive = true;

        // 协议模式：数据经数据管道定界，启动消息泵（先于接收环，首个数据到达前就绪）
        if (Protocol != null) StartMessagePump();

        StartReceive();
    }

    /// <summary>打开</summary>
    /// <param name="cancellationToken">取消通知</param>
    protected override async Task<Boolean> OnOpenAsync(CancellationToken cancellationToken)
    {
        // 服务端会话没有打开
        if (_Server != null) return false;

        var span = DefaultSpan.Current;
        var timeout = Timeout;
        var uri = Remote;
        var isUnix = uri != null && uri.IsUnix;
        var sock = Client;
        if (sock == null || !sock.IsBound)
        {
            span?.AppendTag($"Local={Local}");

            if (isUnix)
            {
                // Unix域套接字以文件路径为地址，客户端无需绑定本地地址
                sock = Client = NetHelper.CreateUnix();
                if (timeout > 0)
                {
                    sock.SendTimeout = timeout;
                    sock.ReceiveTimeout = timeout;
                }
            }
            else
            {
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
            }

            WriteLog("Open {0}", this);
        }

        // 打开端口前如果已设定远程地址，则自动连接
        if (uri == null) return false;
        if (isUnix)
        {
            if (uri.Path.IsNullOrEmpty()) return false;
        }
        else if (uri.EndPoint.IsAny()) return false;

        try
        {
            // Unix域套接字直接以文件路径连接
            if (isUnix)
            {
#if NETFRAMEWORK || NETSTANDARD2_0
                throw new PlatformNotSupportedException("Unix Domain Socket 需要 .NET Standard 2.1 或更高版本的目标框架");
#else
                var ep = new UnixDomainSocketEndPoint(uri.Path!);
                span?.AppendTag($"RemoteEndPoint={ep}");

                await ConnectAsync(sock, ep, timeout, cancellationToken).ConfigureAwait(false);
#endif
            }
            else
            {
                var addrs = uri.GetAddresses();
                addrs = addrs.Where(ip => ip.AddressFamily == sock.AddressFamily).ToArray();
                span?.AppendTag($"addrs={addrs.Join()} port={uri.Port}");

                await ConnectAsync(sock, addrs, uri.Port, timeout, cancellationToken, uri).ConfigureAwait(false);
            }

            // 作为客户端，启用KeepAlive，及时释放无效连接。Unix域套接字不支持
            if (KeepAliveInterval > 0 && !isUnix) sock.SetTcpKeepAlive(true, KeepAliveInterval, KeepAliveInterval);

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
#if NETCOREAPP
                using var source = new CancellationTokenSource(timeout);
                await sslStream.AuthenticateAsClientAsync(
                    new SslClientAuthenticationOptions
                    {
                        TargetHost = host,
                        ClientCertificates = certs,
                        EnabledSslProtocols = sp,
                        CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    },
                    source.Token).ConfigureAwait(false);
#else
                await sslStream.AuthenticateAsClientAsync(host, certs, sp, false).ConfigureAwait(false);
#endif

                _Stream = sslStream;
            }
        }
        catch (Exception ex)
        {
            // 连接失败时，任何错误都放弃当前Socket。TLS 认证失败时 socket 已建立，不显式关闭会把句柄挂到 GC
            sock.Close();

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

#if !NETFRAMEWORK && !NETSTANDARD2_0
    /// <summary>异步连接Unix域套接字，支持超时</summary>
    /// <param name="sock">套接字</param>
    /// <param name="ep">远程终结点</param>
    /// <param name="timeout">超时时间（毫秒）</param>
    /// <param name="cancellationToken">取消通知</param>
    private static Task ConnectAsync(Socket sock, EndPoint ep, Int32 timeout, CancellationToken cancellationToken)
        => ConnectCoreAsync(sock, timeout, cancellationToken, null, 0, ep, ep);
#endif

    /// <summary>异步连接IP地址数组（同族地址按序回退），支持超时</summary>
    /// <param name="sock">套接字</param>
    /// <param name="addrs">目标地址数组</param>
    /// <param name="port">目标端口</param>
    /// <param name="timeout">超时时间（毫秒）</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <param name="remote">远程描述，仅用于超时异常消息</param>
    private static Task ConnectAsync(Socket sock, IPAddress[] addrs, Int32 port, Int32 timeout, CancellationToken cancellationToken, Object remote)
        => ConnectCoreAsync(sock, timeout, cancellationToken, addrs, port, null, remote);

    /// <summary>连接核心：统一TCP与Unix域套接字的同步/异步建连及超时处理</summary>
    /// <remarks>
    /// <para>无超时（timeout小于等于零）时同步连接；高版本.NET用可取消的 ConnectAsync 配预算取消源；低版本用 BeginConnect+WaitOne 模拟超时。</para>
    /// <para>超时异常类型随目标框架而异：高版本为预算取消源触发的 OperationCanceledException，低版本为本方法抛出的 TimeoutException。</para>
    /// </remarks>
    /// <param name="sock">套接字</param>
    /// <param name="timeout">超时时间（毫秒），小于等于零表示不启用超时</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <param name="addrs">目标地址数组（TCP），与 <paramref name="ep"/> 二选一</param>
    /// <param name="port">目标端口（TCP）</param>
    /// <param name="ep">远程终结点（Unix域套接字），与 <paramref name="addrs"/> 二选一</param>
    /// <param name="remote">远程描述，仅用于超时异常消息</param>
    private static async Task ConnectCoreAsync(Socket sock, Int32 timeout, CancellationToken cancellationToken,
        IPAddress[]? addrs, Int32 port, EndPoint? ep, Object? remote)
    {
        if (timeout <= 0)
        {
            if (ep != null)
                sock.Connect(ep);
            else
                sock.Connect(addrs!, port);

            return;
        }

#if NET5_0_OR_GREATER
        using var source = new CancellationTokenSource(timeout);
        using var cts2 = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, source.Token);
        using var _ = cts2.Token.Register(() => sock.Close());

        if (ep != null)
            await sock.ConnectAsync(ep, cts2.Token).ConfigureAwait(false);
        else
            await sock.ConnectAsync(addrs!, port, cts2.Token).ConfigureAwait(false);
#else
        // 采用异步来解决连接超时设置问题
        var ar = ep != null ? sock.BeginConnect(ep, null, null) : sock.BeginConnect(addrs!, port, null, null);
        if (!ar.AsyncWaitHandle.WaitOne(timeout, true))
        {
            sock.Close();
            throw new TimeoutException($"The connection to server [{remote}] timed out! [{timeout}ms]");
        }

        //sock.EndConnect(ar);
        await Task.Factory.FromAsync(ar, sock.EndConnect).ConfigureAwait(false);
#endif
    }

    /// <summary>关闭</summary>
    /// <param name="reason">关闭原因。便于日志分析</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>是否成功关闭</returns>
    protected override Task<Boolean> OnCloseAsync(String reason, CancellationToken cancellationToken)
    {
        var client = Client;
        if (client != null)
        {
            WriteLog("Close {0} {1}", reason, this);

            // 提前关闭这个标识，否则Close时可能触发自动重连机制
            // 此处只改“传输可用性”：服务端会话随后会被 Dispose（连带移出会话集合），
            // 客户端会话保留未释放以便重开，因此“Active 为假、Disposed 尚未置位”是关闭过程中的正常瞬态
            Active = false;
            try
            {
                // 先关闭SSL流
                var stream = _Stream;
                if (stream != null)
                {
                    _Stream = null;
                    try
                    {
                        stream.Close();
                        stream.Dispose();
                    }
                    catch { }
                }

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

                // 关闭动作（Shutdown/Close）抛异常时底层连接状态已不可信，服务端会话必须在此释放：
                // 否则它会以 Active=false 留在会话集合里，一直等到会话超时清理才摘除（这段时间集合与计数都把它算作在线）。
                // 此处 Dispose 是安全的：Active 已在上方置 false，Dispose 内部再走的 Close 会幂等短路；
                // 客户端会话不在此释放，由调用方保管以便重开。
                if (_Server != null) Dispose();

                return Task.FromResult(false);
            }
            Client = null;
        }

        return Task.FromResult(true);
    }

    #endregion 方法

    #region 发送
    // 发送出口：默认直发，另有可选的发送队列作第二出口。两者共用一把写锁，任一时刻只有一位写者在写套接字。
    // 例外：SSL 握手写（AuthenticateAsServer / AuthenticateAsClientAsync）不经写锁——握手发生在打开流程内，
    // 此时会话尚未对业务可用、不可能有并发发送；若将来允许握手期间发送数据，必须把握手写纳入写锁。
    //   Send(IPacket/byte[]/Span)  锁内同步直写：0 分配 0 拷贝，写完才返回，失败返回 -1
    //   SendAsync(IPacket)         锁内异步直写：0 分配 0 拷贝，等待可写期间不占线程
    //   SendAsync(Stream)          流式：读一块 → 写完（或挂起）再读下一块，内核缓冲即背压，内存有界
    //   SendFileAsync(文件)        锁内让内核零拷贝推文件（SendFile/TransmitFile）
    // 慢对端由内核发送缓冲形成天然背压；需要排队/限流的场合可在上层用 Actor 组合，不进发送核心。
    // 批量场景可选走 TcpSession.SendQueue（见 TcpSession.Stream.cs 出站队列），由发送泵在锁内批量写出。

    /// <summary>写锁。四个发送入口共用：任一时刻只有一位写者在写套接字，故多线程/多入口混用不会交错</summary>
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    /// <summary>获取续发总预算的截止时间。返回 0 表示不限制（Timeout 未启用）</summary>
    private Int64 GetSendDeadline() => Timeout > 0 ? Runtime.TickCount64 + Timeout : 0;

    private Int32 _bsize;

    /// <summary>按本次发送量调优内核发送缓冲（_bsize 缓存，读取 SendBufferSize 耗时很大）</summary>
    /// <remarks>
    /// <para>调用方须已持有写锁：套接字参数调整不能与锁内的 Send/Write 并发。</para>
    /// <para>_bsize 只增不减，仅同步直写路径使用；异步直写与流式分块不调整内核缓冲。</para>
    /// </remarks>
    /// <param name="sock">目标套接字</param>
    /// <param name="count">本次发送字节数</param>
    private void TuneSendBufferSize(Socket sock, Int32 count)
    {
        if (_bsize == 0) _bsize = sock.SendBufferSize;
        if (_bsize < count) sock.SendBufferSize = _bsize = count;
    }

    /// <summary>发送一段数据，短计数自动续发。返回时全部字节已交给内核，失败抛异常</summary>
    /// <remarks>同步 Send 在接收方窗口受限时可能只发出一部分；循环续发直到发完，总耗时超过预算按超时失败。异常由调用方统一转为发送失败处理</remarks>
    /// <param name="sock">目标套接字</param>
    /// <param name="buffer">数据缓冲</param>
    /// <param name="offset">起始偏移</param>
    /// <param name="count">字节数</param>
    /// <param name="deadline">续发总预算截止时间（Runtime.TickCount64 毫秒），0 不限制</param>
    /// <returns>已发送字节数，等于 count</returns>
    private static Int32 SendAll(Socket sock, Byte[] buffer, Int32 offset, Int32 count, Int64 deadline)
    {
        var total = 0;
        while (total < count)
        {
            var sent = sock.Send(buffer, offset + total, count - total, SocketFlags.None);
            if (sent <= 0) throw new IOException($"Send failed (result={sent}), {total}/{count} bytes sent");

            total += sent;

            // 续发前检查总预算：对端持续慢读时，单次 SendTimeout 约束不住整包发送的总耗时
            if (deadline > 0 && total < count && Runtime.TickCount64 > deadline)
                throw new TimeoutException($"Send timeout, {total}/{count} bytes sent");
        }

        return total;
    }

#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
    /// <summary>发送一段数据，短计数自动续发。返回时全部字节已交给内核，失败抛异常</summary>
    /// <param name="sock">目标套接字</param>
    /// <param name="data">数据</param>
    /// <param name="deadline">续发总预算截止时间（Runtime.TickCount64 毫秒），0 不限制</param>
    /// <returns>已发送字节数，等于 data.Length</returns>
    private static Int32 SendAll(Socket sock, ReadOnlySpan<Byte> data, Int64 deadline)
    {
        var total = 0;
        while (total < data.Length)
        {
            var sent = sock.Send(data[total..]);
            if (sent <= 0) throw new IOException($"Send failed (result={sent}), {total}/{data.Length} bytes sent");

            total += sent;

            if (deadline > 0 && total < data.Length && Runtime.TickCount64 > deadline)
                throw new TimeoutException($"Send timeout, {total}/{data.Length} bytes sent");
        }

        return total;
    }
#endif

    /// <summary>发送多段数据（scatter-gather），短计数后按段序跳过已发字节逐段续发</summary>
    /// <param name="sock">目标套接字</param>
    /// <param name="segments">数据段列表</param>
    /// <param name="total">总字节数</param>
    /// <param name="deadline">续发总预算截止时间（Runtime.TickCount64 毫秒），0 不限制</param>
    /// <returns>已发送字节数，等于 total</returns>
    private static Int32 SendAll(Socket sock, IList<ArraySegment<Byte>> segments, Int32 total, Int64 deadline)
    {
        // 先尝试一次多段发送（平台可合并系统调用）；正常路径一次发完直接返回
        var sent = sock.Send(segments);
        if (sent <= 0) throw new IOException($"Send failed (result={sent}), 0/{total} bytes sent");
        if (sent >= total) return sent;

        // 短计数：按段序跳过已发字节，逐段续发（复用原段缓冲，零拷贝）
        var skip = sent;
        foreach (var seg in segments)
        {
            if (skip >= seg.Count)
            {
                skip -= seg.Count;
                continue;
            }

            sent += SendAll(sock, seg.Array!, seg.Offset + skip, seg.Count - skip, deadline);
            skip = 0;
        }

        return sent;
    }

#if NET5_0_OR_GREATER
    /// <summary>异步发送的预算取消源。单写者复用；正常完成时解除计时，触发取消即超时失败</summary>
    private CancellationTokenSource? _sendCts;
#endif

    /// <summary>直发：锁内同步写出整个数据包（直接写数据包缓冲，0 拷贝 0 分配），写完才返回</summary>
    /// <remarks>写锁保证任一时刻只有一位写者；失败已上报并按失败关闭会话</remarks>
    /// <param name="pk">数据包。借用语义：调用方保留句柄，用完自行释放</param>
    /// <returns>已发送字节数；失败返回 -1</returns>
    private Int32 DirectSend(IPacket pk)
    {
        if (pk == null) return -1;

        if (Log != null && Log.Enable && LogSend) WriteLog("Send [{0}]: {1}", pk.Total, pk.ToHex(LogDataLength));

        Exception? error = null;
        var rs = -1;

        _writeLock.Wait();
        try
        {
            rs = WritePacket(pk);
        }
        catch (Exception ex)
        {
            error = ex;
        }
        finally
        {
            _writeLock.Release();
        }

        // 上报与关闭放到锁外：OnError/Close 会触发用户事件回调，回调内再次 Send 将在同一线程二次进入写锁而死锁
        if (error != null)
        {
            ReportSendError(error, pk);

            return -1;
        }

        return rs;
    }

    /// <summary>直发：锁内同步写出一段数据（直接写调用方缓冲，0 拷贝 0 分配），写完才返回</summary>
    /// <param name="data">数据。调用方需保证返回前不改写该缓冲</param>
    /// <returns>已发送字节数；失败返回 -1</returns>
    private Int32 DirectSend(ReadOnlySpan<Byte> data)
    {
        if (data.IsEmpty) return 0;

        if (Log != null && Log.Enable && LogSend) WriteLog("Send [{0}]: {1}", data.Length, data.ToHex(LogDataLength));

        Exception? error = null;
        var rs = -1;

        _writeLock.Wait();
        try
        {
            rs = WriteMemory(data);
        }
        catch (Exception ex)
        {
            error = ex;
        }
        finally
        {
            _writeLock.Release();
        }

        if (error != null)
        {
            ReportSendError(error, null);

            return -1;
        }

        return rs;
    }

    /// <summary>直发：锁内异步写出整个数据包（0 拷贝 0 分配），等待可写期间不占线程</summary>
    /// <remarks>失败已上报并按失败关闭会话；同时使用多个发送入口时由写锁串行化</remarks>
    /// <param name="pk">数据包。借用语义：调用方保留句柄，用完自行释放</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已发送字节数；失败返回 -1</returns>
    private async ValueTask<Int32> DirectSendAsync(IPacket pk, CancellationToken cancellationToken = default)
    {
        if (pk == null) return -1;

        cancellationToken.ThrowIfCancellationRequested();

        if (Log != null && Log.Enable && LogSend) WriteLog("Send [{0}]: {1}", pk.Total, pk.ToHex(LogDataLength));

        Exception? error = null;
        var rs = -1;

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            rs = await WritePacketAsync(pk).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            error = ex;
        }
        finally
        {
            _writeLock.Release();
        }

        // 上报与关闭放锁外：OnError/Close 会触发用户事件回调，回调内再次 Send 会在同一把写锁上二次等待而死锁
        if (error != null)
        {
            ReportSendError(error, pk);

            return -1;
        }

        return rs;
    }

    /// <summary>直发：锁内异步写出一块内存（供流式分块使用，0 拷贝 0 分配）</summary>
    /// <param name="data">数据。调用方需保证写完前不改写该缓冲</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已发送字节数；失败返回 -1</returns>
    private async ValueTask<Int32> DirectSendAsync(ReadOnlyMemory<Byte> data, CancellationToken cancellationToken = default)
    {
        if (data.IsEmpty) return 0;

        cancellationToken.ThrowIfCancellationRequested();

        if (Log != null && Log.Enable && LogSend) WriteLog("Send [{0}]: {1}", data.Length, data.Span.ToHex(LogDataLength));

        Exception? error = null;
        var rs = -1;

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            rs = await WriteMemoryAsync(data).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            error = ex;
        }
        finally
        {
            _writeLock.Release();
        }

        // 上报与关闭放锁外：OnError/Close 会触发用户事件回调，回调内再次 Send 会在同一把写锁上二次等待而死锁
        if (error != null)
        {
            ReportSendError(error, null);

            return -1;
        }

        return rs;
    }

    /// <summary>写核心（同步）：把整包（含链）写完才返回；短计数自动续发，失败抛异常</summary>
    /// <param name="pk">数据包</param>
    /// <returns>已发送字节数</returns>
    private Int32 WritePacket(IPacket pk)
    {
        var count = pk.Total;
        if (count == 0) return 0;

        var sock = Client ?? throw new InvalidOperationException($"Session [{Name}] is not open.");

        TuneSendBufferSize(sock, count);

        // SSL 流内部处理部分写：整段写完才返回，无需续发循环
        if (_Stream is { } stream)
        {
            pk.CopyTo(stream);

            LastTime = DateTime.Now;

            return count;
        }

        // 一次系统调用写出整包：单节点直接写数据包自己的缓冲（0 拷贝 0 分配）；
        // 链式包用散列写（scatter-gather）一次提交整条链——逐节点多次写会把一条逻辑帧拆成多个 TCP 段，
        // 对端“整帧同窗到达”的零拷贝快路径随之失效（同一次解读出内存视图而非流式体）
        var deadline = GetSendDeadline();
        if (pk.Next == null && pk.TryGetArray(out var segment))
            SendAll(sock, segment.Array!, segment.Offset, segment.Count, deadline);
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
        else if (pk.Next == null)
            SendAll(sock, pk.GetMemory().Span, deadline);
#endif
        else
            SendAll(sock, pk.ToSegments(), count, deadline);

        LastTime = DateTime.Now;

        return count;
    }

    /// <summary>同步写一块内存（零拷贝：直接写调用方缓冲），短计数自动续发</summary>
    /// <param name="data">数据</param>
    /// <returns>已发送字节数</returns>
    private Int32 WriteMemory(ReadOnlySpan<Byte> data)
    {
        var sock = Client ?? throw new InvalidOperationException($"Session [{Name}] is not open.");
        Int32 rs;

        TuneSendBufferSize(sock, data.Length);

        if (_Stream is { } stream)
        {
            // SSL 流内部处理部分写：整段写完才返回，失败直接抛异常，无需续发循环
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
            stream.Write(data);
#else
            stream.Write(data.ToArray());
#endif
            rs = data.Length;
        }
        else
        {
            // 同步 Send 在接收方窗口受限时可能只发出一部分，续发循环保证整段送出；总预算按会话 Timeout 计时
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
            rs = SendAll(sock, data, GetSendDeadline());
#else
            rs = SendAll(sock, data.ToArray(), 0, data.Length, GetSendDeadline());
#endif
        }

        LastTime = DateTime.Now;

        return rs;
    }

#if NET5_0_OR_GREATER
    /// <summary>写核心（异步）：把整包（含链）写完才返回，等待可写期间不占线程；失败抛异常</summary>
    /// <remarks>失败一律抛异常、不在此上报：调用方须先释放写锁再调 <see cref="ReportSendError"/>，否则用户回调内再次 Send 会在同一把写锁上二次等待而死锁</remarks>
    /// <param name="pk">数据包</param>
    /// <returns>已发送字节数；无套接字返回 -1</returns>
    private async ValueTask<Int32> WritePacketAsync(IPacket pk)
    {
        var count = pk.Total;
        if (count == 0) return 0;

        // 逐段写出（通常只有一段），每段发完再发下一段
        for (var node = pk; node != null; node = node.Next)
        {
            if (node.Length == 0) continue;

            var rs = await WriteMemoryAsync(node.GetMemory()).ConfigureAwait(false);
            if (rs < 0) return -1;
        }

        LastTime = DateTime.Now;

        return count;
    }

    /// <summary>写核心（异步）：写完一块内存才返回，短计数自动续发；失败抛异常</summary>
    /// <remarks>
    /// <para>失败一律抛异常、不在此上报（同 <see cref="WritePacketAsync"/>）。</para>
    /// <para>不接收调用方取消令牌：写侧取消由 <see cref="SessionBase.Timeout"/> 预算承担（与同步写一致），
    /// 调用方取消在等写锁与流式读块（<see cref="Stream.ReadAsync(Byte[], Int32, Int32, CancellationToken)"/>）的边界生效。</para>
    /// </remarks>
    /// <param name="data">数据</param>
    /// <returns>已发送字节数；无套接字返回 -1</returns>
    private async ValueTask<Int32> WriteMemoryAsync(ReadOnlyMemory<Byte> data)
    {
        var count = data.Length;
        if (count == 0) return 0;

        var sock = Client;
        if (sock == null) return -1;

        var total = 0;

        // 预算取消源：正常完成不取消（可复用）；已取消说明上轮超时失败，重建后本轮结束即终止
        var cts = _sendCts ??= new CancellationTokenSource();
        if (cts.IsCancellationRequested) cts = _sendCts = new CancellationTokenSource();

        var timeout = Timeout;
        try
        {
            while (total < count)
            {
                // 每次发送前重设计时预算
                if (timeout > 0) cts.CancelAfter(timeout);

                Int32 sent;
                if (_Stream is { } stream)
                {
                    // SSL 流内部处理部分写：整段写完才返回，无需续发
                    await stream.WriteAsync(data[total..], cts.Token).ConfigureAwait(false);
                    sent = count - total;
                }
                else
                    sent = await sock.SendAsync(data[total..], SocketFlags.None, cts.Token).ConfigureAwait(false);

                if (sent <= 0) throw new IOException($"Send failed (result={sent}), {total}/{count} bytes sent");

                total += sent;
            }
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // 预算超时：与同步发送超时一致，按发送失败处理（由调用方在锁外上报）
            throw new TimeoutException($"Send timeout, {total}/{count} bytes sent");
        }
        finally
        {
            // 解除计时预算（触发过取消的取消源不复用）
            if (!cts.IsCancellationRequested) cts.CancelAfter(System.Threading.Timeout.Infinite);
        }

        LastTime = DateTime.Now;

        return total;
    }
#else
    /// <summary>写核心（异步）。本框架无带取消令牌的 Socket.SendAsync 重载，降级为同步写完（仍然零拷贝）；失败抛异常</summary>
    /// <param name="pk">数据包</param>
    /// <returns>已发送字节数</returns>
    private ValueTask<Int32> WritePacketAsync(IPacket pk) => new(WritePacket(pk));

    /// <summary>写核心（异步，低版本 TFM）。降级为同步写完；失败抛异常</summary>
    /// <param name="data">数据</param>
    /// <returns>已发送字节数</returns>
    private ValueTask<Int32> WriteMemoryAsync(ReadOnlyMemory<Byte> data) => new(WriteMemory(data.Span));
#endif

    /// <summary>发送失败上报：记错误日志并关闭会话</summary>
    /// <remarks>调用方必须已释放写锁：OnError/Close 会触发用户事件回调，回调内再次 Send 会二次进入写锁而死锁</remarks>
    /// <param name="error">发送异常</param>
    /// <param name="data">出错的发送数据，便于日志定位</param>
    private void ReportSendError(Exception error, Object? data)
    {
        if (error.IsDisposed()) return;

        if (Tracer is { } tracer)
        {
            using var span = tracer.NewSpan($"net:{Name}:Send");
            span?.SetError(error, data);
        }

        OnError("Send", error);

        // 发送异常可能是连接出了问题，需要关闭
        Close("SendError");
    }

    /// <summary>发送数据包（同步直发）：写完才返回</summary>
    /// <param name="data">数据包。借用语义：调用方保留句柄，用完自行释放</param>
    /// <returns>已发送字节数；失败返回 -1</returns>
    protected override Int32 OnSend(IPacket data) => DirectSend(data);

    /// <summary>发送数据（同步直发）：写完才返回</summary>
    /// <param name="data">数据</param>
    /// <returns>已发送字节数；失败返回 -1</returns>
    protected override Int32 OnSend(ArraySegment<Byte> data)
        => data.Array == null || data.Count <= 0 ? 0 : DirectSend(new ReadOnlySpan<Byte>(data.Array, data.Offset, data.Count));

    /// <summary>发送数据（同步直发）：写完才返回</summary>
    /// <param name="data">数据</param>
    /// <returns>已发送字节数；失败返回 -1</returns>
    protected override Int32 OnSend(ReadOnlySpan<Byte> data) => DirectSend(data);

    #region 发送泵
    private CancellationTokenSource? _pumpCts;
    private Task? _pumpTask;
    private Int32 _pumpThreadId;

    /// <summary>启动发送泵。专用线程（LongRunning）：同步泵不能占用线程池线程，否则同步入队方在等待水位时把线程池占满，泵与入队方会互相等待而死锁</summary>
    /// <param name="queue">发送队列</param>
    private void StartSendPump(Pipe queue)
    {
        var cts = new CancellationTokenSource();
        _pumpCts = cts;

        _pumpTask = Task.Factory.StartNew(() => SendPumpLoop(queue, cts.Token), cts.Token,
            TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    /// <summary>停止发送泵。先结束写侧唤醒挂起读，再限时等泵退出——泵退出后不再触碰队列缓冲，方能安全释放队列</summary>
    /// <remarks>本方法可能由泵线程自身触发（发送失败→上报→关闭），此时等待就是自死锁，按线程判定跳过</remarks>
    /// <param name="queue">发送队列</param>
    private void StopSendPump(Pipe queue)
    {
        // 结束写侧：挂起的 ReadAsync 立即返回完成，入队方的水位等待随之结束
        queue.Writer.Complete();

        var task = _pumpTask;
        if (task == null || task.IsCompleted) return;
        if (Environment.CurrentManagedThreadId == _pumpThreadId) return;

        // 限时等待：泵可能正阻塞在写锁或慢对端的套接字写上。超时先中止泵再给一小段收尾时间，
        // 仍不退出就放手——残余缓冲随管道回收，泵持有的帧自带引用计数，不会读到已归还的内存。
        // 等待上限刻意压在 2 秒内：Close 是常见操作，不能因为一个慢对端把调用方长时间卡住
        try
        {
            if (!task.Wait(1000))
            {
                _pumpCts?.Cancel();
                task.Wait(1000);
            }
        }
        catch (AggregateException) { }
    }

    /// <summary>发送泵：从队列取数据批量写出。一个读窗口内累积的多条消息一次散列写提交，得出批量发送</summary>
    /// <param name="queue">发送队列</param>
    /// <param name="cancellationToken">取消令牌。会话关闭时触发</param>
    private void SendPumpLoop(Pipe queue, CancellationToken cancellationToken)
    {
        _pumpThreadId = Environment.CurrentManagedThreadId;

        var reader = queue.Reader;
        try
        {
            // 会话已终结时立即收尾退出。只认两种终态：已释放，或服务端会话已关闭不再活动。
            // 服务端会话关闭即 Dispose、不会重开，而客户端会话关闭后保留以便重开，
            // 故客户端“已关闭未释放”不算终结，其新建的泵理应留着等重开后的数据。
            //
            // 本泵可能是关闭收尾之后才建出来的——入队方过了 Open 检查、随后关闭完成（见 SendQueue 的建队列路径），
            // 此时队列读侧不会再有写入、泵令牌也无人取消，继续阻塞在读取上会让这条 LongRunning 专用线程
            // 连同它引用的会话永久存活。
            if (Disposed || (_Server != null && !Active))
            {
                // 按关闭收尾的同构动作收尾本队列：完成写侧让等水位的入队方立即退出
                // （完成写侧不触发 Resumed，必须显式广播），读侧由 finally 完成并归还残余句柄
                queue.Writer.Complete();
                WakeQueueWaiters();

                return;
            }

            while (true)
            {
                var result = reader.ReadAsync(cancellationToken).GetAwaiter().GetResult();
                var count = result.Buffer.Length;

                if (count > 0)
                {
                    // 零拷贝切出整窗并推进消费窗口（同时解除写侧水位）：多条消息合并为一次散列写
                    var frame = reader.TakeFrame(count);

                    Exception? error = null;
                    _writeLock.Wait();
                    try
                    {
                        WritePacket(frame);
                        LastTime = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }
                    finally
                    {
                        _writeLock.Release();
                    }

                    frame.TryDispose();

                    // 上报与关闭放锁外：OnError/Close 会触发用户事件回调，回调内再次 Send 会在同一线程二次进入写锁而死锁
                    if (error != null)
                    {
                        ReportSendError(error, null);

                        break;
                    }
                }

                if (result.IsCompleted) break;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            ReportSendError(ex, null);
        }
        finally
        {
            reader.Complete();
        }
    }
    #endregion
    #endregion 发送

    #region 接收
    /// <summary>同步直读数据。重写以支持SSL</summary>
    /// <returns></returns>
    protected override IOwnerPacket? OnDirectReceive()
    {
        var ss = _Stream;
        if (ss == null) return base.OnDirectReceive();

        using var span = Tracer?.NewSpan($"net:{Name}:Receive");
        try
        {
            var pk = new OwnerPacket(BufferSize);
            var size = ss.Read(pk.Buffer, 0, pk.Length);
            span?.Value = size;

            return pk.Resize(size);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    /// <summary>异步直读数据。重写以支持SSL</summary>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    protected override async Task<IOwnerPacket?> OnDirectReceiveAsync(CancellationToken cancellationToken = default)
    {
        var ss = _Stream;
        if (ss == null) return await base.OnDirectReceiveAsync(cancellationToken).ConfigureAwait(false);

        using var span = Tracer?.NewSpan($"net:{Name}:ReceiveAsync", BufferSize + "");
        try
        {
            var pk = new OwnerPacket(BufferSize);
            var size = await ss.ReadAsync(pk.Buffer, 0, pk.Length, cancellationToken).ConfigureAwait(false);
            span?.Value = size;

            return pk.Resize(size);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    internal override Boolean OnReceiveAsync(SocketAsyncEventArgs se)
    {
        var sock = Client;
        if (sock == null || !Active || Disposed) throw new ObjectDisposedException(GetType().Name);

        // 背压：数据管道达到暂停水位时暂存接收参数，消费恢复后经 Resumed 事件重启（仍暂停则再次暂存）
        if (_pipe?.IsPaused == true)
        {
            ParkReceive(se);

            return true;
        }

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
            XTrace.WriteException(ex);

            // 读失败统一按对端已关闭（0字节）处理，触发会话断开链路。
            // 仅记日志会让本接收参数既不重投也不释放，SSL 会话无感知悬挂直到超时清理。
            if (ar.AsyncState is SocketAsyncEventArgs args)
            {
                args.SocketError = SocketError.Success;
                ProcessEvent(args, 0, 1);
            }

            return;
        }
        if (ar.AsyncState is SocketAsyncEventArgs se) ProcessEvent(se, bytes, 1);
    }

    //private Int32 _empty;

    /// <summary>预处理</summary>
    /// <param name="pk">数据包</param>
    /// <param name="local">接收数据的本地地址</param>
    /// <param name="remote">远程地址</param>
    /// <returns>将要处理该数据包的会话</returns>
    protected internal override ISocketSession? OnPreReceive(IPacket pk, IPAddress local, IPEndPoint remote)
    {
        if (pk.Length == 0)
        {
            using var span = Tracer?.NewSpan($"net:{Name}:EmptyData", remote?.ToString());

            // 连续多次空数据，则断开
            //if (DisconnectWhenEmptyData && ++_empty >= 3)
            {
                var reason = CheckClosed();
                if (reason != null)
                {
                    Close(reason);
                    Dispose();

                    return null;
                }
            }
        }
        //else
        //    _empty = 0;

        // 流式视图：本轮数据投递共享切片到入站管道（引用计数），不影响轮句柄与后续传统管道处理链
        AppendToPipe(pk);

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

    #region 辅助
    /// <summary>日志前缀</summary>
    public override String? LogPrefix
    {
        get
        {
            var pf = base.LogPrefix;
            if (pf == null && _Server != null)
                pf = base.LogPrefix = $"{_Server.Name}[{ID}].";

            return pf;
        }
        set { base.LogPrefix = value; }
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
    #endregion
}