using System.Globalization;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Http.Headers;
using NewLife.Log;
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Serialization;

namespace NewLife.Http;

/// <summary>迷你Http客户端。支持https和302跳转</summary>
public class TinyHttpClient : DisposeBase, IApiClient
{
    #region 属性
    /// <summary>客户端</summary>
    public TcpClient Client { get; set; }

    /// <summary>基础地址</summary>
    public Uri BaseAddress { get; set; }

    /// <summary>默认请求头</summary>
    public HttpRequestHeaders DefaultRequestHeaders { get; } = new();

    /// <summary>保持连接</summary>
    public Boolean KeepAlive { get; set; }

    /// <summary>超时时间。默认15s</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>缓冲区大小。接收缓冲区默认64*1024</summary>
    public Int32 BufferSize { get; set; } = 64 * 1024;

    /// <summary>令牌</summary>
    public String Token { get; set; }

    /// <summary>性能跟踪器</summary>
    public ITracer Tracer { get; set; } = DefaultTracer.Instance;

    private Stream _stream;
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

        //Client.TryDispose();
        Close();
    }
    #endregion

    #region 异步核心方法
    /// <summary>获取网络数据流</summary>
    /// <param name="uri"></param>
    /// <returns></returns>
    protected virtual async Task<Stream> GetStreamAsync(Uri uri)
    {
        var tc = Client;
        var ns = _stream;

        // 判断连接是否可用
        var active = false;
        try
        {
            active = tc != null && tc.Connected && ns != null && ns.CanWrite && ns.CanRead;
            if (active) return ns;

            ns = tc?.GetStream();
            active = tc != null && tc.Connected && ns != null && ns.CanWrite && ns.CanRead;
        }
        catch { }

        // 如果连接不可用，则重新建立连接
        if (!active)
        {
            var remote = new NetUri(NetType.Tcp, uri.Host, uri.Port);

            tc.TryDispose();
            tc = new TcpClient { ReceiveTimeout = (Int32)Timeout.TotalMilliseconds };
            await Task.Factory.FromAsync(tc.BeginConnect, tc.EndConnect, remote.GetAddresses(), remote.Port, null);

            Client = tc;
            ns = tc.GetStream();

            if (BaseAddress == null) BaseAddress = new Uri(uri, "/");

            active = true;
        }

        // 支持SSL
        if (active)
        {
            if (uri.Scheme.EqualIgnoreCase("https"))
            {
                var sslStream = new SslStream(ns, false, (sender, certificate, chain, sslPolicyErrors) => true);
                sslStream.AuthenticateAsClient(uri.Host, new X509CertificateCollection(), SslProtocols.Tls, false);
                ns = sslStream;
            }

            _stream = ns;
        }

        return ns;
    }

    /// <summary>异步请求</summary>
    /// <param name="uri"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    protected virtual async Task<Packet> SendDataAsync(Uri uri, Packet request)
    {
        var ns = await GetStreamAsync(uri).ConfigureAwait(false);

        // 发送
        if (request != null) await request.CopyToAsync(ns).ConfigureAwait(false);

        // 接收
        var buf = new Byte[BufferSize];
        var count = ns.Read(buf, 0, buf.Length);

        return new Packet(buf, 0, count);
    }

    /// <summary>异步发出请求，并接收响应</summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public virtual async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        // 构造请求
        var uri = request.RequestUri;
        if (!uri.IsAbsoluteUri) uri = request.RequestUri = new Uri(BaseAddress, uri);
        var req = request.Build();

        using var span = Tracer?.NewSpan(request);
        try
        {
            var res = new HttpResponseMessage();
            Packet rs = null;
            var retry = 5;
            while (retry-- > 0)
            {
                // 发出请求
                rs = await SendDataAsync(uri, req).ConfigureAwait(false);
                if (rs == null || rs.Count == 0) return null;

                // 解析响应
                if (!res.Parse(rs)) throw new HttpParseException();
                rs = res.Body;

                // 跳转
                if (res.StatusCode is HttpStatusCode.Moved or HttpStatusCode.Redirect)
                {
                    if (res.TryGetValue("Location", out var location) && !location.IsNullOrEmpty())
                    {
                        // 再次请求
                        var uri2 = new Uri(location);

                        if (uri.Host != uri2.Host || uri.Scheme != uri2.Scheme) Close();

                        uri = uri2;
                        request.RequestUri = uri;
                        req = request.Build();

                        continue;
                    }
                }

                break;
            }

            if (res.StatusCode != HttpStatusCode.OK) throw new Exception($"{(Int32)res.StatusCode} {res.StatusDescription}");

            // 如果没有收完数据包
            if (res.ContentLength > 0 && rs.Count < res.ContentLength)
            {
                var total = rs.Total;
                var last = rs;
                while (total < res.ContentLength)
                {
                    var pk = await SendDataAsync(null, null).ConfigureAwait(false);
                    last.Append(pk);

                    last = pk;
                    total += pk.Total;
                }
            }

            // chunk编码
            if (rs.Count > 0 && res.TryGetValue("Transfer-Encoding", out var s) && s.EqualIgnoreCase("chunked"))
            {
                res.Body = await ReadChunkAsync(rs);
            }

            res.SetContent();

            // 断开连接
            if (!KeepAlive) Close();

            return res;
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    /// <summary>读取分片，返回链式Packet</summary>
    /// <param name="body"></param>
    /// <returns></returns>
    protected virtual async Task<Packet> ReadChunkAsync(Packet body)
    {
        var rs = body;
        var last = body;
        var pk = body;
        while (true)
        {
            // 分析一个片段，如果该片段数据不足，则需要多次读取
            var chunk = ParseChunk(pk, out var offset, out var len);
            if (len <= 0) break;

            // 更新pk，可能还有粘包数据。每一帧数据后面有\r\n
            var next = offset + len + 2;
            if (next < pk.Total)
                pk = pk.Slice(next);
            else
                pk = null;

            // 第一个包需要替换，因为偏移量改变
            if (last == body)
                rs = chunk;
            else
                last.Append(chunk);

            last = chunk;

            // 如果该片段数据不足，则需要多次读取
            var total = chunk.Total;
            while (total < len || pk == null)
            {
                pk = await SendDataAsync(null, null).ConfigureAwait(false);

                // 结尾的间断符号（如换行或00）。这里有可能一个数据包里面同时返回多个分片，暂时不支持
                if (total + pk.Total > len) pk = pk.Slice(0, len - total);

                last.Append(pk);
                last = pk;
                total += pk.Total;
            }

            // 还有粘包数据，继续分析
            if (pk == null || pk.Total == 0) break;
            if (pk.Total > 0) continue;

            // 读取新的数据片段，如果不存在则跳出
            pk = await SendDataAsync(null, null).ConfigureAwait(false);
            if (pk == null || pk.Total == 0) break;
        }

        return rs;
    }
    #endregion

    #region 同步核心方法
    /// <summary>获取网络数据流</summary>
    /// <param name="uri"></param>
    /// <returns></returns>
    protected virtual Stream GetStream(Uri uri)
    {
        var tc = Client;
        var ns = _stream;

        // 判断连接是否可用
        var active = false;
        try
        {
            active = tc != null && tc.Connected && ns != null && ns.CanWrite && ns.CanRead;
            if (active) return ns;

            ns = tc?.GetStream();
            active = tc != null && tc.Connected && ns != null && ns.CanWrite && ns.CanRead;
        }
        catch { }

        // 如果连接不可用，则重新建立连接
        if (!active)
        {
            var remote = new NetUri(NetType.Tcp, uri.Host, uri.Port);

            var ms = (Int32)Timeout.TotalMilliseconds;
            tc.TryDispose();
            tc = new TcpClient { SendTimeout = ms, ReceiveTimeout = ms };
            tc.Connect(remote.GetAddresses(), remote.Port);

            Client = tc;
            ns = tc.GetStream();

            if (BaseAddress == null) BaseAddress = new Uri(uri, "/");

            active = true;
        }

        // 支持SSL
        if (active)
        {
            if (uri.Scheme.EqualIgnoreCase("https"))
            {
                var sslStream = new SslStream(ns, false, (sender, certificate, chain, sslPolicyErrors) => true);
                sslStream.AuthenticateAsClient(uri.Host, new X509CertificateCollection(), SslProtocols.Tls, false);
                ns = sslStream;
            }

            _stream = ns;
        }

        return ns;
    }

    /// <summary>异步请求</summary>
    /// <param name="uri"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    protected virtual Packet SendData(Uri uri, Packet request)
    {
        var ns = GetStream(uri);

        // 发送
        request?.CopyTo(ns);

        // 接收
        var buf = new Byte[BufferSize];
        var count = ns.Read(buf, 0, buf.Length);

        return new Packet(buf, 0, count);
    }

    /// <summary>异步发出请求，并接收响应</summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public virtual HttpResponseMessage Send(HttpRequestMessage request)
    {
        // 构造请求
        var uri = request.RequestUri;
        if (!uri.IsAbsoluteUri) uri = request.RequestUri = new Uri(BaseAddress, uri);
        var req = request.Build();

        using var span = Tracer?.NewSpan(request);
        try
        {
            var res = new HttpResponseMessage();
            Packet rs = null;
            var retry = 5;
            while (retry-- > 0)
            {
                // 发出请求
                rs = SendData(uri, req);
                if (rs == null || rs.Count == 0) return null;

                // 解析响应
                if (!res.Parse(rs)) throw new HttpParseException();
                rs = res.Body;

                // 跳转
                if (res.StatusCode is HttpStatusCode.Moved or HttpStatusCode.Redirect)
                {
                    if (res.TryGetValue("Location", out var location) && !location.IsNullOrEmpty())
                    {
                        // 再次请求
                        var uri2 = new Uri(location);

                        if (uri.Host != uri2.Host || uri.Scheme != uri2.Scheme) Close();

                        // 重建请求头，因为Uri改变后，头部字段也可能改变
                        uri = uri2;
                        //req = BuildRequest(uri, data);
                        request.RequestUri = uri;
                        req = request.Build();

                        continue;
                    }
                }

                break;
            }

            if (res.StatusCode != HttpStatusCode.OK) throw new Exception($"{(Int32)res.StatusCode} {res.StatusDescription}");

            // 如果没有收完数据包
            if (res.ContentLength > 0 && rs.Count < res.ContentLength)
            {
                var total = rs.Total;
                var last = rs;
                while (total < res.ContentLength)
                {
                    var pk = SendData(null, null);
                    last.Append(pk);

                    last = pk;
                    total += pk.Total;
                }
            }

            // chunk编码
            if (rs.Count > 0 && res.TryGetValue("Transfer-Encoding", out var s) && s.EqualIgnoreCase("chunked"))
            {
                res.Body = ReadChunk(rs);
            }

            res.SetContent();

            // 断开连接
            if (!KeepAlive) Close();

            return res;
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    /// <summary>读取分片，返回链式Packet</summary>
    /// <param name="body"></param>
    /// <returns></returns>
    protected virtual Packet ReadChunk(Packet body)
    {
        var rs = body;
        var last = body;
        var pk = body;
        while (true)
        {
            // 分析一个片段，如果该片段数据不足，则需要多次读取
            var chunk = ParseChunk(pk, out var offset, out var len);
            if (len <= 0) break;

            // 更新pk，可能还有粘包数据。每一帧数据后面有\r\n
            var next = offset + len + 2;
            if (next < pk.Total)
                pk = pk.Slice(next);
            else
                pk = null;

            // 第一个包需要替换，因为偏移量改变
            if (last == body)
                rs = chunk;
            else
                last.Append(chunk);

            last = chunk;

            // 如果该片段数据不足，则需要多次读取
            var total = chunk.Total;
            while (total < len)
            {
                pk = SendData(null, null);

                // 结尾的间断符号（如换行或00）。这里有可能一个数据包里面同时返回多个分片，暂时不支持
                if (total + pk.Total > len) pk = pk.Slice(0, len - total);

                last.Append(pk);
                last = pk;
                total += pk.Total;
            }

            // 还有粘包数据，继续分析
            if (pk == null || pk.Total == 0) break;
            if (pk.Total > 0) continue;

            // 读取新的数据片段，如果不存在则跳出
            pk = SendData(null, null);
            if (pk == null || pk.Total == 0) break;
        }

        return rs;
    }
    #endregion

    #region 辅助
    void Close()
    {
        var tc = Client;
        Client = null;
        tc.TryDispose();
    }

    ///// <summary>构造请求头</summary>
    ///// <param name="uri"></param>
    ///// <param name="data"></param>
    ///// <returns></returns>
    //protected virtual Packet BuildRequest(Uri uri, Packet data)
    //{
    //    // GET / HTTP/1.1

    //    var hasData = data != null && data.Total > 0;
    //    var method = hasData ? "POST" : "GET";

    //    var header = Pool.StringBuilder.Get();
    //    header.AppendLine($"{method} {uri.PathAndQuery} HTTP/1.1");
    //    header.AppendLine($"Host: {uri.Host}");

    //    if (!ContentType.IsNullOrEmpty()) header.AppendLine($"Content-Type: {ContentType}");

    //    // 主体数据长度
    //    if (hasData) header.AppendLine($"Content-Length: {data.Total}");

    //    // 保持连接
    //    if (KeepAlive) header.AppendLine("Connection: keep-alive");

    //    header.Append("\r\n");

    //    var req = new Packet(header.Put(true).GetBytes());

    //    // 请求主体数据
    //    if (hasData) req.Next = data;

    //    return req;
    //}

    //private static readonly Byte[] NewLine4 = new[] { (Byte)'\r', (Byte)'\n', (Byte)'\r', (Byte)'\n' };
    //private static readonly Byte[] NewLine3 = new[] { (Byte)'\r', (Byte)'\n', (Byte)'\n' };
    ///// <summary>解析响应</summary>
    ///// <param name="rs"></param>
    ///// <returns></returns>
    //protected virtual Packet ParseResponse(Packet rs)
    //{
    //    var p = rs.IndexOf(NewLine4);
    //    // 兼容某些非法响应
    //    if (p < 0) p = rs.IndexOf(NewLine3);
    //    if (p < 0) return null;

    //    var str = rs.ToStr(Encoding.ASCII, 0, p);
    //    var lines = str.Split("\r\n");

    //    // HTTP/1.1 502 Bad Gateway

    //    var ss = lines[0].Split(" ");
    //    if (ss.Length < 3) return null;

    //    // 分析响应码
    //    var code = StatusCode = ss[1].ToInt();
    //    StatusDescription = ss.Skip(2).Join(" ");
    //    //if (code == 302) return null;
    //    if (code >= 400) throw new Exception($"{code} {StatusDescription}");

    //    // 分析头部
    //    var hs = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
    //    foreach (var item in lines)
    //    {
    //        var p2 = item.IndexOf(':');
    //        if (p2 <= 0) continue;

    //        var key = item.Substring(0, p2);
    //        var value = item.Substring(p2 + 1).Trim();

    //        hs[key] = value;
    //    }
    //    Headers = hs;

    //    var len = -1;
    //    if (hs.TryGetValue("Content-Length", out str)) len = str.ToInt(-1);
    //    ContentLength = len;

    //    if (hs.TryGetValue("Connection", out str) && str.EqualIgnoreCase("Close")) Client.TryDispose();

    //    return rs.Slice(p + 4, len);
    //}

    private static readonly Byte[] NewLine = new[] { (Byte)'\r', (Byte)'\n' };
    private Packet ParseChunk(Packet rs, out Int32 offset, out Int32 octets)
    {
        // chunk编码
        // 1 ba \r\n xxxx \r\n 0 \r\n\r\n

        offset = 0;
        octets = 0;
        var p = rs.IndexOf(NewLine);
        if (p <= 0) return rs;

        // 第一段长度
        var str = rs.Slice(0, p).ToStr();
        //if (str.Length % 2 != 0) str = "0" + str;
        //var len = (Int32)str.ToHex().ToUInt32(0, false);
        //Int32.TryParse(str, NumberStyles.HexNumber, null, out var len);
        octets = Int32.Parse(str, NumberStyles.HexNumber);

        //if (ContentLength < 0) ContentLength = len;

        offset = p + 2;
        return rs.Slice(p + 2, octets);
    }
    #endregion

    #region 主要方法
    /// <summary>异步获取</summary>
    /// <param name="url">地址</param>
    /// <returns></returns>
    public async Task<String> GetStringAsync(String url)
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(url),
        };

        var rs = (await SendAsync(request, default).ConfigureAwait(false));
        if (rs == null) return null;

        rs.EnsureSuccessStatusCode();

        return rs.Body?.ToStr();
    }

    /// <summary>同步获取</summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public String GetString(String url)
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(url),
        };

        var rs = Send(request);
        return rs?.Body?.ToStr();
    }

    public async Task<Stream> GetStreamAsync(String url)
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(url),
        };

        var rs = (await SendAsync(request, default).ConfigureAwait(false));
        if (rs == null) return null;

        rs.EnsureSuccessStatusCode();

        return await rs.Content.ReadAsStreamAsync(default);
    }
    #endregion

    #region 远程调用
    /// <summary>同步调用，阻塞等待</summary>
    /// <param name="method">Get/Post</param>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <returns></returns>
    public TResult Invoke<TResult>(String method, String action, Object args = null)
    {
        if (BaseAddress == null) throw new ArgumentNullException(nameof(BaseAddress));

        var headers = DefaultRequestHeaders;
        if (method == "POST" && headers.ContentType.IsNullOrEmpty()) headers.ContentType = "application/json";

        // 序列化参数，决定GET/POST
        var request = CreateRequest(method, action, args);

        var rs = Send(request);

        if (rs == null || rs.Body == null || rs.Body.Total == 0) return default;

        return ProcessResponse<TResult>(rs.Body);
    }

    /// <summary>异步调用，等待返回结果</summary>
    /// <typeparam name="TResult">返回类型</typeparam>
    /// <param name="method">Get/Post</param>
    /// <param name="action">服务操作</param>
    /// <param name="args">参数</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public async Task<TResult> InvokeAsync<TResult>(String method, String action, Object args = null, CancellationToken cancellationToken = default)
    {
        if (BaseAddress == null) throw new ArgumentNullException(nameof(BaseAddress));

        var headers = DefaultRequestHeaders;
        if (method == "POST" && headers.ContentType.IsNullOrEmpty()) headers.ContentType = "application/json";

        var request = CreateRequest(method, action, args);

        var rs = await SendAsync(request, cancellationToken);

        if (rs == null || rs.Body == null || rs.Body.Total == 0) return default;

        return ProcessResponse<TResult>(rs.Body);
    }

    public TResult Get<TResult>(String action, Object args = null) => Invoke<TResult>("GET", action, args);

    public TResult Post<TResult>(String action, Object args = null) => Invoke<TResult>("POST", action, args);

    public Task<TResult> GetAsync<TResult>(String action, Object args = null) => InvokeAsync<TResult>("GET", action, args);

    public Task<TResult> PostAsync<TResult>(String action, Object args = null) => InvokeAsync<TResult>("POST", action, args);

    /// <summary>构造请求</summary>
    /// <param name="method"></param>
    /// <param name="action"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public HttpRequestMessage CreateRequest(String method, String action, Object args)
    {
        var headers = DefaultRequestHeaders;
        var request = new HttpRequestMessage
        {
            Method = method.ToUpper(),
            RequestUri = new Uri(BaseAddress, action),
            KeepAlive = KeepAlive,

            ContentType = headers.ContentType,
        };

        if (!headers.Accept.IsNullOrEmpty()) request["Accept"] = headers.Accept;
        if (!headers.UserAgent.IsNullOrEmpty()) request["User-Agent"] = headers.UserAgent;

        // 加上令牌或其它身份验证
        if (!Token.IsNullOrEmpty()) request["Authorization"] = Token.Contains(" ") ? Token : $"Bearer {Token}";

        if (method.EqualIgnoreCase("POST"))
        {
            // 准备参数，二进制优先
            if (args is Packet pk)
                request.Body = pk;
            // 支持IAccessor
            else if (args is IAccessor acc)
                request.Body = acc.ToPacket();
            else if (args is Byte[] buf)
                request.Body = buf;
            else
                request.Body = args.ToDictionary().ToJson().GetBytes();
        }
        else
        {
            var sb = Pool.StringBuilder.Get();
            sb.Append(action);
            sb.Append('?');

            var first = true;
            foreach (var item in args.ToDictionary())
            {
                if (!first) sb.Append('&');
                first = false;

                var v = item.Value is DateTime dt ? dt.ToFullString() : (item.Value + "");
                sb.AppendFormat("{0}={1}", item.Key, HttpUtility.UrlEncode(v));
            }

            request.RequestUri = new Uri(BaseAddress, sb.Put(true));
        }

        return request;
    }

    /// <summary>处理响应</summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="rs"></param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="ApiException"></exception>
    public TResult ProcessResponse<TResult>(Packet rs)
    {
        var str = rs.ToStr();
        if (typeof(TResult).IsBaseType()) return str.ChangeType<TResult>();

        // 反序列化
        var dic = JsonParser.Decode(str);
        if (!dic.TryGetValue("data", out var data)) throw new InvalidDataException("未识别响应数据");

        if (dic.TryGetValue("result", out var result))
        {
            if (result is Boolean res && !res) throw new InvalidOperationException($"远程错误，{data}");
        }
        else if (dic.TryGetValue("code", out var code))
        {
            if (code is Int32 cd && cd != 0) throw new ApiException(cd, data + "");
        }
        else
        {
            throw new InvalidDataException("未识别响应数据");
        }

        return JsonHelper.Convert<TResult>(data);
    }

    TResult IApiClient.Invoke<TResult>(String action, Object args) => Invoke<TResult>(args == null ? "GET" : "POST", action, args);

    Task<TResult> IApiClient.InvokeAsync<TResult>(String action, Object args, CancellationToken cancellationToken) => InvokeAsync<TResult>(args == null ? "GET" : "POST", action, args, cancellationToken);
    #endregion

    #region 日志
    /// <summary>日志</summary>
    public ILog Log { get; set; }

    /// <summary>写日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
    #endregion
}