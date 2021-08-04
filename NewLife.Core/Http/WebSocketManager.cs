using System;
using System.Net;
using System.Security.Cryptography;
using NewLife.Data;

namespace NewLife.Http
{
    /// <summary>WebSocket会话管理</summary>
    public class WebSocketManager
    {
        #region 属性
        /// <summary>是否WebSocket连接</summary>
        public Boolean IsWebSocketRequest { get; set; }

        /// <summary>消息处理器</summary>
        public Action<WebSocketMessage> Handler { get; set; }

        /// <summary>Http上下文</summary>
        public IHttpContext Context { get; set; }
        #endregion

        #region 方法
        /// <summary>WebSocket 握手</summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static WebSocketManager Handshake(IHttpContext context)
        {
            var request = context.Request;
            if (!request.Headers.TryGetValue("Sec-WebSocket-Key", out var key) || key.IsNullOrEmpty()) return null;

            var buf = SHA1.Create().ComputeHash((key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11").GetBytes());
            key = buf.ToBase64();

            var response = context.Response;
            response.StatusCode = HttpStatusCode.SwitchingProtocols;
            response.Headers["Upgrade"] = "websocket";
            response.Headers["Connection"] = "Upgrade";
            response.Headers["Sec-WebSocket-Accept"] = key;

            var manager = new WebSocketManager
            {
                IsWebSocketRequest = true,
                Context = context
            };
            if (context is DefaultHttpContext dhc) dhc.WebSockets = manager;

            return manager;
        }

        /// <summary>处理WebSocket数据包</summary>
        /// <param name="pk"></param>
        public void Process(Packet pk)
        {
            if (Handler != null)
            {
                var message = new WebSocketMessage();
                if (message.Read(pk))
                {
                    Handler(message);

                    switch (message.Type)
                    {
                        case WebSocketMessageType.Close:
                            Context.Connection.Dispose();
                            break;
                        //case WebSocketMessageType.Ping:
                        //    break;
                        //case WebSocketMessageType.Pong:
                        //    break;
                    }
                }
            }
        }

        /// <summary>发送消息</summary>
        /// <param name="type"></param>
        /// <param name="data"></param>
        public void Send(WebSocketMessageType type, Packet data)
        {
            var msg = new WebSocketMessage { Type = type, Payload = data };
            var session = Context.Connection;
            session.Send(msg.ToPacket());
        }

        /// <summary>发送文本消息</summary>
        /// <param name="message"></param>
        public void Send(String message) => Send(WebSocketMessageType.Text, message.GetBytes());
        #endregion
    }
}