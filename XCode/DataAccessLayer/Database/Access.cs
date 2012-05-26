using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using ADODB;
using ADOX;
using DAO;
using NewLife;
using NewLife.IO;
using NewLife.Log;
using NewLife.Reflection;
using XCode.Exceptions;

#if NET4
using ConnectionClass = ADODB.Connection;
using DBEngineClass = DAO.DBEngine;
using CatalogClass = ADOX.Catalog;
#endif

namespace XCode.DataAccessLayer
{
    class Access : FileDbBase
    {
        #region 属性
        /// <summary>返回数据库类型。外部DAL数据库类请使用Other</summary>
        public override DatabaseType DbType
        {
            get { return DatabaseType.Access; }
        }

        private static DbProviderFactory _dbProviderFactory;
        /// <summary>工厂</summary>
        public override DbProviderFactory Factory
        {
            get
            {
                if (_dbProviderFactory == null)
                {
                    _dbProviderFactory = OleDbFactory.Instance;

                    // 检查ADOX
#if !NET4
                    CheckAndDownload("Interop.ADOX.dll");
#endif
                }
                return _dbProviderFactory;
            }
        }

        protected override string DefaultConnectionString
        {
            get
            {
                var builder = Factory.CreateConnectionStringBuilder();
                if (builder != null)
                {
                    var name = Path.GetTempFileName();
                    FileSource.ReleaseFile(Assembly.GetExecutingAssembly(), "Database.mdb", name, true);

                    builder[_.DataSource] = name;
                    builder["Provider"] = "Microsoft.Jet.OLEDB.4.0";
                    return builder.ToString();
                }

                return base.DefaultConnectionString;
            }
        }
        #endregion

        #region 方法
        /// <summary>创建数据库会话</summary>
        /// <returns></returns>
        protected override IDbSession OnCreateSession()
        {
            return new AccessSession();
        }

        /// <summary>创建元数据对象</summary>
        /// <returns></returns>
        protected override IMetaData OnCreateMetaData()
        {
            return new AccessMetaData();
        }

        public override bool Support(string providerName)
        {
            providerName = providerName.ToLower();
            if (providerName.Contains("microsoft.jet.oledb")) return true;
            if (providerName.Contains("access")) return true;
            if (providerName.Contains("oledb")) return true;

            return false;
        }

        protected override void OnSetConnectionString(XDbConnectionStringBuilder builder)
        {
            base.OnSetConnectionString(builder);

            // 特别处理一下Excel
            if (!String.IsNullOrEmpty(FileName))
            {
                String ext = Path.GetExtension(FileName);
                if (ext.EqualIgnoreCase(".xls"))
                {
                    if (!builder.ContainsKey("Extended Properties")) builder["Extended Properties"] = "Excel 8.0";
                }
            }
        }
        #endregion

        #region 数据库特性
        /// <summary>当前时间函数</summary>
        public override String DateTimeNow { get { return "now()"; } }

        /// <summary>最小时间</summary>
        public override DateTime DateTimeMin { get { return DateTime.MinValue; } }

        /// <summary>长文本长度</summary>
        public override Int32 LongTextLength { get { return 255; } }

        //protected override string ReservedWordsStr
        //{
        //    get { return "ABSOLUTE,ACTION,ADD,ALL,ALLOCATE,ALTER,AND,ANY,ARE,AS,ASC,ASSERTION,AT,AUTHORIZATION,AVG,BEGIN,BETWEEN,BIT,BIT_LENGTH,BOTH,BY,CASCADE,CASCADED,CASE,CAST,CATALOG,CHAR,CHAR_LENGTH,CHARACTER,CHARACTER_LENGTH,CHECK,CLOSE,COALESCE,COLLATE,COLLATION,COLUMN,COMMIT,CONNECT,CONNECTION,CONSTRAINT,CONSTRAINTS,CONTINUE,CONVERT,CORRESPONDING,COUNT,CREATE,CROSS,CURRENT,CURRENT_DATE,CURRENT_TIME,CURRENT_TIMESTAMP,CURRENT_USER,CURSOR,DATE,DAY,DEALLOCATE,DEC,DECIMAL,DECLARE,DEFAULT,DEFERRABLE,DEFERRED,DELETE,DESC,DESCRIBE,DESCRIPTOR,DIAGNOSTICS,DISCONNECT,DISTINCT,DISTINCTROW,DOMAIN,DOUBLE,DROP,ELSE,END,END-EXEC,ESCAPE,EXCEPT,EXCEPTION,EXEC,EXECUTE,EXISTS,EXTERNAL,EXTRACT,FALSE,FETCH,FIRST,FLOAT,FOR,FOREIGN,FOUND,FROM,FULL,GET,GLOBAL,GO,GOTO,GRANT,GROUP,HAVING,HOUR,IDENTITY,IMMEDIATE,IN,INDICATOR,INITIALLY,INNER,INPUT,INSENSITIVE,INSERT,INT,INTEGER,INTERSECT,INTERVAL,INTO,IS,ISOLATION,JOIN,KEY,LANGUAGE,LAST,LEADING,LEFT,LEVEL,LIKE,LOCAL,LOWER,MATCH,MAX,MIN,MINUTE,MODULE,MONTH,NAMES,NATIONAL,NATURAL,NCHAR,NEXT,NO,NOT,NULL,NULLIF,NUMERIC,OCTET_LENGTH,OF,ON,ONLY,OPEN,OPTION,OR,ORDER,OUTER,OUTPUT,OVERLAPS,PARTIAL,POSITION,PRECISION,PREPARE,PRESERVE,PRIMARY,PRIOR,PRIVILEGES,PROCEDURE,PUBLIC,READ,REAL,REFERENCES,RELATIVE,RESTRICT,REVOKE,RIGHT,ROLLBACK,ROWS,SCHEMA,SCROLL,SECOND,SECTION,SELECT,SESSION,SESSION_USER,SET,SIZE,SMALLINT,SOME,SQL,SQLCODE,SQLERROR,SQLSTATE,SUBSTRING,SUM,SYSTEM_USER,TABLE,TEMPORARY,THEN,TIME,TIMESTAMP,TIMEZONE_HOUR,TIMEZONE_MINUTE,TO,TRAILING,TRANSACTION,TRANSLATE,TRANSLATION,TRIGGER,TRIM,TRUE,UNION,UNIQUE,UNKNOWN,UPDATE,UPPER,USAGE,USER,USING,VALUE,VALUES,VARCHAR,VARYING,VIEW,WHEN,WHENEVER,WHERE,WITH,WORK,WRITE,YEAR,ZONE,AdminDB,Alphanumeric,Autoincrement,BAND,Binary,BNOT,BOR,BXOR,Byte,Comp,Compression,Container,Counter,CreateDB,Currency,Database,DateTime,Disallow,ExclusiveConnect,Float4,Float8,General,Guid,IEEEDouble,IEEESingle,Ignore,Image,Index,Inheritable,Integer1,Integer2,Integer4,Logical,Logical1,Long,LongBinary,LongChar,LongText,Memo,Money,Note,Number,Object,OLEObject,OwnerAccess,Pad,Parameters,Password,Percent,Pivot,Proc,SelectSchema,SelectSecurity,Short,Single,Space,String,Tableid,Text,Top,Transform,Uniqueidentifier,UpdateIdentity,UpdateOwner,UpdateSecurity,Varbinary,YesNo"; }
        //}

        public override string FormatName(string name)
        {
            if (!String.IsNullOrEmpty(name) && name.Contains("$"))
                return FormatKeyWord(name);
            else
                return base.FormatName(name);
        }

        /// <summary>格式化时间为SQL字符串</summary>
        /// <param name="dateTime">时间值</param>
        /// <returns></returns>
        public override String FormatDateTime(DateTime dateTime)
        {
            return String.Format("#{0:yyyy-MM-dd HH:mm:ss}#", dateTime);
        }

        /// <summary>格式化关键字</summary>
        /// <param name="keyWord">关键字</param>
        /// <returns></returns>
        public override String FormatKeyWord(String keyWord)
        {
            //if (String.IsNullOrEmpty(keyWord)) throw new ArgumentNullException("keyWord");
            if (String.IsNullOrEmpty(keyWord)) return keyWord;

            if (keyWord.StartsWith("[") && keyWord.EndsWith("]")) return keyWord;

            return String.Format("[{0}]", keyWord);
            //return keyWord;
        }

        /// <summary>格式化数据为SQL数据</summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override string FormatValue(IDataColumn field, object value)
        {
            if (field != null && field.DataType == typeof(Boolean) || value != null && value.GetType() == typeof(Boolean))
            {
                if (value == null) return field.Nullable ? "null" : "";

                return value.ToString();
            }

            return base.FormatValue(field, value);
        }
        #endregion

        #region 分页
        public override SelectBuilder PageSplit(SelectBuilder builder, int startRowIndex, int maximumRows)
        {
            return MSPageSplit.PageSplit(builder, startRowIndex, maximumRows, false, b => CreateSession().QueryCount(b));
        }
        #endregion

        #region 平台检查
        /// <summary>是否支持</summary>
        public static void CheckSupport()
        {
            Module module = typeof(Object).Module;

            PortableExecutableKinds kind;
            ImageFileMachine machine;
            module.GetPEKind(out kind, out machine);

            if (machine != ImageFileMachine.I386) throw new NotSupportedException("64位平台不支持OLEDB驱动！");
        }
        #endregion
    }

    /// <summary>Access数据库</summary>
    internal class AccessSession : FileDbSession
    {
        #region 方法
        /// <summary>打开。已重写，为了建立数据库</summary>
        public override void Open()
        {
            Access.CheckSupport();

            base.Open();
        }
        #endregion

        #region 基本方法 查询/执行
        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>新增行的自动编号</returns>
        public override Int64 InsertAndGetIdentity(String sql, CommandType type = CommandType.Text, params DbParameter[] ps)
        {
            Boolean b = IsAutoClose;
            // 禁用自动关闭，保证两次在同一会话
            IsAutoClose = false;

            BeginTransaction();
            try
            {
                Int64 rs = Execute(sql, type, ps);
                if (rs > 0) rs = ExecuteScalar<Int64>("Select @@Identity");
                Commit();
                return rs;
            }
            catch { Rollback(); throw; }
            finally
            {
                IsAutoClose = b;
                AutoClose();
            }
        }
        #endregion
    }

    /// <summary>Access元数据</summary>
    class AccessMetaData : FileDbMetaData
    {
        #region 构架
        protected override List<IDataTable> OnGetTables(ICollection<String> names)
        {
            DataTable dt = GetSchema(_.Tables, null);
            if (dt == null || dt.Rows == null || dt.Rows.Count < 1) return null;

            // 默认列出所有字段
            DataRow[] rows = dt.Select(String.Format("{0}='Table' Or {0}='View'", "TABLE_TYPE"));
            rows = OnGetTables(names, rows);
            if (rows == null || rows.Length < 1) return null;

            return GetTables(rows);
        }

        protected override List<IDataColumn> GetFields(IDataTable xt)
        {
            List<IDataColumn> list = base.GetFields(xt);
            if (list == null || list.Count < 1) return null;

            Dictionary<String, IDataColumn> dic = new Dictionary<String, IDataColumn>(StringComparer.OrdinalIgnoreCase);
            foreach (IDataColumn xf in list)
            {
                dic.Add(xf.Name, xf);
            }

            try
            {
                using (ADOTabe table = GetTable(xt.Name))
                {
                    if (table.Supported && table.Columns != null)
                    {
                        foreach (ADOColumn item in table.Columns)
                        {
                            if (!dic.ContainsKey(item.Name)) continue;

                            IDataColumn field = dic[item.Name];
                            field.Identity = item.AutoIncrement;
                            if (!field.Identity) field.Nullable = item.Nullable;
                        }
                    }
                }
            }
            catch { }

            return list;
        }

        protected override void FixField(IDataColumn field, DataRow drColumn)
        {
            base.FixField(field, drColumn);

            // 字段标识
            Int64 flag = GetDataRowValue<Int64>(drColumn, "COLUMN_FLAGS");

            Boolean? isLong = null;

            Int32 id = 0;
            if (Int32.TryParse(GetDataRowValue<String>(drColumn, "DATA_TYPE"), out id))
            {
                DataRow[] drs = FindDataType(field, "" + id, isLong);
                if (drs != null && drs.Length > 0)
                {
                    String typeName = GetDataRowValue<String>(drs[0], "TypeName");
                    field.RawType = typeName;

                    if (TryGetDataRowValue<String>(drs[0], "DataType", out typeName)) field.DataType = TypeX.GetType(typeName);

                    // 修正备注类型
                    if (field.DataType == typeof(String) && drs.Length > 1)
                    {
                        isLong = (flag & 0x80) == 0x80;
                        drs = FindDataType(field, "" + id, isLong);
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

        protected override void FixField(IDataColumn field, DataRow drColumn, DataRow drDataType)
        {
            base.FixField(field, drColumn, drDataType);

            // 修正原始类型
            String typeName = null;
            if (TryGetDataRowValue<String>(drDataType, "TypeName", out typeName)) field.RawType = typeName;
        }

        protected override List<IDataIndex> GetIndexes(IDataTable table)
        {
            List<IDataIndex> list = base.GetIndexes(table);
            if (list != null && list.Count > 0)
            {
                // Access的索引直接以索引字段的方式排布，所以需要重新组合起来
                Dictionary<String, IDataIndex> dic = new Dictionary<String, IDataIndex>();
                foreach (IDataIndex item in list)
                {
                    IDataIndex di = null;
                    if (!dic.TryGetValue(item.Name, out di))
                    {
                        dic.Add(item.Name, item);
                    }
                    else
                    {
                        List<String> ss = new List<string>(di.Columns);
                        if (item.Columns != null && item.Columns.Length > 0 && !ss.Contains(item.Columns[0]))
                        {
                            ss.Add(item.Columns[0]);
                            di.Columns = ss.ToArray();
                        }
                    }
                }
                list.Clear();
                foreach (IDataIndex item in dic.Values)
                {
                    list.Add(item);
                }
            }
            return list;
        }

        //protected override void FixIndex(IDataIndex index, DataRow dr)
        //{
        //    base.FixIndex(index, dr);

        //    Boolean b = false;
        //    if (TryGetDataRowValue<Boolean>(dr, "PRIMARY_KEY", out b)) index.PrimaryKey = b;
        //    if (TryGetDataRowValue<Boolean>(dr, "UNIQUE", out b)) index.PrimaryKey = b;
        //}

        protected override string GetFieldConstraints(IDataColumn field, Boolean onlyDefine)
        {
            String str = base.GetFieldConstraints(field, onlyDefine);

            if (field.Identity) str = " AUTOINCREMENT(1,1)" + str;

            return str;
        }

        /// <summary>取得字段默认值</summary>
        /// <param name="field"></param>
        /// <param name="onlyDefine">仅仅定义</param>
        /// <returns></returns>
        protected override string GetFieldDefault(IDataColumn field, bool onlyDefine)
        {
            // Access不能通过DDL来操作默认值
            return null;
        }
        #endregion

        #region 数据定义
        /// <summary>设置数据定义模式</summary>
        /// <param name="schema"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public override object SetSchema(DDLSchema schema, object[] values)
        {
            Object obj = null;
            switch (schema)
            {
                case DDLSchema.CreateTable:
                    obj = base.SetSchema(DDLSchema.CreateTable, values);
                    IDataTable table = values[0] as IDataTable;

                    // Access建表语句的不能操作默认值，所以在这里操作

                    // 默认值
                    foreach (IDataColumn item in table.Columns)
                    {
                        if (!String.IsNullOrEmpty(item.Default)) AddDefault(item, item.Default);
                    }

                    return obj;
                case DDLSchema.AddTableDescription:
                    return AddTableDescription((IDataTable)values[0], ((IDataTable)values[0]).Description);
                case DDLSchema.DropTableDescription:
                    return DropTableDescription((IDataTable)values[0]);
                case DDLSchema.AddColumnDescription:
                    return AddColumnDescription((IDataColumn)values[0], ((IDataColumn)values[0]).Description);
                case DDLSchema.DropColumnDescription:
                    return DropColumnDescription((IDataColumn)values[0]);
                case DDLSchema.AddDefault:
                    return AddDefault((IDataColumn)values[0], ((IDataColumn)values[0]).Default);
                case DDLSchema.DropDefault:
                    return DropDefault((IDataColumn)values[0]);
                default:
                    break;
            }
            return base.SetSchema(schema, values);
        }
        #endregion

        #region 创建数据库
        /// <summary>创建数据库</summary>
        protected override void CreateDatabase()
        {
            if (String.IsNullOrEmpty(FileName) || File.Exists(FileName)) return;

            DAL.WriteDebugLog("创建数据库：{0}", FileName);

            //FileSource.ReleaseFile(Assembly.GetExecutingAssembly(), "Database.mdb", FileName, true);

            var dbe = new DBEngineClass();
            try
            {
                var db = dbe.CreateDatabase(FileName, LanguageConstants.dbLangChineseSimplified);
                if (db != null)
                {
                    db.Close();
                    Marshal.ReleaseComObject(db);
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
            finally
            {
                Marshal.ReleaseComObject(dbe);
            }
        }
        #endregion

        #region 反向工程创建表
        protected override void CreateTable(StringBuilder sb, IDataTable table, bool onlySql)
        {
            base.CreateTable(sb, table, onlySql);

            if (!onlySql)
            {
                IDatabase entityDb = null;
                foreach (IDataColumn dc in table.Columns)
                {
                    // 如果实体存在默认值，则增加
                    if (!String.IsNullOrEmpty(dc.Default))
                    {
                        var tc = Type.GetTypeCode(dc.DataType);
                        String dv = dc.Default;
                        // 特殊处理时间
                        if (tc == TypeCode.DateTime)
                        {
                            if (entityDb != null && dv == entityDb.DateTimeNow) dc.Default = Database.DateTimeNow;
                        }
                        // 特殊处理Guid
                        else if (tc == TypeCode.String || dc.DataType == typeof(Guid))
                        {
                            if (entityDb != null && dv == entityDb.NewGuid) dc.Default = Database.NewGuid;
                        }

                        PerformSchema(sb, onlySql, DDLSchema.AddDefault, dc);

                        // 还原
                        dc.Default = dv;
                    }
                }
            }
        }

        public override string CreateTableSQL(IDataTable table)
        {
            String sql = base.CreateTableSQL(table);
            if (String.IsNullOrEmpty(sql) || table.PrimaryKeys == null || table.PrimaryKeys.Length < 2) return sql;

            // 处理多主键
            String[] names = new String[table.PrimaryKeys.Length];
            for (int i = 0; i < table.PrimaryKeys.Length; i++)
            {
                names[i] = table.PrimaryKeys[i].Name;
            }
            IDataIndex di = ModelHelper.GetIndex(table, names);
            if (di == null)
            {
                di = table.CreateIndex();
                di.PrimaryKey = true;
                di.Unique = true;
                di.Columns = names;
            }
            // Access里面的主键索引名必须叫这个
            di.Name = "PrimaryKey";

            sql += ";" + Environment.NewLine;
            sql += CreateIndexSQL(di);
            return sql;
        }

        public override string CreateIndexSQL(IDataIndex index)
        {
            String sql = base.CreateIndexSQL(index);
            if (String.IsNullOrEmpty(sql) || !index.PrimaryKey) return sql;

            return sql + " WITH PRIMARY";
        }
        #endregion

        #region 表和字段备注
        public Boolean AddTableDescription(IDataTable table, String value)
        {
            try
            {
                using (ADOTabe tb = GetTable(table.Name))
                {
                    tb.Description = value;
                    return true;
                }
            }
            catch { return false; }
        }

        public Boolean DropTableDescription(IDataTable table)
        {
            return AddTableDescription(table, null);
        }

        public Boolean AddColumnDescription(IDataColumn field, String value)
        {
            try
            {
                using (ADOTabe table = GetTable(field.Table.Name))
                {
                    if (table.Supported && table.Columns != null)
                    {
                        foreach (ADOColumn item in table.Columns)
                        {
                            if (item.Name == field.Name)
                            {
                                item.Description = value;
                                return true;
                            }
                        }
                    }
                    return false;
                }
            }
            catch { return false; }
        }

        public Boolean DropColumnDescription(IDataColumn field)
        {
            return AddColumnDescription(field, null);
        }
        #endregion

        #region 默认值
        public virtual Boolean AddDefault(IDataColumn field, String value)
        {
            //if (field.DataType == typeof(DateTime))
            //{
            CheckAndGetDefault(field, ref value);
            //}

            try
            {
                using (ADOTabe table = GetTable(field.Table.Name))
                {
                    if (table.Supported && table.Columns != null)
                    {
                        foreach (ADOColumn item in table.Columns)
                        {
                            if (item.Name == field.Name)
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

        public virtual Boolean DropDefault(IDataColumn field)
        {
            return AddDefault(field, null);
        }
        #endregion

        #region 数据类型
        protected override DataRow[] FindDataType(IDataColumn field, string typeName, bool? isLong)
        {
            DataRow[] drs = base.FindDataType(field, typeName, isLong);
            if (drs != null && drs.Length > 0) return drs;

            //// 处理SByte类型
            //if (typeName == typeof(SByte).FullName)
            //{
            //    typeName = typeof(Byte).FullName;
            //    drs = base.FindDataType(field, typeName, isLong);
            //    if (drs != null && drs.Length > 0) return drs;
            //}

            DataTable dt = DataTypes;
            if (dt == null) return null;

            // 转为整数
            Int32 n = 0;
            if (!Int32.TryParse(typeName, out n)) return null;

            try
            {
                if (isLong == null)
                {
                    drs = dt.Select(String.Format("NativeDataType={0}", n));
                    if (drs == null || drs.Length < 1) drs = dt.Select(String.Format("ProviderDbType={0}", n));
                }
                else
                {
                    drs = dt.Select(String.Format("NativeDataType={0} And IsLong={1}", n, isLong.Value));
                    if (drs == null || drs.Length < 1) drs = dt.Select(String.Format("ProviderDbType={0} And IsLong={1}", n, isLong.Value));
                }
            }
            catch { }

            return drs;
        }

        protected override string GetFieldType(IDataColumn field)
        {
            String typeName = base.GetFieldType(field);

            //if (typeName.StartsWith("VarChar")) return typeName.Replace("VarChar", "Text");
            if (field.Identity) return null;

            return typeName;
        }
        #endregion

        #region 辅助函数
        ADOTabe GetTable(String tableName)
        {
            return new ADOTabe(Database.ConnectionString, FileName, tableName);
        }
        #endregion
    }

    #region ADOX封装
    internal class ADOTabe : DisposeBase
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
                    //Dictionary<String, DAO.Field> dic = new Dictionary<string, DAO.Field>();
                    //foreach (DAO.Field item in TableDef.Fields)
                    //{
                    //    dic.Add(item.Name, item);
                    //}

                    _Columns = new List<ADOColumn>();
                    foreach (Column item in Table.Columns)
                    {
                        //_Columns.Add(new ADOColumn(this, item, dic[item.Name]));
                        _Columns.Add(new ADOColumn(this, item));
                    }
                }
                return _Columns;
            }
        }

        /// <summary>是否支持</summary>
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
                        XTrace.WriteLine("表" + Table.Name + "没有Description属性！" + ex.ToString());
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

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            try
            {
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
            catch (Exception ex)
            {
                if (DAL.Debug) DAL.WriteLog(ex.ToString());
            }
        }
        #endregion
    }

    internal class ADOColumn : DisposeBase
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
        //private DAO.Field _Field;
        ///// <summary>字段</summary>
        //public DAO.Field Field
        //{
        //    get { return _Field; }
        //    set { _Field = value; }
        //}
        #endregion

        #region 扩展属性
        /// <summary>名称</summary>
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
                    throw new XCodeException("列" + Column.Name + "没有Description属性！");
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
                    throw new XCodeException("列" + Column.Name + "没有Default属性！");
            }
        }

        /// <summary>是否自增</summary>
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
                    throw new XCodeException("列" + Column.Name + "没有Autoincrement属性！");
            }
        }

        /// <summary>是否允许空</summary>
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
                    throw new XCodeException("列" + Column.Name + "没有Nullable属性！");
            }
        }
        #endregion

        #region 构造
        public ADOColumn(ADOTabe table, Column column/*, DAO.Field field*/)
        {
            Table = table;
            Column = column;
            //Field = field;
        }

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            try
            {
                if (Column != null) Marshal.ReleaseComObject(Column);
            }
            catch (Exception ex)
            {
                if (DAL.Debug) DAL.WriteLog(ex.ToString());
            }
        }
        #endregion
    }
    #endregion
}