using System.Buffers;
using System.Globalization;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Web;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Serialization;

namespace NewLife.Http;

/// <summary>迷你Http客户端。支持https和302跳转</summary>
/// <remarks>
/// 基于Tcp连接设计，用于高吞吐的HTTP通信场景，功能较少，但一切均在掌控之中。
/// 单个实例使用单个连接，建议外部使用ObjectPool建立连接池。
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

    /// <summary>超时时间。默认15s</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>缓冲区大小。接收缓冲区默认64*1024</summary>
    public Int32 BufferSize { get; set; } = 64 * 1024;

    /// <summary>单个分块长度上限，默认 64M。0 表示不限制</summary>
    public Int32 MaxChunkSize { get; set; } = 64 * 1024 * 1024;

    /// <summary>响应体总长上限，默认 1G。0 表示不限制</summary>
    public Int64 MaxBodySize { get; set; } = 1024L * 1024 * 1024;

    /// <summary>响应头最大长度，默认 64K。0 表示不限制</summary>
    /// <remarks>响应头可能跨接收块到达（服务端头部稍慢时首块只有半截头），头未完整时会累积读取；
    /// 超过上限视为无效响应，避免畸形数据无界累积。</remarks>
    public Int32 MaxHeadLength { get; set; } = 64 * 1024;

    /// <summary>Json序列化</summary>
    public IJsonHost JsonHost { get; set; } = JsonHelper.Default;

    /// <summary>是否忽略服务器证书校验。默认 false（严格校验）</summary>
    /// <remarks>
    /// <para>默认按系统信任链校验服务器证书，防范中间人攻击。仅在连接自签证书的测试/内网服务时才置为 true。</para>
    /// <para>该选项仅影响 https 连接，对 http 无影响。</para>
    /// </remarks>
    public Boolean IgnoreServerCertificate { get; set; }

    /// <summary>JSON序列化选项，影响复杂对象的编码和解码行为</summary>
    public JsonOptions? JsonOptions { get; set; }

    /// <summary>性能追踪</summary>
    public ITracer? Tracer { get; set; } = HttpHelper.Tracer;

    private Stream? _stream;
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
            await tc.ConnectAsync(remote.GetAddresses(), remote.Port).ConfigureAwait(false);

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

                // 默认严格校验服务器证书（防中间人）；仅显式开启 IgnoreServerCertificate 才放行任意证书。
                // 协议版本交给运行时协商（SslProtocols.None），不再固定 Tls12，以便用上 TLS1.3 与后续版本
                var sslStream = IgnoreServerCertificate
                    ? new SslStream(ns, false, (_, _, _, _) => true)
                    : new SslStream(ns, false);
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
        using var source = new CancellationTokenSource(Timeout);

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
        // 构造请求
        var uri = request.RequestUri ?? throw new ArgumentNullException(nameof(request.RequestUri));
        var req = request.Build();

        var res = new HttpResponse();
        IPacket? rs = null;
        var retry = 5;
        while (retry-- > 0)
        {
            // 发出请求
            using var rs2 = await SendDataAsync(uri, req).ConfigureAwait(false);
            if (rs2 == null || rs2.Length == 0) return null;

            // 解析响应。入参句柄由本层释放（主体等切片已取得独立引用）
            var parsed = res.Parse(rs2);

            // 响应头可能跨接收块：服务端头部稍慢时首个数据块只到达半截头，解析失败。
            // 继续读取并累积到连续缓冲，直到拼出完整头部；否则会把半截头当成空的成功响应返回给调用方（静默错误）。
            if (!parsed)
            {
                // 先清掉失败解析可能已设置的体切片（头部完整但首行非法时），避免重解析时泄漏
                res.Body.TryDispose();
                res.Body = null;

                var ms = new MemoryStream(BufferSize);
                rs2.CopyTo(ms);
                while (MaxHeadLength <= 0 || ms.Length < MaxHeadLength)
                {
                    using var more = await SendDataAsync(null, null).ConfigureAwait(false);
                    if (more == null || more.Length == 0) break;

                    more.CopyTo(ms);
                    if (res.Parse(new ArrayPacket(ms.GetBuffer(), 0, (Int32)ms.Length)))
                    {
                        parsed = true;
                        break;
                    }
                }
            }

            if (!parsed) return null;

            rs = res.Body;

            // 跳转。301/302/303/307/308 都是重定向，此前只认 301/302，其余会被当成成功响应原样返回
            // （调用方拿到重定向页面的 HTML 却以为成功）。
            // 注意 HttpStatusCode.PermanentRedirect（308）在 net45/netstandard2.0 不存在，故按状态码数值判断
            if ((Int32)res.StatusCode is 301 or 302 or 303 or 307 or 308)
            {
                if (res.Headers.TryGetValue("Location", out var location) && !location.IsNullOrEmpty())
                {
                    // 再次请求。Location 可能是相对路径（真实服务器常见），按当前请求 URI 解析；
                    // 直接 new Uri(location) 会在相对路径上抛 UriFormatException
                    var uri2 = Uri.TryCreate(location, UriKind.Absolute, out var abs) ? abs : new Uri(uri, location);

                    if (uri.Host != uri2.Host || uri.Scheme != uri2.Scheme) Client.TryDispose();

                    uri = uri2;
                    request.RequestUri = uri;

                    req?.Dispose();
                    req = request.Build();

                    // 跳转重试：上一响应作废，释放其主体句柄（共享切片独立持有引用，不释放会吊住接收缓冲）
                    // 同时清空 Body，避免下一轮解析失败时把已释放句柄返回给调用方
                    rs.TryDispose();
                    rs = null;
                    res.Body = null;

                    continue;
                }
            }

            break;
        }

        // 释放数据包，还给缓冲池
        req?.Dispose();

        // 4xx/5xx 才是错误响应；2xx 家族（含 201/204/206）均属正常成功
        if ((Int32)res.StatusCode >= 400)
        {
            // 错误路径不返回响应对象，必须在此释放响应体句柄（池化接收缓冲），否则永不归还
            var body = res.Body;
            res.Body = null;
            body.TryDispose();

            throw new Exception($"{(Int32)res.StatusCode} {res.StatusDescription}");
        }

        // 如果没有收完数据包
        if (rs != null && res.ContentLength > 0 && rs.Length < res.ContentLength)
        {
            // 使用内存流拼接需要多次接收的数据包，降低逻辑复杂度
            var ms = new MemoryStream(res.ContentLength);
            await rs.CopyToAsync(ms).ConfigureAwait(false);

            var total = rs.Length;
            while (total < res.ContentLength)
            {
                var pk = await SendDataAsync(null, null).ConfigureAwait(false);
                if (pk == null) break;

                pk.CopyTo(ms);
                var len = pk.Length;
                pk.TryDispose();

                total += len;
                if (len == 0) break;
            }

            // 从内存流获取缓冲区，打包为数据包返回，避免再次内存分配
            ms.Position = 0;
            rs.TryDispose();          // 旧句柄（部分体）已拷入 ms，归还池化缓冲
            rs = new ArrayPacket(ms);
            res.Body = rs;            // 写回响应：否则调用方通过 res.Body 拿到的仍是拼接前的旧（可能为空）句柄
        }

        // chunk编码
        if (rs != null && res.Headers.TryGetValue("Transfer-Encoding", out var s) && s.EqualIgnoreCase("chunked"))
        {
            // 如果不足则读取一个chunk，因为有可能第一个响应包只有头部
            if (rs.Length == 0)
            {
                rs.TryDispose();
                rs = await SendDataAsync(null, null).ConfigureAwait(false);
            }

            res.Body = await ReadChunkAsync(rs).ConfigureAwait(false);
            rs.TryDispose();    // 入参句柄由调用方释放（分片方法只借阅，不释放入参）
        }

        // 断开连接
        if (!KeepAlive) Client.TryDispose();

        return res;
    }

    /// <summary>读取分片，返回链式 IPacket</summary>
    /// <remarks>
    /// <para>只借阅入参，不释放 <paramref name="body"/>；入参句柄由调用方负责释放。</para>
    /// <para>分块长度行与分块数据都可能跨接收包：未解析字节累积在连续缓冲中，凑满一个完整分块才消费。
    /// 旧实现假定长度行与分块数据同在单个数据包内，长度行落到下一个包时会把后续所有分块静默丢弃（仍返回“成功”）。</para>
    /// <para>单块与总计长度分别由 <see cref="MaxChunkSize"/> 与 <see cref="MaxBodySize"/> 限制；未解析残留超限按协议异常报错。</para>
    /// <para>末块后的 trailers 段会被消费（内容忽略），避免残留字节让复用连接上的下一个响应错位。</para>
    /// </remarks>
    /// <param name="body">待解析的数据包（调用方负责释放）</param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException">分块长度行非法，或单块/总计长度超过上限</exception>
    protected virtual async Task<IPacket> ReadChunkAsync(IPacket body)
    {
        var result = new MemoryStream(BufferSize);

        // 未解析字节的连续缓冲；随解析推进收缩前缀，避免随分块数量无限增长
        var pending = new MemoryStream(BufferSize);
        var pos = 0;
        var total = 0L;
        var finished = false;
        var trailer = false;

        // 首个数据包为借阅（调用方释放），后续读取的包由本方法释放
        IPacket? input = body;
        var owned = false;
        try
        {
            while (!finished)
            {
                if (input != null && input.Length > 0) input.CopyTo(pending);
                if (owned) input?.TryDispose();
                input = null;

                var buf = pending.GetBuffer();
                var end = (Int32)pending.Length;

                // 尽可能多地解析完整分块
                while (true)
                {
                    var window = buf.AsSpan(pos, end - pos);

                    // 长度行需完整（含 CRLF），否则等下一批数据
                    var p = window.IndexOf(NewLine);
                    if (p <= 0) break;

                    if (!TryParseChunkLength(window[..p], out var len))
                        throw new InvalidDataException($"非法的分块长度行 [{window[..p].ToStr()}]");

                    if (MaxChunkSize > 0 && len > MaxChunkSize)
                        throw new InvalidDataException($"分块长度 {len} 超过上限 {MaxChunkSize}");

                    // 末块：长度为 0。其后是 trailers 段，以空行结束
                    if (len == 0)
                    {
                        pos += p + 2;
                        trailer = true;
                        break;
                    }

                    // 分块数据与尾随 CRLF 必须完整
                    if (window.Length < p + 2 + len + 2) break;

                    if (MaxBodySize > 0 && total + len > MaxBodySize)
                        throw new InvalidDataException($"响应体累计长度超过上限 {MaxBodySize}");

                    result.Write(buf, pos + p + 2, len);
                    total += len;
                    pos += p + 2 + len + 2;
                }

                // 末块后的 trailers 段必须一并消费：残留在连接上会让下一个响应错位
                if (trailer)
                {
                    var q = buf.AsSpan(pos, end - pos).IndexOf(NewLine);
                    if (q >= 0)
                    {
                        pos += q + 2;
                        finished = true;
                    }
                }

                // 收缩已解析前缀
                if (pos > 0)
                {
                    var rest = end - pos;
                    if (rest > 0) Array.Copy(buf, pos, buf, 0, rest);

                    pending.SetLength(rest);
                    pending.Position = rest;
                    pos = 0;
                }

                if (finished) break;

                // 长度行迟迟不完整（如对端持续发送不含 CRLF 的字节）时 pending 会无界增长，超限按协议异常处理
                if (MaxChunkSize > 0 && pending.Length > MaxChunkSize)
                    throw new InvalidDataException($"分块数据未按协议定界，已累积 {pending.Length} 字节");

                // 读取下一批数据；读不到即结束，不完整分块按截断处理（与原行为一致）
                var more = await SendDataAsync(null, null).ConfigureAwait(false);
                if (more == null || more.Length == 0)
                {
                    more?.TryDispose();
                    break;
                }

                input = more;
                owned = true;
            }
        }
        finally
        {
            pending.Dispose();
        }

        result.Position = 0;
        return new ArrayPacket(result);
    }
    #endregion

    #region 辅助
    private static readonly Byte[] NewLine = [(Byte)'\r', (Byte)'\n'];
    /// <summary>解析分块长度行。支持分块扩展（分号后内容忽略），非法字符返回 false</summary>
    /// <remarks>只做字符校验的十六进制解析：旧实现直接 Int32.Parse，分块扩展或干扰字符会抛 FormatException 穿透到调用方</remarks>
    /// <param name="data">长度行（不含 CRLF）</param>
    /// <param name="length">解析出的长度</param>
    /// <returns>是否解析成功</returns>
    private static Boolean TryParseChunkLength(ReadOnlySpan<Byte> data, out Int32 length)
    {
        length = 0;

        // 分块扩展（如 1ba;ext=1）只取分号前的十六进制长度
        var p = data.IndexOf((Byte)';');
        if (p >= 0) data = data[..p];
        if (data.IsEmpty || data.Length > 8) return false;

        var value = 0;
        foreach (var b in data)
        {
            var d = b switch
            {
                >= (Byte)'0' and <= (Byte)'9' => b - (Byte)'0',
                >= (Byte)'a' and <= (Byte)'f' => b - (Byte)'a' + 10,
                >= (Byte)'A' and <= (Byte)'F' => b - (Byte)'A' + 10,
                _ => -1,
            };
            if (d < 0) return false;

            value = (value << 4) + d;

            // 左移溢出为负：超 31 位直接拒收
            if (value < 0) return false;
        }

        length = value;
        return true;
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

    #region 远程调用
    /// <summary>异步调用，等待返回结果</summary>
    /// <typeparam name="TResult">返回类型</typeparam>
    /// <param name="method">Get/Post</param>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <returns></returns>
    public async Task<TResult?> InvokeAsync<TResult>(String method, String action, Object? args = null)
    {
        var baseAddress = BaseAddress ?? throw new ArgumentNullException(nameof(BaseAddress));
        var request = BuildRequest(baseAddress, method, action, args);

        using var rs = await SendAsync(request).ConfigureAwait(false);

        if (rs == null || rs.Body == null || rs.Body.Length == 0) return default;

        return ProcessResponse<TResult>(rs.Body);
    }

    private HttpRequest BuildRequest(Uri baseAddress, String method, String action, Object? args)
    {
        var req = new HttpRequest
        {
            Method = method.ToUpper(),
            RequestUri = new Uri(baseAddress, action),
            KeepAlive = KeepAlive,
        };

        if (args == null) return req;

        var ps = args.ToDictionary();
        if (method.EqualIgnoreCase("Post"))
            req.Body = (ArrayPacket)JsonHost.Write(ps, JsonOptions).GetBytes();
        else
        {
            var sb = Pool.StringBuilder.Get();
            sb.Append(action);
            sb.Append('?');

            var first = true;
            foreach (var item in ps)
            {
                if (!first) sb.Append('&');
                first = false;

                var v = item.Value is DateTime dt ? dt.ToFullString() : (item.Value + "");
                sb.AppendFormat("{0}={1}", item.Key, HttpUtility.UrlEncode(v));
            }

            req.RequestUri = new Uri(baseAddress, sb.Return(true));
        }

        return req;
    }

    private TResult? ProcessResponse<TResult>(IPacket rs)
    {
        var str = rs.ToStr();
        if (typeof(TResult).IsBaseType()) return str.ChangeType<TResult>();

        // 反序列化
        var obj = JsonHost.Parse(str);
        if (obj is TResult result) return result;

        var dic = obj as IDictionary<String, Object?>;
        if (dic == null || !dic.TryGetValue("data", out var data)) throw new InvalidDataException("Unrecognized response data");

        if (dic.TryGetValue("result", out var result2))
        {
            if (result2 is Boolean res && !res) throw new InvalidOperationException($"remote error: {data}");
        }
        else if (dic.TryGetValue("code", out var code))
        {
            if (code is Int32 cd && cd != 0) throw new ApiException(cd, data + "");
        }
        else
        {
            throw new InvalidDataException("Unrecognized response data");
        }

        if (data == null) return default;

        return JsonHost.Convert<TResult>(data);
    }
    #endregion

    #region 日志
    /// <summary>日志</summary>
    public ILog Log { get; set; } = Logger.Null;

    /// <summary>写日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object?[] args) => Log?.Info(format, args);
    #endregion
}