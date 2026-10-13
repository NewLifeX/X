using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using NewLife.Data;
using NewLife.Net;

namespace NewLife.Http;

/// <summary>迷你Http客户端。仅支持 HTTP/1.1 基础子集，用于内网高吞吐的接口调用与文件下载</summary>
/// <remarks>
/// <para>基于 Tcp 连接设计，单个实例使用单个连接，建议外部使用 ObjectPool 建立连接池。</para>
/// <para>支持：http/https（按系统信任链严格校验服务器证书）、KeepAlive、定长体与分块传输、整次请求超时、
/// 301/302/303/307/308 重定向、4xx/5xx 抛异常。</para>
/// <para>不支持（遇到即显式抛异常，不静默降级）：1xx 临时响应、非 chunked 的传输编码（含 gzip 等压缩，
/// 本端不解压）、重复或非法 Content-Length、压缩/Cookie/认证/代理/HTTP2/HTTP3、连接池、流式请求响应体、
/// 取消令牌、自动重试；RPC 调用请改用 ApiHttpClient。</para>
/// </remarks>
public class TinyHttpClient : DisposeBase
{
    #region 属性
    /// <summary>客户端</summary>
    public TcpClient? Client { get; set; }

    /// <summary>基础地址</summary>
    public Uri? BaseAddress { get; set; }

    /// <summary>保持连接</summary>
    public Boolean KeepAlive { get; set; }

    /// <summary>整次请求的超时时间。默认15s。重定向与响应补读共用同一份预算</summary>
    /// <remarks>直接调用 SendDataAsync 时按单次读取预算计。截止时刻存于实例字段，故同一实例不可并发发请求</remarks>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>缓冲区大小。接收缓冲区默认64*1024</summary>
    public Int32 BufferSize { get; set; } = 64 * 1024;

    /// <summary>响应头最大长度。响应头可跨接收块累积，超过上限视为无效响应，避免畸形数据无界累积</summary>
    private const Int32 MaxHeadLength = 64 * 1024;

    /// <summary>响应总长上限。避免畸形响应无界占用内存</summary>
    private const Int64 MaxBodySize = 1024L * 1024 * 1024;

    private Stream? _stream;

    /// <summary>本次请求的截止时刻（UTC）。由 <see cref="SendAsync"/> 入口设定，SendDataAsync 按剩余时间建取消令牌</summary>
    private DateTime _deadline;
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public TinyHttpClient() { }

    /// <summary>实例化</summary>
    /// <param name="server"></param>
    public TinyHttpClient(String server) => BaseAddress = new Uri(server);

    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        Client.TryDispose();
    }
    #endregion

    #region 核心方法
    /// <summary>获取网络数据流</summary>
    /// <param name="uri"></param>
    /// <returns></returns>
    protected virtual async Task<Stream> GetStreamAsync(Uri? uri)
    {
        var tc = Client;
        var ns = _stream;

        // 判断连接是否可用
        var active = false;
        try
        {
            active = ns != null && tc != null && tc.Connected && ns.CanWrite && ns.CanRead;
            if (active) return ns!;

            ns = tc?.GetStream();
            active = ns != null && tc != null && tc.Connected && ns.CanWrite && ns.CanRead;
        }
        catch { }

        // 如果连接不可用，则重新建立连接
        if (!active)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));

            var remote = new NetUri(NetType.Tcp, uri.Host, uri.Port);

            tc.TryDispose();
            tc = new TcpClient { ReceiveTimeout = (Int32)Timeout.TotalMilliseconds };

            // 建连同读取一样花整次请求的剩余预算。指向黑洞地址或被丢包的内网 VIP 时，操作系统默认 TCP 超时
            // （Windows 约 21 秒）远超 Timeout，调用方会被长时间卡住（健康检查/网关调用还会连锁超时）。
            // TcpClient 的 ConnectAsync 入参没有取消令牌，改由令牌触发时关闭客户端来中止连接
            var remain = GetRemain();
            using (var source = new CancellationTokenSource(remain))
            {
                try
                {
                    using (source.Token.Register(() => tc.Close()))
                    {
                        await tc.ConnectAsync(remote.GetAddresses(), remote.Port).ConfigureAwait(false);
                    }

                    // 令牌恰好落在握手完成之后：客户端已被回调关闭，按超时处理，不把已关闭的连接交给上层
                    if (source.IsCancellationRequested) throw new OperationCanceledException(source.Token);
                }
                catch (Exception ex) when (source.IsCancellationRequested)
                {
                    // 中止连接抛出的异常形态随运行时不同（SocketException / ObjectDisposedException），统一收敛为超时
                    throw new TimeoutException($"连接 {remote} 超时（{remain.TotalMilliseconds:0}ms）", ex);
                }
            }

            Client = tc;
            ns = tc.GetStream();

            if (BaseAddress == null) BaseAddress = new Uri(uri, "/");

            active = true;
        }

        // 支持SSL
        if (active)
        {
            if (uri != null && uri.Scheme.EqualIgnoreCase("https"))
            {
                if (ns == null) throw new InvalidOperationException(nameof(NetworkStream));

                // 按系统信任链严格校验服务器证书，防范中间人；协议版本交给运行时协商（SslProtocols.None），
                // 以便用上 TLS1.3 与后续版本
                var sslStream = new SslStream(ns, false);
                await sslStream.AuthenticateAsClientAsync(uri.Host, [], SslProtocols.None, false).ConfigureAwait(false);
                ns = sslStream;
            }

            _stream = ns;
        }

        return ns!;
    }

    /// <summary>异步请求</summary>
    /// <param name="uri"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    protected virtual async Task<IOwnerPacket> SendDataAsync(Uri? uri, IPacket? request)
    {
        var ns = await GetStreamAsync(uri).ConfigureAwait(false);

        // 发送
        if (request != null) await request.CopyToAsync(ns).ConfigureAwait(false);

        // 接收
        var pk = new OwnerPacket(BufferSize);

        // 只花剩余时间：若每轮读取都给完整 Timeout，服务端每隔 Timeout-1ms 滴漏一个字节，
        // 每轮读取都能在预算内拿到数据，调用方就被无限挂住（半截头补读循环同样每轮重新计时）
        using var source = new CancellationTokenSource(GetRemain());

#if NETCOREAPP || NETSTANDARD2_1
        var count = await ns.ReadAsync(pk.GetMemory(), source.Token).ConfigureAwait(false);
#else
        var count = await ns.ReadAsync(pk.Buffer, 0, pk.Length, source.Token).ConfigureAwait(false);
#endif

        return pk.Resize(count);
    }

    /// <summary>异步发出请求，并接收响应</summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public virtual async Task<HttpResponse?> SendAsync(HttpRequest request)
    {
        // 整次请求共用一个截止时刻：重定向、响应头累积、响应体补读都在同一份预算内完成，
        // 超时不会随“又读了一轮”而无限顺延
        _deadline = DateTime.UtcNow + Timeout;

        var uri = request.RequestUri ?? throw new ArgumentNullException(nameof(request.RequestUri));
        var req = request.Build();

        HttpResponse? res = null;
        var retry = 5;
        while (retry-- > 0)
        {
            res = await ReadResponseAsync(uri, req).ConfigureAwait(false);
            if (res == null) break;

            // 跳转。301/302/303/307/308 都是重定向，此前只认 301/302，其余会被当成成功响应原样返回
            // （调用方拿到重定向页面的 HTML 却以为成功）。
            // 注意 HttpStatusCode.PermanentRedirect（308）在 net45/netstandard2.0 不存在，故按状态码数值判断
            if ((Int32)res.StatusCode is 301 or 302 or 303 or 307 or 308 &&
                res.Headers.TryGetValue("Location", out var location) && !location.IsNullOrEmpty())
            {
                // 再次请求。Location 可能是绝对 URL、绝对路径（/b）或相对路径，以当前请求 URI 为基解析；
                // 不能先用 UriKind.Absolute 试解析：Unix 上绝对路径 "/b" 会被当作 file:///b 的绝对 URI，随后按 file 协议建连时报端口非法
                var uri2 = new Uri(uri, location);

                // 只支持 http(s) 重定向目标；其它协议（如 file）就地报错，避免带着非法端点继续建连
                if (uri2.Scheme is not ("http" or "https")) throw new NotSupportedException($"不支持重定向到 {uri2.Scheme} 协议：{location}");

                if (uri.Host != uri2.Host || uri.Scheme != uri2.Scheme) Client.TryDispose();

                uri = uri2;
                request.RequestUri = uri;

                // 上一响应作废，释放其主体句柄（共享切片独立持有引用，不释放会吊住接收缓冲）
                res.Body.TryDispose();
                res.Body = null;

                req?.Dispose();
                req = request.Build();

                continue;
            }

            break;
        }

        // 释放请求数据包，还给缓冲池
        req?.Dispose();

        if (res == null) return null;

        // 4xx/5xx 才是错误响应；2xx 家族（含 201/204/206）均属正常成功
        if ((Int32)res.StatusCode >= 400)
        {
            // 错误路径不返回响应对象，必须在此释放响应体句柄（池化接收缓冲），否则永不归还
            var body = res.Body;
            res.Body = null;
            body.TryDispose();

            throw new Exception($"{(Int32)res.StatusCode} {res.StatusDescription}");
        }

        // 断开连接
        if (!KeepAlive) Client.TryDispose();

        return res;
    }

    /// <summary>读取一条完整响应。响应头可跨接收块累积，实体按 Content-Length 或分块传输定界</summary>
    /// <remarks>
    /// <para>响应头累积、定长体补读、分块跨包共用一条循环，统一按“没凑齐就再读一次”推进。</para>
    /// <para>不支持的协议形态一律显式抛异常，不静默降级：1xx 临时响应、末位非 chunked 的传输编码、
    /// 重复或非法 Content-Length、响应超过内部上限、对端提前关闭导致实体不完整。</para>
    /// </remarks>
    /// <param name="uri">请求地址，连接不可用时用于重新建连</param>
    /// <param name="request">待发送的请求数据。为 null 表示只读取不发送</param>
    /// <returns>响应；对端未给出任何数据时返回 null</returns>
    private async Task<HttpResponse?> ReadResponseAsync(Uri uri, IPacket? request)
    {
        // 累积缓冲：响应头与实体统一在其中推进，不再有三段各自重试的逻辑
        var acc = new MemoryStream(BufferSize);
        var res = new HttpResponse();
        var parsed = false;
        var chunked = false;
        var bodyStart = 0;

        try
        {
            while (true)
            {
                using var pk = await SendDataAsync(request == null ? null : uri, request).ConfigureAwait(false);
                request = null;     // 请求只发一次，后续轮次只继续读同一连接

                if (pk == null || pk.Length == 0)
                {
                    if (!parsed)
                    {
                        if (acc.Length == 0) return null;

                        throw new InvalidDataException($"响应头无法解析：已累积 {acc.Length} 字节仍未成头");
                    }

                    throw new InvalidDataException(chunked ? "分块传输未收到终止块" : "响应体不完整：对端提前关闭");
                }

                pk.CopyTo(acc);

                if (acc.Length > MaxHeadLength + MaxBodySize) throw new InvalidDataException($"响应超过上限 {MaxBodySize}");

                // 每轮重新解析：MemoryStream 扩容后，上一轮取到的主体切片指向的是已被替换的旧缓冲区
                if (!res.Parse(new ArrayPacket(acc.GetBuffer(), 0, (Int32)acc.Length)))
                {
                    if (acc.Length > MaxHeadLength) throw new InvalidDataException($"响应头无法解析：已累积 {acc.Length} 字节仍未成头");

                    continue;
                }

                if (!parsed)
                {
                    var code = (Int32)res.StatusCode;
                    if (code is >= 100 and < 200) throw new NotSupportedException($"不支持 1xx 临时响应：{code}");
                    if (res.InvalidContentLength) throw new InvalidDataException($"响应包含重复或非法的 Content-Length：{res.Headers["Content-Length"]}");

                    // 分块编码优先于 Content-Length（RFC 9112 §6.1）：实体长度由分块解码决定，CL 视为未知
                    if (res.Headers.TryGetValue("Transfer-Encoding", out var te) && !te.IsNullOrEmpty())
                    {
                        // 传输编码只认 chunked（RFC 9112 §6.1：chunked 必须位于末位）。含其余编码（gzip 等）时
                        // 本端无法完整还原实体，原样交出会让调用方拿到未解压却以为成功的字节流，故显式拒绝
                        if (!te.EqualIgnoreCase("chunked")) throw new NotSupportedException($"不支持的传输编码：{te}");

                        chunked = true;
                    }

                    // 实体起始位置 = 已消费长度 - 本轮主体切片长度
                    bodyStart = res.ConsumedLength - res.BodyLength;
                    parsed = true;

                    // 跳转不必等齐实体，交给上层重发
                    if (code is 301 or 302 or 303 or 307 or 308) return res;

                    // 无长度声明：读到多少算多少，兼容以连接关闭定界的老式响应
                    if (!chunked && res.ContentLength < 0) return res;
                }

                if (chunked)
                {
                    res.ContentLength = -1;

                    var status = ChunkedDecoder.TryDecode(acc.GetBuffer(), bodyStart, (Int32)acc.Length, out var body, out _);
                    if (status == ChunkedDecoder.ChunkedStatus.Invalid) throw new InvalidDataException("非法的分块传输编码");
                    if (status == ChunkedDecoder.ChunkedStatus.Complete)
                    {
                        // 解码结果独立于累积缓冲，替换掉原始分块帧的切片
                        res.Body.TryDispose();
                        res.Body = body;

                        return res;
                    }
                }
                else if (res.IsCompleted) return res;
            }
        }
        catch
        {
            // 协议错误后，连接上剩余的字节无法定位下一条消息边界，留着会让后续请求读到错位数据
            Client.TryDispose();

            throw;
        }
    }
    #endregion

    #region 辅助
    /// <summary>剩余时间预算。按 <see cref="SendAsync"/> 设定的整次截止时刻推算，未设定时按单次 <see cref="Timeout"/> 计</summary>
    private TimeSpan GetRemain()
    {
        var remain = _deadline > DateTime.MinValue ? _deadline - DateTime.UtcNow : Timeout;

        return remain > TimeSpan.Zero ? remain : TimeSpan.Zero;
    }
    #endregion

    #region 主要方法
    /// <summary>异步获取。连接池操作</summary>
    /// <param name="url">地址</param>
    /// <returns></returns>
    public async Task<String?> GetStringAsync(String url)
    {
        var request = new HttpRequest
        {
            RequestUri = new Uri(url),
        };

        using var rs = (await SendAsync(request).ConfigureAwait(false));
        return rs?.Body?.ToStr();
    }
    #endregion

}
