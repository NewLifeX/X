using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
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

        private IHttpContext _websocket;
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
                if (_websocket.WebSockets?.Handler != null)
                {
                    var message = new WebSocketMessage();
                    if (message.Read(e.Packet)) _websocket.WebSockets.Handler(message);
                }

                base.OnReceive(e);
                return;
            }

            var req = Request;
            var request = new HttpRequest();
            if (request.Parse(e.Packet))
            {
                req = Request = request;

                WriteLog("{0} {1}", request.Method, request.Url);

                OnNewRequest(request, e);
            }
            else if (req != null)
            {
                // 链式数据包
                req.Body.Append(e.Packet);
            }

            // 收到全部数据后，触发请求处理
            if (req != null && req.IsCompleted)
            {
                var rs = ProcessRequest(req, e);
                if (rs != null)
                {
                    var server = (this as INetSession).Host as HttpServer;
                    if (!server.ServerName.IsNullOrEmpty() && !rs.Headers.ContainsKey("Server")) rs.Headers["Server"] = server.ServerName;

                    Send(rs.Build());
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
            // 匹配路由处理器
            var server = (this as INetSession).Host as HttpServer;
            var path = request.Url.OriginalString;
            var p = path.IndexOf('?');
            if (p > 0) path = path.Substring(0, p);

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

                // 处理 WebSocket 握手
                if (_websocket == null) Handshake(context);

                handler.ProcessRequest(context);
            }
            catch (Exception ex)
            {
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
            var url = req.Url.OriginalString;
            var p = url.IndexOf('?');
            if (p > 0)
            {
                var qs = url.Substring(p + 1).SplitAsDictionary("=", "&");
                ps.Merge(qs);
            }

            // 提交参数
            if (req.Method == "POST" && req.BodyLength > 0)
            {
                var body = req.Body;
                if (body[0] == (Byte)'{' && body[body.Total - 1] == (Byte)'}')
                {
                    var js = JsonParser.Decode(body.ToStr());
                    ps.Merge(js);
                }
            }
        }
        #endregion

        #region WebSocket
        /// <summary>WebSocket 握手</summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected virtual void Handshake(IHttpContext context)
        {
            var request = context.Request;
            if (!request.Headers.TryGetValue("Sec-WebSocket-Key", out var key) || key.IsNullOrEmpty()) return;

            var buf = SHA1.Create().ComputeHash((key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11").GetBytes());
            key = buf.ToBase64();

            var response = context.Response;
            response.StatusCode = HttpStatusCode.SwitchingProtocols;
            response.Headers["Upgrade"] = "websocket";
            response.Headers["Connection"] = "Upgrade";
            response.Headers["Sec-WebSocket-Accept"] = key;

            if (context is DefaultHttpContext dhc) dhc.WebSockets = new WebSocketManager { IsWebSocketRequest = true };
            _websocket = context;
        }
        #endregion
    }
}