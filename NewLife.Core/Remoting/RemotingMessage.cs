using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Messaging;
using NewLife.IO;

namespace NewLife.Remoting
{
    /// <summary>
    /// 远程消息
    /// </summary>
    public abstract class RemotingMessage : Message
    {
        #region 属性
        /// <summary>
        /// 消息唯一编码
        /// </summary>
        public override int ID
        {
            get { return (Int32)MessageType; }
        }

        /// <summary>消息类型</summary>
        public abstract RemotingMessageType MessageType { get; }
        #endregion

        #region 构造
        //static RemotingMessage()
        //{
        //    // 注册远程消息的数据流处理器
        //    StreamHandler.Register(StreamHandlerName, new RemotingStreamHandler(), false);
        //}

        ///// <summary>
        ///// 数据流处理器名称
        ///// </summary>
        //public new const String StreamHandlerName = "Remoting";
        #endregion
    }
}