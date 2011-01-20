using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web;
using NewLife.Threading;
using XCode.Exceptions;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// SQLite数据库
    /// </summary>
    internal class SQLiteSession : DbSession<SQLiteSession>
    {
        #region 属性
        ///// <summary>
        ///// 返回数据库类型。
        ///// </summary>
        //public override DatabaseType DbType
        //{
        //    get { return DatabaseType.SQLite; }
        //}

        //private static DbProviderFactory _dbProviderFactory;
        ///// <summary>
        ///// 静态构造函数
        ///// </summary>
        //static DbProviderFactory dbProviderFactory
        //{
        //    get
        //    {
        //        if (_dbProviderFactory == null)
        //        {
        //            Module module = typeof(Object).Module;

        //            PortableExecutableKinds kind;
        //            ImageFileMachine machine;
        //            module.GetPEKind(out kind, out machine);

        //            //反射实现获取数据库工厂
        //            String file = "System.Data.SQLite.dll";
        //            //if (machine != ImageFileMachine.I386) file = "System.Data.SQLite64.dll";

        //            if (String.IsNullOrEmpty(HttpRuntime.AppDomainAppId))
        //                file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file);
        //            else
        //                file = Path.Combine(HttpRuntime.BinDirectory, file);

        //            //if (!File.Exists(file) && machine == ImageFileMachine.I386)
        //            //{
        //            //    file = "System.Data.SQLite32.dll";
        //            //    file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file);
        //            //}
        //            if (!File.Exists(file)) throw new InvalidOperationException("缺少文件" + file + "！");

        //            Assembly asm = Assembly.LoadFile(file);
        //            Type type = asm.GetType("System.Data.SQLite.SQLiteFactory");
        //            FieldInfo field = type.GetField("Instance");
        //            _dbProviderFactory = field.GetValue(null) as DbProviderFactory;
        //        }
        //        return _dbProviderFactory;
        //    }
        //}

        ///// <summary>工厂</summary>
        //public override DbProviderFactory Factory
        //{
        //    get { return dbProviderFactory; }
        //}

        /// <summary>文件</summary>
        public String FileName
        {
            get { return (Database as SQLite).FileName; }
        }
        #endregion

        #region 基本方法 查询/执行
        /// <summary>
        /// 已重载。在没有数据库时创建数据库
        /// </summary>
        public override void Open()
        {
            if (!File.Exists(FileName))
            {
                // 提前创建目录，SQLite不会自己创建目录
                if (!Directory.Exists(Path.GetDirectoryName(FileName))) Directory.CreateDirectory(Path.GetDirectoryName(FileName));

                CreateDatabase();
            }

            base.Open();

            //// 以文件名为键创建读写锁，表示多线程将会争夺文件的写入操作
            //_lock = ReadWriteLock.Create(FileName);
        }

        private ReadWriteLock __lock = null;
        private ReadWriteLock _lock
        {
            get
            {
                // 以文件名为键创建读写锁，表示多线程将会争夺文件的写入操作
                return __lock ?? (__lock = ReadWriteLock.Create(FileName));
            }
        }

        /// <summary>
        /// 已重载。增加锁
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public override int Execute(string sql)
        {
            _lock.AcquireWrite();
            try
            {
                return base.Execute(sql);
            }
            finally
            {
                _lock.ReleaseWrite();
            }
        }

        /// <summary>
        /// 已重载。增加锁
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public override int Execute(DbCommand cmd)
        {
            _lock.AcquireWrite();
            try
            {
                return base.Execute(cmd);
            }
            finally
            {
                _lock.ReleaseWrite();
            }
        }

        /// <summary>
        /// 执行插入语句并返回新增行的自动编号
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>新增行的自动编号</returns>
        public override Int32 InsertAndGetIdentity(string sql)
        {
            _lock.AcquireWrite();
            try
            {
                ExecuteTimes++;
                sql = sql + ";Select last_insert_rowid() newid";
                if (Debug) WriteLog(sql);
                try
                {
                    DbCommand cmd = PrepareCommand();
                    cmd.CommandText = sql;
                    Object obj = cmd.ExecuteScalar();
                    if (obj == null) return 0;

                    Int32 rs = Convert.ToInt32(obj);
                    return rs;
                }
                catch (DbException ex)
                {
                    throw OnException(ex, sql);
                }
            }
            finally
            {
                AutoClose();
                _lock.ReleaseWrite();
            }
        }
        #endregion

        #region 构架
        public override List<XTable> GetTables()
        {
            DataTable dt = GetSchema("Tables", null);
            if (dt == null || dt.Rows == null || dt.Rows.Count < 1) return null;

            // 默认列出所有字段
            DataRow[] rows = dt.Select("TABLE_TYPE='table'");
            return GetTables(rows);
        }

        //protected override List<XField> GetFields(XTable xt)
        //{
        //    DataColumnCollection columns = GetColumns(xt.Name);

        //    DataTable dt = GetSchema("Columns", new String[] { null, null, xt.Name });

        //    List<XField> list = new List<XField>();
        //    DataRow[] drs = dt.Select("", "ORDINAL_POSITION");
        //    //List<String> pks = GetPrimaryKeys(xt);
        //    Int32 IDCount = 0;
        //    foreach (DataRow dr in drs)
        //    {
        //        XField xf = xt.CreateField();

        //        xf.ID = Int32.Parse(dr["ORDINAL_POSITION"].ToString());
        //        xf.Name = dr["COLUMN_NAME"].ToString();
        //        xf.RawType = dr["DATA_TYPE"].ToString();
        //        xf.Identity = Convert.ToBoolean(dr["AUTOINCREMENT"]);
        //        xf.PrimaryKey = Convert.ToBoolean(dr["PRIMARY_KEY"]);

        //        if (columns != null && columns.Contains(xf.Name))
        //        {
        //            DataColumn dc = columns[xf.Name];
        //            xf.DataType = dc.DataType;
        //        }

        //        if (Type.GetTypeCode(xf.DataType) == TypeCode.Int32 || Type.GetTypeCode(xf.DataType) == TypeCode.Double)
        //        {
        //            xf.Length = dr["NUMERIC_PRECISION"] == DBNull.Value ? 0 : Int32.Parse(dr["NUMERIC_PRECISION"].ToString());
        //            xf.NumOfByte = 0;
        //            xf.Digit = dr["NUMERIC_SCALE"] == DBNull.Value ? 0 : Int32.Parse(dr["NUMERIC_SCALE"].ToString());
        //        }
        //        else if (Type.GetTypeCode(xf.DataType) == TypeCode.DateTime)
        //        {
        //            xf.Length = dr["DATETIME_PRECISION"] == DBNull.Value ? 0 : Int32.Parse(dr["DATETIME_PRECISION"].ToString());
        //            xf.NumOfByte = 0;
        //            xf.Digit = 0;
        //        }
        //        else
        //        {
        //            if (dr["DATA_TYPE"].ToString() == "130" && dr["COLUMN_FLAGS"].ToString() == "234") //备注类型
        //            {
        //                xf.Length = Int32.MaxValue;
        //                xf.NumOfByte = Int32.MaxValue;
        //            }
        //            else
        //            {
        //                xf.Length = dr["CHARACTER_MAXIMUM_LENGTH"] == DBNull.Value ? 0 : Int32.Parse(dr["CHARACTER_MAXIMUM_LENGTH"].ToString());
        //                xf.NumOfByte = dr["CHARACTER_OCTET_LENGTH"] == DBNull.Value ? 0 : Int32.Parse(dr["CHARACTER_OCTET_LENGTH"].ToString());
        //            }
        //            xf.Digit = 0;
        //        }

        //        try
        //        {
        //            xf.Nullable = Boolean.Parse(dr["IS_NULLABLE"].ToString());
        //        }
        //        catch
        //        {
        //            xf.Nullable = dr["IS_NULLABLE"].ToString() == "YES";
        //        }
        //        try
        //        {
        //            xf.Default = dr["COLUMN_HASDEFAULT"].ToString() == "False" ? "" : dr["COLUMN_DEFAULT"].ToString();
        //        }
        //        catch
        //        {
        //            xf.Default = dr["COLUMN_DEFAULT"].ToString();
        //        }
        //        try
        //        {
        //            xf.Description = dr["DESCRIPTION"] == DBNull.Value ? "" : dr["DESCRIPTION"].ToString();
        //        }
        //        catch
        //        {
        //            xf.Description = "";
        //        }

        //        //处理默认值
        //        while (!String.IsNullOrEmpty(xf.Default) && xf.Default[0] == '(' && xf.Default[xf.Default.Length - 1] == ')')
        //        {
        //            xf.Default = xf.Default.Substring(1, xf.Default.Length - 2);
        //        }
        //        if (!String.IsNullOrEmpty(xf.Default)) xf.Default = xf.Default.Trim(new Char[] { '"', '\'' });

        //        //修正自增字段属性
        //        if (xf.Identity)
        //        {
        //            if (xf.Nullable)
        //                xf.Identity = false;
        //            else if (!String.IsNullOrEmpty(xf.Default))
        //                xf.Identity = false;
        //        }
        //        if (xf.Identity) IDCount++;

        //        list.Add(xf);
        //    }

        //    //再次修正自增字段
        //    if (IDCount > 1)
        //    {
        //        foreach (XField xf in list)
        //        {
        //            if (!xf.Identity) continue;

        //            if (!String.Equals(xf.Name, "ID", StringComparison.OrdinalIgnoreCase))
        //            {
        //                xf.Identity = false;
        //                IDCount--;
        //            }
        //        }
        //    }
        //    if (IDCount > 1)
        //    {
        //        foreach (XField xf in list)
        //        {
        //            if (!xf.Identity) continue;

        //            if (xf.ID > 1)
        //            {
        //                xf.Identity = false;
        //                IDCount--;
        //            }
        //        }
        //    }

        //    return list;
        //}
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

                    if (File.Exists(FileName)) File.Delete(FileName);
                    return null;
                case DDLSchema.DatabaseExist:
                    return File.Exists(FileName);
                case DDLSchema.CreateTable:
                    obj = base.SetSchema(DDLSchema.CreateTable, values);
                    //XTable table = values[0] as XTable;
                    //if (!String.IsNullOrEmpty(table.Description)) AddTableDescription(table.Name, table.Description);
                    //foreach (XField item in table.Fields)
                    //{
                    //    if (!String.IsNullOrEmpty(item.Description)) AddColumnDescription(table.Name, item.Name, item.Description);
                    //}
                    return obj;
                case DDLSchema.DropTable:
                    break;
                case DDLSchema.TableExist:
                    DataTable dt = GetSchema("Tables", new String[] { null, null, (String)values[0], "TABLE" });
                    if (dt == null || dt.Rows == null || dt.Rows.Count < 1) return false;
                    return true;
                //case DDLSchema.AddTableDescription:
                //    return AddTableDescription((String)values[0], (String)values[1]);
                //case DDLSchema.DropTableDescription:
                //    return DropTableDescription((String)values[0]);
                case DDLSchema.AddColumn:
                    obj = base.SetSchema(DDLSchema.AddColumn, values);
                    //AddColumnDescription((String)values[0], ((XField)values[1]).Name, ((XField)values[1]).Description);
                    return obj;
                case DDLSchema.AlterColumn:
                    break;
                case DDLSchema.DropColumn:
                    break;
                //case DDLSchema.AddColumnDescription:
                //    return AddColumnDescription((String)values[0], (String)values[1], (String)values[2]);
                //case DDLSchema.DropColumnDescription:
                //    return DropColumnDescription((String)values[0], (String)values[1]);
                //case DDLSchema.AddDefault:
                //    return AddDefault((String)values[0], (String)values[1], (String)values[2]);
                //case DDLSchema.DropDefault:
                //    return DropDefault((String)values[0], (String)values[1]);
                default:
                    break;
            }
            return base.SetSchema(schema, values);
        }

        public override string AlterColumnSQL(string tablename, XField field)
        {
            return null;
        }

        public override string FieldClause(XField field, bool onlyDefine)
        {
            StringBuilder sb = new StringBuilder();

            //字段名
            sb.AppendFormat("{0} ", FormatKeyWord(field.Name));

            //类型
            TypeCode tc = Type.GetTypeCode(field.DataType);
            switch (tc)
            {
                case TypeCode.Boolean:
                    sb.Append("BOOLEAN");
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
                    sb.AppendFormat("decimal");
                    break;
                case TypeCode.Double:
                    sb.Append("double");
                    break;
                case TypeCode.Empty:
                    break;
                case TypeCode.Int16:
                case TypeCode.UInt16:
                //sb.Append("smallint");
                //break;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                //sb.Append("interger");
                //break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    sb.Append("INTEGER");
                    break;
                case TypeCode.Object:
                    break;
                case TypeCode.SByte:
                    sb.Append("byte");
                    break;
                case TypeCode.Single:
                    sb.Append("float");
                    break;
                case TypeCode.String:
                    Int32 len = field.Length;
                    if (len < 1) len = 50;
                    if (len > 4000)
                        sb.Append("TEXT");
                    else
                        sb.AppendFormat("nvarchar({0})", len);
                    break;
                default:
                    break;
            }

            if (field.PrimaryKey)
            {
                sb.Append(" primary key");

                // 如果不加这个关键字，SQLite会利用最大值加1的方式，所以，最大行被删除后，它的编号将会被重用
                if (onlyDefine && field.Identity) sb.Append(" autoincrement");
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
                    if (String.Equals(d, "getdate()", StringComparison.OrdinalIgnoreCase)) d = Database.DateTimeNow;
                    if (String.Equals(d, "now()", StringComparison.OrdinalIgnoreCase)) d = Database.DateTimeNow;
                    sb.AppendFormat(" DEFAULT {0}", d);
                }
                else
                    sb.AppendFormat(" DEFAULT {0}", field.Default);
            }

            return sb.ToString();
        }
        #endregion

        #region 创建数据库
        private void CreateDatabase()
        {
            // 提前创建目录，SQLite不会自己创建目录
            if (!Directory.Exists(Path.GetDirectoryName(FileName))) Directory.CreateDirectory(Path.GetDirectoryName(FileName));

            File.Create(FileName);
        }
        #endregion

        #region 表和字段备注
        //public Boolean AddTableDescription(String tablename, String description)
        //{
        //    try
        //    {
        //        using (ADOTabe table = new ADOTabe(ConnectionString, FileName, tablename))
        //        {
        //            table.Description = description;
        //            return true;
        //        }
        //    }
        //    catch { return false; }
        //}

        //public Boolean DropTableDescription(String tablename)
        //{
        //    return AddTableDescription(tablename, null);
        //}

        //public Boolean AddColumnDescription(String tablename, String columnname, String description)
        //{
        //    try
        //    {
        //        using (ADOTabe table = new ADOTabe(ConnectionString, FileName, tablename))
        //        {
        //            if (table.Supported && table.Columns != null)
        //            {
        //                foreach (ADOColumn item in table.Columns)
        //                {
        //                    if (item.Name == columnname)
        //                    {
        //                        item.Description = description;
        //                        return true;
        //                    }
        //                }
        //            }
        //            return false;
        //        }
        //    }
        //    catch { return false; }
        //}

        //public Boolean DropColumnDescription(String tablename, String columnname)
        //{
        //    return AddColumnDescription(tablename, columnname, null);
        //}
        #endregion

        #region 默认值
        //public virtual Boolean AddDefault(String tablename, String columnname, String value)
        //{
        //    try
        //    {
        //        using (ADOTabe table = new ADOTabe(ConnectionString, FileName, tablename))
        //        {
        //            if (table.Supported && table.Columns != null)
        //            {
        //                foreach (ADOColumn item in table.Columns)
        //                {
        //                    if (item.Name == columnname)
        //                    {
        //                        item.Default = value;
        //                        return true;
        //                    }
        //                }
        //            }
        //            return false;
        //        }
        //    }
        //    catch { return false; }
        //}

        //public virtual Boolean DropDefault(String tablename, String columnname)
        //{
        //    return AddDefault(tablename, columnname, null);
        //}
        #endregion
    }

    class SQLite : DbBase<SQLite, SQLiteSession>
    {
        #region 属性
        /// <summary>
        /// 返回数据库类型。
        /// </summary>
        public override DatabaseType DbType
        {
            get { return DatabaseType.SQLite; }
        }

        private static DbProviderFactory _dbProviderFactory;
        /// <summary>
        /// 静态构造函数
        /// </summary>
        static DbProviderFactory dbProviderFactory
        {
            get
            {
                if (_dbProviderFactory == null)
                {
                    Module module = typeof(Object).Module;

                    PortableExecutableKinds kind;
                    ImageFileMachine machine;
                    module.GetPEKind(out kind, out machine);

                    //反射实现获取数据库工厂
                    String file = "System.Data.SQLite.dll";
                    //if (machine != ImageFileMachine.I386) file = "System.Data.SQLite64.dll";

                    if (String.IsNullOrEmpty(HttpRuntime.AppDomainAppId))
                        file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file);
                    else
                        file = Path.Combine(HttpRuntime.BinDirectory, file);

                    //if (!File.Exists(file) && machine == ImageFileMachine.I386)
                    //{
                    //    file = "System.Data.SQLite32.dll";
                    //    file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file);
                    //}
                    if (!File.Exists(file)) throw new InvalidOperationException("缺少文件" + file + "！");

                    Assembly asm = Assembly.LoadFile(file);
                    Type type = asm.GetType("System.Data.SQLite.SQLiteFactory");
                    FieldInfo field = type.GetField("Instance");
                    _dbProviderFactory = field.GetValue(null) as DbProviderFactory;
                }
                return _dbProviderFactory;
            }
        }

        /// <summary>工厂</summary>
        public override DbProviderFactory Factory
        {
            get { return dbProviderFactory; }
        }

        /// <summary>链接字符串</summary>
        public override string ConnectionString
        {
            get
            {
                return base.ConnectionString;
            }
            set
            {
                base.ConnectionString = value;
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
                    //throw new XDbException(this, "分析SQLite连接字符串时出错", ex);
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

        #region 分页
        /// <summary>
        /// 已重写。获取分页
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="startRowIndex">开始行，0开始</param>
        /// <param name="maximumRows">最大返回行数</param>
        /// <param name="keyColumn">主键列。用于not in分页</param>
        /// <returns></returns>
        public override string PageSplit(string sql, Int32 startRowIndex, Int32 maximumRows, string keyColumn)
        {
            // 从第一行开始，不需要分页
            if (startRowIndex <= 0)
            {
                if (maximumRows < 1)
                    return sql;
                else
                    return String.Format("{0} limit {1}", sql, maximumRows);
            }
            if (maximumRows < 1)
                throw new NotSupportedException("不支持取第几条数据之后的所有数据！");
            else
                sql = String.Format("{0} limit {1}, {2}", sql, startRowIndex, maximumRows);
            return sql;
        }

        /// <summary>
        /// 已重写。获取分页
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="startRowIndex"></param>
        /// <param name="maximumRows"></param>
        /// <param name="keyColumn"></param>
        /// <returns></returns>
        public override string PageSplit(SelectBuilder builder, int startRowIndex, int maximumRows, string keyColumn)
        {
            return PageSplit(builder.ToString(), startRowIndex, maximumRows, keyColumn);
        }
        #endregion

        #region 数据库特性
        /// <summary>
        /// 当前时间函数
        /// </summary>
        public override String DateTimeNow { get { return "CURRENT_TIMESTAMP"; } }

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
            return String.Format("'{0:yyyy-MM-dd HH:mm:ss}'", dateTime);
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
}
