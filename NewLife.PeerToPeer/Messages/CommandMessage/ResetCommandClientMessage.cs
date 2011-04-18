using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NewLife.Messaging;

namespace NewLife.PeerToPeer.Messages
{
    /// <summary>
    /// 重启客户端
    /// </summary>
    public class ResetCommandClientMessage : CommandMessageBase<ResetCommandClientMessage>
    {
        #region 属性
        /// <summary>消息类型</summary>
        public override MessageTypes MessageType { get { return MessageTypes.ResetCommandClient; } }

        #endregion

        #region 响应
        /// <summary>
        /// 响应
        /// </summary>
        public class Response : CommandMessageBase<Response>
        {
            /// <summary>消息类型</summary>
            public override MessageTypes MessageType { get { return MessageTypes.ResetCommandClientResponse; } }

        }
        #endregion
    }
}