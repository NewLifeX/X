using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using NewLife;
using NewLife.Collections;

namespace XCode.DataAccessLayer
{
    /// <summary>数据库元数据</summary>
    abstract partial class DbMetaData : DisposeBase, IMetaData
    {
        #region 属性
        private IDatabase _Database;
        /// <summary>数据库</summary>
        public virtual IDatabase Database { get { return _Database; } set { _Database = value; } }

        private ICollection<String> _MetaDataCollections;
        /// <summary>所有元数据集合</summary>
        public ICollection<String> MetaDataCollections
        {
            get
            {
                if (_MetaDataCollections == null)
                {
                    var list = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
                    var dt = GetSchema(DbMetaDataCollectionNames.MetaDataCollections, null);
                    if (dt != null && dt.Rows != null && dt.Rows.Count > 0)
                    {
                        foreach (DataRow dr in dt.Rows)
                        {
                            String name = "" + dr[0];
                            if (!String.IsNullOrEmpty(name) && !list.Contains(name)) list.Add(name);
                        }
                    }
                    _MetaDataCollections = list;
                }
                return _MetaDataCollections;
            }
        }

        private ICollection<String> _ReservedWords;
        /// <summary>保留关键字</summary>
        public virtual ICollection<String> ReservedWords
        {
            get
            {
                if (_ReservedWords == null)
                {
                    var list = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
                    if (MetaDataCollections.Contains(DbMetaDataCollectionNames.ReservedWords))
                    {
                        var dt = GetSchema(DbMetaDataCollectionNames.ReservedWords, null);
                        if (dt != null && dt.Rows != null && dt.Rows.Count > 0)
                        {
                            foreach (DataRow dr in dt.Rows)
                            {
                                String name = "" + dr[0];
                                if (!String.IsNullOrEmpty(name) && !list.Contains(name)) list.Add(name);
                            }
                        }
                    }
                    _ReservedWords = list;
                }
                return _ReservedWords;
            }
        }

        //String _ParamPrefix;
        ///// <summary>参数前缀</summary>
        //public String ParamPrefix
        //{
        //    get
        //    {
        //        if (_ParamPrefix == null)
        //        {
        //            var dt = GetSchema(DbMetaDataCollectionNames.DataSourceInformation, null);
        //            if (dt != null && dt.Rows != null && dt.Rows.Count > 0)
        //            {
        //                String str = null;
        //                if (TryGetDataRowValue<String>(dt.Rows[0], DbMetaDataColumnNames.ParameterMarkerPattern, out str) ||
        //                    TryGetDataRowValue<String>(dt.Rows[0], DbMetaDataColumnNames.ParameterMarkerFormat, out str))
        //                    _ParamPrefix = str.StartsWith("\\") ? str.Substring(1, 1) : str.Substring(0, 1);
        //            }

        //            if (_ParamPrefix == null) _ParamPrefix = "";
        //        }
        //        return _ParamPrefix;
        //    }
        //}
        #endregion

        #region GetSchema方法
        /// <summary>返回数据源的架构信息</summary>
        /// <param name="collectionName">指定要返回的架构的名称。</param>
        /// <param name="restrictionValues">为请求的架构指定一组限制值。</param>
        /// <returns></returns>
        public DataTable GetSchema(string collectionName, string[] restrictionValues)
        {
            // 如果不是MetaDataCollections，并且MetaDataCollections中没有该集合，则返回空
            if (!String.Equals(collectionName, DbMetaDataCollectionNames.MetaDataCollections, StringComparison.OrdinalIgnoreCase))
            {
                if (!MetaDataCollections.Contains(collectionName)) return null;
            }
            return Database.CreateSession().GetSchema(collectionName, restrictionValues);
        }
        #endregion

        #region 辅助函数
        /// <summary>尝试从指定数据行中读取指定名称列的数据</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dr"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static Boolean TryGetDataRowValue<T>(DataRow dr, String name, out T value)
        {
            value = default(T);
            if (dr == null || !dr.Table.Columns.Contains(name) || dr.IsNull(name)) return false;

            Object obj = dr[name];

            // 特殊处理布尔类型
            if (Type.GetTypeCode(typeof(T)) == TypeCode.Boolean && obj != null)
            {
                if (obj is Boolean)
                {
                    value = (T)obj;
                    return true;
                }

                if (String.Equals("YES", obj.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    value = (T)(Object)true;
                    return true;
                }
                if (String.Equals("NO", obj.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    value = (T)(Object)false;
                    return true;
                }
            }

            try
            {
                if (obj is T)
                    value = (T)obj;
                else
                    value = (T)Convert.ChangeType(obj, typeof(T));
            }
            catch { return false; }

            return true;
        }

        /// <summary>获取指定数据行指定字段的值，不存在时返回空</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dr"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        protected static T GetDataRowValue<T>(DataRow dr, String name)
        {
            T value = default(T);
            if (TryGetDataRowValue<T>(dr, name, out value)) return value;
            return default(T);
        }

        /// <summary>格式化关键字</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected String FormatName(String name)
        {
            //return Database.FormatKeyWord(keyWord);
            return Database.FormatName(name);
        }

        /// <summary>检查并获取当前数据库的默认值</summary>
        /// <param name="dc"></param>
        /// <param name="oriDefault"></param>
        /// <returns></returns>
        protected virtual Boolean CheckAndGetDefault(IDataColumn dc, ref String oriDefault)
        {
            // 如果数据库类型等于原始类型，则直接通过
            if (dc.Table.DbType == Database.DbType) return true;

            // 原始数据库类型
            var db = DbFactory.Create(dc.Table.DbType);
            if (db == null) return false;

            var tc = Type.GetTypeCode(dc.DataType);
            // 特殊处理时间
            if (tc == TypeCode.DateTime)
            {
                if (String.IsNullOrEmpty(oriDefault) || oriDefault.EqualIgnoreCase(db.DateTimeNow))
                {
                    oriDefault = Database.DateTimeNow;
                    return true;
                }
                else
                {
                    // 出现了不支持的时间默认值
                    if (DAL.Debug) DAL.WriteLog("出现了{0}不支持的时间默认值：{1}.{2}={3}", Database.DbType, dc.Table.Name, dc.Name, oriDefault);

                    oriDefault = null;
                    return true;
                }
            }
            // 特殊处理Guid
            else if (tc == TypeCode.String || dc.DataType == typeof(Guid))
            {
                // 如果字段类型是Guid，不需要设置默认值，则也说明是Guid字段
                if (String.IsNullOrEmpty(oriDefault) || oriDefault.EqualIgnoreCase(db.NewGuid) ||
                   String.IsNullOrEmpty(db.NewGuid) && dc.DataType == typeof(Guid))
                {
                    oriDefault = Database.NewGuid;
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region 日志输出
        /// <summary>输出日志</summary>
        /// <param name="msg"></param>
        public static void WriteLog(String msg) { DAL.WriteLog(msg); }

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLog(String format, params Object[] args) { DAL.WriteLog(format, args); }
        #endregion
    }
}