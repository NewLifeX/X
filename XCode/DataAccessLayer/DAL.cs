using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NewLife;
using NewLife.Collections;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Serialization;
using NewLife.Threading;

namespace XCode.DataAccessLayer
{
    /// <summary>数据访问层</summary>
    /// <remarks>
    /// 主要用于选择不同的数据库，不同的数据库的操作有所差别。
    /// 每一个数据库链接字符串，对应唯一的一个DAL实例。
    /// 数据库链接字符串可以写在配置文件中，然后在Create时指定名字；
    /// 也可以直接把链接字符串作为AddConnStr的参数传入。
    /// </remarks>
    public partial class DAL
    {
        #region 属性
        /// <summary>连接名</summary>
        public String ConnName { get; }

        /// <summary>实现了IDatabase接口的数据库类型</summary>
        public Type ProviderType { get; private set; }

        /// <summary>数据库类型</summary>
        public DatabaseType DbType { get; private set; }

        /// <summary>连接字符串</summary>
        /// <remarks>
        /// 修改连接字符串将会清空<see cref="Db"/>
        /// </remarks>
        public String ConnStr { get; private set; }

        private IDatabase _Db;
        /// <summary>数据库。所有数据库操作在此统一管理，强烈建议不要直接使用该属性，在不同版本中IDatabase可能有较大改变</summary>
        public IDatabase Db
        {
            get
            {
                if (_Db != null) return _Db;
                lock (this)
                {
                    if (_Db != null) return _Db;

                    var type = ProviderType;
                    if (type == null) throw new XCodeException("无法识别{0}的数据提供者！", ConnName);

                    //!!! 重量级更新：经常出现链接字符串为127/master的连接错误，非常有可能是因为这里线程冲突，A线程创建了实例但未来得及赋值连接字符串，就被B线程使用了
                    var db = type.CreateInstance() as IDatabase;
                    if (!ConnName.IsNullOrEmpty()) db.ConnName = ConnName;
                    if (!ConnStr.IsNullOrEmpty()) db.ConnectionString = DecodeConnStr(ConnStr);

                    _Db = db;

                    return _Db;
                }
            }
        }

        /// <summary>数据库会话</summary>
        public IDbSession Session => Db.CreateSession();
        #endregion

        #region 创建函数
        /// <summary>构造函数</summary>
        /// <param name="connName">配置名</param>
        private DAL(String connName) => ConnName = connName;

        private Boolean _inited;
        private void Init()
        {
            if (_inited) return;
            lock (this)
            {
                if (_inited) return;

                var connName = ConnName;
                var css = ConnStrs;
                //if (!css.ContainsKey(connName)) throw new XCodeException("请在使用数据库前设置[" + connName + "]连接字符串");
                if (!css.ContainsKey(connName)) GetFromConfigCenter(connName);
                if (!css.ContainsKey(connName)) OnResolve?.Invoke(this, new ResolveEventArgs(connName));
                if (!css.ContainsKey(connName))
                {
                    var cfg = NewLife.Setting.Current;
                    var set = Setting.Current;
                    var connstr = "Data Source=" + cfg.DataPath.CombinePath(connName + ".db");
                    if (set.Migration <= Migration.On) connstr += ";Migration=On";
                    WriteLog("自动为[{0}]设置SQLite连接字符串：{1}", connName, connstr);
                    AddConnStr(connName, connstr, null, "SQLite");
                }

                ConnStr = css[connName];
                if (ConnStr.IsNullOrEmpty()) throw new XCodeException("请在使用数据库前设置[" + connName + "]连接字符串");

                ProviderType = _connTypes[connName];
                DbType = DbFactory.GetDefault(ProviderType)?.Type ?? DatabaseType.None;

                // 读写分离
                if (!connName.EndsWithIgnoreCase(".readonly"))
                {
                    var connName2 = connName + ".readonly";
                    if (ConnStrs.ContainsKey(connName2)) ReadOnly = Create(connName2);
                }

                _inited = true;
            }
        }
        #endregion

        #region 静态管理
        private static readonly ConcurrentDictionary<String, DAL> _dals = new ConcurrentDictionary<String, DAL>(StringComparer.OrdinalIgnoreCase);
        /// <summary>创建一个数据访问层对象。</summary>
        /// <param name="connName">配置名</param>
        /// <returns>对应于指定链接的全局唯一的数据访问层对象</returns>
        public static DAL Create(String connName)
        {
            if (String.IsNullOrEmpty(connName)) throw new ArgumentNullException(nameof(connName));

            // 如果需要修改一个DAL的连接字符串，不应该修改这里，而是修改DAL实例的ConnStr属性
            //if (!_dals.TryGetValue(connName, out var dal))
            //{
            //    lock (_dals)
            //    {
            //        if (!_dals.TryGetValue(connName, out dal))
            //        {
            //            dal = new DAL(connName);
            //            // 不用connName，因为可能在创建过程中自动识别了ConnName
            //            _dals.Add(dal.ConnName, dal);
            //        }
            //    }
            //}

            // Dictionary.TryGetValue 在多线程高并发下有可能抛出空异常
            var dal = _dals.GetOrAdd(connName, k => new DAL(k));

            // 创建完成对象后，初始化时单独锁这个对象，避免整体加锁
            dal.Init();

            return dal;
        }

        private void Reset()
        {
            _Db.TryDispose();

            _Db = null;
            _Tables = null;
            _hasCheck = false;
            HasCheckTables.Clear();

            GC.Collect(2);

            _inited = false;
            Init();
        }

        private static Dictionary<String, Type> _connTypes;
        private static void InitConnections()
        {
            var cs = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            var ts = new Dictionary<String, Type>(StringComparer.OrdinalIgnoreCase);

            LoadConfig(cs, ts);
            //LoadAppSettings(cs, ts);

            // 联合使用 appsettings.json
            LoadAppSettings("appsettings.json", cs, ts);
            //读取环境变量:ASPNETCORE_ENVIRONMENT=Development
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (String.IsNullOrWhiteSpace(env))
            {
                env = "Production";
            }
            LoadAppSettings($"appsettings.{env.Trim()}.json", cs, ts);

            ConnStrs = cs;
            _connTypes = ts;
        }

        /// <summary>链接字符串集合</summary>
        /// <remarks>
        /// 如果需要修改一个DAL的连接字符串，不应该修改这里，而是修改DAL实例的<see cref="ConnStr"/>属性
        /// </remarks>
        public static Dictionary<String, String> ConnStrs { get; private set; }

        private static void LoadConfig(IDictionary<String, String> cs, IDictionary<String, Type> ts)
        {
            var file = "web.config".GetFullPath();
            var fname = AppDomain.CurrentDomain.FriendlyName;
            // 2020-10-22 阴 fname可能是特殊情况，要特殊处理 "TestSourceHost: Enumerating source (E:\projects\bin\Debug\project.dll)"
            if (!File.Exists(fname) && fname.StartsWith("TestSourceHost: Enumerating"))
            {
                XTrace.WriteLine($"AppDomain.CurrentDomain.FriendlyName不太友好，处理一下：{fname}");
                fname = fname.Substring(fname.IndexOf(AppDomain.CurrentDomain.BaseDirectory, StringComparison.Ordinal)).TrimEnd(')');
            }
            if (!File.Exists(file)) file = "app.config".GetFullPath();
            if (!File.Exists(file)) file = $"{fname}.config".GetFullPath();
            if (!File.Exists(file)) file = $"{fname}.exe.config".GetFullPath();
            if (!File.Exists(file)) file = $"{fname}.dll.config".GetFullPath();

            if (File.Exists(file))
            {
                // 读取配置文件
                var doc = new XmlDocument();
                doc.Load(file);

                var nodes = doc.SelectNodes("/configuration/connectionStrings/add");
                if (nodes != null)
                {
                    foreach (XmlNode item in nodes)
                    {
                        var name = item.Attributes["name"]?.Value;
                        var connstr = item.Attributes["connectionString"]?.Value;
                        var provider = item.Attributes["providerName"]?.Value;
                        if (name.IsNullOrEmpty() || connstr.IsNullOrWhiteSpace()) continue;

                        var type = DbFactory.GetProviderType(connstr, provider);
                        if (type == null) XTrace.WriteLine("无法识别{0}的提供者{1}！", name, provider);

                        cs[name] = connstr;
                        ts[name] = type;
                    }
                }
            }
        }

        private static void LoadAppSettings(String fileName, IDictionary<String, String> cs, IDictionary<String, Type> ts)
        {
            // Asp.Net Core的Debug模式下配置文件位于项目目录而不是输出目录
            var file = fileName.GetBasePath();
            if (!File.Exists(file)) file = fileName.GetFullPath();
            if (!File.Exists(file)) file = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            if (File.Exists(file))
            {
                var lines = File.ReadAllLines(file);

                // 预处理注释
                var text = lines
                    .Where(e => !e.IsNullOrEmpty() && !e.TrimStart().StartsWith("//"))
                    // 没考虑到链接中带双斜杠的，以下导致链接的内容被干掉
                    //.Select(e =>
                    //{
                    //    // 单行注释 “//” 放在最后的情况
                    //    var p0 = e.IndexOf("//");
                    //    if (p0 > 0) return e.Substring(0, p0);

                    //    return e;
                    //})
                    .Join(Environment.NewLine);

                while (true)
                {
                    // 以下处理多行注释 “/**/” 放在一行的情况
                    var p = text.IndexOf("/*");
                    if (p < 0) break;

                    var p2 = text.IndexOf("*/", p + 2);
                    if (p2 < 0) break;

                    text = text.Substring(0, p) + text.Substring(p2 + 2);
                }

                var dic = JsonParser.Decode(text);
                dic = dic?["ConnectionStrings"] as IDictionary<String, Object>;
                if (dic != null && dic.Count > 0)
                {
                    foreach (var item in dic)
                    {
                        var name = item.Key;
                        if (name.IsNullOrEmpty() || item.Value is not IDictionary<String, Object> ds) continue;

                        var connstr = ds["connectionString"] + "";
                        var provider = ds["providerName"] + "";
                        if (connstr.IsNullOrWhiteSpace()) continue;

                        var type = DbFactory.GetProviderType(connstr, provider);
                        if (type == null) XTrace.WriteLine("无法识别{0}的提供者{1}！", name, provider);

                        cs[name] = connstr;
                        ts[name] = type;
                    }
                }
            }
        }

        /// <summary>添加连接字符串</summary>
        /// <param name="connName">连接名</param>
        /// <param name="connStr">连接字符串</param>
        /// <param name="type">实现了IDatabase接口的数据库类型</param>
        /// <param name="provider">数据库提供者，如果没有指定数据库类型，则有提供者判断使用哪一种内置类型</param>
        public static void AddConnStr(String connName, String connStr, Type type, String provider)
        {
            if (connName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(connName));
            if (connStr.IsNullOrEmpty()) return;

            //2016.01.04 @宁波-小董，加锁解决大量分表分库多线程带来的提供者无法识别错误
            lock (_connTypes)
            {
                if (!ConnStrs.TryGetValue(connName, out var oldConnStr)) oldConnStr = null;

                if (type == null) type = DbFactory.GetProviderType(connStr, provider);

                // 允许后来者覆盖前面设置过了的
                //var set = new ConnectionStringSettings(connName, connStr, provider);
                ConnStrs[connName] = connStr;
                _connTypes[connName] = type ?? throw new XCodeException("无法识别{0}的提供者{1}！", connName, provider);

                // 如果连接字符串改变，则重置所有
                if (!oldConnStr.IsNullOrEmpty() && !oldConnStr.EqualIgnoreCase(connStr))
                {
                    WriteLog("[{0}]的连接字符串改变，准备重置！", connName);

                    var dal = Create(connName);
                    dal.ConnStr = connStr;
                    dal.Reset();
                }
            }
        }

        /// <summary>找不到连接名时调用。支持用户自定义默认连接</summary>
        public static event EventHandler<ResolveEventArgs> OnResolve;

        /// <summary>获取连接字符串的委托。可以二次包装在连接名前后加上标识，存放在配置中心</summary>
        public static GetConfigCallback GetConfig { get; set; }

        private static readonly ConcurrentHashSet<String> _conns = new ConcurrentHashSet<String>();
        private static TimerX _timerGetConfig;
        /// <summary>从配置中心加载连接字符串，并支持定时刷新</summary>
        /// <param name="connName"></param>
        /// <returns></returns>
        private static Boolean GetFromConfigCenter(String connName)
        {
            {
                var str = GetConfig?.Invoke(connName);
                if (str.IsNullOrEmpty()) return false;

                AddConnStr(connName, str, null, null);

                // 加入集合，定时更新
                if (!_conns.Contains(connName)) _conns.TryAdd(connName);
            }

            // 读写分离
            if (!connName.EndsWithIgnoreCase(".readonly"))
            {
                var connName2 = connName + ".readonly";
                var str = GetConfig?.Invoke(connName2);
                if (!str.IsNullOrEmpty()) AddConnStr(connName2, str, null, null);

                // 加入集合，定时更新
                if (!_conns.Contains(connName2)) _conns.TryAdd(connName2);
            }

            if (_timerGetConfig == null) _timerGetConfig = new TimerX(DoGetConfig, null, 5_000, 60_000) { Async = true };

            return true;
        }

        private static void DoGetConfig(Object state)
        {
            foreach (var item in _conns)
            {
                var str = GetConfig?.Invoke(item);
                if (!str.IsNullOrEmpty()) AddConnStr(item, str, null, null);
            }
        }
        #endregion

        #region 连接字符串编码解码
        /// <summary>连接字符串编码</summary>
        /// <remarks>明文=>UTF8字节=>Base64</remarks>
        /// <param name="connstr"></param>
        /// <returns></returns>
        public static String EncodeConnStr(String connstr)
        {
            if (String.IsNullOrEmpty(connstr)) return connstr;

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(connstr));
        }

        /// <summary>连接字符串解码</summary>
        /// <remarks>Base64=>UTF8字节=>明文</remarks>
        /// <param name="connstr"></param>
        /// <returns></returns>
        private static String DecodeConnStr(String connstr)
        {
            if (String.IsNullOrEmpty(connstr)) return connstr;

            // 如果包含任何非Base64编码字符，直接返回
            foreach (var c in connstr)
            {
                if (!(c >= 'a' && c <= 'z' ||
                    c >= 'A' && c <= 'Z' ||
                    c >= '0' && c <= '9' ||
                    c == '+' || c == '/' || c == '=')) return connstr;
            }

            Byte[] bts = null;
            try
            {
                // 尝试Base64解码，如果解码失败，估计就是连接字符串，直接返回
                bts = Convert.FromBase64String(connstr);
            }
            catch { return connstr; }

            return Encoding.UTF8.GetString(bts);
        }
        #endregion

        #region 正向工程
        private List<IDataTable> _Tables;
        /// <summary>取得所有表和视图的构架信息（异步缓存延迟1秒）。设为null可清除缓存</summary>
        /// <remarks>
        /// 如果不存在缓存，则获取后返回；否则使用线程池线程获取，而主线程返回缓存。
        /// </remarks>
        /// <returns></returns>
        public List<IDataTable> Tables
        {
            get
            {
                // 如果不存在缓存，则获取后返回；否则使用线程池线程获取，而主线程返回缓存
                if (_Tables == null)
                    _Tables = GetTables();
                else
                    Task.Factory.StartNew(() => { _Tables = GetTables(); });

                return _Tables;
            }
            set =>
                //设为null可清除缓存
                _Tables = null;
        }

        private List<IDataTable> GetTables()
        {
            if (Db is DbBase db2 && !db2.SupportSchema) return new List<IDataTable>();

            CheckDatabase();
            return Db.CreateMetaData().GetTables();
        }

        /// <summary>导出模型</summary>
        /// <returns></returns>
        public String Export()
        {
            var list = Tables;

            if (list == null || list.Count < 1) return null;

            return Export(list);
        }

        /// <summary>导出模型</summary>
        /// <param name="tables"></param>
        /// <returns></returns>
        public static String Export(IEnumerable<IDataTable> tables) => ModelHelper.ToXml(tables);

        /// <summary>导入模型</summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static List<IDataTable> Import(String xml)
        {
            if (String.IsNullOrEmpty(xml)) return null;

            return ModelHelper.FromXml(xml, CreateTable);
        }

        /// <summary>导入模型文件</summary>
        /// <param name="xmlFile"></param>
        /// <returns></returns>
        public static List<IDataTable> ImportFrom(String xmlFile)
        {
            if (xmlFile.IsNullOrEmpty()) return null;

            xmlFile = xmlFile.GetFullPath();
            if (!File.Exists(xmlFile)) return null;

            return ModelHelper.FromXml(File.ReadAllText(xmlFile), CreateTable);
        }
        #endregion

        #region 反向工程
        private Boolean _hasCheck;
        /// <summary>使用数据库之前检查表架构</summary>
        /// <remarks>不阻塞，可能第一个线程正在检查表架构，别的线程已经开始使用数据库了</remarks>
        public void CheckDatabase()
        {
            if (_hasCheck) return;
            lock (this)
            {
                if (_hasCheck) return;

                try
                {
                    switch (Db.Migration)
                    {
                        case Migration.Off:
                            break;
                        case Migration.ReadOnly:
                            Task.Factory.StartNew(CheckTables);
                            break;
                        case Migration.On:
                        case Migration.Full:
                            CheckTables();
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    if (Debug) WriteLog(ex.GetMessage());
                }
                _hasCheck = true;
            }
        }

        internal List<String> HasCheckTables = new List<String>();
        /// <summary>检查是否已存在，如果不存在则添加</summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        internal Boolean CheckAndAdd(String tableName)
        {
            var tbs = HasCheckTables;
            if (tbs.Contains(tableName)) return true;
            lock (tbs)
            {
                if (tbs.Contains(tableName)) return true;

                tbs.Add(tableName);
            }

            return false;
        }

        /// <summary>检查数据表架构，不受反向工程启用开关限制，仅检查未经过常规检查的表</summary>
        public void CheckTables()
        {
            var name = ConnName;
            WriteLog("开始检查连接[{0}/{1}]的数据库架构……", name, DbType);

            var sw = Stopwatch.StartNew();

            try
            {
                var list = EntityFactory.GetTables(name, true);
                if (list != null && list.Count > 0)
                {
                    // 移除所有已初始化的
                    list.RemoveAll(dt => CheckAndAdd(dt.TableName));

                    //// 过滤掉被排除的表名
                    //list.RemoveAll(dt => NegativeExclude.Contains(dt.TableName));

                    // 过滤掉视图
                    list.RemoveAll(dt => dt.IsView);

                    if (list != null && list.Count > 0)
                    {
                        WriteLog(name + "待检查表架构的实体个数：" + list.Count);

                        SetTables(list.ToArray());
                    }
                }
            }
            finally
            {
                sw.Stop();

                WriteLog("检查连接[{0}/{1}]的数据库架构耗时{2:n0}ms", name, DbType, sw.Elapsed.TotalMilliseconds);
            }
        }

        /// <summary>在当前连接上检查指定数据表的架构</summary>
        /// <param name="tables"></param>
        public void SetTables(params IDataTable[] tables)
        {
            if (Db is DbBase db2 && !db2.SupportSchema) return;

            //// 构建DataTable时也要注意表前缀，避免反向工程用错
            //var pf = Db.TablePrefix;
            //if (!pf.IsNullOrEmpty())
            //{
            //    foreach (var tbl in tables)
            //    {
            //        if (!tbl.TableName.StartsWithIgnoreCase(pf)) tbl.TableName = pf + tbl.TableName;
            //    }
            //}

            Db.CreateMetaData().SetTables(Db.Migration, tables);
        }
        #endregion
    }
}