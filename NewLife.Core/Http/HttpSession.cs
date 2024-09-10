using System.Net;
using System.Web;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
using NewLife.Serialization;

namespace NewLife.Http;

/// <summary>Http会话</summary>
public class HttpSession : INetHandler
{
    #region 属性
    /// <summary>请求</summary>
    public HttpRequest? Request { get; set; }

    /// <summary>Http服务主机。不一定是HttpServer</summary>
    public IHttpHost? Host { get; set; }

    /// <summary>最大请求长度。单位字节，默认1G</summary>
    public Int32 MaxRequestLength { get; set; } = 1 * 1024 * 1024 * 1024;

    /// <summary>忽略的头部</summary>
    public static String[] ExcludeHeaders { get; set; } = [
        "traceparent", "Authorization", "Cookie"
    ];

    /// <summary>支持作为标签数据的内容类型</summary>
    public static String[] TagTypes { get; set; } = [
        "text/plain", "text/xml", "application/json", "application/xml", "application/x-www-form-urlencoded"
    ];

    private INetSession _session = null!;
    private WebSocket? _websocket;
    private MemoryStream? _cache;
    #endregion

    #region 收发数据
    /// <summary>建立连接时初始化会话</summary>
    /// <param name="session">会话</param>
    public void Init(INetSession session)
    {
        _session = session;
        Host ??= session.Host as IHttpHost;
    }

    /// <summary>处理客户端发来的数据</summary>
    /// <param name="data"></param>
    public void Process(IData data)
    {
        var pk = data.Packet;
        if (pk == null || pk.Total == 0) return;

        // WebSocket 数据
        if (_websocket != null)
        {
            _websocket.Process(pk);

            //base.OnReceive(e);
            return;
        }

        // 解码请求头，单个连接可能有多个请求
        var req = Request;
        var request = new HttpRequest();
        if (request.Parse(pk))
        {
            req = Request = request;

            (_session as NetSession)?.WriteLog("{0} {1}", request.Method, request.RequestUri);

            _websocket = null;
            OnNewRequest(request, data);

            // 后面还有数据包，克隆缓冲区
            if (req.IsCompleted)
                _cache = null;
            else
            {
                // 限制最大请求为1G
                if (req.ContentLength > MaxRequestLength)
                {
                    var rs = new HttpResponse { StatusCode = HttpStatusCode.RequestEntityTooLarge };
                    _session.Send(rs.Build());

                    _session.Dispose();

                    return;
                }

                _cache = new MemoryStream(req.ContentLength);
                req.Body?.CopyTo(_cache);
                //req.Body = req.Body.Clone();
            }
        }
        else if (req != null)
        {
            if (_cache != null)
            {
                // 链式数据包
                //req.Body.Append(pk.Clone());
                pk.CopyTo(_cache);

                if (_cache.Length >= req.ContentLength)
                {
                    _cache.Position = 0;
                    req.Body = new ArrayPacket(_cache);
                    _cache = null;
                }
            }
        }

        if (req != null)
        {
            // 改变数据
            data.Message = req;
            data.Packet = req.Body as Packet;
        }

        // 收到全部数据后，触发请求处理
        if (req != null && req.IsCompleted)
        {
            var rs = ProcessRequest(req, data);
            if (rs != null)
            {
                var server = _session.Host as HttpServer;
                if (server != null && !server.ServerName.IsNullOrEmpty() && !rs.Headers.ContainsKey("Server"))
                    rs.Headers["Server"] = server.ServerName;

                var closing = !req.KeepAlive && _websocket == null;
                if (closing && !rs.Headers.ContainsKey("Connection")) rs.Headers["Connection"] = "close";

                _session.Send(rs.Build());

                if (closing) _session.Dispose();
            }
        }
    }

    /// <summary>收到新的Http请求，只有头部</summary>
    /// <param name="request"></param>
    /// <param name="data"></param>
    protected virtual void OnNewRequest(HttpRequest request, IData data) { }

    /// <summary>处理Http请求</summary>
    /// <param name="request"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    protected virtual HttpResponse ProcessRequest(HttpRequest request, IData data)
    {
        if (request?.RequestUri == null) return new HttpResponse { StatusCode = HttpStatusCode.NotFound };

        // 匹配路由处理器
        var path = request.RequestUri.OriginalString;
        var p = path.IndexOf('?');
        if (p > 0) path = path[..p];

        // 埋点
        using var span = _session.Host.Tracer?.NewSpan(path);
        if (span != null)
        {
            span.Tag = $"{_session.Remote.EndPoint} {request.Method} {request.RequestUri}";
            span.Detach(request.Headers);
            span.Value = request.ContentLength;

            if (span is DefaultSpan ds && ds.TraceFlag > 0)
            {
                var flag = false;
                if (request.BodyLength > 0 &&
                    request.Body != null &&
                    request.Body.Length < 8 * 1024 &&
                    request.ContentType.EqualIgnoreCase(TagTypes))
                {
                    var body = request.Body.GetSpan();
                    if (body.Length > 1024) body = body[..1024];
                    span.AppendTag("\r\n<=\r\n" + body.ToStr(null));
                    flag = true;
                }

                if (span.Tag.Length < 500)
                {
                    if (!flag) span.AppendTag("\r\n<=");
                    var vs = request.Headers.Where(e => !e.Key.EqualIgnoreCase(ExcludeHeaders)).ToDictionary(e => e.Key, e => e.Value + "");
                    span.AppendTag("\r\n" + vs.Join(Environment.NewLine, e => $"{e.Key}:{e.Value}"));
                }
                else if (!flag)
                {
                    span.AppendTag("\r\n<=\r\n");
                    span.AppendTag($"ContentLength: {request.ContentLength}\r\n");
                    span.AppendTag($"ContentType: {request.ContentType}");
                }
            }
        }

        // 路径安全检查，防止越界
        if (path.Contains("..")) return new HttpResponse { StatusCode = HttpStatusCode.Forbidden };

        var handler = Host?.MatchHandler(path, request);
        //if (handler == null) return new HttpResponse { StatusCode = HttpStatusCode.NotFound };

        var context = new DefaultHttpContext(_session, request, path, handler)
        {
            ServiceProvider = _session as IServiceProvider
        };

        try
        {
            PrepareRequest(context);

            //if (span != null && context.Parameters.Count > 0) span.SetError(null, context.Parameters);

            // 处理 WebSocket 握手
            _websocket ??= WebSocket.Handshake(context);

            if (handler != null)
                handler.ProcessRequest(context);
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
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            context.Response.SetResult(ex);
        }

        return context.Response;
    }

    /// <summary>准备请求参数</summary>
    /// <param name="context"></param>
    protected virtual void PrepareRequest(IHttpContext context)
    {
        var req = context.Request;
        var ps = context.Parameters;

        //// 头部参数
        //ps.Merge(req.Headers);

        // 地址参数
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

        // POST提交参数，支持Url编码、表单提交、Json主体
        if (req.Method == "POST" && req.BodyLength > 0 && req.Body != null)
        {
            var body = req.Body.GetSpan();
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
            else if (body[0] == (Byte)'{' && body[^1] == (Byte)'}')
            {
                var js = body.ToStr().DecodeJson();
                if (js != null) ps.Merge(js);
            }
        }
    }
    #endregion
}
