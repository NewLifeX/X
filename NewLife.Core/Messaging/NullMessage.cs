using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Messaging
{
    /// <summary>
    /// 空消息
    /// </summary>
    public class NullMessage : Message
    {
        /// <summary>
        /// 消息编号
        /// </summary>
        public override int ID
        {
            get { return 0xFE; }
        }
    }
}
