using System;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 数据索引。
    /// 可根据索引生成查询方法，是否唯一决定该索引返回的是单个实体还是实体集合。
    /// 正向工程将会为所有一对一索引建立关系。
    /// </summary>
    public interface IDataIndex
    {
        #region 属性
        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>数据列集合</summary>
        String[] Columns { get; set; }

        /// <summary>是否唯一</summary>
        Boolean Unique { get; set; }

        /// <summary>是否主键</summary>
        Boolean PrimaryKey { get; set; }

        /// <summary>是否计算出来的，而不是数据库内置的。主要供反向工程识别该索引是否由计算产生，反向工程会要求数据库拥有真正的索引。</summary>
        Boolean Computed { get; set; }
        #endregion

        #region 扩展属性
        /// <summary>说明数据表</summary>
        IDataTable Table { get; }
        #endregion

        #region 方法
        /// <summary>克隆到指定的数据表</summary>
        /// <param name="table"></param>
        IDataIndex Clone(IDataTable table);
        #endregion
    }
}