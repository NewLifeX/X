using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NewLife;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Reflection;
using XCode.Exceptions;

namespace XCode.DataAccessLayer
{
    /// <summary>数据库会话基类</summary>
    internal abstract partial class DbSession : DisposeBase, IDbSession
    {
        #region 构造函数
        protected DbSession(IDatabase db)
        {
            Database = db;
            ShowSQL = db.ShowSQL;
        }

        /// <summary>销毁资源时，回滚未提交事务，并关闭数据库连接</summary>
        /// <param name="disposing"></param>
        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            Transaction.TryDispose();
        }
        #endregion

        #region 属性
        /// <summary>数据库</summary>
        public IDatabase Database { get; }
        #endregion

        #region 打开/关闭
        /// <summary>当异常发生时触发。关闭数据库连接，或者返还连接到连接池。</summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        protected virtual XDbException OnException(Exception ex)
        {
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
            if (sql.IsNullOrEmpty()) sql = GetSql(cmd);
            if (ex != null)
                return new XSqlException(sql, this, ex);
            else
                return new XSqlException(sql, this);
        }

        /// <summary>打开连接并执行操作</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="callback"></param>
        /// <returns></returns>
        public virtual TResult Process<TResult>(Func<DbConnection, TResult> callback)
        {
            var delay = 1000;
            var retry = Database.RetryOnFailure;
            for (var i = 0; i <= retry; i++)
            {
                try
                {
                    using var conn = Database.OpenConnection();
                    return callback(conn);
                }
                catch (Exception ex)
                {
                    // 如果重试次数用完，或者不应该在该异常上重试，则直接向上抛出异常
                    if (i == retry || !ShouldRetryOn(ex)) throw;

                    if (XTrace.Log.Level <= LogLevel.Debug) WriteLog("retry {0}，delay {1}", i + 1, delay);
                    Thread.Sleep(delay);

                    // 间隔时间倍增，最大30秒
                    delay *= 2;
                    if (delay > 30_000) delay = 30_000;
                }
            }

            return default;
        }

        /// <summary>打开连接并执行操作</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="callback"></param>
        /// <returns></returns>
        public virtual async Task<TResult> ProcessAsync<TResult>(Func<DbConnection, Task<TResult>> callback)
        {
            var delay = 1000;
            var retry = Database.RetryOnFailure;
            for (var i = 0; i <= retry; i++)
            {
                try
                {
                    using var conn = await Database.OpenConnectionAsync();
                    return await callback(conn);
                }
                catch (Exception ex)
                {
                    // 如果重试次数用完，或者不应该在该异常上重试，则直接向上抛出异常
                    if (i == retry || !ShouldRetryOn(ex)) throw;

                    if (XTrace.Log.Level <= LogLevel.Debug) WriteLog("retry {0}，delay {1}", i + 1, delay);
                    Thread.Sleep(delay);

                    // 间隔时间倍增，最大30秒
                    delay *= 2;
                    if (delay > 30_000) delay = 30_000;
                }
            }

            return default;
        }

        /// <summary>是否应该在该异常上重试</summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        protected virtual Boolean ShouldRetryOn(Exception ex)
        {
            if (ex == null) return false;

            // 基础异常
            if (ex is TimeoutException) return true;
            if (ex is SocketException sex)
            {
                switch (sex.SocketErrorCode)
                {
                    case SocketError.ConnectionAborted:
                    case SocketError.ConnectionReset:
                    case SocketError.ConnectionRefused:
                        return true;
                }
            }

            // 叠加异常
            if (ex is AggregateException agg)
            {
                foreach (var item in agg.InnerExceptions)
                {
                    if (ShouldRetryOn(item)) return true;
                }
            }

            // 内部异常
            var inner = ex.InnerException;
            if (inner != null && ShouldRetryOn(inner)) return true;

            return false;
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
            using var cmd = OnCreateCommand(sql, type, ps);
            return Query(cmd);
        }

        /// <summary>执行DbCommand，返回记录集</summary>
        /// <param name="cmd">DbCommand</param>
        /// <returns></returns>
        public virtual DataSet Query(DbCommand cmd)
        {
            return Execute(cmd, true, cmd2 =>
            {
                using var da = Database.Factory.CreateDataAdapter();
                da.SelectCommand = cmd2;

                var ds = new DataSet();
                da.Fill(ds);

                return ds;
            });
        }

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public virtual DbTable Query(String sql, IDataParameter[] ps)
        {
            using var cmd = OnCreateCommand(sql, CommandType.Text, ps);
            return Execute(cmd, true, cmd2 =>
            {
                using var dr = cmd2.ExecuteReader();
                return OnFill(dr);
            });
        }

        protected virtual DbTable OnFill(DbDataReader dr)
        {
            var dt = new DbTable();
            dt.Read(dr);
            return dt;
        }

        private static readonly Regex reg_QueryCount = new(@"^\s*select\s+\*\s+from\s+([\w\W]+)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        /// <summary>执行SQL查询，返回总记录数</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public virtual Int64 QueryCount(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            if (sql.Contains(" "))
            {
                _ = DbBase.CheckOrderClause(ref sql);
                var ms = reg_QueryCount.Matches(sql);
                if (ms != null && ms.Count > 0)
                    sql = $"Select Count(*) From {ms[0].Groups[1].Value}";
                else
                    sql = $"Select Count(*) From {DbBase.CheckSimpleSQL(sql)}";
            }
            else
                sql = $"Select Count(*) From {Database.FormatName(sql)}";

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
            using var cmd = OnCreateCommand(sql, type, ps);
            return Execute(cmd, false, cmd2 => cmd2.ExecuteNonQuery());
        }

        /// <summary>执行DbCommand，返回受影响的行数</summary>
        /// <param name="cmd">DbCommand</param>
        /// <returns></returns>
        public virtual Int32 Execute(DbCommand cmd) => Execute(cmd, false, cmd2 => cmd2.ExecuteNonQuery());

        public virtual T Execute<T>(DbCommand cmd, Boolean query, Func<DbCommand, T> callback)
        {
            Transaction?.Check(cmd, !query);

            var text = WriteSQL(cmd);

            return Process(conn =>
            {
                try
                {
                    if (cmd.Connection == null) cmd.Connection = conn;

                    BeginTrace();
                    return callback(cmd);
                }
                catch (DbException ex)
                {
                    throw OnException(ex, cmd, text);
                }
                finally
                {
                    EndTrace(cmd, text);
                }
            });
        }

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>新增行的自动编号</returns>
        public virtual Int64 InsertAndGetIdentity(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            using var cmd = OnCreateCommand(sql, type, ps);

            return Execute(cmd, false, cmd2 =>
            {
                var rs = cmd.ExecuteScalar();
                if (rs == null || rs == DBNull.Value) return 0;

                return Reflect.ChangeType<Int64>(rs);
            });
        }

        /// <summary>执行SQL语句，返回结果中的第一行第一列</summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public virtual T ExecuteScalar<T>(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            using var cmd = OnCreateCommand(sql, type, ps);
            return Execute(cmd, true, cmd2 =>
            {
                var rs = cmd.ExecuteScalar();
                if (rs == null || rs == DBNull.Value) return default;
                if (rs is T t) return t;

                return (T)Reflect.ChangeType(rs, typeof(T));
            });
        }

        /// <summary>获取一个DbCommand。</summary>
        /// <remark>
        /// 配置了连接，并关联了事务。
        /// 连接已打开。
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
            var cmd = Database.Factory?.CreateCommand();
            if (cmd == null) return null;

            cmd.CommandType = type;
            cmd.CommandText = sql;
            if (ps != null && ps.Length > 0) cmd.Parameters.AddRange(ps);

            var timeout = Database.CommandTimeout;
            if (timeout <= 0) timeout = Setting.Current.CommandTimeout;
            if (timeout > 0) cmd.CommandTimeout = timeout;

            return cmd;
        }
        #endregion

        #region 异步操作
        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public virtual Task<DbTable> QueryAsync(String sql, IDataParameter[] ps)
        {
            using var cmd = OnCreateCommand(sql, CommandType.Text, ps);
            return ExecuteAsync(cmd, true, async cmd2 =>
            {
                using var dr = await cmd2.ExecuteReaderAsync();
                var dt = new DbTable();
                await dt.ReadAsync(dr);
                return dt;
            });
        }

        /// <summary>执行SQL查询，返回总记录数</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public virtual Task<Int64> QueryCountAsync(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            if (sql.Contains(" "))
            {
                _ = DbBase.CheckOrderClause(ref sql);
                var ms = reg_QueryCount.Matches(sql);
                if (ms != null && ms.Count > 0)
                    sql = $"Select Count(*) From {ms[0].Groups[1].Value}";
                else
                    sql = $"Select Count(*) From {DbBase.CheckSimpleSQL(sql)}";
            }
            else
                sql = $"Select Count(*) From {Database.FormatName(sql)}";

            return ExecuteScalarAsync<Int64>(sql, type, ps);
        }

        /// <summary>执行SQL查询，返回总记录数</summary>
        /// <param name="builder">查询生成器</param>
        /// <returns>总记录数</returns>
        public virtual Task<Int64> QueryCountAsync(SelectBuilder builder) => ExecuteScalarAsync<Int64>(builder.SelectCount().ToString(), CommandType.Text, builder.Parameters.ToArray());

        /// <summary>快速查询单表记录数，稍有偏差</summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public virtual Task<Int64> QueryCountFastAsync(String tableName) => QueryCountAsync(tableName);

        /// <summary>执行SQL语句，返回受影响的行数</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public virtual Task<Int32> ExecuteAsync(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            using var cmd = OnCreateCommand(sql, type, ps);
            return ExecuteAsync(cmd, false, cmd2 => cmd2.ExecuteNonQueryAsync());
        }

        /// <summary>执行DbCommand，返回受影响的行数</summary>
        /// <param name="cmd">DbCommand</param>
        /// <returns></returns>
        public virtual Task<Int32> ExecuteAsync(DbCommand cmd) => ExecuteAsync(cmd, false, cmd2 => cmd2.ExecuteNonQueryAsync());

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>新增行的自动编号</returns>
        public virtual Task<Int64> InsertAndGetIdentityAsync(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            using var cmd = OnCreateCommand(sql, type, ps);

            return ExecuteAsync(cmd, false, async cmd2 =>
            {
                var rs = await cmd.ExecuteScalarAsync();
                if (rs == null || rs == DBNull.Value) return 0;

                return Reflect.ChangeType<Int64>(rs);
            });
        }

        /// <summary>执行SQL语句，返回结果中的第一行第一列</summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public virtual Task<T> ExecuteScalarAsync<T>(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            using var cmd = OnCreateCommand(sql, type, ps);
            return ExecuteAsync(cmd, true, async cmd2 =>
            {
                var rs = await cmd.ExecuteScalarAsync();
                if (rs == null || rs == DBNull.Value) return default;
                if (rs is T t) return t;

                return (T)Reflect.ChangeType(rs, typeof(T));
            });
        }

        public virtual Task<T> ExecuteAsync<T>(DbCommand cmd, Boolean query, Func<DbCommand, Task<T>> callback)
        {
            Transaction?.Check(cmd, !query);

            var text = WriteSQL(cmd);

            return ProcessAsync(conn =>
            {
                try
                {
                    if (cmd.Connection == null) cmd.Connection = conn;

                    BeginTrace();
                    return callback(cmd);
                }
                catch (DbException ex)
                {
                    throw OnException(ex, cmd, text);
                }
                finally
                {
                    EndTrace(cmd, text);
                }
            });
        }
        #endregion

        #region 批量操作
        /// <summary>批量插入</summary>
        /// <param name="table">数据表</param>
        /// <param name="columns">要插入的字段，默认所有字段</param>
        /// <param name="list">实体列表</param>
        /// <returns></returns>
        public virtual Int32 Insert(IDataTable table, IDataColumn[] columns, IEnumerable<IExtend> list) => throw new NotSupportedException();

        /// <summary>批量忽略插入</summary>
        /// <param name="table">数据表</param>
        /// <param name="columns">要插入的字段，默认所有字段</param>
        /// <param name="list">实体列表</param>
        /// <returns></returns>
        public virtual Int32 InsertIgnore(IDataTable table, IDataColumn[] columns, IEnumerable<IExtend> list) => throw new NotSupportedException();

        /// <summary>批量替换</summary>
        /// <param name="table">数据表</param>
        /// <param name="columns">要插入的字段，默认所有字段</param>
        /// <param name="list">实体列表</param>
        /// <returns></returns>
        public virtual Int32 Replace(IDataTable table, IDataColumn[] columns, IEnumerable<IExtend> list) => throw new NotSupportedException();

        /// <summary>批量更新</summary>
        /// <param name="table">数据表</param>
        /// <param name="columns">要更新的字段，默认所有字段</param>
        /// <param name="updateColumns">要更新的字段，默认脏数据</param>
        /// <param name="addColumns">要累加更新的字段，默认累加</param>
        /// <param name="list">实体列表</param>
        /// <returns></returns>
        public virtual Int32 Update(IDataTable table, IDataColumn[] columns, ICollection<String> updateColumns, ICollection<String> addColumns, IEnumerable<IExtend> list) => throw new NotSupportedException();

        /// <summary>批量插入或更新</summary>
        /// <param name="table">数据表</param>
        /// <param name="columns">要插入的字段，默认所有字段</param>
        /// <param name="updateColumns">主键已存在时，要更新的字段</param>
        /// <param name="addColumns">主键已存在时，要累加更新的字段</param>
        /// <param name="list">实体列表</param>
        /// <returns></returns>
        public virtual Int32 Upsert(IDataTable table, IDataColumn[] columns, ICollection<String> updateColumns, ICollection<String> addColumns, IEnumerable<IExtend> list) => throw new NotSupportedException();
        #endregion

        #region 高级
        /// <summary>清空数据表，标识归零</summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public virtual Int32 Truncate(String tableName) => Execute($"Truncate Table {Database.FormatName(tableName)}");
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
            var dt = db._SchemaCache.Get<DataTable>(key);
            if (dt == null)
            {
                /*
                * TODO: Bug
                * sqlserver切换到master库时,仍然使用Process去获取DbConnection，然而此时DataBase对象为连接字符串中的数据库
                * 这里不知道是应该在RemoteDb的OpenDatabase方法（改变DataBase对象）抑或是修改这里的Process方法
                */
                if (conn != null)
                    dt = GetSchemaInternal(conn, key, collectionName, restrictionValues);
                else
                    dt = Process(conn2 => GetSchemaInternal(conn2, key, collectionName, restrictionValues));

                db._SchemaCache.Set(key, dt, 10);
            }

            return dt;
        }

        private DataTable GetSchemaInternal(DbConnection conn, String key, String collectionName, String[] restrictionValues)
        {
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

        private static ILog logger;

        /// <summary>写入SQL到文本中</summary>
        /// <param name="sql"></param>
        public void WriteSQL(String sql)
        {
            if (sql.IsNullOrEmpty()) return;

            // 如果页面设定有XCode_SQLList列表，则往列表写入SQL语句
            DAL.LocalFilter?.Invoke(sql);

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
            var max = (Database as DbBase).SQLMaxLength;
            try
            {
                var sql = cmd.CommandText;

                // 诊断信息
                /*if (XTrace.Log.Level <= LogLevel.Debug)*/
                sql = $"[{Database.ConnName}] {sql}";

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
                                sv = $"[{bv.Length}]0x{BitConverter.ToString(bv, 0, 8)}...";
                            else
                                sv = $"[{bv.Length}]0x{BitConverter.ToString(bv)}";
                        }
                        else if (v is String str && str.Length > 64)
                            sv = $"[{str.Length}]{str[..64]}...";
                        else
                            sv = v is DateTime dt ? dt.ToFullString() : (v + "");
                        sb.AppendFormat("{0}={1}", ps[i].ParameterName, sv);
                    }
                    sb.Append(']');
                    sql = sb.Put(true);
                }

                // 截断超长字符串
                if (max > 0)
                {
                    if (sql.Length > max && sql.StartsWithIgnoreCase("Insert")) sql = sql[..(max / 2)] + "..." + sql[^(max / 2)..];
                }

                return sql;
            }
            catch { return null; }
        }

        public String WriteSQL(DbCommand cmd)
        {
            // 如果页面设定有XCode_SQLList列表，则往列表写入SQL语句
            if (!ShowSQL && DAL.LocalFilter == null) return null;

            var sql = GetSql(cmd);

            WriteSQL(sql);

            return sql;
        }

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLog(String format, params Object[] args) => XTrace.WriteLine(format, args);
        #endregion

        #region SQL时间跟踪
        private Stopwatch _swSql;
        private static readonly HashSet<String> _trace_sqls = new(StringComparer.OrdinalIgnoreCase);

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

            XTrace.WriteLine("SQL耗时较长，建议优化 {0:n0}毫秒 {1}", _swSql.ElapsedMilliseconds, sql);
        }
        #endregion
    }
}