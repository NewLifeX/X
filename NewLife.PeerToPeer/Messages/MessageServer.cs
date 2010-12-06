using System;
using System.IO;
using NewLife.Messaging;

namespace NewLife.PeerToPeer.Messages
{
    /// <summary>
    /// 消息服务器
    /// </summary>
    public abstract class MessageServer
    {
        #region 构造
        static MessageServer()
        {
            P2PMessage.Init();
        }

        /// <summary>
        /// 实例化
        /// </summary>
        public MessageServer()
        {
            MessageHandler.Received += new EventHandler<EventArgs<Message, Stream>>(OnReceived);
        }

        /// <summary>
        /// 析构，取消事件注册
        /// </summary>
        ~MessageServer()
        {
            MessageHandler.Received -= new EventHandler<EventArgs<Message, Stream>>(OnReceived);
        }
        #endregion

        #region 处理
        /// <summary>
        /// 消息到达时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected abstract void OnReceived(object sender, EventArgs<Message, Stream> e);
        #endregion
    }
}
