using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Web;
using NewLife;
using NewLife.Collections;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Threading;
using XCode.Cache;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XCode
{
    partial class Entity<TEntity>
    {
        /// <summary>
        /// 元数据
        /// </summary>
        public static class Meta
        {
            #region 基本属性
            private static Type _ThisType;
            /// <summary>实体类型</summary>
            public static Type ThisType { get { return _ThisType ?? (_ThisType = typeof(TEntity)); } set { _ThisType = value; } }

            /// <summary>表信息</summary>
            public static TableItem Table { get { return TableItem.Create(ThisType); } }

            [ThreadStatic]
            private static String _ConnName;
            /// <summary>链接名。线程内允许修改，修改者负责还原</summary>
            public static String ConnName
            {
                get { return _ConnName ?? (_ConnName = Table.ConnName); }
                set
                {
                    //修改链接名，挂载当前表
                    if (!String.IsNullOrEmpty(value) && !String.Equals(_ConnName, value, StringComparison.OrdinalIgnoreCase))
                    {
                        CheckTable(value, TableName);
                    }
                    _ConnName = value;

                    if (String.IsNullOrEmpty(_ConnName)) _ConnName = Table.ConnName;
                }
            }

            [ThreadStatic]
            private static String _TableName;
            /// <summary>表名。线程内允许修改，修改者负责还原</summary>
            public static String TableName
            {
                get { return _TableName ?? (_TableName = Table.TableName); }
                set
                {
                    //修改表名
                    if (!String.IsNullOrEmpty(value) && !String.Equals(_TableName, value, StringComparison.OrdinalIgnoreCase))
                    {
                        CheckTable(ConnName, value);
                    }
                    _TableName = value;

                    if (String.IsNullOrEmpty(_TableName)) _TableName = Table.TableName;
                }
            }

            private static ICollection<String> hasCheckedTables = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            private static void CheckTable(String connName, String tableName)
            {
                if (hasCheckedTables.Contains(tableName)) return;
                lock (hasCheckedTables)
                {
                    if (hasCheckedTables.Contains(tableName)) return;

                    // 检查新表名对应的数据表
                    IDataTable table = TableItem.Create(ThisType).DataTable;
                    // 克隆一份，防止修改
                    table = table.Clone() as IDataTable;
                    table.Name = tableName;

                    //DatabaseSchema.Check(DAL.Create(connName).Db, table);
                    DAL.Create(connName).Db.CreateMetaData().SetTables(table);
                }
            }

            /// <summary>所有数据属性</summary>
            public static FieldItem[] AllFields { get { return Table.AllFields; } }

            /// <summary>所有绑定到数据表的属性</summary>
            public static FieldItem[] Fields { get { return Table.Fields; } }

            /// <summary>字段名列表</summary>
            public static IList<String> FieldNames { get { return Table.FieldNames; } }

            /// <summary>
            /// 唯一键，返回第一个标识列或者唯一的主键
            /// </summary>
            public static FieldItem Unique
            {
                get
                {
                    if (Table.Identity != null) return Table.Identity;
                    if (Table.PrimaryKeys != null && Table.PrimaryKeys.Length > 0) return Table.PrimaryKeys[0];
                    return null;
                }
            }

            /// <summary>
            /// 实体操作者
            /// </summary>
            internal static IEntityOperate Factory
            {
                get
                {
                    Type type = ThisType;
                    if (type.IsInterface) return null;

                    return EntityFactory.CreateOperate(type);
                }
            }
            #endregion

            #region 数据库操作
            /// <summary>
            /// 数据操作对象。
            /// </summary>
            public static DAL DBO { get { return DAL.Create(ConnName); } }

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
                CheckInitData();

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
                CheckInitData();

                return DBO.Select(sql, tableNames);
            }

            /// <summary>
            /// 查询记录数
            /// </summary>
            /// <param name="sql">SQL语句</param>
            /// <returns>记录数</returns>
            public static Int32 QueryCount(String sql)
            {
                CheckInitData();

                return DBO.SelectCount(sql, Meta.TableName);
            }

            /// <summary>
            /// 查询记录数
            /// </summary>
            /// <param name="sb">查询生成器</param>
            /// <returns>记录数</returns>
            public static Int32 QueryCount(SelectBuilder sb)
            {
                CheckInitData();

                return DBO.SelectCount(sb, new String[] { Meta.TableName });
            }

            /// <summary>
            /// 执行
            /// </summary>
            /// <param name="sql">SQL语句</param>
            /// <returns>影响的结果</returns>
            public static Int32 Execute(String sql)
            {
                CheckInitData();

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
                CheckInitData();

                Int64 rs = DBO.InsertAndGetIdentity(sql, Meta.TableName);
                if (rs > 0) DataChange();
                return rs;
            }

            /// <summary>
            /// 根据条件把普通查询SQL格式化为分页SQL。
            /// </summary>
            /// <param name="sql">SQL语句</param>
            /// <param name="startRowIndex">开始行，0表示第一行</param>
            /// <param name="maximumRows">最大返回行数，0表示所有行</param>
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
            /// <param name="startRowIndex">开始行，0表示第一行</param>
            /// <param name="maximumRows">最大返回行数，0表示所有行</param>
            /// <returns>分页SQL</returns>
            public static String PageSplit(SelectBuilder builder, Int32 startRowIndex, Int32 maximumRows)
            {
                return DBO.PageSplit(builder, startRowIndex, maximumRows);
            }

            static void DataChange()
            {
                // 还在事务保护里面，不更新缓存，最后提交或者回滚的时候再更新
                // 一般事务保护用于批量更新数据，次数频繁删除缓存将会打来巨大的性能损耗
                if (TransCount > 0) return;

                Cache.Clear();
                //_Count = null;
                ClearCountCache();

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

            static Int32 hasCheckModel;
            private static void CheckModel()
            {
                if (Interlocked.CompareExchange(ref hasCheckModel, 1, 0) != 0) return;

                if (Table.ModelCheckMode == ModelCheckModes.CheckTableWhenFirstUse)
                {
                    if (!DAL.NegativeEnable || DAL.NegativeExclude.Contains(ConnName) || DAL.NegativeExclude.Contains(TableName)) return;

                    Func check = delegate
                    {
                        DAL.WriteLog("开始检查表[{0}/{1}]的数据表架构……", Table.DataTable.Name, DbType);

                        Stopwatch sw = new Stopwatch();
                        sw.Start();

                        try
                        {
                            DBO.Db.CreateMetaData().SetTables(Table.DataTable);
                        }
                        finally
                        {
                            sw.Stop();

                            DAL.WriteLog("检查表[{0}/{1}]的数据表架构耗时{2}", Table.DataTable.Name, DbType, sw.Elapsed);
                        }
                    };

                    // 打开了开关，并且设置为true时，使用同步方式检查
                    // 设置为false时，使用异步方式检查，因为上级的意思是不大关心数据库架构
                    if (!DAL.NegativeCheckOnly)
                        check();
                    else
                        ThreadPoolX.QueueUserWorkItem(check);
                }
            }

            /// <summary>
            /// 记录已进行数据初始化的表
            /// </summary>
            static List<String> hasCheckInitData = new List<String>();
            /// <summary>
            /// 检查并初始化数据
            /// </summary>
            public static void CheckInitData()
            {
                String key = ConnName + "$$$" + TableName;
                if (hasCheckInitData.Contains(key)) return;
                hasCheckInitData.Add(key);

                // 如果该实体类是首次使用检查模型，则在这个时候检查
                CheckModel();

                Func check = delegate
                {
                    try
                    {
                        //Factory.InitData();
                        //if (Factory.Default is EntityBase) (Factory.Default as EntityBase).InitData();
                        EntityBase entity = Factory.Default as EntityBase;
                        if (entity != null) entity.InitData();
                    }
                    catch (Exception ex)
                    {
                        if (XTrace.Debug) XTrace.WriteLine("初始化数据出错！" + ex.ToString());
                    }
                };

                // 异步执行，并捕获错误日志
                if (Config.GetConfig<Boolean>("XCode.InitDataAsync", true) && !InitDataHelper.Running)
                {
                    ThreadPool.QueueUserWorkItem(delegate
                    {
                        // 记录当前线程正在初始化数据，内部调用的时候，不要再使用异步
                        InitDataHelper.Running = true;
                        try
                        {
                            check();
                        }
                        finally
                        {
                            // 异步完成，修改设置
                            InitDataHelper.Running = false;
                        }
                    });
                }
                else
                {
                    check();
                }
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
                // 提交事务时更新数据，虽然不是绝对准确，但没有更好的办法
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
            public static String FormatName(String name)
            {
                return DBO.Db.FormatName(name);
            }

            /// <summary>
            /// 格式化关键字
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            [Obsolete("改为使用FormatName")]
            public static String FormatKeyWord(String name)
            {
                return FormatName(name);
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

            /// <summary>
            /// 格式化数据为SQL数据
            /// </summary>
            /// <param name="name"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public static String FormatValue(String name, Object value)
            {
                return FormatValue(Table.FindByName(name), value);
            }

            /// <summary>
            /// 格式化数据为SQL数据
            /// </summary>
            /// <param name="field"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public static String FormatValue(FieldItem field, Object value)
            {
                return DBO.Db.FormatValue(field != null ? field.Field : null, value);
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

            /// <summary>总记录数，小于1000时是精确的，大于1000时缓存10分钟</summary>
            public static Int32 Count { get { return (Int32)LongCount; } }

            /// <summary>总记录数较小时，使用静态字段，较大时增加使用Cache</summary>
            private static Int64? _Count;
            /// <summary>总记录数，小于1000时是精确的，大于1000时缓存10分钟</summary>
            public static Int64 LongCount
            {
                get
                {
                    Int64? n = _Count;
                    if (n != null && n.HasValue)
                    {
                        if (n.Value < 1000) return n.Value;

                        // 大于1000，使用HttpCache
                        String key = ThisType.Name + "_Count";
                        Int64? k = (Int64?)HttpRuntime.Cache[key];
                        if (k != null && k.HasValue) return k.Value;
                    }

                    CheckModel();

                    Int64 m = 0;
                    if (n != null && n.HasValue && n.Value < 1000)
                        m = FindCount();
                    else
                        m = DBO.Session.QueryCountFast(TableName);
                    _Count = m;

                    if (m >= 1000) HttpRuntime.Cache.Insert(ThisType.Name + "_Count", m, null, DateTime.Now.AddMinutes(10), System.Web.Caching.Cache.NoSlidingExpiration);

                    // 先拿到记录数再初始化，因为初始化时会用到记录数，同时也避免了死循环
                    CheckInitData();

                    return m;
                }
            }

            private static void ClearCountCache()
            {
                Int64? n = _Count;
                if (n == null || !n.HasValue) return;

                // 只有小于1000时才清空_Count，因为大于1000时它要作为HttpCache的见证
                if (n.Value < 1000)
                {
                    _Count = null;
                    return;
                }

                String key = ThisType.Name + "_Count";
                HttpRuntime.Cache.Remove(key);
            }
            #endregion
        }
    }

    /// <summary>
    /// 初始化数据助手，用于判断当前线程是否正在初始化之中
    /// </summary>
    static class InitDataHelper
    {
        /// <summary>
        /// 是否正在运行
        /// </summary>
        [ThreadStatic]
        public static Boolean Running;
    }
}