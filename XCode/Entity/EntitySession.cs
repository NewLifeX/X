using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Web;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Threading;
using XCode.Cache;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XCode
{
    /// <summary>实体会话。每个实体类、连接名和表名形成一个实体会话</summary>
    public class EntitySession<TEntity> where TEntity : Entity<TEntity>, new()
    {
        #region 属性
        private String _ConnName;
        /// <summary>连接名</summary>
        public String ConnName { get { return _ConnName; } private set { _ConnName = value; _Key = null; } }

        private String _TableName;
        /// <summary>表名</summary>
        public String TableName { get { return _TableName; } private set { _TableName = value; _Key = null; } }

        private String _Key;
        /// <summary>用于标识会话的键值</summary>
        public String Key { get { return _Key ?? (_Key = String.Format("{0}$$${1}", ConnName, TableName)); } }
        #endregion

        #region 构造
        private EntitySession() { }

        private static DictionaryCache<String, EntitySession<TEntity>> _es = new DictionaryCache<string, EntitySession<TEntity>>(StringComparer.OrdinalIgnoreCase);
        /// <summary>创建指定表名连接名的会话</summary>
        /// <param name="connName"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static EntitySession<TEntity> Create(String connName, String tableName)
        {
            if (String.IsNullOrEmpty(connName)) throw new ArgumentNullException("connName");
            if (String.IsNullOrEmpty(tableName)) throw new ArgumentNullException("tableName");

            var key = connName + "$$$" + tableName;
            return _es.GetItem<String, String>(key, connName, tableName, (k, c, t) => new EntitySession<TEntity> { ConnName = c, TableName = t });
        }
        #endregion

        #region 主要属性
        private Type ThisType { get { return typeof(TEntity); } }

        /// <summary>表信息</summary>
        TableItem Table { get { return TableItem.Create(ThisType); } }
        #endregion

        #region 数据初始化
        /// <summary>记录已进行数据初始化</summary>
        AutoResetEvent hasCheckInitData = null;
        /// <summary>初始化状态，0未初始化，1正在初始化，2已完成</summary>
        Int32 initState = 0;

        /// <summary>检查并初始化数据。参数等待时间为0表示不等待</summary>
        /// <param name="ms">等待时间，-1表示不限，0表示不等待</param>
        /// <returns>如果等待，返回是否收到信号</returns>
        public Boolean WaitForInitData(Int32 ms = 1000)
        {
            // 已初始化
            if (initState >= 2) return true;

            var name = ThisType.FullName;

            // 是否需要等待
            if (initState == 1)
            {
                #region 等待初始化
                //if (DAL.Debug) DAL.WriteLog("开始等待初始化{0}数据{1}ms，调用栈：{2}", name, ms, XTrace.GetCaller());
                if (DAL.Debug) DAL.WriteLog("开始等待初始化{0}数据{1}ms", name, ms);
                try
                {
                    // 如果未收到信号，表示超时
                    if (!hasCheckInitData.WaitOne(ms, false)) return false;
                    return true;
                }
                finally
                {
                    //if (DAL.Debug) DAL.WriteLog("结束等待初始化{0}数据，调用栈：{1}", name, XTrace.GetCaller());
                    if (DAL.Debug) DAL.WriteLog("结束等待初始化{0}数据", name);
                }
                #endregion
            }

            if (hasCheckInitData != null) return true;
            hasCheckInitData = new AutoResetEvent(false);
            initState = 1;

            // 如果该实体类是首次使用检查模型，则在这个时候检查
            CheckModel();

            // 输出调用者，方便调试
            //if (DAL.Debug) DAL.WriteLog("初始化{0}数据，调用栈：{1}", name, XTrace.GetCaller());
            if (DAL.Debug) DAL.WriteLog("初始化{0}数据", name);

            try
            {
                var entity = EntityFactory.CreateOperate(ThisType).Default as EntityBase;
                if (entity != null) entity.InitData();
            }
            catch (Exception ex)
            {
                if (XTrace.Debug) XTrace.WriteLine("初始化数据出错！" + ex.ToString());
            }
            finally
            {
                hasCheckInitData.Set();
                hasCheckInitData.Close();

                initState = 2;
            }

            return true;
        }
        #endregion

        #region 架构检查
        private void CheckTable()
        {
            // 检查新表名对应的数据表
            var table = TableItem.Create(ThisType).DataTable;
            // 克隆一份，防止修改
            table = table.Clone() as IDataTable;

            if (table.TableName != TableName)
            {
                // 修改一下索引名，否则，可能因为同一个表里面不同的索引冲突
                if (table.Indexes != null)
                {
                    foreach (var di in table.Indexes)
                    {
                        var sb = new StringBuilder();
                        sb.AppendFormat("IX_{0}", TableName);
                        foreach (var item in di.Columns)
                        {
                            sb.Append("_");
                            sb.Append(item);
                        }

                        di.Name = sb.ToString();
                    }
                }
                table.TableName = TableName;
            }

            //var set = new NegativeSetting();
            //set.CheckOnly = DAL.NegativeCheckOnly;
            //set.NoDelete = DAL.NegativeNoDelete;
            //DAL.Create(connName).Db.CreateMetaData().SetTables(set, table);

            var dal = DAL.Create(ConnName);
            if (!dal.HasCheckTables.Contains(TableName)) dal.HasCheckTables.Add(TableName);
            DAL.Create(ConnName).SetTables(table);
        }

        private Boolean IsGenerated { get { return ThisType.GetCustomAttribute<CompilerGeneratedAttribute>(true) != null; } }
        Boolean hasCheckModel = false;
        Object _check_lock = new Object();
        private void CheckModel()
        {
            if (hasCheckModel) return;
            lock (_check_lock)
            {
                if (hasCheckModel) return;

                if (!DAL.NegativeEnable || DAL.NegativeExclude.Contains(ConnName) || DAL.NegativeExclude.Contains(TableName) || IsGenerated)
                {
                    hasCheckModel = true;
                    return;
                }

                // 输出调用者，方便调试
#if DEBUG
                if (DAL.Debug) DAL.WriteLog("检查实体{0}的数据表架构，模式：{1}，调用栈：{2}", ThisType.FullName, Table.ModelCheckMode, XTrace.GetCaller());
#else
                // CheckTableWhenFirstUse的实体类，在这里检查，有点意思，记下来
                if (DAL.Debug && Table.ModelCheckMode == ModelCheckModes.CheckTableWhenFirstUse)
                    DAL.WriteLog("检查实体{0}的数据表架构，模式：{1}", ThisType.FullName, Table.ModelCheckMode);
#endif

                // 第一次使用才检查的，此时检查
                Boolean ck = false;
                if (Table.ModelCheckMode == ModelCheckModes.CheckTableWhenFirstUse) ck = true;
                // 或者前面初始化的时候没有涉及的，也在这个时候检查
                var dal = DAL.Create(ConnName);
                if (!dal.HasCheckTables.Contains(TableName))
                {
                    if (!ck)
                    {
                        dal.HasCheckTables.Add(TableName);

#if DEBUG
                        if (!ck && DAL.Debug) DAL.WriteLog("集中初始化表架构时没赶上，现在补上！");
#endif

                        ck = true;
                    }
                }
                else
                    ck = false;

                if (ck)
                {
                    Func check = delegate
                    {
#if DEBUG
                        DAL.WriteLog("开始{2}检查表[{0}/{1}]的数据表架构……", Table.DataTable.Name, dal.Db.DbType, DAL.NegativeCheckOnly ? "异步" : "同步");
#endif

                        var sw = new Stopwatch();
                        sw.Start();

                        try
                        {
                            //var set = new NegativeSetting();
                            //set.CheckOnly = DAL.NegativeCheckOnly;
                            //set.NoDelete = DAL.NegativeNoDelete;
                            //DBO.Db.CreateMetaData().SetTables(set, Table.DataTable);
                            dal.SetTables(Table.DataTable);
                        }
                        finally
                        {
                            sw.Stop();

#if DEBUG
                            DAL.WriteLog("检查表[{0}/{1}]的数据表架构耗时{2:n0}ms", Table.DataTable.Name, dal.Db.DbType, sw.Elapsed.TotalMilliseconds);
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

                hasCheckModel = true;
            }
        }
        #endregion

        #region 缓存
        private EntityCache<TEntity> _cache;
        /// <summary>实体缓存</summary>
        /// <returns></returns>
        public EntityCache<TEntity> Cache
        {
            get
            {
                // 以连接名和表名为key，因为不同的库不同的表，缓存也不一样
                return _cache ?? (_cache = new EntityCache<TEntity> { ConnName = ConnName, TableName = TableName });
            }
        }

        private SingleEntityCache<Object, TEntity> _singleCache;
        /// <summary>单对象实体缓存。
        /// 建议自定义查询数据方法，并从二级缓存中获取实体数据，以抵消因初次填充而带来的消耗。
        /// </summary>
        public SingleEntityCache<Object, TEntity> SingleCache
        {
            get
            {
                // 以连接名和表名为key，因为不同的库不同的表，缓存也不一样
                return _singleCache ?? (_singleCache = new SingleEntityCache<Object, TEntity> { ConnName = ConnName, TableName = TableName });
            }
        }

        /// <summary>总记录数，小于1000时是精确的，大于1000时缓存10分钟</summary>
        public Int32 Count { get { return (Int32)LongCount; } }

        /// <summary>总记录数较小时，使用静态字段，较大时增加使用Cache</summary>
        private Int64? _Count;
        /// <summary>总记录数，小于1000时是精确的，大于1000时缓存10分钟</summary>
        /// <remarks>
        /// 1，检查静态字段，如果有数据且小于1000，直接返回，否则=>3
        /// 2，如果有数据但大于1000，则返回缓存里面的有效数据
        /// 3，来到这里，有可能是第一次访问，静态字段没有缓存，也有可能是大于1000的缓存过期
        /// 4，检查模型
        /// 5，根据需要查询数据
        /// 6，如果大于1000，缓存数据
        /// 7，检查数据初始化
        /// </remarks>
        public Int64 LongCount
        {
            get
            {
                var key = CacheKey;

                Int64? n = _Count;
                if (n != null && n.HasValue)
                {
                    // 等于0的时候也应该缓存，否则会一直查询这个表
                    if (n.Value >= 0 && n.Value < 1000) return n.Value;

                    // 大于1000，使用HttpCache
                    Int64? k = (Int64?)HttpRuntime.Cache[key];
                    if (k != null && k.HasValue) return k.Value;
                }
                // 来到这里，有可能是第一次访问，静态字段没有缓存，也有可能是大于1000的缓存过期

                CheckModel();

                Int64 m = 0;
                var dal = DAL.Create(ConnName);
                // 大于1000的缓存过期
                if (n != null && n.HasValue && n.Value < 1000)
                {
                    var sb = new SelectBuilder();
                    sb.Table = dal.Db.FormatName(TableName);

                    WaitForInitData();
                    m = dal.SelectCount(sb, new String[] { TableName });
                }
                else
                {
                    // 第一次访问
                    m = dal.Session.QueryCountFast(TableName);
                }

                _Count = m;

                if (m >= 1000) HttpRuntime.Cache.Insert(key, m, null, DateTime.Now.AddMinutes(10), System.Web.Caching.Cache.NoSlidingExpiration);

                // 先拿到记录数再初始化，因为初始化时会用到记录数，同时也避免了死循环
                WaitForInitData();

                return m;
            }
        }

        public void ClearCache()
        {
            if (_cache != null) _cache.Clear();

            Int64? n = _Count;
            if (n == null || !n.HasValue) return;

            // 只有小于1000时才清空_Count，因为大于1000时它要作为HttpCache的见证
            if (n.Value < 1000)
            {
                _Count = null;
                return;
            }

            HttpRuntime.Cache.Remove(CacheKey);
        }

        String CacheKey { get { return String.Format("{0}_{1}_{2}_Count", ConnName, TableName, ThisType.Name); } }
        #endregion
    }
}