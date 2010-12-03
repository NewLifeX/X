using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NewLife.Messaging
{
    /// <summary>
    /// 消息处理器
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// 处理消息
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="stream">数据流，已经从里面读取消息实体</param>
        void Process(Message message, Stream stream);
    }
}