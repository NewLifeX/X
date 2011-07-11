using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 数据列
    /// </summary>
    public interface IDataColumn
    {
        #region 属性
        /// <summary>
        /// 顺序编号
        /// </summary>
        Int32 ID { get; }

        /// <summary>
        /// 名称
        /// </summary>
        String Name { get; }

        /// <summary>
        /// 别名
        /// </summary>
        String Alias { get; }

        /// <summary>
        /// 数据类型
        /// </summary>
        Type DataType { get; }

        /// <summary>
        /// 原始数据类型
        /// </summary>
        String RawType { get; }

        /// <summary>
        /// 标识
        /// </summary>
        Boolean Identity { get; }

        /// <summary>
        /// 主键
        /// </summary>
        Boolean PrimaryKey { get; }

        /// <summary>
        /// 长度
        /// </summary>
        Int32 Length { get; }

        /// <summary>
        /// 字节数
        /// </summary>
        Int32 NumOfByte { get; }

        /// <summary>
        /// 精度
        /// </summary>
        Int32 Precision { get; }

        /// <summary>
        /// 位数
        /// </summary>
        Int32 Scale { get; }

        /// <summary>
        /// 允许空
        /// </summary>
        Boolean Nullable { get; }

        /// <summary>
        /// 是否Unicode
        /// </summary>
        Boolean IsUnicode { get; }

        /// <summary>
        /// 默认值
        /// </summary>
        String Default { get; }

        /// <summary>
        /// 说明
        /// </summary>
        String Description { get; }
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
        IDataColumn Clone(IDataTable table);
        #endregion
    }
}