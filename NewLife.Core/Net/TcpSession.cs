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

        // 服务端SSL：异步认证。同步握手会在受理线程上阻塞，慢/恶意客户端可长时间占住受理槽位
        // （MaxAsync 个慢握手即让新连接受理停摆）
        var cert = Certificate;
        if (sock != null && cert != null)
        {
            // 先占写锁再发起握手：握手期间到达的业务发送会在锁上等待，不会把明文写进TLS流；
            // 锁由握手任务在结束后释放，故此处不能省略
            _writeLock.Wait();
            _ = StartSslAsync(sock, cert, timeout);

            return;
        }

        StartServerReceive();
    }

    /// <summary>服务端SSL认证（异步），完成后启动接收环</summary>
    /// <remarks>
    /// <para>握手写持有写锁（由 <see cref="Start"/> 抢占）：握手期间到达的业务发送会等待锁，不会把明文写进TLS流。</para>
    /// <para>握手不占受理槽位：慢/恶意客户端挂着不完成握手时，新连接仍可被受理（旧实现在受理线程上同步握手）。</para>
    /// <para>握手失败（超时、证书错误、明文请求打到TLS端口）即释放会话，避免其滞留会话集合直到超时。</para>
    /// </remarks>
    /// <param name="sock">已接受的套接字</param>
    /// <param name="cert">服务端证书</param>
    /// <param name="timeout">认证超时（毫秒），小于等于零表示不启用</param>
    private async Task StartSslAsync(Socket sock, X509Certificate cert, Int32 timeout)
    {
        Exception? error = null;
        SslStream? sslStream = null;
        try
        {
            var ns = new NetworkStream(sock);
            sslStream = new SslStream(ns, false);

            var sp = SslProtocol;
            if (sp == SslProtocols.None) sp = SslProtocols.Tls12;

            WriteLog("服务端SSL认证，SslProtocol={0}，Issuer: {1}", sp, cert.Issuer);

            await AuthenticateAsServerAsync(sslStream, sock, cert, sp, timeout).ConfigureAwait(false);

            _Stream = sslStream;
        }
        catch (Exception ex)
        {
            error = ex;
        }
        finally
        {
            // 握手结束即释放写锁；其后的失败处理（含会话释放）可能触发用户回调，必须在锁外执行
            _writeLock.Release();
        }

        if (error != null)
        {
            WriteLog("服务端SSL认证失败，释放会话 {0}", error.Message);
            if (!Disposed && !error.IsDisposed()) OnError("SslAuthenticate", error);

            sslStream?.TryDispose();
            Dispose();

            return;
        }

        try
        {
            StartServerReceive();
        }
        catch (Exception ex)
        {
            // 启动接收环失败。同步路径由受理流程捕获后释放会话，异步路径必须自行兜底，
            // 否则会话留在集合里既不收数据也不断开
            WriteLog("服务端会话启动失败，释放会话 {0}", ex.Message);
            if (!Disposed && !ex.IsDisposed()) OnError("Start", ex);

            Dispose();
        }
    }

    /// <summary>启动服务端会话的接收环与消息泵（服务端会话由服务器统一接管接收）</summary>
    private void StartServerReceive()
    {
        // 服务端会话由服务器统一接管，必须启动接收环（事件模式），不支持拉取模式。
        // 这里直接置位而不是读取配置：子类若在 CreateSession 里设 AutoReceive=false，
        // 旧的"条件启动"会让属性与实际接收模式不一致（事件照收、拉取却被拒），必须在启动时收敛为 true
        AutoReceive = true;

        // 协议模式：数据经数据管道定界，启动消息泵（先于接收环，首个数据到达前就绪）
        StartReceiveFlow();

        StartReceive();
    }

    /// <summary>服务端SSL认证，支持超时</summary>
    /// <param name="sslStream">SSL流</param>
    /// <param name="sock">底层套接字（旧框架超时后关闭以中止挂起的握手读）</param>
    /// <param name="cert">服务端证书</param>
    /// <param name="protocols">协议版本</param>
    /// <param name="timeout">超时（毫秒），小于等于零表示不启用</param>
#if NETCOREAPP
    private static async Task AuthenticateAsServerAsync(SslStream sslStream, Socket sock, X509Certificate cert, SslProtocols protocols, Int32 timeout)
    {
        using var source = timeout > 0 ? new CancellationTokenSource(timeout) : new CancellationTokenSource();
        await sslStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
        {
            ServerCertificate = cert,
            EnabledSslProtocols = protocols,
            CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
        }, source.Token).ConfigureAwait(false);
    }
#else
    private static async Task AuthenticateAsServerAsync(SslStream sslStream, Socket sock, X509Certificate cert, SslProtocols protocols, Int32 timeout)
    {
        var task = sslStream.AuthenticateAsServerAsync(cert, false, protocols, false);
        if (timeout > 0)
        {
            // 旧框架不支持取消耗时认证。超时后先关闭套接字中止挂起的握手读，再等待任务结束以观测其异常
            var completed = await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false);
            if (completed != task)
            {
                sock.Close();
                try { await task.ConfigureAwait(false); } catch { }

                throw new TimeoutException($"服务端SSL认证超时！[{timeout}ms]");
            }
        }

        await task.ConfigureAwait(false);
    }
#endif

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

                // 内核接收窗口决定大报文吞吐上限，默认不修改（0）
                var rcvBuf = SocketSetting.Current.ReceiveBufferSize;
                if (rcvBuf > 0) sock.ReceiveBufferSize = rcvBuf;
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
                // 超时预算 + 调用方取消：两者任一触发都中止握手（超时以 OperationCanceledException 呈现，与建连口径一致）
                using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                if (timeout > 0) source.CancelAfter(timeout);
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
                // 旧框架不支持取消耗时认证：超时/取消后先关闭套接字中止挂起的握手读，再等待任务结束以观测其异常
                var authTask = sslStream.AuthenticateAsClientAsync(host, certs, sp, false);
                if (timeout > 0 || cancellationToken.CanBeCanceled)
                {
                    var completed = await Task.WhenAny(authTask, Task.Delay(timeout > 0 ? timeout : System.Threading.Timeout.Infinite, cancellationToken)).ConfigureAwait(false);
                    if (completed != authTask)
                    {
                        sock.Close();
                        try { await authTask.ConfigureAwait(false); } catch { }

                        cancellationToken.ThrowIfCancellationRequested();

                        throw new TimeoutException($"客户端SSL认证超时！[{timeout}ms]");
                    }
                }

                await authTask.ConfigureAwait(false);
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
            // 客户端会话保留未释放（会话对象一次性，由调用方保管），因此“Active 为假、Disposed 尚未置位”是关闭流程的正常瞬态
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
                // 客户端会话不在此释放，由调用方保管（会话对象一次性，关闭后不可重新打开）。
                if (_Server != null) Dispose();

                return Task.FromResult(false);
            }
            Client = null;
        }

        return Task.FromResult(true);
    }

    #endregion 方法

    #region 关闭收尾
    /// <summary>关闭。重写以收尾流式收发：出站队列残余直接丢弃并归还缓冲（不排空），入站管道终止（挂起读立即完成）</summary>
    /// <param name="reason">关闭原因。便于日志分析</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>是否成功</returns>
    public override async Task<Boolean> CloseAsync(String reason, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.CloseAsync(reason, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // 仅在关闭真正发生（会话已不活动）时执行流式收尾：取消在入口被拦截（会话仍活动）时
            // 不得拆掉管道与队列——否则会话外显“还开着”却收不到数据。
            // 未打开的关闭（Active 亦为假）照常收尾，幂等复位上一轮资源
            if (!Active) CleanupStreamFlows();
        }
    }

    /// <summary>流式收尾：停发送泵并释放出站队列（残余丢弃）、终止入站管道、释放暂存接收槽</summary>
    private void CleanupStreamFlows()
    {
        // 出站队列：先停泵（结束写侧唤醒挂起读，并等泵退出后不再触碰队列缓冲），
        // 入队方的水位等待随之结束，最后释放队列归还残余池缓冲。
        // 不排空——停机场景对端往往已不可达，队列残余本就送不出去，逐会话限时排空会让总耗时随会话数线性放大。
        //
        // 取走队列与 getter 的双检锁互斥，避免关闭期间又新建出一个无人释放的队列与发送泵
        Pipe? queue;
        lock (_sendQueueLock)
        {
            queue = _sendQueue;
            _sendQueue = null;
        }

        if (queue != null)
        {
            queue.Resumed -= OnSendQueueResumed;

            StopSendPump(queue);

            // 唤醒仍在等水位的入队方：完成写侧不会触发 Resumed，不唤醒它们会永久挂起
            WakeQueueWaiters();

            queue.Dispose();
        }

        // 入站流：释放管道（等价于完成写侧唤醒挂起读 + 结束读侧归还残余池缓冲）。
        // 只完成写侧会让读侧链上尚未消费的池缓冲一直挂到管道随对象不可达，由终结器兜底归还；
        // 服务端会话还可能存活到 SessionTimeout，等于长期占用内存池。
        _pipe?.Dispose();

        // 复位入站管道，交由懒创建在再次访问时重建。客户端会话关闭后未释放（会话对象一次性，由调用方保管），
        // 若继续持有已完成管道，再次访问会拿到已结束的读取器，协议模式接收泵在其上读取即抛异常
        _pipe = null;

        ReleaseParkedReceive();
    }
    #endregion

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