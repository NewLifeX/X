using System;

namespace NewLife.MessageQueue
{
    /// <summary>消息</summary>
    public class Message
    {
        /// <summary>发送者</summary>
        public String Sender { get; set; }

        /// <summary>开始时间</summary>
        public DateTime StartTime { get; set; }

        /// <summary>过期时间</summary>
        public DateTime EndTime { get; set; }

        /// <summary>主体</summary>
        public Byte[] Body { get; set; }
    }
}