using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 数据索引。可根据索引生成查询方法，是否唯一决定该索引返回的是单个实体还是实体集合。
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

        /// <summary>
        /// 是否主键
        /// </summary>
        Boolean PrimaryKey { get; set; }
        #endregion

        #region 扩展属性
        /// <summary>
        /// 说明数据表
        /// </summary>
        IDataTable Table { get; }
        #endregion

        #region 方法
        /// <summary>
        /// 克隆到指定的数据表
        /// </summary>
        /// <param name="table"></param>
        IDataIndex Clone(IDataTable table);
        #endregion
    }
}