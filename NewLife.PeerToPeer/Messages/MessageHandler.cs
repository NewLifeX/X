//using System;
//using System.Collections.Generic;
//using System.Text;
//using NewLife.Messaging;
//using System.IO;

//namespace NewLife.PeerToPeer.Messages
//{
//    /// <summary>
//    /// 消息处理中心
//    /// </summary>
//    internal class MessageHandler : IMessageHandler
//    {
//        #region 构造
//        private static MessageHandler _Instance;
//        /// <summary>唯一实例</summary>
//        public static MessageHandler Instance
//        {
//            get { return _Instance ?? (_Instance = new MessageHandler()); }
//        }

//        private MessageHandler() { }
//        #endregion

//        #region 消息处理器
//        /// <summary>
//        /// 消息到达事件
//        /// </summary>
//        public event EventHandler<EventArgs<Message, Stream>> MessageReceived;

//        void IMessageHandler.Process(Message message, Stream stream)
//        {
//            if (MessageReceived != null) MessageReceived(this, new EventArgs<Message, Stream>(message, stream));
//        }

//        #endregion
//    }
//}