using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using NewLife;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Reflection;
using XCode.Exceptions;

namespace XCode.DataAccessLayer
{
    /// <summary>数据库会话基类</summary>
    abstract partial class DbSession : DisposeBase, IDbSession
    {
        #region 构造函数
        protected DbSession(IDatabase db)
        {
            Database = db;
            ShowSQL = db.ShowSQL;
        }

        /// <summary>销毁资源时，回滚未提交事务，并关闭数据库连接</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            try
            {
                // 注意，没有Commit的数据，在这里将会被回滚
                //if (Trans != null) Rollback();
                // 在嵌套事务中，Rollback只能减少嵌套层数，而_Trans.Rollback能让事务马上回滚
                if (Opened) Transaction?.Rollback();

                Close();
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                WriteLog("执行" + DbType.ToString() + "的Dispose时出错：" + ex.ToString());
            }
        }
        #endregion

        #region 属性
        /// <summary>数据库</summary>
        public IDatabase Database { get; }

        /// <summary>返回数据库类型。外部DAL数据库类请使用Other</summary>
        private DatabaseType DbType { get { return Database.Type; } }

        /// <summary>工厂</summary>
        private DbProviderFactory Factory { get { return Database.Factory; } }

        /// <summary>链接字符串，会话单独保存，允许修改，修改不会影响数据库中的连接字符串</summary>
        public String ConnectionString { get; set; }

        /// <summary>数据连接对象。</summary>
        public DbConnection Conn { get; private set; }

        /// <summary>查询次数</summary>
        public Int32 QueryTimes { get; set; }

        /// <summary>执行次数</summary>
        public Int32 ExecuteTimes { get; set; }

        /// <summary>线程编号，每个数据库会话应该只属于一个线程，该属性用于检查错误的跨线程操作</summary>
        public Int32 ThreadID { get; } = Thread.CurrentThread.ManagedThreadId;
        #endregion

        #region 打开/关闭
        /// <summary>连接是否已经打开</summary>
        public Boolean Opened
        {
            get
            {
                var conn = Conn;
                if (conn == null) return false;

                try
                {
                    return conn.State != ConnectionState.Closed;
                }
                catch (ObjectDisposedException)
                {
                    return false;
                }
            }
        }

        /// <summary>打开</summary>
        public virtual void Open()
        {
            var tid = Thread.CurrentThread.ManagedThreadId;
            if (ThreadID != tid) DAL.WriteLog("本会话由线程{0}创建，当前线程{1}非法使用该会话！", ThreadID, tid);

            if (Conn == null) Conn = Database.Pool.Acquire();
        }

        /// <summary>关闭</summary>
        public void Close()
        {
            // 有可能是GC调用关闭
            //var tid = Thread.CurrentThread.ManagedThreadId;
            //if (ThreadID != tid) DAL.WriteLog("本会话由线程{0}创建，当前线程{1}非法使用该会话！", ThreadID, tid);

            var conn = Conn;
            if (conn != null)
            {
                Conn = null;
                Database.Pool.Release(conn);
            }
        }

        /// <summary>自动关闭。启用事务后，不关闭连接。</summary>
        public virtual void AutoClose()
        {
            if (Transaction != null || !Opened) return;

            // 检查是否支持自动关闭
            if (_EnableAutoClose != null)
            {
                if (!_EnableAutoClose.Value) return;
            }
            else
            {
                if (!Database.AutoClose) return;
            }

            Close();
        }

        private Boolean? _EnableAutoClose;
        /// <summary>设置自动关闭。启用、禁用、继承</summary>
        /// <param name="enable"></param>
        public void SetAutoClose(Boolean? enable)
        {
            _EnableAutoClose = enable;

            if (enable == null || enable.Value) AutoClose();
        }

        private String _DatabaseName;
        /// <summary>数据库名</summary>
        public String DatabaseName
        {
            get
            {
                if (_DatabaseName == null)
                {
                    using (var pi = Database.Pool.AcquireItem())
                    {
                        _DatabaseName = pi.Value.Database;
                    }
                }

                return _DatabaseName;
            }
        }

        /// <summary>当异常发生时触发。关闭数据库连接，或者返还连接到连接池。</summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        protected virtual XDbException OnException(Exception ex)
        {
            if (Transaction == null && Opened) Close(); // 强制关闭数据库
            if (ex != null)
                return new XDbSessionException(this, ex);
            else
                return new XDbSessionException(this);
        }

        /// <summary>当异常发生时触发。关闭数据库连接，或者返还连接到连接池。</summary>
        /// <param name="ex"></param>
        /// <param name="cmd"></param>
        /// <returns></returns>
        protected virtual XSqlException OnException(Exception ex, DbCommand cmd)
        {
            if (Transaction == null && Opened) Close(); // 强制关闭数据库
            var sql = GetSql(cmd);
            if (ex != null)
                return new XSqlException(sql, this, ex);
            else
                return new XSqlException(sql, this);
        }
        #endregion

        #region 事务
        /// <summary>数据库事务</summary>
        public ITransaction Transaction { get; private set; }

        /// <summary>开始事务</summary>
        /// <remarks>
        /// Read Uncommitted: 允许读取脏数据，一个事务能看到另一个事务还没有提交的数据。（不会阻止其它操作）
        /// Read Committed: 确保事务读取的数据都必须是已经提交的数据。它限制了读取中间的，没有提交的，脏的数据。
        /// 但是它不能确保当事务重新去读取的时候，读的数据跟上次读的数据是一样的，也就是说当事务第一次读取完数据后，
        /// 该数据是可能被其他事务修改的，当它再去读取的时候，数据可能是不一样的。（数据隐藏，不阻止）
        /// Repeatable Read: 是一个更高级别的隔离级别，如果事务再去读取同样的数据，先前的数据是没有被修改过的。（阻止其它修改）
        /// Serializable: 它做出了最有力的保证，除了每次读取的数据是一样的，它还确保每次读取没有新的数据。（阻止其它添删改）
        /// </remarks>
        /// <param name="level">事务隔离等级</param>
        /// <returns>剩下的事务计数</returns>
        public virtual Int32 BeginTransaction(IsolationLevel level)
        {
            if (Disposed) throw new ObjectDisposedException(GetType().Name);

            var tr = Transaction;
            if (tr != null) return tr.Begin().Count;

            try
            {
                tr = new Transaction(this, level);
                tr.Completed += (s, e) => { Transaction = null; /*AutoClose();*/ };

                Transaction = tr;

                return tr.Count;
            }
            catch (DbException ex)
            {
                throw OnException(ex);
            }
        }

        /// <summary>提交事务</summary>
        /// <returns>剩下的事务计数</returns>
        public virtual Int32 Commit()
        {
            var tr = Transaction;
            if (tr == null) throw new XDbSessionException(this, "当前并未开始事务，请用BeginTransaction方法开始新事务！");

            try
            {
                tr.Commit();
            }
            catch (DbException ex)
            {
                throw OnException(ex);
            }

            return tr.Count;
        }

        /// <summary>回滚事务</summary>
        /// <param name="ignoreException">是否忽略异常</param>
        /// <returns>剩下的事务计数</returns>
        public Int32 Rollback(Boolean ignoreException = true)
        {
            var tr = Transaction;
            if (tr == null) throw new XDbSessionException(this, "当前并未开始事务，请用BeginTransaction方法开始新事务！");

            try
            {
                tr.Rollback();
            }
            catch (DbException ex)
            {
                if (!ignoreException) throw OnException(ex);
            }

            return tr.Count;
        }
        #endregion

        #region 基本方法 查询/执行
        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public virtual DataSet Query(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            using (var cmd = OnCreateCommand(sql, type, ps))
            {
                return Query(cmd);
            }
        }

        /// <summary>执行DbCommand，返回记录集</summary>
        /// <param name="cmd">DbCommand</param>
        /// <returns></returns>
        public virtual DataSet Query(DbCommand cmd)
        {
            Transaction?.Check(cmd, false);

            QueryTimes++;
            WriteSQL(cmd);

            using (var da = Factory.CreateDataAdapter())
            using (var pi = Database.Pool.AcquireItem())
            {
                try
                {
                    //if (!Opened) Open();
                    if (cmd.Connection == null) cmd.Connection = pi.Value;
                    da.SelectCommand = cmd;

                    var ds = new DataSet();
                    BeginTrace();
                    da.Fill(ds);
                    return ds;
                }
                catch (DbException ex)
                {
                    // 数据库异常最好销毁连接
                    cmd.Connection.TryDispose();

                    throw OnException(ex, cmd);
                }
                finally
                {
                    EndTrace(cmd);

                    //AutoClose();
                    //cmd.Parameters.Clear();
                }
            }
        }

        private static Regex reg_QueryCount = new Regex(@"^\s*select\s+\*\s+from\s+([\w\W]+)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        /// <summary>执行SQL查询，返回总记录数</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public virtual Int64 QueryCount(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            if (sql.Contains(" "))
            {
                var orderBy = DbBase.CheckOrderClause(ref sql);
                var ms = reg_QueryCount.Matches(sql);
                if (ms != null && ms.Count > 0)
                    sql = String.Format("Select Count(*) From {0}", ms[0].Groups[1].Value);
                else
                    sql = String.Format("Select Count(*) From {0}", DbBase.CheckSimpleSQL(sql));
            }
            else
                sql = String.Format("Select Count(*) From {0}", Database.FormatName(sql));

            return ExecuteScalar<Int64>(sql, type, ps);
        }

        /// <summary>执行SQL查询，返回总记录数</summary>
        /// <param name="builder">查询生成器</param>
        /// <returns>总记录数</returns>
        public virtual Int64 QueryCount(SelectBuilder builder)
        {
            return ExecuteScalar<Int64>(builder.SelectCount().ToString(), CommandType.Text, builder.Parameters.ToArray());
        }

        /// <summary>快速查询单表记录数，稍有偏差</summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public virtual Int64 QueryCountFast(String tableName) => QueryCount(tableName);

        /// <summary>执行SQL语句，返回受影响的行数</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public virtual Int32 Execute(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            using (var cmd = OnCreateCommand(sql, type, ps))
            {
                return Execute(cmd);
            }
        }

        /// <summary>执行DbCommand，返回受影响的行数</summary>
        /// <param name="cmd">DbCommand</param>
        /// <returns></returns>
        public virtual Int32 Execute(DbCommand cmd)
        {
            Transaction?.Check(cmd, true);

            ExecuteTimes++;
            WriteSQL(cmd);

            using (var pi = Database.Pool.AcquireItem())
            {
                try
                {
                    //if (!Opened) Open();
                    if (cmd.Connection == null) cmd.Connection = pi.Value;

                    BeginTrace();
                    return cmd.ExecuteNonQuery();
                }
                catch (DbException ex)
                {
                    throw OnException(ex, cmd);
                }
                finally
                {
                    EndTrace(cmd);

                    //AutoClose();
                    //cmd.Parameters.Clear();
                }
            }
        }

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>新增行的自动编号</returns>
        public virtual Int64 InsertAndGetIdentity(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            //return Execute(sql, type, ps);
            using (var cmd = OnCreateCommand(sql, type, ps))
            {
                Transaction?.Check(cmd, true);

                return ExecuteScalar<Int64>(cmd);
            }
        }

        /// <summary>执行SQL语句，返回结果中的第一行第一列</summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public virtual T ExecuteScalar<T>(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            using (var cmd = OnCreateCommand(sql, type, ps))
            {
                return ExecuteScalar<T>(cmd);
            }
        }

        protected virtual T ExecuteScalar<T>(DbCommand cmd)
        {
            Transaction?.Check(cmd, false);

            QueryTimes++;
            WriteSQL(cmd);

            using (var pi = Database.Pool.AcquireItem())
            {
                try
                {
                    if (cmd.Connection == null) cmd.Connection = pi.Value;

                    BeginTrace();
                    var rs = cmd.ExecuteScalar();
                    if (rs == null || rs == DBNull.Value) return default(T);
                    if (rs is T) return (T)rs;

                    return (T)Reflect.ChangeType(rs, typeof(T));
                }
                catch (DbException ex)
                {
                    throw OnException(ex, cmd);
                }
                finally
                {
                    EndTrace(cmd);

                    //AutoClose();
                    cmd.Parameters.Clear();
                }
            }
        }

        /// <summary>获取一个DbCommand。</summary>
        /// <remark>
        /// 配置了连接，并关联了事务。
        /// 连接已打开。
        /// 使用完毕后，必须调用AutoClose方法，以使得在非事务及设置了自动关闭的情况下关闭连接
        /// </remark>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public virtual DbCommand CreateCommand(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            var cmd = OnCreateCommand(sql, type, ps);
            Transaction?.Check(cmd, true);

            return cmd;
        }

        /// <summary>获取一个DbCommand。</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        protected virtual DbCommand OnCreateCommand(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            var cmd = Factory?.CreateCommand();
            if (cmd == null) return null;

            //if (!Opened) Open();
            //cmd.Connection = Conn;
            cmd.CommandType = type;
            cmd.CommandText = sql;
            if (ps != null && ps.Length > 0) cmd.Parameters.AddRange(ps);

            var timeout = Setting.Current.CommandTimeout;
            if (timeout > 0) cmd.CommandTimeout = timeout;

            return cmd;
        }
        #endregion

        #region 异步操作
#if !NET4
        ///// <summary>异步打开</summary>
        ///// <returns></returns>
        //public virtual async Task OpenAsync()
        //{
        //    var tid = Thread.CurrentThread.ManagedThreadId;
        //    if (ThreadID != tid) DAL.WriteLog("本会话由线程{0}创建，当前线程{1}非法使用该会话！", ThreadID, tid);

        //    var conn = Conn;
        //    if (conn == null || conn.State != ConnectionState.Closed) return;

        //    await conn.OpenAsync();
        //}

        public virtual async Task<DbDataReader> ExecuteReaderAsync(DbCommand cmd)
        {
            Transaction?.Check(cmd, false);

            QueryTimes++;
            WriteSQL(cmd);

            using (var pi = Database.Pool.AcquireItem())
            {
                try
                {
                    //if (!Opened) await OpenAsync();
                    if (cmd.Connection == null) cmd.Connection = pi.Value;

                    BeginTrace();

                    return await cmd.ExecuteReaderAsync();
                }
                catch (DbException ex)
                {
                    throw OnException(ex, cmd);
                }
                finally
                {
                    EndTrace(cmd);

                    //AutoClose();
                    //cmd.Parameters.Clear();
                }
            }
        }

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public virtual async Task<DataResult> QueryAsync(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            var ds = new DataResult
            {
                Rows = new List<Object[]>()
            };

            using (var cmd = OnCreateCommand(sql, type, ps))
            {
                var reader = await ExecuteReaderAsync(cmd);

                var fieldCount = reader.FieldCount;

                ds.Names = new String[fieldCount];
                ds.Types = new Type[fieldCount];
                for (var i = 0; i < fieldCount; i++)
                {
                    ds.Names[i] = reader.GetName(i);
                    ds.Types[i] = reader.GetFieldType(i);
                }

                while (await reader.ReadAsync())
                {
                    var row = new Object[fieldCount];
                    reader.GetValues(row);

                    ds.Rows.Add(row);
                }
            }

            return ds;
        }

        public virtual async Task<Int32> ExecuteNonQueryAsync(DbCommand cmd)
        {
            Transaction?.Check(cmd, true);

            ExecuteTimes++;
            WriteSQL(cmd);

            using (var pi = Database.Pool.AcquireItem())
            {
                try
                {
                    //if (!Opened) await OpenAsync();
                    if (cmd.Connection == null) cmd.Connection = pi.Value;

                    BeginTrace();
                    return await cmd.ExecuteNonQueryAsync();
                }
                catch (DbException ex)
                {
                    throw OnException(ex, cmd);
                }
                finally
                {
                    EndTrace(cmd);

                    //AutoClose();
                    //cmd.Parameters.Clear();
                }
            }
        }

        public virtual async Task<T> ExecuteScalarAsync<T>(DbCommand cmd)
        {
            QueryTimes++;

            WriteSQL(cmd);
            try
            {
                BeginTrace();

                var rs = await cmd.ExecuteScalarAsync();
                if (rs == null || rs == DBNull.Value) return default(T);
                if (rs is T) return (T)rs;

                return (T)Reflect.ChangeType(rs, typeof(T));
            }
            catch (DbException ex)
            {
                throw OnException(ex, cmd);
            }
            finally
            {
                EndTrace(cmd);

                //AutoClose();
                //cmd.Parameters.Clear();
            }
        }
#endif
        #endregion

        #region 高级
        /// <summary>清空数据表，标识归零</summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public virtual Int32 Truncate(String tableName)
        {
            var sql = "Truncate Table {0}".F(Database.FormatName(tableName));
            return Execute(sql);
        }
        #endregion

        #region 架构
        private DictionaryCache<String, DataTable> _schCache = new DictionaryCache<String, DataTable>(StringComparer.OrdinalIgnoreCase)
        {
            Expire = 10,
            Period = 10 * 60,
            //// 不能异步。否则，修改表结构后，第一次获取会是旧的
            //Asynchronous = false
        };

        /// <summary>返回数据源的架构信息。缓存10分钟</summary>
        /// <param name="collectionName">指定要返回的架构的名称。</param>
        /// <param name="restrictionValues">为请求的架构指定一组限制值。</param>
        /// <returns></returns>
        public virtual DataTable GetSchema(String collectionName, String[] restrictionValues)
        {
            // 小心collectionName为空，此时列出所有架构名称
            var key = "" + collectionName;
            if (restrictionValues != null && restrictionValues.Length > 0) key += "_" + String.Join("_", restrictionValues);

            //return _schCache.GetItem(key, k => GetSchemaInternal(k, collectionName, restrictionValues));
            var dt = _schCache[key];
            if (dt == null) _schCache[key] = dt = GetSchemaInternal(key, collectionName, restrictionValues);

            return dt;
        }

        DataTable GetSchemaInternal(String key, String collectionName, String[] restrictionValues)
        {
            QueryTimes++;
            //// 如果启用了事务保护，这里要新开一个连接，否则MSSQL里面报错，SQLite不报错，其它数据库未测试
            //var isTrans = Transaction != null;

            //DbConnection conn = null;
            //if (isTrans)
            //{
            //    conn = Factory.CreateConnection();
            //    CheckConnStr();
            //    conn.ConnectionString = ConnectionString;
            //    conn.Open();
            //}
            //else
            //{
            //    if (!Opened) Open();
            //    conn = Conn;
            //}

            //// 连接未打开
            //if (conn.State == ConnectionState.Closed) return null;

            using (var pi = Database.Pool.AcquireItem())
            {
                var conn = pi.Value;
                //try
                //{
                DataTable dt = null;

                var sw = Stopwatch.StartNew();

                if (restrictionValues == null || restrictionValues.Length < 1)
                {
                    if (String.IsNullOrEmpty(collectionName))
                    {
                        WriteSQL("[" + Database.ConnName + "]GetSchema");
                        dt = conn.GetSchema();
                    }
                    else
                    {
                        WriteSQL("[" + Database.ConnName + "]GetSchema(\"" + collectionName + "\")");
                        dt = conn.GetSchema(collectionName);
                    }
                }
                else
                {
                    var sb = new StringBuilder();
                    foreach (var item in restrictionValues)
                    {
                        sb.Append(", ");
                        if (item == null)
                            sb.Append("null");
                        else
                            sb.AppendFormat("\"{0}\"", item);
                    }
                    WriteSQL("[" + Database.ConnName + "]GetSchema(\"" + collectionName + "\"" + sb + ")");
                    dt = conn.GetSchema(collectionName, restrictionValues);
                }

                sw.Stop();
                // 耗时超过多少秒输出错误日志
                if (sw.ElapsedMilliseconds > 1000) DAL.WriteLog("GetSchema耗时 {0:n0}ms", sw.ElapsedMilliseconds);

                return dt;
                //}
                //catch (DbException ex)
                //{
                //    throw new XDbSessionException(this, "取得所有表构架出错！", ex);
                //}
                //finally
                //{
                //    if (isTrans)
                //        conn.Close();
                //    else
                //        AutoClose();
                //}
            }
        }
        #endregion

        #region Sql日志输出
        /// <summary>是否输出SQL语句，默认为XCode调试开关XCode.Debug</summary>
        public Boolean ShowSQL { get; set; }

        static ILog logger;

        /// <summary>写入SQL到文本中</summary>
        /// <param name="sql"></param>
        public void WriteSQL(String sql)
        {
            if (!ShowSQL) return;

#if !__CORE__
            // 如果页面设定有XCode_SQLList列表，则往列表写入SQL语句
            var context = HttpContext.Current;
            if (context != null)
            {
                if (context.Items["XCode_SQLList"] is List<String> list) list.Add(sql);
            }
#endif

            var sqlpath = Setting.Current.SQLPath;
            if (String.IsNullOrEmpty(sqlpath))
                WriteLog(sql);
            else
            {
                if (logger == null) logger = TextFileLog.Create(sqlpath);
                logger.Info(sql);
            }
        }

        private String GetSql(DbCommand cmd)
        {
            var sql = cmd.CommandText;
            var ps = cmd.Parameters;
            if (ps != null && ps.Count > 0)
            {
                var sb = new StringBuilder(64);
                sb.Append(sql);
                sb.Append(" [");
                for (var i = 0; i < ps.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    var v = ps[i].Value;
                    var sv = "";
                    if (v is Byte[])
                    {
                        var bv = v as Byte[];
                        if (bv.Length > 8)
                            sv = String.Format("[{0}]0x{1}...", bv.Length, BitConverter.ToString(bv, 0, 8));
                        else
                            sv = String.Format("[{0}]0x{1}", bv.Length, BitConverter.ToString(bv));
                    }
                    else if (v is String)
                    {
                        sv = v as String;
                        if (sv.Length > 32) sv = String.Format("[{0}]{1}...", sv.Length, sv.Substring(0, 8));
                    }
                    else
                        sv = "{0}".F(v);
                    sb.AppendFormat("{0}={1}", ps[i].ParameterName, sv);
                }
                sb.Append("]");
                sql = sb.ToString();
            }

            return sql;
        }

        public void WriteSQL(DbCommand cmd)
        {
            if (!ShowSQL) return;

            //var sql = cmd.CommandText;
            //if (cmd.CommandType != CommandType.Text) sql = String.Format("[{0}]{1}", cmd.CommandType, sql);

            var sql = GetSql(cmd);

            WriteSQL(sql);
        }

        ///// <summary>输出日志</summary>
        ///// <param name="msg"></param>
        //public static void WriteLog(String msg) { DAL.WriteLog(msg); }

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLog(String format, params Object[] args)
        {
            //DAL.WriteLog(format, args);
            XTrace.WriteLine(format, args);
        }
        #endregion

        #region SQL时间跟踪
        private Stopwatch _swSql;
        private static HashSet<String> _trace_sqls = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

        protected void BeginTrace()
        {
            if ((Database as DbBase).TraceSQLTime <= 0) return;

            if (_swSql == null) _swSql = new Stopwatch();

            if (_swSql.IsRunning) _swSql.Stop();

            _swSql.Reset();
            _swSql.Start();
        }

        protected void EndTrace(DbCommand cmd)
        {
            if (_swSql == null) return;

            _swSql.Stop();

            if (_swSql.ElapsedMilliseconds < (Database as DbBase).TraceSQLTime) return;

            var sql = GetSql(cmd);

            // 同一个SQL只需要报警一次
            if (_trace_sqls.Contains(sql)) return;
            lock (_trace_sqls)
            {
                if (_trace_sqls.Contains(sql)) return;

                if (_trace_sqls.Count >= 1000) _trace_sqls.Clear();
                _trace_sqls.Add(sql);
            }

#if !__CORE__
            var obj = new SQLRunEvent() { Sql = sql, RunTime = _swSql.ElapsedMilliseconds };
            EventBus.Instance.Publish(obj);
#endif
            XTrace.WriteLine("SQL耗时较长，建议优化 {0:n0}毫秒 {1}", _swSql.ElapsedMilliseconds, sql);
        }
        #endregion
    }
}