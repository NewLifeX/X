using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.PeerToPeer.Messages
{
    /// <summary>
    /// 抽象所有Peer之间的通讯消息
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public abstract class PeerMessageBase<TMessage> : Message<TMessage> where TMessage : Message<TMessage>, new()
    {
        #region 属性
        private Guid _TorrentToken;
        /// <summary>种子标识。所有通讯必须指定种子，否则没有任何意义</summary>
        public Guid TorrentToken
        {
            get { return _TorrentToken; }
            set { _TorrentToken = value; }
        }
        #endregion
    }
}