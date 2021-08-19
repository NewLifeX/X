using System;
using System.Net;
using System.Security.Cryptography;
using NewLife.Data;
using NewLife.Net;
using NewLife.Security;

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

                    var session = Context.Connection;
                    switch (message.Type)
                    {
                        case WebSocketMessageType.Close:
                            {
                                //var msg = new WebSocketMessage { Type = WebSocketMessageType.Close, Payload = "Finished".GetBytes() };
                                //session.Send(msg.ToPacket());
                                Close(1000, "Finished");
                                session.Dispose();
                            }
                            break;
                        case WebSocketMessageType.Ping:
                            {
                                var msg = new WebSocketMessage { Type = WebSocketMessageType.Pong, MaskKey = Rand.NextBytes(4) };
                                session.Send(msg.ToPacket());
                            }
                            break;
                    }
                }
            }
        }

        /// <summary>发送消息</summary>
        /// <param name="data"></param>
        /// <param name="type"></param>
        public void Send(Packet data, WebSocketMessageType type)
        {
            var msg = new WebSocketMessage { Type = type, Payload = data };
            var session = Context.Connection;
            session.Send(msg.ToPacket());
        }

        /// <summary>发送文本消息</summary>
        /// <param name="message"></param>
        public void Send(String message) => Send(message.GetBytes(), WebSocketMessageType.Text);

        /// <summary>向所有连接发送消息</summary>
        /// <param name="data"></param>
        /// <param name="type"></param>
        /// <param name="predicate"></param>
        public void SendAll(Packet data, WebSocketMessageType type, Func<INetSession, Boolean> predicate = null)
        {
            var msg = new WebSocketMessage { Type = type, Payload = data };
            var session = Context.Connection;
            session.Host.SendAllAsync(msg.ToPacket(), predicate);
        }

        /// <summary>想所有连接发送文本消息</summary>
        /// <param name="message"></param>
        /// <param name="predicate"></param>
        public void SendAll(String message, Func<INetSession, Boolean> predicate = null) => SendAll(message.GetBytes(), WebSocketMessageType.Text, predicate);

        /// <summary>发送关闭连接</summary>
        /// <param name="closeStatus"></param>
        /// <param name="statusDescription"></param>
        public void Close(Int32 closeStatus, String statusDescription)
        {
            var msg = new WebSocketMessage
            {
                Type = WebSocketMessageType.Close,
                CloseStatus = closeStatus,
                StatusDescription = statusDescription
            };
            var session = Context.Connection;
            session.Send(msg.ToPacket());
        }
        #endregion
    }
}