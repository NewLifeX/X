using System;
using System.Collections.Generic;
using System.Data;
using NewLife;
using NewLife.Collections;
using XCode.Cache;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XCode
{
    partial class Entity<TEntity>
    {
        #region 元数据
        /// <summary>
        /// 元数据
        /// </summary>
        public static class Meta
        {
            #region 基本属性
            private static Type _ThisType;
            /// <summary>
            /// 实体类型
            /// </summary>
            public static Type ThisType
            {
                get { return _ThisType ?? (_ThisType = typeof(TEntity)); }
                set { _ThisType = value; }
            }

            /// <summary>
            /// 实体链接名
            /// </summary>
            public static String DefaultConnName { get { return XCodeConfig.ConnName(ThisType); } }

            [ThreadStatic]
            private static String _ConnName;
            /// <summary>链接名。线程内允许修改，修改者负责还原</summary>
            public static String ConnName
            {
                get { return _ConnName ?? (_ConnName = DefaultConnName); }
                set
                {
                    //修改链接名，挂载当前表
                    if (!String.IsNullOrEmpty(value) && !String.Equals(_ConnName, value, StringComparison.OrdinalIgnoreCase))
                    {
                        _ConnName = value;

                        DatabaseSchema ds = DatabaseSchema.Create(DBO);
                        XTable table = null;
                        //检查该表是否检查过
                        if (ds.EntityTables != null && ds.EntityTables.Count > 0)
                        {
                            table = ds.EntityTables.Find(delegate(XTable item)
                            {
                                return String.Equals(item.Name, TableName, StringComparison.OrdinalIgnoreCase);
                            });
                        }

                        if (table == null)
                        {
                            //检查新表名对应的数据表
                            table = DatabaseSchema.Create(ThisType, TableName);
                            ds.CheckTable(table);

                            lock (ds.EntityTables)
                            {
                                ds.EntityTables.Add(table);
                            }
                        }
                    }
                    _ConnName = value;

                    if (String.IsNullOrEmpty(_ConnName)) _ConnName = DefaultConnName;
                }
            }

            /// <summary>
            /// 表名
            /// </summary>
            public static String DefaultTableName { get { return XCodeConfig.TableName(ThisType); } }

            [ThreadStatic]
            private static String _TableName;
            /// <summary>表名。线程内允许修改，修改者负责还原</summary>
            public static String TableName
            {
                get { return _TableName ?? (_TableName = DefaultTableName); }
                set
                {
                    //修改表名
                    if (!String.IsNullOrEmpty(value) && !String.Equals(_TableName, value, StringComparison.OrdinalIgnoreCase))
                    {
                        DatabaseSchema ds = DatabaseSchema.Create(DBO);
                        XTable table = null;
                        //检查该表是否检查过
                        if (ds.EntityTables != null && ds.EntityTables.Count > 0)
                        {
                            table = ds.EntityTables.Find(delegate(XTable item)
                            {
                                return String.Equals(item.Name, value, StringComparison.OrdinalIgnoreCase);
                            });
                        }

                        if (table == null)
                        {
                            //检查新表名对应的数据表
                            table = DatabaseSchema.Create(ThisType, value);
                            ds.CheckTable(table);

                            lock (ds.EntityTables)
                            {
                                ds.EntityTables.Add(table);
                            }
                        }
                    }
                    _TableName = value;

                    if (String.IsNullOrEmpty(_TableName)) _TableName = DefaultTableName;
                }
            }

            /// <summary>
            /// 所有数据属性
            /// </summary>
            public static List<FieldItem> AllFields { get { return XCodeConfig.AllFields(ThisType); } }

            /// <summary>
            /// 所有绑定到数据表的属性
            /// </summary>
            public static List<FieldItem> Fields { get { return XCodeConfig.Fields(ThisType); } }

            private static ReadOnlyList<String> _FieldNames;
            /// <summary>
            /// 字段名列表
            /// </summary>
            public static List<String> FieldNames
            {
                get
                {
                    if (_FieldNames != null && !_FieldNames.Changed) return _FieldNames;
                    lock (typeof(Meta))
                    {
                        if (_FieldNames != null)
                        {
                            if (_FieldNames.Changed) _FieldNames = _FieldNames.Keep();
                            return _FieldNames;
                        }

                        List<String> list = new List<String>();
                        //Fields.ForEach(delegate(FieldItem item) { if (!_FieldNames.Contains(item.Name))_FieldNames.Add(item.Name); });
                        foreach (FieldItem item in Fields)
                        {
                            if (!list.Contains(item.Name)) list.Add(item.Name);
                        }

                        _FieldNames = new ReadOnlyList<String>(list);
                        return _FieldNames;
                    }
                }
            }

            /// <summary>
            /// 唯一键集合，返回标识列集合或主键集合
            /// </summary>
            public static List<FieldItem> Uniques { get { return XCodeConfig.Unique(ThisType); } }

            /// <summary>
            /// 唯一键，返回第一个标识列或者唯一的主键
            /// </summary>
            public static FieldItem Unique
            {
                get
                {
                    List<FieldItem> fis = Uniques;
                    if (fis == null || fis.Count < 1) return null;
                    foreach (FieldItem item in fis)
                    {
                        if (item.DataObjectField != null && item.DataObjectField.IsIdentity) return item;
                    }
                    if (fis.Count == 1) return fis[0];
                    return null;
                }
            }

            /// <summary>
            /// 取得字段前缀
            /// </summary>
            public static String ColumnPrefix { get { return XCodeConfig.ColumnPrefix(ThisType); } }

            /// <summary>
            /// 取得指定类对应的Select字句字符串。
            /// </summary>
            public static String Selects { get { return XCodeConfig.Selects(ThisType); } }
            #endregion

            #region 数据库操作
            /// <summary>
            /// 数据操作对象。
            /// </summary>
            public static DAL DBO { get { return DAL.Create(Meta.ConnName); } }

            /// <summary>
            /// 数据库类型
            /// </summary>
            public static DatabaseType DbType { get { return DBO.DbType; } }

            /// <summary>
            /// 查询
            /// </summary>
            /// <param name="sql">SQL语句</param>
            /// <returns>结果记录集</returns>
            public static DataSet Query(String sql)
            {
                return DBO.Select(sql, Meta.TableName);
            }

            /// <summary>
            /// 查询
            /// </summary>
            /// <param name="sql">SQL语句</param>
            /// <param name="tableNames">所依赖的表的表名</param>
            /// <returns>结果记录集</returns>
            public static DataSet Query(String sql, String[] tableNames)
            {
                return DBO.Select(sql, tableNames);
            }

            /// <summary>
            /// 查询记录数
            /// </summary>
            /// <param name="sql">SQL语句</param>
            /// <returns>记录数</returns>
            public static Int32 QueryCount(String sql)
            {
                return DBO.SelectCount(sql, Meta.TableName);
            }

            /// <summary>
            /// 执行
            /// </summary>
            /// <param name="sql">SQL语句</param>
            /// <returns>影响的结果</returns>
            public static Int32 Execute(String sql)
            {
                Int32 rs = DBO.Execute(sql, Meta.TableName);
                if (rs > 0) DataChange();
                return rs;
            }

            /// <summary>
            /// 执行插入语句并返回新增行的自动编号
            /// </summary>
            /// <param name="sql">SQL语句</param>
            /// <returns>新增行的自动编号</returns>
            public static Int64 InsertAndGetIdentity(String sql)
            {
                Int64 rs = DBO.InsertAndGetIdentity(sql, Meta.TableName);
                if (rs > 0) DataChange();
                return rs;
            }

            /// <summary>
            /// 根据条件把普通查询SQL格式化为分页SQL。
            /// </summary>
            /// <param name="sql">SQL语句</param>
            /// <param name="startRowIndex">开始行，0开始</param>
            /// <param name="maximumRows">最大返回行数</param>
            /// <param name="keyColumn">唯一键。用于not in分页</param>
            /// <returns>分页SQL</returns>
            public static String PageSplit(String sql, Int32 startRowIndex, Int32 maximumRows, String keyColumn)
            {
                return DBO.PageSplit(sql, startRowIndex, maximumRows, keyColumn);
            }

            /// <summary>
            /// 根据条件把普通查询SQL格式化为分页SQL。
            /// </summary>
            /// <param name="builder">查询生成器</param>
            /// <param name="startRowIndex">开始行，0开始</param>
            /// <param name="maximumRows">最大返回行数</param>
            /// <param name="keyColumn">唯一键。用于not in分页</param>
            /// <returns>分页SQL</returns>
            public static String PageSplit(SelectBuilder builder, Int32 startRowIndex, Int32 maximumRows, String keyColumn)
            {
                return DBO.PageSplit(builder, startRowIndex, maximumRows, keyColumn);
            }

            static void DataChange()
            {
                // 还在事务保护里面，不更新缓存，最后提交或者回滚的时候再更新
                // 一般事务保护用于批量更新数据，次数频繁删除缓存将会打来巨大的性能损耗
                if (TransCount > 0) return;

                Cache.Clear();
                _Count = null;

                if (_OnDataChange != null) _OnDataChange(ThisType);
            }

            //private static WeakReference<Action<Type>> _OnDataChange = new WeakReference<Action<Type>>();
            private static Action<Type> _OnDataChange;
            /// <summary>数据改变后触发</summary>
            public static event Action<Type> OnDataChange
            {
                add
                {
                    if (value != null)
                    {
                        // 这里不能对委托进行弱引用，因为GC会回收委托，应该改为对对象进行弱引用
                        //WeakReference<Action<Type>> w = value;

                        _OnDataChange += new WeakAction<Type>(value, delegate(Action<Type> handler) { _OnDataChange -= handler; }, true);
                    }
                }
                remove { }
            }
            #endregion

            #region 事务保护
            [ThreadStatic]
            private static Int32 TransCount = 0;

            /// <summary>开始事务</summary>
            /// <returns></returns>
            public static Int32 BeginTrans() { return TransCount = DBO.BeginTransaction(); }

            /// <summary>提交事务</summary>
            /// <returns></returns>
            public static Int32 Commit()
            {
                TransCount = DBO.Commit();
                // 提交事务时更新数据，虽然不是绝对准备，但没有更好的办法
                if (TransCount <= 0) DataChange();
                return TransCount;
            }

            /// <summary>回滚事务</summary>
            /// <returns></returns>
            public static Int32 Rollback()
            {
                TransCount = DBO.Rollback();
                if (TransCount <= 0) DataChange();
                return TransCount;
            }
            #endregion

            #region 辅助方法
            /// <summary>
            /// 格式化关键字
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public static String FormatKeyWord(String name)
            {
                return DBO.Db.FormatKeyWord(name);
            }

            /// <summary>
            /// 格式化时间
            /// </summary>
            /// <param name="dateTime"></param>
            /// <returns></returns>
            public static String FormatDateTime(DateTime dateTime)
            {
                return DBO.Db.FormatDateTime(dateTime);
            }
            #endregion

            #region 缓存
            private static DictionaryCache<String, EntityCache<TEntity>> _cache = new DictionaryCache<string, EntityCache<TEntity>>();
            /// <summary>
            /// 实体缓存
            /// </summary>
            /// <returns></returns>
            public static EntityCache<TEntity> Cache
            {
                get
                {
                    // 以连接名和表名为key，因为不同的库不同的表，缓存也不一样
                    //String key = String.Format("{0}_{1}", ConnName, TableName);
                    //if (_cache.ContainsKey(key)) return _cache[key];
                    //lock (_cache)
                    //{
                    //    if (_cache.ContainsKey(key)) return _cache[key];

                    return _cache.GetItem(String.Format("{0}_{1}", ConnName, TableName), delegate(String key)
                    {
                        EntityCache<TEntity> ec = new EntityCache<TEntity>();
                        ec.ConnName = ConnName;
                        ec.TableName = TableName;
                        //_cache.Add(key, ec);

                        return ec;
                    });
                }
            }

            //private static Dictionary<String, SingleEntityCache<Int32, TEntity>> _singleCacheInt = new Dictionary<string, SingleEntityCache<Int32, TEntity>>();
            ///// <summary>
            ///// 单实体整型主键缓存。
            ///// 建议自定义查询数据方法，并从二级缓存中获取实体数据，以抵消因初次填充而带来的消耗。
            ///// </summary>
            //[Obsolete("下一个版本将不再支持该功能，请改为使用SingleCache！")]
            //public static SingleEntityCache<Int32, TEntity> SingleCacheInt
            //{
            //    get
            //    {
            //        // 以连接名和表名为key，因为不同的库不同的表，缓存也不一样
            //        String key = String.Format("{0}_{1}", ConnName, TableName);
            //        if (_singleCacheInt.ContainsKey(key)) return _singleCacheInt[key];
            //        lock (_singleCacheInt)
            //        {
            //            if (_singleCacheInt.ContainsKey(key)) return _singleCacheInt[key];

            //            SingleEntityCache<Int32, TEntity> ec = new SingleEntityCache<Int32, TEntity>();
            //            ec.ConnName = ConnName;
            //            ec.TableName = TableName;
            //            _singleCacheInt.Add(key, ec);

            //            return ec;
            //        }
            //    }
            //}

            //private static Dictionary<String, SingleEntityCache<String, TEntity>> _singleCacheStr = new Dictionary<string, SingleEntityCache<String, TEntity>>();
            ///// <summary>
            ///// 单实体字符串主键缓存。
            ///// 建议自定义查询数据方法，并从二级缓存中获取实体数据，以抵消因初次填充而带来的消耗。
            ///// </summary>
            //[Obsolete("下一个版本将不再支持该功能，请改为使用SingleCache！")]
            //public static SingleEntityCache<String, TEntity> SingleCacheStr
            //{
            //    get
            //    {
            //        // 以连接名和表名为key，因为不同的库不同的表，缓存也不一样
            //        String key = String.Format("{0}_{1}", ConnName, TableName);
            //        if (_singleCacheStr.ContainsKey(key)) return _singleCacheStr[key];
            //        lock (_singleCacheStr)
            //        {
            //            if (_singleCacheStr.ContainsKey(key)) return _singleCacheStr[key];

            //            SingleEntityCache<String, TEntity> ec = new SingleEntityCache<String, TEntity>();
            //            ec.ConnName = ConnName;
            //            ec.TableName = TableName;
            //            _singleCacheStr.Add(key, ec);

            //            return ec;
            //        }
            //    }
            //}

            private static DictionaryCache<String, SingleEntityCache<Object, TEntity>> _singleCache = new DictionaryCache<String, SingleEntityCache<Object, TEntity>>();
            /// <summary>
            /// 单对象实体缓存。
            /// 建议自定义查询数据方法，并从二级缓存中获取实体数据，以抵消因初次填充而带来的消耗。
            /// </summary>
            public static SingleEntityCache<Object, TEntity> SingleCache
            {
                get
                {
                    // 以连接名和表名为key，因为不同的库不同的表，缓存也不一样
                    return _singleCache.GetItem(String.Format("{0}_{1}", ConnName, TableName), delegate(String key)
                    {
                        SingleEntityCache<Object, TEntity> ec = new SingleEntityCache<Object, TEntity>();
                        ec.ConnName = ConnName;
                        ec.TableName = TableName;
                        return ec;
                    });
                }
            }

            private static Int32? _Count;
            /// <summary>
            /// 总数
            /// </summary>
            public static Int32 Count
            {
                get
                {
                    if (_Count != null) return _Count.Value;

                    //if (_Count <= 1000)
                    //    _Count = QueryCount(SQL(null, DataObjectMethodType.Fill));
                    //else
                    _Count = DBO.Session.QueryCountFast(TableName);
                    return _Count.Value;
                }
            }
            #endregion
        }
        #endregion
    }
}