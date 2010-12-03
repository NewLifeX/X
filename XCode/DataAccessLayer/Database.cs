using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Text;
using System.Text.RegularExpressions;
using NewLife.Log;

namespace XCode.DataAccessLayer
{
    #region 数据库类型
    /// <summary>
    /// 数据库类型
    /// </summary>
    public enum DatabaseType
    {
        /// <summary>
        /// MS的Access文件数据库
        /// </summary>
        Access = 0,

        /// <summary>
        /// MS的SqlServer数据库
        /// </summary>
        SqlServer = 1,

        /// <summary>
        /// Oracle数据库
        /// </summary>
        Oracle = 2,

        /// <summary>
        /// MySql数据库
        /// </summary>
        MySql = 3,

        /// <summary>
        /// MS的SqlServer2005数据库
        /// </summary>
        SqlServer2005 = 4,

        /// <summary>
        /// SQLite数据库
        /// </summary>
        SQLite = 5
    }
    #endregion

    /// <summary>
    /// 数据库基类。
    /// 声明为public，程序集以外对象可以集成，方便编写别的数据库子类。
    /// 基类为各数据库类定制了一个框架，默认使用Access。
    /// SqlServer和Oracle可重写以提高性能
    /// </summary>
    internal abstract class Database : IDatabase, IDisposable
    {
        #region 构造函数
        /// <summary>
        /// 是否已经释放
        /// </summary>
        private Boolean IsDisposed = false;
        /// <summary>
        /// 释放资源
        /// </summary>
        public virtual void Dispose()
        {
            if (IsDisposed) return;
            try
            {
                // 注意，没有Commit的数据，在这里将会被回滚
                //if (Trans != null) Rollback();
                if (_Trans != null && Opened) _Trans.Rollback();
                if (_Conn != null) Close();
                IsDisposed = true;
            }
            catch (Exception ex)
            {
                WriteLog("执行" + this.GetType().FullName + "的Dispose时出错：" + ex.ToString());
            }
        }

        ~Database()
        {
            Dispose();
        }
        #endregion

        #region 属性
        private static Int32 gid = 0;
        private Int32? _ID;
        /// <summary>
        /// 标识
        /// </summary>
        public Int32 ID
        {
            get
            {
                if (_ID == null) _ID = ++gid;
                return _ID.Value;
            }
        }

        /// <summary>
        /// 返回数据库类型。外部DAL数据库类请使用Other
        /// </summary>
        public abstract DatabaseType DbType { get; }

        /// <summary>工厂</summary>
        public abstract DbProviderFactory Factory { get; }

        private String _ConnectionString;
        /// <summary>链接字符串</summary>
        public virtual String ConnectionString
        {
            get { return _ConnectionString; }
            set
            {
                _ConnectionString = value;
                if (!String.IsNullOrEmpty(_ConnectionString))
                {
                    DbConnectionStringBuilder builder = new DbConnectionStringBuilder();
                    builder.ConnectionString = _ConnectionString;
                    if (builder.ContainsKey("owner"))
                    {
                        if (builder["owner"] != null) Owner = builder["owner"].ToString();
                        builder.Remove("owner");
                    }
                    _ConnectionString = builder.ToString();
                }
            }
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
                    _Conn.ConnectionString = ConnectionString;
                }
                return _Conn;
            }
            set { _Conn = value; }
        }

        private String _Owner;
        /// <summary>拥有者</summary>
        public String Owner
        {
            get { return _Owner; }
            set { _Owner = value; }
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

        /// <summary>
        /// 数据库服务器版本
        /// </summary>
        public String ServerVersion
        {
            get
            {
                if (!Opened) Open();
                String ver = Conn.ServerVersion;
                AutoClose();
                return ver;
            }
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
            if (Conn != null && Conn.State == ConnectionState.Closed)
            {
                //try { 
                Conn.Open();
                //}
                //catch (Exception ex)
                //{
                //    WriteLog("执行" + this.GetType().FullName + "的Open时出错：" + ex.ToString());
                //    throw;
                //}
            }
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
                    WriteLog("执行" + this.GetType().FullName + "的Close时出错：" + ex.ToString());
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
        protected virtual Exception OnException(Exception ex)
        {
            if (Trans == null && Opened) Close(); // 强制关闭数据库
            //return new XException("内部数据库实体" + this.GetType().FullName + "异常，执行" + Environment.StackTrace + "方法出错！", ex);
            String err = "内部数据库实体" + DbType.ToString() + "异常，执行方法出错！" + Environment.NewLine + ex.Message;
            if (ex != null)
                return new Exception(err, ex);
            else
                return new Exception(err);
        }

        /// <summary>
        /// 当异常发生时触发。关闭数据库连接，或者返还连接到连接池。
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        protected virtual Exception OnException(Exception ex, String sql)
        {
            if (Trans == null && Opened) Close(); // 强制关闭数据库
            //return new XException("内部数据库实体" + this.GetType().FullName + "异常，执行" + Environment.StackTrace + "方法出错！", ex);
            String err = "内部数据库实体" + DbType.ToString() + "异常，执行方法出错！" + Environment.NewLine;
            if (!String.IsNullOrEmpty(sql)) err += "SQL语句：" + sql + Environment.NewLine;
            err += ex.Message;
            if (ex != null)
                return new Exception(err, ex);
            else
                return new Exception(err);
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
            if (Debug) WriteLog("开始事务：{0}", ID);

            TransactionCount++;
            if (TransactionCount > 1) return TransactionCount;

            try
            {
                if (!Opened) Open();
                Trans = Conn.BeginTransaction();
                TransactionCount = 1;
                return TransactionCount;
            }
            catch (Exception ex)
            {
                throw OnException(ex);
            }
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public Int32 Commit()
        {
            if (Debug) WriteLog("提交事务：{0}", ID);

            TransactionCount--;
            if (TransactionCount > 0) return TransactionCount;

            if (Trans == null) throw new InvalidOperationException("当前并未开始事务，请用BeginTransaction方法开始新事务！ID=" + ID);
            try
            {
                Trans.Commit();
                Trans = null;
                if (IsAutoClose) Close();
            }
            catch (Exception ex)
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
            if (Debug) WriteLog("回滚事务：{0}", ID);

            TransactionCount--;
            if (TransactionCount > 0) return TransactionCount;

            if (Trans == null) throw new InvalidOperationException("当前并未开始事务，请用BeginTransaction方法开始新事务！ID=" + ID);
            try
            {
                Trans.Rollback();
                Trans = null;
                if (IsAutoClose) Close();
            }
            catch (Exception ex)
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
                    //AutoClose();
                    return ds;
                }
            }
            catch (Exception ex)
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
            return Query(PageSplit(builder, startRowIndex, maximumRows, keyColumn));
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
                    //AutoClose();
                    return ds;
                }
                catch (Exception ex)
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
                String orderBy = CheckOrderClause(ref sql);
                //sql = String.Format("Select Count(*) From {0}", CheckSimpleSQL(sql));
                //Match m = reg_QueryCount.Match(sql);
                MatchCollection ms = reg_QueryCount.Matches(sql);
                if (ms != null && ms.Count > 0)
                {
                    sql = String.Format("Select Count(*) From {0}", ms[0].Groups[1].Value);
                }
                else
                {
                    sql = String.Format("Select Count(*) From {0}", CheckSimpleSQL(sql));
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
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
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

        #region 分页
        /// <summary>
        /// 构造分页SQL
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="startRowIndex">开始行，0开始</param>
        /// <param name="maximumRows">最大返回行数</param>
        /// <param name="keyColumn">唯一键。用于not in分页</param>
        /// <returns>分页SQL</returns>
        public virtual String PageSplit(String sql, Int32 startRowIndex, Int32 maximumRows, String keyColumn)
        {
            // 从第一行开始，不需要分页
            if (startRowIndex <= 0 && maximumRows < 1) return sql;

            #region Max/Min分页
            // 如果要使用max/min分页法，首先keyColumn必须有asc或者desc
            if (!String.IsNullOrEmpty(keyColumn))
            {
                String kc = keyColumn.ToLower();
                if (kc.EndsWith(" desc") || kc.EndsWith(" asc") || kc.EndsWith(" unknown"))
                {
                    String str = PageSplitMaxMin(sql, startRowIndex, maximumRows, keyColumn);
                    if (!String.IsNullOrEmpty(str)) return str;
                    keyColumn = keyColumn.Substring(0, keyColumn.IndexOf(" "));
                }
            }
            #endregion

            //检查简单SQL。为了让生成分页SQL更短
            String tablename = CheckSimpleSQL(sql);
            if (tablename != sql)
                sql = tablename;
            else
                sql = String.Format("({0}) XCode_Temp_a", sql);

            // 取第一页也不用分页。把这代码放到这里，主要是数字分页中要自己处理这种情况
            if (startRowIndex <= 0 && maximumRows > 0)
                return String.Format("Select Top {0} * From {1}", maximumRows, sql);

            if (String.IsNullOrEmpty(keyColumn)) throw new ArgumentNullException("keyColumn", "这里用的not in分页算法要求指定主键列！");

            if (maximumRows < 1)
                sql = String.Format("Select * From {1} Where {2} Not In(Select Top {0} {2} From {1})", startRowIndex, sql, keyColumn);
            else
                sql = String.Format("Select Top {0} * From {1} Where {2} Not In(Select Top {3} {2} From {1})", maximumRows, sql, keyColumn, startRowIndex);
            return sql;
        }

        protected String PageSplitMaxMin(String sql, Int32 startRowIndex, Int32 maximumRows, String keyColumn)
        {
            // 唯一键的顺序。默认为Empty，可以为asc或desc，如果有，则表明主键列是数字唯一列，可以使用max/min分页法
            Boolean isAscOrder = keyColumn.ToLower().EndsWith(" asc");
            // 是否使用max/min分页法
            Boolean canMaxMin = false;

            // 如果sql最外层有排序，且唯一的一个排序字段就是keyColumn时，可用max/min分页法
            // 如果sql最外层没有排序，其排序不是unknown，可用max/min分页法
            MatchCollection ms = Regex.Matches(sql, @"\border\s*by\b([^)]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (ms != null && ms.Count > 0 && ms[0].Index > 0)
            {
                // 取第一页也不用分页。把这代码放到这里，主要是数字分页中要自己处理这种情况
                if (startRowIndex <= 0 && maximumRows > 0)
                    return String.Format("Select Top {0} * From {1}", maximumRows, CheckSimpleSQL(sql));

                keyColumn = keyColumn.Substring(0, keyColumn.IndexOf(" "));
                sql = sql.Substring(0, ms[0].Index);

                String strOrderBy = ms[0].Groups[1].Value.Trim();
                // 只有一个排序字段
                if (!String.IsNullOrEmpty(strOrderBy) && !strOrderBy.Contains(","))
                {
                    // 有asc或者desc。没有时，默认为asc
                    if (strOrderBy.ToLower().EndsWith(" desc"))
                    {
                        String str = strOrderBy.Substring(0, strOrderBy.Length - " desc".Length).Trim();
                        // 排序字段等于keyColumn
                        if (str.ToLower() == keyColumn.ToLower())
                        {
                            isAscOrder = false;
                            canMaxMin = true;
                        }
                    }
                    else if (strOrderBy.ToLower().EndsWith(" asc"))
                    {
                        String str = strOrderBy.Substring(0, strOrderBy.Length - " asc".Length).Trim();
                        // 排序字段等于keyColumn
                        if (str.ToLower() == keyColumn.ToLower())
                        {
                            isAscOrder = true;
                            canMaxMin = true;
                        }
                    }
                    else if (!strOrderBy.Contains(" ")) // 不含空格，是唯一排序字段
                    {
                        // 排序字段等于keyColumn
                        if (strOrderBy.ToLower() == keyColumn.ToLower())
                        {
                            isAscOrder = true;
                            canMaxMin = true;
                        }
                    }
                }
            }
            else
            {
                // 取第一页也不用分页。把这代码放到这里，主要是数字分页中要自己处理这种情况
                if (startRowIndex <= 0 && maximumRows > 0)
                {
                    //数字分页中，业务上一般使用降序，Entity类会给keyColumn指定降序的
                    //但是，在第一页的时候，没有用到keyColumn，而数据库一般默认是升序
                    //这时候就会出现第一页是升序，后面页是降序的情况了。这里改正这个BUG
                    if (keyColumn.ToLower().EndsWith(" desc") || keyColumn.ToLower().EndsWith(" asc"))
                        return String.Format("Select Top {0} * From {1} Order By {2}", maximumRows, CheckSimpleSQL(sql), keyColumn);
                    else
                        return String.Format("Select Top {0} * From {1}", maximumRows, CheckSimpleSQL(sql));
                }

                if (!keyColumn.ToLower().EndsWith(" unknown")) canMaxMin = true;

                keyColumn = keyColumn.Substring(0, keyColumn.IndexOf(" "));
            }

            if (canMaxMin)
            {
                if (maximumRows < 1)
                    sql = String.Format("Select * From {1} Where {2}{3}(Select {4}({2}) From (Select Top {0} {2} From {1} Order By {2} {5}) XCode_Temp_a) Order By {2} {5}", startRowIndex, CheckSimpleSQL(sql), keyColumn, isAscOrder ? ">" : "<", isAscOrder ? "max" : "min", isAscOrder ? "Asc" : "Desc");
                else
                    sql = String.Format("Select Top {0} * From {1} Where {2}{4}(Select {5}({2}) From (Select Top {3} {2} From {1} Order By {2} {6}) XCode_Temp_a) Order By {2} {6}", maximumRows, CheckSimpleSQL(sql), keyColumn, startRowIndex, isAscOrder ? ">" : "<", isAscOrder ? "max" : "min", isAscOrder ? "Asc" : "Desc");
                return sql;
            }
            return null;
        }

        /// <summary>
        /// 检查简单SQL语句，比如Select * From table
        /// </summary>
        /// <param name="sql">待检查SQL语句</param>
        /// <returns>如果是简单SQL语句则返回表名，否则返回子查询(sql) XCode_Temp_a</returns>
        protected static String CheckSimpleSQL(String sql)
        {
            if (String.IsNullOrEmpty(sql)) return sql;

            Regex reg = new Regex(@"^\s*select\s+\*\s+from\s+([\w\[\]]+)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            MatchCollection ms = reg.Matches(sql);
            if (ms == null || ms.Count < 1 || ms[0].Groups.Count < 2 ||
                String.IsNullOrEmpty(ms[0].Groups[1].Value)) return String.Format("({0}) XCode_Temp_a", sql);
            return ms[0].Groups[1].Value;
        }

        /// <summary>
        /// 检查是否以Order子句结尾，如果是，分割sql为前后两部分
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        private static String CheckOrderClause(ref String sql)
        {
            if (!sql.ToLower().Contains("order")) return null;

            // 使用正则进行严格判断。必须包含Order By，并且它右边没有右括号)，表明有order by，且不是子查询的，才需要特殊处理
            MatchCollection ms = Regex.Matches(sql, @"\border\s*by\b([^)]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (ms == null || ms.Count < 1 || ms[0].Index < 1) return null;
            String orderBy = sql.Substring(ms[0].Index).Trim();
            sql = sql.Substring(0, ms[0].Index).Trim();

            return orderBy;
        }

        /// <summary>
        /// 构造分页SQL
        /// </summary>
        /// <param name="builder">查询生成器</param>
        /// <param name="startRowIndex">开始行，0开始</param>
        /// <param name="maximumRows">最大返回行数</param>
        /// <param name="keyColumn">唯一键。用于not in分页</param>
        /// <returns>分页SQL</returns>
        public virtual String PageSplit(SelectBuilder builder, Int32 startRowIndex, Int32 maximumRows, String keyColumn)
        {
            if (String.IsNullOrEmpty(builder.GroupBy) && startRowIndex <= 0 && maximumRows > 0) return PageSplit(builder, maximumRows);

            return PageSplit(builder.ToString(), startRowIndex, maximumRows, keyColumn);
        }

        protected virtual String PageSplit(SelectBuilder builder, Int32 maximumRows)
        {
            SelectBuilder sb = builder.Clone();
            if (String.IsNullOrEmpty(builder.Column)) builder.Column = "*";
            builder.Column = String.Format("Top {0} {1}", maximumRows, builder.Column);
            return builder.ToString();
        }
        #endregion

        #region 构架
        /// <summary>
        /// 返回数据源的架构信息
        /// </summary>
        /// <param name="collectionName">指定要返回的架构的名称。</param>
        /// <param name="restrictionValues">为请求的架构指定一组限制值。</param>
        /// <returns></returns>
        public virtual DataTable GetSchema(string collectionName, string[] restrictionValues)
        {
            if (!Opened) Open();

            DataTable dt;
            if (restrictionValues == null || restrictionValues.Length < 1)
            {
                if (String.IsNullOrEmpty(collectionName))
                    dt = Conn.GetSchema();
                else
                    dt = Conn.GetSchema(collectionName);
            }
            else
                dt = Conn.GetSchema(collectionName, restrictionValues);

            AutoClose();

            return dt;
        }

        /// <summary>
        /// 取得所有表构架
        /// </summary>
        /// <returns></returns>
        public virtual List<XTable> GetTables()
        {
            List<XTable> list = null;
            //try
            //{
            DataTable[] dts = new DataTable[2];
            dts[0] = GetSchema("Tables", new String[] { null, null, null, "TABLE" });
            dts[1] = GetSchema("Tables", new String[] { null, null, null, "VIEW" });
            list = new List<XTable>();
            for (Int32 i = 0; i < dts.Length; i++)
            {
                if (dts[i] != null && dts[i].Rows != null && dts[i].Rows.Count > 0)
                {
                    foreach (DataRow drTable in dts[i].Rows)
                    {
                        XTable xt = new XTable();
                        xt.ID = list.Count + 1;
                        xt.Name = drTable["TABLE_NAME"].ToString();
                        xt.Description = drTable["DESCRIPTION"].ToString();
                        xt.IsView = i > 0;

                        xt.Fields = GetFields(xt);

                        list.Add(xt);
                    }
                }
            }
            //}
            //catch (Exception ex)
            //{
            //    throw new Exception("取得所有表构架出错！", ex);
            //}

            return list;
        }

        /// <summary>
        /// 取得指定表的所有列构架
        /// </summary>
        /// <param name="xt"></param>
        /// <returns></returns>
        protected virtual List<XField> GetFields(XTable xt)
        {
            DataColumnCollection columns = GetColumns(xt.Name);

            DataTable dt = GetSchema("Columns", new String[] { null, null, xt.Name });

            List<XField> list = new List<XField>();
            DataRow[] drs = dt.Select("", "ORDINAL_POSITION");
            List<String> pks = GetPrimaryKeys(xt);
            Int32 IDCount = 0;
            foreach (DataRow dr in drs)
            {
                XField xf = xt.CreateField(); ;

                xf.ID = Int32.Parse(dr["ORDINAL_POSITION"].ToString());
                xf.Name = dr["COLUMN_NAME"].ToString();
                //xf.DataType = FieldTypeToClassType(dr["DATA_TYPE"].ToString());
                xf.Identity = dr["DATA_TYPE"].ToString() == "3" && (dr["COLUMN_FLAGS"].ToString() == "16" || dr["COLUMN_FLAGS"].ToString() == "90");

                // 使用这种方式获取类型，非常精确，因为类型映射是由ADO.Net实现的，但是速度非常慢，建议每个具体数据库类重写，自己建立映射表
                if (columns != null && columns.Contains(xf.Name))
                {
                    DataColumn dc = columns[xf.Name];
                    xf.DataType = dc.DataType;
                }

                xf.PrimaryKey = pks != null && pks.Contains(xf.Name);

                if (Type.GetTypeCode(xf.DataType) == TypeCode.Int32 || Type.GetTypeCode(xf.DataType) == TypeCode.Double)
                {
                    xf.Length = dr["NUMERIC_PRECISION"] == DBNull.Value ? 0 : Int32.Parse(dr["NUMERIC_PRECISION"].ToString());
                    xf.NumOfByte = 0;
                    xf.Digit = dr["NUMERIC_SCALE"] == DBNull.Value ? 0 : Int32.Parse(dr["NUMERIC_SCALE"].ToString());
                }
                else if (Type.GetTypeCode(xf.DataType) == TypeCode.DateTime)
                {
                    xf.Length = dr["DATETIME_PRECISION"] == DBNull.Value ? 0 : Int32.Parse(dr["DATETIME_PRECISION"].ToString());
                    xf.NumOfByte = 0;
                    xf.Digit = 0;
                }
                else
                {
                    if (dr["DATA_TYPE"].ToString() == "130" && dr["COLUMN_FLAGS"].ToString() == "234") //备注类型
                    {
                        xf.Length = Int32.MaxValue;
                        xf.NumOfByte = Int32.MaxValue;
                    }
                    else
                    {
                        xf.Length = dr["CHARACTER_MAXIMUM_LENGTH"] == DBNull.Value ? 0 : Int32.Parse(dr["CHARACTER_MAXIMUM_LENGTH"].ToString());
                        xf.NumOfByte = dr["CHARACTER_OCTET_LENGTH"] == DBNull.Value ? 0 : Int32.Parse(dr["CHARACTER_OCTET_LENGTH"].ToString());
                    }
                    xf.Digit = 0;
                }

                try
                {
                    xf.Nullable = Boolean.Parse(dr["IS_NULLABLE"].ToString());
                }
                catch
                {
                    xf.Nullable = dr["IS_NULLABLE"].ToString() == "YES";
                }
                try
                {
                    xf.Default = dr["COLUMN_HASDEFAULT"].ToString() == "False" ? "" : dr["COLUMN_DEFAULT"].ToString();
                }
                catch
                {
                    xf.Default = dr["COLUMN_DEFAULT"].ToString();
                }
                try
                {
                    xf.Description = dr["DESCRIPTION"] == DBNull.Value ? "" : dr["DESCRIPTION"].ToString();
                }
                catch
                {
                    xf.Description = "";
                }

                //处理默认值
                while (!String.IsNullOrEmpty(xf.Default) && xf.Default[0] == '(' && xf.Default[xf.Default.Length - 1] == ')')
                {
                    xf.Default = xf.Default.Substring(1, xf.Default.Length - 2);
                }
                if (!String.IsNullOrEmpty(xf.Default)) xf.Default = xf.Default.Trim(new Char[] { '"', '\'' });

                //修正自增字段属性
                if (xf.Identity)
                {
                    if (xf.Nullable)
                        xf.Identity = false;
                    else if (!String.IsNullOrEmpty(xf.Default))
                        xf.Identity = false;
                }
                if (xf.Identity) IDCount++;

                list.Add(xf);
            }

            //再次修正自增字段
            if (IDCount > 1)
            {
                foreach (XField xf in list)
                {
                    if (!xf.Identity) continue;

                    if (!String.Equals(xf.Name, "ID", StringComparison.OrdinalIgnoreCase))
                    {
                        xf.Identity = false;
                        IDCount--;
                    }
                }
            }
            if (IDCount > 1)
            {
                foreach (XField xf in list)
                {
                    if (!xf.Identity) continue;

                    if (xf.ID > 1)
                    {
                        xf.Identity = false;
                        IDCount--;
                    }
                }
            }

            return list;
        }

        protected DataColumnCollection GetColumns(String tableName)
        {
            //return Query(PageSplit("Select * from " + tableName, 0, 1, null)).Tables[0].Columns;
            try
            {
                return Query(PageSplit("Select * from " + FormatKeyWord(tableName), 0, 1, null)).Tables[0].Columns;
            }
            catch (Exception ex)
            {
                WriteLog(ex.ToString());
                return null;
            }
        }

        #region 主键构架
        /// <summary>
        /// 取得指定表的所有主键构架
        /// </summary>
        /// <param name="xt"></param>
        /// <returns></returns>
        protected List<String> GetPrimaryKeys(XTable xt)
        {
            if (PrimaryKeys == null) return null;
            try
            {
                DataRow[] drs = PrimaryKeys.Select("TABLE_NAME='" + xt.Name + @"'");
                if (drs == null || drs.Length < 1) return null;
                List<String> list = new List<string>();
                foreach (DataRow dr in drs)
                {
                    list.Add(dr["COLUMN_NAME"] == DBNull.Value ? "" : dr["COLUMN_NAME"].ToString());
                }
                return list;
            }
            catch { return null; }
        }

        protected DataTable _PrimaryKeys;
        /// <summary>
        /// 主键构架
        /// </summary>
        protected virtual DataTable PrimaryKeys
        {
            get
            {
                if (_PrimaryKeys == null) _PrimaryKeys = GetSchema("PrimaryKeys", new String[] { null, null, null });
                return _PrimaryKeys;
            }
        }
        #endregion

        #region 字段类型到数据类型对照表
        /// <summary>
        /// 字段类型到数据类型对照表
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual Type FieldTypeToClassType(String type)
        {
            Int32 t = Int32.Parse(type);
            switch (t)
            {
                case 16:// adTinyInt
                case 2:// adSmallInt
                case 17:// adUnsignedTinyInt
                case 18:// adUnsignedSmallInt
                    return typeof(Int16);
                case 3:// adInteger
                case 19:// adUnsignedInt
                case 14:// adDecimal
                case 131:// adNumeric
                    return typeof(Int32);
                case 20:// adBigInt
                case 21:// adUnsignedBigInt
                    return typeof(Int64);
                case 4:// adSingle
                case 5:// adDouble
                case 6:// adCurrency
                    return typeof(Double);
                case 11:// adBoolean
                    return typeof(Boolean);
                case 7:// adDate
                case 133:// adDBDate
                case 134:// adDBTime
                case 135:// adDBTimeStamp
                    return typeof(DateTime);
                case 8:// adBSTR
                case 129:// adChar
                case 200:// adVarChar
                case 201:// adLongVarChar
                case 130:// adWChar
                case 202:// adVarWChar
                case 203:// adLongVarWChar
                    return typeof(String);
                case 128:// adBinary
                case 204:// adVarBinary
                case 205:// adLongVarBinary 
                case 0:// adEmpty
                case 10:// adError
                case 132:// adUserDefined
                case 12:// adVariant
                case 9:// adIDispatch
                case 13:// adIUnknown
                case 72:// adGUID
                default:
                    return typeof(String);
            }
        }
        #endregion

        #region 数据定义
        /// <summary>
        /// 获取数据定义语句
        /// </summary>
        /// <param name="schema">数据定义模式</param>
        /// <param name="values">其它信息</param>
        /// <returns></returns>
        public virtual String GetSchemaSQL(DDLSchema schema, params Object[] values)
        {
            switch (schema)
            {
                case DDLSchema.CreateDatabase:
                    return CreateDatabaseSQL((String)values[0], (String)values[1]);
                case DDLSchema.DropDatabase:
                    return String.Format("Drop Database [{0}]", (String)values[0]);
                case DDLSchema.DatabaseExist:
                    return DatabaseExistSQL(values == null || values.Length < 1 ? null : (String)values[0]);
                case DDLSchema.CreateTable:
                    return CreateTableSQL((XTable)values[0]);
                case DDLSchema.DropTable:
                    return String.Format("Drop Table [{0}]", (String)values[0]);
                case DDLSchema.TableExist:
                    return TableExistSQL((String)values[0]);
                case DDLSchema.AddTableDescription:
                    return AddTableDescriptionSQL((String)values[0], (String)values[1]);
                case DDLSchema.DropTableDescription:
                    return DropTableDescriptionSQL((String)values[0]);
                case DDLSchema.AddColumn:
                    return AddColumnSQL((String)values[0], (XField)values[1]);
                case DDLSchema.AlterColumn:
                    return AlterColumnSQL((String)values[0], (XField)values[1]);
                case DDLSchema.DropColumn:
                    return DropColumnSQL((String)values[0], (String)values[1]);
                case DDLSchema.AddColumnDescription:
                    return AddColumnDescriptionSQL((String)values[0], (String)values[1], (String)values[2]);
                case DDLSchema.DropColumnDescription:
                    return DropColumnDescriptionSQL((String)values[0], (String)values[1]);
                case DDLSchema.AddDefault:
                    return AddDefaultSQL((String)values[0], (XField)values[1]);
                case DDLSchema.DropDefault:
                    return DropDefaultSQL((String)values[0], (String)values[1]);
                default:
                    break;
            }

            throw new NotSupportedException("不支持该操作！");
        }

        /// <summary>
        /// 设置数据定义模式
        /// </summary>
        /// <param name="schema">数据定义模式</param>
        /// <param name="values">其它信息</param>
        /// <returns></returns>
        public virtual Object SetSchema(DDLSchema schema, params Object[] values)
        {
            String sql = GetSchemaSQL(schema, values);
            if (String.IsNullOrEmpty(sql)) return null;

            if (schema == DDLSchema.TableExist || schema == DDLSchema.DatabaseExist)
            {
                return QueryCount(sql) > 0;
            }
            else
            {
                String[] ss = sql.Split(new String[] { ";" + Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                if (ss == null || ss.Length < 1)
                    return Execute(sql);
                else
                {
                    foreach (String item in ss)
                    {
                        Execute(item);
                    }
                    return 0;
                }
            }
        }

        #region 数据定义语句
        public virtual String CreateDatabaseSQL(String dbname, String file)
        {
            return null;
        }

        public virtual String DatabaseExistSQL(String dbname)
        {
            throw new NotSupportedException("该功能未实现！");
        }

        public virtual String CreateTableSQL(XTable table)
        {
            List<XField> Fields = new List<XField>(table.Fields);
            Fields.Sort(delegate(XField item1, XField item2) { return item1.ID.CompareTo(item2.ID); });

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("CREATE TABLE [{0}](", table.Name);
            List<String> keys = new List<string>();
            for (Int32 i = 0; i < Fields.Count; i++)
            {
                sb.AppendLine();
                sb.Append("\t");
                sb.Append(FieldClause(Fields[i], true));
                if (i < Fields.Count - 1) sb.Append(",");

                if (Fields[i].PrimaryKey) keys.Add(Fields[i].Name);
            }
            sb.AppendLine();
            sb.Append(")");

            ////默认值
            //foreach (XField item in Fields)
            //{
            //    if (!String.IsNullOrEmpty(item.Default))
            //    {
            //        sb.AppendLine(";");
            //        sb.Append(AlterColumnSQL(table.Name, item));
            //    }
            //}

            //注释
            if (!String.IsNullOrEmpty(table.Description))
            {
                String sql = AddTableDescriptionSQL(table.Name, table.Description);
                if (!String.IsNullOrEmpty(sql))
                {
                    sb.AppendLine(";");
                    sb.Append(sql);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 字段片段
        /// </summary>
        /// <param name="field"></param>
        /// <param name="onlyDefine">仅仅定义。定义操作才允许设置自增和使用默认值</param>
        /// <returns></returns>
        public virtual String FieldClause(XField field, Boolean onlyDefine)
        {
            StringBuilder sb = new StringBuilder();

            //字段名
            //sb.AppendFormat("[{0}] ", field.Name);
            sb.AppendFormat("{0} ", FormatKeyWord(field.Name));

            //类型
            TypeCode tc = Type.GetTypeCode(field.DataType);
            switch (tc)
            {
                case TypeCode.Boolean:
                    sb.Append("bit");
                    break;
                case TypeCode.Byte:
                    sb.Append("byte");
                    break;
                case TypeCode.Char:
                    sb.Append("bit");
                    break;
                case TypeCode.DBNull:
                    break;
                case TypeCode.DateTime:
                    sb.Append("datetime");
                    break;
                case TypeCode.Decimal:
                    sb.AppendFormat("NUMERIC({0},{1})", field.Length, field.Digit);
                    break;
                case TypeCode.Double:
                    sb.Append("double");
                    break;
                case TypeCode.Empty:
                    break;
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    if (onlyDefine && field.Identity)
                        sb.Append("AUTOINCREMENT(1,1)");
                    else
                        sb.Append("short");
                    break;
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    if (onlyDefine && field.Identity)
                        sb.Append("AUTOINCREMENT(1,1)");
                    else
                        sb.Append("Long");
                    break;
                case TypeCode.Object:
                    break;
                case TypeCode.SByte:
                    sb.Append("byte");
                    break;
                case TypeCode.Single:
                    sb.Append("real");
                    break;
                case TypeCode.String:
                    Int32 len = field.Length;
                    if (len < 1) len = 50;
                    if (len > 255)
                        sb.Append("Memo ");
                    else
                        sb.AppendFormat("Text({0}) ", len);
                    break;
                default:
                    break;
            }

            if (field.PrimaryKey)
            {
                sb.Append(" primary key");
            }
            else
            {
                //是否为空
                //if (!field.Nullable) sb.Append(" NOT NULL");
                if (field.Nullable)
                    sb.Append(" NULL");
                else
                {
                    sb.Append(" NOT NULL");
                }
            }

            //默认值
            if (onlyDefine && !String.IsNullOrEmpty(field.Default))
            {
                if (tc == TypeCode.String)
                    sb.AppendFormat(" DEFAULT '{0}'", field.Default);
                else if (tc == TypeCode.DateTime)
                {
                    String d = field.Default;
                    //if (String.Equals(d, "getdate()", StringComparison.OrdinalIgnoreCase)) d = "now()";
                    if (String.Equals(d, "getdate()", StringComparison.OrdinalIgnoreCase)) d = DateTimeNow;
                    sb.AppendFormat(" DEFAULT {0}", d);
                }
                else
                    sb.AppendFormat(" DEFAULT {0}", field.Default);
            }
            //else if (onlyDefine && !field.PrimaryKey && !field.Nullable)
            //{
            //    //该字段不允许空，而又没有默认值时，设置默认值
            //    if (!includeDefault || String.IsNullOrEmpty(field.Default))
            //    {
            //        if (tc == TypeCode.String)
            //            sb.AppendFormat(" DEFAULT ('{0}')", "");
            //        else if (tc == TypeCode.DateTime)
            //        {
            //            String d = SqlDateTime.MinValue.Value.ToString("yyyy-MM-dd HH:mm:ss");
            //            sb.AppendFormat(" DEFAULT {0}", d);
            //        }
            //        else
            //            sb.AppendFormat(" DEFAULT {0}", "''");
            //    }
            //}

            return sb.ToString();
        }

        public virtual String TableExistSQL(String tablename)
        {
            throw new NotSupportedException("该功能未实现！");
        }

        public virtual String AddTableDescriptionSQL(String tablename, String description)
        {
            return null;
        }

        public virtual String DropTableDescriptionSQL(String tablename)
        {
            return null;
        }

        public virtual String AddColumnSQL(String tablename, XField field)
        {
            return String.Format("Alter TABLE {0} Add {1}", FormatKeyWord(tablename), FieldClause(field, true));
        }

        public virtual String AlterColumnSQL(String tablename, XField field)
        {
            return String.Format("Alter Table {0} Alter Column {1}", FormatKeyWord(tablename), FieldClause(field, false));
        }

        public virtual String DropColumnSQL(String tablename, String columnname)
        {
            return String.Format("Alter TABLE {0} Drop Column {1}", FormatKeyWord(tablename), columnname);
        }

        public virtual String AddColumnDescriptionSQL(String tablename, String columnname, String description)
        {
            return null;
        }

        public virtual String DropColumnDescriptionSQL(String tablename, String columnname)
        {
            return null;
        }

        public virtual String AddDefaultSQL(String tablename, XField field)
        {
            return null;
        }

        public virtual String DropDefaultSQL(String tablename, String columnname)
        {
            return null;
        }
        #endregion

        #region 数据定义操作
        #endregion
        #endregion
        #endregion

        #region 数据库特性
        /// <summary>
        /// 当前时间函数
        /// </summary>
        public virtual String DateTimeNow { get { throw new NotImplementedException("数据库实体不支持该操作！"); } }

        /// <summary>
        /// 最小时间
        /// </summary>
        public virtual DateTime DateTimeMin { get { throw new NotImplementedException("数据库实体不支持该操作！"); } }

        /// <summary>
        /// 格式化时间为SQL字符串
        /// </summary>
        /// <param name="dateTime">时间值</param>
        /// <returns></returns>
        public virtual String FormatDateTime(DateTime dateTime)
        {
            throw new NotImplementedException("数据库实体不支持该操作！");
        }

        /// <summary>
        /// 格式化关键字
        /// </summary>
        /// <param name="keyWord">表名</param>
        /// <returns></returns>
        public virtual String FormatKeyWord(String keyWord)
        {
            throw new NotImplementedException("数据库实体不支持该操作！");
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

                String str = ConfigurationManager.AppSettings["XCode.Debug"];
                if (String.IsNullOrEmpty(str)) str = ConfigurationManager.AppSettings["OrmDebug"];
                if (String.IsNullOrEmpty(str))
                    _Debug = false;
                else if (str == "1" || str.Equals(Boolean.TrueString, StringComparison.OrdinalIgnoreCase))
                    _Debug = true;
                else if (str == "0" || str.Equals(Boolean.FalseString, StringComparison.OrdinalIgnoreCase))
                    _Debug = false;
                else
                    _Debug = Convert.ToBoolean(str);
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