using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>
    /// 读写器基类
    /// </summary>
    public abstract class ReaderWriterBase : IReaderWriter
    {
        #region 属性
        private Encoding _Encoding;
        /// <summary>编码</summary>
        public virtual Encoding Encoding
        {
            get { return _Encoding; }
            set { _Encoding = value; }
        }
        #endregion
    }
}
