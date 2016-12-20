using System;
using System.ComponentModel;
using NewLife.Remoting;

namespace NewLife.MessageQueue
{
    /// <summary>消息控制器</summary>
    public class MessageController:IApi
    {
        /// <summary>Api接口会话</summary>
        public IApiSession Session { get; set; }

        /// <summary>发布消息</summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        [DisplayName("发布消息")]
        public Boolean Public(Message msg)
        {
            return true;
        }
    }
}