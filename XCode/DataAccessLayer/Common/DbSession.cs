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
using NewLife.Data;
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
                /*if (Opened)*/
                Transaction.TryDispose();

                //Close();
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
        private DatabaseType DbType => Database.Type;

        /// <summary>工厂</summary>
        private DbProviderFactory Factory => Database.Factory;

        /// <summary>链接字符串，会话单独保存，允许修改，修改不会影响数据库中的连接字符串</summary>
        public String ConnectionString { get; set; }

        ///// <summary>数据连接对象。</summary>
        //public DbConnection Conn { get; protected set; }

        /// <summary>查询次数</summary>
        public Int32 QueryTimes { get; set; }

        /// <summary>执行次数</summary>
        public Int32 ExecuteTimes { get; set; }

        /// <summary>线程编号，每个数据库会话应该只属于一个线程，该属性用于检查错误的跨线程操作</summary>
        public Int32 ThreadID { get; } = Thread.CurrentThread.ManagedThreadId;
        #endregion

        #region 打开/关闭
        /// <summary>当异常发生时触发。关闭数据库连接，或者返还连接到连接池。</summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        protected virtual XDbException OnException(Exception ex)
        {
            //if (Transaction == null && Opened) Close(); // 强制关闭数据库
            if (ex != null)
                return new XDbSessionException(this, ex);
            else
                return new XDbSessionException(this);
        }

        /// <summary>当异常发生时触发。关闭数据库连接，或者返还连接到连接池。</summary>
        /// <param name="ex"></param>
        /// <param name="cmd"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        protected virtual XSqlException OnException(Exception ex, DbCommand cmd, String sql)
        {
            //if (Transaction == null && Opened) Close(); // 强制关闭数据库
            if (sql.IsNullOrEmpty()) sql = GetSql(cmd);
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

            try
            {
                var tr = Transaction;
                if (tr == null || tr is DisposeBase db && db.Disposed)
                {
                    tr = new Transaction(this, level);

                    Transaction = tr;
                }

                return tr.Begin().Count;
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
            finally
            {
                if (tr.Count == 0) Transaction = null;
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
            finally
            {
                if (tr.Count == 0) Transaction = null;
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
            return Execute(cmd, true, cmd2 =>
            {
                using (var da = Factory.CreateDataAdapter())
                {
                    da.SelectCommand = cmd2;

                    var ds = new DataSet();
                    da.Fill(ds);

                    return ds;
                }
            });
        }

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public virtual DbTable Query(String sql, IDataParameter[] ps)
        {
            //var dps = ps == null ? null : Database.CreateParameters(ps);
            using (var cmd = OnCreateCommand(sql, CommandType.Text, ps))
            {
                return Execute(cmd, true, cmd2 =>
                {
                    using (var dr = cmd2.ExecuteReader())
                    {
                        var ds = new DbTable();
                        OnFill(ds, dr);
                        ds.Read(dr);

                        return ds;
                    }
                });
            }
        }

        protected virtual void OnFill(DbTable ds, DbDataReader dr) { }

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
        public virtual Int64 QueryCount(SelectBuilder builder) => ExecuteScalar<Int64>(builder.SelectCount().ToString(), CommandType.Text, builder.Parameters.ToArray());

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
                return Execute(cmd, true, cmd2 => cmd2.ExecuteNonQuery());
            }
        }

        /// <summary>执行DbCommand，返回受影响的行数</summary>
        /// <param name="cmd">DbCommand</param>
        /// <returns></returns>
        public virtual Int32 Execute(DbCommand cmd) => Execute(cmd, true, cmd2 => cmd2.ExecuteNonQuery());

        public virtual T Execute<T>(DbCommand cmd, Boolean query, Func<DbCommand, T> callback)
        {
            Transaction?.Check(cmd, !query);

            if (query)
                QueryTimes++;
            else
                ExecuteTimes++;

            var text = WriteSQL(cmd);

            DbConnection conn = null;
            try
            {
                if (cmd.Connection == null) cmd.Connection = conn = Database.Pool.Get();

                BeginTrace();
                return callback(cmd);
            }
            catch (DbException ex)
            {
                throw OnException(ex, cmd, text);
            }
            finally
            {
                if (conn != null) Database.Pool.Put(conn);

                EndTrace(cmd, text);
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
                //Transaction?.Check(cmd, true);

                //return ExecuteScalar<Int64>(cmd);
                return Execute(cmd, false, cmd2 =>
                {
                    var rs = cmd.ExecuteScalar();
                    if (rs == null || rs == DBNull.Value) return 0;

                    return Reflect.ChangeType<Int64>(rs);
                });
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
                //return ExecuteScalar<T>(cmd);
                return Execute(cmd, true, cmd2 =>
                {
                    var rs = cmd.ExecuteScalar();
                    if (rs == null || rs == DBNull.Value) return default(T);
                    if (rs is T) return (T)rs;

                    return (T)Reflect.ChangeType(rs, typeof(T));
                });
            }
        }

        //protected virtual T ExecuteScalar<T>(DbCommand cmd)
        //{
        //    Transaction?.Check(cmd, false);

        //    QueryTimes++;
        //    WriteSQL(cmd);

        //    var conn = Database.Pool.Get();
        //    try
        //    {
        //        if (cmd.Connection == null) cmd.Connection = conn;

        //        BeginTrace();
        //        var rs = cmd.ExecuteScalar();
        //        if (rs == null || rs == DBNull.Value) return default(T);
        //        if (rs is T) return (T)rs;

        //        return (T)Reflect.ChangeType(rs, typeof(T));
        //    }
        //    catch (DbException ex)
        //    {
        //        throw OnException(ex, cmd);
        //    }
        //    finally
        //    {
        //        Database.Pool.Put(conn);
        //        EndTrace(cmd);

        //        //AutoClose();
        //        cmd.Parameters.Clear();
        //    }
        //}

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

        #region 批量操作
        /// <summary>批量插入</summary>
        /// <param name="tableName">表名</param>
        /// <param name="columns">要插入的字段，默认所有字段</param>
        /// <param name="list">实体列表</param>
        /// <returns></returns>
        public virtual Int32 Insert(String tableName, IDataColumn[] columns, IEnumerable<IIndexAccessor> list) => throw new NotSupportedException();

        /// <summary>批量更新</summary>
        /// <param name="tableName">表名</param>
        /// <param name="columns">要更新的字段，默认所有字段</param>
        /// <param name="updateColumns">要更新的字段，默认脏数据</param>
        /// <param name="addColumns">要累加更新的字段，默认累加</param>
        /// <param name="list">实体列表</param>
        /// <returns></returns>
        public virtual Int32 Update(String tableName, IDataColumn[] columns, ICollection<String> updateColumns, ICollection<String> addColumns, IEnumerable<IIndexAccessor> list) => throw new NotSupportedException();

        /// <summary>批量插入或更新</summary>
        /// <param name="tableName">表名</param>
        /// <param name="columns">要插入的字段，默认所有字段</param>
        /// <param name="updateColumns">主键已存在时，要更新的字段</param>
        /// <param name="addColumns">主键已存在时，要累加更新的字段</param>
        /// <param name="list">实体列表</param>
        /// <returns></returns>
        public virtual Int32 InsertOrUpdate(String tableName, IDataColumn[] columns, ICollection<String> updateColumns, ICollection<String> addColumns, IEnumerable<IIndexAccessor> list) => throw new NotSupportedException();
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

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public virtual async Task<DbTable> QueryAsync(String sql, params IDataParameter[] ps)
        {
            using (var cmd = OnCreateCommand(sql, CommandType.Text, ps))
            {
                Transaction?.Check(cmd, false);

                QueryTimes++;
                var text = WriteSQL(cmd);

                var conn = Database.Pool.Get();
                try
                {
                    if (cmd.Connection == null) cmd.Connection = conn;

                    BeginTrace();

                    using (var dr = await cmd.ExecuteReaderAsync())
                    {
                        var ds = new DbTable();
                        OnFill(ds, dr);
                        ds.Read(dr);

                        return ds;
                    }
                }
                catch (DbException ex)
                {
                    // 数据库异常最好销毁连接
                    cmd.Connection.TryDispose();

                    throw OnException(ex, cmd, text);
                }
                finally
                {
                    Database.Pool.Put(conn);
                    EndTrace(cmd, text);
                }
            }
        }

        public virtual async Task<Int32> ExecuteNonQueryAsync(DbCommand cmd)
        {
            Transaction?.Check(cmd, true);

            ExecuteTimes++;
            var text = WriteSQL(cmd);

            var conn = Database.Pool.Get();
            try
            {
                if (cmd.Connection == null) cmd.Connection = conn;

                BeginTrace();
                return await cmd.ExecuteNonQueryAsync();
            }
            catch (DbException ex)
            {
                throw OnException(ex, cmd, text);
            }
            finally
            {
                Database.Pool.Put(conn);
                EndTrace(cmd, text);
            }
        }

        public virtual async Task<T> ExecuteScalarAsync<T>(DbCommand cmd)
        {
            QueryTimes++;

            var text = WriteSQL(cmd);
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
                throw OnException(ex, cmd, text);
            }
            finally
            {
                EndTrace(cmd, text);
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
        /// <summary>返回数据源的架构信息。缓存10分钟</summary>
        /// <param name="conn">连接</param>
        /// <param name="collectionName">指定要返回的架构的名称。</param>
        /// <param name="restrictionValues">为请求的架构指定一组限制值。</param>
        /// <returns></returns>
        public virtual DataTable GetSchema(DbConnection conn, String collectionName, String[] restrictionValues)
        {
            // 小心collectionName为空，此时列出所有架构名称
            var key = "" + collectionName;
            if (restrictionValues != null && restrictionValues.Length > 0) key += "_" + String.Join("_", restrictionValues);

            var db = Database as DbBase;
            var dt = db._SchemaCache[key];
            if (dt == null)
            {
                var conn2 = conn ?? Database.Pool.Get();
                try
                {
                    dt = GetSchemaInternal(conn2, key, collectionName, restrictionValues);
                }
                finally
                {
                    if (conn == null) Database.Pool.Put(conn2);
                }

                db._SchemaCache[key] = dt;
            }

            return dt;
        }

        DataTable GetSchemaInternal(DbConnection conn, String key, String collectionName, String[] restrictionValues)
        {
            QueryTimes++;

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
            if (sql.IsNullOrEmpty()) return;

#if !__CORE__
            // 如果页面设定有XCode_SQLList列表，则往列表写入SQL语句
            var context = HttpContext.Current;
            if (context != null)
            {
                if (context.Items["XCode_SQLList"] is List<String> list) list.Add(sql);
            }
#endif

            if (!ShowSQL) return;

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
            try
            {
                var sql = cmd.CommandText;
                var ps = cmd.Parameters;
                if (ps != null && ps.Count > 0)
                {
                    var sb = Pool.StringBuilder.Get();
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
                        else if (v is String str && str.Length > 64)
                            sv = String.Format("[{0}]{1}...", str.Length, str.Substring(0, 64));
                        else
                            sv = "{0}".F(v);
                        sb.AppendFormat("{0}={1}", ps[i].ParameterName, sv);
                    }
                    sb.Append("]");
                    sql = sb.Put(true);
                }

                // 阶段超长字符串
                if (sql.Length > 1024) sql = sql.Substring(0, 512) + "..." + sql.Substring(sql.Length - 512);

                return sql;
            }
            catch { return null; }
        }

        public String WriteSQL(DbCommand cmd)
        {
            var flag = ShowSQL;
#if !__CORE__
            // 如果页面设定有XCode_SQLList列表，则往列表写入SQL语句
            var context = HttpContext.Current;
            if (context != null)
            {
                if (context.Items["XCode_SQLList"] is List<String> list) flag = true;
            }
#endif

            if (!flag) return null;

            var sql = GetSql(cmd);

            WriteSQL(sql);

            return sql;
        }

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

        protected void EndTrace(DbCommand cmd, String sql = null)
        {
            if (_swSql == null) return;

            _swSql.Stop();

            if (_swSql.ElapsedMilliseconds < (Database as DbBase).TraceSQLTime) return;

            if (sql.IsNullOrEmpty()) sql = GetSql(cmd);
            if (sql.IsNullOrEmpty()) return;

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