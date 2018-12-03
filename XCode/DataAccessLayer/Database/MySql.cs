using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Net;
using NewLife.Collections;
using NewLife.Reflection;

namespace XCode.DataAccessLayer
{
    class MySql : RemoteDb
    {
        #region 属性
        /// <summary>返回数据库类型。</summary>
        public override DatabaseType Type => DatabaseType.MySql;

        private static DbProviderFactory _Factory;
        /// <summary>工厂</summary>
        public override DbProviderFactory Factory
        {
            get
            {
                if (_Factory == null)
                {
                    lock (typeof(MySql))
                    {
                        //_Factory = GetProviderFactory("NewLife.MySql.dll", "NewLife.MySql.MySqlClientFactory") ??
                        //           GetProviderFactory("MySql.Data.dll", "MySql.Data.MySqlClient.MySqlClientFactory");
                        // MewLife.MySql 在开发过程中，数据驱动下载站点没有它的包，暂时不支持下载
                        _Factory = GetProviderFactory(null, "NewLife.MySql.MySqlClientFactory", true) ??
                                  GetProviderFactory("MySql.Data.dll", "MySql.Data.MySqlClient.MySqlClientFactory");
                    }
                }

                return _Factory;
            }
        }

        const String Server_Key = "Server";
        const String CharSet = "CharSet";
        const String AllowZeroDatetime = "Allow Zero Datetime";
        const String MaxPoolSize = "MaxPoolSize";
        const String Sslmode = "Sslmode";
        protected override void OnSetConnectionString(ConnectionStringBuilder builder)
        {
            base.OnSetConnectionString(builder);

            var key = builder[Server_Key];
            if (key.EqualIgnoreCase(".", "localhost"))
            {
                //builder[Server_Key] = "127.0.0.1";
                builder[Server_Key] = IPAddress.Loopback.ToString();
            }

            // 默认设置为utf8mb4，支持表情符
            builder.TryAdd(CharSet, "utf8mb4");

            //if (!builder.ContainsKey(AllowZeroDatetime)) builder[AllowZeroDatetime] = "True";
            // 默认最大连接数1000
            if (builder["Pooling"].ToBoolean()) builder.TryAdd(MaxPoolSize, "1000");

            // 如未设置Sslmode，默认为none
            if (builder[Sslmode] == null) builder.TryAdd(Sslmode, "none");
        }
        #endregion

        #region 方法
        /// <summary>创建数据库会话</summary>
        /// <returns></returns>
        protected override IDbSession OnCreateSession() => new MySqlSession(this);

        /// <summary>创建元数据对象</summary>
        /// <returns></returns>
        protected override IMetaData OnCreateMetaData() => new MySqlMetaData();

        public override Boolean Support(String providerName)
        {
            providerName = providerName.ToLower();
            if (providerName.Contains("mysql.data.mysqlclient")) return true;
            if (providerName.Contains("mysql")) return true;

            return false;
        }
        #endregion

        #region 分页
        /// <summary>已重写。获取分页</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <param name="keyColumn">主键列。用于not in分页</param>
        /// <returns></returns>
        public override String PageSplit(String sql, Int64 startRowIndex, Int64 maximumRows, String keyColumn)
        {
            // 从第一行开始，不需要分页
            if (startRowIndex <= 0)
            {
                if (maximumRows < 1) return sql;

                return "{0} limit {1}".F(sql, maximumRows);
            }
            if (maximumRows < 1) throw new NotSupportedException("不支持取第几条数据之后的所有数据！");

            return "{0} limit {1}, {2}".F(sql, startRowIndex, maximumRows);
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
        public override SelectBuilder PageSplit(SelectBuilder builder, Int64 startRowIndex, Int64 maximumRows)
        {
            // 从第一行开始，不需要分页
            if (startRowIndex <= 0)
            {
                if (maximumRows > 0) builder.Limit += " limit {0}".F(maximumRows);
                return builder;
            }
            if (maximumRows < 1) throw new NotSupportedException("不支持取第几条数据之后的所有数据！");

            builder.Limit += " limit {0}, {1}".F(startRowIndex, maximumRows);
            return builder;
        }
        #endregion

        #region 数据库特性
        protected override String ReservedWordsStr
        {
            get
            {
                return "ACCESSIBLE,ADD,ALL,ALTER,ANALYZE,AND,AS,ASC,ASENSITIVE,BEFORE,BETWEEN,BIGINT,BINARY,BLOB,BOTH,BY,CALL,CASCADE,CASE,CHANGE,CHAR,CHARACTER,CHECK,COLLATE,COLUMN,CONDITION,CONNECTION,CONSTRAINT,CONTINUE,CONTRIBUTORS,CONVERT,CREATE,CROSS,CURRENT_DATE,CURRENT_TIME,CURRENT_TIMESTAMP,CURRENT_USER,CURSOR,DATABASE,DATABASES,DAY_HOUR,DAY_MICROSECOND,DAY_MINUTE,DAY_SECOND,DEC,DECIMAL,DECLARE,DEFAULT,DELAYED,DELETE,DESC,DESCRIBE,DETERMINISTIC,DISTINCT,DISTINCTROW,DIV,DOUBLE,DROP,DUAL,EACH,ELSE,ELSEIF,ENCLOSED,ESCAPED,EXISTS,EXIT,EXPLAIN,FALSE,FETCH,FLOAT,FLOAT4,FLOAT8,FOR,FORCE,FOREIGN,FROM,FULLTEXT,GRANT,GROUP,HAVING,HIGH_PRIORITY,HOUR_MICROSECOND,HOUR_MINUTE,HOUR_SECOND,IF,IGNORE,IN,INDEX,INFILE,INNER,INOUT,INSENSITIVE,INSERT,INT,INT1,INT2,INT3,INT4,INT8,INTEGER,INTERVAL,INTO,IS,ITERATE,JOIN,KEY,KEYS,KILL,LEADING,LEAVE,LEFT,LIKE,LIMIT,LINEAR,LINES,LOAD,LOCALTIME,LOCALTIMESTAMP,LOCK,LONG,LONGBLOB,LONGTEXT,LOOP,LOW_PRIORITY,MATCH,MEDIUMBLOB,MEDIUMINT,MEDIUMTEXT,MIDDLEINT,MINUTE_MICROSECOND,MINUTE_SECOND,MOD,MODIFIES,NATURAL,NOT,NO_WRITE_TO_BINLOG,NULL,NUMERIC,ON,OPTIMIZE,OPTION,OPTIONALLY,OR,ORDER,OUT,OUTER,OUTFILE,PRECISION,PRIMARY,PROCEDURE,PURGE,RANGE,READ,READS,READ_ONLY,READ_WRITE,REAL,REFERENCES,REGEXP,RELEASE,RENAME,REPEAT,REPLACE,REQUIRE,RESTRICT,RETURN,REVOKE,RIGHT,RLIKE,SCHEMA,SCHEMAS,SECOND_MICROSECOND,SELECT,SENSITIVE,SEPARATOR,SET,SHOW,SMALLINT,SPATIAL,SPECIFIC,SQL,SQLEXCEPTION,SQLSTATE,SQLWARNING,SQL_BIG_RESULT,SQL_CALC_FOUND_ROWS,SQL_SMALL_RESULT,SSL,STARTING,STRAIGHT_JOIN,TABLE,TERMINATED,THEN,TINYBLOB,TINYINT,TINYTEXT,TO,TRAILING,TRIGGER,TRUE,UNDO,UNION,UNIQUE,UNLOCK,UNSIGNED,UPDATE,UPGRADE,USAGE,USE,USING,UTC_DATE,UTC_TIME,UTC_TIMESTAMP,VALUES,VARBINARY,VARCHAR,VARCHARACTER,VARYING,WHEN,WHERE,WHILE,WITH,WRITE,X509,XOR,YEAR_MONTH,ZEROFILL," +
                    "LOG,User,Role,Admin,Rank";
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
                var v = value.ToBoolean();
                if (field.Table != null && EnumTables.Contains(field.Table.TableName))
                    return v ? "'Y'" : "'N'";
                else
                    return v ? "1" : "0";
            }

            return base.FormatValue(field, value);
        }

        /// <summary>长文本长度</summary>
        public override Int32 LongTextLength => 4000;

        internal protected override String ParamPrefix => "?";

        /// <summary>创建参数</summary>
        /// <param name="name">名称</param>
        /// <param name="value">值</param>
        /// <param name="field">字段</param>
        /// <returns></returns>
        public override IDataParameter CreateParameter(String name, Object value, IDataColumn field = null)
        {
            var dp = base.CreateParameter(name, value, field);

            var type = field?.DataType;
            if (type == null) type = value?.GetType();

            // MySql的枚举要用 DbType.String
            if (type == typeof(Boolean))
            {
                var v = value.ToBoolean();
                if (field?.Table != null && EnumTables.Contains(field.Table.TableName))
                {
                    dp.DbType = DbType.String;
                    dp.Value = value.ToBoolean() ? 'Y' : 'N';
                }
                else
                {
                    dp.DbType = DbType.Int16;
                    dp.Value = v ? 1 : 0;
                }
            }

            return dp;
        }

        /// <summary>系统数据库名</summary>
        public override String SystemDatabaseName => "mysql";

        /// <summary>字符串相加</summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public override String StringConcat(String left, String right) => String.Format("concat({0},{1})", (!String.IsNullOrEmpty(left) ? left : "\'\'"), (!String.IsNullOrEmpty(right) ? right : "\'\'"));
        #endregion

        #region 跨版本兼容
        /// <summary>采用枚举来表示布尔型的数据表。由正向工程赋值</summary>
        public ICollection<String> EnumTables { get; } = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
        #endregion
    }

    /// <summary>MySql数据库</summary>
    internal class MySqlSession : RemoteDbSession
    {
        #region 构造函数
        public MySqlSession(IDatabase db) : base(db) { }
        #endregion

        #region 快速查询单表记录数
        /// <summary>快速查询单表记录数，大数据量时，稍有偏差。</summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public override Int64 QueryCountFast(String tableName)
        {
            tableName = tableName.Trim().Trim('`', '`').Trim();

            var db = Database.DatabaseName;
            var sql = $"select table_rows from information_schema.tables where table_schema='{db}' and table_name='{tableName}'";
            return ExecuteScalar<Int64>(sql);
        }
        #endregion

        #region 基本方法 查询/执行
        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>新增行的自动编号</returns>
        public override Int64 InsertAndGetIdentity(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            sql += ";Select LAST_INSERT_ID()";
            return base.InsertAndGetIdentity(sql, type, ps);
        }
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

        private String GetBatchSql(String tableName, IDataColumn[] columns, ICollection<String> updateColumns, ICollection<String> addColumns, IEnumerable<IIndexAccessor> list)
        {
            var sb = Pool.StringBuilder.Get();
            var db = Database as DbBase;

            // 字段列表
            //if (columns == null) columns = table.Columns.ToArray();
            sb.AppendFormat("Insert Into {0}(", db.FormatTableName(tableName));
            foreach (var dc in columns)
            {
                if (dc.Identity) continue;

                sb.Append(db.FormatName(dc.ColumnName));
                sb.Append(",");
            }
            sb.Length--;
            sb.Append(")");

            // 值列表
            sb.Append(" Values");
            foreach (var entity in list)
            {
                sb.Append("(");
                foreach (var dc in columns)
                {
                    if (dc.Identity) continue;

                    var value = entity[dc.Name];
                    sb.Append(db.FormatValue(dc, value));
                    sb.Append(",");
                }
                sb.Length--;
                sb.Append("),");
            }
            sb.Length--;

            // 重复键执行update
            if (updateColumns != null || addColumns != null)
            {
                sb.Append(" On Duplicate Key Update ");
                if (updateColumns != null)
                {
                    foreach (var dc in columns)
                    {
                        if (dc.Identity || dc.PrimaryKey) continue;

                        if (updateColumns.Contains(dc.Name) && (addColumns == null || !addColumns.Contains(dc.Name)))
                            sb.AppendFormat("{0}=Values({0}),", db.FormatName(dc.ColumnName));
                    }
                    sb.Length--;
                }
                if (addColumns != null)
                {
                    sb.Append(",");
                    foreach (var dc in columns)
                    {
                        if (dc.Identity || dc.PrimaryKey) continue;

                        if (addColumns.Contains(dc.Name))
                            sb.AppendFormat("{0}={0}+Values({0}),", db.FormatName(dc.ColumnName));
                    }
                    sb.Length--;
                }
            }

            return sb.Put(true);
        }

        public override Int32 Insert(String tableName, IDataColumn[] columns, IEnumerable<IIndexAccessor> list)
        {
            // 分批
            var batchSize = 10_000;
            var rs = 0;
            for (var i = 0; i < list.Count();)
            {
                var es = list.Skip(i).Take(batchSize).ToList();
                var sql = GetBatchSql(tableName, columns, null, null, es);
                rs += Execute(sql);

                i += es.Count;
            }

            return rs;
        }

        public override Int32 InsertOrUpdate(String tableName, IDataColumn[] columns, ICollection<String> updateColumns, ICollection<String> addColumns, IEnumerable<IIndexAccessor> list)
        {
            // 分批
            var batchSize = 10_000;
            var rs = 0;
            for (var i = 0; i < list.Count();)
            {
                var es = list.Skip(i).Take(batchSize).ToList();
                var sql = GetBatchSql(tableName, columns, updateColumns, addColumns, es);
                rs += Execute(sql);

                i += es.Count;
            }

            return rs;
        }
        #endregion
    }

    /// <summary>MySql元数据</summary>
    class MySqlMetaData : RemoteDbMetaData
    {
        public MySqlMetaData() => Types = _DataTypes;

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
        private static Dictionary<Type, String[]> _DataTypes = new Dictionary<Type, String[]>
        {
            { typeof(Byte[]), new String[] { "BLOB", "TINYBLOB", "MEDIUMBLOB", "LONGBLOB", "binary({0})", "varbinary({0})" } },
            //{ typeof(TimeSpan), new String[] { "TIME" } },
            //{ typeof(SByte), new String[] { "TINYINT" } },
            { typeof(Byte), new String[] { "TINYINT", "TINYINT UNSIGNED" } },
            { typeof(Int16), new String[] { "SMALLINT", "SMALLINT UNSIGNED" } },
            //{ typeof(UInt16), new String[] { "SMALLINT UNSIGNED" } },
            { typeof(Int32), new String[] { "INT", "YEAR", "MEDIUMINT", "MEDIUMINT UNSIGNED", "INT UNSIGNED" } },
            //{ typeof(UInt32), new String[] { "MEDIUMINT UNSIGNED", "INT UNSIGNED" } },
            { typeof(Int64), new String[] { "BIGINT", "BIT", "BIGINT UNSIGNED" } },
            //{ typeof(UInt64), new String[] { "BIT", "BIGINT UNSIGNED" } },
            { typeof(Single), new String[] { "FLOAT" } },
            { typeof(Double), new String[] { "DOUBLE" } },
            { typeof(Decimal), new String[] { "DECIMAL({0}, {1})" } },
            { typeof(DateTime), new String[] { "DATETIME", "DATE", "TIMESTAMP", "TIME" } },
            { typeof(String), new String[] { "NVARCHAR({0})", "TEXT", "CHAR({0})", "NCHAR({0})", "VARCHAR({0})", "SET", "ENUM", "TINYTEXT", "TEXT", "MEDIUMTEXT", "LONGTEXT" } },
            { typeof(Boolean), new String[] { "TINYINT" } },
        };
        #endregion

        #region 架构
        protected override List<IDataTable> OnGetTables(String[] names)
        {
            var ss = Database.CreateSession();
            var db = Database.DatabaseName;

            var sql = $"SHOW TABLE STATUS FROM `{db}`";
            var dt = ss.Query(sql, null);
            if (dt.Rows.Count == 0) return null;

            var list = new List<IDataTable>();
            var hs = new HashSet<String>(names ?? new String[0], StringComparer.OrdinalIgnoreCase);

            // 所有表
            foreach (var dr in dt)
            {
                var name = dr["Name"] + "";
                if (name.IsNullOrEmpty() || hs.Count > 0 && !hs.Contains(name)) continue;

                var table = DAL.CreateTable();
                table.TableName = name;
                table.Description = dr["Comment"] + "";

                #region 字段
                sql = $"SHOW FULL COLUMNS FROM `{db}`.`{name}`";
                var dcs = ss.Query(sql, null);
                foreach (var dc in dcs)
                {
                    var field = table.CreateColumn();

                    field.ColumnName = dc["Field"] + "";
                    field.RawType = dc["Type"] + "";
                    field.DataType = GetDataType(field.RawType);
                    field.Description = dc["Comment"] + "";

                    if (dc["Extra"] + "" == "auto_increment") field.Identity = true;
                    if (dc["Key"] + "" == "PRI") field.PrimaryKey = true;
                    if (dc["Null"] + "" == "YES") field.Nullable = true;

                    field.Length = field.RawType.Substring("(", ")").ToInt();

                    if (field.DataType == null)
                    {
                        if (field.RawType.StartsWithIgnoreCase("varchar", "nvarchar")) field.DataType = typeof(String);
                    }

                    // MySql中没有布尔型，这里处理YN枚举作为布尔型
                    if (field.RawType == "enum('N','Y')" || field.RawType == "enum('Y','N')") field.DataType = typeof(Boolean);

                    table.Columns.Add(field);
                }
                #endregion

                #region 索引
                sql = $"SHOW INDEX FROM `{db}`.`{name}`";
                var dis = ss.Query(sql, null);
                foreach (var dr2 in dis)
                {
                    var dname = dr2["Key_name"] + "";
                    var di = table.Indexes.FirstOrDefault(e => e.Name == dname) ?? table.CreateIndex();
                    di.Name = dname;
                    di.Unique = dr2.Get<Int32>("Non_unique") == 0;

                    var cname = dr2.Get<String>("Column_name");
                    var cs = new List<String>();
                    if (di.Columns != null && di.Columns.Length > 0) cs.AddRange(di.Columns);
                    cs.Add(cname);
                    di.Columns = cs.ToArray();

                    table.Indexes.Add(di);
                }
                #endregion

                // 修正关系数据
                table.Fix();

                list.Add(table);
            }

            // 找到使用枚举作为布尔型的旧表
            var es = (Database as MySql).EnumTables;
            foreach (var table in list)
            {
                if (!es.Contains(table.TableName))
                {
                    var dc = table.Columns.FirstOrDefault(c => c.DataType == typeof(Boolean)
                      && c.RawType.EqualIgnoreCase("enum('N','Y')", "enum('Y','N')"));
                    if (dc != null)
                    {
                        es.Add(table.TableName);

                        WriteLog("发现MySql中旧格式的布尔型字段 {0} {1}", table.TableName, dc);
                    }
                }
            }

            return list;
        }

        public override String FieldClause(IDataColumn field, Boolean onlyDefine)
        {
            var sql = base.FieldClause(field, onlyDefine);
            // 加上注释
            if (!String.IsNullOrEmpty(field.Description)) sql = $"{sql} COMMENT '{field.Description}'";
            return sql;
        }

        protected override String GetFieldConstraints(IDataColumn field, Boolean onlyDefine)
        {
            String str = null;
            if (!field.Nullable) str = " NOT NULL";

            if (field.Identity) str = " NOT NULL AUTO_INCREMENT";

            return str;
        }
        #endregion

        #region 反向工程
        protected override Boolean DatabaseExist(String databaseName)
        {
            var dt = GetSchema(_.Databases, new String[] { databaseName });
            return dt != null && dt.Rows != null && dt.Rows.Count > 0;
        }

        public override String CreateDatabaseSQL(String dbname, String file) => base.CreateDatabaseSQL(dbname, file) + " DEFAULT CHARACTER SET utf8mb4";

        public override String DropDatabaseSQL(String dbname) => $"Drop Database If Exists {FormatName(dbname)}";

        public override String CreateTableSQL(IDataTable table)
        {
            var fs = new List<IDataColumn>(table.Columns);

            //var sb = new StringBuilder(32 + fs.Count * 20);
            var sb = Pool.StringBuilder.Get();
            var pks = new List<String>();

            sb.AppendFormat("Create Table If Not Exists {0}(", FormatName(table.TableName));
            for (var i = 0; i < fs.Count; i++)
            {
                sb.AppendLine();
                sb.Append("\t");
                sb.Append(FieldClause(fs[i], true));
                if (i < fs.Count - 1) sb.Append(",");

                if (fs[i].PrimaryKey) pks.Add(FormatName(fs[i].ColumnName));
            }
            // 如果有自增，则自增必须作为主键
            foreach (var item in table.Columns)
            {
                if (item.Identity && !item.PrimaryKey)
                {
                    pks.Clear();
                    pks.Add(FormatName(item.ColumnName));
                    break;
                }
            }
            if (pks.Count > 0)
            {
                sb.AppendLine(",");
                sb.AppendFormat("\tPrimary Key ({0})", String.Join(",", pks.ToArray()));
            }
            sb.AppendLine();
            sb.Append(")");

            // 引擎和编码
            //sb.Append(" ENGINE=InnoDB");
            sb.Append(" DEFAULT CHARSET=utf8mb4");
            sb.Append(";");

            return sb.Put(true);
        }

        public override String AddTableDescriptionSQL(IDataTable table)
        {
            if (String.IsNullOrEmpty(table.Description)) return null;

            return $"Alter Table {FormatName(table.TableName)} Comment '{table.Description}'";
        }

        public override String AlterColumnSQL(IDataColumn field, IDataColumn oldfield) => $"Alter Table {FormatName(field.Table.TableName)} Modify Column {FieldClause(field, false)}";

        public override String AddColumnDescriptionSQL(IDataColumn field)
        {
            // 返回String.Empty表示已经在别的SQL中处理
            return String.Empty;
        }
        #endregion
    }
}