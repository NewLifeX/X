using System;

namespace NewLife.Messaging
{
    /// <summary>
    /// 消息工厂接口，用于创建信息实例和消息处理器
    /// </summary>
    public interface IMessageFactory
    {
        /// <summary>
        /// 创建指定编号的消息
        /// </summary>
        /// <param name="messageID"></param>
        /// <returns></returns>
        Message Create(Int32 messageID);

        /// <summary>
        /// 创建消息处理器
        /// </summary>
        /// <param name="messageID"></param>
        /// <returns></returns>
        IMessageHandler CreateHandler(Int32 messageID);
    }
}