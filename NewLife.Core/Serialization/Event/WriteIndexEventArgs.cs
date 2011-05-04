using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>
    /// 写入序数事件参数
    /// </summary>
    public class WriteIndexEventArgs : WriterEventArgs
    {
        private Int32 _Index;
        /// <summary>成员序号</summary>
        public Int32 Index
        {
            get { return _Index; }
            set { _Index = value; }
        }

        #region 构造
        /// <summary>
        /// 实例化
        /// </summary>
        /// <param name="index">成员序号</param>
        /// <param name="callback"></param>
        public WriteIndexEventArgs(Int32 index, WriteObjectCallback callback)
            : base(callback)
        {
            Index = index;
        }
        #endregion
    }
}