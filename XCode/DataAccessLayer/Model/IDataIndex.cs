using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 索引
    /// </summary>
    public interface IDataIndex
    {
        #region 属性
        /// <summary>
        /// 名称
        /// </summary>
        String Name { get; set; }

        /// <summary>
        /// 数据列集合
        /// </summary>
        String[] Columns { get; set; }

        /// <summary>
        /// 是否唯一
        /// </summary>
        Boolean Unique { get; set; }
        #endregion

        #region 扩展属性
        /// <summary>
        /// 说明数据表
        /// </summary>
        IDataTable Table { get; }
        #endregion
    }
}