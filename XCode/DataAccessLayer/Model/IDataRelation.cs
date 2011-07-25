using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 数据关系。一个表如果有多个数据关系，表明是多对多的关系表；如果只有一个关系，需要看是否唯一，它决定是一对一还是一对多。并可根据关系，生成对应的数据索引。
    /// </summary>
    public interface IDataRelation
    {
        #region 属性
        /// <summary>
        /// 数据列
        /// </summary>
        String Column { get; }

        /// <summary>
        /// 引用表
        /// </summary>
        String RelationTable { get; }

        /// <summary>
        /// 引用列
        /// </summary>
        String RelationColumn { get; }

        /// <summary>
        /// 是否唯一
        /// </summary>
        Boolean Unique { get; }
        #endregion

        #region 扩展属性
        /// <summary>
        /// 说明数据表
        /// </summary>
        IDataTable Table { get; }
        #endregion
    }
}