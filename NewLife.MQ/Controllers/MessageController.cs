using System;
using System.ComponentModel;
using NewLife.Log;
using NewLife.Remoting;

namespace NewLife.MessageQueue
{
    /// <summary>消息控制器</summary>
    public class MessageController : IApi
    {
        /// <summary>Api接口会话</summary>
        public IApiSession Session { get; set; }

        /// <summary>当前上下文</summary>
        public ControllerContext Context { get; set; }

        /// <summary>发布消息</summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        [DisplayName("发布消息")]
        public Boolean Public(Message msg)
        {
            XTrace.WriteLine("发布消息 {0}", msg);

            var user = Session["user"] as String;

            var tp = Session["Topic"] as Topic;
            if (tp == null) throw new Exception("未订阅");

            msg.Sender = user;
            tp.Enqueue(msg);

            return true;
        }
    }
}