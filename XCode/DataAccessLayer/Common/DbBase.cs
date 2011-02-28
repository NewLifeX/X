using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.OleDb;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using NewLife;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 数据库基类。数据库类的职责是抽象不同数据库的共同点，理应最小化，保证原汁原味，因此不做缓存等实现。
    /// 对于每一个连接字符串配置，都有一个数据库实例，而不是每个数据库类型一个实例，因为同类型数据库不同版本行为不同。
    /// </summary>
    abstract class DbBase : DisposeBase, IDatabase
    {
        #region 构造函数
        /// <summary>
        /// 销毁资源时，回滚未提交事务，并关闭数据库连接
        /// </summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            if (_sessions != null)
            {
                // 销毁本线程的数据库会话，似乎无法销毁别的线程的数据库会话
                if (_sessions.ContainsKey(this)) _sessions.Remove(this);
            }

            if (_MySessions != null) ReleaseSession();

            if (_metadata != null)
            {
                // 销毁本数据库的元数据对象
                try
                {
                    _metadata.Dispose();
                }
                catch { }
                _metadata = null;
            }
        }

        /// <summary>
        /// 释放所有会话
        /// </summary>
        internal void ReleaseSession()
        {
            if (_MySessions != null)
            {
                // 销毁本数据库的所有数据库会话
                foreach (IDbSession session in _MySessions)
                {
                    try
                    {
                        session.Dispose();
                    }
                    catch { }
                }
                _MySessions.Clear();
                _MySessions = null;
            }
        }
        #endregion

        #region 属性
        /// <summary>
        /// 返回数据库类型。外部DAL数据库类请使用Other
        /// </summary>
        public virtual DatabaseType DbType { get { return DatabaseType.Other; } }

        /// <summary>工厂</summary>
        public virtual DbProviderFactory Factory { get { return OleDbFactory.Instance; } }

        private String _ConnName;
        /// <summary>连接名</summary>
        public String ConnName
        {
            get { return _ConnName; }
            set { _ConnName = value; }
        }

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

        private String _Owner;
        /// <summary>拥有者</summary>
        public virtual String Owner
        {
            get { return _Owner; }
            set { _Owner = value; }
        }

        private String _ServerVersion;
        /// <summary>
        /// 数据库服务器版本
        /// </summary>
        public virtual String ServerVersion
        {
            get
            {
                if (_ServerVersion != null) return _ServerVersion;
                _ServerVersion = String.Empty;

                IDbSession session = CreateSession();
                if (!session.Opened) session.Open();
                try
                {
                    _ServerVersion = session.Conn.ServerVersion;

                    return _ServerVersion;
                }
                finally { session.AutoClose(); }
            }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 保证数据库在每一个线程都有唯一的一个实例
        /// </summary>
        [ThreadStatic]
        private static Dictionary<DbBase, IDbSession> _sessions;

        /// <summary>
        /// 保存当前数据库的所有会话
        /// </summary>
        private List<IDbSession> _MySessions;

        /// <summary>
        /// 创建数据库会话，数据库在每一个线程都有唯一的一个实例
        /// </summary>
        /// <returns></returns>
        public IDbSession CreateSession()
        {
            if (_sessions == null) _sessions = new Dictionary<DbBase, IDbSession>();

            IDbSession session = null;
            if (_sessions.TryGetValue(this, out session))
            {
                // 会话可能已经被销毁
                if (!(session is DbSession) || !(session as DbSession).Disposed) return session;
            }
            lock (_sessions)
            {
                if (_sessions.TryGetValue(this, out session))
                {
                    // 会话可能已经被销毁
                    if (!(session is DbSession) || !(session as DbSession).Disposed) return session;
                }

                if (_MySessions == null) _MySessions = new List<IDbSession>();
                if (session != null && _MySessions.Contains(session)) _MySessions.Remove(session);

                Boolean isNew = session == null;

                session = OnCreateSession();
                session.ConnectionString = ConnectionString;
                if (session is DbSession) (session as DbSession).Database = this;

                if (isNew)
                    _sessions.Add(this, session);
                else
                    _sessions[this] = session;

                _MySessions.Add(session);

                return session;
            }
        }

        /// <summary>
        /// 创建数据库会话
        /// </summary>
        /// <returns></returns>
        protected abstract IDbSession OnCreateSession();

        /// <summary>
        /// 唯一实例
        /// </summary>
        private IMetaData _metadata;

        /// <summary>
        /// 创建元数据对象，唯一实例
        /// </summary>
        /// <returns></returns>
        public IMetaData CreateMetaData()
        {
            if (_metadata != null)
            {
                // 实例可能已经被销毁
                if (!(_metadata is DbMetaData) || !(_metadata as DbMetaData).Disposed) return _metadata;
            }

            _metadata = OnCreateMetaData();
            if (_metadata is DbMetaData) (_metadata as DbMetaData).Database = this;

            return _metadata;
        }

        /// <summary>
        /// 创建元数据对象
        /// </summary>
        /// <returns></returns>
        protected abstract IMetaData OnCreateMetaData();

        /// <summary>
        /// 获取提供者工厂
        /// </summary>
        /// <param name="assemblyFile"></param>
        /// <param name="className"></param>
        /// <returns></returns>
        protected static DbProviderFactory GetProviderFactory(String assemblyFile, String className)
        {
            //反射实现获取数据库工厂
            String file = assemblyFile;

            if (String.IsNullOrEmpty(HttpRuntime.AppDomainAppId))
                file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file);
            else
                file = Path.Combine(HttpRuntime.BinDirectory, file);

            if (!File.Exists(file)) throw new FileNotFoundException("缺少文件" + file + "！", file);

            Assembly asm = Assembly.LoadFile(file);
            if (asm == null) return null;

            Type type = asm.GetType(className);
            if (type == null) return null;

            FieldInfo field = type.GetField("Instance");
            if (field == null) return Activator.CreateInstance(type) as DbProviderFactory;

            return field.GetValue(null) as DbProviderFactory;
        }
        #endregion

        #region 分页
        /// <summary>
        /// 构造分页SQL，优先选择max/min，然后选择not in
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
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

        /// <summary>
        /// 按唯一数字最大最小分析
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <param name="keyColumn">唯一键。用于not in分页</param>
        /// <returns>分页SQL</returns>
        public static String PageSplitMaxMin(String sql, Int32 startRowIndex, Int32 maximumRows, String keyColumn)
        {
            // 唯一键的顺序。默认为Empty，可以为asc或desc，如果有，则表明主键列是数字唯一列，可以使用max/min分页法
            Boolean isAscOrder = keyColumn.ToLower().EndsWith(" asc");
            // 是否使用max/min分页法
            Boolean canMaxMin = false;

            // 如果sql最外层有排序，且唯一的一个排序字段就是keyColumn时，可用max/min分页法
            // 如果sql最外层没有排序，其排序不是unknown，可用max/min分页法
            MatchCollection ms = reg_Order.Matches(sql);
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

        private static Regex reg_SimpleSQL = new Regex(@"^\s*select\s+\*\s+from\s+([\w\[\]\""\""\']+)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        /// <summary>
        /// 检查简单SQL语句，比如Select * From table
        /// </summary>
        /// <param name="sql">待检查SQL语句</param>
        /// <returns>如果是简单SQL语句则返回表名，否则返回子查询(sql) XCode_Temp_a</returns>
        internal protected static String CheckSimpleSQL(String sql)
        {
            if (String.IsNullOrEmpty(sql)) return sql;

            MatchCollection ms = reg_SimpleSQL.Matches(sql);
            if (ms == null || ms.Count < 1 || ms[0].Groups.Count < 2 ||
                String.IsNullOrEmpty(ms[0].Groups[1].Value)) return String.Format("({0}) XCode_Temp_a", sql);
            return ms[0].Groups[1].Value;
        }

        private static Regex reg_Order = new Regex(@"\border\s*by\b([^)]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        /// <summary>
        /// 检查是否以Order子句结尾，如果是，分割sql为前后两部分
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        internal protected static String CheckOrderClause(ref String sql)
        {
            if (!sql.ToLower().Contains("order")) return null;

            // 使用正则进行严格判断。必须包含Order By，并且它右边没有右括号)，表明有order by，且不是子查询的，才需要特殊处理
            MatchCollection ms = reg_Order.Matches(sql);
            if (ms == null || ms.Count < 1 || ms[0].Index < 1) return null;
            String orderBy = sql.Substring(ms[0].Index).Trim();
            sql = sql.Substring(0, ms[0].Index).Trim();

            return orderBy;
        }

        /// <summary>
        /// 构造分页SQL
        /// </summary>
        /// <param name="builder">查询生成器</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
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

        #region 数据库特性
        /// <summary>
        /// 当前时间函数
        /// </summary>
        public virtual String DateTimeNow { get { return "now()"; } }

        /// <summary>
        /// 最小时间
        /// </summary>
        public virtual DateTime DateTimeMin { get { return DateTime.MinValue; } }

        /// <summary>
        /// 长文本长度
        /// </summary>
        public virtual Int32 LongTextLength { get { return 4000; } }

        protected virtual String ReservedWordsStr { get { return null; } }

        private List<String> _ReservedWords = null;
        /// <summary>
        /// 保留字
        /// </summary>
        public virtual List<String> ReservedWords
        {
            get
            {
                if (_ReservedWords == null)
                {
                    _ReservedWords = new List<String>((ReservedWordsStr + "").Split(','));
                }
                return _ReservedWords;
            }
        }

        /// <summary>
        /// 格式化时间为SQL字符串
        /// </summary>
        /// <param name="dateTime">时间值</param>
        /// <returns></returns>
        public virtual String FormatDateTime(DateTime dateTime)
        {
            return String.Format("'{0:yyyy-MM-dd HH:mm:ss}'", dateTime);
        }

        /// <summary>
        /// 格式化关键字
        /// </summary>
        /// <param name="keyWord">表名</param>
        /// <returns></returns>
        public virtual String FormatKeyWord(String keyWord)
        {
            return keyWord;
        }

        /// <summary>
        /// 格式化名称，如果是关键字，则原样返回
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual String FormatName(String name)
        {
            if (String.IsNullOrEmpty(name)) return name;

            if (ReservedWords.Contains(name))
                return FormatKeyWord(name);

            return name;
        }

        /// <summary>
        /// 格式化数据为SQL数据
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual String FormatValue(XField field, Object value)
        {
            //if (field.DataType == typeof(DateTime)) return FormatDateTime(Convert.ToDateTime(value));

            //return value == null ? null : value.ToString();

            TypeCode code = Type.GetTypeCode(field.DataType);
            Boolean isNullable = field.Nullable;

            //// 类型处理。如果数据不是目标字段类型，则先转为目标字段类型
            //if (value != null && value.GetType() != field.DataType) value = Convert.ChangeType(value, field.DataType);

            if (code == TypeCode.String)
            {
                if (value == null) return isNullable ? "null" : "''";
                if (String.IsNullOrEmpty(value.ToString()) && isNullable) return "null";
                return "'" + value.ToString().Replace("'", "''") + "'";
            }
            else if (code == TypeCode.DateTime)
            {
                if (value == null) return isNullable ? "null" : "''";
                DateTime dt = Convert.ToDateTime(value);

                //if (Meta.DbType == DatabaseType.Access) return "#" + dt.ToString("yyyy-MM-dd HH:mm:ss") + "#";
                //if (Meta.DbType == DatabaseType.Access) return Meta.FormatDateTime(dt);

                //if (Meta.DbType == DatabaseType.Oracle)
                //    return String.Format("To_Date('{0}', 'YYYYMMDDHH24MISS')", dt.ToString("yyyyMMddhhmmss"));
                // SqlServer拒绝所有其不能识别为 1753 年到 9999 年间的日期的值
                //if (Meta.DbType == DatabaseType.SqlServer)// || Meta.DbType == DatabaseType.SqlServer2005)
                {
                    if (dt < DateTimeMin || dt > DateTime.MaxValue) return isNullable ? "null" : "''";
                }
                if ((dt == DateTime.MinValue || dt == DateTimeMin) && isNullable) return "null";
                //return "'" + dt.ToString("yyyy-MM-dd HH:mm:ss") + "'";
                return FormatDateTime(dt);
            }
            //else if (typeName.Contains("Boolean"))
            else if (code == TypeCode.Boolean)
            {
                if (value == null) return isNullable ? "null" : "";
                //if (Meta.DbType == DatabaseType.SqlServer || Meta.DbType == DatabaseType.SqlServer2005)
                //    return Convert.ToBoolean(obj) ? "1" : "0";
                //else
                //    return obj.ToString();

                //if (Meta.DbType == DatabaseType.Access)
                //    return obj.ToString();
                //else
                return Convert.ToBoolean(value) ? "1" : "0";
            }
            else if (field.DataType == typeof(Byte[]))
            {
                Byte[] bts = (Byte[])value;
                if (bts == null || bts.Length < 1) return "0x0";

                return "0x" + BitConverter.ToString(bts).Replace("-", null);
            }
            else
            {
                if (value == null) return isNullable ? "null" : "";
                return value.ToString();
            }
        }
        #endregion

        #region 辅助函数
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ConnName;
        }

        /// <summary>
        /// 除去字符串两端成对出现的符号
        /// </summary>
        /// <param name="str"></param>
        /// <param name="prefix"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public static String Trim(String str, String prefix, String suffix)
        {
            while (!String.IsNullOrEmpty(str))
            {
                if (!str.StartsWith(prefix)) return str;
                if (!str.EndsWith(suffix)) return str;

                str = str.Substring(prefix.Length, str.Length - suffix.Length - prefix.Length);
            }
            return str;
        }
        #endregion

        #region Sql日志输出
        /// <summary>
        /// 是否调试
        /// </summary>
        public static Boolean Debug { get { return DAL.Debug; } }

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