using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NewLife;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Web;
using XCode.TDengine;

namespace XCode.DataAccessLayer
{
    class TDengine : RemoteDb
    {
        #region 属性
        /// <summary>返回数据库类型。</summary>
        public override DatabaseType Type => DatabaseType.TDengine;

        ///// <summary>工厂</summary>
        //public override DbProviderFactory Factory => TDengineFactory.Instance;

        private static DbProviderFactory _Factory;
        /// <summary>工厂</summary>
        public override DbProviderFactory Factory
        {
            get
            {
                if (_Factory == null)
                {
                    lock (typeof(TDengine))
                    {
                        //GetProviderFactory("NewLife.TDengine.dll", "NewLife.TDengine.TDengineFactory");
                        var links = GetLinkNames("NewLife.TDengine.dll", true);
                        var type = PluginHelper.LoadPlugin("NewLife.TDengine.TDengineFactory", null, "taos.dll", links.Join(","));

                        _Factory = TDengineFactory.Instance;
                    }
                }

                return _Factory;
            }
        }

        const String Server_Key = "Server";
        protected override void OnSetConnectionString(ConnectionStringBuilder builder)
        {
            base.OnSetConnectionString(builder);

            var key = builder[Server_Key];
            if (key.EqualIgnoreCase(".", "localhost")) builder[Server_Key] = IPAddress.Loopback.ToString();
        }
        #endregion

        #region 方法
        /// <summary>创建数据库会话</summary>
        /// <returns></returns>
        protected override IDbSession OnCreateSession() => new TDengineSession(this);

        /// <summary>创建元数据对象</summary>
        /// <returns></returns>
        protected override IMetaData OnCreateMetaData() => new TDengineMetaData();

        public override Boolean Support(String providerName)
        {
            providerName = providerName.ToLower();
            if (providerName.EqualIgnoreCase("TD", "TDengine")) return true;

            return false;
        }
        #endregion

        #region 数据库特性
        protected override String ReservedWordsStr
        {
            get
            {
                return "ACCESSIBLE,ADD,ALL,ALTER,ANALYZE,AND,AS,ASC,ASENSITIVE,BEFORE,BETWEEN,BIGINT,BINARY,BLOB,BOTH,BY,CALL,CASCADE,CASE,CHANGE,CHAR,CHARACTER,CHECK,COLLATE,COLUMN,CONDITION,CONNECTION,CONSTRAINT,CONTINUE,CONTRIBUTORS,CONVERT,CREATE,CROSS,CURRENT_DATE,CURRENT_TIME,CURRENT_TIMESTAMP,CURRENT_USER,CURSOR,DATABASE,DATABASES,DAY_HOUR,DAY_MICROSECOND,DAY_MINUTE,DAY_SECOND,DEC,DECIMAL,DECLARE,DEFAULT,DELAYED,DELETE,DESC,DESCRIBE,DETERMINISTIC,DISTINCT,DISTINCTROW,DIV,DOUBLE,DROP,DUAL,EACH,ELSE,ELSEIF,ENCLOSED,ESCAPED,EXISTS,EXIT,EXPLAIN,FALSE,FETCH,FLOAT,FLOAT4,FLOAT8,FOR,FORCE,FOREIGN,FROM,FULLTEXT,GRANT,GROUP,HAVING,HIGH_PRIORITY,HOUR_MICROSECOND,HOUR_MINUTE,HOUR_SECOND,IF,IGNORE,IN,INDEX,INFILE,INNER,INOUT,INSENSITIVE,INSERT,INT,INT1,INT2,INT3,INT4,INT8,INTEGER,INTERVAL,INTO,IS,ITERATE,JOIN,KEY,KEYS,KILL,LEADING,LEAVE,LEFT,LIKE,LIMIT,LINEAR,LINES,LOAD,LOCALTIME,LOCALTIMESTAMP,LOCK,LONG,LONGBLOB,LONGTEXT,LOOP,LOW_PRIORITY,MATCH,MEDIUMBLOB,MEDIUMINT,MEDIUMTEXT,MIDDLEINT,MINUTE_MICROSECOND,MINUTE_SECOND,MOD,MODIFIES,NATURAL,NOT,NO_WRITE_TO_BINLOG,NULL,NUMERIC,ON,OPTIMIZE,OPTION,OPTIONALLY,OR,ORDER,OUT,OUTER,OUTFILE,PRECISION,PRIMARY,PROCEDURE,PURGE,RANGE,READ,READS,READ_ONLY,READ_WRITE,REAL,REFERENCES,REGEXP,RELEASE,RENAME,REPEAT,REPLACE,REQUIRE,RESTRICT,RETURN,REVOKE,RIGHT,RLIKE,SCHEMA,SCHEMAS,SECOND_MICROSECOND,SELECT,SENSITIVE,SEPARATOR,SET,SHOW,SMALLINT,SPATIAL,SPECIFIC,SQL,SQLEXCEPTION,SQLSTATE,SQLWARNING,SQL_BIG_RESULT,SQL_CALC_FOUND_ROWS,SQL_SMALL_RESULT,SSL,STARTING,STRAIGHT_JOIN,TABLE,TERMINATED,THEN,TINYBLOB,TINYINT,TINYTEXT,TO,TRAILING,TRIGGER,TRUE,UNDO,UNION,UNIQUE,UNLOCK,UNSIGNED,UPDATE,UPGRADE,USAGE,USE,USING,UTC_DATE,UTC_TIME,UTC_TIMESTAMP,VALUES,VARBINARY,VARCHAR,VARCHARACTER,VARYING,WHEN,WHERE,WHILE,WITH,WRITE,X509,XOR,YEAR_MONTH,ZEROFILL," +
                    "TBName," +
                    "LOG,User,Role,Admin,Rank,Member";
            }
        }

        /// <summary>格式化关键字</summary>
        /// <param name="keyWord">关键字</param>
        /// <returns></returns>
        public override String FormatKeyWord(String keyWord)
        {
            //if (String.IsNullOrEmpty(keyWord)) throw new ArgumentNullException("keyWord");
            if (keyWord.IsNullOrEmpty()) return keyWord;

            if (keyWord.StartsWith("`") && keyWord.EndsWith("`")) return keyWord;

            return $"`{keyWord}`";
        }

        /// <summary>格式化数据为SQL数据</summary>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public override String FormatValue(IDataColumn field, Object value)
        {
            var code = System.Type.GetTypeCode(field.DataType);
            if (code == TypeCode.String)
            {
                if (value == null)
                    return field.Nullable ? "null" : "''";

                return "'" + value.ToString()
                    .Replace("\\", "\\\\")//反斜杠需要这样才能插入到数据库
                    .Replace("'", @"\'") + "'";
            }
            else if (code == TypeCode.Boolean)
            {
                return value.ToBoolean() ? "1" : "0";
            }

            return base.FormatValue(field, value);
        }

        /// <summary>格式化时间为SQL字符串</summary>
        /// <remarks>
        /// 优化DateTime转为全字符串，平均耗时从25.76ns降为15.07。
        /// 调用非常频繁，每分钟都有数百万次调用。
        /// </remarks>
        /// <param name="dateTime">时间值</param>
        /// <returns></returns>
        public override String FormatDateTime(DateTime dateTime) => $"'{dateTime:yyyy-MM-dd HH:mm:ss.fff}'";

        /// <summary>长文本长度</summary>
        public override Int32 LongTextLength => 4000;

        internal protected override String ParamPrefix => "?";

        /// <summary>创建参数</summary>
        /// <param name="name">名称</param>
        /// <param name="value">值</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public override IDataParameter CreateParameter(String name, Object value, Type type = null)
        {
            var dp = base.CreateParameter(name, value, type);

            //var type = field?.DataType;
            if (type == null) type = value?.GetType();

            // TDengine的枚举要用 DbType.String
            if (type == typeof(Boolean))
            {
                var v = value.ToBoolean();
                dp.DbType = DbType.Int16;
                dp.Value = v ? 1 : 0;
            }

            return dp;
        }

        /// <summary>系统数据库名</summary>
        public override String SystemDatabaseName => "db";

        /// <summary>字符串相加</summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public override String StringConcat(String left, String right) => $"concat({(!String.IsNullOrEmpty(left) ? left : "\'\'")},{(!String.IsNullOrEmpty(right) ? right : "\'\'")})";
        #endregion
    }

    /// <summary>TDengine数据库</summary>
    internal class TDengineSession : RemoteDbSession
    {
        #region 构造函数
        public TDengineSession(IDatabase db) : base(db) { }
        #endregion

        #region 基本方法 查询/执行
        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>新增行的自动编号</returns>
        public override Int64 InsertAndGetIdentity(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps) => throw new NotSupportedException();

        public override Task<Int64> InsertAndGetIdentityAsync(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps) => throw new NotSupportedException();
        #endregion

        #region 批量操作
        /*
        insert into stat (siteid,statdate,`count`,cost,createtime,updatetime) values 
        (1,'2018-08-11 09:34:00',1,123,now(),now()),
        (2,'2018-08-11 09:34:00',1,456,now(),now()),
        (3,'2018-08-11 09:34:00',1,789,now(),now()),
        (2,'2018-08-11 09:34:00',1,456,now(),now())
        on duplicate key update 
        `count`=`count`+values(`count`),cost=cost+values(cost),
        updatetime=values(updatetime);
         */

        private String GetBatchSql(String action, IDataTable table, IDataColumn[] columns, ICollection<String> updateColumns, ICollection<String> addColumns, IEnumerable<IExtend> list)
        {
            var sb = Pool.StringBuilder.Get();
            var db = Database as DbBase;

            // 字段列表
            if (columns == null) columns = table.Columns.ToArray();
            BuildInsert(sb, db, action, table, columns);

            // 值列表
            sb.Append(" Values");
            BuildBatchValues(sb, db, action, table, columns, list);

            // 重复键执行update
            BuildDuplicateKey(sb, db, columns, updateColumns, addColumns);

            return sb.Put(true);
        }

        public override Int32 Insert(IDataTable table, IDataColumn[] columns, IEnumerable<IExtend> list)
        {
            var sql = GetBatchSql("Insert Into", table, columns, null, null, list);
            return Execute(sql);
        }

        public override Int32 Upsert(IDataTable table, IDataColumn[] columns, ICollection<String> updateColumns, ICollection<String> addColumns, IEnumerable<IExtend> list)
        {
            var sql = GetBatchSql("Insert Into", table, columns, updateColumns, addColumns, list);
            return Execute(sql);
        }
        #endregion

        #region 架构
        public override DataTable GetSchema(DbConnection conn, String collectionName, String[] restrictionValues) => null;
        #endregion
    }

    /// <summary>TDengine元数据</summary>
    class TDengineMetaData : RemoteDbMetaData
    {
        public TDengineMetaData() => Types = _DataTypes;

        #region 数据类型
        protected override List<KeyValuePair<Type, Type>> FieldTypeMaps
        {
            get
            {
                if (_FieldTypeMaps == null)
                {
                    var list = base.FieldTypeMaps;
                    if (!list.Any(e => e.Key == typeof(Byte) && e.Value == typeof(Boolean)))
                        list.Add(new KeyValuePair<Type, Type>(typeof(Byte), typeof(Boolean)));
                }
                return base.FieldTypeMaps;
            }
        }

        /// <summary>数据类型映射</summary>
        private static readonly Dictionary<Type, String[]> _DataTypes = new()
        {
            { typeof(Byte), new String[] { "TINYINT" } },
            { typeof(Int16), new String[] { "SMALLINT" } },
            { typeof(Int32), new String[] { "INT" } },
            { typeof(Int64), new String[] { "BIGINT" } },
            { typeof(Single), new String[] { "FLOAT" } },
            { typeof(Double), new String[] { "DOUBLE" } },
            { typeof(Decimal), new String[] { "DOUBLE" } },
            { typeof(DateTime), new String[] { "TIMESTAMP" } },
            { typeof(String), new String[] { "NCHAR({0})", "BINARY({0})" } },
            { typeof(Boolean), new String[] { "BOOL" } },
            //{ typeof(Byte[]), new String[] { "BINARY" } },
        };
        #endregion

        #region 架构
        protected override List<IDataTable> OnGetTables(String[] names)
        {
            var ss = Database.CreateSession();
            var list = new List<IDataTable>();

            var old = ss.ShowSQL;
            ss.ShowSQL = false;
            try
            {
                var sql = "SHOW TABLES";
                var dt = ss.Query(sql, null);
                if (dt.Rows.Count == 0) return null;

                var hs = new HashSet<String>(names ?? new String[0], StringComparer.OrdinalIgnoreCase);

                // 所有表
                foreach (var dr in dt)
                {
                    var name = dr["table_name"] + "";
                    if (name.IsNullOrEmpty() || hs.Count > 0 && !hs.Contains(name)) continue;

                    var table = DAL.CreateTable();
                    table.TableName = name;
                    table.Owner = dr["stable_name"] as String;

                    #region 字段
                    sql = $"DESCRIBE {name}";
                    var dcs = ss.Query(sql, null);
                    foreach (var dc in dcs)
                    {
                        var field = table.CreateColumn();

                        field.ColumnName = dc["Field"] + "";
                        field.RawType = dc["Type"] + "";
                        field.DataType = GetDataType(field.RawType);
                        field.Length = dc["Length"].ToInt();
                        field.Master = dc["Note"] as String == "TAGS";

                        field.Fix();

                        table.Columns.Add(field);
                    }
                    #endregion

                    // 修正关系数据
                    table.Fix();

                    list.Add(table);
                }
            }
            finally
            {
                ss.ShowSQL = old;
            }

            return list;
        }

        /// <summary>
        /// 快速取得所有表名
        /// </summary>
        /// <returns></returns>
        public override IList<String> GetTableNames()
        {
            var list = new List<String>();

            var dt = base.Database.CreateSession().Query("SHOW TABLES", null);
            if (dt.Rows.Count == 0) return list;

            // 所有表
            foreach (var dr in dt)
            {
                var name = dr["table_name"] + "";
                if (!name.IsNullOrEmpty()) list.Add(name);
            }

            return list;
        }

        public override String FieldClause(IDataColumn field, Boolean onlyDefine)
        {
            var sb = new StringBuilder();

            // 字段名
            sb.AppendFormat("{0} ", FormatName(field));

            String typeName = null;
            // 每种数据库的自增差异太大，理应由各自处理，而不采用原始值
            if (Database.Type == field.Table.DbType && !field.Identity) typeName = field.RawType;

            if (String.IsNullOrEmpty(typeName)) typeName = GetFieldType(field);

            sb.Append(typeName);

            return sb.ToString();
        }
        #endregion

        #region 反向工程
        protected override Boolean DatabaseExist(String databaseName)
        {
            var ss = Database.CreateSession();
            var sql = $"SHOW DATABASES";
            var dt = ss.Query(sql, null);
            return dt != null && dt.Rows != null && dt.Rows.Any(e => e[0] as String == databaseName);
        }

        public override String CreateDatabaseSQL(String dbname, String file) => $"Create Database If Not Exists {Database.FormatName(dbname)}";
        //public override String CreateDatabaseSQL(String dbname, String file) => $"Create Database If Not Exists {Database.FormatName(dbname)} KEEP 365 DAYS 10 BLOCKS 6 UPDATE 1;";

        public override String DropDatabaseSQL(String dbname) => $"Drop Database If Exists {Database.FormatName(dbname)}";

        public override String CreateTableSQL(IDataTable table)
        {
            var fs = new List<IDataColumn>(table.Columns);

            var sb = Pool.StringBuilder.Get();

            sb.AppendFormat("Create Table If Not Exists {0}(", FormatName(table));
            var ss = fs.Where(e => !e.Master).ToList();
            var ms = fs.Where(e => e.Master).ToList();
            sb.Append(ss.Join(",", e => FieldClause(e, true)));
            sb.Append(')');

            if (ms.Count > 0)
            {
                sb.Append(" TAGS (");
                sb.Append(ms.Join(",", e => FieldClause(e, true)));
                sb.Append(')');
            }
            sb.Append(';');

            return sb.Put(true);
        }

        public override String AddTableDescriptionSQL(IDataTable table) =>
            // 返回String.Empty表示已经在别的SQL中处理
            String.Empty;

        public override String AlterColumnSQL(IDataColumn field, IDataColumn oldfield) => $"Alter Table {FormatName(field.Table)} Modify Column {FieldClause(field, false)}";

        public override String AddColumnDescriptionSQL(IDataColumn field) =>
            // 返回String.Empty表示已经在别的SQL中处理
            String.Empty;
        #endregion
    }
}