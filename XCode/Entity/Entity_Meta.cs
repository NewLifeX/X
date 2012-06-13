using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Web;
using NewLife;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Threading;
using XCode.Cache;
using XCode.Configuration;
using XCode.DataAccessLayer;

#if DEBUG
using XCode.Common;
#endif

namespace XCode
{
    partial class Entity<TEntity>
    {
        /// <summary>实体元数据</summary>
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
            /// <summary>链接名。线程内允许修改，修改者负责还原。若要还原默认值，设为null即可</summary>
            public static String ConnName
            {
                get { return _ConnName ?? (_ConnName = Table.ConnName); }
                set
                {
                    //修改链接名，挂载当前表
                    if (!String.IsNullOrEmpty(value) && !String.Equals(_ConnName, value, StringComparison.OrdinalIgnoreCase))
                    {
                        CheckTable(value, TableName);

                        // 清空记录数缓存
                        ClearCountCache();
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

                        // 清空记录数缓存
                        ClearCountCache();
                    }
                    _TableName = value;

                    if (String.IsNullOrEmpty(_TableName)) _TableName = Table.TableName;
                }
            }

            private static ICollection<String> hasCheckedTables = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            private static void CheckTable(String connName, String tableName)
            {
                var key = String.Format("{0}#{1}", connName, tableName);
                if (hasCheckedTables.Contains(key)) return;
                lock (hasCheckedTables)
                {
                    if (hasCheckedTables.Contains(key)) return;

                    // 检查新表名对应的数据表
                    var table = TableItem.Create(ThisType).DataTable;
                    // 克隆一份，防止修改
                    table = table.Clone() as IDataTable;

                    if (table.Name != tableName)
                    {
                        // 修改一下索引名，否则，可能因为同一个表里面不同的索引冲突
                        if (table.Indexes != null)
                        {
                            foreach (var di in table.Indexes)
                            {
                                var sb = new StringBuilder();
                                sb.AppendFormat("IX_{0}", tableName);
                                foreach (var item in di.Columns)
                                {
                                    sb.Append("_");
                                    sb.Append(item);
                                }

                                di.Name = sb.ToString();
                            }
                        }
                        table.Name = tableName;
                    }

                    //var set = new NegativeSetting();
                    //set.CheckOnly = DAL.NegativeCheckOnly;
                    //set.NoDelete = DAL.NegativeNoDelete;
                    //DAL.Create(connName).Db.CreateMetaData().SetTables(set, table);
                    DAL.Create(connName).SetTables(table);

                    hasCheckedTables.Add(key);
                }
            }

            /// <summary>所有数据属性</summary>
            public static FieldItem[] AllFields { get { return Table.AllFields; } }

            /// <summary>所有绑定到数据表的属性</summary>
            public static FieldItem[] Fields { get { return Table.Fields; } }

            /// <summary>字段名列表</summary>
            public static IList<String> FieldNames { get { return Table.FieldNames; } }

            /// <summary>唯一键，返回第一个标识列或者唯一的主键</summary>
            public static FieldItem Unique
            {
                get
                {
                    if (Table.Identity != null) return Table.Identity;
                    if (Table.PrimaryKeys != null && Table.PrimaryKeys.Length > 0) return Table.PrimaryKeys[0];
                    return null;
                }
            }

            /// <summary>实体操作者</summary>
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
            /// <summary>数据操作对象。</summary>
            public static DAL DBO { get { return DAL.Create(ConnName); } }

            /// <summary>数据库类型</summary>
            public static DatabaseType DbType { get { return DBO.DbType; } }

            /// <summary>执行SQL查询，返回记录集</summary>
            /// <param name="builder">SQL语句</param>
            /// <param name="startRowIndex">开始行，0表示第一行</param>
            /// <param name="maximumRows">最大返回行数，0表示所有行</param>
            /// <returns></returns>
            public static DataSet Query(SelectBuilder builder, Int32 startRowIndex, Int32 maximumRows)
            {
                WaitForInitData();

                return DBO.Select(builder, startRowIndex, maximumRows, Meta.TableName);
            }

            /// <summary>查询</summary>
            /// <param name="sql">SQL语句</param>
            /// <returns>结果记录集</returns>
            //[Obsolete("请优先考虑使用SelectBuilder参数做查询！")]
            public static DataSet Query(String sql)
            {
                WaitForInitData();

                return DBO.Select(sql, Meta.TableName);
            }

            /// <summary>查询记录数</summary>
            /// <param name="sql">SQL语句</param>
            /// <returns>记录数</returns>
            [Obsolete("请优先考虑使用SelectBuilder参数做查询！")]
            public static Int32 QueryCount(String sql)
            {
                WaitForInitData();

                return DBO.SelectCount(sql, Meta.TableName);
            }

            /// <summary>查询记录数</summary>
            /// <param name="sb">查询生成器</param>
            /// <returns>记录数</returns>
            public static Int32 QueryCount(SelectBuilder sb)
            {
                WaitForInitData();

                return DBO.SelectCount(sb, new String[] { Meta.TableName });
            }

            /// <summary>执行</summary>
            /// <param name="sql">SQL语句</param>
            /// <returns>影响的结果</returns>
            public static Int32 Execute(String sql)
            {
                WaitForInitData();

                Int32 rs = DBO.Execute(sql, Meta.TableName);
                executeCount++;
                DataChange();
                return rs;
            }

            /// <summary>执行插入语句并返回新增行的自动编号</summary>
            /// <param name="sql">SQL语句</param>
            /// <returns>新增行的自动编号</returns>
            public static Int64 InsertAndGetIdentity(String sql)
            {
                WaitForInitData();

                Int64 rs = DBO.InsertAndGetIdentity(sql, Meta.TableName);
                executeCount++;
                DataChange();
                return rs;
            }

            /// <summary>执行</summary>
            /// <param name="sql">SQL语句</param>
            /// <param name="type">命令类型，默认SQL文本</param>
            /// <param name="ps">命令参数</param>
            /// <returns>影响的结果</returns>
            public static Int32 Execute(String sql, CommandType type = CommandType.Text, params DbParameter[] ps)
            {
                WaitForInitData();

                Int32 rs = DBO.Execute(sql, type, ps, Meta.TableName);
                executeCount++;
                DataChange();
                return rs;
            }

            /// <summary>执行插入语句并返回新增行的自动编号</summary>
            /// <param name="sql">SQL语句</param>
            /// <param name="type">命令类型，默认SQL文本</param>
            /// <param name="ps">命令参数</param>
            /// <returns>新增行的自动编号</returns>
            public static Int64 InsertAndGetIdentity(String sql, CommandType type = CommandType.Text, params DbParameter[] ps)
            {
                WaitForInitData();

                Int64 rs = DBO.InsertAndGetIdentity(sql, type, ps, Meta.TableName);
                executeCount++;
                DataChange();
                return rs;
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
            /// <summary>数据改变后触发。参数指定触发该事件的实体类</summary>
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

            static Int32[] hasCheckModel = new Int32[] { 0 };
            private static void CheckModel()
            {
                //if (Interlocked.CompareExchange(ref hasCheckModel, 1, 0) != 0) return;
                if (hasCheckModel[0] > 0) return;
                lock (hasCheckModel)
                {
                    if (hasCheckModel[0] > 0) return;

                    if (!DAL.NegativeEnable || DAL.NegativeExclude.Contains(ConnName) || DAL.NegativeExclude.Contains(TableName) || IsGenerated)
                    {
                        hasCheckModel[0] = 1;
                        return;
                    }

                    // 输出调用者，方便调试
#if DEBUG
                    if (DAL.Debug) DAL.WriteLog("检查实体{0}的数据表架构，模式：{1}，调用栈：{2}", ThisType.FullName, Table.ModelCheckMode, Helper.GetCaller());
#else
                    // CheckTableWhenFirstUse的实体类，在这里检查，有点意思，记下来
                    if (DAL.Debug && Table.ModelCheckMode == ModelCheckModes.CheckTableWhenFirstUse)
                        DAL.WriteLog("检查实体{0}的数据表架构，模式：{1}，调用栈：{2}", ThisType.FullName, Table.ModelCheckMode, XTrace.GetCaller(0, 0, "<-"));
#endif

                    // 第一次使用才检查的，此时检查
                    Boolean ck = false;
                    if (Table.ModelCheckMode == ModelCheckModes.CheckTableWhenFirstUse) ck = true;
                    // 或者前面初始化的时候没有涉及的，也在这个时候检查
                    if (!DBO.HasCheckTables.Contains(TableName))
                    {
                        DBO.HasCheckTables.Add(TableName);

#if DEBUG
                        if (!ck && DAL.Debug) DAL.WriteLog("集中初始化表架构时没赶上，现在补上！");
#endif

                        ck = true;
                    }
                    if (ck)
                    {
                        Func check = delegate
                        {
#if DEBUG
                            DAL.WriteLog("开始{2}检查表[{0}/{1}]的数据表架构……", Table.DataTable.Name, DbType, DAL.NegativeCheckOnly ? "异步" : "同步");
#endif

                            var sw = new Stopwatch();
                            sw.Start();

                            try
                            {
                                //var set = new NegativeSetting();
                                //set.CheckOnly = DAL.NegativeCheckOnly;
                                //set.NoDelete = DAL.NegativeNoDelete;
                                //DBO.Db.CreateMetaData().SetTables(set, Table.DataTable);
                                DBO.SetTables(Table.DataTable);
                            }
                            finally
                            {
                                sw.Stop();

#if DEBUG
                                DAL.WriteLog("检查表[{0}/{1}]的数据表架构耗时{2}", Table.DataTable.Name, DbType, sw.Elapsed);
#endif
                            }
                        };

                        // 打开了开关，并且设置为true时，使用同步方式检查
                        // 设置为false时，使用异步方式检查，因为上级的意思是不大关心数据库架构
                        if (!DAL.NegativeCheckOnly)
                            check();
                        else
                            ThreadPoolX.QueueUserWorkItem(check);
                    }

                    hasCheckModel[0] = 1;
                }
            }

            private static Boolean IsGenerated { get { return ThisType.GetCustomAttribute<CompilerGeneratedAttribute>(true) != null; } }

            /// <summary>记录已进行数据初始化的表</summary>
            static Dictionary<String, AutoResetEvent> hasCheckInitData = new Dictionary<string, AutoResetEvent>(StringComparer.OrdinalIgnoreCase);
            //static List<String> hasCheckInitData = new List<String>();

            /// <summary>检查并初始化数据。参数等待时间为0表示不等待</summary>
            /// <param name="millisecondsTimeout">等待时间，-1表示不限，0表示不等待</param>
            /// <returns>如果等待，返回是否收到信号</returns>
            public static Boolean WaitForInitData(Int32 millisecondsTimeout = 0)
            {
                String key = ConnName + "$$$" + TableName;
                AutoResetEvent e;
                if (hasCheckInitData.TryGetValue(key, out e))
                {
                    // 是否需要等待
                    if (millisecondsTimeout != 0 && e != null)
                    {
#if DEBUG
                        if (DAL.Debug) DAL.WriteLog("开始等待初始化{0}数据{2}ms，调用栈：{1}", ThisType.FullName, Helper.GetCaller(), millisecondsTimeout);
#endif
                        try
                        {
                            // 如果未收到信号，表示超时
                            if (!e.WaitOne(millisecondsTimeout, false)) return false;
                        }
                        finally
                        {
#if DEBUG
                            if (DAL.Debug) DAL.WriteLog("结束等待初始化{0}数据，调用栈：{1}", ThisType.FullName, Helper.GetCaller());
#endif
                        }
                    }
                    return true;
                }

                e = new AutoResetEvent(false);
                if (hasCheckInitData.ContainsKey(key)) return true;
                hasCheckInitData.Add(key, e);

                // 如果该实体类是首次使用检查模型，则在这个时候检查
                CheckModel();

                // 输出调用者，方便调试
#if DEBUG
                if (DAL.Debug) DAL.WriteLog("初始化{0}数据，调用栈：{1}", ThisType.FullName, Helper.GetCaller());
#endif

                try
                {
                    EntityBase entity = Factory.Default as EntityBase;
                    if (entity != null) entity.InitData();
                }
                catch (Exception ex)
                {
                    if (XTrace.Debug) XTrace.WriteLine("初始化数据出错！" + ex.ToString());
                }
                finally { e.Set(); }

                return true;
            }
            #endregion

            #region 事务保护
            [ThreadStatic]
            private static Int32 TransCount = 0;
            [ThreadStatic]
            private static Int32 executeCount = 0;

            /// <summary>开始事务</summary>
            /// <returns></returns>
            public static Int32 BeginTrans()
            {
                // 可能存在多层事务，这里不能把这个清零
                //executeCount = 0;
                return TransCount = DBO.BeginTransaction();
            }

            /// <summary>提交事务</summary>
            /// <returns></returns>
            public static Int32 Commit()
            {
                TransCount = DBO.Commit();
                // 提交事务时更新数据，虽然不是绝对准确，但没有更好的办法
                // 即使提交了事务，但只要事务内没有执行更新数据的操作，也不更新
                // 2012-06-13 测试证明，修改数据后，提交事务后会更新缓存等数据
                if (TransCount <= 0 && executeCount > 0)
                {
                    DataChange();
                    // 回滚到顶层才更新数据
                    executeCount = 0;
                }
                return TransCount;
            }

            /// <summary>回滚事务</summary>
            /// <returns></returns>
            public static Int32 Rollback()
            {
                TransCount = DBO.Rollback();
                // 回滚的时候貌似不需要更新缓存
                //if (TransCount <= 0 && executeCount > 0) DataChange();
                if (TransCount <= 0 && executeCount > 0) executeCount = 0;
                return TransCount;
            }
            #endregion

            #region 参数化
            /// <summary>创建参数</summary>
            /// <returns></returns>
            public static DbParameter CreateParameter() { return DBO.Db.Factory.CreateParameter(); }

            /// <summary>格式化参数名</summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public static String FormatParameterName(String name) { return DBO.Db.FormatParameterName(name); }
            #endregion

            #region 辅助方法
            /// <summary>格式化关键字</summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public static String FormatName(String name)
            {
                return DBO.Db.FormatName(name);
            }

            /// <summary>格式化关键字</summary>
            /// <param name="name"></param>
            /// <returns></returns>
            [Obsolete("改为使用FormatName")]
            public static String FormatKeyWord(String name)
            {
                return FormatName(name);
            }

            /// <summary>格式化时间</summary>
            /// <param name="dateTime"></param>
            /// <returns></returns>
            public static String FormatDateTime(DateTime dateTime)
            {
                return DBO.Db.FormatDateTime(dateTime);
            }

            /// <summary>格式化数据为SQL数据</summary>
            /// <param name="name"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public static String FormatValue(String name, Object value)
            {
                return FormatValue(Table.FindByName(name), value);
            }

            /// <summary>格式化数据为SQL数据</summary>
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
            /// <summary>实体缓存</summary>
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
                    String key = String.Format("{0}_{1}_{2}_Count", ConnName, TableName, ThisType.Name);

                    // By 大石头 2012-05-27
                    // 这里好像有问题，不明白上次为什么要注释
                    Int64? n = _Count;
                    if (n != null && n.HasValue)
                    {
                        if (n.Value > 0 && n.Value < 1000) return n.Value;

                        // 大于1000，使用HttpCache
                        Int64? k = (Int64?)HttpRuntime.Cache[key];
                        if (k != null && k.HasValue) return k.Value;
                    }

                    CheckModel();

                    Int64 m = 0;
                    //if (n != null && n.HasValue && n.Value < 1000)
                    //Int64? n = _Count;
                    if (n != null && n.HasValue && n.Value < 1000)
                        m = FindCount();
                    else
                        m = DBO.Session.QueryCountFast(TableName);
                    _Count = m;

                    if (m >= 1000) HttpRuntime.Cache.Insert(key, m, null, DateTime.Now.AddMinutes(10), System.Web.Caching.Cache.NoSlidingExpiration);

                    // 先拿到记录数再初始化，因为初始化时会用到记录数，同时也避免了死循环
                    WaitForInitData();

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

                //String key = ThisType.Name + "_Count";
                String key = String.Format("{0}_{1}_{2}_Count", ConnName, TableName, ThisType.Name);
                HttpRuntime.Cache.Remove(key);
            }
            #endregion

            #region 一些设置
            [ThreadStatic]
            private static Boolean _AllowInsertIdentity;
            /// <summary>是否允许向自增列插入数据。为免冲突，仅本线程有效</summary>
            public static Boolean AllowInsertIdentity { get { return _AllowInsertIdentity; } set { _AllowInsertIdentity = value; } }

            private static FieldItem _AutoSetGuidField;
            /// <summary>自动设置Guid的字段。对实体类有效，可在实体类类型构造函数里面设置</summary>
            public static FieldItem AutoSetGuidField { get { return _AutoSetGuidField; } set { _AutoSetGuidField = value; } }
            #endregion
        }
    }
}