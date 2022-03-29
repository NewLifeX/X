﻿using System;
using System.Collections.Generic;
using NewLife;

namespace XCode.DataAccessLayer
{
    /// <summary>数据库元数据接口</summary>
    public interface IMetaData : IDisposable2
    {
        #region 属性
        /// <summary>数据库</summary>
        IDatabase Database { get; }

        /// <summary>所有元数据集合</summary>
        ICollection<String> MetaDataCollections { get; }

        /// <summary>保留关键字</summary>
        ICollection<String> ReservedWords { get; }
        #endregion

        #region 构架
        /// <summary>取得表模型，正向工程</summary>
        /// <returns></returns>
        IList<IDataTable> GetTables();

        /// <summary>
        /// 取得所有表名
        /// </summary>
        /// <returns></returns>
        IList<String> GetTableNames();

        /// <summary>设置表模型，检查数据表是否匹配表模型，反向工程</summary>
        /// <param name="setting">设置</param>
        /// <param name="tables"></param>
        void SetTables(Migration setting, params IDataTable[] tables);

        /// <summary>获取数据定义语句</summary>
        /// <param name="schema">数据定义模式</param>
        /// <param name="values">其它信息</param>
        /// <returns>数据定义语句</returns>
        String GetSchemaSQL(DDLSchema schema, params Object[] values);

        /// <summary>设置数据定义模式</summary>
        /// <param name="schema">数据定义模式</param>
        /// <param name="values">其它信息</param>
        /// <returns></returns>
        Object SetSchema(DDLSchema schema, params Object[] values);
        #endregion
    }
}