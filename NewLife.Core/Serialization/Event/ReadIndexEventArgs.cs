using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>读取序数事件参数</summary>>
    public class ReadIndexEventArgs : ReaderEventArgs
    {
        private Int32 _Index;
        /// <summary>成员序号</summary>
        public Int32 Index
        {
            get { return _Index; }
            set { _Index = value; }
        }

        #region 构造
        /// <summary>实例化</summary>>
        /// <param name="index">成员序号</param>
        /// <param name="callback"></param>
        public ReadIndexEventArgs(Int32 index, ReadObjectCallback callback)
            : base(callback)
        {
            Index = index;
        }
        #endregion
    }
}