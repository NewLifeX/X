using System;
using System.Collections.Generic;

namespace XCode.DataAccessLayer
{
    /// <summary>数据列</summary>
    public interface IDataColumn
    {
        #region 属性
        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>列名</summary>
        String ColumnName { get; set; }

        /// <summary>数据类型</summary>
        Type DataType { get; set; }

        /// <summary>
        /// 原始数据类型。
        /// 当且仅当目标数据库同为该数据库类型时，采用实体属性信息上的RawType作为反向工程的目标字段类型，以期获得开发和生产的最佳兼容。
        /// </summary>
        String RawType { get; set; }

        /// <summary>标识</summary>
        Boolean Identity { get; set; }

        /// <summary>主键</summary>
        Boolean PrimaryKey { get; set; }

        /// <summary>是否主字段。主字段作为业务主要字段，代表当前数据行意义</summary>
        Boolean Master { get; set; }

        /// <summary>长度</summary>
        Int32 Length { get; set; }

        /// <summary>精度</summary>
        Int32 Precision { get; set; }

        /// <summary>位数</summary>
        Int32 Scale { get; set; }

        /// <summary>允许空</summary>
        Boolean Nullable { get; set; }

        /// <summary>显示名。如果有Description则使用Description，否则使用Name</summary>
        String DisplayName { get; }

        /// <summary>说明</summary>
        String Description { get; set; }
        #endregion

        #region 扩展属性
        /// <summary>说明数据表</summary>
        IDataTable Table { get; }

        /// <summary>扩展属性</summary>
        IDictionary<String, String> Properties { get; }
        #endregion

        #region 方法
        /// <summary>重新计算修正别名。避免与其它字段名或表名相同，避免关键字</summary>
        /// <returns></returns>
        IDataColumn Fix();

        /// <summary>克隆到指定的数据表</summary>
        /// <param name="table"></param>
        IDataColumn Clone(IDataTable table);
        #endregion
    }
}