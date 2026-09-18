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

                await ConnectUnixAsync(sock, ep, timeout, cancellationToken).ConfigureAwait(false);
#endif
            }
            else
            {
                var addrs = uri.GetAddresses();
                addrs = addrs.Where(ip => ip.AddressFamily == sock.AddressFamily).ToArray();
                span?.AppendTag($"addrs={addrs.Join()} port={uri.Port}");

                if (timeout <= 0)
                    sock.Connect(addrs, uri.Port);
                else
                {
#if NET5_0_OR_GREATER
                    using var source = new CancellationTokenSource(timeout);
                    using var cts2 = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, source.Token);
                    using var _ = cts2.Token.Register(() => sock.Close());
                    await sock.ConnectAsync(addrs, uri.Port, cts2.Token).ConfigureAwait(false);
#else
                    // 采用异步来解决连接超时设置问题
                    var ar = sock.BeginConnect(addrs, uri.Port, null, null);
                    if (!ar.AsyncWaitHandle.WaitOne(timeout, true))
                    {
                        sock.Close();
                        throw new TimeoutException($"The connection to server [{uri}] timed out! [{timeout}ms]");
                    }

                    //sock.EndConnect(ar);
                    await Task.Factory.FromAsync(ar, sock.EndConnect).ConfigureAwait(false);
#endif
                }
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

#if !NETFRAMEWORK && !NETSTANDARD2_0
    /// <summary>异步连接Unix域套接字，支持超时</summary>
    /// <param name="sock">套接字</param>
    /// <param name="ep">远程终结点</param>
    /// <param name="timeout">超时时间（毫秒）</param>
    /// <param name="cancellationToken">取消通知</param>
    private static async Task ConnectUnixAsync(Socket sock, EndPoint ep, Int32 timeout, CancellationToken cancellationToken)
    {
        if (timeout <= 0)
        {
            sock.Connect(ep);
            return;
        }

#if NET5_0_OR_GREATER
        using var source = new CancellationTokenSource(timeout);
        using var cts2 = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, source.Token);
        using var _ = cts2.Token.Register(() => sock.Close());
        await sock.ConnectAsync(ep, cts2.Token).ConfigureAwait(false);
#else
        // 采用异步来解决连接超时设置问题
        var ar = sock.BeginConnect(ep, null, null);
        if (!ar.AsyncWaitHandle.WaitOne(timeout, true))
        {
            sock.Close();
            throw new TimeoutException($"The connection to server [{ep}] timed out! [{timeout}ms]");
        }

        await Task.Factory.FromAsync(ar, sock.EndConnect).ConfigureAwait(false);
#endif
    }
#endif

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

                return Task.FromResult(false);
            }
            Client = null;
        }

        return Task.FromResult(true);
    }

    #endregion 方法

    #region 发送

    private Int32 _bsize;
    private SpinLock _spinLock = new();

    /// <summary>直接发送数据。无发送队列时走此路径；发送泵的发送委托同样指向本方法</summary>
    /// <remarks>
    /// 目标地址由<seealso cref="SessionBase.Remote"/>决定
    /// </remarks>
    /// <param name="pk">数据包</param>
    /// <returns>已发送字节数；失败返回 -1</returns>
    private Int32 DirectSend(IPacket pk)
    {
        var count = pk.Total;

        if (Log != null && Log.Enable && LogSend) WriteLog("Send [{0}]: {1}", count, pk.ToHex(LogDataLength));

        using var span = Tracer?.NewSpan($"net:{Name}:Send", count + "", count);

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

            if (_Stream is not { } stream)
            {
                // 同步 Send 在接收方窗口受限时可能只发出一部分，续发循环保证整包送出；总预算按会话 Timeout 计时
                if (count == 0)
                    rs = sock.Send(Pool.Empty);
                else if (pk.Next == null && pk.TryGetArray(out var segment))
                    rs = SendAll(sock, segment.Array!, segment.Offset, segment.Count, GetSendDeadline());
#if NETCOREAPP || NETSTANDARD2_1
                else if (pk.TryGetSpan(out var data))
                    rs = SendAll(sock, data, GetSendDeadline());
#endif
                else
                    rs = SendAll(sock, pk.ToSegments(), count, GetSendDeadline());
            }
            else
            {
                // SSL 流内部已处理部分写：整段写完才返回，失败直接抛异常，无需续发循环
                if (count == 0)
                    stream.Write([]);
                else
                    pk.CopyTo(stream);
            }
        }
        catch (Exception ex)
        {
            // 发生异常时，全量数据写入埋点
            span?.SetError(ex, pk);

            if (!ex.IsDisposed())
            {
                OnError("Send", ex);

                // 发送异常可能是连接出了问题，需要关闭
                Close("SendError");
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

    /// <summary>直接发送数据。无发送队列时走此路径</summary>
    /// <remarks>
    /// 目标地址由<seealso cref="SessionBase.Remote"/>决定
    /// </remarks>
    /// <param name="data">数据包</param>
    /// <returns>已发送字节数；失败返回 -1</returns>
    private Int32 DirectSend(ArraySegment<Byte> data)
    {
        var count = data.Count;
        var logCount = count > LogDataLength ? count : LogDataLength;

        if (Log != null && Log.Enable && LogSend)
            WriteLog("Send [{0}]: {1}", count, data.Array.ToHex(data.Offset, logCount));

        using var span = Tracer?.NewSpan($"net:{Name}:Send", count + "", count);

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

            if (_Stream is not { } stream)
            {
                // 同步 Send 在接收方窗口受限时可能只发出一部分，续发循环保证整段送出；总预算按会话 Timeout 计时
                if (count == 0)
                    rs = sock.Send(Pool.Empty);
                else
                    rs = SendAll(sock, data.Array!, data.Offset, data.Count, GetSendDeadline());
            }
            else
            {
                // SSL 流内部已处理部分写：整段写完才返回，失败直接抛异常，无需续发循环
                if (count == 0)
                    stream.Write([]);
                else
                    stream.Write(data.Array!, data.Offset, data.Count);
            }
        }
        catch (Exception ex)
        {
            // 发生异常时，全量数据写入埋点
            span?.SetError(ex, data.Array.ToHex(data.Offset, data.Count));

            if (!ex.IsDisposed())
            {
                OnError("Send", ex);

                // 发送异常可能是连接出了问题，需要关闭
                Close("SendError");
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

    /// <summary>直接发送数据。无发送队列时走此路径</summary>
    /// <remarks>
    /// 目标地址由<seealso cref="SessionBase.Remote"/>决定
    /// </remarks>
    /// <param name="data">数据包</param>
    /// <returns>已发送字节数；失败返回 -1</returns>
    private Int32 DirectSend(ReadOnlySpan<Byte> data)
    {
        var count = data.Length;

        if (Log != null && Log.Enable && LogSend) WriteLog("Send [{0}]: {1}", count, data.ToHex(LogDataLength));

        using var span = Tracer?.NewSpan($"net:{Name}:Send", count + "", count);

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

            if (_Stream is not { } stream)
            {
                // 同步 Send 在接收方窗口受限时可能只发出一部分，续发循环保证整段送出；总预算按会话 Timeout 计时
                if (count == 0)
                    rs = sock.Send(Pool.Empty);
                else
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
                    rs = SendAll(sock, data, GetSendDeadline());
#else
                    rs = SendAll(sock, data.ToArray(), 0, count, GetSendDeadline());
#endif
            }
            else
            {
                // SSL 流内部已处理部分写：整段写完才返回，失败直接抛异常，无需续发循环
                if (count == 0)
                    stream.Write([]);
                else
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
                    stream.Write(data);
#else
                    stream.Write(data.ToArray());
#endif
            }
        }
        catch (Exception ex)
        {
            // 发生异常时，全量数据写入埋点
            span?.SetError(ex, data.ToHex());

            if (!ex.IsDisposed())
            {
                OnError("Send", ex);

                // 发送异常可能是连接出了问题，需要关闭
                Close("SendError");
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

    /// <summary>获取续发总预算的截止时间。返回 0 表示不限制（Timeout 未启用）</summary>
    private Int64 GetSendDeadline() => Timeout > 0 ? Runtime.TickCount64 + Timeout : 0;

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
            // 跳过已发段
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
    /// <summary>发送泵异步发送的预算取消源。泵为单消费者，复用；正常完成时解除计时，触发取消即超时失败</summary>
    private CancellationTokenSource? _sendCts;
#endif

#if NET5_0_OR_GREATER
    /// <summary>异步发送一段数据（发送泵专用），短计数自动续发；等待可写期间不占用线程</summary>
    /// <remarks>
    /// <para>与同步直发一致：发送失败记录错误并关闭会话，返回 -1；预算按会话 Timeout 计时，超时取消发送并按失败处理。</para>
    /// <para>SSL 流内部处理部分写，整段写完才返回。仅由发送泵单消费者调用。</para>
    /// </remarks>
    /// <param name="data">数据</param>
    /// <returns>已发送字节数；失败返回 -1</returns>
    private async ValueTask<Int32> DirectSendAsync(ReadOnlyMemory<Byte> data)
    {
        var count = data.Length;
        var sock = Client;
        if (sock == null) return -1;
        if (count == 0) return 0;

        using var span = Tracer?.NewSpan($"net:{Name}:Send", count + "", count);

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
                    await stream.WriteAsync(data, cts.Token).ConfigureAwait(false);
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
            // 预算超时：与同步发送超时一致，按发送失败处理
            var ex = new TimeoutException($"Send timeout, {total}/{count} bytes sent");
            span?.SetError(ex, null);
            OnError("Send", ex);
            Close("SendError");

            return -1;
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);

            if (!ex.IsDisposed())
            {
                OnError("Send", ex);

                // 发送异常可能是连接出了问题，需要关闭
                Close("SendError");
            }

            return -1;
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
    /// <summary>异步发送一段数据（发送泵专用）。当前目标框架无带取消令牌的 Socket.SendAsync 重载，降级为同步续发发送</summary>
    /// <param name="data">数据</param>
    /// <returns>已发送字节数；失败返回 -1</returns>
    private ValueTask<Int32> DirectSendAsync(ReadOnlyMemory<Byte> data) => new(DirectSend(data.Span));
#endif
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