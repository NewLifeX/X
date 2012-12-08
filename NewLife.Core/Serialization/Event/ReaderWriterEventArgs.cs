using System;

namespace NewLife.Serialization
{
    /// <summary>读写器事件参数</summary>
    public class ReaderWriterEventArgs : EventArgs
    {
        #region 属性
        private Boolean _Success;
        /// <summary>是否成功。</summary>
        public Boolean Success { get { return _Success; } set { _Success = value; } }
        #endregion
    }
}