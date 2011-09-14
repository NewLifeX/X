using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using NewLife.Reflection;
using XCode.Code;
using XCode.Exceptions;

namespace XCode.DataAccessLayer
{
    /// <summary>数据访问层</summary>
    /// <remarks>
    /// 主要用于选择不同的数据库，不同的数据库的操作有所差别。
    /// 每一个数据库链接字符串，对应唯一的一个DAL实例。
    /// 数据库链接字符串可以写在配置文件中，然后在Create时指定名字；
    /// 也可以直接把链接字符串作为AddConnStr的参数传入。
    /// 每一个数据库操作都必须指定表名以用于管理缓存，空表名或*将匹配所有缓存
    /// </remarks>
    public partial class DAL
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
            if (String.IsNullOrEmpty(ConnStr)) throw new XCodeException("请在使用数据库前设置[" + connName + "]连接字符串");

            // 创建数据库访问对象的时候，就开始检查数据库架构
            // 尽管这样会占用大量时间，但这种情况往往只存在于安装部署的时候
            // 要尽可能的减少非安装阶段的时间占用
            try
            {
                //DatabaseSchema.Check(Db);
                SetTables();
            }
            catch (Exception ex)
            {
                if (Debug) WriteLog(ex.ToString());
            }
        }

        private static Dictionary<String, DAL> _dals = new Dictionary<String, DAL>(StringComparer.OrdinalIgnoreCase);
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
        private static Dictionary<String, Type> _connTypes = new Dictionary<String, Type>(StringComparer.OrdinalIgnoreCase);
        /// <summary>链接字符串集合</summary>
        public static Dictionary<String, ConnectionStringSettings> ConnStrs
        {
            get
            {
                if (_connStrs != null) return _connStrs;
                lock (_connStrs_lock)
                {
                    if (_connStrs != null) return _connStrs;
                    Dictionary<String, ConnectionStringSettings> cs = new Dictionary<String, ConnectionStringSettings>(StringComparer.OrdinalIgnoreCase);

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

        /// <summary>添加连接字符串</summary>
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

        /// <summary>从提供者和连接字符串猜测数据库处理器</summary>
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
                else if (provider.Contains("firebird"))
                    type = typeof(Firebird);
                else if (provider.Contains("postgresql"))
                    type = typeof(PostgreSQL);
                else if (provider.Contains("npgsql"))
                    type = typeof(PostgreSQL);
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
        /// <summary>连接名</summary>
        public String ConnName { get { return _ConnName; } }

        private Type _ProviderType;
        /// <summary>实现了IDatabase接口的数据库类型</summary>
        private Type ProviderType
        {
            get
            {
                if (_ProviderType == null && _connTypes.ContainsKey(ConnName)) _ProviderType = _connTypes[ConnName];
                return _ProviderType;
            }
        }

        /// <summary>数据库类型</summary>
        public DatabaseType DbType { get { return Db.DbType; } }

        private String _ConnStr;
        /// <summary>连接字符串</summary>
        public String ConnStr { get { return _ConnStr; } private set { _ConnStr = value; } }

        private IDatabase _Db;
        /// <summary>数据库。所有数据库操作在此统一管理，强烈建议不要直接使用该数据，在不同版本中IDatabase可能有较大改变</summary>
        public IDatabase Db
        {
            get
            {
                if (_Db != null) return _Db;

                Type type = ProviderType;
                if (type != null)
                {
                    //_Db = TypeX.CreateInstance(type) as IDatabase;
                    // 使用鸭子类型，避免因接口版本差异而导致无法使用
                    _Db = TypeX.ChangeType<IDatabase>(TypeX.CreateInstance(type));
                    // 不为空才设置连接字符串，因为可能有内部包装
                    if (!String.IsNullOrEmpty(ConnName)) _Db.ConnName = ConnName;
                    if (!String.IsNullOrEmpty(ConnStr)) _Db.ConnectionString = ConnStr;
                }

                return _Db;
            }
        }

        /// <summary>数据库会话</summary>
        public IDbSession Session { get { return Db.CreateSession(); } }
        #endregion

        #region 正向工程
        private List<IDataTable> _Tables;
        /// <summary>
        /// 取得所有表和视图的构架信息，为了提高性能，得到的只是准实时信息，可能会有1秒到3秒的延迟
        /// </summary>
        /// <remarks>如果不存在缓存，则获取后返回；否则使用线程池线程获取，而主线程返回缓存</remarks>
        /// <returns></returns>
        public List<IDataTable> Tables
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

        private List<IDataTable> GetTables()
        {
            List<IDataTable> list = Db.CreateMetaData().GetTables();
            //if (list != null && list.Count > 0) list.Sort(delegate(IDataTable item1, IDataTable item2) { return item1.Name.CompareTo(item2.Name); });
            return list;
        }

        /// <summary>导出模型</summary>
        /// <returns></returns>
        public String Export()
        {
            List<IDataTable> list = Tables;

            if (list == null || list.Count < 1) return null;

            //XmlWriterX writer = new XmlWriterX();
            //writer.Settings.WriteType = false;
            //writer.Settings.UseObjRef = false;
            //writer.Settings.IgnoreDefault = true;
            //writer.Settings.MemberAsAttribute = true;
            //writer.RootName = "Tables";
            //writer.WriteObject(list);
            //return writer.ToString();

            return Export(list);
        }

        /// <summary>导出模型</summary>
        /// <param name="tables"></param>
        /// <returns></returns>
        public static String Export(List<IDataTable> tables)
        {
            MemoryStream ms = new MemoryStream();

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = Encoding.UTF8;
            settings.Indent = true;

            XmlWriter writer = XmlWriter.Create(ms, settings);
            writer.WriteStartDocument();
            writer.WriteStartElement("Tables");
            foreach (IDataTable item in tables)
            {
                writer.WriteStartElement("Table");
                (item as IXmlSerializable).WriteXml(writer);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        /// <summary>导入模型</summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static List<IDataTable> Import(String xml)
        {
            if (String.IsNullOrEmpty(xml)) return null;

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;
            settings.IgnoreComments = true;

            XmlReader reader = XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(xml)), settings);
            while (reader.NodeType != XmlNodeType.Element) { if (!reader.Read())return null; }
            reader.ReadStartElement();

            List<IDataTable> list = new List<IDataTable>();
            while (reader.IsStartElement())
            {
                IDataTable table = CreateTable();
                list.Add(table);

                //reader.ReadStartElement();
                (table as IXmlSerializable).ReadXml(reader);
                //if (reader.NodeType == XmlNodeType.EndElement) reader.ReadEndElement();
            }
            return list;

            //XmlReaderX reader = new XmlReaderX(xml);
            ////XmlSerializer serial = new XmlSerializer(typeof(List<XTable>));
            ////List<XTable> ts = serial.Deserialize(reader.Stream) as List<XTable>;

            //reader.Settings.MemberAsAttribute = true;
            //List<XTable> list = reader.ReadObject(typeof(List<XTable>)) as List<XTable>;
            //if (list == null || list.Count < 1) return null;

            //List<IDataTable> dts = new List<IDataTable>();
            //// 修正字段中的Table引用
            //foreach (XTable item in list)
            //{
            //    if (item.Columns == null || item.Columns.Count < 1) continue;

            //    List<IDataColumn> fs = new List<IDataColumn>();
            //    foreach (IDataColumn field in item.Columns)
            //    {
            //        //fs.Add(field.Clone(item));
            //        item.Columns.Add(field.Clone(item));
            //    }
            //    //item.Columns = fs.ToArray();

            //    dts.Add(item);
            //}

            //return dts;
        }
        #endregion

        #region 反向工程
        /// <summary>
        /// 反向工程
        /// </summary>
        private void SetTables()
        {
            if (NegativeEnable == null || NegativeExclude.Contains(ConnName)) return;

            // 打开了开关，并且设置为true时，使用同步方式检查
            // 设置为false时，使用异步方式检查，因为上级的意思是不大关心数据库架构
            if (NegativeEnable != null && NegativeEnable.Value)
                Check();
            else
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    try { Check(); }
                    catch (Exception ex) { WriteLog(ex.ToString()); }
                });
            }
        }

        private void Check()
        {
            WriteLog("开始检查连接[{0}][{1}]的数据库架构……", ConnName, DbType);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            try
            {
                List<IDataTable> list = EntityFactory.GetTablesByConnName(ConnName);
                if (list != null && list.Count > 0)
                {
                    // 过滤掉被排除的表名
                    if (NegativeExclude.Count > 0)
                    {
                        for (int i = list.Count - 1; i >= 0; i--)
                        {
                            if (NegativeExclude.Contains(list[i].Name)) list.RemoveAt(i);
                        }
                    }
                    if (list != null && list.Count > 0)
                    {
                        WriteLog(ConnName + "实体个数：" + list.Count);

                        Db.CreateMetaData().SetTables(list.ToArray());
                    }
                }
            }
            finally
            {
                sw.Stop();

                WriteLog("检查连接[{0}][{1}]的数据库架构耗时{2}", ConnName, DbType, sw.Elapsed);
            }
        }
        #endregion

        #region 创建数据操作实体
        /// <summary>创建实体操作接口</summary>
        /// <remarks>因为只用来做实体操作，所以只需要一个实例即可</remarks>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public IEntityOperate CreateOperate(String tableName)
        {
            Assembly asm = EntityAssembly.Create(this);
            Type type = TypeX.GetType(asm, tableName);

            return EntityFactory.CreateOperate(type);
        }
        #endregion
    }
}