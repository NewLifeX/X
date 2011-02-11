using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace NewLife.Remoting
{
    /// <summary>
    /// 远程消息类型
    /// </summary>
    public enum RemotingMessageType
    {
        /// <summary>
        /// 方法消息
        /// </summary>
        [Description("方法消息")]
        Method = 0,

        /// <summary>
        /// 实体消息
        /// </summary>
        [Description("实体消息")]
        Entity,
    }
}
