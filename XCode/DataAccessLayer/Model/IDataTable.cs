using System;
using System.Collections.Generic;

namespace XCode.DataAccessLayer
{
    /// <summary>数据表</summary>
    public interface IDataTable : ICloneable
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>表名</summary>
        String TableName { get; set; }

        /// <summary>所有者</summary>
        String Owner { get; set; }

        /// <summary>
        /// 数据库类型。
        /// 仅用于记录实体类由何种类型数据库生成，当且仅当目标数据库同为该数据库类型时，采用实体属性信息上的RawType作为反向工程的目标字段类型，以期获得开发和生产的最佳兼容。
        /// </summary>
        DatabaseType DbType { get; set; }

        /// <summary>是否视图</summary>
        Boolean IsView { get; set; }

        /// <summary>显示名。如果有Description则使用Description，否则使用Name</summary>
        String DisplayName { get; set; }

        /// <summary>说明</summary>
        String Description { get; set; }
        #endregion

        #region 扩展属性
        /// <summary>数据列集合。可以是空集合，但不能为null。</summary>
        List<IDataColumn> Columns { get; }

        /// <summary>数据关系集合。可以是空集合，但不能为null。</summary>
        List<IDataRelation> Relations { get; }

        /// <summary>数据索引集合。可以是空集合，但不能为null。</summary>
        List<IDataIndex> Indexes { get; }

        /// <summary>主键集合。可以是空集合，但不能为null。</summary>
        IDataColumn[] PrimaryKeys { get; }

        /// <summary>扩展属性</summary>
        IDictionary<String, String> Properties { get; }
        #endregion

        #region 方法
        /// <summary>创建数据列</summary>
        /// <returns></returns>
        IDataColumn CreateColumn();

        /// <summary>创建数据关系</summary>
        /// <returns></returns>
        IDataRelation CreateRelation();

        /// <summary>创建数据索引</summary>
        /// <returns></returns>
        IDataIndex CreateIndex();

        /// <summary>根据字段名获取字段</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        IDataColumn GetColumn(String name);

        /// <summary>根据字段名数组获取字段数组</summary>
        /// <param name="names"></param>
        /// <returns></returns>
        IDataColumn[] GetColumns(String[] names);

        /// <summary>连接另一个表，处理两表间关系</summary>
        /// <param name="table"></param>
        IDataTable Connect(IDataTable table);

        /// <summary>修正数据</summary>
        IDataTable Fix();
        #endregion
    }
}