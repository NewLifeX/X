using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using NewLife.Messaging;
using System.IO;
using NewLife.Web;
using NewLife.Net.Sockets;

namespace NewLife.PeerToPeer.Messages
{
    /// <summary>
    /// Ping消息
    /// </summary>
    public class PingMessage : CommandMessageBase<PingMessage>
    {
        /// <summary>消息类型</summary>
        public override MessageTypes MessageType { get { return MessageTypes.Ping; } }

        #region 属性
        private List<IPAddress> _Private;
        /// <summary>私有地址</summary>
        /// <remarks>客户端自己填写的地址</remarks>
        public virtual List<IPAddress> Private
        {
            get { return _Private; }
            set { _Private = value; }
        }
        #endregion


        #region 响应
        /// <summary>
        /// 邀请响应
        /// </summary>
        public class Response : CommandMessageBase<Response>
        {
            /// <summary>消息类型</summary>
            public override MessageTypes MessageType { get { return MessageTypes.PingResponse; } }

            private List<IPAddress> _Private;
            /// <summary>对方的私有地址</summary>
            public List<IPAddress> Private
            {
                get { return _Private; }
                set { _Private = value; }
            }

            private IPEndPoint _Public;
            /// <summary>我的公有地址</summary>
            public IPEndPoint Public
            {
                get { return _Public; }
                set { _Public = value; }
            }

            private List<Peer> _Friends;
            /// <summary>好友</summary>
            public List<Peer> Friends
            {
                get { return _Friends; }
                set { _Friends = value; }
            }
        }
        #endregion
    }
}
