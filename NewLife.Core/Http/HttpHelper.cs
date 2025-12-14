using System.Net;
using System.Net.Http; // 补充：旧框架目标下无隐式全局 using，需要显式引用
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using NewLife.Caching;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Serialization;
using NewLife.Xml;

namespace NewLife.Http;

/// <summary>Http帮助类</summary>
/// <remarks>
/// 1. 兼容多 TargetFramework（含 .NET Framework 4.5 起）
/// 2. 内部提供常用 Post / Get / 表单 / 多段上传等扩展
/// 3. 通过 <see cref="Tracer"/> 注入链路追踪；<see cref="Filter"/> 可拦截请求/响应/异常
/// 4. <see cref="CreateHandler(bool, bool, bool)"/> 提供自定义 <c>SocketsHttpHandler</c> 以解决 DNS 变更缓存和自定义证书验证
/// </remarks>
public static class HttpHelper
{
    /// <summary>性能跟踪器</summary>
    public static ITracer? Tracer { get; set; } = DefaultTracer.Instance;

    /// <summary>Http过滤器。可在请求前后及异常时介入</summary>
    public static IHttpFilter? Filter { get; set; }

    /// <summary>默认用户浏览器UserAgent。用于内部创建的HttpClient请求</summary>
    public static String? DefaultUserAgent { get; set; }

    static HttpHelper()
    {
        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        //if (asm != null) agent = $"{asm.GetName().Name}/{asm.GetName().Version}";
        if (asm != null)
        {
            var aname = asm.GetName();
            var os = Environment.OSVersion?.ToString().TrimStart("Microsoft ");
            // 仅当 OS 字符串为纯 UTF8 单字节（ASCII 子集）时附加，避免非 ASCII 引起的某些网关解析问题
            if (!os.IsNullOrEmpty() && Encoding.UTF8.GetByteCount(os) == os.Length)
                DefaultUserAgent = $"{aname.Name}/{aname.Version} ({os})";
            else
                DefaultUserAgent = $"{aname.Name}/{aname.Version}";
        }
    }

    #region 默认封装
    /// <summary>设置浏览器UserAgent。默认使用应用名和版本（仅当未手动设置）</summary>
    public static HttpClient SetUserAgent(this HttpClient client)
    {
        var userAgent = DefaultUserAgent;
        if (!userAgent.IsNullOrEmpty()) client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

        return client;
    }

    /// <summary>为HttpClient创建Socket处理器，默认设置连接生命为5分钟，有效反映DNS网络更改</summary>
    /// <remarks>
    /// PooledConnectionLifetime 属性定义池中的最大连接生存期，从建立连接的时间跟踪其年龄，而不考虑其空闲时间或活动时间。
    /// 在主动用于服务请求时，连接不会被拆毁。此生存期非常有用，以便定期重新建立连接，以便更好地反映 DNS 或其他网络更改。
    /// </remarks>
    /// <param name="useProxy">是否使用代理</param>
    /// <param name="useCookie">是否使用Cookie</param>
    /// <returns></returns>
    public static HttpMessageHandler CreateHandler(Boolean useProxy, Boolean useCookie) => CreateHandler(useProxy, useCookie, false);

    /// <summary>为HttpClient创建Socket处理器，默认设置连接生命为5分钟，有效反映DNS网络更改</summary>
    /// <remarks>
    /// PooledConnectionLifetime 属性定义池中的最大连接生存期，从建立连接的时间跟踪其年龄，而不考虑其空闲时间或活动时间。
    /// 在主动用于服务请求时，连接不会被拆毁。此生存期非常有用，以便定期重新建立连接，以便更好地反映 DNS 或其他网络更改。
    /// </remarks>
    /// <param name="useProxy">是否使用代理</param>
    /// <param name="useCookie">是否使用Cookie</param>
    /// <param name="ignoreSSL">是否忽略证书检验（仅测试/内网场景使用）</param>
    public static HttpMessageHandler CreateHandler(Boolean useProxy, Boolean useCookie, Boolean ignoreSSL)
    {
#if NET5_0_OR_GREATER
        var handler = new SocketsHttpHandler
        {
            UseProxy = useProxy,
            UseCookies = useCookie,
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectCallback = ConnectCallback,
        };

        if (ignoreSSL)
        {
            handler.SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (_, _, _, _) => true
            };
        }
        return handler;
#elif NETCOREAPP3_0_OR_GREATER
        var handler = new SocketsHttpHandler
        {
            UseProxy = useProxy,
            UseCookies = useCookie,
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        };

        if (ignoreSSL)
        {
            handler.SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (_, _, _, _) => true
            };
        }
        return handler;
#else
        // 旧框架使用 HttpClientHandler（不支持自定义 ConnectCallback）。忽略证书为全局副作用，谨慎使用。
        var handler = new HttpClientHandler
        {
            UseProxy = useProxy,
            UseCookies = useCookie,
            AutomaticDecompression = DecompressionMethods.GZip
        };

        if (ignoreSSL)
            ServicePointManager.ServerCertificateValidationCallback = (_, _, _, _) => true;
        return handler;
#endif
    }

#if NET5_0_OR_GREATER
    /// <summary>连接回调，内部创建Socket，解决DNS解析缓存问题</summary>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    static async ValueTask<Stream> ConnectCallback(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var dep = context.DnsEndPoint;
        var method = context.InitialRequestMessage.Method?.ToString() ?? "Connect";
        using var span = Tracer?.NewSpan($"net:{dep.Host}:{dep.Port}:{method}");

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            var ep = context.DnsEndPoint;
            var addrs = NetUri.ParseAddress(ep.Host);
            span?.AppendTag($"addrs={addrs?.Join()}");
            if (addrs != null && addrs.Length > 0)
                await socket.ConnectAsync(addrs, ep.Port, cancellationToken).ConfigureAwait(false);
            else
                await socket.ConnectAsync(ep, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            if (ex is SocketException se) Tracer?.NewError($"socket:SocketError-{se.SocketErrorCode}", se);
            socket.Dispose();
            throw;
        }

        return new NetworkStream(socket, ownsSocket: true);
    }
#endif
    #endregion

    #region Http封包解包
    /// <summary>创建请求包（低层轻量封包构造，仅用于内部或自定义 Tcp 级交互，不等价于 <see cref="HttpRequestMessage"/>）</summary>
    /// <param name="method"></param>
    /// <param name="uri"></param>
    /// <param name="headers"></param>
    /// <param name="pk"></param>
    /// <returns></returns>
    public static IPacket MakeRequest(String method, Uri uri, IDictionary<String, Object?>? headers, IPacket? pk)
    {
        var count = pk?.Total ?? 0;
        if (method.IsNullOrEmpty()) method = count > 0 ? "POST" : "GET";
        uri ??= new Uri("/"); // 兜底，确保 PathAndQuery 可用

        var host = GetHost(uri);

        // 构建头部
        var sb = Pool.StringBuilder.Get();
        sb.AppendFormat("{0} {1} HTTP/1.1\r\n", method, uri.PathAndQuery);
        sb.AppendFormat("Host: {0}\r\n", host);

        //if (Compressed) sb.AppendLine("Accept-Encoding:gzip, deflate");
        //if (KeepAlive) sb.AppendLine("Connection:keep-alive");
        //if (!UserAgent.IsNullOrEmpty()) sb.AppendFormat("User-Agent:{0}\r\n", UserAgent);

        // 内容长度
        if (count > 0) sb.AppendFormat("Content-Length: {0}\r\n", count);

        if (headers != null)
        {
            foreach (var item in headers)
            {
                // 按 RFC 建议，在冒号后加空格以提升可读性
                sb.AppendFormat("{0}: {1}\r\n", item.Key, item.Value);
            }
        }

        sb.Append("\r\n");

        var rs = new ArrayPacket(sb.Return(true).GetBytes()) { Next = pk };
        return rs;
    }

    /// <summary>创建响应包（低层轻量封包构造）</summary>
    /// <param name="code"></param>
    /// <param name="headers"></param>
    /// <param name="pk"></param>
    /// <returns></returns>
    public static IPacket MakeResponse(HttpStatusCode code, IDictionary<String, Object?>? headers, IPacket? pk)
    {
        // 构建头部
        var sb = Pool.StringBuilder.Get();
        sb.AppendFormat("HTTP/1.1 {0} {1}\r\n", (Int32)code, code);

        // 内容长度
        var count = pk?.Total ?? 0;
        if (count > 0) sb.AppendFormat("Content-Length: {0}\r\n", count);
        if (headers != null)
        {
            foreach (var item in headers)
            {
                sb.AppendFormat("{0}: {1}\r\n", item.Key, item.Value);
            }
        }

        sb.Append("\r\n");

        var rs = new ArrayPacket(sb.Return(true).GetBytes()) { Next = pk };
        return rs;
    }

    private static readonly Byte[] NewLine = [(Byte)'\r', (Byte)'\n', (Byte)'\r', (Byte)'\n'];
    /// <summary>分析头部（修改原 <see cref="Packet"/>，截去首段头部）</summary>
    /// <param name="pk"></param>
    /// <returns></returns>
    public static IDictionary<String, Object> ParseHeader(Packet pk)
    {
        // 客户端收到响应，服务端收到请求
        var headers = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase);

        var p = pk.IndexOf(NewLine);
        if (p < 0) return headers;

        // 截取
        var lines = pk.ReadBytes(0, p).ToStr().Split("\r\n");
        p += 4; // 跳过 CRLFCRLF
        pk.Set(pk.Data, pk.Offset + p, pk.Count - p);

        // 分析头部
        headers.Clear();
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            var k = line.IndexOf(':');
            if (k > 0) headers[line[..k]] = line[(k + 1)..].Trim();
        }

        var first = lines.Length > 0 ? lines[0] : "";
        var ss = first.Split(' ');
        if (ss.Length >= 3 && ss[2].StartsWithIgnoreCase("HTTP/"))
        {
            headers["Method"] = ss[0];

            // 构造资源路径
            var host = headers.TryGetValue("Host", out var s) ? s : "";
            var uri = $"http://{host}{ss[1]}"; // 仅能猜测 http，若需 https 应由上层携带
            headers["Url"] = new Uri(uri);
        }
        else if (ss.Length >= 2)
        {
            // 分析响应码
            var code = ss[1].ToInt();
            if (code > 0) headers["StatusCode"] = (HttpStatusCode)code;
        }

        return headers;
    }
    #endregion

    #region 高级功能扩展
    /// <summary>异步提交Json</summary>
    /// <param name="client">Http客户端</param>
    /// <param name="requestUri">请求资源地址</param>
    /// <param name="data">数据</param>
    /// <param name="headers">附加头部</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public static async Task<String> PostJsonAsync(this HttpClient client, String requestUri, Object data, IDictionary<String, String>? headers = null, CancellationToken cancellationToken = default)
    {
        var content = data is String str
            ? new StringContent(str, Encoding.UTF8, "application/json")
            : new StringContent(data.ToJson(), Encoding.UTF8, "application/json");
        return await PostAsync(client, requestUri, content, headers, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>同步提交Json</summary>
    /// <param name="client">Http客户端</param>
    /// <param name="requestUri">请求资源地址</param>
    /// <param name="data">数据</param>
    /// <param name="headers">附加头部</param>
    /// <returns></returns>
    public static String PostJson(this HttpClient client, String requestUri, Object data, IDictionary<String, String>? headers = null) => client.PostJsonAsync(requestUri, data, headers).ConfigureAwait(false).GetAwaiter().GetResult();

    /// <summary>异步提交Xml</summary>
    /// <param name="client">Http客户端</param>
    /// <param name="requestUri">请求资源地址</param>
    /// <param name="data">数据</param>
    /// <param name="headers">附加头部</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public static async Task<String> PostXmlAsync(this HttpClient client, String requestUri, Object data, IDictionary<String, String>? headers = null, CancellationToken cancellationToken = default)
    {
        var content = data is String str
            ? new StringContent(str, Encoding.UTF8, "application/xml")
            : new StringContent(data.ToXml(), Encoding.UTF8, "application/xml");
        return await PostAsync(client, requestUri, content, headers, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>同步提交Xml</summary>
    /// <param name="client">Http客户端</param>
    /// <param name="requestUri">请求资源地址</param>
    /// <param name="data">数据</param>
    /// <param name="headers">附加头部</param>
    /// <returns></returns>
    public static String PostXml(this HttpClient client, String requestUri, Object data, IDictionary<String, String>? headers = null) => client.PostXmlAsync(requestUri, data, headers).ConfigureAwait(false).GetAwaiter().GetResult();

    /// <summary>异步提交表单，名值对传输字典参数</summary>
    /// <param name="client">Http客户端</param>
    /// <param name="requestUri">请求资源地址</param>
    /// <param name="data">名值对数据。匿名对象或字典</param>
    /// <param name="headers">附加头部</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public static async Task<String> PostFormAsync(this HttpClient client, String requestUri, Object data, IDictionary<String, String>? headers = null, CancellationToken cancellationToken = default)
    {
        HttpContent? content = null;
        if (data is String str)
        {
            content = new StringContent(str, Encoding.UTF8, "application/x-www-form-urlencoded");
        }
#if NET5_0_OR_GREATER
        else if (data is IDictionary<String?, String?> dic)
        {
            content = new FormUrlEncodedContent(dic);
        }
        else
        {
            var list = new List<KeyValuePair<String?, String?>>();
            //var dic2 = new Dictionary<String, String?>();
            foreach (var item in data.ToDictionary())
            {
                //dic2[item.Key + ""] = item.Value + "";
                list.Add(new KeyValuePair<String?, String?>(item.Key, item.Value + ""));
            }
            content = new FormUrlEncodedContent(list);
        }
#else
        else if (data is IDictionary<String, String> dic)
        {
            content = new FormUrlEncodedContent(dic);
        }
        else
        {
            var dic2 = new Dictionary<String, String>();
            foreach (var item in data.ToDictionary())
            {
                dic2[item.Key + ""] = item.Value + "";
            }
            content = new FormUrlEncodedContent(dic2);
        }
#endif
        return await PostAsync(client, requestUri, content, headers, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>同步提交表单，名值对传输字典参数</summary>
    /// <param name="client">Http客户端</param>
    /// <param name="requestUri">请求资源地址</param>
    /// <param name="data">名值对数据。匿名对象或字典</param>
    /// <param name="headers">附加头部</param>
    /// <returns></returns>
    public static String PostForm(this HttpClient client, String requestUri, Object data, IDictionary<String, String>? headers = null) => client.PostFormAsync(requestUri, data, headers).ConfigureAwait(false).GetAwaiter().GetResult();

    /// <summary>异步提交多段表单数据，含文件流</summary>
    /// <param name="client">Http客户端</param>
    /// <param name="requestUri">请求资源地址</param>
    /// <param name="data">名值对数据。匿名对象或字典，支持文件流</param>
    /// <param name="cancellationToken">取消通知</param>
    public static async Task<String> PostMultipartFormAsync(this HttpClient client, String requestUri, Object data, CancellationToken cancellationToken = default)
    {
        var content = new MultipartFormDataContent();

        foreach (var item in data.ToDictionary())
        {
            //if (item.Value == null) continue;

            if (item.Value is FileStream fs)
                content.Add(new StreamContent(fs), item.Key, Path.GetFileName(fs.Name));
            else if (item.Value is Stream stream)
                content.Add(new StreamContent(stream), item.Key);
            else if (item.Value is String str)
                content.Add(new StringContent(str), item.Key);
            else if (item.Value is Byte[] buf)
                content.Add(new ByteArrayContent(buf), item.Key);
            else if (item.Value == null || item.Value.GetType().IsBaseType())
                content.Add(new StringContent(item.Value + ""), item.Key);
            else
                content.Add(new StringContent(item.Value.ToJson()), item.Key);
        }

        return await PostAsync(client, requestUri, content, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>同步获取字符串</summary>
    /// <param name="client">Http客户端</param>
    /// <param name="requestUri">请求资源地址</param>
    /// <param name="headers">附加头部</param>
    /// <returns></returns>
    public static String GetString(this HttpClient client, String requestUri, IDictionary<String, String>? headers = null)
    {
        if (headers != null) client.AddHeaders(headers);
#if NET5_0_OR_GREATER
        using var source = new CancellationTokenSource(client.Timeout);
        return client.GetStringAsync(requestUri, source.Token).ConfigureAwait(false).GetAwaiter().GetResult();
#else
        return client.GetStringAsync(requestUri).ConfigureAwait(false).GetAwaiter().GetResult();
#endif
    }

    /// <summary>内部统一 Post 发送逻辑，支持 Filter / Tracer</summary>
    private static async Task<String> PostAsync(HttpClient client, String requestUri, HttpContent content, IDictionary<String, String>? headers, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = content };
        if (headers != null)
        {
            foreach (var item in headers)
            {
                request.Headers.Add(item.Key, item.Value);
            }
        }

        // 设置接受 mediaType
        if (content.Headers.TryGetValues("Content-Type", out var vs))
        {
            // application/json; charset=utf-8
            var type = vs.FirstOrDefault()?.Split(';').FirstOrDefault();
            if (type.EqualIgnoreCase("application/json", "application/xml")) request.Headers.Accept.ParseAdd(type);
        }

        // 开始跟踪，注入TraceId
        using var span = Tracer?.NewSpan(request);
        //if (span != null) span.SetTag(content.ReadAsStringAsync().Result);
        var filter = Filter;
        try
        {
            if (filter != null) await filter.OnRequest(client, request, null, cancellationToken).ConfigureAwait(false);

            var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (filter != null) await filter.OnResponse(client, response, request, cancellationToken).ConfigureAwait(false);

#if NET5_0_OR_GREATER
            var result = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
            var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

            // 增加埋点数据
            span?.AppendTag(result);

            return result;
        }
        catch (Exception ex)
        {
            // 跟踪异常
            span?.SetError(ex, null);

            if (filter != null) await filter.OnError(client, ex, request, cancellationToken).ConfigureAwait(false);

            throw;
        }
    }

    private static HttpClient AddHeaders(this HttpClient client, IDictionary<String, String> headers)
    {
        //if (client == null) return null;
        if (headers == null || headers.Count == 0) return client;

        foreach (var item in headers)
        {
            if (client.DefaultRequestHeaders.Contains(item.Key)) client.DefaultRequestHeaders.Remove(item.Key);
            client.DefaultRequestHeaders.Add(item.Key, item.Value);
        }

        return client;
    }

    /// <summary>下载文件到本地（覆盖/创建）</summary>
    /// <param name="client">Http客户端</param>
    /// <param name="requestUri">请求资源地址</param>
    /// <param name="fileName">目标文件名</param>
    public static async Task DownloadFileAsync(this HttpClient client, String requestUri, String fileName)
    {
        fileName = fileName.GetFullPath();

        var rs = await client.GetStreamAsync(requestUri).ConfigureAwait(false);

        // 使用系统临时目录生成随机临时文件名，跨平台可用
        var tempFile = Path.GetTempFileName();

        try
        {
            // 先下载文件到临时目录，再移动到目标目录，避免文件下载了半截
            using (var fs = new FileStream(tempFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                await rs.CopyToAsync(fs).ConfigureAwait(false);
                fs.SetLength(fs.Position);
                await fs.FlushAsync().ConfigureAwait(false);
            }

            // 下载成功后再移动到目标目录
            fileName.EnsureDirectory(true);

            // 兼容旧框架：不使用带 overwrite 的 Move 重载
            if (File.Exists(fileName)) File.Delete(fileName);

            File.Move(tempFile, fileName);
        }
        finally
        {
            // 清理临时文件（移动成功后该文件不存在，失败时尽量删除）
            try
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
            catch { }
        }
    }

    /// <summary>下载文件到本地（可取消）</summary>
    /// <param name="client">Http客户端</param>
    /// <param name="requestUri">请求资源地址</param>
    /// <param name="fileName">目标文件名</param>
    /// <param name="cancellationToken">取消通知</param>
    public static async Task DownloadFileAsync(this HttpClient client, String requestUri, String fileName, CancellationToken cancellationToken)
    {
        fileName = fileName.GetFullPath();

#if NET5_0_OR_GREATER
        var rs = await client.GetStreamAsync(requestUri, cancellationToken).ConfigureAwait(false);
#else
        var rs = await client.GetStreamAsync(requestUri).ConfigureAwait(false);
#endif

        // 使用系统临时目录生成随机临时文件名，跨平台可用
        var tempFile = Path.GetTempFileName();

        try
        {
            // 先下载文件到临时目录，再移动到目标目录，避免文件下载了半截
            using (var fs = new FileStream(tempFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var bufferSize = (Int32)Math.Min(81920, fs.Length);
                await rs.CopyToAsync(fs, bufferSize, cancellationToken).ConfigureAwait(false);
                fs.SetLength(fs.Position);
                await fs.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            // 下载成功后再移动到目标目录
            fileName.EnsureDirectory(true);

            // 兼容旧框架：不使用带 overwrite 的 Move 重载
            if (File.Exists(fileName)) File.Delete(fileName);

            File.Move(tempFile, fileName);
        }
        finally
        {
            // 清理临时文件（移动成功后该文件不存在，失败时尽量删除）
            try
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
            catch { }
        }
    }

    /// <summary>下载文件到本地并校验哈希（可取消）</summary>
    /// <remarks>
    /// 本方法增加了对下载文件完整性的验证。传入的 <paramref name="expectedHash"/> 支持带算法前缀（例如 "md5$..."、"sha256$..."）
    /// 或者不带前缀的哈希字符串，方法会依据长度推断算法，具体规则与 `PathHelper.VerifyHash` 保持一致。
    /// 
    /// 关于临时文件策略的两种常见实现：
    /// 1) 下载到系统临时目录（当前实现）：先保存到 `Path.GetTempFileName()` 生成的临时文件，再在校验通过后移动到目标目录。
    ///    优点：避免在目标目录产生临时文件，下载过程中不会打扰目标目录；对磁盘空间分配更灵活（可使用不同磁盘）。
    ///    缺点：跨卷移动（不同磁盘分区）时 Move 操作会变为复制，性能受影响；若目标目录在更严格的权限下，移动可能失败。
    /// 2) 在目标目录直接保存为带 `.tmp` 后缀的临时文件（例如 `aa.zip.tmp`）：下载完成并校验通过后再重命名为最终文件名。
    ///    优点：移动/重命名通常是原子操作（同一分区），速度快且更可靠；对跨卷问题天然避免。
    ///    缺点：在下载期间目标目录会出现临时文件，可能影响监控程序或其它进程对该目录的扫描；需要确保临时文件名不会与现有文件冲突。
    /// 
    /// 在多数场景下，推荐使用方案 2（在目标目录写入 `.tmp` 后缀）以获得更好的原子性与性能。如果存在需要将临时文件放到不同分区的需求，可以保留或扩展为可配置策略。
    /// 
    /// 本实现采用方案 2：在目标目录中创建以 `.tmp` 后缀的临时文件，完成下载并校验通过后重命名为最终文件名。
    /// </remarks>
    /// <param name="client">Http客户端</param>
    /// <param name="requestUri">请求资源地址</param>
    /// <param name="fileName">目标文件名</param>
    /// <param name="expectedHash">预期哈希字符串，支持带算法前缀或自动识别</param>
    /// <param name="cancellationToken">取消通知</param>
    public static async Task DownloadFileAsync(this HttpClient client, String requestUri, String fileName, String? expectedHash, CancellationToken cancellationToken = default)
    {
        if (expectedHash.IsNullOrEmpty())
        {
            // 委托给不带哈希的实现
            await client.DownloadFileAsync(requestUri, fileName, cancellationToken).ConfigureAwait(false);
            return;
        }

        fileName = fileName.GetFullPath();

        // 在目标目录创建 .tmp 后缀临时文件，避免跨卷移动导致的额外复制开销
        var tmp = fileName + ".tmp";

#if NET5_0_OR_GREATER
        var rs = await client.GetStreamAsync(requestUri, cancellationToken).ConfigureAwait(false);
#else
        var rs = await client.GetStreamAsync(requestUri).ConfigureAwait(false);
#endif

        try
        {
            // 确保目录存在
            tmp.EnsureDirectory(true);

            // 打开目标目录下的临时文件进行写入，这样重命名更接近原子操作
            using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var bufferSize = (Int32)Math.Min(81920, fs.Length);
                await rs.CopyToAsync(fs, bufferSize, cancellationToken).ConfigureAwait(false);
                await fs.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            // 校验哈希
            var fi = tmp.AsFile();
            if (!fi.VerifyHash(expectedHash))
            {
                // 校验失败，删除临时文件并抛出异常
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                throw new IOException("Downloaded file hash verification failed.");
            }

            // 校验通过，移动到最终目标位置
            fileName.EnsureDirectory(true);
            if (File.Exists(fileName)) File.Delete(fileName);
            File.Move(tmp, fileName);
        }
        finally
        {
            // 清理残留临时文件
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }

    /// <summary>上传文件以及表单数据</summary>
    /// <param name="client">Http客户端</param>
    /// <param name="requestUri">请求资源地址</param>
    /// <param name="fileName">目标文件名</param>
    /// <param name="data">其它表单数据</param>
    /// <param name="cancellationToken">取消通知</param>
    public static async Task<String> UploadFileAsync(this HttpClient client, String requestUri, String fileName, Object? data = null, CancellationToken cancellationToken = default)
    {
        var content = new MultipartFormDataContent();
        if (!fileName.IsNullOrEmpty())
            content.Add(new StreamContent(fileName.AsFile().OpenRead()), "file", Path.GetFileName(fileName));

        if (data != null)
        {
            foreach (var item in data.ToDictionary())
            {
                //if (item.Value == null) continue;

                if (item.Value is String str)
                    content.Add(new StringContent(str), item.Key);
                else if (item.Value is Byte[] buf)
                    content.Add(new ByteArrayContent(buf), item.Key);
                else if (item.Value == null || item.Value.GetType().IsBaseType())
                    content.Add(new StringContent(item.Value + ""), item.Key);
                else
                    content.Add(new StringContent(item.Value.ToJson()), item.Key);
            }
        }

        return await PostAsync(client, requestUri, content, null, cancellationToken).ConfigureAwait(false);
    }
    #endregion

    #region WebSocket
    /// <summary>从队列消费消息并推送到WebSocket客户端</summary>
    /// <param name="socket">WebSocket实例</param>
    /// <param name="queue">队列</param>
    /// <param name="onProcess">数据处理委托</param>
    /// <param name="source">取消通知源</param>
    /// <returns></returns>
    public static async Task ConsumeAndPushAsync(this WebSocket socket, IProducerConsumer<String> queue, Func<String, Byte[]>? onProcess, CancellationTokenSource source)
    {
        DefaultSpan.Current = null;
        var token = source.Token;
        try
        {
            while (!token.IsCancellationRequested && socket.Connected)
            {
                var msg = await queue.TakeOneAsync(30, token).ConfigureAwait(false);
                if (msg != null)
                {
                    var buf = onProcess != null ? onProcess(msg) : msg.GetBytes();
                    socket.Send(buf, WebSocketMessageType.Text);
                }
                else
                {
                    await Task.Delay(100, token).ConfigureAwait(false);
                }
            }
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
        }
        finally
        {
            source.Cancel();
        }
    }

    /// <summary>从队列消费消息并推送到WebSocket客户端</summary>
    /// <param name="socket">WebSocket实例</param>
    /// <param name="host">缓存主机</param>
    /// <param name="topic">主题</param>
    /// <param name="source">取消通知源</param>
    /// <returns></returns>
    public static Task ConsumeAndPushAsync(this WebSocket socket, ICache host, String topic, CancellationTokenSource source) => ConsumeAndPushAsync(socket, host.GetQueue<String>(topic), null, source);

    /// <summary>从队列消费消息并推送到System.Net.WebSockets客户端</summary>
    /// <param name="socket">WebSocket实例</param>
    /// <param name="queue">队列</param>
    /// <param name="onProcess">数据处理委托</param>
    /// <param name="source">取消通知源</param>
    /// <returns></returns>
    public static async Task ConsumeAndPushAsync(this System.Net.WebSockets.WebSocket socket, IProducerConsumer<String> queue, Func<String, Byte[]>? onProcess, CancellationTokenSource source)
    {
        DefaultSpan.Current = null;
        var token = source.Token;
        try
        {
            while (!token.IsCancellationRequested && socket.State == System.Net.WebSockets.WebSocketState.Open)
            {
                var msg = await queue.TakeOneAsync(30, token).ConfigureAwait(false);
                if (msg != null)
                {
                    var buf = onProcess != null ? onProcess(msg) : msg.GetBytes();

                    if (buf != null && buf.Length > 0)
                        await socket.SendAsync(new ArraySegment<Byte>(buf), System.Net.WebSockets.WebSocketMessageType.Text, true, token).ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay(100, token).ConfigureAwait(false);
                }
            }
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
        }
        finally
        {
            source.Cancel();
        }
    }

    /// <summary>从队列消费消息并推送到System.Net.WebSockets客户端</summary>
    /// <param name="socket">WebSocket实例</param>
    /// <param name="host">缓存主机</param>
    /// <param name="topic">主题</param>
    /// <param name="source">取消通知源</param>
    /// <returns></returns>
    public static Task ConsumeAndPushAsync(this System.Net.WebSockets.WebSocket socket, ICache host, String topic, CancellationTokenSource source) => ConsumeAndPushAsync(socket, host.GetQueue<String>(topic), null, source);

    /// <summary>阻塞等待WebSocket关闭</summary>
    /// <param name="socket">WebSocket实例</param>
    /// <param name="onReceive">数据处理委托</param>
    /// <param name="source">取消通知源</param>
    /// <returns></returns>
    public static async Task WaitForClose(this System.Net.WebSockets.WebSocket socket, Action<String?>? onReceive, CancellationTokenSource source)
    {
        try
        {
            var buf = new Byte[4 * 1024];
            while (!source.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                var data = await socket.ReceiveAsync(new ArraySegment<Byte>(buf), source.Token).ConfigureAwait(false);
                if (data.MessageType == System.Net.WebSockets.WebSocketMessageType.Close) break;
                if (data.MessageType == System.Net.WebSockets.WebSocketMessageType.Text)
                {
                    var str = buf.ToStr(null, 0, data.Count);
                    if (!str.IsNullOrEmpty()) onReceive?.Invoke(str);
                }
            }

            if (!source.IsCancellationRequested) source.Cancel();

            if (socket.State == WebSocketState.Open)
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "finish", default).ConfigureAwait(false);
        }
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }
        catch (WebSocketException ex)
        {
            XTrace.WriteLine("WebSocket异常 {0}", ex.Message);
        }
        finally
        {
            source.Cancel();
        }
    }
    #endregion

    #region 辅助
    /// <summary>根据 Uri 计算 Host 头（含端口）；支持 http/https/ws/wss</summary>
    private static String GetHost(Uri uri)
    {
        if (uri == null) return String.Empty;
        var port = uri.Port;
        return uri.Scheme.ToLowerInvariant() switch
        {
            "http" or "ws" => port == 80 ? uri.Host : $"{uri.Host}:{port}",
            "https" or "wss" => port == 443 ? uri.Host : $"{uri.Host}:{port}",
            _ => uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{port}"
        };
    }
    #endregion
}