﻿using System.Net;
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
#if !NET40
using System.Net.WebSockets;
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.Http;

/// <summary>Http帮助类</summary>
public static class HttpHelper
{
    /// <summary>性能跟踪器</summary>
    public static ITracer Tracer { get; set; } = DefaultTracer.Instance;

    /// <summary>Http过滤器</summary>
    public static IHttpFilter Filter { get; set; }

    /// <summary>默认用户浏览器UserAgent。用于内部创建的HttpClient请求</summary>
    public static String DefaultUserAgent { get; set; }

    static HttpHelper()
    {
        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        //if (asm != null) agent = $"{asm.GetName().Name}/{asm.GetName().Version}";
        if (asm != null)
        {
            var aname = asm.GetName();
            var os = Environment.OSVersion?.ToString().TrimStart("Microsoft ");
            if (!os.IsNullOrEmpty() && Encoding.UTF8.GetByteCount(os) == os.Length)
                DefaultUserAgent = $"{aname.Name}/{aname.Version} ({os})";
            else
                DefaultUserAgent = $"{aname.Name}/{aname.Version}";
        }
    }

    #region 默认封装
    /// <summary>设置浏览器UserAgent。默认使用应用名和版本</summary>
    /// <param name="client"></param>
    /// <returns></returns>
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
    public static HttpMessageHandler CreateHandler(Boolean useProxy, Boolean useCookie)
    {
#if NET5_0_OR_GREATER
        return new SocketsHttpHandler
        {
            UseProxy = useProxy,
            UseCookies = useCookie,
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectCallback = ConnectCallback,
        };
#elif NETCOREAPP3_0_OR_GREATER
        return new SocketsHttpHandler
        {
            UseProxy = useProxy,
            UseCookies = useCookie,
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        };
#else
        return new HttpClientHandler
        {
            UseProxy = useProxy,
            UseCookies = useCookie,
            AutomaticDecompression = DecompressionMethods.GZip
        };
#endif
    }

#if NET5_0_OR_GREATER
    /// <summary>连接回调，内部创建Socket，解决DNS解析缓存问题</summary>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    static async ValueTask<Stream> ConnectCallback(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true
        };
        try
        {
            var ep = context.DnsEndPoint;
            var addrs = NetUri.ParseAddress(ep.Host);
            if (addrs != null && addrs.Length > 0)
                await socket.ConnectAsync(addrs, ep.Port, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            else
                await socket.ConnectAsync(ep, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
        return new NetworkStream(socket, ownsSocket: true);
    }
#endif
    #endregion

    #region Http封包解包
    /// <summary>创建请求包</summary>
    /// <param name="method"></param>
    /// <param name="uri"></param>
    /// <param name="headers"></param>
    /// <param name="pk"></param>
    /// <returns></returns>
    public static Packet MakeRequest(String method, Uri uri, IDictionary<String, Object> headers, Packet pk)
    {
        if (method.IsNullOrEmpty()) method = pk?.Count > 0 ? "POST" : "GET";

        // 分解主机和资源
        var host = "";
        if (uri == null) uri = new Uri("/");

        if (uri.Scheme.EqualIgnoreCase("http", "ws"))
        {
            if (uri.Port == 80)
                host = uri.Host;
            else
                host = $"{uri.Host}:{uri.Port}";
        }
        else if (uri.Scheme.EqualIgnoreCase("https"))
        {
            if (uri.Port == 443)
                host = uri.Host;
            else
                host = $"{uri.Host}:{uri.Port}";
        }

        // 构建头部
        var sb = Pool.StringBuilder.Get();
        sb.AppendFormat("{0} {1} HTTP/1.1\r\n", method, uri.PathAndQuery);
        sb.AppendFormat("Host:{0}\r\n", host);

        //if (Compressed) sb.AppendLine("Accept-Encoding:gzip, deflate");
        //if (KeepAlive) sb.AppendLine("Connection:keep-alive");
        //if (!UserAgent.IsNullOrEmpty()) sb.AppendFormat("User-Agent:{0}\r\n", UserAgent);

        // 内容长度
        if (pk?.Count > 0) sb.AppendFormat("Content-Length:{0}\r\n", pk.Count);

        foreach (var item in headers)
        {
            sb.AppendFormat("{0}:{1}\r\n", item.Key, item.Value);
        }

        sb.Append("\r\n");

        //return sb.ToString();
        var rs = new Packet(sb.Put(true).GetBytes())
        {
            Next = pk
        };
        return rs;
    }

    /// <summary>创建响应包</summary>
    /// <param name="code"></param>
    /// <param name="headers"></param>
    /// <param name="pk"></param>
    /// <returns></returns>
    public static Packet MakeResponse(HttpStatusCode code, IDictionary<String, Object> headers, Packet pk)
    {
        // 构建头部
        var sb = Pool.StringBuilder.Get();
        sb.AppendFormat("HTTP/1.1 {0} {1}\r\n", (Int32)code, code);

        // 内容长度
        if (pk?.Count > 0) sb.AppendFormat("Content-Length:{0}\r\n", pk.Count);

        foreach (var item in headers)
        {
            sb.AppendFormat("{0}:{1}\r\n", item.Key, item.Value);
        }

        sb.Append("\r\n");

        //return sb.ToString();
        var rs = new Packet(sb.Put(true).GetBytes())
        {
            Next = pk
        };
        return rs;
    }

    private static readonly Byte[] NewLine = new[] { (Byte)'\r', (Byte)'\n', (Byte)'\r', (Byte)'\n' };
    /// <summary>分析头部</summary>
    /// <param name="pk"></param>
    /// <returns></returns>
    public static IDictionary<String, Object> ParseHeader(Packet pk)
    {
        // 客户端收到响应，服务端收到请求
        var headers = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase);

        var p = pk.IndexOf(NewLine);
        if (p < 0) return headers;

#if DEBUG
        //WriteLog(pk.ToStr());
#endif

        // 截取
        var lines = pk.ReadBytes(0, p).ToStr().Split("\r\n");
        // 重构
        p += 4;
        pk.Set(pk.Data, pk.Offset + p, pk.Count - p);

        // 分析头部
        headers.Clear();
        var line = lines[0];
        for (var i = 1; i < lines.Length; i++)
        {
            line = lines[i];
            p = line.IndexOf(':');
            if (p > 0) headers[line.Substring(0, p)] = line.Substring(p + 1).Trim();
        }

        line = lines[0];
        var ss = line.Split(" ");
        // 分析请求方法 GET / HTTP/1.1
        if (ss.Length >= 3 && ss[2].StartsWithIgnoreCase("HTTP/"))
        {
            headers["Method"] = ss[0];

            // 构造资源路径
            var host = headers.TryGetValue("Host", out var s) ? s : "";
            var uri = $"http://{host}";
            //var uri = "{0}://{1}".F(IsSSL ? "https" : "http", host);
            //if (host.IsNullOrEmpty() || !host.Contains(":"))
            //{
            //    var port = Local.Port;
            //    if (IsSSL && port != 443 || !IsSSL && port != 80) uri += ":" + port;
            //}
            uri += ss[1];
            headers["Url"] = new Uri(uri);
        }
        else
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
    public static async Task<String> PostJsonAsync(this HttpClient client, String requestUri, Object data, IDictionary<String, String> headers = null, CancellationToken cancellationToken = default)
    {
        HttpContent content = null;
        if (data != null)
        {
            content = data is String str
                ? new StringContent(str, Encoding.UTF8, "application/json")
                : new StringContent(data.ToJson(), Encoding.UTF8, "application/json");
        }

        //if (headers == null && client.DefaultRequestHeaders.Accept.Count == 0) client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

        return await PostAsync(client, requestUri, content, headers, cancellationToken);
    }

    /// <summary>同步提交Json</summary>
    /// <param name="client">Http客户端</param>
    /// <param name="requestUri">请求资源地址</param>
    /// <param name="data">数据</param>
    /// <param name="headers">附加头部</param>
    /// <returns></returns>
    public static String PostJson(this HttpClient client, String requestUri, Object data, IDictionary<String, String> headers = null) => TaskEx.Run(() => client.PostJsonAsync(requestUri, data, headers)).Result;

    /// <summary>异步提交Xml</summary>
    /// <param name="client">Http客户端</param>
    /// <param name="requestUri">请求资源地址</param>
    /// <param name="data">数据</param>
    /// <param name="headers">附加头部</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public static async Task<String> PostXmlAsync(this HttpClient client, String requestUri, Object data, IDictionary<String, String> headers = null, CancellationToken cancellationToken = default)
    {
        HttpContent content = null;
        if (data != null)
        {
            content = data is String str
                ? new StringContent(str, Encoding.UTF8, "application/xml")
                : new StringContent(data.ToXml(), Encoding.UTF8, "application/xml");
        }

        //if (headers == null && client.DefaultRequestHeaders.Accept.Count == 0) client.DefaultRequestHeaders.Accept.ParseAdd("application/xml");
        //client.AddHeaders(headers);

        return await PostAsync(client, requestUri, content, headers, cancellationToken);
    }

    /// <summary>同步提交Xml</summary>
    /// <param name="client">Http客户端</param>
    /// <param name="requestUri">请求资源地址</param>
    /// <param name="data">数据</param>
    /// <param name="headers">附加头部</param>
    /// <returns></returns>
    public static String PostXml(this HttpClient client, String requestUri, Object data, IDictionary<String, String> headers = null) => TaskEx.Run(() => client.PostXmlAsync(requestUri, data, headers)).Result;

    /// <summary>异步提交表单，名值对传输字典参数</summary>
    /// <param name="client">Http客户端</param>
    /// <param name="requestUri">请求资源地址</param>
    /// <param name="data">名值对数据。匿名对象或字典</param>
    /// <param name="headers">附加头部</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public static async Task<String> PostFormAsync(this HttpClient client, String requestUri, Object data, IDictionary<String, String> headers = null, CancellationToken cancellationToken = default)
    {
        HttpContent content = null;
        if (data != null)
        {
            content = data is String str
                ? new StringContent(str, Encoding.UTF8, "application/x-www-form-urlencoded")
                : (
                    data is IDictionary<String, String> dic
                    ? new FormUrlEncodedContent(dic)
                    : new FormUrlEncodedContent(data.ToDictionary().ToDictionary(e => e.Key, e => e.Value + ""))
                );
        }

        return await PostAsync(client, requestUri, content, headers, cancellationToken);
    }

    /// <summary>同步提交表单，名值对传输字典参数</summary>
    /// <param name="client">Http客户端</param>
    /// <param name="requestUri">请求资源地址</param>
    /// <param name="data">名值对数据。匿名对象或字典</param>
    /// <param name="headers">附加头部</param>
    /// <returns></returns>
    public static String PostForm(this HttpClient client, String requestUri, Object data, IDictionary<String, String> headers = null) => TaskEx.Run(() => client.PostFormAsync(requestUri, data, headers)).Result;

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
            if (item.Value == null) continue;

            if (item.Value is FileStream fs)
                content.Add(new StreamContent(fs), item.Key, Path.GetFileName(fs.Name));
            else if (item.Value is Stream stream)
                content.Add(new StreamContent(stream), item.Key);
            else if (item.Value is String str)
                content.Add(new StringContent(str), item.Key);
            else if (item.Value is Byte[] buf)
                content.Add(new ByteArrayContent(buf), item.Key);
            else if (item.Value.GetType().GetTypeCode() != TypeCode.Object)
                content.Add(new StringContent(item.Value + ""), item.Key);
            else
                content.Add(new StringContent(item.Value.ToJson()), item.Key);
        }

        return await PostAsync(client, requestUri, content, null, cancellationToken);
    }

    /// <summary>同步获取字符串</summary>
    /// <param name="client">Http客户端</param>
    /// <param name="requestUri">请求资源地址</param>
    /// <param name="headers">附加头部</param>
    /// <returns></returns>
    public static String GetString(this HttpClient client, String requestUri, IDictionary<String, String> headers = null)
    {
        client.AddHeaders(headers);
        return TaskEx.Run(() => client.GetStringAsync(requestUri)).Result;
    }

    private static async Task<String> PostAsync(HttpClient client, String requestUri, HttpContent content, IDictionary<String, String> headers, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = content
        };

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
            var type = vs.FirstOrDefault()?.Split(";").FirstOrDefault();
            if (type.EqualIgnoreCase("application/json", "application/xml")) request.Headers.Accept.ParseAdd(type);
        }

        // 开始跟踪，注入TraceId
        using var span = Tracer?.NewSpan(request);
        //if (span != null) span.SetTag(content.ReadAsStringAsync().Result);
        var filter = Filter;
        try
        {
            if (filter != null) await filter.OnRequest(client, request, null, cancellationToken);

            var response = await client.SendAsync(request, cancellationToken);

            if (filter != null) await filter.OnResponse(client, response, request, cancellationToken);

#if NET5_0_OR_GREATER
            var result = await response.Content.ReadAsStringAsync(cancellationToken);
#else
            var result = await response.Content.ReadAsStringAsync();
#endif

            // 增加埋点数据
            span?.AppendTag(result);

            return result;
        }
        catch (Exception ex)
        {
            // 跟踪异常
            span?.SetError(ex, null);

            if (filter != null) await filter.OnError(client, ex, request, cancellationToken);

            throw;
        }
    }

    private static HttpClient AddHeaders(this HttpClient client, IDictionary<String, String> headers)
    {
        if (client == null) return null;
        if (headers == null || headers.Count == 0) return client;

        foreach (var item in headers)
        {
            //判断请求头中是否已存在，存在先删除，再添加
            if (client.DefaultRequestHeaders.Contains(item.Key))
                client.DefaultRequestHeaders.Remove(item.Key);
            client.DefaultRequestHeaders.Add(item.Key, item.Value);
        }

        return client;
    }

    /// <summary>下载文件</summary>
    /// <param name="client">Http客户端</param>
    /// <param name="requestUri">请求资源地址</param>
    /// <param name="fileName">目标文件名</param>
    public static async Task DownloadFileAsync(this HttpClient client, String requestUri, String fileName)
    {
        var rs = await client.GetStreamAsync(requestUri);
        fileName.EnsureDirectory(true);
        using var fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
#if NET40
        rs.CopyTo(fs);
        fs.Flush();
#else
        await rs.CopyToAsync(fs);
        await fs.FlushAsync();
#endif
    }

    /// <summary>下载文件</summary>
    /// <param name="client">Http客户端</param>
    /// <param name="requestUri">请求资源地址</param>
    /// <param name="fileName">目标文件名</param>
    /// <param name="cancellationToken">取消通知</param>
    public static async Task DownloadFileAsync(this HttpClient client, String requestUri, String fileName, CancellationToken cancellationToken)
    {
#if NET40
        var rs = await client.GetStreamAsync(requestUri);
        fileName.EnsureDirectory(true);
        using var fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        rs.CopyTo(fs);
        fs.Flush();
#else
        var rs = await client.GetStreamAsync(requestUri);
        fileName.EnsureDirectory(true);
        using var fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        await rs.CopyToAsync(fs, 81920, cancellationToken);
        await fs.FlushAsync(cancellationToken);
#endif
    }

    /// <summary>上传文件以及表单数据</summary>
    /// <param name="client">Http客户端</param>
    /// <param name="requestUri">请求资源地址</param>
    /// <param name="fileName">目标文件名</param>
    /// <param name="data">其它表单数据</param>
    /// <param name="cancellationToken">取消通知</param>
    public static async Task<String> UploadFileAsync(this HttpClient client, String requestUri, String fileName, Object data = null, CancellationToken cancellationToken = default)
    {
        var content = new MultipartFormDataContent();
        if (!fileName.IsNullOrEmpty())
            content.Add(new StreamContent(fileName.AsFile().OpenRead()), "file", Path.GetFileName(fileName));

        if (data != null)
        {
            foreach (var item in data.ToDictionary())
            {
                if (item.Value == null) continue;

                if (item.Value is String str)
                    content.Add(new StringContent(str), item.Key);
                else if (item.Value is Byte[] buf)
                    content.Add(new ByteArrayContent(buf), item.Key);
                else if (item.Value.GetType().GetTypeCode() != TypeCode.Object)
                    content.Add(new StringContent(item.Value + ""), item.Key);
                else
                    content.Add(new StringContent(item.Value.ToJson()), item.Key);
            }
        }

        return await PostAsync(client, requestUri, content, null, cancellationToken);
    }
    #endregion

    #region WebSocket
#if !NET40
    /// <summary>从队列消费消息并推送到WebSocket客户端</summary>
    /// <param name="socket">WebSocket实例</param>
    /// <param name="queue">队列</param>
    /// <param name="onProcess">数据处理委托</param>
    /// <param name="source">取消通知源</param>
    /// <returns></returns>
    public static async Task ConsumeAndPushAsync(this WebSocket socket, IProducerConsumer<String> queue, Func<String, Byte[]> onProcess, CancellationTokenSource source)
    {
        DefaultSpan.Current = null;
        var token = source.Token;
        try
        {
            while (!token.IsCancellationRequested && socket.Connected)
            {
                var msg = await queue.TakeOneAsync(30, token);
                if (msg != null)
                {
                    var buf = onProcess != null ? onProcess(msg) : msg.GetBytes();
                    socket.Send(buf, WebSocketMessageType.Text);
                }
                else
                {
                    await Task.Delay(100, token);
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
    public static async Task ConsumeAndPushAsync(this WebSocket socket, ICache host, String topic, CancellationTokenSource source) => await ConsumeAndPushAsync(socket, host.GetQueue<String>(topic), null, source);

    /// <summary>从队列消费消息并推送到WebSocket客户端</summary>
    /// <param name="socket">WebSocket实例</param>
    /// <param name="queue">队列</param>
    /// <param name="onProcess">数据处理委托</param>
    /// <param name="source">取消通知源</param>
    /// <returns></returns>
    public static async Task ConsumeAndPushAsync(this System.Net.WebSockets.WebSocket socket, IProducerConsumer<String> queue, Func<String, Byte[]> onProcess, CancellationTokenSource source)
    {
        DefaultSpan.Current = null;
        var token = source.Token;
        try
        {
            while (!token.IsCancellationRequested && socket.State == System.Net.WebSockets.WebSocketState.Open)
            {
                var msg = await queue.TakeOneAsync(30, token);
                if (msg != null)
                {
                    var buf = onProcess != null ? onProcess(msg) : msg.GetBytes();

                    if (buf != null && buf.Length > 0)
                        await socket.SendAsync(new ArraySegment<Byte>(buf), System.Net.WebSockets.WebSocketMessageType.Text, true, token);
                }
                else
                {
                    await Task.Delay(100, token);
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
    public static async Task ConsumeAndPushAsync(this System.Net.WebSockets.WebSocket socket, ICache host, String topic, CancellationTokenSource source) => await ConsumeAndPushAsync(socket, host.GetQueue<String>(topic), null, source);

    /// <summary>阻塞等待WebSocket关闭</summary>
    /// <param name="socket">WebSocket实例</param>
    /// <param name="onReceive">数据处理委托</param>
    /// <param name="source">取消通知源</param>
    /// <returns></returns>
    public static async Task WaitForClose(this System.Net.WebSockets.WebSocket socket, Action<String> onReceive, CancellationTokenSource source)
    {
        try
        {
            var buf = new Byte[4 * 1024];
            while (!source.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                var data = await socket.ReceiveAsync(new ArraySegment<Byte>(buf), source.Token);
                if (data.MessageType == System.Net.WebSockets.WebSocketMessageType.Close) break;
                if (data.MessageType == System.Net.WebSockets.WebSocketMessageType.Text)
                {
                    var str = buf.ToStr(null, 0, data.Count);
                    if (!str.IsNullOrEmpty())
                        onReceive?.Invoke(str);
                }
            }

            if (!source.IsCancellationRequested) source.Cancel();

            if (socket.State == WebSocketState.Open)
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "finish", default);
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
#endif
    #endregion
}