using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            try
            {
                // 注意，没有Commit的数据，在这里将会被回滚
                //if (Trans != null) Rollback();
                // 在嵌套事务中，Rollback只能减少嵌套层数，而_Trans.Rollback能让事务马上回滚
                if (Trans != null && Opened) Trans.Rollback();
                if (_Conn != null) Close();
                if (_Conn != null)
                {
                    var conn = _Conn;
                    _Conn = null;
                    conn.Dispose();
                }
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
        private DatabaseType DbType { get { return Database.DbType; } }

        /// <summary>工厂</summary>
        private DbProviderFactory Factory { get { return Database.Factory; } }

        /// <summary>链接字符串，会话单独保存，允许修改，修改不会影响数据库中的连接字符串</summary>
        public String ConnectionString { get; set; }

        private DbConnection _Conn;
        /// <summary>数据连接对象。</summary>
        public DbConnection Conn
        {
            get
            {
                if (_Conn == null)
                {
                    try
                    {
                        _Conn = Factory.CreateConnection();
                    }
                    catch (ObjectDisposedException) { this.Dispose(); throw; }
                    //_Conn.ConnectionString = Database.ConnectionString;
                    checkConnStr();
                    _Conn.ConnectionString = ConnectionString;
                }
                return _Conn;
            }
            //set { _Conn = value; }
        }

        protected void checkConnStr()
        {
            if (ConnectionString.IsNullOrWhiteSpace())
                throw new XCodeException("[{0}]未指定连接字符串！", Database == null ? "" : Database.ConnName);
        }

        /// <summary>查询次数</summary>
        public Int32 QueryTimes { get; set; }

        /// <summary>执行次数</summary>
        public Int32 ExecuteTimes { get; set; }

        /// <summary>线程编号，每个数据库会话应该只属于一个线程，该属性用于检查错误的跨线程操作</summary>
        public Int32 ThreadID { get; } = Thread.CurrentThread.ManagedThreadId;
        #endregion

        #region 打开/关闭
        /// <summary>连接是否已经打开</summary>
        public Boolean Opened { get { return _Conn != null && _Conn.State != ConnectionState.Closed; } }

        /// <summary>打开</summary>
        public virtual void Open()
        {
            var tid = Thread.CurrentThread.ManagedThreadId;
            if (ThreadID != tid) DAL.WriteLog("本会话由线程{0}创建，当前线程{1}非法使用该会话！", ThreadID, tid);

            var conn = Conn;
            if (conn != null && conn.State == ConnectionState.Closed)
            {
                try
                {
                    conn.Open();
                }
                catch (DbException)
                {
                    DAL.WriteLog("Open错误：{0}", conn.ConnectionString);
                    throw;
                }
            }
        }

        /// <summary>异步打开</summary>
        public virtual async Task OpenAsync()
        {
            var tid = Thread.CurrentThread.ManagedThreadId;
            if (ThreadID != tid) DAL.WriteLog("本会话由线程{0}创建，当前线程{1}非法使用该会话！", ThreadID, tid);

            var conn = Conn;
            if (conn == null || conn.State != ConnectionState.Closed) return;

            await conn.OpenAsync();
        }

        /// <summary>关闭</summary>
        public virtual void Close()
        {
            var conn = _Conn;
            if (conn != null)
            {
                try { if (conn.State != ConnectionState.Closed) conn.Close(); }
                catch (ObjectDisposedException) { }
                catch (Exception ex)
                {
                    WriteLog("{0}.Close出错：{1}", DbType, ex);
                }
            }
        }

        /// <summary>自动关闭。启用事务后，不关闭连接。</summary>
        public void AutoClose()
        {
            if (Trans == null && Opened) Close();
        }

        /// <summary>数据库名</summary>
        public String DatabaseName
        {
            get { return Conn?.Database; }
            set
            {
                if (DatabaseName == value) return;

                // 因为MSSQL多次出现因连接字符串错误而导致的报错，连接字符串变错设置变空了，这里统一关闭连接，采用保守做法修改字符串
                var b = Opened;
                if (b) Close();

                //如果没有打开，则改变链接字符串
                var builder = new XDbConnectionStringBuilder();
                builder.ConnectionString = ConnectionString;
                var flag = false;
                if (builder.ContainsKey("Database"))
                {
                    builder["Database"] = value;
                    flag = true;
                }
                else if (builder.ContainsKey("Initial Catalog"))
                {
                    builder["Initial Catalog"] = value;
                    flag = true;
                }
                if (flag)
                {
                    var connStr = builder.ToString();
                    ConnectionString = connStr;
                    Conn.ConnectionString = connStr;
                }
                if (b) Open();
            }
        }

        /// <summary>当异常发生时触发。关闭数据库连接，或者返还连接到连接池。</summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        protected virtual XDbException OnException(Exception ex)
        {
            if (Trans == null && Opened) Close(); // 强制关闭数据库
            if (ex != null)
                return new XDbSessionException(this, ex);
            else
                return new XDbSessionException(this);
        }

        /// <summary>当异常发生时触发。关闭数据库连接，或者返还连接到连接池。</summary>
        /// <param name="ex"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        protected virtual XSqlException OnException(Exception ex, String sql)
        {
            if (Trans == null && Opened) Close(); // 强制关闭数据库
            if (ex != null)
                return new XSqlException(sql, this, ex);
            else
                return new XSqlException(sql, this);
        }
        #endregion

        #region 事务
        /// <summary>数据库事务</summary>
        public ITransaction Trans { get; private set; }

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
            if (Disposed) throw new ObjectDisposedException(this.GetType().Name);

            var tr = Trans;
            if (tr != null) return tr.Begin().Count;

            try
            {
                if (!Opened) Open();

                tr = new Transaction(Conn, level);

                Trans = tr;

                return tr.Count;
            }
            catch (DbException ex)
            {
                throw OnException(ex);
            }
        }

        /// <summary>提交事务</summary>
        /// <returns>剩下的事务计数</returns>
        public Int32 Commit()
        {
            var tr = Trans;
            if (tr == null) throw new XDbSessionException(this, "当前并未开始事务，请用BeginTransaction方法开始新事务！");

            try
            {
                tr.Commit();
            }
            catch (DbException ex)
            {
                throw OnException(ex);
            }
            finally
            {
                if (tr.Count == 0)
                {
                    Trans = null;
                    AutoClose();
                }
            }

            return tr.Count;
        }

        /// <summary>回滚事务</summary>
        /// <param name="ignoreException">是否忽略异常</param>
        /// <returns>剩下的事务计数</returns>
        public Int32 Rollback(Boolean ignoreException = true)
        {
            var tr = Trans;
            if (tr == null) throw new XDbSessionException(this, "当前并未开始事务，请用BeginTransaction方法开始新事务！");

            // 输出事务日志
            if (Setting.Current.TransactionDebug) XTrace.DebugStack();
            try
            {
                tr.Rollback();
            }
            catch (DbException ex)
            {
                if (!ignoreException) throw OnException(ex);
            }
            finally
            {
                if (tr.Count == 0)
                {
                    Trans = null;
                    AutoClose();
                }
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
        public virtual DataSet Query(String sql, CommandType type = CommandType.Text, params DbParameter[] ps)
        {
            return Query(CreateCommand(sql, type, ps));
        }

        /// <summary>执行DbCommand，返回记录集</summary>
        /// <param name="cmd">DbCommand</param>
        /// <returns></returns>
        public virtual DataSet Query(DbCommand cmd)
        {
            QueryTimes++;
            WriteSQL(cmd);
            using (var da = Factory.CreateDataAdapter())
            {
                try
                {
                    if (!Opened) Open();
                    cmd.Connection = Conn;
                    if (Trans != null) cmd.Transaction = Trans.Trans;
                    da.SelectCommand = cmd;

                    var ds = new DataSet();
                    BeginTrace();
                    da.Fill(ds);
                    return ds;
                }
                catch (DbException ex)
                {
                    throw OnException(ex, cmd.CommandText);
                }
                finally
                {
                    EndTrace(cmd.CommandText);

                    AutoClose();
                    cmd.Parameters.Clear();
                }
            }
        }

        /// <summary>异步执行DbCommand，返回记录集</summary>
        /// <param name="cmd">DbCommand</param>
        /// <returns></returns>
        public virtual async Task<DataSet> QueryAsync(DbCommand cmd)
        {
            QueryTimes++;
            WriteSQL(cmd);
            using (var da = Factory.CreateDataAdapter())
            {
                try
                {
                    if (!Opened) await OpenAsync();
                    cmd.Connection = Conn;
                    if (Trans != null) cmd.Transaction = Trans.Trans;
                    da.SelectCommand = cmd;

                    var ds = new DataSet();
                    BeginTrace();
                    da.Fill(ds);
                    return ds;
                }
                catch (DbException ex)
                {
                    throw OnException(ex, cmd.CommandText);
                }
                finally
                {
                    EndTrace(cmd.CommandText);

                    AutoClose();
                    cmd.Parameters.Clear();
                }
            }
        }

        private static Regex reg_QueryCount = new Regex(@"^\s*select\s+\*\s+from\s+([\w\W]+)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        /// <summary>执行SQL查询，返回总记录数</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public virtual Int64 QueryCount(String sql, CommandType type = CommandType.Text, params DbParameter[] ps)
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
        public virtual Int64 QueryCountFast(String tableName) { return QueryCount(tableName); }

        /// <summary>执行SQL语句，返回受影响的行数</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public virtual Int32 Execute(String sql, CommandType type = CommandType.Text, params DbParameter[] ps)
        {
            return Execute(CreateCommand(sql, type, ps));
        }

        /// <summary>执行DbCommand，返回受影响的行数</summary>
        /// <param name="cmd">DbCommand</param>
        /// <returns></returns>
        public virtual Int32 Execute(DbCommand cmd)
        {
            ExecuteTimes++;
            WriteSQL(cmd);
            try
            {
                if (!Opened) Open();
                cmd.Connection = Conn;
                if (Trans != null) cmd.Transaction = Trans.Trans;

                BeginTrace();
                return cmd.ExecuteNonQuery();
            }
            catch (DbException ex)
            {
                throw OnException(ex, cmd.CommandText);
            }
            finally
            {
                EndTrace(cmd.CommandText);

                AutoClose();
                cmd.Parameters.Clear();
            }
        }

        /// <summary>执行DbCommand，返回受影响的行数</summary>
        /// <param name="cmd">DbCommand</param>
        /// <returns></returns>
        public virtual async Task<Int32> ExecuteAsync(DbCommand cmd)
        {
            ExecuteTimes++;
            WriteSQL(cmd);
            try
            {
                if (!Opened) await OpenAsync();
                cmd.Connection = Conn;
                if (Trans != null) cmd.Transaction = Trans.Trans;

                BeginTrace();
                return await cmd.ExecuteNonQueryAsync();
            }
            catch (DbException ex)
            {
                throw OnException(ex, cmd.CommandText);
            }
            finally
            {
                EndTrace(cmd.CommandText);

                AutoClose();
                cmd.Parameters.Clear();
            }
        }

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>新增行的自动编号</returns>
        public virtual Int64 InsertAndGetIdentity(String sql, CommandType type = CommandType.Text, params DbParameter[] ps)
        {
            return Execute(sql, type, ps);
        }

        /// <summary>执行SQL语句，返回结果中的第一行第一列</summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public virtual T ExecuteScalar<T>(String sql, CommandType type = CommandType.Text, params DbParameter[] ps)
        {
            return ExecuteScalar<T>(CreateCommand(sql, type, ps));
        }

        protected virtual T ExecuteScalar<T>(DbCommand cmd)
        {
            QueryTimes++;

            WriteSQL(cmd);
            try
            {
                BeginTrace();
                Object rs = cmd.ExecuteScalar();
                if (rs == null || rs == DBNull.Value) return default(T);
                if (rs is T) return (T)rs;
                return (T)Reflect.ChangeType(rs, typeof(T));
            }
            catch (DbException ex)
            {
                throw OnException(ex, cmd.CommandText);
            }
            finally
            {
                EndTrace(cmd.CommandText);

                AutoClose();
                cmd.Parameters.Clear();
            }
        }

        /// <summary>获取一个DbCommand。</summary>
        /// <remark>
        /// 配置了连接，并关联了事务。
        /// 连接已打开。
        /// 使用完毕后，必须调用AutoClose方法，以使得在非事务及设置了自动关闭的情况下关闭连接
        /// </remark>
        /// <returns></returns>
        public virtual DbCommand CreateCommand()
        {
            var cmd = Factory.CreateCommand();
            if (!Opened) Open();
            cmd.Connection = Conn;
            if (Trans != null) cmd.Transaction = Trans.Trans;

            return cmd;
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
        public virtual DbCommand CreateCommand(String sql, CommandType type = CommandType.Text, params DbParameter[] ps)
        {
            var cmd = CreateCommand();

            cmd.CommandType = type;
            cmd.CommandText = sql;
            if (ps != null && ps.Length > 0) cmd.Parameters.AddRange(ps);

            return cmd;
        }
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
            ClearPeriod = 10 * 60,
            // 不能异步。否则，修改表结构后，第一次获取会是旧的
            Asynchronous = false
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
            return _schCache.GetItem(key, k => GetSchemaInternal(k, collectionName, restrictionValues));
        }

        DataTable GetSchemaInternal(String key, String collectionName, String[] restrictionValues)
        {
            QueryTimes++;
            // 如果启用了事务保护，这里要新开一个连接，否则MSSQL里面报错，SQLite不报错，其它数据库未测试
            var isTrans = Trans != null;

            DbConnection conn = null;
            if (isTrans)
            {
                conn = Factory.CreateConnection();
                checkConnStr();
                conn.ConnectionString = ConnectionString;
                conn.Open();
            }
            else
            {
                if (!Opened) Open();
                conn = Conn;
            }

            try
            {
                DataTable dt;

                var sw = new Stopwatch();
                sw.Start();

                if (restrictionValues == null || restrictionValues.Length < 1)
                {
                    if (String.IsNullOrEmpty(collectionName))
                    {
                        WriteSQL("[" + Database.ConnName + "]GetSchema");
                        if (conn.State != ConnectionState.Closed) //ahuang 2013。06。25 当数据库连接字符串有误
                            dt = conn.GetSchema();
                        else
                            dt = null;
                    }
                    else
                    {
                        WriteSQL("[" + Database.ConnName + "]GetSchema(\"" + collectionName + "\")");
                        if (conn.State != ConnectionState.Closed)
                            dt = conn.GetSchema(collectionName);
                        else
                            dt = null;
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
                    if (conn.State != ConnectionState.Closed)
                        dt = conn.GetSchema(collectionName, restrictionValues);
                    else
                        dt = null;
                }

                sw.Stop();
                // 耗时超过多少秒输出错误日志
                if (sw.ElapsedMilliseconds > 1000) DAL.WriteLog("GetSchema耗时 {0:n0}ms", sw.ElapsedMilliseconds);

                return dt;
            }
            catch (DbException ex)
            {
                throw new XDbSessionException(this, "取得所有表构架出错！", ex);
            }
            finally
            {
                if (isTrans)
                    conn.Close();
                else
                    AutoClose();
            }
        }
        #endregion

        #region Sql日志输出
        /// <summary>是否输出SQL语句，默认为XCode调试开关XCode.Debug</summary>
        public Boolean ShowSQL { get; set; }

        static ILog logger;

        /// <summary>写入SQL到文本中</summary>
        /// <param name="sql"></param>
        /// <param name="ps"></param>
        public void WriteSQL(String sql, params DbParameter[] ps)
        {
            if (!ShowSQL) return;

            if (ps != null && ps.Length > 0)
            {
                var sb = new StringBuilder(64);
                sb.Append(sql);
                sb.Append("[");
                for (int i = 0; i < ps.Length; i++)
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
                        sv = "" + v;
                    sb.AppendFormat("{1}:{0}={2}", ps[i].ParameterName, ps[i].DbType, sv);
                }
                sb.Append("]");
                sql = sb.ToString();
            }

            // 如果页面设定有XCode_SQLList列表，则往列表写入SQL语句
            var context = HttpContext.Current;
            if (context != null)
            {
                var list = context.Items["XCode_SQLList"] as List<String>;
                if (list != null) list.Add(sql);
            }

            var sqlpath = Setting.Current.SQLPath;
            if (String.IsNullOrEmpty(sqlpath))
                WriteLog(sql);
            else
            {
                if (logger == null) logger = TextFileLog.Create(sqlpath);
                logger.Info(sql);
            }
        }

        public void WriteSQL(DbCommand cmd)
        {
            var sql = cmd.CommandText;
            if (cmd.CommandType != CommandType.Text) sql = String.Format("[{0}]{1}", cmd.CommandType, sql);

            DbParameter[] ps = null;
            if (cmd.Parameters != null)
            {
                var cps = cmd.Parameters;
                ps = new DbParameter[cps.Count];
                //cmd.Parameters.CopyTo(ps, 0);
                for (int i = 0; i < ps.Length; i++)
                {
                    ps[i] = cps[i];
                }
            }

            WriteSQL(sql, ps);
        }

        /// <summary>输出日志</summary>
        /// <param name="msg"></param>
        public static void WriteLog(String msg) { DAL.WriteLog(msg); }

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLog(String format, params Object[] args) { DAL.WriteLog(format, args); }
        #endregion

        #region SQL时间跟踪
        private Stopwatch _swSql;
        private static HashSet<String> _trace_sqls = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

        protected void BeginTrace()
        {
            if (Setting.Current.TraceSQLTime <= 0) return;

            if (_swSql == null) _swSql = new Stopwatch();

            if (_swSql.IsRunning) _swSql.Stop();

            _swSql.Reset();
            _swSql.Start();
        }

        protected void EndTrace(String sql)
        {
            if (_swSql == null) return;

            _swSql.Stop();

            if (_swSql.ElapsedMilliseconds < Setting.Current.TraceSQLTime) return;

            // 同一个SQL只需要报警一次
            if (_trace_sqls.Contains(sql)) return;
            lock (_trace_sqls)
            {
                if (_trace_sqls.Contains(sql)) return;

                _trace_sqls.Add(sql);
            }

            XTrace.WriteLine("SQL耗时较长，建议优化 {0:n0}毫秒 {1}", _swSql.ElapsedMilliseconds, sql);
        }
        #endregion
    }
}