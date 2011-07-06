using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 数据表
    /// </summary>
    public interface IDataTable : ICloneable
    {
        #region 属性
        /// <summary>
        /// 编号
        /// </summary>
        Int32 ID { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        String Name { get; set; }

        /// <summary>
        /// 别名
        /// </summary>
        String Alias { get; set; }

        /// <summary>
        /// 所有者
        /// </summary>
        String Owner { get; set; }

        /// <summary>
        /// 数据库类型
        /// </summary>
        DatabaseType DbType { get; set; }

        /// <summary>
        /// 是否视图
        /// </summary>
        Boolean IsView { get; set; }

        /// <summary>
        /// 说明
        /// </summary>
        String Description { get; set; }
        #endregion

        #region 扩展属性
        /// <summary>
        /// 数据列集合
        /// </summary>
        IDataColumn[] Columns { get; set; }

        /// <summary>
        /// 数据外键集合
        /// </summary>
        IDataForeignKey[] ForeignKeys { get; set; }

        /// <summary>
        /// 数据索引集合
        /// </summary>
        IDataIndex[] Indexes { get; set; }
        #endregion

        #region 方法
        /// <summary>
        /// 创建数据列
        /// </summary>
        /// <returns></returns>
        IDataColumn CreateColumn();

        /// <summary>
        /// 创建数据外键
        /// </summary>
        /// <returns></returns>
        IDataForeignKey CreateForeignKey();

        /// <summary>
        /// 创建数据索引
        /// </summary>
        /// <returns></returns>
        IDataIndex CreateIndex();
        #endregion
    }
}