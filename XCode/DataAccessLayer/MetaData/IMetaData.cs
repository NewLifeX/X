using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 数据库元数据接口
    /// </summary>
    public interface IMetaData : IDisposable
    {
        #region 属性
        /// <summary>
        /// 数据库
        /// </summary>
        IDatabase Database { get; }
        #endregion

        #region 构架
        ///// <summary>
        ///// 创建数据表
        ///// </summary>
        ///// <returns></returns>
        //IDataTable CreateTable();

        /// <summary>
        /// 取得所有表构架
        /// </summary>
        /// <returns></returns>
        List<IDataTable> GetTables();

        /// <summary>
        /// 获取数据定义语句
        /// </summary>
        /// <param name="schema">数据定义模式</param>
        /// <param name="values">其它信息</param>
        /// <returns>数据定义语句</returns>
        String GetSchemaSQL(DDLSchema schema, params Object[] values);

        /// <summary>
        /// 设置数据定义模式
        /// </summary>
        /// <param name="schema">数据定义模式</param>
        /// <param name="values">其它信息</param>
        /// <returns></returns>
        Object SetSchema(DDLSchema schema, params Object[] values);
        #endregion
    }
}
