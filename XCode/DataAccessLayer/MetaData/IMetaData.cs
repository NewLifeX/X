using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 数据库元数据接口
    /// </summary>
    public interface IMetaData
    {
        #region 构架
        /// <summary>
        /// 返回数据源的架构信息
        /// </summary>
        /// <param name="collectionName">指定要返回的架构的名称。</param>
        /// <param name="restrictionValues">为请求的架构指定一组限制值。</param>
        /// <returns></returns>
        DataTable GetSchema(string collectionName, string[] restrictionValues);

        /// <summary>
        /// 取得所有表构架
        /// </summary>
        /// <returns></returns>
        List<XTable> GetTables();

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
