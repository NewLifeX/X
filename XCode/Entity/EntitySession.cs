using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using NewLife;
using NewLife.Collections;
using NewLife.Log;
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
 * 缓存流程：
 * Insert/Update/Delete
 *      IEntityPersistence.Insert/Update/Delete
 *          Execute
 *              DataChange <= Tran!=null
 *                  ClearCache <= !HoldCache
 *                  OnDataChange
 *      更新缓存
 *      Tran.Completed
 *          Tran = null
 *          Success
 *              DataChange
 *          else
 *              回滚缓存
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
        /// <summary>连接名</summary>
        public String ConnName { get; }

        /// <summary>表名</summary>
        public String TableName { get; }

        /// <summary>用于标识会话的键值</summary>
        public String Key { get; }
        #endregion

        #region 构造
        private EntitySession(String connName, String tableName)
        {
            ConnName = connName;
            TableName = tableName;
            Key = connName + "###" + tableName;
        }

        private static DictionaryCache<String, EntitySession<TEntity>> _es = new DictionaryCache<string, EntitySession<TEntity>>(StringComparer.OrdinalIgnoreCase);
        /// <summary>创建指定表名连接名的会话</summary>
        /// <param name="connName"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static EntitySession<TEntity> Create(String connName, String tableName)
        {
            if (String.IsNullOrEmpty(connName)) throw new ArgumentNullException("connName");
            if (String.IsNullOrEmpty(tableName)) throw new ArgumentNullException("tableName");

            // 字符串连接有较大性能损耗
            var key = connName + "###" + tableName;
            return _es.GetItem(key, k => new EntitySession<TEntity>(connName, tableName));
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

                var init = Setting.Current.InitData;
                if (init)
                {
                    BeginTrans();
                    try
                    {
                        var entity = Operate.Default as EntityBase;
                        if (entity != null) entity.InitData();

                        Commit();
                    }
                    catch (Exception ex)
                    {
                        if (XTrace.Debug) XTrace.WriteLine("初始化数据出错！" + ex.ToString());

                        Rollback();
                    }
                }

                return true;
            }
            finally
            {
                initThread = 0;
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
            DAL.WriteLog("开始{2}检查表[{0}/{1}]的数据表架构……", Table.DataTable.Name, Dal.Db.DbType, Setting.Current.Negative.CheckOnly ? "异步" : "同步");
#endif

            var sw = new Stopwatch();
            sw.Start();

            try
            {
                // 检查新表名对应的数据表
                var table = Table.DataTable;
                // 克隆一份，防止修改
                table = table.Clone() as IDataTable;

                if (table != null && table.TableName != TableName)
                {
                    FixIndexName(table);
                    table.TableName = TableName;
                }

                var set = new NegativeSetting
                {
                    CheckOnly = Setting.Current.Negative.CheckOnly,
                    NoDelete = Setting.Current.Negative.NoDelete
                };

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
        Boolean _hasCheckModel = false;
        readonly Object _checkLock = new Object();
        /// <summary>检查模型。依据反向工程设置、是否首次使用检查、是否已常规检查等</summary>
        private void CheckModel()
        {
            if (_hasCheckModel) return;
            lock (_checkLock)
            {
                if (_hasCheckModel) return;

                // 是否默认连接和默认表名，非默认则强制检查，并且不允许异步检查（异步检查会导致ConnName和TableName不对）
                var def = Default;

                if (def == this)
                {
                    if (!Setting.Current.Negative.Enable ||
                        DAL.NegativeExclude.Contains(ConnName) ||
                        DAL.NegativeExclude.Contains(TableName) ||
                        IsGenerated)
                    {
                        _hasCheckModel = true;
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
                var mode = Table.ModelCheckMode;
                if (DAL.Debug && mode == ModelCheckModes.CheckTableWhenFirstUse)
                    DAL.WriteLog("检查实体{0}的数据表架构，模式：{1}", ThisType.FullName, mode);

                // 第一次使用才检查的，此时检查
                var ck = mode == ModelCheckModes.CheckTableWhenFirstUse;
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
                    if (!Setting.Current.Negative.CheckOnly || def != this)
                        CheckTable();
                    else
                        Task.Factory.StartNew(CheckTable).LogException();
                }

                _hasCheckModel = true;
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
                    Interlocked.CompareExchange(ref _cache, ec, null);
                }
                return _cache;
            }
        }

        private ISingleEntityCache<Object, TEntity> _singleCache;
        /// <summary>单对象实体缓存。</summary>
        public ISingleEntityCache<Object, TEntity> SingleCache
        {
            get
            {
                // 以连接名和表名为key，因为不同的库不同的表，缓存也不一样
                if (_singleCache == null)
                {
                    var sc = new SingleEntityCache<Object, TEntity>();
                    sc.ConnName = ConnName;
                    sc.TableName = TableName;

                    // 从默认会话复制参数
                    if (Default != this) sc.CopySettingFrom(Default.SingleCache);

                    Interlocked.CompareExchange(ref _singleCache, sc, null);
                }
                return _singleCache;
            }
        }

        IEntityCache IEntitySession.Cache { get { return Cache; } }
        ISingleEntityCache IEntitySession.SingleCache { get { return SingleCache; } }

        /// <summary>总记录数，小于1000时是精确的，大于1000时缓存10秒</summary>
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
                if (n >= 0)
                {
                    // 等于0的时候也应该缓存，否则会一直查询这个表
                    if (n < 1000L) return n;

                    // 大于1000，使用HttpCache
                    Int64? k = (Int64?)HttpRuntime.Cache[key];
                    if (k != null && k.HasValue) return k.Value;
                }
                // 来到这里，有可能是第一次访问，静态字段没有缓存，也有可能是大于1000的缓存过期

                CheckModel();

                Int64 m = 0L;
                // 小于1000的精确查询，大于1000的快速查询
                if (n >= 0 && n <= 1000L)
                {
                    var sb = new SelectBuilder();
                    sb.Table = FormatedTableName;

                    WaitForInitData();
                    m = Dal.SelectCount(sb);
                }
                else
                {
                    // 第一次访问，SQLite的Select Count非常慢，数据大于阀值时，使用最大ID作为表记录数
                    var max = 0L;
                    if (Dal.DbType == DatabaseType.SQLite && Table.Identity != null)
                    {
                        // 除第一次外，将依据上一次记录数决定是否使用最大ID
                        if (_LastCount == null)
                        {
                            // 先查一下最大值
                            //max = Entity<TEntity>.FindMax(Table.Identity.ColumnName);
                            // 依赖关系FindMax=>FindAll=>Query=>InitData=>Meta.Count，所以不能使用

                            //if (DAL.Debug) DAL.WriteLog("第一次访问{0}，SQLite的Select Count非常慢，数据大于阀值时，使用最大ID作为表记录数", ThisType.Name);

                            var builder = new SelectBuilder();
                            builder.Table = FormatedTableName;
                            builder.OrderBy = Table.Identity.Desc();
                            var ds = Dal.Select(builder, 0, 1);
                            if (ds.Tables[0].Rows.Count > 0)
                                max = Convert.ToInt64(ds.Tables[0].Rows[0][Table.Identity.ColumnName]);
                        }
                        else
                            max = _LastCount.Value;
                    }

                    // 100w数据时，没有预热Select Count需要3000ms，预热后需要500ms
                    if (max < 500000)
                        m = Dal.Session.QueryCountFast(TableName);
                    else
                    {
                        m = max;

                        // 异步查询弥补不足
                        Task.Factory.StartNew(() =>
                        {
                            _LastCount = _Count = Dal.Session.QueryCountFast(TableName);

                            if (_Count >= 1000) HttpRuntime.Cache.Insert(key, _Count, null, DateTime.Now.AddSeconds(10), System.Web.Caching.Cache.NoSlidingExpiration);
                        }).LogException();
                    }
                }

                _Count = m;
                _LastCount = m;

                if (m >= 1000) HttpRuntime.Cache.Insert(key, m, null, DateTime.Now.AddSeconds(10), System.Web.Caching.Cache.NoSlidingExpiration);

                // 先拿到记录数再初始化，因为初始化时会用到记录数，同时也避免了死循环
                WaitForInitData();

                return m;
            }
            private set
            {
                if (value == 0)
                {
                    _LastCount = null;
                    _Count = 0;
                    HttpRuntime.Cache.Remove(CacheKey);
                }
            }
        }

        /// <summary>清除缓存</summary>
        /// <param name="reason">原因</param>
        public void ClearCache(String reason)
        {
            var ec = _cache;
            if (ec != null && ec.Using) ec.Clear(reason);

            var sc = _singleCache;
            if (sc != null && sc.Using) sc.Clear(reason);

            Int64 n = _Count;
            if (n < 0L) return;

            // 只有小于等于1000时才清空_Count，因为大于1000时它要作为HttpCache的见证
            if (n < 1000L)
                _Count = -1L;
            else
                HttpRuntime.Cache.Remove(CacheKey);
        }

        String CacheKey { get { return String.Format("{0}_{1}_{2}_Count", ConnName, TableName, ThisType.Name); } }
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

            return Dal.Select(builder, startRowIndex, maximumRows);
        }

        /// <summary>查询</summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>结果记录集</returns>
        //[Obsolete("请优先考虑使用SelectBuilder参数做查询！")]
        public virtual DataSet Query(String sql)
        {
            InitData();

            return Dal.Select(sql);
        }

        /// <summary>查询记录数</summary>
        /// <param name="builder">查询生成器</param>
        /// <returns>记录数</returns>
        public virtual Int32 QueryCount(SelectBuilder builder)
        {
            InitData();

            return Dal.SelectCount(builder);
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
            return Dal.PageSplit(builder, startRowIndex, maximumRows);
        }

        /// <summary>执行</summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>影响的结果</returns>
        public Int32 Execute(String sql)
        {
            InitData();

            var rs = Dal.Execute(sql);
            DataChange("Execute");

            return rs;
        }

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>新增行的自动编号</returns>
        public Int64 InsertAndGetIdentity(String sql)
        {
            InitData();

            Int64 rs = Dal.InsertAndGetIdentity(sql);
            DataChange("InsertAndGetIdentity");
            return rs;
        }

        /// <summary>执行</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>影响的结果</returns>
        public Int32 Execute(String sql, CommandType type = CommandType.Text, params DbParameter[] ps)
        {
            InitData();

            Int32 rs = Dal.Execute(sql, type, ps);
            DataChange("Execute " + type);
            return rs;
        }

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>新增行的自动编号</returns>
        public Int64 InsertAndGetIdentity(String sql, CommandType type = CommandType.Text, params DbParameter[] ps)
        {
            InitData();

            Int64 rs = Dal.InsertAndGetIdentity(sql, type, ps);
            DataChange("InsertAndGetIdentity " + type);
            return rs;
        }

        private void DataChange(String reason)
        {
            if (_Tran != null) return;

            ClearCache(reason);

            _OnDataChange?.Invoke(ThisType);
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

        #region 数据库高级操作
        /// <summary>清空数据表，标识归零</summary>
        /// <returns></returns>
        public Int32 Truncate()
        {
            var rs = Dal.Session.Truncate(TableName);

            // 干掉所有缓存
            _cache?.Clear("Truncate");
            _singleCache?.Clear("Truncate");
            LongCount = 0;

            // 重新初始化
            hasCheckInitData = false;

            return rs;
        }
        #endregion

        #region 事务保护
        private ITransaction _Tran;

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

            var count = Dal.BeginTransaction();

            var tr = _Tran = (Dal.Session as DbSession).Transaction;
            tr.Completed += (s, e) =>
            {
                _Tran = null;
                if (e.Executes > 0)
                {
                    if (e.Success)
                        DataChange($"修改数据{e.Executes}次后提交事务");
                    else
                        DataChange($"修改数据{e.Executes}次后回滚事务");
                }
            };

            return count;
        }

        /// <summary>提交事务</summary>
        /// <returns>剩下的事务计数</returns>
        public virtual Int32 Commit()
        {
            return Dal.Commit();
        }

        /// <summary>回滚事务，忽略异常</summary>
        /// <returns>剩下的事务计数</returns>
        public virtual Int32 Rollback()
        {
            return Dal.Rollback();
        }
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

        /// <summary>把该对象持久化到数据库，添加/更新实体缓存和单对象缓存，增加总计数</summary>
        /// <param name="entity">实体对象</param>
        /// <returns></returns>
        public virtual Int32 Insert(IEntity entity)
        {
            var rs = persistence.Insert(entity);

            // 标记来自数据库
            var e = entity as TEntity;
            e.MarkDb(true);

            // 加入实体缓存
            var ec = _cache;
            if (ec != null) ec.Add(e);

            // 加入单对象缓存
            var sc = _singleCache;
            if (sc != null && sc.Using) sc.Add(e);

            // 增加计数
            if (_Count >= 0) Interlocked.Increment(ref _Count);

            // 事务回滚时执行逆向操作
            var tr = _Tran;
            if (tr != null) tr.Completed += (s, se) =>
            {
                if (!se.Success && se.Executes > 0)
                {
                    if (ec != null) ec.Remove(e);
                    if (sc != null && sc.Using) sc.Remove(e, false);
                    if (_Count >= 0) Interlocked.Decrement(ref _Count);
                }
            };

            return rs;
        }

        /// <summary>更新数据库，同时更新实体缓存</summary>
        /// <param name="entity">实体对象</param>
        /// <returns></returns>
        public virtual Int32 Update(IEntity entity)
        {
            var rs = persistence.Update(entity);

            // 标记来自数据库
            var e = entity as TEntity;
            e.MarkDb(true);

            // 更新缓存
            TEntity old = null;
            var ec = _cache;
            if (ec != null) old = ec.Update(e);

            // 自动加入单对象缓存
            var sc = _singleCache;
            if (sc != null && sc.Using) sc.Add(e);

            // 事务回滚时执行逆向操作
            var tr = _Tran;
            if (tr != null) tr.Completed += (s, se) =>
            {
                if (!se.Success && se.Executes > 0)
                {
                    // 如果存在替换，则换回来；
                    //!!! 如果先后是同一个对象，那就没有办法回滚回去了
                    if (ec != null && old != e)
                    {
                        ec.Remove(e);
                        if (old != null) ec.Add(old);
                    }
                    // 干掉缓存项，让它重新获取
                    if (sc != null && sc.Using) sc.Remove(e, false);
                }
            };

            return rs;
        }

        /// <summary>从数据库中删除该对象，同时从实体缓存和单对象缓存中删除，扣减总数量</summary>
        /// <param name="entity">实体对象</param>
        /// <returns></returns>
        public virtual Int32 Delete(IEntity entity)
        {
            var rs = persistence.Delete(entity);

            var e = entity as TEntity;

            // 从实体缓存删除
            TEntity old = null;
            var ec = _cache;
            if (ec != null) old = ec.Remove(e);

            // 从单对象缓存删除
            var sc = _singleCache;
            if (sc != null) sc.Remove(e, false);

            // 减少计数
            if (_Count > 0) Interlocked.Decrement(ref _Count);

            // 事务回滚时执行逆向操作
            var tr = _Tran;
            if (tr != null) tr.Completed += (s, se) =>
            {
                if (!se.Success && se.Executes > 0)
                {
                    if (ec != null && old != null) ec.Add(old);
                    if (sc != null && sc.Using) sc.Add(entity);
                    Interlocked.Increment(ref _Count);
                }
            };

            return rs;
        }
        #endregion
    }
}