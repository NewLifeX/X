using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using NewLife.Messaging;

namespace NewLife.PeerToPeer.Messages
{
    /// <summary>
    /// 命令服务器
    /// </summary>
    public abstract class CommandMessage : P2PMessage
    {
        private Int32 _TaskID;
        /// <summary>任务ID</summary>
        public Int32 TaskID
        {
            get { return _TaskID; }
            set { _TaskID = value; }
        }
    }

    /// <summary>
    /// 命令服务器消息基类
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public abstract class CommandMessageBase<TMessage> : CommandMessage
        where TMessage : CommandMessageBase<TMessage>, new()
    {
        #region IMessageFactory 成员
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        protected override P2PMessage Create()
        {
            return new TMessage();
        }
        #endregion
    }
}
