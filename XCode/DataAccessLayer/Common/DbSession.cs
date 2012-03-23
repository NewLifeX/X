using System;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using NewLife;
using NewLife.Log;
using XCode.Exceptions;

namespace XCode.DataAccessLayer
{
    /// <summary>数据库会话基类</summary>
    abstract partial class DbSession : DisposeBase, IDbSession
    {
        #region 构造函数
        /// <summary>
        /// 销毁资源时，回滚未提交事务，并关闭数据库连接
        /// </summary>
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

        /// <summary>
        /// 返回数据库类型。外部DAL数据库类请使用Other
        /// </summary>
        private DatabaseType DbType { get { return Database.DbType; } }

        /// <summary>工厂</summary>
        private DbProviderFactory Factory { get { return Database.Factory; } }

        private String _ConnectionString;
        /// <summary>链接字符串，会话单独保存，允许修改，修改不会影响数据库中的连接字符串</summary>
        public String ConnectionString
        {
            get { return _ConnectionString; }
            set { _ConnectionString = value; }
        }

        private DbConnection _Conn;
        /// <summary>
        /// 数据连接对象。
        /// </summary>
        public DbConnection Conn
        {
            get
            {
                if (_Conn == null)
                {
                    _Conn = Factory.CreateConnection();
                    //_Conn.ConnectionString = Database.ConnectionString;
                    _Conn.ConnectionString = ConnectionString;
                }
                return _Conn;
            }
            //set { _Conn = value; }
        }

        private Int32 _QueryTimes;
        /// <summary>
        /// 查询次数
        /// </summary>
        public Int32 QueryTimes
        {
            get { return _QueryTimes; }
            set { _QueryTimes = value; }
        }

        private Int32 _ExecuteTimes;
        /// <summary>
        /// 执行次数
        /// </summary>
        public Int32 ExecuteTimes
        {
            get { return _ExecuteTimes; }
            set { _ExecuteTimes = value; }
        }

        private Int32 _ThreadID = Thread.CurrentThread.ManagedThreadId;
        /// <summary>线程编号，每个数据库会话应该只属于一个线程，该属性用于检查错误的跨线程操作</summary>
        public Int32 ThreadID
        {
            get { return _ThreadID; }
            set { _ThreadID = value; }
        }
        #endregion

        #region 打开/关闭
        private Boolean _IsAutoClose = true;
        /// <summary>
        /// 是否自动关闭。
        /// 启用事务后，该设置无效。
        /// 在提交或回滚事务时，如果IsAutoClose为true，则会自动关闭
        /// </summary>
        public Boolean IsAutoClose
        {
            get { return _IsAutoClose; }
            set { _IsAutoClose = value; }
        }

        /// <summary>
        /// 连接是否已经打开
        /// </summary>
        public Boolean Opened
        {
            get { return _Conn != null && _Conn.State != ConnectionState.Closed; }
        }

        /// <summary>
        /// 打开
        /// </summary>
        public virtual void Open()
        {
            if (DAL.Debug && ThreadID != Thread.CurrentThread.ManagedThreadId) DAL.WriteLog("本会话由线程{0}创建，当前线程{1}非法使用该会话！");

            if (Conn != null && Conn.State == ConnectionState.Closed) Conn.Open();
        }

        /// <summary>
        /// 关闭
        /// </summary>
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

        /// <summary>
        /// 自动关闭。
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
                if (Opened)
                {
                    //如果已打开，则调用链接来切换
                    Conn.ChangeDatabase(value);
                }
                else
                {
                    //DAL.WriteDebugLog("{0}=>{1}", Conn.Database, value);
                    ////XTrace.DebugStack(3);

                    //如果没有打开，则改变链接字符串
                    DbConnectionStringBuilder builder = new DbConnectionStringBuilder();
                    builder.ConnectionString = ConnectionString;
                    if (builder.ContainsKey("Database"))
                    {
                        builder["Database"] = value;
                        ConnectionString = builder.ToString();
                        Conn.ConnectionString = ConnectionString;
                    }
                    else if (builder.ContainsKey("Initial Catalog"))
                    {
                        builder["Initial Catalog"] = value;
                        ConnectionString = builder.ToString();
                        Conn.ConnectionString = ConnectionString;
                    }
                }
            }
        }

        /// <summary>
        /// 当异常发生时触发。关闭数据库连接，或者返还连接到连接池。
        /// </summary>
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

        /// <summary>
        /// 当异常发生时触发。关闭数据库连接，或者返还连接到连接池。
        /// </summary>
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
        /// <summary>
        /// 数据库事务
        /// </summary>
        protected DbTransaction Trans
        {
            get { return _Trans; }
            set { _Trans = value; }
        }

        /// <summary>
        /// 事务计数。
        /// 当且仅当事务计数等于1时，才提交或回滚。
        /// </summary>
        private Int32 TransactionCount = 0;

        /// <summary>
        /// 开始事务
        /// </summary>
        /// <returns></returns>
        public Int32 BeginTransaction()
        {
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

        /// <summary>
        /// 提交事务
        /// </summary>
        public Int32 Commit()
        {
            TransactionCount--;
            if (TransactionCount > 0) return TransactionCount;

            if (Trans == null) throw new XDbSessionException(this, "当前并未开始事务，请用BeginTransaction方法开始新事务！");
            try
            {
                Trans.Commit();
                Trans = null;
                if (IsAutoClose) Close();
            }
            catch (DbException ex)
            {
                throw OnException(ex);
            }

            return TransactionCount;
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        public Int32 Rollback()
        {
            // 这里要小心，在多层事务中，如果内层回滚，而最外层提交，则内层的回滚会变成提交
            TransactionCount--;
            if (TransactionCount > 0) return TransactionCount;

            if (Trans == null) throw new XDbSessionException(this, "当前并未开始事务，请用BeginTransaction方法开始新事务！");
            try
            {
                Trans.Rollback();
                Trans = null;
                if (IsAutoClose) Close();
            }
            catch (DbException ex)
            {
                throw OnException(ex);
            }

            return TransactionCount;
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

            //QueryTimes++;
            //WriteSQL(sql, ps);
            //try
            //{
            //    DbCommand cmd = CreateCommand(sql, type, ps);
            //    using (DbDataAdapter da = Factory.CreateDataAdapter())
            //    {
            //        da.SelectCommand = cmd;
            //        DataSet ds = new DataSet();
            //        da.Fill(ds);
            //        return ds;
            //    }
            //}
            //catch (DbException ex)
            //{
            //    throw OnException(ex, sql);
            //}
            //finally
            //{
            //    AutoClose();
            //}
        }

        ///// <summary>
        ///// 执行SQL查询，返回记录集
        ///// </summary>
        ///// <param name="builder">查询生成器</param>
        ///// <param name="startRowIndex">开始行，0表示第一行</param>
        ///// <param name="maximumRows">最大返回行数，0表示所有行</param>
        ///// <returns>记录集</returns>
        //public virtual DataSet Query(SelectBuilder builder, Int32 startRowIndex, Int32 maximumRows)
        //{
        //    return Query(Database.PageSplit(builder, startRowIndex, maximumRows).ToString(), CommandType.Text, builder.Parameters.ToArray());
        //}

        /// <summary>
        /// 执行DbCommand，返回记录集
        /// </summary>
        /// <param name="cmd">DbCommand</param>
        /// <returns></returns>
        public virtual DataSet Query(DbCommand cmd)
        {
            QueryTimes++;
            WriteSQL(cmd);
            using (DbDataAdapter da = Factory.CreateDataAdapter())
            {
                try
                {
                    if (!Opened) Open();
                    cmd.Connection = Conn;
                    if (Trans != null) cmd.Transaction = Trans;
                    da.SelectCommand = cmd;
                    DataSet ds = new DataSet();
                    da.Fill(ds);
                    return ds;
                }
                catch (DbException ex)
                {
                    throw OnException(ex, cmd.CommandText);
                }
                finally
                {
                    AutoClose();
                }
            }
        }

        private static Regex reg_QueryCount = new Regex(@"^\s*select\s+\*\s+from\s+([\w\W]+)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        /// <summary>
        /// 执行SQL查询，返回总记录数
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public virtual Int64 QueryCount(String sql, CommandType type = CommandType.Text, params DbParameter[] ps)
        {
            if (sql.Contains(" "))
            {
                String orderBy = DbBase.CheckOrderClause(ref sql);
                //sql = String.Format("Select Count(*) From {0}", CheckSimpleSQL(sql));
                //Match m = reg_QueryCount.Match(sql);
                MatchCollection ms = reg_QueryCount.Matches(sql);
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

        /// <summary>
        /// 执行SQL查询，返回总记录数
        /// </summary>
        /// <param name="builder">查询生成器</param>
        /// <returns>总记录数</returns>
        public virtual Int64 QueryCount(SelectBuilder builder)
        {
            return ExecuteScalar<Int64>(builder.SelectCount().ToString(), CommandType.Text, builder.Parameters.ToArray());
        }

        /// <summary>
        /// 快速查询单表记录数，稍有偏差
        /// </summary>
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

            //ExecuteTimes++;
            //WriteSQL(sql, ps);
            //try
            //{
            //    DbCommand cmd = CreateCommand();
            //    cmd.CommandType = type;
            //    cmd.CommandText = sql;
            //    if (ps != null && ps.Length > 0) cmd.Parameters.AddRange(ps);
            //    return cmd.ExecuteNonQuery();
            //}
            //catch (DbException ex)
            //{
            //    throw OnException(ex, sql);
            //}
            //finally { AutoClose(); }
        }

        /// <summary>
        /// 执行DbCommand，返回受影响的行数
        /// </summary>
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
                return cmd.ExecuteNonQuery();
            }
            catch (DbException ex)
            {
                throw OnException(ex, cmd.CommandText);
            }
            finally { AutoClose(); }
        }

        /// <summary>
        /// 执行插入语句并返回新增行的自动编号
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>新增行的自动编号</returns>
        public virtual Int64 InsertAndGetIdentity(String sql, CommandType type = CommandType.Text, params DbParameter[] ps)
        {
            return Execute(sql, type, ps);
        }

        /// <summary>
        /// 执行SQL语句，返回结果中的第一行第一列
        /// </summary>
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
                AutoClose();
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

        #region 架构
        /// <summary>
        /// 返回数据源的架构信息
        /// </summary>
        /// <param name="collectionName">指定要返回的架构的名称。</param>
        /// <param name="restrictionValues">为请求的架构指定一组限制值。</param>
        /// <returns></returns>
        public virtual DataTable GetSchema(string collectionName, string[] restrictionValues)
        {
            QueryTimes++;
            if (!Opened) Open();

            DbConnection conn = Conn;
            try
            {
                // 如果启用了事务保护，这里要新开一个连接，否则MSSQL里面报错，SQLite不报错，其它数据库未测试
                if (TransactionCount > 0)
                {
                    conn = Factory.CreateConnection();
                    conn.ConnectionString = ConnectionString;
                    conn.Open();
                }

                DataTable dt;

                if (restrictionValues == null || restrictionValues.Length < 1)
                {
                    if (String.IsNullOrEmpty(collectionName))
                    {
                        WriteSQL("GetSchema");
                        dt = conn.GetSchema();
                    }
                    else
                    {
                        WriteSQL("GetSchema(\"" + collectionName + "\")");
                        dt = conn.GetSchema(collectionName);
                    }
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (String item in restrictionValues)
                    {
                        sb.Append(", ");
                        if (item == null)
                            sb.Append("null");
                        else
                            sb.AppendFormat("\"{0}\"", item);
                    }
                    WriteSQL("GetSchema(\"" + collectionName + "\"" + sb + ")");
                    dt = conn.GetSchema(collectionName, restrictionValues);
                }

                return dt;
            }
            catch (DbException ex)
            {
                throw new XDbSessionException(this, "取得所有表构架出错！", ex);
            }
            finally
            {
                AutoClose();
                if (conn != Conn) conn.Close();
            }
        }
        #endregion

        #region Sql日志输出
        [ThreadStatic]
        private static Boolean? _ShowSQL;
        /// <summary>
        /// 是否输出SQL语句，默认为XCode调试开关XCode.Debug
        /// </summary>
        public static Boolean ShowSQL
        {
            get
            {
                if (_ShowSQL == null) return DAL.ShowSQL;
                return _ShowSQL.Value;
            }
            set { _ShowSQL = value; }
        }

        static TextFileLog logger;

        /// <summary>
        /// 写入SQL到文本中
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="ps"></param>
        public static void WriteSQL(String sql, params DbParameter[] ps)
        {
            if (!ShowSQL) return;

            if (ps != null && ps.Length > 0)
            {
                StringBuilder sb = new StringBuilder();
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
                        if (sv.Length > 8) sv = String.Format("[{0}]{1}...", sv.Length, sv.Substring(0, 8));
                    }
                    else
                        sv = "" + v;
                    sb.AppendFormat("{1}:{0}={2}", ps[i].ParameterName, ps[i].DbType, sv);
                }
                sb.Append("]");
                sql = sb.ToString();
            }

            if (String.IsNullOrEmpty(DAL.SQLPath))
                WriteLog(sql);
            else
            {
                if (logger == null) logger = TextFileLog.Create(DAL.SQLPath);
                logger.WriteLine(sql);
            }
        }

        public static void WriteSQL(DbCommand cmd)
        {
            String sql = cmd.CommandText;
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

        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="msg"></param>
        public static void WriteLog(String msg) { DAL.WriteLog(msg); }

        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLog(String format, params Object[] args) { DAL.WriteLog(format, args); }
        #endregion
    }
}