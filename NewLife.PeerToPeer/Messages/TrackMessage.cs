using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace NewLife.PeerToPeer.Messages
{
    /// <summary>
    /// 跟踪消息
    /// </summary>
    public class TrackMessage : Message<TestMessage>
    {
        /// <summary>消息类型</summary>
        public override MessageTypes MessageType { get { return MessageTypes.Track; } }

        #region 属性
        private Guid _TorrentToken;
        /// <summary>种子标识</summary>
        public Guid TorrentToken
        {
            get { return _TorrentToken; }
            set { _TorrentToken = value; }
        }

        private Double _Complete;
        /// <summary>完成度</summary>
        public Double Complete
        {
            get { return _Complete; }
            set { _Complete = value; }
        }

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
        public class Response : Message<Response>
        {
            /// <summary>消息类型</summary>
            public override MessageTypes MessageType { get { return MessageTypes.TrackResponse; } }

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