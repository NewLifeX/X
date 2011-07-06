using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 数据模型工厂
    /// </summary>
    public interface IDataFactory
    {
        #region 方法
        /// <summary>
        /// 创建数据表
        /// </summary>
        /// <returns></returns>
        IDataTable CreateTable();

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