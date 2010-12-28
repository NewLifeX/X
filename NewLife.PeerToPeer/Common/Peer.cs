using System;
using System.Collections.Generic;
using System.Net;

namespace NewLife.PeerToPeer
{
    /// <summary>
    /// 远程对等方
    /// </summary>
    /// <remarks>不同的相对网络类型，有不同的实现</remarks>
    public class Peer
    {
        #region 基础属性
        private Guid _Token;
        /// <summary>唯一识别码</summary>
        public Guid Token
        {
            get { return _Token; }
            set { _Token = value; }
        }

        private List<IPAddress> _Private;
        /// <summary>私有地址</summary>
        /// <remarks>客户端自己填写的地址</remarks>
        public virtual List<IPAddress> Private
        {
            get { return _Private; }
            set { _Private = value; }
        }

        private IPEndPoint _Public;
        /// <summary>公共地址</summary>
        /// <remarks>服务器检测到之后赋予它的地址</remarks>
        public virtual IPEndPoint Public
        {
            get { return _Public; }
            set { _Public = value; }
        }
        #endregion

        #region 属性
        private Double _Complete;
        /// <summary>完成度</summary>
        public Double Complete
        {
            get { return _Complete; }
            set { _Complete = value; }
        }
        
        private DateTime _InviteTime;
        /// <summary>邀请时间</summary>
        public DateTime InviteTime
        {
            get { return _InviteTime; }
            set { _InviteTime = value; }
        }

        private DateTime _ActiveTime;
        /// <summary>活动时间</summary>
        public DateTime ActiveTime
        {
            get { return _ActiveTime; }
            set { _ActiveTime = value; }
        }
        #endregion
    }
}