using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using NewLife.Caching;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Serialization;
using NewLife.Xml;
using WebSocketMessageType = NewLife.Messaging.WebSocketMessageType;

namespace NewLife.Http;

/// <summary>Http帮助类</summary>
/// <remarks>
/// 1. 兼容多 TargetFramework（含 .NET Framework 4.5 起）
/// 2. 内部提供常用 Post / Get / 表单 / 多段上传等扩展
/// 3. 通过 <see cref="Tracer"/> 注入链路追踪；<see cref="Filter"/> 可拦截请求/响应/异常
/// 4. <see cref="CreateHandler(Boolean, Boolean, Boolean)"/> 提供自定义 <c>SocketsHttpHandler</c> 以解决 DNS 变更缓存和自定义证书验证
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
            var os = Environment.OSVersion?.ToString().TrimPrefix("Microsoft ");
            var name = aname.Name;

            // 检查 name 是否只包含 ASCII 字符（纯 UTF8 单字节），如果包含非 ASCII 则进行 URL 编码保留中文
            var hasNonAscii = !name.IsNullOrEmpty() && Encoding.UTF8.GetByteCount(name) != name.Length;
            if (hasNonAscii && !name.IsNullOrEmpty())
            {
                // URL 编码非 ASCII 字符，如"测试" -> "%E6%B5%8B%E8%AF%95"
                name = Uri.EscapeDataString(name);
            }

            // 仅当 OS 字符串为纯 UTF8 单字节（ASCII 子集）时附加，避免非 ASCII 引起的某些网关解析问题
            if (!os.IsNullOrEmpty() && Encoding.UTF8.GetByteCount(os) == os.Length)
                DefaultUserAgent = $"{name}/{aname.Version} ({os})";
            else
                DefaultUserAgent = $"{name}/{aname.Version}";
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

        // 内容长度：有实体时写明长度；无实体时也必须显式声明 0，否则报文不可自定界，
        // keep-alive 下客户端会把下一个响应当成本次实体。1xx/204/304 按 RFC 7230 §3.3.2 不得声明。
        // 调用方已自行声明 Content-Length/Transfer-Encoding 时不再插手，避免写出重复头部（CL.CL 走私面）
        var count = pk?.Total ?? 0;
        if (count > 0)
            sb.AppendFormat("Content-Length: {0}\r\n", count);
        else if (!HttpBase.IsNoEntityStatus(code) && !HasHeader(headers, "Content-Length") && !HasHeader(headers, "Transfer-Encoding"))
            sb.Append("Content-Length: 0\r\n");

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

    /// <summary>判断头部集合中是否已存在指定名称的头部（忽略大小写）</summary>
    /// <param name="headers">头部集合</param>
    /// <param name="name">头部名称</param>
    /// <returns>是否存在</returns>
    private static Boolean HasHeader(IDictionary<String, Object?>? headers, String name)
    {
        if (headers == null) return false;

        foreach (var item in headers)
        {
            if (item.Key.EqualIgnoreCase(name)) return true;
        }

        return false;
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
        // 请求与响应均为本次调用独占：发送完成后即可释放，不再依赖 GC（请求释放会连带释放其内容）
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = content };
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

            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

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

        using var rs = await client.GetStreamAsync(requestUri).ConfigureAwait(false);

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
#if NET5_0_OR_GREATER
        using var rs = await client.GetStreamAsync(requestUri, cancellationToken).ConfigureAwait(false);
#else
        using var rs = await client.GetStreamAsync(requestUri).ConfigureAwait(false);
#endif

        await SaveFileAsync(rs, fileName, null, cancellationToken).ConfigureAwait(false);
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

#if NET5_0_OR_GREATER
        using var rs = await client.GetStreamAsync(requestUri, cancellationToken).ConfigureAwait(false);
#else
        using var rs = await client.GetStreamAsync(requestUri).ConfigureAwait(false);
#endif

        await SaveFileAsync(rs, fileName, expectedHash, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>下载文件到本地，支持响应头/响应体分段超时、进度回调与哈希校验</summary>
    /// <param name="client">Http客户端</param>
    /// <param name="requestUri">请求资源地址</param>
    /// <param name="fileName">目标文件名。开启 <see cref="HttpDownloadOptions.UseContentDispositionFileName"/> 时可能被响应头文件名覆盖</param>
    /// <param name="options">下载选项</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>下载后的实际文件路径</returns>
    /// <remarks>
    /// 分段超时依赖独立的取消令牌，不会修改传入的 <paramref name="client"/>。
    /// 如果客户端自身设置了 <see cref="HttpClient.Timeout"/>，该超时仍可能生效；需要由本选项完整控制下载时，
    /// 建议调用方将 <see cref="HttpClient.Timeout"/> 设为 <see cref="System.Threading.Timeout.InfiniteTimeSpan"/>。
    /// </remarks>
    public static async Task<String> DownloadFileAsync(this HttpClient client, String requestUri, String fileName, HttpDownloadOptions options, CancellationToken cancellationToken = default)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (requestUri.IsNullOrEmpty()) throw new ArgumentNullException(nameof(requestUri));
        if (fileName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(fileName));

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        HttpResponseMessage rs;
        using (var headerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            if (options.HeaderTimeout > TimeSpan.Zero) headerCts.CancelAfter(options.HeaderTimeout);

            try
            {
                // 只等到响应头，响应体随后分块读取，便于统计进度与分段超时
                rs = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, headerCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // 只把本方法的响应头超时转换为 TimeoutException，客户端自身超时保持原样
                if (!headerCts.IsCancellationRequested) throw;

                throw new TimeoutException(L.T("No response headers received within {0} seconds: {1}", options.HeaderTimeout.TotalSeconds, requestUri), ex);
            }
        }

        using (rs)
        {
            rs.EnsureSuccessStatusCode();

            // 从Http响应头中获取文件名。FileNameStar 优先，FileName 可能带引号
            if (options.UseContentDispositionFileName)
            {
                var disposition = rs.Content.Headers?.ContentDisposition;
                var name = disposition?.FileNameStar;
                if (name.IsNullOrEmpty()) name = disposition?.FileName;
                if (!name.IsNullOrEmpty())
                {
                    name = name.Trim('"');
                    if (!name.IsNullOrEmpty())
                    {
                        var safeName = Path.GetFileName(name);
                        if (!safeName.IsNullOrEmpty()) fileName = Path.GetDirectoryName(fileName).CombinePath(safeName);
                    }
                }
            }

            var total = rs.Content.Headers?.ContentLength ?? 0;
            using var downloadCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (options.DownloadTimeout > TimeSpan.Zero) downloadCts.CancelAfter(options.DownloadTimeout);

            using var stream = await rs.Content.ReadAsStreamAsync().ConfigureAwait(false);
            try
            {
                await SaveFileCoreAsync(stream, fileName, options.ExpectedHash, total, options.Progress, options.BufferSize, downloadCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // 只把本方法的响应体超时转换为 TimeoutException，客户端自身超时保持原样
                if (!downloadCts.IsCancellationRequested) throw;

                throw new TimeoutException(L.T("Download did not complete within {0} seconds: {1}", options.DownloadTimeout.TotalSeconds, requestUri), ex);
            }
        }

        return fileName.GetFullPath();
    }

    /// <summary>保存流到本地文件并校验哈希。常用于文件下载</summary>
    /// <param name="stream">数据流</param>
    /// <param name="fileName">目标文件名</param>
    /// <param name="expectedHash">预期哈希字符串，支持带算法前缀或自动识别</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    /// <exception cref="IOException"></exception>
    public static Task SaveFileAsync(this Stream stream, String fileName, String? expectedHash, CancellationToken cancellationToken = default)
        => SaveFileCoreAsync(stream, fileName, expectedHash, 0, null, 0, cancellationToken);

    /// <summary>保存流到本地文件并校验哈希。支持进度回调与按 Content-Length 校验完整性</summary>
    private static async Task SaveFileCoreAsync(Stream stream, String fileName, String? expectedHash, Int64 total, Action<Int64, Int64>? progress, Int32 bufferSize, CancellationToken cancellationToken)
    {
        fileName = fileName.GetFullPath();

        // 在目标目录创建 .tmp 后缀临时文件，避免跨卷移动导致的额外复制开销
        var tmp = fileName + ".tmp";

        try
        {
            // 确保目录存在
            tmp.EnsureDirectory(true);

            // 打开目标目录下的临时文件进行写入，这样重命名更接近原子操作
            using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                if (bufferSize <= 0) bufferSize = SocketSetting.Current.BufferSize;
                if (bufferSize < 1024) bufferSize = 1024;

                var received = 0L;
                if (progress == null && total <= 0)
                {
                    // 无进度与完整性要求时走流复制，保持 SaveFileAsync 的原有性能特征
                    await stream.CopyToAsync(fs, bufferSize, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var buffer = new Byte[bufferSize];
                    while (true)
                    {
                        var n = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                        if (n <= 0) break;

                        await fs.WriteAsync(buffer, 0, n, cancellationToken).ConfigureAwait(false);
                        received += n;

                        progress?.Invoke(received, total);
                    }
                }

                // 上游提前断流时可能正常结束读取，按预期总长度校验完整性
                if (total > 0 && received != total)
                    throw new IOException(L.T("Incomplete download, expected {0} bytes but received {1} bytes.", total, received));

                await fs.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            // 校验哈希
            if (!expectedHash.IsNullOrEmpty())
            {
                var fi = tmp.AsFile();
                if (!fi.VerifyHash(expectedHash))
                {
                    // 校验失败，删除临时文件并抛出异常
                    try
                    {
                        if (File.Exists(tmp)) File.Delete(tmp);
                    }
                    catch { }
                    throw new IOException(L.T("Save file hash verification failed."));
                }
            }

            // 校验通过，移动到最终目标位置
            fileName.EnsureDirectory(true);
            if (File.Exists(fileName))
            {
                try
                {
                    File.Delete(fileName);
                }
                catch
                {
                    // 既然删不掉，就换个临时文件名吧
                    File.Move(fileName, $"{fileName}.tmp.{DateTime.Now:yyMMddHHmmss}");
                }
            }
            File.Move(tmp, fileName);
        }
        finally
        {
            // 清理残留临时文件
            try
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }
            catch { }
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
        // 内容容器由本方法释放（连带释放所持有的文件流），避免上传完成后文件句柄残留到 GC
        using var content = new MultipartFormDataContent();
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
}