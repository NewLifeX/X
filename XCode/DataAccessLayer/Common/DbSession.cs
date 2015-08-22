using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using NewLife;
using NewLife.Collections;
using NewLife.Log;
using XCode.Exceptions;

namespace XCode.DataAccessLayer
{
    /// <summary>数据库会话基类</summary>
    abstract partial class DbSession : DisposeBase, IDbSession
    {
        #region 构造函数
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
                if (_Trans != null && Opened) _Trans.Rollback();
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
        private IDatabase _Database;
        /// <summary>数据库</summary>
        public IDatabase Database { get { return _Database; } set { _Database = value; } }

        /// <summary>返回数据库类型。外部DAL数据库类请使用Other</summary>
        private DatabaseType DbType { get { return Database.DbType; } }

        /// <summary>工厂</summary>
        private DbProviderFactory Factory { get { return Database.Factory; } }

        private String _ConnectionString;
        /// <summary>链接字符串，会话单独保存，允许修改，修改不会影响数据库中的连接字符串</summary>
        public String ConnectionString { get { return _ConnectionString; } set { _ConnectionString = value; } }

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

        private Int32 _QueryTimes;
        /// <summary>查询次数</summary>
        public Int32 QueryTimes { get { return _QueryTimes; } set { _QueryTimes = value; } }

        private Int32 _ExecuteTimes;
        /// <summary>执行次数</summary>
        public Int32 ExecuteTimes { get { return _ExecuteTimes; } set { _ExecuteTimes = value; } }

        private Int32 _ThreadID = Thread.CurrentThread.ManagedThreadId;
        /// <summary>线程编号，每个数据库会话应该只属于一个线程，该属性用于检查错误的跨线程操作</summary>
        public Int32 ThreadID { get { return _ThreadID; } set { _ThreadID = value; } }
        #endregion

        #region 打开/关闭
        private Boolean _IsAutoClose = true;
        /// <summary>是否自动关闭。
        /// 启用事务后，该设置无效。
        /// 在提交或回滚事务时，如果IsAutoClose为true，则会自动关闭
        /// </summary>
        public Boolean IsAutoClose { get { return _IsAutoClose; } set { _IsAutoClose = value; } }

        /// <summary>连接是否已经打开</summary>
        public Boolean Opened { get { return _Conn != null && _Conn.State != ConnectionState.Closed; } }

        /// <summary>打开</summary>
        public virtual void Open()
        {
            if (DAL.Debug && ThreadID != Thread.CurrentThread.ManagedThreadId) DAL.WriteLog("本会话由线程{0}创建，当前线程{1}非法使用该会话！");

            if (Conn != null && Conn.State == ConnectionState.Closed)
            {
#if DEBUG
                try
                {
                    Conn.Open();
                }
                catch (DbException)
                {
                    DAL.WriteLog("导致Open错误的连接字符串：{0}", Conn.ConnectionString);
                    throw;
                }
#else
                Conn.Open();
#endif
            }
        }

        /// <summary>关闭</summary>
        public virtual void Close()
        {
            if (_Conn != null && Conn.State != ConnectionState.Closed)
            {
                try { Conn.Close(); }
                catch (Exception ex)
                {
                    WriteLog("执行" + DbType.ToString() + "的Close时出错：" + ex.ToString());
                }
            }
        }

        /// <summary>自动关闭。
        /// 启用事务后，不关闭连接。
        /// 在提交或回滚事务时，如果IsAutoClose为true，则会自动关闭
        /// </summary>
        public void AutoClose()
        {
            if (IsAutoClose && Trans == null && Opened) Close();
        }

        /// <summary>数据库名</summary>
        public String DatabaseName
        {
            get
            {
                return Conn == null ? null : Conn.Database;
            }
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
        private DbTransaction _Trans;
        /// <summary>数据库事务</summary>
        protected DbTransaction Trans
        {
            get { return _Trans; }
            set { _Trans = value; }
        }

        /// <summary>事务计数。当且仅当事务计数等于1时，才提交或回滚。</summary>
        private Int32 TransactionCount = 0;
        /// <summary>脏实体会话</summary>
        private Dictionary<String, DirtiedEntitySession> _EntitySession = new Dictionary<String, DirtiedEntitySession>();

        /// <summary>开始事务</summary>
        /// <returns>剩下的事务计数</returns>
        public virtual Int32 BeginTransaction()
        {
            if (Disposed) throw new ObjectDisposedException(this.GetType().Name);

            if (TransactionCount <= 0)
            {
                TransactionCount = 0;
                _EntitySession.Clear();
            }
            TransactionCount++;
            if (TransactionCount > 1) return TransactionCount;

            try
            {
                if (!Opened) Open();
                Trans = Conn.BeginTransaction();
                TransactionCount = 1;
                return TransactionCount;
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
            TransactionCount--;
            if (TransactionCount > 0) return TransactionCount;

            if (Trans == null) throw new XDbSessionException(this, "当前并未开始事务，请用BeginTransaction方法开始新事务！");
            try
            {
                if (Trans.Connection != null)
                {
                    Trans.Commit();

                    foreach (var item in _EntitySession)
                    {
                        var dirtiedSession = item.Value;
                        if (dirtiedSession.ExecuteCount > 0)
                        {
                            dirtiedSession.Session.RaiseCommitDataChange(dirtiedSession.UpdateCount, dirtiedSession.DirectExecuteSQLCount);
                        }
                    }
                }
            }
            catch (DbException ex)
            {
                throw OnException(ex);
            }
            finally
            {
                Trans = null;
                _EntitySession.Clear();
                if (IsAutoClose) { Close(); }
            }

            return TransactionCount;
        }

        /// <summary>回滚事务</summary>
        /// <param name="ignoreException">是否忽略异常</param>
        /// <returns>剩下的事务计数</returns>
        public Int32 Rollback(Boolean ignoreException = true)
        {
            // 这里要小心，在多层事务中，如果内层回滚，而最外层提交，则内层的回滚会变成提交
            TransactionCount--;
            if (TransactionCount > 0) return TransactionCount;

            var tr = Trans;
            if (tr == null) throw new XDbSessionException(this, "当前并未开始事务，请用BeginTransaction方法开始新事务！");
            Trans = null;
            try
            {
                if (tr.Connection != null)
                {
                    tr.Rollback();

                    foreach (var item in _EntitySession)
                    {
                        var dirtiedSession = item.Value;
                        if (dirtiedSession.ExecuteCount > 0)
                        {
                            dirtiedSession.Session.RaiseRoolbackDataChange(dirtiedSession.UpdateCount, dirtiedSession.DirectExecuteSQLCount);
                        }
                    }
                }
            }
            catch (DbException ex)
            {
                if (!ignoreException) throw OnException(ex);
            }
            finally
            {
                tr = null;
                _EntitySession.Clear();
                if (IsAutoClose) { Close(); }
            }

            return TransactionCount;
        }

        /// <summary>添加脏实体会话</summary>
        /// <param name="key">实体会话关键字</param>
        /// <param name="entitySession">事务嵌套处理中，事务真正提交或回滚之前，进行了子事务提交的实体会话</param>
        /// <param name="executeCount">实体操作次数</param>
        /// <param name="updateCount">实体更新操作次数</param>
        /// <param name="directExecuteSQLCount">直接执行SQL语句次数</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void AddDirtiedEntitySession(String key, IEntitySession entitySession, Int32 executeCount, Int32 updateCount, Int32 directExecuteSQLCount)
        {
            DirtiedEntitySession oldsession;
            if (_EntitySession.TryGetValue(key, out oldsession))
            {
                oldsession.ExecuteCount += executeCount;
                oldsession.UpdateCount += updateCount;
                oldsession.DirectExecuteSQLCount += directExecuteSQLCount;
            }
            else
            {
                _EntitySession.Add(key, new DirtiedEntitySession(entitySession, executeCount, updateCount, directExecuteSQLCount));
            }
        }

        /// <summary>移除脏实体会话</summary>
        /// <param name="key">实体会话关键字</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void RemoveDirtiedEntitySession(String key)
        {
            _EntitySession.Remove(key);
        }

        /// <summary>获取脏实体会话</summary>
        /// <param name="key">实体会话关键字</param>
        /// <param name="session">脏实体会话</param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Boolean TryGetDirtiedEntitySession(String key, out DirtiedEntitySession session)
        {
            return _EntitySession.TryGetValue(key, out session);
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
                    if (Trans != null) cmd.Transaction = Trans;
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
                //sql = String.Format("Select Count(*) From {0}", CheckSimpleSQL(sql));
                //Match m = reg_QueryCount.Match(sql);
                var ms = reg_QueryCount.Matches(sql);
                if (ms != null && ms.Count > 0)
                {
                    sql = String.Format("Select Count(*) From {0}", ms[0].Groups[1].Value);
                }
                else
                {
                    sql = String.Format("Select Count(*) From {0}", DbBase.CheckSimpleSQL(sql));
                }
            }
            else
                sql = String.Format("Select Count(*) From {0}", Database.FormatName(sql));

            //return QueryCountInternal(sql);
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
                if (Trans != null) cmd.Transaction = Trans;

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
                return (T)Convert.ChangeType(rs, typeof(T));
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

        /// <summary>
        /// 获取一个DbCommand。
        /// 配置了连接，并关联了事务。
        /// 连接已打开。
        /// 使用完毕后，必须调用AutoClose方法，以使得在非事务及设置了自动关闭的情况下关闭连接
        /// </summary>
        /// <returns></returns>
        public virtual DbCommand CreateCommand()
        {
            var cmd = Factory.CreateCommand();
            if (!Opened) Open();
            cmd.Connection = Conn;
            if (Trans != null) cmd.Transaction = Trans;

            return cmd;
        }

        /// <summary>
        /// 获取一个DbCommand。
        /// 配置了连接，并关联了事务。
        /// 连接已打开。
        /// 使用完毕后，必须调用AutoClose方法，以使得在非事务及设置了自动关闭的情况下关闭连接
        /// </summary>
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
            Expriod = 10,
            ClearExpriod = 10 * 60//,
            // 不能异步。否则，修改表结构后，第一次获取会是旧的
            //Asynchronous = true
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
            return _schCache.GetItem<String, String[]>(key, collectionName, restrictionValues, GetSchemaInternal);
        }

        DataTable GetSchemaInternal(String key, String collectionName, String[] restrictionValues)
        {
            QueryTimes++;
            // 如果启用了事务保护，这里要新开一个连接，否则MSSQL里面报错，SQLite不报错，其它数据库未测试
            var isTrans = TransactionCount > 0;

            DbConnection conn = null;
            if (isTrans)
            {
#if DEBUG
                try
                {
                    conn = Factory.CreateConnection();
                    checkConnStr();
                    conn.ConnectionString = ConnectionString;
                    conn.Open();
                }
                catch (DbException ex)
                {
                    DAL.WriteLog("导致GetSchema错误的连接字符串：{0}", conn.ConnectionString);
                    throw new XDbSessionException(this, "取得所有表构架出错！连接字符串有问题，请查看日志！", ex);
                }
#else
                conn = Factory.CreateConnection();
                checkConnStr();
                conn.ConnectionString = ConnectionString;
                conn.Open();
#endif
            }
            else
            {
                if (!Opened) Open();
                conn = Conn;
            }

            try
            {
                DataTable dt;

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
        private Boolean? _ShowSQL;
        /// <summary>是否输出SQL语句，默认为XCode调试开关XCode.Debug</summary>
        public Boolean ShowSQL
        {
            get
            {
                if (_ShowSQL == null) return Database.ShowSQL;
                return _ShowSQL.Value;
            }
            set
            {
                // 如果设定值跟Database.ShowSQL相同，则直接使用Database.ShowSQL
                if (value == Database.ShowSQL)
                    _ShowSQL = null;
                else
                    _ShowSQL = value;
            }
        }

        static TextFileLog logger;

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

            if (String.IsNullOrEmpty(DAL.SQLPath))
                WriteLog(sql);
            else
            {
                if (logger == null) logger = TextFileLog.Create(DAL.SQLPath);
                logger.WriteLine(sql);
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

            if (_trace_sqls.Contains(sql)) return;
            lock (_trace_sqls)
            {
                if (_trace_sqls.Contains(sql)) return;

                _trace_sqls.Add(sql);
            }

            XTrace.WriteLine("SQL耗时较长，建议优化 {0:n}毫秒 {1}", _swSql.ElapsedMilliseconds, sql);
        }
        #endregion
    }
}