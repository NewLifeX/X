using System;
using System.ComponentModel;
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
using NewLife.Threading;
using XCode.Cache;
using XCode.Configuration;
using XCode.DataAccessLayer;
using XCode.Model;

/*
 * 检查表结构流程：
 * Create           创建实体会话，此时不能做任何操作，原则上各种操作要延迟到最后
 * Query/Execute    查询修改数据
 *      WaitForInitData     等待数据初始化
 *          Monitor.TryEnter    只会被调用一次，后续线程进入需要等待
 *          CheckModel          lock阻塞检查模型架构
 *              CheckTable      检查表
 *                  FixIndexName    修正索引名称
 *                  SetTables       设置表架构
 *              CheckTableAync  异步检查表
 *          InitData
 *          Monitor.Exit        初始化完成
 * Count            总记录数
 *      CheckModel
 *      WaitForInitData
 *
 * 缓存更新规则如下：
 * 1、直接执行SQL语句进行新增、编辑、删除操作，强制清空实体缓存、单对象缓存
 * 2、无论独占模式或非独占模式，使用实体对象或实体列表进行的对象操作，不再主动清空缓存。
 * 3、事务提交时对缓存的操作参考前两条
 * 4、事务回滚时一律强制清空缓存（因为无法判断什么异常触发回滚，回滚之前对缓存进行了哪些修改无法记录）
 * 5、强制清空缓存时传入执行更新操作记录数，如果存在更新操作清除实体缓存、单对象缓存。
 * 6、强制清空缓存时如果只有新增和删除操作，先判断当前实体类有无使用实体缓存，如果使用了实体缓存证明当前实体总记录数不大，
 *    清空实体缓存的同时清空单对象缓存，确保单对象缓存和实体缓存引用同一实体对象；没有实体缓存则不对单对象缓存进行清空操作。
 * */

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
        public DAL Dal { get { return _Dal ?? (_Dal = DAL.Create(ConnName)); } }

        private String _FormatedTableName;
        /// <summary>已格式化的表名，带有中括号等</summary>
        public virtual String FormatedTableName { get { return _FormatedTableName ?? (_FormatedTableName = Dal.Db.FormatName(TableName)); } }

        private EntitySession<TEntity> _Default;
        /// <summary>该实体类的默认会话。</summary>
        private EntitySession<TEntity> Default
        {
            get
            {
                if (_Default != null) return _Default;

                if (ConnName == Table.ConnName && TableName == Table.TableName)
                    _Default = this;
                else
                    _Default = Create(Table.ConnName, Table.TableName);

                return _Default;
            }
        }
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
                    name = String.Format("{0}@{1}", ThisType.Name, ConnName);
                else
                    name = String.Format("{0}#{1}@{2}", ThisType.Name, TableName, ConnName);

                // 如果该实体类是首次使用检查模型，则在这个时候检查
                try
                {
                    CheckModel();
                }
                catch { }

                // 输出调用者，方便调试
                //if (DAL.Debug) DAL.WriteLog("初始化{0}数据，调用栈：{1}", name, XTrace.GetCaller());
                //if (DAL.Debug) DAL.WriteLog("初始化{0}数据", name);

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
            //if (Dal.CheckAndAdd(TableName)) return;

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
                    FixIndexName(table);
                    table.TableName = TableName;
                }

                var set = new NegativeSetting();
                set.CheckOnly = DAL.NegativeCheckOnly;
                set.NoDelete = DAL.NegativeNoDelete;

                // 对于分库操作，强制检查架构，但不删除数据
                if (Default != this)
                {
                    set.CheckOnly = false;
                    set.NoDelete = true;
                }

                Dal.SetTables(set, table);
            }
            finally
            {
                sw.Stop();

#if DEBUG
                DAL.WriteLog("检查表[{0}/{1}]的数据表架构耗时{2:n0}ms", Table.DataTable.Name, Dal.DbType, sw.Elapsed.TotalMilliseconds);
#endif
            }
        }

        void FixIndexName(IDataTable table)
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
        }

        private Boolean IsGenerated { get { return ThisType.GetCustomAttribute<CompilerGeneratedAttribute>(true) != null; } }
        Boolean hasCheckModel = false;
        Object _check_lock = new Object();
        /// <summary>检查模型。依据反向工程设置、是否首次使用检查、是否已常规检查等</summary>
        private void CheckModel()
        {
            if (hasCheckModel) return;
            lock (_check_lock)
            {
                if (hasCheckModel) return;

                // 是否默认连接和默认表名，非默认则强制检查，并且不允许异步检查（异步检查会导致ConnName和TableName不对）
                var def = Default;

                if (def == this)
                {
                    if (!DAL.NegativeEnable ||
                        DAL.NegativeExclude.Contains(ConnName) ||
                        DAL.NegativeExclude.Contains(TableName) ||
                        IsGenerated)
                    {
                        hasCheckModel = true;
                        return;
                    }
                }
#if DEBUG
                else
                {
                    DAL.WriteLog("[{0}@{1}]非默认表名连接名，强制要求检查架构！", TableName, ConnName);
                }
#endif

                // 输出调用者，方便调试
                //if (DAL.Debug) DAL.WriteLog("检查实体{0}的数据表架构，模式：{1}，调用栈：{2}", ThisType.FullName, Table.ModelCheckMode, XTrace.GetCaller(1, 0, "\r\n<-"));
                // CheckTableWhenFirstUse的实体类，在这里检查，有点意思，记下来
                if (DAL.Debug && Table.ModelCheckMode == ModelCheckModes.CheckTableWhenFirstUse)
                    DAL.WriteLog("检查实体{0}的数据表架构，模式：{1}", ThisType.FullName, Table.ModelCheckMode);

                // 第一次使用才检查的，此时检查
                var ck = false;
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
                    if (!DAL.NegativeCheckOnly || def != this)
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
                if (_cache == null)
                {
                    var ec = new EntityCache<TEntity> { ConnName = ConnName, TableName = TableName };
                    // 从默认会话复制参数
                    if (Default != this) ec.CopySettingFrom(Default.Cache);
                    //_cache = ec;
                    Interlocked.CompareExchange<EntityCache<TEntity>>(ref _cache, ec, null);
                }
                return _cache;
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
                    var sc = new SingleEntityCache<Object, TEntity>();
                    sc.ConnName = ConnName;
                    sc.TableName = TableName;
                    //sc.Expriod = Entity<TEntity>.Meta.SingleEntityCacheExpriod;
                    //sc.MaxEntity = Entity<TEntity>.Meta.SingleEntityCacheMaxEntity;
                    //sc.AutoSave = Entity<TEntity>.Meta.SingleEntityCacheAutoSave;
                    //sc.AllowNull = Entity<TEntity>.Meta.SingleEntityCacheAllowNull;
                    //sc.FindKeyMethodInternal = Entity<TEntity>.Meta.SingleEntityCacheFindKeyMethod;

                    // 从默认会话复制参数
                    if (Default != this) sc.CopySettingFrom(Default.SingleCache);

                    //_singleCache = sc;
                    Interlocked.CompareExchange<SingleEntityCache<Object, TEntity>>(ref _singleCache, sc, null);
                }
                return _singleCache;
            }
        }

        IEntityCache IEntitySession.Cache { get { return Cache; } }
        ISingleEntityCache IEntitySession.SingleCache { get { return SingleCache; } }

        /// <summary>总记录数，小于1000时是精确的，大于1000时缓存10分钟</summary>
        public Int32 Count { get { return (Int32)LongCount; } }

        /// <summary>上一次记录数，用于衡量缓存策略，不受缓存清空</summary>
        private Int64? _LastCount;
        /// <summary>总记录数较小时，使用静态字段，较大时增加使用Cache</summary>
        private Int64 _Count = -1L;
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

                // 当前缓存的值
                Int64 n = _Count;

                // 如果有缓存，则考虑返回吧
                if (n > -1L)
                {
                    // 等于0的时候也应该缓存，否则会一直查询这个表
                    if (n < 1000L) { return n; }

                    // 大于1000，使用HttpCache
                    Int64? k = (Int64?)HttpRuntime.Cache[key];
                    if (k != null && k.HasValue) return k.Value;
                }
                // 来到这里，有可能是第一次访问，静态字段没有缓存，也有可能是大于1000的缓存过期

                CheckModel();

                Int64 m = 0L;
                // 小于1000的精确查询，大于1000的快速查询
                if (n > -1L && n <= 1000L)
                {
                    var sb = new SelectBuilder();
                    sb.Table = FormatedTableName;

                    WaitForInitData();
                    m = Dal.SelectCount(sb, new String[] { TableName });
                }
                else
                {
                    // 第一次访问，SQLite的Select Count非常慢，数据大于阀值时，使用最大ID作为表记录数
                    var max = 0L;
                    if (Dal.DbType == DatabaseType.SQLite && Table.Identity != null)
                    {
                        // 除第一次外，将依据上一次记录数决定是否使用最大ID
                        if (_LastCount == null || _LastCount.Value > 500000)
                        {
                            // 先查一下最大值
                            //max = Entity<TEntity>.FindMax(Table.Identity.ColumnName);
                            // 依赖关系FindMax=>FindAll=>Query=>InitData=>Meta.Count，所以不能使用

                            var builder = new SelectBuilder();
                            builder.Table = FormatedTableName;
                            builder.OrderBy = Table.Identity.Desc();
                            var ds = Dal.Select(builder, 0, 1, TableName);
                            if (ds.Tables[0].Rows.Count > 0)
                                max = Convert.ToInt64(ds.Tables[0].Rows[0][Table.Identity.ColumnName]);
                        }
                    }

                    // 100w数据时，没有预热Select Count需要3000ms，预热后需要500ms
                    if (max < 500000)
                        m = Dal.Session.QueryCountFast(TableName);
                    else
                        m = max;
                }

                _Count = m;
                _LastCount = m;

                if (m >= 1000) HttpRuntime.Cache.Insert(key, m, null, DateTime.Now.AddMinutes(10), System.Web.Caching.Cache.NoSlidingExpiration);

                // 先拿到记录数再初始化，因为初始化时会用到记录数，同时也避免了死循环
                WaitForInitData();

                return m;
            }
        }

        /// <summary>清除缓存</summary>
        /// <param name="reason">原因</param>
        /// <param name="isUpdateMode">是否执行更新实体操作</param>
        /// <param name="isTransRoolback">是否在事务回滚时执行更新实体操作</param>
        private void ClearCache(String reason, Boolean isUpdateMode, Boolean isTransRoolback)
        {
            // 无法判断事务回滚是由什么样的异常触发，缓存数据可能已被修改，所有强制清空缓存
            //if (HoldCache && !isTransRoolback)
            if (!isTransRoolback) // 非独占模式下也保持缓存，由缓存有效期设置来清空
            {
                if (_cache != null && _cache.Using)
                {
                    /* 这里我先屏蔽，需要改动业务代码里是否使用实体缓存的代码，所以就不加这个判断了 */

                    // 在独占模式下，当实体记录数大于1000，清空实体缓存后不再使用
                    // 如果不清空，业务代码在记录数大于1000后不再调用实体缓存，就没有清空的机会了，
                    // 而且在独占模式或事务保护中实体的新增、编辑、删除操作会一直维护实体缓存
                    // http://www.newlifex.com/showtopic-1152.aspx
                    //if (Count > _cache.MaxCount)
                    //{
                    //    _cache.Clear("实体记录数大于{0}条，销毁实体缓存！！！".FormatWith(_cache.MaxCount));
                    //}
                }
            }
            else
            {
                ForceClearCache(reason, isUpdateMode);
            }
        }

        /// <summary>清除缓存，直接执行SQl语句或事务回滚时使用</summary>
        /// <param name="reason">原因</param>
        /// <param name="isUpdateMode">是否执行更新实体操作</param>
        /// <remarks>http://www.newlifex.com/showtopic-1216.aspx</remarks>
        private void ForceClearCache(String reason, Boolean isUpdateMode)
        {
            var clearEntityCache = false;
            if (_cache != null && _cache.Using)
            {
                clearEntityCache = true;
                _cache.Clear(reason);
            }

            // 因为单对象缓存在实际应用场景中可能会非常庞大
            // 添加、删除不再维护单对象缓存，新添加的会自动获取，删除的一直保留在内存中等待SingleCache的定时清理或RemoveFirst方法慢慢清除
            // 如果清理了实体缓存证明当前实体数据量不大，也清除单对象缓存，尽量使单对象缓存的实体对象与实体缓存中的实体对象一致
            if (isUpdateMode || clearEntityCache)
            {
                if (_singleCache != null && _singleCache.Using)
                {
                    ThreadPoolX.QueueUserWorkItem(() =>
                    {
                        _singleCache.Clear(reason);
                        _singleCache.Initialize();
                    });
                }
            }

            if (!isUpdateMode)
            {
                Int64 n = _Count;
                if (n < 0L) { return; }

                // 只有小于等于1000时才清空_Count，因为大于1000时它要作为HttpCache的见证
                if (n > 1000L)
                {
                    HttpRuntime.Cache.Remove(CacheKey);
                }
                else
                {
                    _Count = -1L;
                }
            }
        }

        /// <summary>清除缓存</summary>
        /// <param name="reason">原因</param>
        public void ClearCache(String reason)
        {
            if (_cache != null && _cache.Using) { _cache.Clear(reason); }

            if (_singleCache != null && _singleCache.Using) { _singleCache.Clear(reason); }

            Int64 n = _Count;
            if (n < 0L) { return; }

            // 只有小于等于1000时才清空_Count，因为大于1000时它要作为HttpCache的见证
            if (n < 1000L)
            {
                _Count = -1L;
            }
            else
            {
                HttpRuntime.Cache.Remove(CacheKey);
            }
        }

        String CacheKey { get { return String.Format("{0}_{1}_{2}_Count", ConnName, TableName, ThisType.Name); } }

        private Boolean _HoldCache = CacheSetting.Alone;
        /// <summary>在数据修改时保持缓存，直到数据过期，独占数据库时默认打开，否则默认关闭</summary>
        /// <remarks>实体缓存和单对象缓存能够自动维护更新数据，保持缓存数据最新，在普通CURD中足够使用</remarks>
        public Boolean HoldCache
        {
            get { return _HoldCache; }
            set
            {
                _HoldCache = value;
                Cache.HoldCache = value;
                SingleCache.HoldCache = value;
            }
        }
        #endregion

        #region 数据库操作
        void InitData() { WaitForInitData(); }

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="builder">SQL语句</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns></returns>
        public virtual DataSet Query(SelectBuilder builder, Int32 startRowIndex, Int32 maximumRows)
        {
            InitData();

            //builder.Table = FormatedTableName;
            return Dal.Select(builder, startRowIndex, maximumRows, TableName);
        }

        /// <summary>查询</summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>结果记录集</returns>
        //[Obsolete("请优先考虑使用SelectBuilder参数做查询！")]
        public virtual DataSet Query(String sql)
        {
            InitData();

            return Dal.Select(sql, TableName);
        }

        /// <summary>查询记录数</summary>
        /// <param name="builder">查询生成器</param>
        /// <returns>记录数</returns>
        public virtual Int32 QueryCount(SelectBuilder builder)
        {
            InitData();

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
        public Int32 Execute(String sql)
        {
            //InitData();

            //Int32 rs = Dal.Execute(sql, TableName);
            //executeCount++;
            //DataChange("Execute");
            //return rs;
            return Execute(true, false, sql);
        }

        /// <summary>执行</summary>
        /// <param name="forceClearCache">是否跨越实体操作，直接执行SQL语句</param>
        /// <param name="isUpdateMode">是否执行更新实体操作</param>
        /// <param name="sql">SQL语句</param>
        /// <returns>影响的结果</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Int32 Execute(Boolean forceClearCache, Boolean isUpdateMode, String sql)
        {
            InitData();

            Int32 rs = Dal.Execute(sql, TableName);
            _ExecuteCount++;
            if (forceClearCache) { _DirectExecuteSQLCount++; }
            if (isUpdateMode) { _UpdateCount++; }
            DataChange("Execute", forceClearCache, isUpdateMode);
            return rs;
        }

        /// <summary>执行Truncate语句</summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>影响的结果</returns>
        public virtual Int32 Truncate(String sql)
        {
            InitData();

            Int32 rs;
            if (Dal.DbType == DatabaseType.SQLite)
            {
                rs = Dal.Execute(String.Format("Delete From {0}", FormatedTableName), TableName);
                Dal.Execute("VACUUM", TableName);
                Dal.Execute(String.Format("update sqlite_sequence set seq=0 where name={0}", FormatedTableName), TableName);
                return rs;
            }
            else
            {
                rs = Dal.Execute(sql, TableName);
            }

            ClearCache("TRUNCATE TABLE");
            if (_OnDataChange != null) { _OnDataChange(ThisType); }

            return rs;
        }

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>新增行的自动编号</returns>
        public Int64 InsertAndGetIdentity(String sql)
        {
            //InitData();

            //Int64 rs = Dal.InsertAndGetIdentity(sql, TableName);
            //executeCount++;
            //DataChange("InsertAndGetIdentity");
            //return rs;
            return InsertAndGetIdentity(true, sql);
        }

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="forceClearCache">是否跨越实体操作，直接执行SQL语句</param>
        /// <param name="sql">SQL语句</param>
        /// <returns>新增行的自动编号</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Int64 InsertAndGetIdentity(Boolean forceClearCache, String sql)
        {
            InitData();

            Int64 rs = Dal.InsertAndGetIdentity(sql, TableName);
            _ExecuteCount++;
            if (forceClearCache) { _DirectExecuteSQLCount++; }
            DataChange("InsertAndGetIdentity", forceClearCache, false);
            return rs;
        }

        /// <summary>执行</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>影响的结果</returns>
        public Int32 Execute(String sql, CommandType type = CommandType.Text, params DbParameter[] ps)
        {
            //InitData();

            //Int32 rs = Dal.Execute(sql, type, ps, TableName);
            //executeCount++;
            //DataChange("Execute " + type);
            //return rs;
            return Execute(true, false, sql, type, ps);
        }

        /// <summary>执行</summary>
        /// <param name="forceClearCache">是否跨越实体操作，直接执行SQL语句</param>
        /// <param name="isUpdateMode">是否执行更新实体操作</param>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>影响的结果</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Int32 Execute(Boolean forceClearCache, Boolean isUpdateMode, String sql, CommandType type = CommandType.Text, params DbParameter[] ps)
        {
            InitData();

            Int32 rs = Dal.Execute(sql, type, ps, TableName);
            _ExecuteCount++;
            if (forceClearCache) { _DirectExecuteSQLCount++; }
            if (isUpdateMode) { _UpdateCount++; }
            DataChange("Execute " + type, forceClearCache, isUpdateMode);
            return rs;
        }

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>新增行的自动编号</returns>
        public Int64 InsertAndGetIdentity(String sql, CommandType type = CommandType.Text, params DbParameter[] ps)
        {
            //InitData();

            //Int64 rs = Dal.InsertAndGetIdentity(sql, type, ps, TableName);
            //executeCount++;
            //DataChange("InsertAndGetIdentity " + type);
            //return rs;
            return InsertAndGetIdentity(true, sql, type, ps);
        }

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="forceClearCache">是否跨越实体操作，直接执行SQL语句</param>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>新增行的自动编号</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Int64 InsertAndGetIdentity(Boolean forceClearCache, String sql, CommandType type = CommandType.Text, params DbParameter[] ps)
        {
            InitData();

            Int64 rs = Dal.InsertAndGetIdentity(sql, type, ps, TableName);
            _ExecuteCount++;
            if (forceClearCache) { _DirectExecuteSQLCount++; }
            DataChange("InsertAndGetIdentity " + type, forceClearCache, false);
            return rs;
        }

        private void DataChange(String reason, Boolean forceClearCache, Boolean isUpdateMode)
        {
            // 还在事务保护里面，不更新缓存，最后提交或者回滚的时候再更新
            // 一般事务保护用于批量更新数据，频繁删除缓存将会打来巨大的性能损耗
            // 2012-07-17 当前实体类开启的事务保护，必须由当前类结束，否则可能导致缓存数据的错乱
            if (_TransCount > 0) { return; }

            DataChange(reason, forceClearCache, isUpdateMode, false);
        }

        private void DataChange(String reason, Boolean forceClearCache, Boolean isUpdateMode, Boolean isTransRoolback)
        {
            if (!forceClearCache)
            {
                ClearCache(String.Format("{0}（F{1}-U{2}）", reason, forceClearCache ? 1 : 0, isUpdateMode ? 1 : 0), isUpdateMode, isTransRoolback);
            }
            else
            {
                ForceClearCache(String.Format("{0}（F{1}-U{2}）", reason, forceClearCache ? 1 : 0, isUpdateMode ? 1 : 0), isUpdateMode);
            }

            if (_OnDataChange != null) { _OnDataChange(ThisType); }
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

                    // 弱引用事件，只会执行一次，一次以后自动取消注册
                    _OnDataChange += new WeakAction<Type>(value, handler => { _OnDataChange -= handler; }, true);
                }
            }
            remove { }
        }
        #endregion

        #region 事务保护
        /// <summary>事务计数</summary>
        [ThreadStatic]
        private static Int32 _TransCount;
        /// <summary>实体操作次数</summary>
        [ThreadStatic]
        private static Int32 _ExecuteCount = 0;
        /// <summary>实体更新操作次数</summary>
        [ThreadStatic]
        private static Int32 _UpdateCount = 0;
        /// <summary>直接执行SQL语句次数</summary>
        [ThreadStatic]
        private static Int32 _DirectExecuteSQLCount = 0;

        /// <summary>开始事务</summary>
        /// <returns>剩下的事务计数</returns>
        public virtual Int32 BeginTrans()
        {
            /* 这里也需要执行初始化检查架构，因为无法确定在调用此方法前是否已使用实体类进行架构检查，如下调用会造成事务不平衡而上抛异常：
             * Exception：执行SqlServer的Dispose时出错：System.InvalidOperationException: 此 SqlTransaction 已完成；它再也无法使用。
             * 
             * 如果实体的模型检查模式为CheckTableWhenFirstUse，直接执行静态操作
             * using (var trans = new EntityTransaction<TEntity>())
             * {
             *   TEntity.Delete(whereExp);
             *   TEntity.Update(new String[] { 字段1,字段2 }, new Object[] { 值1,值1 }, whereExp);
             *   trans.Commit();
             * }
             */
            InitData();

            // 可能存在多层事务，这里不能把这个清零
            //executeCount = 0;

            _ExecuteCount = 0;
            _UpdateCount = 0;
            _DirectExecuteSQLCount = 0;

            return _TransCount = Dal.BeginTransaction();
        }

        /// <summary>提交事务</summary>
        /// <returns>剩下的事务计数</returns>
        public virtual Int32 Commit()
        {
            //TransCount = Dal.Commit();
            //// 提交事务时更新数据，虽然不是绝对准确，但没有更好的办法
            //// 即使提交了事务，但只要事务内没有执行更新数据的操作，也不更新
            //// 2012-06-13 测试证明，修改数据后，提交事务后会更新缓存等数据
            //if (TransCount <= 0 && executeCount > 0)
            //{
            //    DataChange("修改数据后提交事务");
            //    // 回滚到顶层才更新数据
            //    executeCount = 0;
            //}
            //return TransCount;
            if (_ExecuteCount > 0)
            {
                Dal.AddDirtiedEntitySession(Key, this, _ExecuteCount, _UpdateCount, _DirectExecuteSQLCount);

                _ExecuteCount = 0;
                _UpdateCount = 0;
                _DirectExecuteSQLCount = 0;
            }
            _TransCount = Dal.Commit();
            return _TransCount;
        }

        /// <summary>回滚事务，忽略异常</summary>
        /// <returns>剩下的事务计数</returns>
        public virtual Int32 Rollback()
        {
            //TransCount = Dal.Rollback();
            //// 回滚的时候貌似不需要更新缓存
            ////if (TransCount <= 0 && executeCount > 0) DataChange();
            //if (TransCount <= 0 && executeCount > 0)
            //{
            //    // 因为在事务保护中添加或删除实体时直接操作了实体缓存，所以需要更新
            //    DataChange("修改数据后回滚事务");
            //    executeCount = 0;
            //}
            //return TransCount;
            if (_ExecuteCount > 0)
            {
                Dal.AddDirtiedEntitySession(Key, this, _ExecuteCount, _UpdateCount, _DirectExecuteSQLCount);

                //// 因为在事务保护中添加或删除实体时直接操作了实体缓存，所以需要更新
                //DataChange("修改数据后回滚事务", _DirectExecuteSQLCount > 0, _UpdateCount > 0, true);
                _ExecuteCount = 0;
                _UpdateCount = 0;
                _DirectExecuteSQLCount = 0;
            }
            _TransCount = Dal.Rollback();
            return _TransCount;
        }

        /// <summary>触发脏实体会话提交事务后的缓存更新操作</summary>
        /// <param name="updateCount">实体更新操作次数</param>
        /// <param name="directExecuteSQLCount">直接执行SQL语句次数</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void RaiseCommitDataChange(Int32 updateCount, Int32 directExecuteSQLCount)
        {
            DataChange("修改数据后提交事务", directExecuteSQLCount > 0, updateCount > 0, false);
        }

        /// <summary>触发脏实体会话回滚事务后的缓存更新操作</summary>
        /// <param name="updateCount">实体更新操作次数</param>
        /// <param name="directExecuteSQLCount">直接执行SQL语句次数</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void RaiseRoolbackDataChange(Int32 updateCount, Int32 directExecuteSQLCount)
        {
            DataChange("修改数据后回滚事务", directExecuteSQLCount > 0, updateCount > 0, true);
        }

        /// <summary>是否在事务保护中</summary>
        internal Boolean UsingTrans { get { return _TransCount > 1;/*因为Insert上面一定有一层缓存，这里减去1*/ } }
        //internal Boolean UsingTrans { get { return TransCount > 0; } }
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

            TEntity entityobj = null;
            // 如果当前在事务中，并使用了缓存，则尝试更新缓存
            if (HoldCache || UsingTrans)
            {
                if (Cache.Using)
                {
                    // 尽管用了事务保护，但是仍然可能有别的地方导致实体缓存更新，这点务必要注意
                    var fi = Operate.Unique;
                    var e = Cache.Entities.Find(fi.Name, entity[fi.Name]);
                    if (e != null)
                    {
                        if (e != entity) e.CopyFrom(entity);
                    }
                    else
                    {
                        // 加入超级缓存的实体对象，需要标记来自数据库
                        entityobj = entity as TEntity;
                        if (entityobj != null)
                        {
                            entityobj.OnLoad();
                            Cache.Entities.Add(entityobj);
                        }
                    }
                }
                // 自动加入单对象缓存
                if (SingleCache.Using)
                {
                    if (entityobj == null) entityobj = entity as TEntity;
                    if (entityobj != null)
                    {
                        var getkeymethod = SingleCache.GetKeyMethod;
                        if (getkeymethod != null)
                        {
                            // 这里也需要标记来自数据库，防止实体缓存没有使用的情况下不对新增的实体对象做标记，不再正确验证脏数据
                            var key = getkeymethod(entityobj);
                            entityobj.OnLoad();
                            SingleCache.Add(key, entityobj);
                        }
                    }
                }
            }

            if (_Count > -1L) Interlocked.Increment(ref _Count);

            return rs;
        }

        /// <summary>更新数据库，同时更新实体缓存</summary>
        /// <param name="entity">实体对象</param>
        /// <returns></returns>
        public virtual Int32 Update(IEntity entity)
        {
            var rs = persistence.Update(entity);

            TEntity entityobj = null;
            // 如果当前在事务中，并使用了缓存，则尝试更新缓存
            if (HoldCache || UsingTrans)
            {
                if (Cache.Using)
                {
                    // 尽管用了事务保护，但是仍然可能有别的地方导致实体缓存更新，这点务必要注意
                    var fi = Operate.Unique;
                    var e = Cache.Entities.Find(fi.Name, entity[fi.Name]);
                    if (e != null)
                    {
                        if (e != entity) e.CopyFrom(entity);
                    }
                    else
                    {
                        // 加入超级缓存的实体对象，需要标记来自数据库
                        entityobj = entity as TEntity;
                        if (entityobj != null)
                        {
                            entityobj.OnLoad();
                            Cache.Entities.Add(entityobj);
                        }
                    }
                }

                // 自动加入单对象缓存
                if (SingleCache.Using)
                {
                    if (entityobj == null) entityobj = entity as TEntity;
                    if (entityobj != null)
                    {
                        var getkeymethod = SingleCache.GetKeyMethod;
                        if (getkeymethod != null)
                        {
                            var key = getkeymethod(entityobj);
                            // 复制到单对象缓存
                            TEntity cacheitem;
                            if (SingleCache.TryGetItem(key, out cacheitem))
                            {
                                if (cacheitem != null)
                                {
                                    if (cacheitem != entityobj) { cacheitem.CopyFrom(entityobj); }
                                }
                                else // 存在允许存储空对象的情况
                                {
                                    SingleCache.RemoveKey(key);
                                    entityobj.OnLoad();
                                    SingleCache.Add(key, entityobj);
                                }
                            }
                            else
                            {
                                entityobj.OnLoad();
                                SingleCache.Add(key, entityobj);
                            }
                        }
                    }
                }
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
            if (HoldCache || UsingTrans  )
            {
                if (Cache.Using)
                {
                    var fi = Operate.Unique;
                    if (fi != null)
                    {
                        var v = entity[fi.Name];
                        Cache.Entities.RemoveAll(e => Object.Equals(e[fi.Name], v));
                    }
                }
                // 自动加入单对象缓存
                if (SingleCache.Using)
                {
                    var entityobj = entity as TEntity;
                    if (entityobj != null)
                    {
                        var getkeymethod = SingleCache.GetKeyMethod;
                        if (getkeymethod != null)
                        {
                            var key = getkeymethod(entityobj);
                            SingleCache.RemoveKey(key);
                        }
                    }
                }
            }

            if (_Count > -1L) { Interlocked.Decrement(ref _Count); }

            return rs;
        }
        #endregion
    }

    #region 脏实体会话

    /// <summary>脏实体会话，嵌套事务回滚时使用</summary>
    /// <remarks>http://www.newlifex.com/showtopic-1216.aspx</remarks>
    public class DirtiedEntitySession
    {
        internal Int32 ExecuteCount { get; set; }

        internal Int32 UpdateCount { get; set; }

        internal Int32 DirectExecuteSQLCount { get; set; }

        internal IEntitySession Session { get; set; }

        internal DirtiedEntitySession(IEntitySession session, Int32 executeCount, Int32 updateCount, Int32 directExecuteSQLCount)
        {
            Session = session;
            ExecuteCount = executeCount;
            UpdateCount = updateCount;
            DirectExecuteSQLCount = directExecuteSQLCount;
        }
    }

    #endregion
}