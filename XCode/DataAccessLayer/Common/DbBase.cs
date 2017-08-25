using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using NewLife;
using NewLife.Log;
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
        static DbBase()
        {
#if !__CORE__
            var root = Runtime.IsWeb ? HttpRuntime.BinDirectory : AppDomain.CurrentDomain.BaseDirectory;
#else
            var root = AppDomain.CurrentDomain.BaseDirectory;
#endif

            // 根据进程版本，设定x86或者x64为DLL目录
            var dir = root.CombinePath(!Runtime.Is64BitProcess ? "x86" : "x64");
            //if (Directory.Exists(dir)) SetDllDirectory(dir);
            // 不要判断是否存在，因为可能目录还不存在，一会下载驱动后将创建目录
            if (!Runtime.Mono) SetDllDirectory(dir);

            root = NewLife.Setting.Current.GetPluginPath();
            dir = root.CombinePath(!Runtime.Is64BitProcess ? "x86" : "x64");
            if (!Runtime.Mono) SetDllDirectory(dir);
        }

        /// <summary>销毁资源时，回滚未提交事务，并关闭数据库连接</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            if (_sessions != null) ReleaseSession();

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

        /// <summary>释放所有会话</summary>
        internal void ReleaseSession()
        {
            var ss = _sessions;
            if (ss != null)
            {
                // 不要清空，否则可能引起CreateSession中的_sessions[tid] = session;报null异常
                //_sessions = null;

                List<IDbSession> list = null;
                // 销毁本数据库的所有数据库会话
                // 复制后再销毁，避免销毁导致异常，也降低加锁时间避免死锁
                lock (ss)
                {
                    list = ss.Values.ToList();
                    ss.Clear();
                }
                foreach (var item in list)
                {
                    try
                    {
                        if (item != null) item.Dispose();
                    }
                    catch { }
                }
            }
        }
        #endregion

        #region 常量
        protected static class _
        {
            public static readonly String DataSource = "Data Source";
            public static readonly String Owner = "Owner";
            public static readonly String ShowSQL = "ShowSQL";
            public static readonly String UserParameter = "UserParameter";
            public static readonly String Migration = "Migration";
        }
        #endregion

        #region 属性
        /// <summary>返回数据库类型。外部DAL数据库类请使用Other</summary>
        public virtual DatabaseType Type { get { return DatabaseType.None; } }

        /// <summary>工厂</summary>
        public abstract DbProviderFactory Factory { get; }

        /// <summary>连接名</summary>
        public String ConnName { get; set; }

        private String _ConnectionString;
        /// <summary>链接字符串</summary>
        public virtual String ConnectionString
        {
            get
            {
                //if (_ConnectionString == null) _ConnectionString = DefaultConnectionString;
                return _ConnectionString;
            }
            set
            {
#if DEBUG
                XTrace.WriteLine("{0} 设定 {1}", ConnName, value);
#endif
                var builder = new XDbConnectionStringBuilder
                {
                    ConnectionString = value
                };

                OnSetConnectionString(builder);

                // 只有连接字符串改变，才释放会话
                var connStr = builder.ConnectionString;
#if DEBUG
                XTrace.WriteLine("{0} 格式 {1}", ConnName, connStr);
#endif
                if (_ConnectionString != connStr)
                {
                    _ConnectionString = connStr;

                    ReleaseSession();
                }
            }
        }

        protected void CheckConnStr()
        {
            if (ConnectionString.IsNullOrWhiteSpace())
                throw new XCodeException("[{0}]未指定连接字符串！", ConnName);
        }

        protected virtual String DefaultConnectionString { get { return String.Empty; } }

        /// <summary>设置连接字符串时允许从中取值或修改，基类用于读取拥有者Owner，子类重写时应调用基类</summary>
        /// <param name="builder"></param>
        protected virtual void OnSetConnectionString(XDbConnectionStringBuilder builder)
        {
            if (builder.TryGetAndRemove(_.Owner, out var value) && !String.IsNullOrEmpty(value)) Owner = value;
            if (builder.TryGetAndRemove(_.ShowSQL, out value) && !String.IsNullOrEmpty(value)) ShowSQL = value.ToBoolean();
            if (builder.TryGetAndRemove(_.UserParameter, out value) && !String.IsNullOrEmpty(value)) UserParameter = value.ToBoolean();
            if (builder.TryGetAndRemove(_.Migration, out value) && !String.IsNullOrEmpty(value)) Migration = (Migration)Enum.Parse(typeof(Migration), value, true);
        }

        /// <summary>拥有者</summary>
        public virtual String Owner { get; set; }

        private String _ServerVersion;
        /// <summary>数据库服务器版本</summary>
        public virtual String ServerVersion
        {
            get
            {
                if (_ServerVersion != null) return _ServerVersion;
                _ServerVersion = String.Empty;

                var session = CreateSession();
                if (!session.Opened) session.Open();
                try
                {
                    _ServerVersion = session.Conn.ServerVersion;

                    return _ServerVersion;
                }
                finally { session.AutoClose(); }
            }
        }

        /// <summary>反向工程。Off 关闭；ReadOnly 只读不执行；On 打开，新建；Full 完全，修改删除</summary>
        public Migration Migration { get; set; } = Migration.On;
        #endregion

        #region 方法
        /// <summary>保证数据库在每一个线程都有唯一的一个实例</summary>
        private Dictionary<Int32, IDbSession> _sessions;

        /// <summary>创建数据库会话，数据库在每一个线程都有唯一的一个实例</summary>
        /// <returns></returns>
        public IDbSession CreateSession()
        {
            if (_sessions == null) _sessions = new Dictionary<Int32, IDbSession>();

            var tid = Thread.CurrentThread.ManagedThreadId;
            // 会话可能已经被销毁
            if (_sessions.TryGetValue(tid, out var session) && session != null && !session.Disposed) return session;
            lock (_sessions)
            {
                if (_sessions.TryGetValue(tid, out session) && session != null && !session.Disposed) return session;

                session = OnCreateSession();

                CheckConnStr();
                session.ConnectionString = ConnectionString;

                _sessions[tid] = session;

                return session;
            }
        }

        /// <summary>创建数据库会话</summary>
        /// <returns></returns>
        protected abstract IDbSession OnCreateSession();

        /// <summary>唯一实例</summary>
        private IMetaData _metadata;

        /// <summary>创建元数据对象，唯一实例</summary>
        /// <returns></returns>
        public IMetaData CreateMetaData()
        {
            if (_metadata != null && !_metadata.Disposed) return _metadata;
            lock (this)
            {
                if (_metadata != null && !_metadata.Disposed) return _metadata;

                _metadata = OnCreateMetaData();
                // 减少一步类型转换
                if (_metadata is DbMetaData meta) meta.Database = this;

                return _metadata;
            }
        }

        /// <summary>创建元数据对象</summary>
        /// <returns></returns>
        protected abstract IMetaData OnCreateMetaData();

        /// <summary>是否支持该提供者所描述的数据库</summary>
        /// <param name="providerName">提供者</param>
        /// <returns></returns>
        public virtual Boolean Support(String providerName) { return !String.IsNullOrEmpty(providerName) && providerName.ToLower().Contains(Type.ToString().ToLower()); }
        #endregion

        #region 下载驱动
        /// <summary>获取提供者工厂</summary>
        /// <param name="assemblyFile"></param>
        /// <param name="className"></param>
        /// <returns></returns>
        protected static DbProviderFactory GetProviderFactory(String assemblyFile, String className)
        {
            var name = Path.GetFileNameWithoutExtension(assemblyFile);
            var linkName = name;
            if (Runtime.Is64BitProcess) linkName += "64";
            var ver = Environment.Version;
            if (ver.Major >= 4) linkName += "Fx" + ver.Major + ver.Minor;
            // 有些数据库驱动不区分x86/x64，并且逐步以Fx4为主，所以来一个默认
            linkName += ";" + name;

            var type = PluginHelper.LoadPlugin(className, null, assemblyFile, linkName);

            // 反射实现获取数据库工厂
            var file = assemblyFile;
            file = NewLife.Setting.Current.GetPluginPath().CombinePath(file);

            // 如果还没有，就写异常
            if (type == null && !File.Exists(file)) throw new FileNotFoundException("缺少文件" + file + "！", file);

            if (type == null)
            {
                XTrace.WriteLine("驱动文件{0}无效或不适用于当前环境，准备删除后重新下载！", assemblyFile);

                try
                {
                    File.Delete(file);
                }
                catch (UnauthorizedAccessException) { }
                catch (Exception ex) { XTrace.Log.Error(ex.ToString()); }

                type = PluginHelper.LoadPlugin(className, null, file, linkName);

                // 如果还没有，就写异常
                if (!File.Exists(file)) throw new FileNotFoundException("缺少文件" + file + "！", file);
            }
            if (type == null) return null;

            var asm = type.Assembly;
            if (DAL.Debug) DAL.WriteLog("{2}驱动{0} 版本v{1}", asm.Location, asm.GetName().Version, className.TrimEnd("Client", "Factory"));

            var field = type.GetFieldEx("Instance");
            if (field == null) return Activator.CreateInstance(type) as DbProviderFactory;

            return Reflect.GetValue(null, field) as DbProviderFactory;
        }

        [DllImport("kernel32.dll")]
        static extern Int32 SetDllDirectory(String pathName);
        #endregion

        #region 分页
        /// <summary>构造分页SQL，优先选择max/min，然后选择not in</summary>
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
        public virtual String PageSplit(String sql, Int64 startRowIndex, Int64 maximumRows, String keyColumn)
        {
            // 从第一行开始，不需要分页
            if (startRowIndex <= 0 && maximumRows < 1) return sql;

            #region Max/Min分页
            // 如果要使用max/min分页法，首先keyColumn必须有asc或者desc
            if (!String.IsNullOrEmpty(keyColumn))
            {
                var kc = keyColumn.ToLower();
                if (kc.EndsWith(" desc") || kc.EndsWith(" asc") || kc.EndsWith(" unknown"))
                {
                    var str = PageSplitMaxMin(sql, startRowIndex, maximumRows, keyColumn);
                    if (!String.IsNullOrEmpty(str)) return str;

                    // 如果不能使用最大最小值分页，则砍掉排序，为TopNotIn分页做准备
                    keyColumn = keyColumn.Substring(0, keyColumn.IndexOf(" "));
                }
            }
            #endregion

            //检查简单SQL。为了让生成分页SQL更短
            var tablename = CheckSimpleSQL(sql);
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

        /// <summary>按唯一数字最大最小分析</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <param name="keyColumn">唯一键。用于not in分页</param>
        /// <returns>分页SQL</returns>
        public static String PageSplitMaxMin(String sql, Int64 startRowIndex, Int64 maximumRows, String keyColumn)
        {
            // 唯一键的顺序。默认为Empty，可以为asc或desc，如果有，则表明主键列是数字唯一列，可以使用max/min分页法
            var isAscOrder = keyColumn.ToLower().EndsWith(" asc");
            // 是否使用max/min分页法
            var canMaxMin = false;

            // 如果sql最外层有排序，且唯一的一个排序字段就是keyColumn时，可用max/min分页法
            // 如果sql最外层没有排序，其排序不是unknown，可用max/min分页法
            var ms = reg_Order.Matches(sql);
            if (ms != null && ms.Count > 0 && ms[0].Index > 0)
            {
                #region 有OrderBy
                // 取第一页也不用分页。把这代码放到这里，主要是数字分页中要自己处理这种情况
                if (startRowIndex <= 0 && maximumRows > 0)
                    return String.Format("Select Top {0} * From {1}", maximumRows, CheckSimpleSQL(sql));

                keyColumn = keyColumn.Substring(0, keyColumn.IndexOf(" "));
                sql = sql.Substring(0, ms[0].Index);

                var strOrderBy = ms[0].Groups[1].Value.Trim();
                // 只有一个排序字段
                if (!String.IsNullOrEmpty(strOrderBy) && !strOrderBy.Contains(","))
                {
                    // 有asc或者desc。没有时，默认为asc
                    if (strOrderBy.ToLower().EndsWith(" desc"))
                    {
                        var str = strOrderBy.Substring(0, strOrderBy.Length - " desc".Length).Trim();
                        // 排序字段等于keyColumn
                        if (str.ToLower() == keyColumn.ToLower())
                        {
                            isAscOrder = false;
                            canMaxMin = true;
                        }
                    }
                    else if (strOrderBy.ToLower().EndsWith(" asc"))
                    {
                        var str = strOrderBy.Substring(0, strOrderBy.Length - " asc".Length).Trim();
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
        /// <summary>检查简单SQL语句，比如Select * From table</summary>
        /// <param name="sql">待检查SQL语句</param>
        /// <returns>如果是简单SQL语句则返回表名，否则返回子查询(sql) XCode_Temp_a</returns>
        internal protected static String CheckSimpleSQL(String sql)
        {
            if (String.IsNullOrEmpty(sql)) return sql;

            var ms = reg_SimpleSQL.Matches(sql);
            if (ms == null || ms.Count < 1 || ms[0].Groups.Count < 2 ||
                String.IsNullOrEmpty(ms[0].Groups[1].Value)) return String.Format("({0}) XCode_Temp_a", sql);
            return ms[0].Groups[1].Value;
        }

        private static Regex reg_Order = new Regex(@"\border\s*by\b([^)]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        /// <summary>检查是否以Order子句结尾，如果是，分割sql为前后两部分</summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        internal protected static String CheckOrderClause(ref String sql)
        {
            if (!sql.ToLower().Contains("order")) return null;

            // 使用正则进行严格判断。必须包含Order By，并且它右边没有右括号)，表明有order by，且不是子查询的，才需要特殊处理
            var ms = reg_Order.Matches(sql);
            if (ms == null || ms.Count < 1 || ms[0].Index < 1) return null;
            var orderBy = sql.Substring(ms[0].Index).Trim();
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
        public virtual SelectBuilder PageSplit(SelectBuilder builder, Int64 startRowIndex, Int64 maximumRows)
        {
            // 从第一行开始，不需要分页
            if (startRowIndex <= 0 && maximumRows < 1) return builder;

            var sql = PageSplit(builder.ToString(), startRowIndex, maximumRows, builder.Key);
            var sb = new SelectBuilder();
            sb.Parse(sql);
            return sb;
        }
        #endregion

        #region 数据库特性
        /// <summary>长文本长度</summary>
        public virtual Int32 LongTextLength { get { return 4000; } }

        /// <summary>
        /// 保留字字符串，其实可以在首次使用时动态从Schema中加载
        /// </summary>
        protected virtual String ReservedWordsStr { get { return null; } }

        private Dictionary<String, Boolean> _ReservedWords = null;
        /// <summary>
        /// 保留字
        /// </summary>
        private Dictionary<String, Boolean> ReservedWords
        {
            get
            {
                if (_ReservedWords == null)
                {
                    var dic = new Dictionary<String, Boolean>(StringComparer.OrdinalIgnoreCase);
                    var ss = (ReservedWordsStr + "").Split(',');
                    foreach (var item in ss)
                    {
                        var key = item.Trim();
                        if (!dic.ContainsKey(key)) dic.Add(key, true);
                    }
                    _ReservedWords = dic;
                }
                return _ReservedWords;
            }
        }

        /// <summary>
        /// 是否保留字
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        internal Boolean IsReservedWord(String word) { return !String.IsNullOrEmpty(word) && ReservedWords.ContainsKey(word); }

        /// <summary>格式化时间为SQL字符串</summary>
        /// <remarks>
        /// 优化DateTime转为全字符串，平均耗时从25.76ns降为15.07。
        /// 调用非常频繁，每分钟都有数百万次调用。
        /// </remarks>
        /// <param name="dateTime">时间值</param>
        /// <returns></returns>
        public virtual String FormatDateTime(DateTime dateTime) { return "'" + dateTime.ToFullString() + "'"; }

        /// <summary>格式化关键字</summary>
        /// <param name="keyWord">表名</param>
        /// <returns></returns>
        public virtual String FormatKeyWord(String keyWord) { return keyWord; }

        /// <summary>格式化名称，如果是关键字，则格式化后返回，否则原样返回</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual String FormatName(String name)
        {
            if (String.IsNullOrEmpty(name)) return name;

            //if (CreateMetaData().ReservedWords.Contains(name)) return FormatKeyWord(name);
            if (CreateMetaData() is DbMetaData md && md.ReservedWords.Contains(name)) return FormatKeyWord(name);

            if (IsReservedWord(name)) return FormatKeyWord(name);

            return name;
        }

        /// <summary>格式化数据为SQL数据</summary>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public virtual String FormatValue(IDataColumn field, Object value)
        {
            var isNullable = true;
            Type type = null;
            if (field != null)
            {
                type = field.DataType;
                isNullable = field.Nullable;
            }
            else if (value != null)
                type = value.GetType();

            // 枚举
            if (type.IsEnum) type = typeof(Int32);

            var code = System.Type.GetTypeCode(type);
            if (code == TypeCode.String)
            {
                if (value == null) return isNullable ? "null" : "''";
                //!!! 为SQL格式化数值时，如果字符串是Empty，将不再格式化为null
                //if (String.IsNullOrEmpty(value.ToString()) && isNullable) return "null";

                return "'" + value.ToString().Replace("'", "''") + "'";
            }
            else if (code == TypeCode.DateTime)
            {
                if (value == null) return isNullable ? "null" : "''";
                var dt = Convert.ToDateTime(value);

                if (dt <= DateTime.MinValue || dt >= DateTime.MaxValue) return isNullable ? "null" : "''";

                if ((dt == DateTime.MinValue) && isNullable) return "null";

                return FormatDateTime(dt);
            }
            else if (code == TypeCode.Boolean)
            {
                if (value == null) return isNullable ? "null" : "";
                return Convert.ToBoolean(value) ? "1" : "0";
            }
            else if (type == typeof(Byte[]))
            {
                var bts = (Byte[])value;
                if (bts == null || bts.Length < 1) return isNullable ? "null" : "0x0";

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
                value = value.ChangeType(type);
                if (value == null) return isNullable ? "null" : "";

                return value.ToString();
            }
        }

        /// <summary>格式化标识列，返回插入数据时所用的表达式，如果字段本身支持自增，则返回空</summary>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public virtual String FormatIdentity(IDataColumn field, Object value) { return null; }

        /// <summary>格式化参数名</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual String FormatParameterName(String name)
        {
            if (String.IsNullOrEmpty(name)) return name;

            //DbMetaData md = CreateMetaData() as DbMetaData;
            //if (md != null) name = md.ParamPrefix + name;

            //return name;

            return ParamPrefix + name;
        }

        internal protected virtual String ParamPrefix { get { return "@"; } }

        /// <summary>字符串相加</summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public virtual String StringConcat(String left, String right) { return (!String.IsNullOrEmpty(left) ? left : "\'\'") + "+" + (!String.IsNullOrEmpty(right) ? right : "\'\'"); }

        /// <summary>创建参数</summary>
        /// <param name="name">名称</param>
        /// <param name="value">值</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public virtual IDataParameter CreateParameter(String name, Object value, Type type = null)
        {
            if (value == null && type == null) throw new ArgumentNullException(nameof(type));

            var dp = Factory.CreateParameter();
            dp.ParameterName = FormatParameterName(name);
            dp.Value = value;

            if (type == null) type = value?.GetType();

            if (dp.DbType == DbType.AnsiString)
            {
                // 写入数据类型
                switch (type.GetTypeCode())
                {
                    case TypeCode.Boolean:
                        dp.DbType = DbType.Boolean;
                        break;
                    case TypeCode.Char:
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                        dp.DbType = DbType.Byte;
                        break;
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                        dp.DbType = DbType.Int16;
                        break;
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                        dp.DbType = DbType.Int32;
                        break;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        dp.DbType = DbType.Int64;
                        break;
                    case TypeCode.Single:
                        dp.DbType = DbType.Double;
                        break;
                    case TypeCode.Double:
                        dp.DbType = DbType.Double;
                        break;
                    case TypeCode.Decimal:
                        dp.DbType = DbType.Decimal;
                        break;
                    case TypeCode.DateTime:
                        dp.DbType = DbType.DateTime;
                        break;
                    case TypeCode.String:
                        dp.DbType = DbType.String;
                        break;
                    default:
                        break;
                }
            }

            return dp;
        }

        /// <summary>创建参数数组</summary>
        /// <param name="ps"></param>
        /// <returns></returns>
        public IDataParameter[] CreateParameters(IDictionary<String, Object> ps)
        {
            return ps.Select(e => CreateParameter(e.Key, e.Value)).ToArray();
        }
        #endregion

        #region 辅助函数
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            return String.Format("[{0}] {1} {2}", ConnName, Type, ServerVersion);
        }

        protected static String ResolveFile(String file)
        {
            if (String.IsNullOrEmpty(file)) return file;

            file = file.Replace("|DataDirectory|", @"~\App_Data");

            var sep = Path.DirectorySeparatorChar + "";
            var sep2 = sep == "/" ? "\\" : "/";
            var bpath = AppDomain.CurrentDomain.BaseDirectory.EnsureEnd(sep);
            if (file.StartsWith("~" + sep) || file.StartsWith("~" + sep2))
            {
                file = file.Replace(sep2, sep).Replace("~" + sep, bpath);
            }
            else if (file.StartsWith("." + sep) || file.StartsWith("." + sep2))
            {
                file = file.Replace(sep2, sep).Replace("." + sep, bpath);
            }
            else if (!Path.IsPathRooted(file))
            {
                file = bpath.CombinePath(file.Replace(sep2, sep));
            }
            // 过滤掉不必要的符号
            file = new FileInfo(file).FullName;

            return file;
        }
        #endregion

        #region Sql日志输出
        /// <summary>是否输出SQL语句，默认为XCode调试开关XCode.Debug</summary>
        public Boolean ShowSQL { get; set; } = Setting.Current.ShowSQL;
        #endregion

        #region 参数化
        /// <summary>参数化添删改查。默认关闭</summary>
        public Boolean UserParameter { get; set; } = Setting.Current.UserParameter;
        #endregion
    }
}