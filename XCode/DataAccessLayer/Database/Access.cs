using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using ADODB;
using ADOX;
using DAO;
using NewLife.Log;
using XCode.Common;
using XCode.Exceptions;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// Access数据库
    /// </summary>
    internal class AccessSession : DbSession
    {
        #region 属性
        ///// <summary>
        ///// 返回数据库类型。外部DAL数据库类请使用Other
        ///// </summary>
        //public override DatabaseType DbType
        //{
        //    get { return DatabaseType.Access; }
        //}

        ///// <summary>工厂</summary>
        //public override DbProviderFactory Factory
        //{
        //    get
        //    {

        //        return OleDbFactory.Instance;
        //    }
        //}

        /// <summary>链接字符串</summary>
        public override string ConnectionString
        {
            get
            {
                return base.ConnectionString;
            }
            set
            {
                try
                {
                    OleDbConnectionStringBuilder csb = new OleDbConnectionStringBuilder(value);
                    // 不是绝对路径
                    if (!String.IsNullOrEmpty(csb.DataSource) && csb.DataSource.Length > 1 && csb.DataSource.Substring(1, 1) != ":")
                    {
                        String mdbPath = csb.DataSource;
                        if (mdbPath.StartsWith("~/") || mdbPath.StartsWith("~\\"))
                        {
                            mdbPath = mdbPath.Replace("/", "\\").Replace("~\\", AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\') + "\\");
                        }
                        else if (mdbPath.StartsWith("./") || mdbPath.StartsWith(".\\"))
                        {
                            mdbPath = mdbPath.Replace("/", "\\").Replace(".\\", AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\') + "\\");
                        }
                        else
                        {
                            mdbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, mdbPath.Replace("/", "\\"));
                        }
                        csb.DataSource = mdbPath;
                        FileName = mdbPath;
                        value = csb.ConnectionString;
                    }
                }
                catch (DbException ex)
                {
                    throw new XDbException(this, "分析OLEDB连接字符串时出错", ex);
                }
                base.ConnectionString = value;
            }
        }

        private String _FileName;
        /// <summary>文件</summary>
        public String FileName
        {
            get { return _FileName; }
            private set { _FileName = value; }
        }
        #endregion

        #region 重载使用连接池
        /// <summary>
        /// 打开。已重写，为了建立数据库
        /// </summary>
        public override void Open()
        {
            if (!Supported) return;

            if (!File.Exists(FileName)) CreateDatabase();

            //try
            //{
            base.Open();
            //}
            //catch (InvalidOperationException ex)
            //{
            //    if (ex.Message.Contains("Microsoft.Jet.OLEDB.4.0"))
            //        throw new InvalidOperationException("64位系统不支持OLEDB，请把编译平台改为x86。", ex);

            //    throw;
            //}
        }
        #endregion

        #region 基本方法 查询/执行
        /// <summary>
        /// 执行插入语句并返回新增行的自动编号
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>新增行的自动编号</returns>
        public override Int32 InsertAndGetIdentity(String sql)
        {
            ExecuteTimes++;
            if (Debug) WriteLog(sql);
            try
            {
                DbCommand cmd = PrepareCommand();
                cmd.CommandText = sql;
                Int32 rs = cmd.ExecuteNonQuery();
                if (rs > 0)
                {
                    cmd.CommandText = "Select @@Identity";
                    rs = Int32.Parse(cmd.ExecuteScalar().ToString());
                }
                AutoClose();
                return rs;
            }
            catch (DbException ex)
            {
                throw OnException(ex, sql);
            }
        }
        #endregion

        #region 构架
        //static TypeX oledbSchema;
        ///// <summary>
        ///// 已重载。特殊情况下使用OleDb引擎的GetOleDbSchemaTable
        ///// </summary>
        ///// <param name="collectionName"></param>
        ///// <param name="restrictionValues"></param>
        ///// <returns></returns>
        //public override DataTable GetSchema(string collectionName, string[] restrictionValues)
        //{
        //    if (oledbSchema == null) oledbSchema = TypeX.Create(typeof(OleDbSchemaGuid));

        //    if (String.IsNullOrEmpty(collectionName))
        //    {
        //        DataTable dt = base.GetSchema(collectionName, restrictionValues);
        //        foreach (FieldInfoX item in oledbSchema.Fields)
        //        {
        //            DataRow dr = dt.NewRow();
        //            dr[0] = item.Field.Name;
        //            dt.Rows.Add(dr);
        //        }
        //        return dt;
        //    }

        //    if (oledbSchema.Fields != null && oledbSchema.Fields.Count > 0)
        //    {
        //        foreach (FieldInfoX item in oledbSchema.Fields)
        //        {
        //            if (!String.Equals(item.Field.Name, collectionName, StringComparison.OrdinalIgnoreCase)) continue;

        //            Guid guid = (Guid)item.GetValue();
        //            if (guid != Guid.Empty)
        //            {
        //                Object[] pms = null;
        //                if (restrictionValues != null)
        //                {
        //                    pms = new Object[restrictionValues.Length];
        //                    for (int i = 0; i < restrictionValues.Length; i++)
        //                    {
        //                        pms[i] = restrictionValues[i];
        //                    }
        //                }
        //                //return (Conn as OleDbConnection).GetOleDbSchemaTable(guid, pms);
        //                return GetOleDbSchemaTable(guid, pms);
        //            }
        //        }
        //    }

        //    return base.GetSchema(collectionName, restrictionValues);
        //}

        //private DataTable GetOleDbSchemaTable(Guid schema, object[] restrictions)
        //{
        //    if (!Opened) Open();

        //    try
        //    {
        //        return (Conn as OleDbConnection).GetOleDbSchemaTable(schema, restrictions);
        //    }
        //    //catch (Exception ex)
        //    //{
        //    //    if (Debug) WriteLog(ex.ToString());
        //    //    return null;
        //    //}
        //    catch (DbException ex)
        //    {
        //        throw new XDbException(this, "取得所有表构架出错！", ex);
        //    }
        //    finally
        //    {
        //        AutoClose();
        //    }
        //}

        public override List<XTable> GetTables()
        {
            DataTable dt = GetSchema("Tables", null);
            if (dt == null || dt.Rows == null || dt.Rows.Count < 1) return null;

            // 默认列出所有字段
            DataRow[] rows = dt.Select(String.Format("{0}='Table' Or {0}='View'", "TABLE_TYPE"));
            return GetTables(rows);
        }

        protected override List<XField> GetFields(XTable xt)
        {
            List<XField> list = base.GetFields(xt);
            if (list == null || list.Count < 1) return null;

            Dictionary<String, XField> dic = new Dictionary<String, XField>();
            foreach (XField xf in list)
            {
                dic.Add(xf.Name, xf);
            }

            try
            {
                using (ADOTabe table = new ADOTabe(ConnectionString, FileName, xt.Name))
                {
                    if (table.Supported && table.Columns != null)
                    {
                        foreach (ADOColumn item in table.Columns)
                        {
                            if (!dic.ContainsKey(item.Name)) continue;

                            dic[item.Name].Identity = item.AutoIncrement;
                            if (!dic[item.Name].Identity) dic[item.Name].Nullable = item.Nullable;
                        }
                    }
                }
            }
            catch { }

            return list;
        }

        protected override void FixField(XField field, DataRow dr)
        {
            base.FixField(field, dr);

            // 字段标识
            Int64 flag = GetDataRowValue<Int64>(dr, "COLUMN_FLAGS");

            Boolean? isLong = null;

            Int32 id = 0;
            if (Int32.TryParse(GetDataRowValue<String>(dr, "DATA_TYPE"), out id))
            {
                DataRow[] drs = FindDataType(id, isLong);
                if (drs != null && drs.Length > 0)
                {
                    String typeName = GetDataRowValue<String>(drs[0], "TypeName");
                    field.RawType = typeName;

                    if (TryGetDataRowValue<String>(drs[0], "DataType", out typeName)) field.DataType = Type.GetType(typeName);

                    // 修正备注类型
                    if (field.DataType == typeof(String) && drs.Length > 1)
                    {
                        isLong = (flag & 0x80) == 0x80;
                        drs = FindDataType(id, isLong);
                        if (drs != null && drs.Length > 0)
                        {
                            typeName = GetDataRowValue<String>(drs[0], "TypeName");
                            field.RawType = typeName;
                        }
                    }
                }
            }

            //// 处理自增
            //if (field.DataType == typeof(Int32))
            //{
            //    //field.Identity = (flag & 0x20) != 0x20;
            //}
        }

        protected override Dictionary<DataRow, String> GetPrimaryKeys(string tableName)
        {
            Dictionary<DataRow, String> pks = base.GetPrimaryKeys(tableName);
            if (pks == null || pks.Count < 1) return null;
            if (pks.Count == 1) return pks;

            // 避免把索引错当成主键
            List<DataRow> list = new List<DataRow>();
            foreach (DataRow item in pks.Keys)
            {
                if (!GetDataRowValue<Boolean>(item, "PRIMARY_KEY")) list.Add(item);
            }
            if (list.Count == pks.Count) return pks;

            foreach (DataRow item in list)
            {
                pks.Remove(item);
            }
            return pks;
        }
        #endregion

        #region 数据定义
        /// <summary>
        /// 设置数据定义模式
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public override object SetSchema(DDLSchema schema, object[] values)
        {
            Object obj = null;
            switch (schema)
            {
                case DDLSchema.CreateDatabase:
                    CreateDatabase();
                    return null;
                case DDLSchema.DropDatabase:
                    //首先关闭数据库
                    Close();

                    OleDbConnection.ReleaseObjectPool();
                    GC.Collect();

                    if (File.Exists(FileName)) File.Delete(FileName);
                    return null;
                case DDLSchema.DatabaseExist:
                    return File.Exists(FileName);
                case DDLSchema.CreateTable:
                    obj = base.SetSchema(DDLSchema.CreateTable, values);
                    XTable table = values[0] as XTable;
                    if (!String.IsNullOrEmpty(table.Description)) AddTableDescription(table.Name, table.Description);
                    foreach (XField item in table.Fields)
                    {
                        if (!String.IsNullOrEmpty(item.Description)) AddColumnDescription(table.Name, item.Name, item.Description);
                    }
                    return obj;
                case DDLSchema.DropTable:
                    break;
                case DDLSchema.TableExist:
                    DataTable dt = GetSchema("Tables", new String[] { null, null, (String)values[0], "TABLE" });
                    if (dt == null || dt.Rows == null || dt.Rows.Count < 1) return false;
                    return true;
                case DDLSchema.AddTableDescription:
                    return AddTableDescription((String)values[0], (String)values[1]);
                case DDLSchema.DropTableDescription:
                    return DropTableDescription((String)values[0]);
                case DDLSchema.AddColumn:
                    obj = base.SetSchema(DDLSchema.AddColumn, values);
                    AddColumnDescription((String)values[0], ((XField)values[1]).Name, ((XField)values[1]).Description);
                    return obj;
                case DDLSchema.AlterColumn:
                    break;
                case DDLSchema.DropColumn:
                    break;
                case DDLSchema.AddColumnDescription:
                    return AddColumnDescription((String)values[0], (String)values[1], (String)values[2]);
                case DDLSchema.DropColumnDescription:
                    return DropColumnDescription((String)values[0], (String)values[1]);
                case DDLSchema.AddDefault:
                    return AddDefault((String)values[0], (String)values[1], (String)values[2]);
                case DDLSchema.DropDefault:
                    return DropDefault((String)values[0], (String)values[1]);
                default:
                    break;
            }
            return base.SetSchema(schema, values);
        }

        #region 创建数据库
        private void CreateDatabase()
        {
            FileSource.ReleaseFile("Database.mdb", FileName, true);
        }
        #endregion

        #region 表和字段备注
        public Boolean AddTableDescription(String tablename, String description)
        {
            try
            {
                using (ADOTabe table = new ADOTabe(ConnectionString, FileName, tablename))
                {
                    table.Description = description;
                    return true;
                }
            }
            catch { return false; }
        }

        public Boolean DropTableDescription(String tablename)
        {
            return AddTableDescription(tablename, null);
        }

        public Boolean AddColumnDescription(String tablename, String columnname, String description)
        {
            try
            {
                using (ADOTabe table = new ADOTabe(ConnectionString, FileName, tablename))
                {
                    if (table.Supported && table.Columns != null)
                    {
                        foreach (ADOColumn item in table.Columns)
                        {
                            if (item.Name == columnname)
                            {
                                item.Description = description;
                                return true;
                            }
                        }
                    }
                    return false;
                }
            }
            catch { return false; }
        }

        public Boolean DropColumnDescription(String tablename, String columnname)
        {
            return AddColumnDescription(tablename, columnname, null);
        }
        #endregion

        #region 默认值
        public virtual Boolean AddDefault(String tablename, String columnname, String value)
        {
            try
            {
                using (ADOTabe table = new ADOTabe(ConnectionString, FileName, tablename))
                {
                    if (table.Supported && table.Columns != null)
                    {
                        foreach (ADOColumn item in table.Columns)
                        {
                            if (item.Name == columnname)
                            {
                                item.Default = value;
                                return true;
                            }
                        }
                    }
                    return false;
                }
            }
            catch { return false; }
        }

        public virtual Boolean DropDefault(String tablename, String columnname)
        {
            return AddDefault(tablename, columnname, null);
        }
        #endregion
        #endregion

        #region 数据类型
        //public override Type FieldTypeToClassType(String typeName)
        //{
        //    Int32 id = 0;
        //    if (Int32.TryParse(typeName, out id))
        //    {
        //        DataRow[] drs = FindDataType(id, null);
        //        if (drs != null && drs.Length > 0)
        //        {
        //            if (!TryGetDataRowValue<String>(drs[0], "DataType", out typeName)) return null;
        //            return Type.GetType(typeName);
        //        }
        //    }

        //    return base.FieldTypeToClassType(typeName);
        //}

        DataRow[] FindDataType(Int32 typeID, Boolean? isLong)
        {
            DataTable dt = DataTypes;
            if (dt == null) return null;

            DataRow[] drs = null;
            if (isLong == null)
            {
                drs = dt.Select(String.Format("NativeDataType={0}", typeID));
                if (drs == null || drs.Length < 1) drs = dt.Select(String.Format("ProviderDbType={0}", typeID));
            }
            else
            {
                drs = dt.Select(String.Format("NativeDataType={0} And IsLong={1}", typeID, isLong.Value));
                if (drs == null || drs.Length < 1) drs = dt.Select(String.Format("ProviderDbType={0} And IsLong={1}", typeID, isLong.Value));
            }
            return drs;
        }
        #endregion

        #region 构造
        //static Access()
        //{
        //    Module module = typeof(Object).Module;

        //    PortableExecutableKinds kind;
        //    ImageFileMachine machine;
        //    module.GetPEKind(out kind, out machine);

        //    if (machine != ImageFileMachine.I386) throw new NotSupportedException("64位平台不支持OLEDB驱动！");

        //    //AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
        //}

        //static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        //{
        //    try
        //    {
        //        Assembly asm = null;
        //        if (args.Name.StartsWith("Interop.DAO,")) asm = FileSource.GetAssembly("Interop.DAO.dll");
        //        if (args.Name.StartsWith("Interop.ADODB,")) asm = FileSource.GetAssembly("Interop.ADODB.dll");
        //        if (args.Name.StartsWith("Interop.ADOX,")) asm = FileSource.GetAssembly("Interop.ADOX.dll");

        //        if (asm != null)
        //        {
        //            FileSource.ReleaseFile("Interop.DAO.dll", null, false);
        //            FileSource.ReleaseFile("Interop.ADODB.dll", null, false);
        //            FileSource.ReleaseFile("Interop.ADOX.dll", null, false);

        //            return asm;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        XTrace.WriteLine(ex.ToString());
        //    }

        //    throw new Exception("未能加载程序集" + args.Name);
        //}
        #endregion

        #region 平台检查
        private static Boolean? _Supported;
        /// <summary>
        /// 是否支持
        /// </summary>
        private static Boolean Supported
        {
            get
            {
                if (_Supported != null) return _Supported.Value;

                Module module = typeof(Object).Module;

                PortableExecutableKinds kind;
                ImageFileMachine machine;
                module.GetPEKind(out kind, out machine);

                if (machine != ImageFileMachine.I386) throw new NotSupportedException("64位平台不支持OLEDB驱动！");

                _Supported = true;

                return true;
            }
        }
        #endregion
    }

    class Access : Database
    {
        #region 构造
        private Access() { }

        public static Access Instance = new Access();
        #endregion

        #region 属性
        /// <summary>
        /// 返回数据库类型。外部DAL数据库类请使用Other
        /// </summary>
        public override DatabaseType DbType
        {
            get { return DatabaseType.Access; }
        }

        /// <summary>工厂</summary>
        public override DbProviderFactory Factory
        {
            get { return OleDbFactory.Instance; }
        }
        #endregion

        #region 方法
        public override IDbSession CreateSession()
        {
            return new AccessSession();
        }
        #endregion

        #region 数据库特性
        /// <summary>
        /// 当前时间函数
        /// </summary>
        public override String DateTimeNow { get { return "now()"; } }

        /// <summary>
        /// 最小时间
        /// </summary>
        public override DateTime DateTimeMin { get { return DateTime.MinValue; } }

        /// <summary>
        /// 格式化时间为SQL字符串
        /// </summary>
        /// <param name="dateTime">时间值</param>
        /// <returns></returns>
        public override String FormatDateTime(DateTime dateTime)
        {
            return String.Format("#{0:yyyy-MM-dd HH:mm:ss}#", dateTime);
        }

        /// <summary>
        /// 格式化关键字
        /// </summary>
        /// <param name="keyWord">关键字</param>
        /// <returns></returns>
        public override String FormatKeyWord(String keyWord)
        {
            if (String.IsNullOrEmpty(keyWord)) throw new ArgumentNullException("keyWord");

            if (keyWord.StartsWith("[") && keyWord.EndsWith("]")) return keyWord;

            return String.Format("[{0}]", keyWord);
            //return keyWord;
        }
        #endregion
    }

    #region OleDb连接池
    ///// <summary>
    ///// Access数据库连接池。
    ///// 每个连接字符串一个连接池。
    ///// 一定时间后，关闭未使用的多余连接；
    ///// </summary>
    //internal class AccessPool : IDisposable
    //{
    //    #region 连接池的创建与销毁
    //    /// <summary>
    //    /// 连接字符串
    //    /// </summary>
    //    private String ConnectionString;
    //    /// <summary>
    //    /// 私有构造函数，禁止外部创建实例。
    //    /// </summary>
    //    /// <param name="connStr">连接字符串</param>
    //    private AccessPool(String connStr)
    //    {
    //        ConnectionString = connStr;
    //    }

    //    private Boolean Disposed = false;
    //    /// <summary>
    //    /// 释放所有连接
    //    /// </summary>
    //    public void Dispose()
    //    {
    //        if (Disposed) return;
    //        lock (this)
    //        {
    //            if (Disposed) return;
    //            foreach (OleDbConnection conn in FreeList)
    //            {
    //                try
    //                {
    //                    if (conn != null && conn.State != ConnectionState.Closed) conn.Close();
    //                }
    //                catch (Exception ex)
    //                {
    //                    Trace.WriteLine("在AccessPool连接池中释放所有连接时出错！" + ex.ToString());
    //                }
    //            }
    //            FreeList.Clear();
    //            foreach (OleDbConnection conn in UsedList)
    //            {
    //                try
    //                {
    //                    if (conn != null && conn.State != ConnectionState.Closed) conn.Close();
    //                }
    //                catch (Exception ex)
    //                {
    //                    Trace.WriteLine("在AccessPool连接池中释放所有连接时出错！" + ex.ToString());
    //                }
    //            }
    //            UsedList.Clear();
    //            //双锁
    //            if (Pools.ContainsKey(ConnectionString))
    //            {
    //                lock (Pools)
    //                {
    //                    if (Pools.ContainsKey(ConnectionString)) Pools.Remove(ConnectionString);
    //                }
    //            }
    //            Disposed = true;
    //        }
    //    }

    //    ~AccessPool()
    //    {
    //        // 析构调用每个连接池对象的Dispose，Dispose后又可能引发析构
    //        Dispose();
    //    }
    //    #endregion

    //    #region 借/还 连接
    //    /// <summary>
    //    /// 空闲列表
    //    /// </summary>
    //    private List<OleDbConnection> FreeList = new List<OleDbConnection>();
    //    /// <summary>
    //    /// 使用列表
    //    /// </summary>
    //    private List<OleDbConnection> UsedList = new List<OleDbConnection>();
    //    /// <summary>
    //    /// 最大池大小
    //    /// </summary>
    //    public Int32 MaxPoolSize = 100;
    //    /// <summary>
    //    /// 最小池大小
    //    /// </summary>
    //    public Int32 MinPoolSize = 0;

    //    /// <summary>
    //    /// 取连接
    //    /// </summary>
    //    /// <returns></returns>
    //    private OleDbConnection Open()
    //    {
    //        // 多线程冲突锁定，以下代码在同一时刻只能有一个线程进入
    //        lock (this)
    //        {
    //            if (UsedList.Count >= MaxPoolSize) throw new XException("连接池的连接数超过最大限制，无法提供服务");
    //            OleDbConnection conn;
    //            // 看看是否还有连接，如果没有，需要马上创建
    //            if (FreeList.Count < 1)
    //            {
    //                Trace.WriteLine("新建连接");
    //                conn = new OleDbConnection(ConnectionString);
    //                conn.Open();
    //                // 直接进入使用列表
    //                UsedList.Add(conn);
    //                return conn;
    //            }
    //            // 从空闲列表中取第一个连接
    //            conn = FreeList[0];
    //            // 第一个连接离开空闲列表
    //            FreeList.RemoveAt(0);
    //            // 该连接进入使用列表
    //            UsedList.Add(conn);
    //            // 检查连接是否已经打开，如果没打开，则打开
    //            if (conn.State == ConnectionState.Closed) conn.Open();
    //            return conn;
    //        }
    //    }

    //    /// <summary>
    //    /// 返还连接
    //    /// </summary>
    //    /// <param name="conn">连接对象</param>
    //    private void Close(OleDbConnection conn)
    //    {
    //        if (conn == null || UsedList == null || UsedList.Count < 1) return;
    //        lock (this)
    //        {
    //            if (UsedList == null || UsedList.Count < 1) return;
    //            // 下面的检查，原来放在lock外面，在高并发的环境下报了那个不可能的异常，谨记以后一定要Double Lock
    //            // Double Lock也就是：检查->锁定->再检查->执行
    //            // 检查该连接对象是否来自本连接池。该信息应该在设计时期就显示，以帮助开发者快速修正错误
    //            if (!UsedList.Contains(conn)) throw new XException("返还给AccessPool连接池的连接，不是来自本连接池！");
    //            // 离开使用列表
    //            UsedList.Remove(conn);
    //            // 回到空闲列表
    //            FreeList.Add(conn);
    //        }
    //    }
    //    #endregion

    //    #region 检查连接
    //    /// <summary>
    //    /// 检查连接池。关闭未使用连接，防止打开过多连接而又不关闭
    //    /// </summary>
    //    /// <returns>是否关闭了连接，调用者将以此为依据来决定是否停用定时器</returns>
    //    private Boolean Check()
    //    {
    //        if (FreeList.Count < 1 || FreeList.Count + UsedList.Count <= MinPoolSize) return false;
    //        lock (this)
    //        {
    //            if (FreeList.Count < 1 || FreeList.Count + UsedList.Count <= MinPoolSize) return false;
    //            Trace.WriteLine("删除连接");
    //            try
    //            {
    //                // 关闭所有空闲连接，仅保留最小池大小
    //                while (FreeList.Count > 0 && FreeList.Count + UsedList.Count > MinPoolSize)
    //                {
    //                    OleDbConnection conn = FreeList[0];
    //                    FreeList.RemoveAt(0);
    //                    conn.Close();
    //                    conn.Dispose();
    //                }
    //            }
    //            catch (Exception ex)
    //            {
    //                Trace.WriteLine("检查AccessPool连接池时出错！" + ex.ToString());
    //            }
    //            return true;
    //        }
    //    }
    //    #endregion

    //    #region 从连接池中 借/还 连接
    //    /// <summary>
    //    /// 连接池集合。连接字符串作为索引，每个连接字符串对应一个连接池。
    //    /// </summary>
    //    private static Dictionary<String, AccessPool> Pools = new Dictionary<string, AccessPool>();

    //    /// <summary>
    //    /// 获得连接
    //    /// </summary>
    //    /// <param name="connStr">连接字符串</param>
    //    /// <returns></returns>
    //    public static OleDbConnection Open(String connStr)
    //    {
    //        if (String.IsNullOrEmpty(connStr)) return null;
    //        // 检查是否存在连接字符串为connStr的连接池
    //        if (!Pools.ContainsKey(connStr))
    //        {
    //            lock (Pools)
    //            {
    //                if (!Pools.ContainsKey(connStr))
    //                {
    //                    Pools.Add(connStr, new AccessPool(connStr));
    //                    // 从现在开始10秒后，每隔10秒检查一次连接池，删除一个不使用的连接
    //                    CreateAndStartTimer();
    //                }
    //            }
    //        }
    //        return Pools[connStr].Open();
    //    }

    //    /// <summary>
    //    /// 把连接返回连接池
    //    /// </summary>
    //    /// <param name="connStr">连接字符串</param>
    //    /// <param name="conn">连接</param>
    //    public static void Close(String connStr, OleDbConnection conn)
    //    {
    //        if (String.IsNullOrEmpty(connStr)) return;
    //        if (conn == null) return;
    //        if (!Pools.ContainsKey(connStr)) return;
    //        Pools[connStr].Close(conn);
    //    }
    //    #endregion

    //    #region 检查连接池
    //    /// <summary>
    //    /// 检查连接池定时器。用于定时清理多余的连接
    //    /// </summary>
    //    private static Timer CheckPoolTimer;

    //    /// <summary>
    //    /// 建立并启动计时器。
    //    /// 使用无线等待时间的方式，使得线程池检查工作在可控的方式下进行
    //    /// 无限等待时间时，检查工作只会执行一次。
    //    /// 可以在一次检查完成的时候再启动新一次的等待。
    //    /// </summary>
    //    private static void CreateAndStartTimer()
    //    {
    //        if (CheckPoolTimer == null)
    //            CheckPoolTimer = new Timer(new TimerCallback(CheckPool), null, 10000, Timeout.Infinite);
    //        else
    //            CheckPoolTimer.Change(10000, Timeout.Infinite);
    //    }

    //    /// <summary>
    //    /// 定时检查连接池，每次检查都删除每个连接池的一个空闲连接
    //    /// </summary>
    //    /// <param name="obj"></param>
    //    private static void CheckPool(Object obj)
    //    {
    //        // 是否有连接被关闭
    //        Boolean IsClose = false;
    //        if (Pools != null && Pools.Values != null && Pools.Values.Count > 0)
    //        {
    //            foreach (AccessPool pool in Pools.Values)
    //            {
    //                Trace.WriteLine("CheckPool " + Pools.Count.ToString());
    //                if (pool.Check()) IsClose = true;
    //                Trace.WriteLine("CheckPool " + Pools.Count.ToString());
    //            }
    //        }
    //        if (IsClose) CreateAndStartTimer();
    //        //// 所有连接池都没有连接被关闭，那么，停止计时器，节省线程资源
    //        //if (!IsClose && CheckPoolTimer != null)
    //        //{
    //        //    lock (CheckPoolTimer)
    //        //    {
    //        //        if (!IsClose && CheckPoolTimer != null)
    //        //        {
    //        //            CheckPoolTimer.Dispose();
    //        //            CheckPoolTimer = null;
    //        //        }
    //        //    }
    //        //}
    //    }
    //    #endregion
    //}
    #endregion

    #region ADOX封装
    internal class ADOTabe : IDisposable
    {
        #region ADOX属性
        private Table _Table;
        /// <summary>表</summary>
        public Table Table
        {
            get
            {
                if (_Table == null) _Table = Cat.Tables[TableName];
                return _Table;
            }
        }

        private String _ConnectionString;
        /// <summary>连接字符串</summary>
        public String ConnectionString
        {
            get { return _ConnectionString; }
            set { _ConnectionString = value; }
        }

        private String _FileName;
        /// <summary>文件名</summary>
        public String FileName
        {
            get { return _FileName; }
            set { _FileName = value; }
        }

        private ConnectionClass _Conn;
        /// <summary>链接</summary>
        public ConnectionClass Conn
        {
            get
            {
                if (_Conn == null)
                {
                    _Conn = new ConnectionClass();
                    _Conn.Open(ConnectionString, null, null, 0);
                }
                return _Conn;
            }
        }

        private Catalog _Cat;
        /// <summary></summary>
        public Catalog Cat
        {
            get
            {
                if (_Cat == null)
                {
                    _Cat = new CatalogClass();
                    _Cat.ActiveConnection = Conn;
                }
                return _Cat;
            }
        }
        #endregion

        #region DAO属性
        private String _TableName;
        /// <summary>表名</summary>
        public String TableName
        {
            get { return _TableName; }
            set { _TableName = value; }
        }

        private TableDef _TableDef;
        /// <summary>表定义</summary>
        public TableDef TableDef
        {
            get
            {
                if (_TableDef == null) _TableDef = Db.TableDefs[TableName];
                return _TableDef;
            }
        }

        private DBEngineClass _Dbe;
        /// <summary>链接</summary>
        public DBEngineClass Dbe
        {
            get
            {
                if (_Dbe == null) _Dbe = new DBEngineClass();
                return _Dbe;
            }
        }

        private DAO.Database _Db;
        /// <summary></summary>
        public DAO.Database Db
        {
            get
            {
                if (_Db == null) _Db = Dbe.OpenDatabase(FileName, null, null, null);
                return _Db;
            }
        }
        #endregion

        #region 扩展属性
        private List<ADOColumn> _Columns;
        /// <summary>字段集合</summary>
        public List<ADOColumn> Columns
        {
            get
            {
                if (_Columns == null)
                {
                    Dictionary<String, DAO.Field> dic = new Dictionary<string, DAO.Field>();
                    foreach (DAO.Field item in TableDef.Fields)
                    {
                        dic.Add(item.Name, item);
                    }

                    _Columns = new List<ADOColumn>();
                    foreach (Column item in Table.Columns)
                    {
                        _Columns.Add(new ADOColumn(this, item, dic[item.Name]));
                        //_Columns.Add(new ADOColumn(this, item));
                    }
                }
                return _Columns;
            }
        }

        /// <summary>
        /// 是否支持
        /// </summary>
        public Boolean Supported
        {
            get
            {
                try
                {
                    return Conn != null;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>描述</summary>
        public String Description
        {
            get
            {
                DAO.Property p = TableDef.Properties["Description"];
                if (p == null && p.Value == null)
                    return null;
                else
                    return p.Value.ToString();
            }
            set
            {
                DAO.Property p = null;
                try
                {
                    p = TableDef.Properties["Description"];
                }
                catch { }

                if (p != null)
                {
                    p.Value = value;
                }
                else
                {
                    try
                    {
                        p = TableDef.CreateProperty("Description", DAO.DataTypeEnum.dbText, value, false);
                        //Thread.Sleep(1000);
                        TableDef.Properties.Append(p);
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteLine("表" + Table.Name + "没有Description属性！" + ex.ToString()); ;
#if DEBUG
                        throw new Exception("表" + Table.Name + "没有Description属性！", ex);
#endif
                    }
                }
            }
        }
        #endregion

        #region 构造
        public ADOTabe(String connstr, String filename, String tablename)
        {
            ConnectionString = connstr;
            FileName = filename;
            TableName = tablename;
        }

        ~ADOTabe()
        {
            Dispose();
        }

        private Boolean disposed = false;
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            if (_Columns != null && _Columns.Count > 0)
            {
                foreach (ADOColumn item in _Columns)
                {
                    item.Dispose();
                }
            }
            if (_Table != null) Marshal.ReleaseComObject(_Table);
            if (_Cat != null) Marshal.ReleaseComObject(_Cat);
            if (_Conn != null)
            {
                _Conn.Close();
                Marshal.ReleaseComObject(_Conn);
            }

            if (_TableDef != null) Marshal.ReleaseComObject(_TableDef);
            if (_Db != null)
            {
                _Db.Close();
                Marshal.ReleaseComObject(_Db);
            }
            if (_Dbe != null) Marshal.ReleaseComObject(_Dbe);
        }
        #endregion
    }

    internal class ADOColumn : IDisposable
    {
        #region 属性
        private Column _Column;
        /// <summary>字段</summary>
        public Column Column
        {
            get { return _Column; }
            set { _Column = value; }
        }

        private ADOTabe _Table;
        /// <summary>表</summary>
        public ADOTabe Table
        {
            get { return _Table; }
            set { _Table = value; }
        }
        #endregion

        #region DAO属性
        private DAO.Field _Field;
        /// <summary>字段</summary>
        public DAO.Field Field
        {
            get { return _Field; }
            set { _Field = value; }
        }
        #endregion

        #region 扩展属性
        /// <summary>
        /// 名称
        /// </summary>
        public String Name
        {
            get { return Column.Name; }
            set { Column.Name = value; }
        }

        /// <summary>描述</summary>
        public String Description
        {
            get
            {
                ADOX.Property p = Column.Properties["Description"];
                if (p == null && p.Value == null)
                    return null;
                else
                    return p.Value.ToString();
            }
            set
            {
                ADOX.Property p = Column.Properties["Description"];
                if (p != null)
                    p.Value = value;
                else
                    throw new Exception("列" + Column.Name + "没有Description属性！");
            }
        }

        /// <summary>描述</summary>
        public String Default
        {
            get
            {
                ADOX.Property p = Column.Properties["Default"];
                if (p == null && p.Value == null)
                    return null;
                else
                    return p.Value.ToString();
            }
            set
            {
                ADOX.Property p = Column.Properties["Default"];
                if (p != null)
                    p.Value = value;
                else
                    throw new Exception("列" + Column.Name + "没有Default属性！");
            }
        }

        /// <summary>
        /// 是否自增
        /// </summary>
        public Boolean AutoIncrement
        {
            get
            {
                ADOX.Property p = Column.Properties["Autoincrement"];
                if (p == null && p.Value == null)
                    return false;
                else
                    return (Boolean)p.Value;
            }
            set
            {
                ADOX.Property p = Column.Properties["Autoincrement"];
                if (p != null)
                    p.Value = value;
                else
                    throw new Exception("列" + Column.Name + "没有Autoincrement属性！");
            }
        }

        /// <summary>
        /// 是否允许空
        /// </summary>
        public Boolean Nullable
        {
            get
            {
                ADOX.Property p = Column.Properties["Nullable"];
                if (p == null && p.Value == null)
                    return false;
                else
                    return (Boolean)p.Value;
            }
            set
            {
                ADOX.Property p = Column.Properties["Nullable"];
                if (p != null)
                    p.Value = value;
                else
                    throw new Exception("列" + Column.Name + "没有Nullable属性！");
            }
        }
        #endregion

        #region 构造
        public ADOColumn(ADOTabe table, Column column, DAO.Field field)
        {
            Table = table;
            Column = column;
            Field = field;
        }

        //public ADOColumn(ADOTabe table, Column column)
        //{
        //    Table = table;
        //    Column = column;
        //}

        ~ADOColumn()
        {
            Dispose();
        }

        private Boolean disposed = false;
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            if (Column != null) Marshal.ReleaseComObject(Column);
        }
        #endregion
    }
    #endregion
}