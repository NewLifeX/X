using System;
using System.Net;
using NewLife.Data;

namespace NewLife.Net
{
    /// <summary>收到数据时的事件参数</summary>
    public class ReceivedEventArgs : EventArgs, IData
    {
        #region 属性
        /// <summary>原始数据包</summary>
        public Packet Packet { get; set; }

        /// <summary>远程地址</summary>
        public IPEndPoint Remote { get; set; }

        /// <summary>解码后的消息</summary>
        public Object Message { get; set; }

        /// <summary>用户自定义数据</summary>
        public Object UserState { get; set; }
        #endregion
    }
}