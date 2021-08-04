using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewLife.Http
{
    /// <summary>WebSocket会话管理</summary>
    public class WebSocketManager
    {
        /// <summary>是否WebSocket连接</summary>
        public Boolean IsWebSocketRequest { get; set; }

        /// <summary>消息处理器</summary>
        public Action<WebSocketMessage> Handler { get; set; }
    }
}