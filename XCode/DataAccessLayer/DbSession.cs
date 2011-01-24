using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Threading;
using NewLife;
using NewLife.Log;
using XCode.Exceptions;
using NewLife.Configuration;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 泛型数据库会话基类
    /// </summary>
    /// <typeparam name="TDbSession"></typeparam>
    abstract class DbSession<TDbSession> : DbSession where TDbSession : DbSession<TDbSession>
    {
        #region 数据库
        #endregion
    }

    /// <summary>
    /// 数据库会话基类。
    /// </summary>
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
            catch (Exception ex)
            {
                WriteLog("执行" + DbType.ToString() + "的Dispose时出错：" + ex.ToString());
            }
        }
        #endregion

        #region 属性
        private IDatabase _Database;
        /// <summary>数据库</summary>
        public virtual IDatabase Database { get { return _Database; } set { _Database = value; } }

        /// <summary>
        /// 返回数据库类型。外部DAL数据库类请使用Other
        /// </summary>
        private DatabaseType DbType { get { return Database.DbType; } }

        /// <summary>工厂</summary>
        private DbProviderFactory Factory { get { return Database.Factory; } }

        private String _ConnectionString;
        /// <summary>链接字符串</summary>
        public String ConnectionString
        {
            get { return _ConnectionString; }
            set { _ConnectionString = value; }
        }

        private DbConnection _Conn;
        /// <summary>
        /// 数据连接对象。
        /// </summary>
        public virtual DbConnection Conn
        {
            get
            {
                if (_Conn == null)
                {
                    _Conn = Factory.CreateConnection();
                    _Conn.ConnectionString = Database.ConnectionString;
                }
                return _Conn;
            }
            set { _Conn = value; }
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

        ///// <summary>
        ///// 数据库服务器版本
        ///// </summary>
        //public String ServerVersion
        //{
        //    get
        //    {
        //        if (!Opened) Open();
        //        String ver = Conn.ServerVersion;
        //        AutoClose();
        //        return ver;
        //    }
        //}
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
                    //如果没有打开，则改变链接字符串
                    DbConnectionStringBuilder builder = new DbConnectionStringBuilder();
                    builder.ConnectionString = ConnectionString;
                    builder["Database"] = value;
                    ConnectionString = builder.ToString();
                    Conn.ConnectionString = ConnectionString;
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
            //return new XException("内部数据库实体" + this.GetType().FullName + "异常，执行" + Environment.StackTrace + "方法出错！", ex);
            //String err = "内部数据库实体" + DbType.ToString() + "异常，执行方法出错！" + Environment.NewLine + ex.Message;
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
            //return new XException("内部数据库实体" + this.GetType().FullName + "异常，执行" + Environment.StackTrace + "方法出错！", ex);
            //String err = "内部数据库实体" + DbType.ToString() + "异常，执行方法出错！" + Environment.NewLine;
            //if (!String.IsNullOrEmpty(sql)) err += "SQL语句：" + sql + Environment.NewLine;
            //err += ex.Message;
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
            //if (Debug) WriteLog("开始事务：{0}", ID);

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
            //if (Debug) WriteLog("提交事务：{0}", ID);

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
            //if (Debug) WriteLog("回滚事务：{0}", ID);

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
        /// <summary>
        /// 执行SQL查询，返回记录集
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns></returns>
        public virtual DataSet Query(String sql)
        {
            QueryTimes++;
            if (Debug) WriteLog(sql);
            try
            {
                DbCommand cmd = PrepareCommand();
                cmd.CommandText = sql;
                using (DbDataAdapter da = Factory.CreateDataAdapter())
                {
                    da.SelectCommand = cmd;
                    DataSet ds = new DataSet();
                    da.Fill(ds);
                    return ds;
                }
            }
            catch (DbException ex)
            {
                throw OnException(ex, sql);
            }
            finally
            {
                AutoClose();
            }
        }

        /// <summary>
        /// 执行SQL查询，返回附加了主键等架构信息的记录集。性能稍差于普通查询
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns></returns>
        public virtual DataSet QueryWithKey(String sql)
        {
            QueryTimes++;
            if (Debug) WriteLog(sql);
            try
            {
                DbCommand cmd = PrepareCommand();
                cmd.CommandText = sql;
                using (DbDataAdapter da = Factory.CreateDataAdapter())
                {
                    da.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                    da.SelectCommand = cmd;
                    DataSet ds = new DataSet();
                    da.Fill(ds);
                    return ds;
                }
            }
            catch (DbException ex)
            {
                throw OnException(ex, sql);
            }
            finally
            {
                AutoClose();
            }
        }

        /// <summary>
        /// 执行SQL查询，返回记录集
        /// </summary>
        /// <param name="builder">查询生成器</param>
        /// <param name="startRowIndex">开始行，0开始</param>
        /// <param name="maximumRows">最大返回行数</param>
        /// <param name="keyColumn">唯一键。用于not in分页</param>
        /// <returns>记录集</returns>
        public virtual DataSet Query(SelectBuilder builder, Int32 startRowIndex, Int32 maximumRows, String keyColumn)
        {
            return Query(Database.PageSplit(builder, startRowIndex, maximumRows, keyColumn));
        }

        /// <summary>
        /// 执行DbCommand，返回记录集
        /// </summary>
        /// <param name="cmd">DbCommand</param>
        /// <returns></returns>
        public virtual DataSet Query(DbCommand cmd)
        {
            QueryTimes++;
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
        /// <returns></returns>
        public virtual Int32 QueryCount(String sql)
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
                sql = String.Format("Select Count(*) From {0}", FormatKeyWord(sql));

            QueryTimes++;
            DbCommand cmd = PrepareCommand();
            cmd.CommandText = sql;
            if (Debug) WriteLog(cmd.CommandText);
            try
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
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
        /// 执行SQL查询，返回总记录数
        /// </summary>
        /// <param name="builder">查询生成器</param>
        /// <returns>总记录数</returns>
        public virtual Int32 QueryCount(SelectBuilder builder)
        {
            QueryTimes++;
            DbCommand cmd = PrepareCommand();
            cmd.CommandText = builder.SelectCount().ToString();
            if (Debug) WriteLog(cmd.CommandText);
            try
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
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
        /// 快速查询单表记录数，稍有偏差
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public virtual Int32 QueryCountFast(String tableName)
        {
            return QueryCount(tableName);
        }

        /// <summary>
        /// 执行SQL语句，返回受影响的行数
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns></returns>
        public virtual Int32 Execute(String sql)
        {
            ExecuteTimes++;
            if (Debug) WriteLog(sql);
            try
            {
                DbCommand cmd = PrepareCommand();
                cmd.CommandText = sql;
                Int32 rs = cmd.ExecuteNonQuery();
                //AutoClose();
                return rs;
            }
            catch (DbException ex)
            {
                throw OnException(ex, sql);
            }
            finally
            {
                AutoClose();
            }
        }

        /// <summary>
        /// 执行DbCommand，返回受影响的行数
        /// </summary>
        /// <param name="cmd">DbCommand</param>
        /// <returns></returns>
        public virtual Int32 Execute(DbCommand cmd)
        {
            ExecuteTimes++;
            try
            {
                if (!Opened) Open();
                cmd.Connection = Conn;
                if (Trans != null) cmd.Transaction = Trans;
                Int32 rs = cmd.ExecuteNonQuery();
                //AutoClose();
                return rs;
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
        /// 执行插入语句并返回新增行的自动编号
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>新增行的自动编号</returns>
        public virtual Int32 InsertAndGetIdentity(String sql)
        {
            ExecuteTimes++;
            //SQLServer写法
            sql = "SET NOCOUNT ON;" + sql + ";Select SCOPE_IDENTITY()";
            if (Debug) WriteLog(sql);
            try
            {
                DbCommand cmd = PrepareCommand();
                cmd.CommandText = sql;
                Int32 rs = Int32.Parse(cmd.ExecuteScalar().ToString());
                //AutoClose();
                return rs;
            }
            catch (DbException ex)
            {
                throw OnException(ex, sql);
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
        public virtual DbCommand PrepareCommand()
        {
            DbCommand cmd = Factory.CreateCommand();
            if (!Opened) Open();
            cmd.Connection = Conn;
            if (Trans != null) cmd.Transaction = Trans;
            return cmd;
        }
        #endregion

        #region 辅助函数
        protected String FormatKeyWord(String keyWord)
        {
            return Database.FormatKeyWord(keyWord);
        }
        #endregion

        #region Sql日志输出
        private static Boolean? _Debug;
        /// <summary>
        /// 是否调试
        /// </summary>
        public static Boolean Debug
        {
            get
            {
                if (_Debug != null) return _Debug.Value;

                //String str = ConfigurationManager.AppSettings["XCode.Debug"];
                //if (String.IsNullOrEmpty(str)) str = ConfigurationManager.AppSettings["OrmDebug"];
                //if (String.IsNullOrEmpty(str))
                //    _Debug = false;
                //else if (str == "1" || str.Equals(Boolean.TrueString, StringComparison.OrdinalIgnoreCase))
                //    _Debug = true;
                //else if (str == "0" || str.Equals(Boolean.FalseString, StringComparison.OrdinalIgnoreCase))
                //    _Debug = false;
                //else
                //    _Debug = Convert.ToBoolean(str);

                _Debug = Config.GetConfig<Boolean>("XCode.Debug", Config.GetConfig<Boolean>("OrmDebug"));

                return _Debug.Value;
            }
            set { _Debug = value; }
        }

        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="msg"></param>
        public static void WriteLog(String msg)
        {
            XTrace.WriteLine(msg);
        }

        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLog(String format, params Object[] args)
        {
            XTrace.WriteLine(format, args);
        }
        #endregion
    }
}