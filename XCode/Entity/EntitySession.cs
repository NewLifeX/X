using System;
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
using XCode.Model;

namespace XCode
{
    /// <summary>实体会话。每个实体类、连接名和表名形成一个实体会话</summary>
    public class EntitySession<TEntity> : IEntitySession where TEntity : Entity<TEntity>, new()
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

        IEntityOperate Operate { get { return EntityFactory.CreateOperate(ThisType); } }

        private DAL _Dal;
        /// <summary>数据操作层</summary>
        internal DAL Dal { get { return _Dal ?? (_Dal = DAL.Create(ConnName)); } }

        private String _FormatedTableName;
        /// <summary>已格式化的表名，带有中括号等</summary>
        public virtual String FormatedTableName { get { return _FormatedTableName ?? (_FormatedTableName = Dal.Db.FormatName(TableName)); } }
        #endregion

        #region 数据初始化
        /// <summary>记录已进行数据初始化</summary>
        Boolean hasCheckInitData = false;
        Int32 initThread = 0;
        Object _wait_lock = new Object();

        /// <summary>检查并初始化数据。参数等待时间为0表示不等待</summary>
        /// <param name="ms">等待时间，-1表示不限，0表示不等待</param>
        /// <returns>如果等待，返回是否收到信号</returns>
        public Boolean WaitForInitData(Int32 ms = 1000)
        {
            // 已初始化
            if (hasCheckInitData) return true;

            //!!! 一定一定小心堵塞的是自己
            if (initThread == Thread.CurrentThread.ManagedThreadId) return true;

            if (!Monitor.TryEnter(_wait_lock, ms))
            {
                //if (DAL.Debug) DAL.WriteLog("开始等待初始化{0}数据{1}ms，调用栈：{2}", name, ms, XTrace.GetCaller());
                if (DAL.Debug) DAL.WriteLog("等待初始化{0}数据{1:n0}ms失败", ThisType.Name, ms);
                return false;
            }
            initThread = Thread.CurrentThread.ManagedThreadId;
            try
            {
                // 已初始化
                if (hasCheckInitData) return true;

                var name = ThisType.Name;
                if (name == TableName)
                    name = String.Format("{0}/{1}", ThisType.Name, ConnName);
                else
                    name = String.Format("{0}@{1}/{2}", ThisType.Name, TableName, ConnName);

                // 如果该实体类是首次使用检查模型，则在这个时候检查
                try
                {
                    CheckModel();
                }
                catch { }

                // 输出调用者，方便调试
                //if (DAL.Debug) DAL.WriteLog("初始化{0}数据，调用栈：{1}", name, XTrace.GetCaller());
                if (DAL.Debug) DAL.WriteLog("初始化{0}数据", name);

                try
                {
                    var entity = Operate.Default as EntityBase;
                    if (entity != null) entity.InitData();
                }
                catch (Exception ex)
                {
                    if (XTrace.Debug) XTrace.WriteLine("初始化数据出错！" + ex.ToString());
                }

                return true;
            }
            finally
            {
                hasCheckInitData = true;
                Monitor.Exit(_wait_lock);
            }
        }
        #endregion

        #region 架构检查
        private void CheckTable()
        {
#if DEBUG
            DAL.WriteLog("开始{2}检查表[{0}/{1}]的数据表架构……", Table.DataTable.Name, Dal.Db.DbType, DAL.NegativeCheckOnly ? "异步" : "同步");
#endif

            var sw = new Stopwatch();
            sw.Start();

            try
            {
                // 检查新表名对应的数据表
                var table = Table.DataTable;
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

                if (!Dal.HasCheckTables.Contains(TableName)) Dal.HasCheckTables.Add(TableName);
                Dal.SetTables(table);
            }
            finally
            {
                sw.Stop();

#if DEBUG
                DAL.WriteLog("检查表[{0}/{1}]的数据表架构耗时{2:n0}ms", Table.DataTable.Name, Dal.DbType, sw.Elapsed.TotalMilliseconds);
#endif
            }
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
                //if (DAL.Debug) DAL.WriteLog("检查实体{0}的数据表架构，模式：{1}，调用栈：{2}", ThisType.FullName, Table.ModelCheckMode, XTrace.GetCaller(1, 0, "\r\n<-"));
                // CheckTableWhenFirstUse的实体类，在这里检查，有点意思，记下来
                if (DAL.Debug && Table.ModelCheckMode == ModelCheckModes.CheckTableWhenFirstUse)
                    DAL.WriteLog("检查实体{0}的数据表架构，模式：{1}", ThisType.FullName, Table.ModelCheckMode);

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
                    // 打开了开关，并且设置为true时，使用同步方式检查
                    // 设置为false时，使用异步方式检查，因为上级的意思是不大关心数据库架构
                    if (!DAL.NegativeCheckOnly)
                        CheckTable();
                    else
                        ThreadPoolX.QueueUserWorkItem(CheckTable);
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
                //return _singleCache ?? (_singleCache = new SingleEntityCache<Object, TEntity> { ConnName = ConnName, TableName = TableName });
                if (_singleCache == null)
                {
                    _singleCache = new SingleEntityCache<Object, TEntity>();
                    _singleCache.ConnName = ConnName;
                    _singleCache.TableName = TableName;
                    _singleCache.Expriod = Entity<TEntity>.Meta.SingleEntityCacheExpriod;
                    _singleCache.MaxEntity = Entity<TEntity>.Meta.SingleEntityCacheMaxEntity;
                    _singleCache.AutoSave = Entity<TEntity>.Meta.SingleEntityCacheAutoSave;
                    _singleCache.AllowNull = Entity<TEntity>.Meta.SingleEntityCacheAllowNull;
                    _singleCache.FindKeyMethodInternal = Entity<TEntity>.Meta.SingleEntityCacheFindKeyMethod;
                }
                return _singleCache;
            }
        }

        IEntityCache IEntitySession.Cache { get { return Cache; } }
        ISingleEntityCache IEntitySession.SingleCache { get { return SingleCache; } }

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
                // 大于1000的缓存过期
                if (n != null && n.HasValue && n.Value < 1000)
                {
                    var sb = new SelectBuilder();
                    sb.Table = FormatedTableName;

                    WaitForInitData();
                    m = Dal.SelectCount(sb, new String[] { TableName });
                }
                else
                {
                    // 第一次访问
                    m = Dal.Session.QueryCountFast(TableName);
                }

                _Count = m;

                if (m >= 1000) HttpRuntime.Cache.Insert(key, m, null, DateTime.Now.AddMinutes(10), System.Web.Caching.Cache.NoSlidingExpiration);

                // 先拿到记录数再初始化，因为初始化时会用到记录数，同时也避免了死循环
                WaitForInitData();

                return m;
            }
        }

        /// <summary>清除缓存</summary>
        /// <param name="reason">原因</param>
        public void ClearCache(String reason = null)
        {
            if (_cache != null) _cache.Clear(reason);

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

        #region 数据库操作
        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="builder">SQL语句</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns></returns>
        public virtual DataSet Query(SelectBuilder builder, Int32 startRowIndex, Int32 maximumRows)
        {
            WaitForInitData();

            //builder.Table = FormatedTableName;
            return Dal.Select(builder, startRowIndex, maximumRows, TableName);
        }

        /// <summary>查询</summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>结果记录集</returns>
        //[Obsolete("请优先考虑使用SelectBuilder参数做查询！")]
        public virtual DataSet Query(String sql)
        {
            WaitForInitData();

            return Dal.Select(sql, TableName);
        }

        /// <summary>查询记录数</summary>
        /// <param name="builder">查询生成器</param>
        /// <returns>记录数</returns>
        public virtual Int32 QueryCount(SelectBuilder builder)
        {
            WaitForInitData();

            //builder.Table = FormatedTableName;
            return Dal.SelectCount(builder, new String[] { TableName });
        }

        /// <summary>根据条件把普通查询SQL格式化为分页SQL。</summary>
        /// <remarks>
        /// 因为需要继承重写的原因，在数据类中并不方便缓存分页SQL。
        /// 所以在这里做缓存。
        /// </remarks>
        /// <param name="builder">查询生成器</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>分页SQL</returns>
        public virtual SelectBuilder PageSplit(SelectBuilder builder, Int32 startRowIndex, Int32 maximumRows)
        {
            //builder.Table = FormatedTableName;
            return Dal.PageSplit(builder, startRowIndex, maximumRows);
        }

        /// <summary>执行</summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>影响的结果</returns>
        public virtual Int32 Execute(String sql)
        {
            WaitForInitData();

            Int32 rs = Dal.Execute(sql, TableName);
            executeCount++;
            DataChange("修改数据");
            return rs;
        }

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>新增行的自动编号</returns>
        public virtual Int64 InsertAndGetIdentity(String sql)
        {
            WaitForInitData();

            Int64 rs = Dal.InsertAndGetIdentity(sql, TableName);
            executeCount++;
            DataChange("修改数据");
            return rs;
        }

        /// <summary>执行</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>影响的结果</returns>
        public virtual Int32 Execute(String sql, CommandType type = CommandType.Text, params DbParameter[] ps)
        {
            WaitForInitData();

            Int32 rs = Dal.Execute(sql, type, ps, TableName);
            executeCount++;
            DataChange("修改数据");
            return rs;
        }

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>新增行的自动编号</returns>
        public virtual Int64 InsertAndGetIdentity(String sql, CommandType type = CommandType.Text, params DbParameter[] ps)
        {
            WaitForInitData();

            Int64 rs = Dal.InsertAndGetIdentity(sql, type, ps, TableName);
            executeCount++;
            DataChange("修改数据");
            return rs;
        }

        void DataChange(String reason = null)
        {
            // 还在事务保护里面，不更新缓存，最后提交或者回滚的时候再更新
            // 一般事务保护用于批量更新数据，频繁删除缓存将会打来巨大的性能损耗
            // 2012-07-17 当前实体类开启的事务保护，必须由当前类结束，否则可能导致缓存数据的错乱
            if (TransCount > 0) return;

            ClearCache(reason);

            if (_OnDataChange != null) _OnDataChange(ThisType);
        }

        private Action<Type> _OnDataChange;
        /// <summary>数据改变后触发。参数指定触发该事件的实体类</summary>
        public event Action<Type> OnDataChange
        {
            add
            {
                if (value != null)
                {
                    // 这里不能对委托进行弱引用，因为GC会回收委托，应该改为对对象进行弱引用
                    //WeakReference<Action<Type>> w = value;

                    _OnDataChange += new WeakAction<Type>(value, handler => { _OnDataChange -= handler; }, true);
                }
            }
            remove { }
        }
        #endregion

        #region 事务保护
        private Int32 _TransCount;
        /// <summary>事务计数</summary>
        public virtual Int32 TransCount { get { return _TransCount; } private set { _TransCount = value; } }

        private Int32 executeCount = 0;

        /// <summary>开始事务</summary>
        /// <returns>剩下的事务计数</returns>
        public virtual Int32 BeginTrans()
        {
            // 可能存在多层事务，这里不能把这个清零
            //executeCount = 0;
            return TransCount = Dal.BeginTransaction();
        }

        /// <summary>提交事务</summary>
        /// <returns>剩下的事务计数</returns>
        public virtual Int32 Commit()
        {
            TransCount = Dal.Commit();
            // 提交事务时更新数据，虽然不是绝对准确，但没有更好的办法
            // 即使提交了事务，但只要事务内没有执行更新数据的操作，也不更新
            // 2012-06-13 测试证明，修改数据后，提交事务后会更新缓存等数据
            if (TransCount <= 0 && executeCount > 0)
            {
                DataChange("修改数据后提交事务");
                // 回滚到顶层才更新数据
                executeCount = 0;
            }
            return TransCount;
        }

        /// <summary>回滚事务，忽略异常</summary>
        /// <returns>剩下的事务计数</returns>
        public virtual Int32 Rollback()
        {
            TransCount = Dal.Rollback();
            // 回滚的时候貌似不需要更新缓存
            //if (TransCount <= 0 && executeCount > 0) DataChange();
            if (TransCount <= 0 && executeCount > 0)
            {
                // 因为在事务保护中添加或删除实体时直接操作了实体缓存，所以需要更新
                DataChange("修改数据后回滚事务");
                executeCount = 0;
            }
            return TransCount;
        }

        /// <summary>是否在事务保护中</summary>
        internal Boolean UsingTrans { get { return TransCount > 0; } }
        #endregion

        #region 参数化
        /// <summary>创建参数</summary>
        /// <returns></returns>
        public virtual DbParameter CreateParameter() { return Dal.Db.Factory.CreateParameter(); }

        /// <summary>格式化参数名</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual String FormatParameterName(String name) { return Dal.Db.FormatParameterName(name); }
        #endregion

        #region 实体操作
        private IEntityPersistence persistence { get { return XCodeService.Container.ResolveInstance<IEntityPersistence>(); } }

        /// <summary>把该对象持久化到数据库，添加/更新实体缓存。</summary>
        /// <param name="entity">实体对象</param>
        /// <returns></returns>
        public virtual Int32 Insert(IEntity entity)
        {
            var rs = persistence.Insert(entity);

            // 如果当前在事务中，并使用了缓存，则尝试更新缓存
            if (UsingTrans && Cache.Using)
            {
                // 尽管用了事务保护，但是仍然可能有别的地方导致实体缓存更新，这点务必要注意
                var fi = Operate.Unique;
                var e = Cache.Entities.Find(fi.Name, entity[fi.Name]);
                if (e != null)
                {
                    if (e != entity) e.CopyFrom(entity);
                }
                else
                    Cache.Entities.Add(entity as TEntity);
            }

            return rs;
        }

        /// <summary>更新数据库，同时更新实体缓存</summary>
        /// <param name="entity">实体对象</param>
        /// <returns></returns>
        public virtual Int32 Update(IEntity entity)
        {
            var rs = persistence.Update(entity);

            // 如果当前在事务中，并使用了缓存，则尝试更新缓存
            if (UsingTrans && Cache.Using)
            {
                // 尽管用了事务保护，但是仍然可能有别的地方导致实体缓存更新，这点务必要注意
                var fi = Operate.Unique;
                var e = Cache.Entities.Find(fi.Name, entity[fi.Name]);
                if (e != null)
                {
                    if (e != entity) e.CopyFrom(entity);
                }
                else
                    Cache.Entities.Add(entity as TEntity);
            }

            return rs;
        }

        /// <summary>从数据库中删除该对象，同时从实体缓存中删除</summary>
        /// <param name="entity">实体对象</param>
        /// <returns></returns>
        public virtual Int32 Delete(IEntity entity)
        {
            var rs = persistence.Delete(entity);

            // 如果当前在事务中，并使用了缓存，则尝试更新缓存
            if (UsingTrans && Cache.Using)
            {
                var fi = Operate.Unique;
                if (fi != null)
                {
                    var v = entity[fi.Name];
                    Cache.Entities.RemoveAll(e => Object.Equals(e[fi.Name], v));
                }
            }

            return rs;
        }
        #endregion
    }
}