using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 数据外键
    /// </summary>
    public interface IDataForeignKey
    {
        #region 属性
        /// <summary>
        /// 引用表
        /// </summary>
        String ForeignTable { get; set; }

        /// <summary>
        /// 引用列
        /// </summary>
        String ForeignColumn { get; set; }
        #endregion

        #region 扩展属性
        /// <summary>
        /// 说明数据表
        /// </summary>
        IDataTable Table { get; }
        #endregion
    }
}