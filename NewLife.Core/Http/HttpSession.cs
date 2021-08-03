using System;
using System.Net;
using System.Net.Sockets;
using NewLife.Data;
using NewLife.Net;

namespace NewLife.Http
{
    /// <summary>Http会话</summary>
    public class HttpSession : NetSession
    {
        #region 属性
        /// <summary>是否WebSocket</summary>
        public Boolean IsWebSocket { get; set; }

        ///// <summary>是否启用SSL</summary>
        //public Boolean IsSSL { get; set; }

        /// <summary>请求</summary>
        public HttpRequest Request { get; set; }

        /// <summary>响应</summary>
        public HttpResponse Response { get; set; }
        #endregion

        #region 收发数据
        /// <summary>收到客户端发来的数据</summary>
        /// <param name="e"></param>
        protected override void OnReceive(ReceivedEventArgs e)
        {
            if (e.Packet.Total == 0 || !HttpBase.FastValidHeader(e.Packet))
            {
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
            return new HttpResponse
            {
                StatusCode = HttpStatusCode.OK,
                Body = $"Hello NewLife!".GetBytes()
            };
        }
        #endregion

        #region WebSocket
        /// <summary>检查WebSocket</summary>
        /// <param name="pk"></param>
        /// <param name="remote"></param>
        /// <returns></returns>
        protected virtual Boolean CheckWebSocket(ref Packet pk, IPEndPoint remote)
        {
            if (!IsWebSocket)
            {
                var key = Request["Sec-WebSocket-Key"];
                if (key.IsNullOrEmpty()) return false;

                WriteLog("WebSocket Handshake {0}", key);

                // 发送握手
                HttpHelper.Handshake(key, Response);
                //Send(null);

                IsWebSocket = true;
                //DisconnectWhenEmptyData = false;
            }
            else
            {
                pk = HttpHelper.ParseWS(pk);

                //return base.OnReceive(pk, remote);
            }

            return true;
        }
        #endregion
    }
}