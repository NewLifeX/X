using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
using NewLife.Serialization;

namespace NewLife.Http
{
    /// <summary>Http会话</summary>
    public class HttpSession : NetSession
    {
        #region 属性
        /// <summary>请求</summary>
        public HttpRequest Request { get; set; }

        /// <summary>忽略的头部</summary>
        public static String[] ExcludeHeaders { get; set; } = new[] {
            "traceparent", "Authorization", "Cookie"
        };

        /// <summary>支持作为标签数据的内容类型</summary>
        public static String[] TagTypes { get; set; } = new[] {
            "text/plain", "text/xml", "application/json", "application/xml", "application/x-www-form-urlencoded"
        };

        private WebSocket _websocket;
        private MemoryStream _cache;
        #endregion

        #region 收发数据
        /// <summary>收到客户端发来的数据</summary>
        /// <param name="e"></param>
        protected override void OnReceive(ReceivedEventArgs e)
        {
            if (e.Packet.Total == 0 /*|| !HttpBase.FastValidHeader(e.Packet)*/)
            {
                base.OnReceive(e);
                return;
            }

            // WebSocket 数据
            if (_websocket != null)
            {
                _websocket.Process(e.Packet);

                base.OnReceive(e);
                return;
            }

            var req = Request;
            var request = new HttpRequest();
            if (request.Parse(e.Packet))
            {
                req = Request = request;

                WriteLog("{0} {1}", request.Method, request.RequestUri);

                _websocket = null;
                OnNewRequest(request, e);

                // 后面还有数据包，克隆缓冲区
                if (req.IsCompleted)
                    _cache = null;
                else
                {
                    // 限制最大请求为1G
                    if (req.ContentLength > 1 * 1024 * 1024 * 1024)
                    {
                        var rs = new HttpResponse { StatusCode = HttpStatusCode.RequestEntityTooLarge };
                        Send(rs.Build());

                        Dispose();

                        return;
                    }

                    _cache = new MemoryStream(req.ContentLength);
                    req.Body.CopyTo(_cache);
                    //req.Body = req.Body.Clone();
                }
            }
            else if (req != null)
            {
                // 链式数据包
                //req.Body.Append(e.Packet.Clone());
                e.Packet.CopyTo(_cache);

                if (_cache.Length >= req.ContentLength)
                {
                    _cache.Position = 0;
                    req.Body = new Packet(_cache);
                    _cache = null;
                }
            }

            // 收到全部数据后，触发请求处理
            if (req != null && req.IsCompleted)
            {
                var rs = ProcessRequest(req, e);
                if (rs != null)
                {
                    var server = (this as INetSession).Host as HttpServer;
                    if (!server.ServerName.IsNullOrEmpty() && !rs.Headers.ContainsKey("Server")) rs.Headers["Server"] = server.ServerName;

                    var closing = !req.KeepAlive && _websocket == null;
                    if (closing && !rs.Headers.ContainsKey("Connection")) rs.Headers["Connection"] = "close";

                    Send(rs.Build());

                    if (closing) Dispose();
                }
            }

            base.OnReceive(e);
        }

        /// <summary>收到新的Http请求，只有头部</summary>
        /// <param name="request"></param>
        /// <param name="e"></param>
        protected virtual void OnNewRequest(HttpRequest request, ReceivedEventArgs e) { }

        /// <summary>处理Http请求</summary>
        /// <param name="request"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        protected virtual HttpResponse ProcessRequest(HttpRequest request, ReceivedEventArgs e)
        {
            if (request?.RequestUri == null) return new HttpResponse { StatusCode = HttpStatusCode.NotFound };

            // 匹配路由处理器
            var server = (this as INetSession).Host as HttpServer;
            var path = request.RequestUri.OriginalString;
            var p = path.IndexOf('?');
            if (p > 0) path = path[..p];

            // 埋点
            using var span = (this as INetSession).Host.Tracer?.NewSpan(path);
            if (span != null)
            {
                // 解析上游请求链路
                span.Detach(request.Headers);

                if (span is DefaultSpan ds && ds.TraceFlag > 0)
                {
                    var tag = $"{Remote.EndPoint} {request.Method} {request.RequestUri.OriginalString}";

                    if (request.BodyLength > 0 && 
                        request.Body != null && 
                        request.Body.Total < 8 * 1024 && 
                        request.ContentType.EqualIgnoreCase(TagTypes))
                    {
                        tag += "\r\n" + request.Body.ToStr(null, 0, 1024);
                    }

                    if (tag.Length < 500)
                    {
                        var vs = request.Headers.Where(e => !e.Key.EqualIgnoreCase(ExcludeHeaders)).ToDictionary(e => e.Key, e => e.Value + "");
                        tag += "\r\n" + vs.Join("\r\n", e => $"{e.Key}: {e.Value}");
                    }

                    span.SetTag(tag);
                }
            }

            // 路径安全检查，防止越界
            if (path.Contains("..")) return new HttpResponse { StatusCode = HttpStatusCode.Forbidden };

            var handler = server.MatchHandler(path);
            if (handler == null) return new HttpResponse { StatusCode = HttpStatusCode.NotFound };

            var context = new DefaultHttpContext
            {
                Connection = this,
                Request = request,
                Response = new HttpResponse(),
                Path = path,
                Handler = handler,
            };

            try
            {
                PrepareRequest(context);

                //if (span != null && context.Parameters.Count > 0) span.SetError(null, context.Parameters);

                // 处理 WebSocket 握手
                if (_websocket == null) _websocket = WebSocket.Handshake(context);

                handler.ProcessRequest(context);
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
            var url = req.RequestUri.OriginalString;
            var p = url.IndexOf('?');
            if (p > 0)
            {
                var qs = url[(p + 1)..].SplitAsDictionary("=", "&")
                    .ToDictionary(e => HttpUtility.UrlDecode(e.Key), e => HttpUtility.UrlDecode(e.Value));
                ps.Merge(qs);
            }

            // POST提交参数，支持Url编码、表单提交、Json主体
            if (req.Method == "POST" && req.BodyLength > 0)
            {
                var body = req.Body;
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
                else if (body[0] == (Byte)'{' && body[body.Total - 1] == (Byte)'}')
                {
                    var js = body.ToStr().DecodeJson();
                    ps.Merge(js);
                }
            }
        }
        #endregion
    }
}
