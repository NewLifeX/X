using System.Buffers;
using System.Net;
using System.Web;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;
using NewLife.Net;
using NewLife.Serialization;

namespace NewLife.Http;

/// <summary>Http会话</summary>
/// <remarks>
/// 负责处理单个Http连接的请求和响应，支持：
/// 1. 请求解析和主体分片接收；
/// 2. WebSocket 握手和消息处理；
/// 3. 链路追踪和日志记录。
/// </remarks>
public class HttpSession : INetHandler, IDisposable
{
    #region 属性
    /// <summary>当前请求</summary>
    public HttpRequest? Request { get; set; }

    /// <summary>Http服务主机。不一定是HttpServer</summary>
    public IHttpHost? Host { get; set; }

    /// <summary>最大请求长度。单位字节，默认1G</summary>
    public Int32 MaxRequestLength { get; set; } = 1 * 1024 * 1024 * 1024;

    /// <summary>忽略的头部。在链路追踪中不记录这些头部</summary>
    public static String[] ExcludeHeaders { get; set; } = [
        "traceparent", "Authorization", "Cookie"
    ];

    /// <summary>支持作为标签数据的内容类型</summary>
    public static String[] TagTypes { get; set; } = [
        "text/plain", "text/xml", "application/json", "application/xml", "application/x-www-form-urlencoded"
    ];

    /// <summary>请求头跨轮分片缓存上限。超出丢弃，防止无效数据持续占用内存</summary>
    private const Int32 MaxHeadLength = 64 * 1024;

    private static readonly Byte[] NewLine2 = [(Byte)'\r', (Byte)'\n', (Byte)'\r', (Byte)'\n'];

    private INetSession _session = null!;
    private WebSocket? _websocket;
    private MemoryStream? _cache;
    private MemoryStream? _headCache;
    #endregion

    #region 辅助
    /// <summary>判断本轮数据是否像 HTTP 头部开头且尚不完整（无空行）：需要缓存等待后续分片</summary>
    /// <param name="pk">本轮数据包</param>
    /// <returns>是否应缓存等待后续分片</returns>
    private static Boolean IsIncompleteHead(IPacket pk)
    {
        // 首行断言跨段前缀拼读；总长与空行查找均链式感知
        Span<Byte> buf = stackalloc Byte[10];
        return HttpBase.FastValidHeader(pk.GetPrefix(buf, 10)) && pk.Total <= MaxHeadLength && pk.IndexOf(NewLine2) < 0;
    }
    #endregion

    #region 收发数据
    /// <summary>建立连接时初始化会话</summary>
    /// <param name="session">网络会话</param>
    public void Init(INetSession session)
    {
        _session = session;
        Host ??= session.Host as IHttpHost;
    }

    /// <summary>处理客户端发来的数据</summary>
    /// <param name="data">数据帧</param>
    public void Process(IData data)
    {
        var pk = data.Packet;
        if (pk == null || pk.Length == 0) return;

        // WebSocket 通道已建立，直接交给 WebSocket 处理
        if (_websocket != null)
        {
            _websocket.Process(pk);
            return;
        }

        // 取当前请求上下文引用（可能为 null）
        var req = Request;
        var request = new HttpRequest();

        // 请求头可能跨接收轮分片：有缓存时先与缓存片合并再整体解析，其余情况直接用本轮数据
        var headPk = pk;
        if (_headCache != null)
        {
            pk.CopyTo(_headCache);
            headPk = new ArrayPacket(_headCache.GetBuffer(), 0, (Int32)_headCache.Length);
        }

        if (request.Parse(headPk))
        {
            _headCache = null;
            req = Request = request;

            (_session as NetSession)?.WriteLog("{0} {1}", request.Method, request.RequestUri);

            // 分块请求体（Transfer-Encoding: chunked）暂不支持：ContentLength 会取到 -1，
            // 内容被当作“已完整”，chunk 帧本身会被当成业务参数/JSON 解析（静默错误），与前置代理共存时还有请求走私面。
            // 明确拒绝并告知客户端改用 Content-Length
            if (request.Headers.TryGetValue("Transfer-Encoding", out var te) && te != null && te.IndexOf("chunked", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var rs = new HttpResponse { StatusCode = HttpStatusCode.LengthRequired };

                using var res = rs.Build();
                _session.Send(res);
                _session.Dispose();

                return;
            }

            // 限制最大请求体
            if (req.ContentLength > MaxRequestLength)
            {
                var rs = new HttpResponse { StatusCode = HttpStatusCode.RequestEntityTooLarge };

                // 发送响应。用完后释放数据包，还给缓冲池
                using var res = rs.Build();
                _session.Send(res);
                _session.Dispose();

                return;
            }

            _websocket = null; // 新请求到来，清空 websocket 握手状态
            OnNewRequest(request, data);

            // 后面还有数据包，克隆缓冲区
            if (req.IsCompleted)
            {
                // 头部 + 空主体 或 已一次性接收完整主体
                _cache = null;
            }
            else
            {
                // 缓存流按需增长：按 ContentLength 预分配会让“声明巨大长度但不发送”的请求直接占住等量内存
                // （MaxRequestLength 默认 1GB，一条声明 1GB 的连接即可触发大对象分配）
                _cache = new MemoryStream();

                if (req.Body != null && req.Body.Length > 0)
                {
                    // 解析阶段已经截取到的主体部分先写入缓存
                    req.Body.CopyTo(_cache);
                    req.Body.TryDispose();
                    req.Body = null;
                }
            }
        }
        else if (req != null && _cache != null)
        {
            // 已有正在接收的请求，继续拼接主体
            pk.CopyTo(_cache);

            // 防御：主体量达到声明长度视为完成；若本轮数据超出声明长度，超出部分会一并进入主体缓存
            // （已知局限：不支持 HTTP 流水线请求，超出字节不会拆给下一请求）
            if (_cache.Length >= req.ContentLength)
            {
                _cache.Position = 0;
                req.Body = new ArrayPacket(_cache);
                _cache = null;
            }
        }
        else if (_headCache != null)
        {
            // 缓存的头部片加本轮数据仍不能成头：含完整空行仍解析失败（无效请求头）或超限，均丢弃；否则继续等后续分片
            if (_headCache.Length > MaxHeadLength || headPk.IndexOf(NewLine2) >= 0) _headCache = null;
        }
        else if (IsIncompleteHead(pk))
        {
            // 无活动请求，本轮数据像 HTTP 开头但头部不含空行（不完整）：缓存等待后续分片（请求头跨轮）
            _headCache = new MemoryStream();
            pk.CopyTo(_headCache);
        }

        if (req != null)
        {
            // 改变数据
            data.Message = req;
            data.Packet = req.Body; // 仅在收到完整主体后非空
        }

        // 主体接收完成，触发业务处理
        if (req != null && req.IsCompleted)
        {
            var rs = ProcessRequest(req, data);

            // 请求已交付处理：清除当前请求引用，防止后续轮次（新请求头分片/残片数据）再次处理旧请求（重放）
            if (ReferenceEquals(Request, req)) Request = null;

            if (rs != null)
            {
                var server = _session.Host as HttpServer;
                if (server != null && !server.ServerName.IsNullOrEmpty() && !rs.Headers.ContainsKey("Server"))
                    rs.Headers["Server"] = server.ServerName;

                // 响应版本跟随请求：HTTP/1.0 客户端若收到 HTTP/1.1 响应，会按 1.1 语义（默认 keep-alive）解析，导致连接失步
                if (req != null && !req.Version.IsNullOrEmpty()) rs.Version = req.Version;

                // 请求为空（理论上不会发生）按关闭处理，避免响应无主连接悬留
                var closing = req == null || (!req.KeepAlive && _websocket == null);
                if (closing && !rs.Headers.ContainsKey("Connection")) rs.Headers["Connection"] = "close";

                // HEAD 请求：不得返回实体，但仍需声明实体长度，否则 keep-alive 连接上客户端会把后续响应当成本次实体（连接失步）
                if (req != null && req.Method.EqualIgnoreCase("HEAD") && rs.BodyStream == null)
                {
                    using var res = rs.BuildHeaderPacket(rs.Body?.Total ?? 0);
                    _session.Send(res);
                }
                // 流式响应体：先发头部，再流式发送主体（已知长度走 Content-Length，未知走分块传输）
                else if (rs.BodyStream is { } stream)
                    SendStreamBody(rs, stream);
                else
                {
                    // 发送响应。用完后释放数据包，还给缓冲池
                    using var res = rs.Build();
                    _session.Send(res);
                }

                // 响应体所有权随响应：Build 已把体写入发送缓冲，此处归还其（可能池化的）引用
                rs.Body.TryDispose();
                rs.Body = null;

                if (closing) _session.Dispose();
            }
        }

        // 请求结束后释放主体（响应发送后即可释放）
        if (req != null)
        {
            req.Body.TryDispose();
            req.Body = null;
        }
    }

    /// <summary>发送流式响应体。先发头部，再分块读取并发送主体</summary>
    /// <param name="rs">响应</param>
    /// <param name="stream">主体数据流（所有权随响应，发送完成后释放）</param>
    private void SendStreamBody(HttpResponse rs, Stream stream)
    {
        try
        {
            // 长度可知（可寻址流）：声明 Content-Length；否则分块传输
            var length = -1L;
            if (stream.CanSeek)
            {
                try { length = stream.Length - stream.Position; } catch { length = -1; }
            }

            var chunked = length < 0;
            if (chunked) rs.Headers["Transfer-Encoding"] = "chunked";

            using var head = rs.BuildHeaderPacket(length);
            _session.Send(head);

            using var buffer = Pool.Rent(64 * 1024);

            while (true)
            {
                var count = stream.Read(buffer, 0, buffer.Length);
                if (count <= 0) break;

                if (chunked)
                {
                    // 分块传输：十六进制长度 CRLF + 数据 + CRLF 组装为单包
                    using var pk = BuildChunk(buffer, count);
                    _session.Send(pk);
                }
                else
                {
                    _session.Send(buffer, 0, count);
                }
            }

            // 终止块
            if (chunked) _session.Send("0\r\n\r\n");
        }
        catch (Exception ex)
        {
            // 发送失败（含流已被释放）：记录并关闭连接，客户端可感知响应不完整
            (_session as NetSession)?.WriteLog("流式发送失败 {0}", ex.Message);
            _session.Dispose();
        }
        finally
        {
            stream.Dispose();
        }
    }

    /// <summary>组装分块传输数据块（十六进制长度 CRLF + 数据 CRLF）</summary>
    /// <param name="data">数据缓冲</param>
    /// <param name="count">有效字节数</param>
    /// <returns>分块数据包，调用方负责 Dispose</returns>
    private static IOwnerPacket BuildChunk(Byte[] data, Int32 count)
    {
        var hex = count.ToString("X");
        var size = hex.Length + 2 + count + 2;

        var pk = new OwnerPacket(size);
        var span = pk.GetSpan();

        for (var i = 0; i < hex.Length; i++) span[i] = (Byte)hex[i];
        var p = hex.Length;
        span[p++] = 13;
        span[p++] = 10;
        data.AsSpan(0, count).CopyTo(span[p..]);
        p += count;
        span[p++] = 13;
        span[p] = 10;

        return pk;
    }

    /// <summary>收到新的Http请求（仅请求头解析完成时触发）</summary>
    /// <param name="request">请求</param>
    /// <param name="data">原始数据帧</param>
    protected virtual void OnNewRequest(HttpRequest request, IData data) { }

    /// <summary>处理Http请求</summary>
    /// <param name="request">请求</param>
    /// <param name="data">数据帧</param>
    /// <returns>响应</returns>
    protected virtual HttpResponse ProcessRequest(HttpRequest request, IData data)
    {
        if (request?.RequestUri == null) return new HttpResponse { StatusCode = HttpStatusCode.NotFound };

        //// 提取路径（不含查询）。使用 AbsolutePath 而非手动截取，避免奇异 ? 位置问题
        //var rawUri = request.RequestUri;
        //var path = rawUri.AbsolutePath; // AbsolutePath 已经处理百分号编码解码语义由下游决定
        // 匹配路由处理器。rawUri 没有Host部分，导致取 AbsolutePath 时报错
        var path = request.RequestUri.OriginalString;
        var p = path.IndexOf('?');
        if (p > 0) path = path[..p];

        // 路径安全检查
        if (!IsPathSafe(path)) return new HttpResponse { StatusCode = HttpStatusCode.Forbidden };

        // 埋点
        using var span = _session.Host.Tracer?.NewSpan(path);
        if (span != null)
        {
            span.Tag = $"{_session.Remote.EndPoint} {request.Method} {request.RequestUri}";
            span.Detach(request.Headers);
            span.Value = request.ContentLength;

            if (span is DefaultSpan ds && ds.TraceFlag > 0)
            {
                AppendSpanTag(span, request);
            }
        }

        // 匹配路由处理器。优先使用增强匹配以支持参数化路由
        var parameters = new Dictionary<String, Object?>();
        IHttpHandler? handler;

        if (Host is HttpServer server)
            handler = server.MatchHandler(path, request, parameters);
        else
            handler = Host?.MatchHandler(path, request);

        var context = new DefaultHttpContext(_session, request, path, handler)
        {
            ServiceProvider = _session as IServiceProvider
        };

        // 路由参数在 PrepareRequest 之后再合并（见下方）：路径是本请求的身份，不应被 ?id= 或表单字段覆盖

        // 创建请求级作用域，注册 IHttpContext 使构造函数 DI 可获取；请求结束（finally）释放作用域
        IServiceScope? scope = null;
        var sp = context.ServiceProvider;
        if (sp != null)
        {
            scope = sp.CreateScope();
            if (scope is IServiceRegistry registry)
            {
                registry.TryAdd(typeof(IHttpContext), context);
                context.ServiceProvider = scope.ServiceProvider;
            }
        }

        // 设置为当前上下文，便于静态访问
        DefaultHttpContext.Current = context;

        try
        {
            PrepareRequest(context);

            // 路由参数最后合并：路径参数优先于查询串/请求体参数。旧顺序下 /api/users/5?id=999 会让 id 变成 999，
            // 既是语义错误（路径应优先），也是参数篡改面（下游按 id 取数/鉴权会被绕过）
            foreach (var kv in parameters)
            {
                context.Parameters[kv.Key] = kv.Value;
            }

            // 处理 WebSocket 握手（只在第一次调用时尝试）
            _websocket ??= WebSocket.Handshake(context);

            if (handler != null)
            {
                // 通过 HttpServer 的中间件管道执行处理器
                if (Host is HttpServer svr)
                    svr.ExecutePipeline(context, handler).GetAwaiter().GetResult();
                else
                    handler.ProcessRequest(context);
            }
            else if (_websocket == null)
                return new HttpResponse { StatusCode = HttpStatusCode.NotFound };

            // 根据状态码识别异常
            if (span != null)
            {
                var res = context.Response;
                span.Value += res.ContentLength;
                var code = res.StatusCode;
                if (code == HttpStatusCode.BadRequest || code > HttpStatusCode.NotFound)
                    span.SetError(new HttpRequestException($"Http Error {(Int32)code} {code}"), null);
            }
        }
        catch (HttpException hex)
        {
            span?.SetError(hex, null);
            context.Response.StatusCode = hex.StatusCode;
            context.Response.StatusDescription = hex.Message;
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            context.Response.SetResult(ex);
        }
        finally
        {
            // 清理当前上下文，避免泄漏到后续请求
            DefaultHttpContext.Current = null;

            // 释放请求级作用域（scoped 服务的释放语义随 ServiceProvider 实现）
            scope?.Dispose();
        }

        return context.Response;
    }

    /// <summary>向链路追踪Span追加标签信息</summary>
    /// <param name="span">追踪Span</param>
    /// <param name="request">Http请求</param>
    private void AppendSpanTag(ISpan span, HttpRequest request)
    {
        var includeBody = false;
        var bodyLength = request.Body?.Total ?? 0;
        if (request.BodyLength > 0 && request.Body != null && bodyLength > 0 && bodyLength < 8 * 1024 && request.ContentType.EqualIgnoreCase(TagTypes))
        {
            // 主体可能为链式：链感知截断读取（最多 1024 字节）
            span.AppendTag("\r\n<=\r\n" + request.Body.ToStr(null, 0, 1024));
            includeBody = true;
        }

        if (span.Tag == null || span.Tag.Length < 500)
        {
            if (!includeBody) span.AppendTag("\r\n<=");
            var vs = request.Headers.Where(e => !e.Key.EqualIgnoreCase(ExcludeHeaders)).ToDictionary(e => e.Key, e => e.Value + "");
            span.AppendTag("\r\n" + vs.Join(Environment.NewLine, e => $"{e.Key}:{e.Value}"));
        }
        else if (!includeBody)
        {
            span.AppendTag("\r\n<=\r\n");
            span.AppendTag($"ContentLength: {request.ContentLength}\r\n");
            span.AppendTag($"ContentType: {request.ContentType}");
        }
    }

    /// <summary>简单路径安全检查，防止目录穿越和空字节注入</summary>
    /// <param name="path">请求路径</param>
    /// <returns>路径是否安全</returns>
    private static Boolean IsPathSafe(String path) => path.IndexOf("..", StringComparison.Ordinal) < 0 && path.IndexOf('\0') < 0;

    /// <summary>准备请求参数</summary>
    /// <param name="context">Http上下文</param>
    protected virtual void PrepareRequest(IHttpContext context)
    {
        var req = context.Request;
        var ps = context.Parameters;

        // 注入 IHttpContext 和 IServiceProvider 到参数列表（隐藏参数，供下游按名称匹配）
        ps["__context"] = context;
        ps["__serviceProvider"] = context.ServiceProvider;

        // 解析地址参数
        var uri = req.RequestUri;
        if (uri == null) return;

        var url = uri.OriginalString;
        var p = url.IndexOf('?');
        if (p > 0)
        {
            var qs = url[(p + 1)..].SplitAsDictionary("=", "&")
                .ToDictionary(e => HttpUtility.UrlDecode(e.Key), e => HttpUtility.UrlDecode(e.Value));
            ps.Merge(qs);
        }

        // POST 提交参数：Url编码、表单、Json
        if (req.Method == "POST" && req.BodyLength > 0 && req.Body != null)
        {
            ParsePostBody(req, ps);
        }
    }

    /// <summary>解析POST请求体参数</summary>
    /// <param name="req">Http请求</param>
    /// <param name="ps">参数字典</param>
    private void ParsePostBody(HttpRequest req, IDictionary<String, Object?> ps)
    {
        // 主体可能为链式（跨接收段）：统一用链感知读取
        var body = req.Body!;
        if (req.ContentType.StartsWithIgnoreCase("application/x-www-form-urlencoded", "application/x-www-urlencoded"))
        {
            var qs = body.ToStr().SplitAsDictionary("=", "&")
                .ToDictionary(e => HttpUtility.UrlDecode(e.Key), e => HttpUtility.UrlDecode(e.Value));
            ps.Merge(qs);
        }
        else if (req.ContentType.StartsWithIgnoreCase("multipart/form-data;"))
        {
            var dic = req.ParseFormData();
            var fs = dic.Values.Where(e => e is FormFile).Cast<FormFile>().ToArray();
            if (fs.Length > 0) req.Files = fs;
            ps.Merge(dic);
        }
        else if (body.Total >= 2 && body[0] == (Byte)'{' && body[body.Total - 1] == (Byte)'}')
        {
            var js = body.ToStr().DecodeJson();
            if (js != null) ps.Merge(js);
        }
    }
    #endregion

    #region 销毁
    /// <summary>销毁。释放请求头/体缓存与 WebSocket 粘包编码器（归还段链池缓冲）</summary>
    public void Dispose()
    {
        _cache?.Dispose();
        _cache = null;

        _headCache?.Dispose();
        _headCache = null;

        _websocket?.Dispose();
        _websocket = null;
    }
    #endregion
}
