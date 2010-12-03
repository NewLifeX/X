using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace NewLife.Serialization.Protocol
{
    /// <summary>
    /// 协议反序列化事件参数
    /// </summary>
    public class ProtocolDeserializingEventArgs : CancelEventArgs
    {
        #region 属性
        private ReadContext _Context;
        /// <summary>上下文</summary>
        public ReadContext Context
        {
            get { return _Context; }
            set { _Context = value; }
        }
        #endregion

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="context"></param>
        public ProtocolDeserializingEventArgs(ReadContext context)
        {
            Context = context;
        }
    }
}
