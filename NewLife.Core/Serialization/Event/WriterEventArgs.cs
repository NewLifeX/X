using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>写入器时间参数</summary>
    public class WriterEventArgs : ReaderWriterEventArgs
    {
        private WriteObjectCallback _Callback;
        /// <summary>处理成员的委托</summary>
        public WriteObjectCallback Callback
        {
            get { return _Callback; }
            set { _Callback = value; }
        }

        #region 构造
        /// <summary>实例化</summary>
        /// <param name="callback"></param>
        public WriterEventArgs(WriteObjectCallback callback)
        {
            Callback = callback;
        }
        #endregion
    }
}