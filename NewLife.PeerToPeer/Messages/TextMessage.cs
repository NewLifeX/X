using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;

namespace NewLife.PeerToPeer.Messages
{
    /// <summary>
    /// 邀请消息
    /// </summary>
    public class TextMessage : Message<TextMessage>
    {
        /// <summary>消息类型</summary>
        public override MessageTypes MessageType { get { return MessageTypes.Text; } }

        #region 属性
        private String _Text;
        /// <summary>我的私有地址</summary>
        public String Text
        {
            get { return _Text; }
            set { _Text = value; }
        }
        #endregion

        #region 响应
        /// <summary>
        /// 邀请响应
        /// </summary>
        public class Response : Message<Response>
        {
            /// <summary>消息类型</summary>
            public override MessageTypes MessageType { get { return MessageTypes.TextResponse; } }

            private bool _IsRequest;
            /// <summary>状态，或包号</summary>
            public bool IsRequest
            {
                get { return _IsRequest; }
                set { _IsRequest = value; }
            }
        }
        #endregion

    }
}
