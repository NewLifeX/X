using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using NewLife;
using NewLife.Data;
using NewLife.Reflection;

namespace XCode.DataAccessLayer
{
    /// <summary>数据库元数据</summary>
    abstract partial class DbMetaData : DisposeBase, IMetaData
    {
        #region 属性
        /// <summary>数据库</summary>
        public virtual IDatabase Database { get; set; }

        private ICollection<String> _MetaDataCollections;
        /// <summary>所有元数据集合</summary>
        public ICollection<String> MetaDataCollections
        {
            get
            {
                if (_MetaDataCollections != null) return _MetaDataCollections;
                lock (this)
                {
                    if (_MetaDataCollections != null) return _MetaDataCollections;

                    var list = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
                    var dt = GetSchema(DbMetaDataCollectionNames.MetaDataCollections, null);
                    if (dt?.Rows != null && dt.Rows.Count > 0)
                    {
                        foreach (DataRow dr in dt.Rows)
                        {
                            var name = "" + dr[0];
                            if (!name.IsNullOrEmpty() && !list.Contains(name)) list.Add(name);
                        }
                    }
                    return _MetaDataCollections = list;
                }
            }
        }

        private ICollection<String> _ReservedWords;
        /// <summary>保留关键字</summary>
        public virtual ICollection<String> ReservedWords
        {
            get
            {
                if (_ReservedWords != null) return _ReservedWords;
                lock (this)
                {
                    if (_ReservedWords != null) return _ReservedWords;

                    var list = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
                    if (MetaDataCollections.Contains(DbMetaDataCollectionNames.ReservedWords))
                    {
                        var dt = GetSchema(DbMetaDataCollectionNames.ReservedWords, null);
                        if (dt?.Rows != null && dt.Rows.Count > 0)
                        {
                            foreach (DataRow dr in dt.Rows)
                            {
                                var name = "" + dr[0];
                                if (!name.IsNullOrEmpty() && !list.Contains(name)) list.Add(name);
                            }
                        }
                    }
                    return _ReservedWords = list;
                }
            }
        }
        #endregion

        #region GetSchema方法
        /// <summary>返回数据源的架构信息</summary>
        /// <param name="collectionName">指定要返回的架构的名称。</param>
        /// <param name="restrictionValues">为请求的架构指定一组限制值。</param>
        /// <returns></returns>
        public DataTable GetSchema(String collectionName, String[] restrictionValues)
        {
            // 如果不是MetaDataCollections，并且MetaDataCollections中没有该集合，则返回空
            if (!collectionName.EqualIgnoreCase(DbMetaDataCollectionNames.MetaDataCollections))
            {
                if (!MetaDataCollections.Contains(collectionName)) return null;
            }
            return Database.CreateSession().GetSchema(null, collectionName, restrictionValues);
        }
        #endregion

        #region 辅助函数
        /// <summary>尝试从指定数据行中读取指定名称列的数据</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dr"></param>
        /// <param name="name">名称</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        protected static Boolean TryGetDataRowValue<T>(DataRow dr, String name, out T value)
        {
            value = default(T);
            if (dr == null || !dr.Table.Columns.Contains(name) || dr.IsNull(name)) return false;

            var obj = dr[name];

            // 特殊处理布尔类型
            if (Type.GetTypeCode(typeof(T)) == TypeCode.Boolean && obj != null)
            {
                if (obj is Boolean)
                {
                    value = (T)obj;
                    return true;
                }

                if ("YES".EqualIgnoreCase(obj.ToString()))
                {
                    value = (T)(Object)true;
                    return true;
                }
                if ("NO".EqualIgnoreCase(obj.ToString()))
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
                {
                    if (obj != null && obj.GetType().IsInt())
                    {
                        var n = Convert.ToUInt64(obj);
                        if (n == UInt32.MaxValue && Type.GetTypeCode(typeof(T)) == TypeCode.Int32)
                        {
                            obj = -1;
                        }
                    }
                    value = obj.ChangeType<T>();
                }
            }
            catch { return false; }

            return true;
        }

        /// <summary>获取指定数据行指定字段的值，不存在时返回空</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dr"></param>
        /// <param name="names">名称</param>
        /// <returns></returns>
        protected static T GetDataRowValue<T>(DataRow dr, params String[] names)
        {
            foreach (var item in names)
            {
                if (TryGetDataRowValue(dr, item, out T value)) return value;
            }

            return default(T);
        }

        protected static DbTable Select(DbTable ds, String name, Object value)
        {
            var list = new List<Object[]>();
            var col = ds.GetColumn(name);
            if (col >= 0)
            {
                for (var i = 0; i < ds.Rows.Count; i++)
                {
                    var dr = ds.Rows[i];
                    if (Equals(dr[col], value)) list.Add(dr);
                }
            }

            var ds2 = new DbTable
            {
                Columns = ds.Columns,
                Types = ds.Types,
                Rows = list
            };

            return ds2;
        }

        /// <summary>格式化关键字</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        protected String FormatName(String name) => Database.FormatName(name);
        #endregion

        #region 日志输出
        /// <summary>输出日志</summary>
        /// <param name="msg"></param>
        public static void WriteLog(String msg) => DAL.WriteLog(msg);

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLog(String format, params Object[] args) => DAL.WriteLog(format, args);
        #endregion
    }
}