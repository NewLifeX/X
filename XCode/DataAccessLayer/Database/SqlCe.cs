using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.IO;
using System.Reflection;
using System.Text;
using NewLife;
using NewLife.Collections;
using NewLife.IO;
using NewLife.Log;
using NewLife.Reflection;

namespace XCode.DataAccessLayer
{
    /// <summary>SqlCe数据库。由 @Goon(12600112) 测试并完善正向反向工程</summary>
    class SqlCe : FileDbBase
    {
        #region 属性
        /// <summary>返回数据库类型。外部DAL数据库类请使用Other</summary>
        public override DatabaseType Type => DatabaseType.SqlCe;

        private static DbProviderFactory _Factory;
        /// <summary>工厂</summary>
        public override DbProviderFactory Factory
        {
            get
            {
                if (_Factory == null)
                {
                    lock (typeof(SqlCe))
                    {
                        if (_Factory == null) _Factory = GetProviderFactory("System.Data.SqlServerCe.dll", "System.Data.SqlServerCe.SqlCeProviderFactory");

                        if (_Factory != null)
                        {
                            using (var conn = _Factory.CreateConnection())
                            {
                                if (conn.ServerVersion.StartsWith("4"))
                                    SqlCeProviderVersion = SQLCEVersion.SQLCE40;
                                else
                                    SqlCeProviderVersion = SQLCEVersion.SQLCE35;
                            }
                        }
                    }
                }

                return _Factory;
            }
        }

        /// <summary>SqlCe提供者版本</summary>
        public static SQLCEVersion SqlCeProviderVersion { get; set; } = SQLCEVersion.SQLCE40;

        /// <summary>SqlCe版本,默认4.0</summary>
        public SQLCEVersion SqlCeVer { get; set; } = SQLCEVersion.SQLCE40;

        protected override void OnSetConnectionString(ConnectionStringBuilder builder)
        {
            base.OnSetConnectionString(builder);

            SqlCeVer = SQLCEVersion.SQLCE40;

            var fn = DatabaseName;
            if (!fn.IsNullOrEmpty() && File.Exists(fn))
            {
                try
                {
                    SqlCeVer = SqlCeHelper.DetermineVersion(fn);
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);

                    SqlCeVer = SQLCEVersion.SQLCE40;
                }
            }
        }

        protected override String DefaultConnectionString
        {
            get
            {
                var builder = Factory.CreateConnectionStringBuilder();
                if (builder != null)
                {
                    var name = Path.GetTempFileName();
                    FileSource.ReleaseFile(Assembly.GetExecutingAssembly(), "SqlCe.sdf", name, true);

                    builder[_.DataSource] = name;
                    return builder.ToString();
                }

                return base.DefaultConnectionString;
            }
        }
        #endregion

        #region 方法
        /// <summary>创建数据库会话</summary>
        /// <returns></returns>
        protected override IDbSession OnCreateSession() => new SqlCeSession(this);

        /// <summary>创建元数据对象</summary>
        /// <returns></returns>
        protected override IMetaData OnCreateMetaData() => new SqlCeMetaData();
        #endregion

        #region 数据库特性
        protected override String ReservedWordsStr
        {
            get { return "ADD,ALL,ALTER,AND,ANY,APPLY,AS,ASC,AUTHORIZATION,BACKUP,BEGIN,BETWEEN,BREAK,BROWSE,BULK,BY,CASCADE,CASE,CAST,CHECK,CHECKPOINT,CLOSE,CLUSTERED,COALESCE,COLLATE,COLUMN,COMMIT,COMPUTE,CONSTRAINT,CONTAINS,CONTAINSTABLE,CONTINUE,CONVERT,CREATE,CROSS,CURRENT,CURRENT_DATE,CURRENT_TIME,CURRENT_TIMESTAMP,CURRENT_USER,CURSOR,DATABASE,DBCC,DEALLOCATE,DECLARE,DEFAULT,DELETE,DENY,DESC,DISK,DISTINCT,DISTRIBUTED,DOUBLE,DROP,DUMP,ELSE,END,ERRLVL,ESCAPE,EXCEPT,EXEC,EXECUTE,EXISTS,EXIT,EXTERNAL,FETCH,FILE,FILLFACTOR,FIRST,FOR,FOREIGN,FREETEXT,FREETEXTTABLE,FROM,FULL,FUNCTION,GOTO,GRANT,GROUP,HAVING,HOLDLOCK,IDENTITY,IDENTITY_INSERT,IDENTITYCOL,IF,IN,INDEX,INNER,INSERT,INTERSECT,INTO,IS,JOIN,KEY,KILL,LEFT,LIKE,LINENO,LOAD,NATIONAL,NEXT,NOCHECK,NONCLUSTERED,NOT,NULL,NULLIF,OF,OFF,OFFSET,OFFSETS,ON,ONLY,OPEN,OPENDATASOURCE,OPENQUERY,OPENROWSET,OPENXML,OPTION,OR,ORDER,OUTER,OVER,PERCENT,PIVOT,PLAN,PRIMARY,PRINT,PROC,PROCEDURE,PUBLIC,RAISERROR,READ,READTEXT,RECONFIGURE,REFERENCES,REPLICATION,RESTORE,RESTRICT,RETURN,REVERT,REVOKE,RIGHT,ROLLBACK,ROW,ROWCOUNT,ROWGUIDCOL,ROWS,RULE,SAVE,SCHEMA,SELECT,SESSION_USER,SET,SETUSER,SHUTDOWN,SOME,STATISTICS,SYSTEM_USER,TABLE,TEXTSIZE,THEN,TO,TOP,TRAN,TRANSACTION,TRIGGER,TRUNCATE,TSEQUAL,UNION,UNIQUE,UNPIVOT,UPDATE,UPDATETEXT,USE,USER,VALUES,VARYING,VIEW,WAITFOR,WHEN,WHERE,WHILE,WITH,WRITETEXT,XMLUNNEST"; }
        }

        ///// <summary>当前时间函数</summary>
        //public override String DateTimeNow { get { return "getdate()"; } }

        ///// <summary>最小时间</summary>
        //public override DateTime DateTimeMin { get { return SqlDateTime.MinValue.Value; } }

        ///// <summary>格式化时间为SQL字符串</summary>
        ///// <param name="dateTime">时间值</param>
        ///// <returns></returns>
        //public override String FormatDateTime(DateTime dateTime)
        //{
        //    return String.Format("'{0:yyyy-MM-dd HH:mm:ss}'", dateTime);
        //}

        /// <summary>格式化关键字</summary>
        /// <param name="keyWord">关键字</param>
        /// <returns></returns>
        public override String FormatKeyWord(String keyWord)
        {
            //if (String.IsNullOrEmpty(keyWord)) throw new ArgumentNullException("keyWord");
            if (String.IsNullOrEmpty(keyWord)) return keyWord;

            if (keyWord.StartsWith("[") && keyWord.EndsWith("]")) return keyWord;

            return String.Format("[{0}]", keyWord);
        }
        #endregion

        #region 分页
        public override SelectBuilder PageSplit(SelectBuilder builder, Int64 startRowIndex, Int64 maximumRows)
        {
            return MSPageSplit.PageSplit(builder, startRowIndex, maximumRows, false, b => CreateSession().QueryCount(b));
        }
        #endregion
    }

    /// <summary>SqlCe会话</summary>
    class SqlCeSession : FileDbSession
    {
        #region 构造函数
        public SqlCeSession(IDatabase db) : base(db) { }
        #endregion

        protected override void CreateDatabase()
        {
            if (String.IsNullOrEmpty(FileName) || File.Exists(FileName)) return;

            //FileSource.ReleaseFile(Assembly.GetExecutingAssembly(), "SqlCe.sdf", FileName, true);
            DAL.WriteLog("创建数据库：{0}", FileName);

            var sce = SqlCeEngine.Create(ConnectionString);
            if (sce != null) sce.CreateDatabase().Dispose();
        }

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>新增行的自动编号</returns>
        public override Int64 InsertAndGetIdentity(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            BeginTransaction(IsolationLevel.Serializable);
            try
            {
                Int64 rs = Execute(sql, type, ps);
                if (rs > 0) rs = ExecuteScalar<Int64>("Select @@Identity");
                Commit();
                return rs;
            }
            catch { Rollback(true); throw; }
            //finally
            //{
            //    AutoClose();
            //}
        }

        /// <summary>返回数据源的架构信息</summary>
        /// <param name="conn">连接</param>
        /// <param name="collectionName">指定要返回的架构的名称。</param>
        /// <param name="restrictionValues">为请求的架构指定一组限制值。</param>
        /// <returns></returns>
        public override DataTable GetSchema(DbConnection conn, String collectionName, String[] restrictionValues)
        {
            //sqlce3.5 不支持GetSchema
            if (SqlCe.SqlCeProviderVersion < SQLCEVersion.SQLCE40 && collectionName.EqualIgnoreCase(DbMetaDataCollectionNames.MetaDataCollections))
                return null;

            return base.GetSchema(conn, collectionName, restrictionValues);
        }
    }

    /// <summary>SqlCe元数据</summary>
    class SqlCeMetaData : FileDbMetaData
    {
        #region 构架
        protected override List<IDataTable> OnGetTables(String[] names)
        {
            #region 查表、字段信息、索引信息、主键信息
            var session = Database.CreateSession();

            //表信息
            DataTable dt = null;
            dt = session.Query(_AllTableNameSql).Tables[0];

            var data = new NullableDictionary<String, DataTable>(StringComparer.OrdinalIgnoreCase)
            {
                ["Columns"] = session.Query(_AllColumnSql).Tables[0],
                ["Indexes"] = session.Query(_AllIndexSql).Tables[0]
            };

            ////数据类型DBType --〉DotNetType转换
            //if (SqlCe.SqlCeProviderVersion < SQLCEVersion.SQLCE40)
            //    DataTypes = CreateSqlCeDataType(session.Query(_DataTypeSql).Tables[0]);
            #endregion

            if (dt == null || dt.Rows == null || dt.Rows.Count < 1) return null;

            // 默认列出所有字段
            var rows = dt.Select("TABLE_TYPE='table'");
            if (rows == null || rows.Length < 1) return null;

            return GetTables(rows, names, data);
        }

        /// <summary>获取索引</summary>
        /// <param name="table"></param>
        /// <param name="indexes">索引</param>
        /// <param name="indexColumns">索引列</param>
        /// <returns></returns>
        protected override List<IDataIndex> GetIndexes(IDataTable table, DataTable indexes, DataTable indexColumns)
        {
            var list = base.GetIndexes(table, indexes, indexColumns);
            if (list != null && list.Count > 0)
            {
                // SqlCe的索引直接以索引字段的方式排布，所以需要重新组合起来
                var dic = new Dictionary<String, IDataIndex>();
                foreach (var item in list)
                {
                    if (!dic.TryGetValue(item.Name, out var di))
                    {
                        dic.Add(item.Name, item);
                    }
                    else
                    {
                        var ss = new List<String>(di.Columns);
                        if (item.Columns != null && item.Columns.Length > 0 && !ss.Contains(item.Columns[0]))
                        {
                            ss.Add(item.Columns[0]);
                            di.Columns = ss.ToArray();
                        }
                    }
                }
                list.Clear();
                foreach (var item in dic.Values)
                {
                    list.Add(item);
                }
            }
            return list;
        }

        public override String CreateTableSQL(IDataTable table)
        {
            var sql = base.CreateTableSQL(table);

            var pks = table.PrimaryKeys;
            if (String.IsNullOrEmpty(sql) || pks == null || pks.Length < 2) return sql;

            // 处理多主键
            var sb = new StringBuilder(sql.Length + 32 + pks.Length * 16);
            sb.Append(sql);
            sb.Append(";\r\n");
            sb.AppendFormat("Alter Table {0} Add Constraint PK_{1} Primary Key (", FormatName(table.TableName), table.TableName);

            //foreach (var item in pks)
            //{
            //    sb.Append(FormatName(item.ColumnName));
            //    sb.Append(",");
            //}
            //sb.Remove(sb.Length - 1, 1);

            // sb.Remove涉及内存复制
            for (var i = 0; i < pks.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(FormatName(pks[i].ColumnName));
            }

            sb.Append(")");

            //sql += ";" + Environment.NewLine;
            //sql += String.Format("Alter Table {0} Add Constraint PK_{1} Primary Key ({2})", FormatName(table.TableName), table.TableName, sb);
            return sb.ToString();
        }

        protected override String GetFieldConstraints(IDataColumn field, Boolean onlyDefine)
        {
            // 非定义时（修改字段），主键字段没有约束
            if (!onlyDefine && field.PrimaryKey) return null;

            var str = base.GetFieldConstraints(field, onlyDefine);

            // 非定义时，自增字段没有约束
            if (onlyDefine && field.Identity) str = " IDENTITY(1,1)" + str;

            return str;
        }

        #endregion

        #region 取得字段信息的SQL模版

        private DataTable CreateSqlCeDataType(DataTable src)
        {
            var drs = src.Select();
            foreach (var dr in drs)
            {
                dr["datatype"] = DBTypeToDotNetDataType(dr["typename"].ToString());
            }
            src.AcceptChanges();
            return src;
        }

        private String DBTypeToDotNetDataType(String DBType)
        {
            switch (DBType)
            {
                case "smallint": return "System.Int16";
                case "int": return "System.Int32";
                case "bigint": return "System.Int64";
                case "nvarchar":
                case "char":
                case "nchar":
                case "ntext":
                case "text":
                case "varchar": return "System.String";
                case "bit": return "System.Boolean";
                case "smalldatetime":
                case "datetime": return "System.DateTime";
                case "float": return "System.Double";
                case "decimal":
                case "money":
                case "smallmoney":
                case "numeric": return "System.Decimal";
                case "real": return "System.Single";
                case "uniqueidentifier": return "System.Guid";
                case "tinyint": return "System.Byte";
                case "image":
                case "timestamp":
                case "binary":
                case "varbinary": return "System.Byte[]";
                case "variant": return "System.Object";
                default:
                    return "";
            }
        }

        private readonly String _AllTableNameSql = "SELECT table_name,TABLE_TYPE FROM information_schema.tables WHERE TABLE_TYPE <> N'SYSTEM TABLE' ";

        private readonly String _AllColumnSql =
                "SELECT     Column_name, is_nullable, data_type, character_maximum_length, numeric_precision, autoinc_increment as AUTOINCREMENT, autoinc_seed, column_hasdefault, column_default, column_flags, numeric_scale, table_name, autoinc_next  " +
                "FROM         information_schema.columns " +
                "WHERE      SUBSTRING(COLUMN_NAME, 1,5) <> '__sys'  " +
                "ORDER BY ordinal_position ASC ";

        private readonly String _AllIndexSql =
                "SELECT     TABLE_NAME, INDEX_NAME, PRIMARY_KEY, [UNIQUE], [CLUSTERED], ORDINAL_POSITION, COLUMN_NAME, COLLATION AS SORT_ORDER " +
                "FROM         Information_Schema.Indexes " +
                "WHERE      SUBSTRING(COLUMN_NAME, 1,5) <> '__sys'   " +
                "ORDER BY TABLE_NAME, INDEX_NAME, ORDINAL_POSITION";


        //private readonly String _DataTypeSql =
        //        "SELECT     TYPE_NAME as typename, DATA_TYPE as ProviderDbType,TYPE_NAME as datatype, COLUMN_SIZE as ColumnSize, LITERAL_PREFIX as LiteralPrefix, " +
        //        "           LITERAL_SUFFIX as LiteralSuffix, CREATE_PARAMS as CreateParameters, IS_NULLABLE as IsNullable, CASE_SENSITIVE as IsCaseSensitive, " +
        //        "           SEARCHABLE as IsSearchable, UNSIGNED_ATTRIBUTE as IsUnsigned, FIXED_PREC_SCALE, AUTO_UNIQUE_VALUE, LOCAL_TYPE_NAME,  " +
        //        "           MINIMUM_SCALE as MinimumScale, MAXIMUM_SCALE as MaximumScale, GUID , TYPELIB , VERSION , IS_LONG as IsLong, BEST_MATCH as IsBestMatch, IS_FIXEDLENGTH as IsFixedLength  " +
        //        " FROM      INFORMATION_SCHEMA.PROVIDER_TYPES ";
        #endregion

        /// <summary>数据类型映射</summary>
        private static readonly Dictionary<Type, String[]> _DataTypes = new Dictionary<Type, String[]>
        {
            { typeof(Byte[]), new String[] { "varbinary({0})", "timestamp", "binary({0})", "image" } },
            { typeof(Guid), new String[] { "uniqueidentifier" } },
            { typeof(Boolean), new String[] { "bit" } },
            { typeof(Byte), new String[] { "tinyint" } },
            { typeof(Int16), new String[] { "smallint" } },
            { typeof(Int32), new String[] { "int" } },
            { typeof(Int64), new String[] { "bigint" } },
            { typeof(Single), new String[] { "real" } },
            { typeof(Double), new String[] { "float" } },
            { typeof(Decimal), new String[] { "money", "numeric({0}, {1})" } },
            { typeof(DateTime), new String[] { "datetime" } },
            { typeof(String), new String[] { "nvarchar({0})", "ntext", "nchar({0})" } }
        };
    }

    /// <summary>SqlCe版本</summary>
    public enum SQLCEVersion
    {
        /// <summary>Sqlce Ver2.0</summary>
        SQLCE20 = 0,

        /// <summary>Sqlce Ver3.0</summary>
        SQLCE30 = 1,

        /// <summary>Sqlce Ver3.5</summary>
        SQLCE35 = 2,

        /// <summary>Sqlce Ver4.0</summary>
        SQLCE40 = 3
    }

    /// <summary>SqlCe辅助类</summary>
    public static class SqlCeHelper
    {
        static Dictionary<Int32, SQLCEVersion> versionDictionary = new Dictionary<Int32, SQLCEVersion>
        {
            { 0x73616261, SQLCEVersion.SQLCE20 },
            { 0x002dd714, SQLCEVersion.SQLCE30 },
            { 0x00357b9d, SQLCEVersion.SQLCE35 },
            { 0x003d0900, SQLCEVersion.SQLCE40 }
        };

        /// <summary>检查给定SqlCe文件的版本</summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static SQLCEVersion DetermineVersion(String fileName)
        {
            var versionLONGWORD = 0;

            using (var fs = new FileStream(fileName, FileMode.Open))
            {
                fs.Seek(16, SeekOrigin.Begin);
                using (var reader = new BinaryReader(fs))
                {
                    versionLONGWORD = reader.ReadInt32();
                }
            }

            if (versionDictionary.ContainsKey(versionLONGWORD))
                return versionDictionary[versionLONGWORD];
            else
                throw new ApplicationException("不能确定该sdf的版本！");
        }

        /// <summary>检测SqlServerCe3.5是否安装</summary>
        /// <returns></returns>
        public static Boolean IsV35Installed()
        {
            try
            {
                Assembly.Load("System.Data.SqlServerCe, Version=3.5.1.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91");
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            try
            {
                var factory = DbProviderFactories.GetFactory("System.Data.SqlServerCe.3.5");
            }
            catch (ConfigurationException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            return true;
        }

        /// <summary>检测SqlServerCe4是否安装</summary>
        /// <returns></returns>
        public static Boolean IsV40Installed()
        {
            try
            {
                Assembly.Load("System.Data.SqlServerCe, Version=4.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91");
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            try
            {
                var factory = DbProviderFactories.GetFactory("System.Data.SqlServerCe.4.0");
            }
            catch (ConfigurationException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            return true;
        }
    }

    class SqlCeEngine : IDisposable
    {
        private static Type _EngineType = "System.Data.SqlServerCe.SqlCeEngine".GetTypeEx(true);
        /// <summary></summary>
        public static Type EngineType { get { return _EngineType; } set { _EngineType = value; } }

        private Object _Engine;
        /// <summary>引擎</summary>
        public Object Engine { get { return _Engine; } set { _Engine = value; } }

        public static SqlCeEngine Create(String connstr)
        {
            if (EngineType == null) return null;
            if (String.IsNullOrEmpty(connstr)) return null;

            try
            {
                var e = EngineType.CreateInstance(connstr);
                if (e == null) return null;

                var sce = new SqlCeEngine()
                {
                    Engine = e
                };
                return sce;
            }
            catch { return null; }
        }

        public void Dispose() => Engine.TryDispose();

        public SqlCeEngine CreateDatabase() { Engine.Invoke("CreateDatabase"); return this; }

        public SqlCeEngine Shrink() { Engine.Invoke("Shrink"); return this; }
    }
}