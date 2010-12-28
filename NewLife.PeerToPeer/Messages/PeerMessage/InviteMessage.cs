using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;

namespace NewLife.PeerToPeer.Messages
{
    /// <summary>
    /// 邀请消息。我告诉你我的完成度，你也告诉我你的完成度！
    /// </summary>
    public class InviteMessage : PeerMessageBase<InviteMessage>
    {
        /// <summary>消息类型</summary>
        public override MessageTypes MessageType { get { return MessageTypes.Invite; } }

        #region 属性
        //private List<IPEndPoint> _Private;
        ///// <summary>我的私有地址</summary>
        //public List<IPEndPoint> Private
        //{
        //    get { return _Private; }
        //    set { _Private = value; }
        //}

        private Double _Complete;
        /// <summary>完成度</summary>
        public Double Complete
        {
            get { return _Complete; }
            set { _Complete = value; }
        }

        #endregion

        #region 响应
        /// <summary>
        /// 邀请响应
        /// </summary>
        public class Response : PeerMessageBase<Response>
        {
            /// <summary>消息类型</summary>
            public override MessageTypes MessageType { get { return MessageTypes.InviteResponse; } }

            private Double _Complete;
            /// <summary>完成度</summary>
            public Double Complete
            {
                get { return _Complete; }
                set { _Complete = value; }
            }

            //private List<IPEndPoint> _Private;
            ///// <summary>对方的私有地址</summary>
            //public List<IPEndPoint> Private
            //{
            //    get { return _Private; }
            //    set { _Private = value; }
            //}

            //private IPEndPoint _Public;
            ///// <summary>我的公有地址</summary>
            //public IPEndPoint Public
            //{
            //    get { return _Public; }
            //    set { _Public = value; }
            //}

            //private Dictionary<Guid, Peer> _Friends;
            ///// <summary>对方的好友列表</summary>
            //public Dictionary<Guid, Peer> Friends
            //{
            //    get { return _Friends; }
            //    set { _Friends = value; }
            //}
        }
        #endregion

    }
}