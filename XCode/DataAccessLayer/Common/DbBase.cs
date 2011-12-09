using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.OleDb;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using NewLife;
using NewLife.IO;
using NewLife.Reflection;
using NewLife.Web;

namespace XCode.DataAccessLayer
{
    /// <summary>数据库基类</summary>
    /// <remarks>
    /// 数据库类的职责是抽象不同数据库的共同点，理应最小化，保证原汁原味，因此不做缓存等实现。
    /// 对于每一个连接字符串配置，都有一个数据库实例，而不是每个数据库类型一个实例，因为同类型数据库不同版本行为不同。
    /// </remarks>
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

        #region 常量
        protected static class _
        {
            public static readonly String DataSource = "Data Source";
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
        public String ConnName { get { return _ConnName; } set { _ConnName = value; } }

        private String _ConnectionString;
        /// <summary>链接字符串</summary>
        public virtual String ConnectionString
        {
            get
            {
                if (_ConnectionString == null) _ConnectionString = DefaultConnectionString;
                return _ConnectionString;
            }
            set
            {
                XDbConnectionStringBuilder builder = new XDbConnectionStringBuilder();
                builder.ConnectionString = value;

                OnSetConnectionString(builder);

                _ConnectionString = builder.ConnectionString;
            }
        }

        protected virtual String DefaultConnectionString { get { return String.Empty; } }

        const String KEY_OWNER = "Owner";
        /// <summary>
        /// 设置连接字符串时允许从中取值或修改，基类用于读取拥有者Owner，子类重写时应调用基类
        /// </summary>
        /// <param name="builder"></param>
        protected virtual void OnSetConnectionString(XDbConnectionStringBuilder builder)
        {
            String value;
            if (builder.TryGetAndRemove(KEY_OWNER, out value) && !String.IsNullOrEmpty(value)) Owner = value;
        }

        private String _Owner;
        /// <summary>拥有者</summary>
        public virtual String Owner { get { return _Owner; } set { _Owner = value; } }

        private String _ServerVersion;
        /// <summary>数据库服务器版本</summary>
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

                //// 检查数据库架构。在这里检查，避免线程冲突
                //DatabaseSchema.Create(this).CheckDatabaseOnce();

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

            if (!Runtime.IsWeb)
                file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file);
            else
                file = Path.Combine(HttpRuntime.BinDirectory, file);

            if (!File.Exists(file))
            {
                // 从网上下载文件
                try
                {
                    #region 检测64位平台
                    Module module = typeof(Object).Module;

                    PortableExecutableKinds kind;
                    ImageFileMachine machine;
                    module.GetPEKind(out kind, out machine);

                    if (machine != ImageFileMachine.I386) file += "64";
                    #endregion

                    String url = String.Format("http://nnhy.org/j?id=3&f={0}", Path.GetFileName(file + ".zip"));
                    DAL.WriteLog("准备从{0}下载相关文件！", url);
                    WebClientX client = new WebClientX();
                    // 同步下载，3秒超时
                    client.Timeout = 3000;
                    Byte[] data = client.DownloadData(url);
                    client.Dispose();

                    String dir = Path.GetDirectoryName(file);
                    DAL.WriteLog("下载完成，准备解压到{0}！", dir);
                    MemoryStream ms = new MemoryStream(data);
                    if (file.EndsWith("64")) file = file.Substring(0, file.Length - 2);
                    IOHelper.DecompressFile(ms, dir, file, false);
                    DAL.WriteLog("解压完成！");
                }
                catch (Exception ex)
                {
                    DAL.WriteLog(ex.ToString());
                }

                // 如果还没有，就写异常
                if (!File.Exists(file)) throw new FileNotFoundException("缺少文件" + file + "！", file);
            }

            Assembly asm = Assembly.LoadFile(file);
            if (asm == null) return null;

            Type type = asm.GetType(className);
            if (type == null) return null;

            FieldInfo field = type.GetField("Instance");
            if (field == null) return Activator.CreateInstance(type) as DbProviderFactory;

            return field.GetValue(null) as DbProviderFactory;
        }

        /// <summary>
        /// 是否支持该提供者所描述的数据库
        /// </summary>
        /// <param name="providerName">提供者</param>
        /// <returns></returns>
        public virtual Boolean Support(String providerName) { return !String.IsNullOrEmpty(providerName) && providerName.ToLower().Contains(this.DbType.ToString().ToLower()); }
        #endregion

        #region 分页
        /// <summary>
        /// 构造分页SQL，优先选择max/min，然后选择not in
        /// </summary>
        /// <remarks>
        /// 两个构造分页SQL的方法，区别就在于查询生成器能够构造出来更好的分页语句，尽可能的避免子查询。
        /// MS体系的分页精髓就在于唯一键，当唯一键带有Asc/Desc/Unkown等排序结尾时，就采用最大最小值分页，否则使用较次的TopNotIn分页。
        /// TopNotIn分页和MaxMin分页的弊端就在于无法完美的支持GroupBy查询分页，只能查到第一页，往后分页就不行了，因为没有主键。
        /// </remarks>
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

                    // 如果不能使用最大最小值分页，则砍掉排序，为TopNotIn分页做准备
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
                #region 有OrderBy
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
                #endregion
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

        /// <summary>构造分页SQL</summary>
        /// <remarks>
        /// 两个构造分页SQL的方法，区别就在于查询生成器能够构造出来更好的分页语句，尽可能的避免子查询。
        /// MS体系的分页精髓就在于唯一键，当唯一键带有Asc/Desc/Unkown等排序结尾时，就采用最大最小值分页，否则使用较次的TopNotIn分页。
        /// TopNotIn分页和MaxMin分页的弊端就在于无法完美的支持GroupBy查询分页，只能查到第一页，往后分页就不行了，因为没有主键。
        /// </remarks>
        /// <param name="builder">查询生成器</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>分页SQL</returns>
        public virtual String PageSplit(SelectBuilder builder, Int32 startRowIndex, Int32 maximumRows)
        {
            // 从第一行开始，不需要分页
            if (startRowIndex <= 0 && maximumRows < 1) return builder.ToString();

            return PageSplit(builder.ToString(), startRowIndex, maximumRows, builder.Key);
        }
        #endregion

        #region 数据库特性
        /// <summary>当前时间函数</summary>
        public virtual String DateTimeNow { get { return null; } }

        /// <summary>最小时间</summary>
        public virtual DateTime DateTimeMin { get { return DateTime.MinValue; } }

        /// <summary>长文本长度</summary>
        public virtual Int32 LongTextLength { get { return 4000; } }

        ///// <summary>
        ///// 保留字字符串，其实可以在首次使用时动态从Schema中加载
        ///// </summary>
        //protected virtual String ReservedWordsStr { get { return null; } }

        //private Dictionary<String, Boolean> _ReservedWords = null;
        ///// <summary>
        ///// 保留字
        ///// </summary>
        //private Dictionary<String, Boolean> ReservedWords
        //{
        //    get
        //    {
        //        if (_ReservedWords == null)
        //        {
        //            _ReservedWords = new Dictionary<String, Boolean>(StringComparer.OrdinalIgnoreCase);
        //            String[] ss = (ReservedWordsStr + "").Split(',');
        //            foreach (String item in ss)
        //            {
        //                String key = item.Trim();
        //                if (!_ReservedWords.ContainsKey(key)) _ReservedWords.Add(key, true);
        //            }
        //        }
        //        return _ReservedWords;
        //    }
        //}

        ///// <summary>
        ///// 是否保留字
        ///// </summary>
        ///// <param name="word"></param>
        ///// <returns></returns>
        //private Boolean IsReservedWord(String word) { return String.IsNullOrEmpty(word) ? false : ReservedWords.ContainsKey(word); }

        /// <summary>
        /// 格式化时间为SQL字符串
        /// </summary>
        /// <param name="dateTime">时间值</param>
        /// <returns></returns>
        public virtual String FormatDateTime(DateTime dateTime) { return String.Format("'{0:yyyy-MM-dd HH:mm:ss}'", dateTime); }

        /// <summary>
        /// 格式化关键字
        /// </summary>
        /// <param name="keyWord">表名</param>
        /// <returns></returns>
        public virtual String FormatKeyWord(String keyWord) { return keyWord; }

        /// <summary>
        /// 格式化名称，如果是关键字，则格式化后返回，否则原样返回
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual String FormatName(String name)
        {
            if (String.IsNullOrEmpty(name)) return name;

            //if (CreateMetaData().ReservedWords.Contains(name)) return FormatKeyWord(name);
            DbMetaData md = CreateMetaData() as DbMetaData;
            if (md != null && md.ReservedWords.Contains(name)) return FormatKeyWord(name);

            return name;
        }

        /// <summary>
        /// 格式化数据为SQL数据
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual String FormatValue(IDataColumn field, Object value)
        {
            Boolean isNullable = true;
            Type type = null;
            if (field != null)
            {
                type = field.DataType;
                isNullable = field.Nullable;
            }
            else if (value != null)
                type = value.GetType();

            TypeCode code = Type.GetTypeCode(type);

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

                if (dt < DateTimeMin || dt > DateTime.MaxValue) return isNullable ? "null" : "''";

                if ((dt == DateTime.MinValue || dt == DateTimeMin) && isNullable) return "null";

                return FormatDateTime(dt);
            }
            else if (code == TypeCode.Boolean)
            {
                if (value == null) return isNullable ? "null" : "";
                return Convert.ToBoolean(value) ? "1" : "0";
            }
            else if (type == typeof(Byte[]))
            {
                Byte[] bts = (Byte[])value;
                if (bts == null || bts.Length < 1) return "0x0";

                return "0x" + BitConverter.ToString(bts).Replace("-", null);
            }
            else if (field.DataType == typeof(Guid))
            {
                if (value == null) return isNullable ? "null" : "''";

                return String.Format("'{0}'", value);
            }
            else
            {
                if (value == null) return isNullable ? "null" : "";

                // 转为目标类型，比如枚举转为数字
                value = TypeX.ChangeType(value, type);
                if (value == null) return isNullable ? "null" : "";

                return value.ToString();
            }
        }

        /// <summary>
        /// 格式化标识列，返回插入数据时所用的表达式，如果字段本身支持自增，则返回空
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual String FormatIdentity(IDataColumn field, Object value) { return null; }

        /// <summary>
        /// 是否Unicode编码。只是固定判断n开头的几个常见类型为Unicode编码，这种方法不是很严谨，可以考虑读取DataTypes架构
        /// </summary>
        /// <param name="rawType"></param>
        /// <returns></returns>
        internal protected virtual Boolean IsUnicode(String rawType)
        {
            if (String.IsNullOrEmpty(rawType)) return false;

            rawType = rawType.ToLower();
            if (rawType.StartsWith("nchar") || rawType.StartsWith("nvarchar") || rawType.StartsWith("ntext") || rawType.StartsWith("nclob")) return true;

            return false;
        }

        /// <summary>字符串相加</summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public virtual String StringConcat(String left, String right) { return (!String.IsNullOrEmpty(left) ? left : "\"\"") + "+" + (!String.IsNullOrEmpty(right) ? right : "\"\""); }
        #endregion

        #region 辅助函数
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("[{0}] {1} {2}", ConnName, DbType, ServerVersion);
        }

        protected static String ResolveFile(String file)
        {
            if (String.IsNullOrEmpty(file)) return file;

            if (file.StartsWith("~/") || file.StartsWith("~\\"))
            {
                file = file.Replace("/", "\\").Replace("~\\", AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\') + "\\");
            }
            else if (file.StartsWith("./") || file.StartsWith(".\\"))
            {
                file = file.Replace("/", "\\").Replace(".\\", AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\') + "\\");
            }
            else if (!Path.IsPathRooted(file))
            {
                file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file.Replace("/", "\\"));
            }
            // 过滤掉不必要的符号
            file = new FileInfo(file).FullName;

            return file;
        }
        #endregion
    }
}