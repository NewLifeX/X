using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization.Protocol
{
    /// <summary>
    /// 协议序列化事件参数
    /// </summary>
    public class ProtocolSerializedEventArgs : EventArgs
    {
        #region 属性
        private WriteContext _Context;
        /// <summary>上下文</summary>
        public WriteContext Context
        {
            get { return _Context; }
            set { _Context = value; }
        }
        #endregion

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="context"></param>
        public ProtocolSerializedEventArgs(WriteContext context)
        {
            Context = context;
        }
    }
}
