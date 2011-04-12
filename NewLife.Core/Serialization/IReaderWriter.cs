using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>
    /// 读写器接口
    /// </summary>
    public interface IReaderWriter
    {
        #region 属性
        /// <summary>
        /// 字符串编码
        /// </summary>
        Encoding Encoding { get; set; }
        #endregion
    }
}
