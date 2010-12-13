using System;
using System.IO;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.PeerToPeer.Messages;
using System.Collections.Generic;
//using NewLife.PeerToPeer.Online;

namespace NewLife.PeerToPeer.Server
{
    /// <summary>
    /// 命令服务器
    /// </summary>
    public class CommandServer : MessageServer
    {
        #region 属性
        //private PeerOnline _OnlineUser;
        ///// <summary>在线用户</summary>
        //public PeerOnline OnlineUser
        //{
        //    get { return _OnlineUser; }
        //    set { _OnlineUser = value; }
        //}

        #endregion

        #region 处理
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnReceived(object sender, EventArgs<Message, Stream> e)
        {
            P2PMessage msg = e.Arg1 as P2PMessage;
            if (msg == null) return;

            switch (msg.MessageType)
            {
                case MessageTypes.Test:
                    if (Test != null) Test(sender, e);
                    break;
                case MessageTypes.Ping:
                    if (Ping != null) Ping(sender, e);
                    break;
                case MessageTypes.FindTorrent:
                    if (FindTorrent != null) FindTorrent(sender, e);
                    break;
                case MessageTypes.Text:
                    if (Text != null) Text(sender, e);
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region 事件
        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<EventArgs<Message, Stream>> Ping;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<EventArgs<Message, Stream>> FindTorrent;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<EventArgs<Message, Stream>> Test;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<EventArgs<Message, Stream>> Text;
        #endregion
    }
}