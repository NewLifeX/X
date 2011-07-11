using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 数据关系
    /// </summary>
    public interface IDataRelation
    {
        #region 属性
        /// <summary>
        /// 引用表
        /// </summary>
        String RelationTable { get; }

        /// <summary>
        /// 引用列
        /// </summary>
        String RelationColumn { get; }
        #endregion

        #region 扩展属性
        /// <summary>
        /// 说明数据表
        /// </summary>
        IDataTable Table { get; }
        #endregion
    }
}