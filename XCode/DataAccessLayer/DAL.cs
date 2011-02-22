using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Xml.Serialization;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Reflection;
using XCode.Cache;
using XCode.Code;
using XCode.Exceptions;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 数据访问层。
    /// </summary>
    /// <remarks>
    /// 主要用于选择不同的数据库，不同的数据库的操作有所差别。
    /// 每一个数据库链接字符串，对应唯一的一个DAL实例。
    /// 数据库链接字符串可以写在配置文件中，然后在Create时指定名字；
    /// 也可以直接把链接字符串作为AddConnStr的参数传入。
    /// 每一个数据库操作都必须指定表名以用于管理缓存，空表名或*将匹配所有缓存
    /// </remarks>
    public class DAL
    {
        #region 创建函数
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="connName">配置名</param>
        private DAL(String connName)
        {
            _ConnName = connName;

            if (!ConnStrs.ContainsKey(connName)) throw new XCodeException("请在使用数据库前设置[" + connName + "]连接字符串");

            ConnStr = ConnStrs[connName].ConnectionString;

            try
            {
                DatabaseSchema.Check(this);
            }
            catch (Exception ex)
            {
                if (DbBase.Debug) DbBase.WriteLog(ex.ToString());
            }
        }

        private static Dictionary<String, DAL> _dals = new Dictionary<String, DAL>();
        /// <summary>
        /// 创建一个数据访问层对象。以null作为参数可获得当前默认对象
        /// </summary>
        /// <param name="connName">配置名，或链接字符串</param>
        /// <returns>对应于指定链接的全局唯一的数据访问层对象</returns>
        public static DAL Create(String connName)
        {
            if (String.IsNullOrEmpty(connName)) throw new ArgumentNullException("connName");

            DAL dal = null;
            if (_dals.TryGetValue(connName, out dal)) return dal;
            lock (_dals)
            {
                if (_dals.TryGetValue(connName, out dal)) return dal;

                ////检查数据库最大连接数授权。
                //if (License.DbConnectCount != _dals.Count + 1)
                //    License.DbConnectCount = _dals.Count + 1;

                dal = new DAL(connName);
                // 不用connName，因为可能在创建过程中自动识别了ConnName
                _dals.Add(dal.ConnName, dal);
            }

            return dal;
        }

        private static Object _connStrs_lock = new Object();
        private static Dictionary<String, ConnectionStringSettings> _connStrs;
        private static Dictionary<String, Type> _connTypes = new Dictionary<String, Type>();
        /// <summary>
        /// 链接字符串集合
        /// </summary>
        public static Dictionary<String, ConnectionStringSettings> ConnStrs
        {
            get
            {
                if (_connStrs != null) return _connStrs;
                lock (_connStrs_lock)
                {
                    if (_connStrs != null) return _connStrs;
                    Dictionary<String, ConnectionStringSettings> cs = new Dictionary<String, ConnectionStringSettings>();

                    // 读取配置文件
                    ConnectionStringSettingsCollection css = ConfigurationManager.ConnectionStrings;
                    if (css != null && css.Count > 0)
                    {
                        foreach (ConnectionStringSettings set in css)
                        {
                            if (set.Name == "LocalSqlServer") continue;
                            if (set.Name == "LocalMySqlServer") continue;
                            if (String.IsNullOrEmpty(set.ConnectionString)) continue;
                            if (String.IsNullOrEmpty(set.ConnectionString.Trim())) continue;

                            Type type = GetTypeFromConn(set.ConnectionString, set.ProviderName);
                            if (type == null) throw new XCodeException("无法识别的提供者" + set.ProviderName + "！");

                            cs.Add(set.Name, set);
                            _connTypes.Add(set.Name, type);
                        }
                    }
                    _connStrs = cs;
                }
                return _connStrs;
            }
        }

        /// <summary>
        /// 添加连接字符串
        /// </summary>
        /// <param name="connName">连接名</param>
        /// <param name="connStr">连接字符串</param>
        /// <param name="type">实现了IDatabase接口的数据库类型</param>
        /// <param name="provider">数据库提供者，如果没有指定数据库类型，则有提供者判断使用哪一种内置类型</param>
        public static void AddConnStr(String connName, String connStr, Type type, String provider)
        {
            if (String.IsNullOrEmpty(connName)) throw new ArgumentNullException("connName");

            // ConnStrs对象不可能为null，但可能没有元素
            if (ConnStrs.ContainsKey(connName)) return;
            lock (ConnStrs)
            {
                if (ConnStrs.ContainsKey(connName)) return;

                if (type == null) type = GetTypeFromConn(connStr, provider);
                if (type == null) throw new XCodeException("无法识别的提供者" + provider + "！");

                ConnectionStringSettings set = new ConnectionStringSettings(connName, connStr, provider);
                ConnStrs.Add(connName, set);
                _connTypes.Add(connName, type);
            }
        }

        /// <summary>
        /// 从提供者和连接字符串猜测数据库处理器
        /// </summary>
        /// <param name="connStr"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        private static Type GetTypeFromConn(String connStr, String provider)
        {
            Type type = null;
            if (!String.IsNullOrEmpty(provider))
            {
                provider = provider.ToLower();
                if (provider.Contains("system.data.sqlclient"))
                    type = typeof(SqlServer);
                else if (provider.Contains("oracleclient"))
                    type = typeof(Oracle);
                else if (provider.Contains("microsoft.jet.oledb"))
                    type = typeof(Access);
                else if (provider.Contains("access"))
                    type = typeof(Access);
                else if (provider.Contains("mysql"))
                    type = typeof(MySql);
                else if (provider.Contains("sqlite"))
                    type = typeof(SQLite);
                else if (provider.Contains("sqlce"))
                    type = typeof(SqlCe);
                else if (provider.Contains("sql2008"))
                    type = typeof(SqlServer);
                else if (provider.Contains("sql2005"))
                    type = typeof(SqlServer);
                else if (provider.Contains("sql2000"))
                    type = typeof(SqlServer);
                else if (provider.Contains("sql"))
                    type = typeof(SqlServer);
                else
                {
                    type = TypeX.GetType(provider, true);
                }
            }
            else
            {
                // 分析类型
                String str = connStr.ToLower();
                if (str.Contains("mssql") || str.Contains("sqloledb"))
                    type = typeof(SqlServer);
                else if (str.Contains("oracle"))
                    type = typeof(Oracle);
                else if (str.Contains("microsoft.jet.oledb"))
                    type = typeof(Access);
                else if (str.Contains("sql"))
                    type = typeof(SqlServer);
                else
                    type = typeof(Access);
            }
            return type;
        }
        #endregion

        #region 属性
        private String _ConnName;
        /// <summary>
        /// 连接名
        /// </summary>
        public String ConnName
        {
            get { return _ConnName; }
        }

        private Type _ProviderType;
        /// <summary>
        /// 实现了IDatabase接口的数据库类型
        /// </summary>
        private Type ProviderType
        {
            get
            {
                if (_ProviderType == null && _connTypes.ContainsKey(ConnName)) _ProviderType = _connTypes[ConnName];
                return _ProviderType;
            }
        }

        /// <summary>
        /// 数据库类型
        /// </summary>
        public DatabaseType DbType
        {
            get { return Db.DbType; }
        }

        private String _ConnStr;
        /// <summary>
        /// 连接字符串
        /// </summary>
        public String ConnStr
        {
            get { return _ConnStr; }
            private set { _ConnStr = value; }
        }

        private IDatabase _Db;
        /// <summary>
        /// 数据库。所有数据库操作在此统一管理，强烈建议不要直接使用该数据，在不同版本中IDatabase可能有较大改变
        /// </summary>
        public IDatabase Db
        {
            get
            {
                if (_Db != null) return _Db;

                Type type = ProviderType;
                if (type != null)
                {
                    _Db = TypeX.CreateInstance(type) as IDatabase;
                    _Db.ConnName = ConnName;
                    _Db.ConnectionString = ConnStr;
                }

                return _Db;
            }
        }

        /// <summary>
        /// 数据库会话
        /// </summary>
        [Obsolete("请改为使用Session属性！")]
        public IDbSession DB
        {
            get
            {
                return Session;
            }
        }

        /// <summary>
        /// 数据库会话
        /// </summary>
        public IDbSession Session
        {
            get
            {
                if (String.IsNullOrEmpty(ConnStr)) throw new XCodeException("请在使用数据库前设置[" + ConnName + "]连接字符串");

                return Db.CreateSession();
            }
        }
        #endregion

        #region 使用缓存后的数据操作方法
        #region 属性
        private Boolean _EnableCache = true;
        /// <summary>
        /// 是否启用缓存。
        /// <remarks>设为false可清空缓存</remarks>
        /// </summary>
        public Boolean EnableCache
        {
            get { return _EnableCache; }
            set
            {
                _EnableCache = value;
                if (!_EnableCache) XCache.RemoveAll();
            }
        }

        /// <summary>
        /// 缓存个数
        /// </summary>
        public Int32 CacheCount
        {
            get
            {
                return XCache.Count;
            }
        }

        [ThreadStatic]
        private static Int32 _QueryTimes;
        /// <summary>
        /// 查询次数
        /// </summary>
        public static Int32 QueryTimes
        {
            //get { return DB != null ? DB.QueryTimes : 0; }
            get { return _QueryTimes; }
        }

        [ThreadStatic]
        private static Int32 _ExecuteTimes;
        /// <summary>
        /// 执行次数
        /// </summary>
        public static Int32 ExecuteTimes
        {
            //get { return DB != null ? DB.ExecuteTimes : 0; }
            get { return _ExecuteTimes; }
        }
        #endregion

        private static Dictionary<String, String> _PageSplitCache = new Dictionary<String, String>();
        /// <summary>
        /// 根据条件把普通查询SQL格式化为分页SQL。
        /// </summary>
        /// <remarks>
        /// 因为需要继承重写的原因，在数据类中并不方便缓存分页SQL。
        /// 所以在这里做缓存。
        /// </remarks>
        /// <param name="sql">SQL语句</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <param name="keyColumn">唯一键。用于not in分页</param>
        /// <returns>分页SQL</returns>
        public String PageSplit(String sql, Int32 startRowIndex, Int32 maximumRows, String keyColumn)
        {
            String cacheKey = String.Format("{0}_{1}_{2}_{3}_", sql, startRowIndex, maximumRows, ConnName);
            if (!String.IsNullOrEmpty(keyColumn)) cacheKey += keyColumn;

            String rs = String.Empty;
            if (_PageSplitCache.TryGetValue(cacheKey, out rs)) return rs;
            lock (_PageSplitCache)
            {
                if (_PageSplitCache.TryGetValue(cacheKey, out rs)) return rs;

                String s = Db.PageSplit(sql, startRowIndex, maximumRows, keyColumn);
                _PageSplitCache.Add(cacheKey, s);
                return s;
            }
        }

        /// <summary>
        /// 根据条件把普通查询SQL格式化为分页SQL。
        /// </summary>
        /// <remarks>
        /// 因为需要继承重写的原因，在数据类中并不方便缓存分页SQL。
        /// 所以在这里做缓存。
        /// </remarks>
        /// <param name="builder">查询生成器</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <param name="keyColumn">唯一键。用于not in分页</param>
        /// <returns>分页SQL</returns>
        public String PageSplit(SelectBuilder builder, Int32 startRowIndex, Int32 maximumRows, String keyColumn)
        {
            String cacheKey = String.Format("{0}_{1}_{2}_{3}_", builder.ToString(), startRowIndex, maximumRows, ConnName);
            if (!String.IsNullOrEmpty(keyColumn)) cacheKey += keyColumn;

            String rs = String.Empty;
            if (_PageSplitCache.TryGetValue(cacheKey, out rs)) return rs;
            lock (_PageSplitCache)
            {
                if (_PageSplitCache.TryGetValue(cacheKey, out rs)) return rs;

                String s = Db.PageSplit(builder, startRowIndex, maximumRows, keyColumn);
                _PageSplitCache.Add(cacheKey, s);
                return s;
            }
        }

        /// <summary>
        /// 执行SQL查询，返回记录集
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="tableNames">所依赖的表的表名</param>
        /// <returns></returns>
        public DataSet Select(String sql, String[] tableNames)
        {
            String cacheKey = sql + "_" + ConnName;
            if (EnableCache && XCache.Contain(cacheKey)) return XCache.Item(cacheKey);
            Interlocked.Increment(ref _QueryTimes);
            DataSet ds = Session.Query(sql);
            if (EnableCache) XCache.Add(cacheKey, ds, tableNames);
            return ds;
        }

        /// <summary>
        /// 执行SQL查询，返回记录集
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="tableName">所依赖的表的表名</param>
        /// <returns></returns>
        public DataSet Select(String sql, String tableName)
        {
            return Select(sql, new String[] { tableName });
        }

        /// <summary>
        /// 执行SQL查询，返回分页记录集
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <param name="keyColumn">唯一键。用于not in分页</param>
        /// <param name="tableNames">所依赖的表的表名</param>
        /// <returns></returns>
        public DataSet Select(String sql, Int32 startRowIndex, Int32 maximumRows, String keyColumn, String[] tableNames)
        {
            return Select(PageSplit(sql, startRowIndex, maximumRows, keyColumn), tableNames);
        }

        /// <summary>
        /// 执行SQL查询，返回分页记录集
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <param name="keyColumn">唯一键。用于not in分页</param>
        /// <param name="tableName">所依赖的表的表名</param>
        /// <returns></returns>
        public DataSet Select(String sql, Int32 startRowIndex, Int32 maximumRows, String keyColumn, String tableName)
        {
            return Select(sql, startRowIndex, maximumRows, keyColumn, new String[] { tableName });
        }

        /// <summary>
        /// 执行SQL查询，返回总记录数
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="tableNames">所依赖的表的表名</param>
        /// <returns></returns>
        public Int32 SelectCount(String sql, String[] tableNames)
        {
            String cacheKey = sql + "_SelectCount" + "_" + ConnName;
            if (EnableCache && XCache.IntContain(cacheKey)) return XCache.IntItem(cacheKey);
            Interlocked.Increment(ref _QueryTimes);
            Int32 rs = Session.QueryCount(sql);
            if (EnableCache) XCache.Add(cacheKey, rs, tableNames);
            return rs;
        }

        /// <summary>
        /// 执行SQL查询，返回总记录数
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="tableName">所依赖的表的表名</param>
        /// <returns></returns>
        public Int32 SelectCount(String sql, String tableName)
        {
            return SelectCount(sql, new String[] { tableName });
        }

        /// <summary>
        /// 执行SQL语句，返回受影响的行数
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="tableNames">受影响的表的表名</param>
        /// <returns></returns>
        public Int32 Execute(String sql, String[] tableNames)
        {
            // 移除所有和受影响表有关的缓存
            if (EnableCache) XCache.Remove(tableNames);
            Interlocked.Increment(ref _ExecuteTimes);
            return Session.Execute(sql);
        }

        /// <summary>
        /// 执行SQL语句，返回受影响的行数
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="tableName">受影响的表的表名</param>
        /// <returns></returns>
        public Int32 Execute(String sql, String tableName)
        {
            return Execute(sql, new String[] { tableName });
        }

        /// <summary>
        /// 执行插入语句并返回新增行的自动编号
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="tableNames">受影响的表的表名</param>
        /// <returns>新增行的自动编号</returns>
        public Int64 InsertAndGetIdentity(String sql, String[] tableNames)
        {
            // 移除所有和受影响表有关的缓存
            if (EnableCache) XCache.Remove(tableNames);
            Interlocked.Increment(ref _ExecuteTimes);
            return Session.InsertAndGetIdentity(sql);
        }

        /// <summary>
        /// 执行插入语句并返回新增行的自动编号
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="tableName">受影响的表的表名</param>
        /// <returns>新增行的自动编号</returns>
        public Int64 InsertAndGetIdentity(String sql, String tableName)
        {
            return InsertAndGetIdentity(sql, new String[] { tableName });
        }

        /// <summary>
        /// 执行CMD，返回记录集
        /// </summary>
        /// <param name="cmd">CMD</param>
        /// <param name="tableNames">所依赖的表的表名</param>
        /// <returns></returns>
        public DataSet Select(DbCommand cmd, String[] tableNames)
        {
            String cacheKey = cmd.CommandText + "_" + ConnName;
            if (EnableCache && XCache.Contain(cacheKey)) return XCache.Item(cacheKey);
            Interlocked.Increment(ref _QueryTimes);
            DataSet ds = Session.Query(cmd);
            if (EnableCache) XCache.Add(cacheKey, ds, tableNames);
            Session.AutoClose();
            return ds;
        }

        /// <summary>
        /// 执行CMD，返回受影响的行数
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="tableNames"></param>
        /// <returns></returns>
        public Int32 Execute(DbCommand cmd, String[] tableNames)
        {
            // 移除所有和受影响表有关的缓存
            if (EnableCache) XCache.Remove(tableNames);
            Interlocked.Increment(ref _ExecuteTimes);
            Int32 ret = Session.Execute(cmd);
            Session.AutoClose();
            return ret;
        }

        private List<XTable> _Tables;
        /// <summary>
        /// 取得所有表和视图的构架信息
        /// </summary>
        /// <remarks>如果不存在缓存，则获取后返回；否则使用线程池线程获取，而主线程返回缓存</remarks>
        /// <returns></returns>
        public List<XTable> Tables
        {
            get
            {
                // 如果不存在缓存，则获取后返回；否则使用线程池线程获取，而主线程返回缓存
                if (_Tables == null)
                    _Tables = GetTables();
                else
                    ThreadPool.QueueUserWorkItem(delegate(Object state) { _Tables = GetTables(); });

                return _Tables;
            }
        }

        private List<XTable> GetTables()
        {
            List<XTable> list = Db.CreateMetaData().GetTables();
            if (list != null && list.Count > 0) list.Sort(delegate(XTable item1, XTable item2) { return item1.Name.CompareTo(item2.Name); });
            return list;
        }
        #endregion

        #region 事务
        /// <summary>
        /// 开始事务。
        /// 事务一旦开始，请务必在操作完成后提交或者失败时回滚，否则可能会造成资源失去控制。极度危险！
        /// </summary>
        /// <returns></returns>
        public Int32 BeginTransaction()
        {
            return Session.BeginTransaction();
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        /// <returns></returns>
        public Int32 Commit()
        {
            return Session.Commit();
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        /// <returns></returns>
        public Int32 Rollback()
        {
            return Session.Rollback();
        }
        #endregion

        #region 导入导出
        /// <summary>
        /// 导出架构信息
        /// </summary>
        /// <returns></returns>
        public String Export()
        {
            IList<XTable> list = Tables;

            if (list == null || list.Count < 1) return null;

            XmlSerializer serializer = new XmlSerializer(typeof(XTable[]));
            using (StringWriter sw = new StringWriter())
            {
                serializer.Serialize(sw, list);
                return sw.ToString();
            }
        }

        /// <summary>
        /// 导入架构信息
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static XTable[] Import(String xml)
        {
            if (String.IsNullOrEmpty(xml)) return null;

            XmlSerializer serializer = new XmlSerializer(typeof(XTable[]));
            using (StringReader sr = new StringReader(xml))
            {
                return serializer.Deserialize(sr) as XTable[];
            }
        }
        #endregion

        #region 创建数据操作实体
        /// <summary>
        /// 创建实体操作接口
        /// </summary>
        /// <remarks>因为只用来做实体操作，所以只需要一个实例即可</remarks>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public IEntityOperate CreateOperate(String tableName)
        {
            Assembly asm = EntityAssembly.Create(this);
            Type type = asm.GetType(tableName);
            if (type == null)
            {
                Type[] ts = asm.GetTypes();
                foreach (Type item in ts)
                {
                    if (item.Name == tableName)
                    {
                        type = item;
                        break;
                    }
                }

                if (type == null) return null;
            }

            return EntityFactory.CreateOperate(type);
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

        #region 辅助函数
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ConnName;
        }
        #endregion
    }
}