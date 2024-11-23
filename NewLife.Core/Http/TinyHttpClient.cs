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

    /// <summary>Json序列化</summary>
    public IJsonHost JsonHost { get; set; } = JsonHelper.Default;

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

                var sslStream = new SslStream(ns, false, (sender, certificate, chain, sslPolicyErrors) => true);
                await sslStream.AuthenticateAsClientAsync(uri.Host, [], SslProtocols.Tls12, false).ConfigureAwait(false);
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
            var rs2 = await SendDataAsync(uri, req).ConfigureAwait(false);
            if (rs2 == null || rs2.Length == 0) return null;

            // 解析响应
            if (!res.Parse(rs2)) return res;
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

                    req.TryDispose();
                    req = request.Build();

                    continue;
                }
            }

            break;
        }

        // 释放数据包，还给缓冲池
        req.TryDispose();

        if (res.StatusCode != HttpStatusCode.OK) throw new Exception($"{(Int32)res.StatusCode} {res.StatusDescription}");

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
                if (pk == null || pk.Length == 0) break;

                pk.CopyTo(ms);

                total += pk.Length;
            }

            // 从内存流获取缓冲区，打包为数据包返回，避免再次内存分配
            ms.Position = 0;
            rs = new ArrayPacket(ms);
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

            res.Body = await ReadChunkAsync(rs);
        }

        // 断开连接
        if (!KeepAlive) Client.TryDispose();

        return res;
    }

    /// <summary>读取分片，返回链式Packet</summary>
    /// <param name="body"></param>
    /// <returns></returns>
    protected virtual async Task<IPacket> ReadChunkAsync(IPacket body)
    {
        // 使用内存流拼接需要多次接收的数据包，降低逻辑复杂度
        var ms = new MemoryStream(BufferSize);

        var pk = body;
        while (true)
        {
            // 分析一个片段，如果该片段数据不足，则需要多次读取
            var data = pk.GetSpan();
            if (!ParseChunk(data, out var offset, out var len)) break;

            // 最后一个片段的长度为0
            if (len <= 0) break;

            // chunk是否完整
            var memory = pk.GetMemory();
            if (offset + len <= memory.Length)
            {
                // 完整数据，截取需要的部分
                memory = memory.Slice(offset, len);
                ms.Write(memory);

                // 更新pk，可能还有粘包数据。每一帧数据后面有\r\n
                var next = offset + len + 2;
                if (next < pk.Length)
                    pk = pk.Slice(next, -1, true);
                else
                {
                    pk.TryDispose();
                    pk = null;
                }
            }
            else
            {
                // 写入片段数据，数据不足
                memory = memory[offset..];
                ms.Write(memory);

                pk.TryDispose();
                pk = null;

                // 如果该片段数据不足，则需要多次读取
                var remain = len - memory.Length;
                while (remain > 0)
                {
                    var pk2 = await SendDataAsync(null, null).ConfigureAwait(false);
                    memory = pk2.GetMemory();

                    // 结尾的间断符号（如换行或00）。这里有可能一个数据包里面同时返回多个分片
                    if (remain <= memory.Length)
                    {
                        ms.Write(memory[..remain]);

                        // 如果还有剩余，作为下一个chunk
                        if (remain + 2 < memory.Length)
                            pk = pk2.Slice(remain + 2, -1, true);
                        else
                            pk2.TryDispose();

                        remain = 0;
                    }
                    else
                    {
                        ms.Write(memory);
                        remain -= memory.Length;

                        pk2.TryDispose();
                    }
                }
            }

            // 还有粘包数据，继续分析
            if (pk != null && pk.Length > 0) continue;

            // 读取新的数据片段，如果不存在则跳出
            pk = await SendDataAsync(null, null).ConfigureAwait(false);
            if (pk == null || pk.Length == 0) break;
        }

        ms.Position = 0;
        return new ArrayPacket(ms);
    }
    #endregion

    #region 辅助
    private static readonly Byte[] NewLine = [(Byte)'\r', (Byte)'\n'];
    private Boolean ParseChunk(Span<Byte> data, out Int32 offset, out Int32 octets)
    {
        // chunk编码
        // 1 ba \r\n xxxx \r\n 0 \r\n\r\n

        offset = 0;
        octets = 0;
        var p = data.IndexOf(NewLine);
        if (p <= 0) return false;

        // 第一段长度
#if NET8_0_OR_GREATER
        octets = Int32.Parse(data[..p], NumberStyles.HexNumber);
#else
        var str = data[..p].ToStr();
        octets = Int32.Parse(str, NumberStyles.HexNumber);
#endif

        offset = p + 2;

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

        using var rs = await SendAsync(request);

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
            req.Body = (ArrayPacket)JsonHost.Write(ps).GetBytes();
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