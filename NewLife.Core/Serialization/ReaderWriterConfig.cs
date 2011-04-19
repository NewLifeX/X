using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>
    /// 读写器配置
    /// </summary>
    public class ReaderWriterConfig
    {
        #region 属性
        private Boolean _Required;
        /// <summary>必须的</summary>
        public Boolean Required
        {
            get { return _Required; }
            set { _Required = value; }
        }
        #endregion
    }
}