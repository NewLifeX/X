using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NewLife;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Threading;
using XCode.Cache;
using XCode.Configuration;
using XCode.DataAccessLayer;
using XCode.Model;
#if !NET4
#endif

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
 *                  ClearCache
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

            Queue = new EntityQueue(this);
        }

        private static ConcurrentDictionary<String, EntitySession<TEntity>> _es = new ConcurrentDictionary<String, EntitySession<TEntity>>(StringComparer.OrdinalIgnoreCase);
        /// <summary>创建指定表名连接名的会话</summary>
        /// <param name="connName"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static EntitySession<TEntity> Create(String connName, String tableName)
        {
            if (connName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(connName));
            if (tableName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(tableName));

            // 字符串连接有较大性能损耗
            var key = connName + "###" + tableName;
            return _es.GetOrAdd(key, k => new EntitySession<TEntity>(connName, tableName));
        }
        #endregion

        #region 主要属性
        private Type ThisType => typeof(TEntity);

        /// <summary>表信息</summary>
        TableItem Table => TableItem.Create(ThisType);

        IEntityOperate Operate => EntityFactory.CreateOperate(ThisType);

        private DAL _Dal;
        /// <summary>数据操作层</summary>
        public DAL Dal => _Dal ?? (_Dal = DAL.Create(ConnName));

        private String _FormatedTableName;
        /// <summary>已格式化的表名，带有中括号等</summary>
        public virtual String FormatedTableName
        {
            get
            {
                if (_FormatedTableName.IsNullOrEmpty()) _FormatedTableName = Dal.Db.FormatTableName(TableName);

                return _FormatedTableName;
            }
        }

        private String _TableNameWithPrefix;
        /// <summary>带前缀的表名</summary>
        public virtual String TableNameWithPrefix
        {
            get
            {
                if (_TableNameWithPrefix.IsNullOrEmpty())
                {
                    var str = TableName;

                    // 检查自动表前缀
                    var db = Dal.Db;
                    var pf = db.TablePrefix;
                    if (!pf.IsNullOrEmpty() && !str.StartsWithIgnoreCase(pf)) str = pf + str;

                    _TableNameWithPrefix = str;
                }
                return _TableNameWithPrefix;
            }
        }

        private EntitySession<TEntity> _Default;
        /// <summary>该实体类的默认会话。</summary>
        private EntitySession<TEntity> Default
        {
            get
            {
                if (_Default != null) return _Default;

                var cname = Table.ConnName;
                var tname = Table.TableName;

                if (ConnName == cname && TableName == tname)
                    _Default = this;
                else
                    _Default = Create(cname, tname);

                return _Default;
            }
        }

        /// <summary>用户数据</summary>
        public IDictionary<String, Object> Items { get; set; } = new Dictionary<String, Object>();
        #endregion

        #region 数据初始化
        /// <summary>记录已进行数据初始化</summary>
        Boolean hasCheckInitData = false;
        Int32 initThread = 0;
        readonly Object _wait_lock = new Object();

        /// <summary>检查并初始化数据。参数等待时间为0表示不等待</summary>
        /// <param name="ms">等待时间，-1表示不限，0表示不等待</param>
        /// <returns>如果等待，返回是否收到信号</returns>
        public Boolean WaitForInitData(Int32 ms = 3000)
        {
            // 已初始化
            if (hasCheckInitData) return true;

            var tid = Thread.CurrentThread.ManagedThreadId;

            //!!! 一定一定小心堵塞的是自己
            if (initThread == tid) return true;

            if (!Monitor.TryEnter(_wait_lock, ms))
            {
                //if (DAL.Debug) DAL.WriteLog("等待初始化{0}数据{1}ms，调用栈：{2}", ThisType.Name, ms, XTrace.GetCaller(1, 8));
                if (DAL.Debug) DAL.WriteLog("等待初始化{0}数据{1:n0}ms失败 initThread={2}", ThisType.Name, ms, initThread);
                return false;
            }
            //initThread = tid;
            try
            {
                // 已初始化
                if (hasCheckInitData) return true;

                var name = ThisType.Name;
                if (name == TableName)
                    name = String.Format("{0}@{1}", ThisType.Name, ConnName);
                else
                    name = String.Format("{0}#{1}@{2}", ThisType.Name, TableName, ConnName);

                var task = Task.Factory.StartNew(() =>
                {
                    initThread = Thread.CurrentThread.ManagedThreadId;

                    // 如果该实体类是首次使用检查模型，则在这个时候检查
                    try
                    {
                        CheckModel();
                    }
                    catch (Exception ex) { XTrace.WriteException(ex); }

                    //var init = Setting.Current.InitData;
                    var init = this == Default;
                    if (init)
                    {
                        //BeginTrans();
                        try
                        {
                            if (Operate.Default is EntityBase entity)
                            {
                                entity.InitData();
                                //// 异步执行初始化，只等一会，避免死锁
                                //var task = TaskEx.Run(() => entity.InitData());
                                //if (!task.Wait(ms) && DAL.Debug) DAL.WriteLog("{0}未能在{1:n0}ms内完成数据初始化 Task={2}", ThisType.Name, ms, task.Id);
                            }

                            //Commit();
                        }
                        catch (Exception ex)
                        {
                            if (XTrace.Debug) XTrace.WriteLine("初始化数据出错！" + ex.ToString());

                            //Rollback();
                        }
                    }
                });
                task.Wait(3_000);

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
            DAL.WriteLog("开始{2}检查表[{0}/{1}]的数据表架构……", Table.DataTable.Name, Dal.Db.Type, Setting.Current.Migration == Migration.ReadOnly ? "异步" : "同步");
#endif

            var sw = Stopwatch.StartNew();

            try
            {
                // 检查新表名对应的数据表
                var table = Table.DataTable;
                // 克隆一份，防止修改
                table = table.Clone() as IDataTable;

                if (table != null && table.TableName != TableName)
                {
                    // 表名去掉前缀
                    var name = TableName;
                    if (name.Contains(".")) name = name.Substring(".");

                    table.TableName = name;
                    FixIndexName(table);
                }

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

        void FixIndexName(IDataTable table)
        {
            // 修改一下索引名，否则，可能因为同一个表里面不同的索引冲突
            if (table.Indexes != null)
            {
                foreach (var di in table.Indexes)
                {
                    var sb = Pool.StringBuilder.Get();
                    sb.AppendFormat("IX_{0}", TableName);
                    foreach (var item in di.Columns)
                    {
                        sb.Append("_");
                        sb.Append(item);
                    }

                    di.Name = sb.Put(true);
                }
            }
        }

        private Boolean IsGenerated => ThisType.GetCustomAttribute<CompilerGeneratedAttribute>(true) != null;
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
                var cname = ConnName;
                var tname = TableName;
                if (def == this)
                {
                    //if (!Setting.Current.Negative.Enable ||
                    //    DAL.NegativeExclude.Contains(cname) ||
                    //    DAL.NegativeExclude.Contains(tname) ||
                    //    IsGenerated)
                    if (Dal.Db.Migration == Migration.Off || IsGenerated)
                    {
                        _hasCheckModel = true;
                        return;
                    }
                }
#if DEBUG
                else
                {
                    DAL.WriteLog("[{0}@{1}]非默认表名连接名，强制要求检查架构！", tname, cname);
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
                var dal = DAL.Create(cname);
                if (!dal.HasCheckTables.Contains(tname))
                {
                    if (!ck)
                    {
                        dal.HasCheckTables.Add(tname);

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
                    if (dal.Db.Migration > Migration.ReadOnly || def != this)
                        CheckTable();
                    else
                        ThreadPoolX.QueueUserWorkItem(CheckTable);
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
                    var sc = new SingleEntityCache<Object, TEntity>
                    {
                        ConnName = ConnName,
                        TableName = TableName
                    };

                    // 从默认会话复制参数
                    if (Default != this) sc.CopySettingFrom(Default.SingleCache);

                    Interlocked.CompareExchange(ref _singleCache, sc, null);
                }
                return _singleCache;
            }
        }

        IEntityCache IEntitySession.Cache => Cache;
        ISingleEntityCache IEntitySession.SingleCache => SingleCache;

        /// <summary>总记录数，小于1000时是精确的，大于1000时缓存10秒</summary>
        public Int32 Count
        {
            get
            {
                var v = LongCount;
                return v > Int32.MaxValue ? Int32.MaxValue : (Int32)v;
            }
        }

        private DateTime _NextCount;
        /// <summary>总记录数较小时，使用静态字段，较大时增加使用Cache</summary>
        private Int64 _Count = -2L;
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
                var n = _Count;
                var now = TimerX.Now;

                // 如果有缓存，则考虑返回吧
                if (n >= 0)
                {
                    if (_NextCount < now)
                    {
                        _NextCount = now.AddSeconds(60);
                        // 异步更新
                        ThreadPoolX.QueueUserWorkItem(() => LongCount = GetCount(_Count));
                    }

                    return n;
                }

                // 来到这里，是第一次访问

                CheckModel();

                // 从配置读取
                var dc = DataCache.Current;
                if (n < 0)
                {
                    if (dc.Counts.TryGetValue(key, out var c)) n = c;
                }

                if (DAL.Debug) DAL.WriteLog("{0}.Count 快速计算表记录数（非精确）[{1}/{2}] 参考值 {3:n0}", ThisType.Name, TableName, ConnName, n);

                var m = GetCount(n);
                _Count = m;

                dc.Counts[key] = m;
                dc.SaveAsync();

                _NextCount = now.AddSeconds(60);

                // 先拿到记录数再初始化，因为初始化时会用到记录数，同时也避免了死循环
                //WaitForInitData();
                InitData();

                return m;
            }
            private set
            {
                _Count = value;
                _NextCount = TimerX.Now.AddSeconds(60);

                var dc = DataCache.Current;
                dc.Counts[CacheKey] = value;
                dc.SaveAsync();
            }
        }

        private Int64 GetCount(Int64 count)
        {
            //if (count >= 0)
            //{
            //    // 小于1000的精确查询，大于1000的快速查询
            //    if (count <= 1000L)
            //    {
            //        var builder = new SelectBuilder
            //        {
            //            Table = FormatedTableName
            //        };

            //        return Dal.SelectCount(builder);
            //    }
            //}

            // 第一次访问，SQLite的Select Count非常慢，数据大于阀值时，使用最大ID作为表记录数
            if (count < 0 && Dal.DbType == DatabaseType.SQLite && Table.Identity != null)
            {
                var builder = new SelectBuilder
                {
                    Table = FormatedTableName,
                    OrderBy = Table.Identity.Desc()
                };
                var ds = Dal.Query(builder, 0, 1);
                if (ds.Columns.Length > 0 && ds.Rows.Count > 0)
                    count = Convert.ToInt64(ds.Rows[0][0]);
                //var rs = Dal.Query(builder, 0, 0, dr => dr.Read() ? dr[0].ToInt() : -1);
                //if (rs > 0) count = rs;
            }

            // 100w数据时，没有预热Select Count需要3000ms，预热后需要500ms
            if (count < 500000)
            {
                if (count <= 0) count = Dal.Session.QueryCountFast(TableNameWithPrefix);

                // 查真实记录数，修正FastCount不够准确的情况
                if (count < 10000000)
                {
                    var builder = new SelectBuilder
                    {
                        Table = FormatedTableName
                    };

                    count = Dal.SelectCount(builder);
                }
            }
            else
            {
                // 异步查询弥补不足，千万数据以内
                if (count < 10000000) count = Dal.Session.QueryCountFast(TableNameWithPrefix);
            }

            return count;
        }

        /// <summary>清除缓存</summary>
        /// <param name="reason">原因</param>
        public void ClearCache(String reason)
        {
            _cache?.Clear(reason);

            _singleCache?.Clear(reason);

            // Count提供的是非精确数据，避免频繁更新
            //_Count = -1L;
            //_NextCount = DateTime.MinValue;
        }

        String CacheKey => $"{ConnName}_{TableName}_{ThisType.Name}";
        #endregion

        #region 数据库操作
        /// <summary>初始化数据</summary>
        public void InitData() => WaitForInitData();

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="builder">SQL语句</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns></returns>
        public virtual DbTable Query(SelectBuilder builder, Int64 startRowIndex, Int64 maximumRows)
        {
            InitData();

            return Dal.Query(builder, startRowIndex, maximumRows);
        }

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="sql">SQL语句</param>
        /// <returns></returns>
        public virtual DbTable Query(String sql)
        {
            InitData();

            return Dal.Query(sql);
        }

        /// <summary>查询记录数</summary>
        /// <param name="builder">查询生成器</param>
        /// <returns>记录数</returns>
        public virtual Int32 QueryCount(SelectBuilder builder)
        {
            InitData();

            return Dal.SelectCount(builder);
        }

        /// <summary>查询记录数</summary>
        /// <param name="sql">SQL语句</param>
        /// <returns></returns>
        public virtual Int32 QueryCount(String sql)
        {
            InitData();

            return Dal.SelectCount(sql, CommandType.Text, null);
        }

        /// <summary>执行</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>影响的结果</returns>
        public Int32 Execute(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            InitData();

            var rs = Dal.Execute(sql, type, ps);
            DataChange("Execute " + type);
            return rs;
        }

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>新增行的自动编号</returns>
        public Int64 InsertAndGetIdentity(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            InitData();

            var rs = Dal.InsertAndGetIdentity(sql, type, ps);
            DataChange("InsertAndGetIdentity " + type);
            return rs;
        }

        private void DataChange(String reason)
        {
            //var tr = GetTran();
            //// 实体添删改时，有修改缓存，数据变更事件里不需要再次清空
            //if (tr == null || tr.Count == 0) ClearCache(reason);

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
            var rs = Dal.Session.Truncate(TableNameWithPrefix);

            // 干掉所有缓存
            _cache?.Clear("Truncate");
            _singleCache?.Clear("Truncate");
            LongCount = 0;

            //// 重新初始化
            //hasCheckInitData = false;

            return rs;
        }
        #endregion

        #region 事务保护
        private ITransaction GetTran() => (Dal.Session as DbSession).Transaction;

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

            return count;
        }

        /// <summary>提交事务</summary>
        /// <returns>剩下的事务计数</returns>
        public virtual Int32 Commit()
        {
            var rs = Dal.Commit();

            if (rs == 0) DataChange("Commit");

            return rs;
        }

        /// <summary>回滚事务，忽略异常</summary>
        /// <returns>剩下的事务计数</returns>
        public virtual Int32 Rollback()
        {
            var rs = Dal.Rollback();

            if (rs == 0) DataChange($"Rollback");

            return rs;
        }
        #endregion

        #region 参数化
        /// <summary>格式化参数名</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual String FormatParameterName(String name) => Dal.Db.FormatParameterName(name);
        #endregion

        #region 实体操作
        private IEntityPersistence Persistence => XCodeService.Container.ResolveInstance<IEntityPersistence>();

        /// <summary>把该对象持久化到数据库，添加/更新实体缓存和单对象缓存，增加总计数</summary>
        /// <param name="entity">实体对象</param>
        /// <returns></returns>
        public virtual Int32 Insert(IEntity entity)
        {
            var rs = Persistence.Insert(entity);

            var e = entity as TEntity;

            // 加入实体缓存
            var ec = _cache;
            if (ec != null) ec.Add(e);

            // 增加计数
            if (_Count >= 0) Interlocked.Increment(ref _Count);

            return rs;
        }

        /// <summary>更新数据库，同时更新实体缓存</summary>
        /// <param name="entity">实体对象</param>
        /// <returns></returns>
        public virtual Int32 Update(IEntity entity)
        {
            var rs = Persistence.Update(entity);

            var e = entity as TEntity;

            // 更新缓存
            TEntity old = null;
            var ec = _cache;
            if (ec != null) old = ec.Update(e);

            // 干掉缓存项，让它重新获取
            _singleCache?.Remove(e);

            return rs;
        }

        /// <summary>从数据库中删除该对象，同时从实体缓存和单对象缓存中删除，扣减总数量</summary>
        /// <param name="entity">实体对象</param>
        /// <returns></returns>
        public virtual Int32 Delete(IEntity entity)
        {
            var rs = Persistence.Delete(entity);

            var e = entity as TEntity;

            // 从实体缓存删除
            TEntity old = null;
            var ec = _cache;
            if (ec != null) old = ec.Remove(e);

            // 从单对象缓存删除
            _singleCache?.Remove(e);

            // 减少计数
            if (_Count > 0) Interlocked.Decrement(ref _Count);

            return rs;
        }
        #endregion

        #region 队列
        /// <summary>实体队列</summary>
        public EntityQueue Queue { get; private set; }
        #endregion
    }
}