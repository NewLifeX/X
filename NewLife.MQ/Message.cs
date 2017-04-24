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

        /// <summary>标签</summary>
        public String Tag { get; set; }

        /// <summary>主体</summary>
        public Object Body { get; set; }

        /// <summary>已重载</summary>
        /// <returns></returns>
        public override String ToString()
        {
            var str = "";
            var buf = Body as Byte[];
            if (buf != null)
                str = buf.ToStr();
            else
                str = Body + "";

            return "{0}#{1}".F(Sender, str);
        }
    }
}