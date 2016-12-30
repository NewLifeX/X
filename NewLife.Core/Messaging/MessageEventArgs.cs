using System;
using NewLife.Data;

namespace NewLife.Messaging
{
    /// <summary>收到消息时的事件参数</summary>
    public class MessageEventArgs : EventArgs
    {
        #region 属性
        /// <summary>数据包</summary>
        public Packet Packet { get; set; }

        /// <summary>消息</summary>
        public IMessage Message { get; set; }

        /// <summary>用户数据。比如远程地址等</summary>
        public Object UserState { get; set; }
        #endregion

        #region 方法
        #endregion
    }
}