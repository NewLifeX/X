using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using NewLife.Data;

namespace NewLife.Net;

/// <summary>增强TCP客户端（平台差异桥：本类全部 TFM 条件编译集中地）</summary>
/// <remarks>
/// <para>本文件只放“框架版本差异”的等价桥接，不含业务策略：连接桥（可取消建连与 Unix 域）、SSL 桥（客户端/服务端认证的超时降级）、
/// 写入桥（Span/数组差异、异步写降级、单段/链式路径）、文件桥（内核零拷贝）、等待原语桥与泵内标记桥。</para>
/// <para>维护约定：条件编译只允许出现在本文件；新增差异应在对应桥方法内整对添加（#if/#else 不得跨文件拆分），主逻辑保持无 #if。</para>
/// </remarks>
public partial class TcpSession
{
    #region 连接桥
    /// <summary>回填 Unix 域套接字路径（低版本框架无此终结点类型，空操作）</summary>
    /// <param name="socket">已连接的套接字</param>
    /// <param name="local">本地描述</param>
    /// <param name="remote">远程描述</param>
    private static void TryFillUnixEndPoints(Socket socket, NetUri local, NetUri remote)
    {
#if !NETFRAMEWORK && !NETSTANDARD2_0
        // Unix域套接字回填路径，便于日志显示
        if (socket.LocalEndPoint is UnixDomainSocketEndPoint localUds && !localUds.ToString().IsNullOrEmpty())
        {
            local.Type = NetType.Unix;
            local.Path = localUds.ToString();
        }
        if (socket.RemoteEndPoint is UnixDomainSocketEndPoint remoteUds && !remoteUds.ToString().IsNullOrEmpty())
        {
            remote.Type = NetType.Unix;
            remote.Path = remoteUds.ToString();
        }
#endif
    }

    /// <summary>创建 Unix 域套接字终结点。低版本框架不支持 Unix Domain Socket</summary>
    /// <param name="path">本地文件路径</param>
    /// <returns>远程终结点</returns>
    private static EndPoint CreateUnixEndPoint(String path)
    {
#if NETFRAMEWORK || NETSTANDARD2_0
        throw new PlatformNotSupportedException(L.T("Unix domain sockets require a target framework of .NET Standard 2.1 or later."));
#else
        return new UnixDomainSocketEndPoint(path);
#endif
    }

    /// <summary>异步连接Unix域套接字，支持超时</summary>
    /// <param name="sock">套接字</param>
    /// <param name="ep">远程终结点</param>
    /// <param name="timeout">超时时间（毫秒）</param>
    /// <param name="cancellationToken">取消通知</param>
    private static Task ConnectAsync(Socket sock, EndPoint ep, Int32 timeout, CancellationToken cancellationToken)
        => ConnectCoreAsync(sock, timeout, cancellationToken, null, 0, ep, ep);

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
    #endregion

    #region SSL 桥
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

                throw new TimeoutException(L.T("Server SSL authentication timed out after {0} ms.", timeout));
            }
        }

        await task.ConfigureAwait(false);
    }
#endif

    /// <summary>客户端SSL认证（异步），返回认证完成的SSL流</summary>
    /// <param name="sock">已连接的套接字</param>
    /// <param name="host">目标主机，用于 SNI 与证书校验</param>
    /// <param name="certs">客户端证书集合（服务端请求客户端证书时需要）</param>
    /// <param name="sp">协议版本</param>
    /// <param name="timeout">超时（毫秒），小于等于零表示不启用</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>认证完成的SSL流</returns>
    private async Task<SslStream> AuthenticateAsClientAsync(Socket sock, String host, X509CertificateCollection certs, SslProtocols sp, Int32 timeout, CancellationToken cancellationToken)
    {
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

                throw new TimeoutException(L.T("Client SSL authentication timed out after {0} ms.", timeout));
            }
        }

        await authTask.ConfigureAwait(false);
#endif

        return sslStream;
    }
    #endregion

    #region 写入桥
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

    /// <summary>发送一段内存，短计数自动续发。低版本框架无 Span 发送重载，降级为拷贝数组后发送</summary>
    /// <param name="sock">目标套接字</param>
    /// <param name="data">数据</param>
    /// <param name="deadline">续发总预算截止时间（Runtime.TickCount64 毫秒），0 不限制</param>
    /// <returns>已发送字节数，等于 data.Length</returns>
    private static Int32 SendAllMemory(Socket sock, ReadOnlySpan<Byte> data, Int64 deadline)
    {
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
        return SendAll(sock, data, deadline);
#else
        return SendAll(sock, data.ToArray(), 0, data.Length, deadline);
#endif
    }

    /// <summary>SSL 流写一块内存。低版本框架无 Span 写重载，降级为拷贝数组后写出</summary>
    /// <param name="stream">SSL流</param>
    /// <param name="data">数据</param>
    private static void WriteSsl(SslStream stream, ReadOnlySpan<Byte> data)
    {
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
        stream.Write(data);
#else
        stream.Write(data.ToArray());
#endif
    }

    /// <summary>发送单个数据包：单节点直写数据包自身缓冲（0 拷贝 0 分配），链式包散列写一次系统调用提交整条链</summary>
    /// <param name="sock">目标套接字</param>
    /// <param name="pk">数据包</param>
    /// <param name="count">总字节数</param>
    /// <param name="deadline">续发总预算截止时间（Runtime.TickCount64 毫秒），0 不限制</param>
    private static void SendPacketData(Socket sock, IPacket pk, Int32 count, Int64 deadline)
    {
        if (pk.Next == null && pk.TryGetArray(out var segment))
            SendAll(sock, segment.Array!, segment.Offset, segment.Count, deadline);
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
        else if (pk.Next == null)
            SendAllMemory(sock, pk.GetMemory().Span, deadline);
#endif
        else
            SendAll(sock, pk.ToSegments(), count, deadline);
    }

#if NET5_0_OR_GREATER
    /// <summary>异步发送的预算取消源。单写者复用；正常完成时解除计时，触发取消即超时失败</summary>
    private CancellationTokenSource? _sendCts;

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
    #endregion

    #region 文件桥
    /// <summary>内核零拷贝发送文件（NET6+ 且非 SSL 的 IP 套接字）。返回已发送字节数；不可用或未发出数据时返回 -1，由调用方降级</summary>
    /// <remarks>
    /// <para>非 SSL 会话：整个文件交给内核零拷贝推送，等待可写期间不占线程。仅 IP 套接字可用：Unix 域套接字上 sendfile/TransmitFile
    /// 既不会推进也不会完成（实测永久挂起：对端不读时 1KB 即挂，TCP 同条件 0ms 完成），必须排除，否则静态文件响应会一直挂着不返回。</para>
    /// <para>只有「根本没发出任何数据」的失败才可安全降级：平台/套接字类型不支持零拷贝、或路径在存在性检查后被删除。
    /// 网络中断、对端复位一类失败可能已送出部分文件，重发会让对端收到重复内容，必须交由调用方按发送失败处理。</para>
    /// </remarks>
    /// <param name="filePath">文件路径</param>
    /// <param name="length">文件长度</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已发送字节数；不可用或未发出数据返回 -1</returns>
    private async ValueTask<Int64> TrySendFileKernelAsync(String filePath, Int64 length, CancellationToken cancellationToken)
    {
#if NET6_0_OR_GREATER
        if (_Stream == null && Client is { AddressFamily: AddressFamily.InterNetwork or AddressFamily.InterNetworkV6 } sock)
        {
            try
            {
                // 内核 SendFileAsync(string) 无返回值（推完整个文件即成功），以文件长度作为发送量
                await sock.SendFileAsync(filePath, cancellationToken).ConfigureAwait(false);

                LastTime = DateTime.Now;

                return length;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // 调用方主动取消：不是发送故障，向上抛出
                throw;
            }
            catch (Exception ex) when (ex is NotSupportedException or FileNotFoundException)
            {
                // 只有「根本没发出任何数据」的失败才能安全降级：平台/套接字类型不支持零拷贝、或路径在存在性检查后被删除。
                // 网络中断、对端复位一类失败可能已送出部分文件，重发会让对端收到重复内容，必须交由外层按发送失败处理
                WriteLog("Kernel zero-copy is unavailable; falling back to chunked send for {0}: {1}", filePath, ex.Message);
            }
        }
#endif
        return -1;
    }
    #endregion

    #region 等待原语桥
    /// <summary>创建信号唤醒源。低版本框架无 RunContinuationsAsynchronously 选项</summary>
    /// <returns>信号源</returns>
    private static TaskCompletionSource<Boolean> CreateSignalSource()
    {
#if NET45
        return new TaskCompletionSource<Boolean>();
#else
        return new TaskCompletionSource<Boolean>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
    }

    /// <summary>等待信号任务，支持取消。取消即抛 OperationCanceledException（与高版本 Task.WaitAsync 口径一致）</summary>
    /// <remarks>低版本框架没有 Task.WaitAsync，手工用 WhenAny 附加取消；调用方对未启用取消的场景先行短路，避免多余等待源</remarks>
    /// <param name="task">信号任务</param>
    /// <param name="cancellationToken">取消令牌</param>
    private static async Task WaitSignalAsync(Task<Boolean> task, CancellationToken cancellationToken)
    {
#if NET6_0_OR_GREATER
        await task.WaitAsync(cancellationToken).ConfigureAwait(false);
#else
        var tcs = CreateSignalSource();
        using (cancellationToken.Register(() => tcs.TrySetResult(false)))
        {
            await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
#endif
    }
    #endregion

    #region 泵标记桥
    /// <summary>泵内标记（AsyncLocal）。net45 无 AsyncLocal，退回线程本地（与 TokenHttpFilter 同一降级口径）：异步续体切线程后标记可能丢失，此时退化为等到上限</summary>
    private sealed class PumpFlag
    {
#if NET45
        private readonly ThreadLocal<Boolean> _flag = new();
#else
        private readonly AsyncLocal<Boolean> _flag = new();
#endif

        /// <summary>标记值。标记者应处于消息泵异步流内</summary>
        public Boolean Value { get => _flag.Value; set => _flag.Value = value; }
    }
    #endregion
}
