using System.Globalization;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
    public TcpClient Client { get; set; }

    /// <summary>基础地址</summary>
    public Uri BaseAddress { get; set; }

    /// <summary>保持连接</summary>
    public Boolean KeepAlive { get; set; }

    /// <summary>超时时间。默认15s</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>缓冲区大小。接收缓冲区默认64*1024</summary>
    public Int32 BufferSize { get; set; } = 64 * 1024;

    /// <summary>性能追踪</summary>
    public ITracer Tracer { get; set; } = HttpHelper.Tracer;

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

        Client.TryDispose();
    }
    #endregion

    #region 核心方法
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
            await tc.ConnectAsync(remote.GetAddresses(), remote.Port).ConfigureAwait(false);

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
                await sslStream.AuthenticateAsClientAsync(uri.Host, new X509CertificateCollection(), SslProtocols.Tls12, false).ConfigureAwait(false);
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
        var source = new CancellationTokenSource(Timeout);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
        var count = await ns.ReadAsync(buf, source.Token).ConfigureAwait(false);
#else
        var count = await ns.ReadAsync(buf, 0, buf.Length, source.Token).ConfigureAwait(false);
#endif

        return new Packet(buf, 0, count);
    }

    /// <summary>异步发出请求，并接收响应</summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public virtual async Task<HttpResponse> SendAsync(HttpRequest request)
    {
        // 构造请求
        var uri = request.RequestUri;
        var req = request.Build();

        var res = new HttpResponse();
        Packet rs = null;
        var retry = 5;
        while (retry-- > 0)
        {
            // 发出请求
            rs = await SendDataAsync(uri, req).ConfigureAwait(false);
            if (rs == null || rs.Count == 0) return null;

            // 解析响应
            if (!res.Parse(rs)) return res;
            rs = res.Body;

            // 跳转
            if (res.StatusCode is HttpStatusCode.Moved or HttpStatusCode.Redirect)
            {
                if (res.Headers.TryGetValue("Location", out var location) && !location.IsNullOrEmpty())
                {
                    // 再次请求
                    var uri2 = new Uri(location);

                    if (uri.Host != uri2.Host || uri.Scheme != uri2.Scheme) Client.TryDispose();

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
        if (rs.Count > 0 && res.Headers.TryGetValue("Transfer-Encoding", out var s) && s.EqualIgnoreCase("chunked"))
        {
            res.Body = await ReadChunkAsync(rs);
        }

        // 断开连接
        if (!KeepAlive) Client.TryDispose();

        return res;
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
            while (total < len)
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

    #region 辅助
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
    /// <summary>异步获取。连接池操作</summary>
    /// <param name="url">地址</param>
    /// <returns></returns>
    public async Task<String> GetStringAsync(String url)
    {
        var request = new HttpRequest
        {
            RequestUri = new Uri(url),
        };

        var rs = (await SendAsync(request).ConfigureAwait(false));
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
    public async Task<TResult> InvokeAsync<TResult>(String method, String action, Object args = null)
    {
        if (BaseAddress == null) throw new ArgumentNullException(nameof(BaseAddress));

        var request = BuildRequest(method, action, args);

        var rs = await SendAsync(request);

        if (rs == null || rs.Body == null || rs.Body.Total == 0) return default;

        return ProcessResponse<TResult>(rs.Body);
    }

    private HttpRequest BuildRequest(String method, String action, Object args)
    {
        var req = new HttpRequest
        {
            Method = method.ToUpper(),
            RequestUri = new Uri(BaseAddress, action),
            KeepAlive = KeepAlive,
        };

        var ps = args.ToDictionary();
        if (method.EqualIgnoreCase("Post"))
            req.Body = ps.ToJson().GetBytes();
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

            req.RequestUri = new Uri(BaseAddress, sb.Put(true));
        }

        return req;
    }

    private TResult ProcessResponse<TResult>(Packet rs)
    {
        var str = rs.ToStr();
        if (Type.GetTypeCode(typeof(TResult)) != TypeCode.Object) return str.ChangeType<TResult>();

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