using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Text;

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
                        if (_Factory == null) _Factory = GetProviderFactory("MySql.Data.dll", "MySql.Data.MySqlClient.MySqlClientFactory");
                    }
                }

                return _Factory;
            }
        }

        const String Server_Key = "Server";
        const String CharSet = "CharSet";
        const String AllowZeroDatetime = "Allow Zero Datetime";
        const String MaxPoolSize = "MaxPoolSize";
        protected override void OnSetConnectionString(ConnectionStringBuilder builder)
        {
            base.OnSetConnectionString(builder);

            var key = builder[Server_Key];
            if (key.EqualIgnoreCase(".", "localhost"))
            {
                //builder[Server_Key] = "127.0.0.1";
                builder[Server_Key] = IPAddress.Loopback.ToString();
            }
            builder.TryAdd(CharSet, "utf8");
            //if (!builder.ContainsKey(AllowZeroDatetime)) builder[AllowZeroDatetime] = "True";
            // 默认最大连接数1000
            builder.TryAdd(MaxPoolSize, "1000");
        }
        #endregion

        #region 方法
        /// <summary>创建数据库会话</summary>
        /// <returns></returns>
        protected override IDbSession OnCreateSession() { return new MySqlSession(this); }

        /// <summary>创建元数据对象</summary>
        /// <returns></returns>
        protected override IMetaData OnCreateMetaData() { return new MySqlMetaData(); }

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
                    "LOG,User,Role";
            }
        }

        /// <summary>格式化关键字</summary>
        /// <param name="keyWord">关键字</param>
        /// <returns></returns>
        public override String FormatKeyWord(String keyWord)
        {
            //if (String.IsNullOrEmpty(keyWord)) throw new ArgumentNullException("keyWord");
            if (String.IsNullOrEmpty(keyWord)) return keyWord;

            if (keyWord.StartsWith("`") && keyWord.EndsWith("`")) return keyWord;

            return String.Format("`{0}`", keyWord);
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
                return Convert.ToBoolean(value) ? "'Y'" : "'N'";
            }

            return base.FormatValue(field, value);
        }

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

            if (type == null) type = value?.GetType();

            // MySql的枚举要用 DbType.String
            if (type == typeof(Boolean))
            {
                dp.DbType = DbType.String;
                dp.Value = value.ToBoolean() ? 'Y' : 'N';
            }

            return dp;
        }

        /// <summary>系统数据库名</summary>
        public override String SystemDatabaseName { get { return "mysql"; } }

        /// <summary>字符串相加</summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public override String StringConcat(String left, String right) { return String.Format("concat({0},{1})", (!String.IsNullOrEmpty(left) ? left : "\'\'"), (!String.IsNullOrEmpty(right) ? right : "\'\'")); }
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

            var db = DatabaseName;
            var sql = String.Format("select table_rows from information_schema.tables where table_schema='{1}' and table_name='{0}'", tableName, db);
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
    }

    /// <summary>MySql元数据</summary>
    class MySqlMetaData : RemoteDbMetaData
    {
        public MySqlMetaData()
        {
            Types = _DataTypes;
        }

        protected override void FixTable(IDataTable table, DataRow dr, IDictionary<String, DataTable> data)
        {
            // 注释
            if (TryGetDataRowValue(dr, "TABLE_COMMENT", out String comment)) table.Description = comment;

            base.FixTable(table, dr, data);
        }

        //protected override Boolean IsColumnChanged(IDataColumn entityColumn, IDataColumn dbColumn, IDatabase entityDb)
        //{
        //    return base.IsColumnChanged(entityColumn, dbColumn, entityDb);
        //}

        protected override void FixField(IDataColumn field, DataRow dr)
        {
            // 修正原始类型
            if (TryGetDataRowValue(dr, "COLUMN_TYPE", out String rawType)) field.RawType = rawType;

            // 修正自增字段
            if (TryGetDataRowValue(dr, "EXTRA", out String extra) && extra == "auto_increment") field.Identity = true;

            // 修正主键
            if (TryGetDataRowValue(dr, "COLUMN_KEY", out String key)) field.PrimaryKey = key == "PRI";

            // 注释
            if (TryGetDataRowValue(dr, "COLUMN_COMMENT", out String comment)) field.Description = comment;

            // 布尔类型
            // MySql中没有布尔型，这里处理YN枚举作为布尔型
            if (field.RawType == "enum('N','Y')" || field.RawType == "enum('Y','N')")
            {
                field.DataType = typeof(Boolean);
                //// 处理默认值
                //if (!String.IsNullOrEmpty(field.Default))
                //{
                //    if (field.Default == "Y")
                //        field.Default = "true";
                //    else if (field.Default == "N")
                //        field.Default = "false";
                //}
                return;
            }
            base.FixField(field, dr);
        }

        protected override String GetFieldType(IDataColumn field)
        {
            if (field.DataType == typeof(Boolean)) return "enum('N','Y')";

            return base.GetFieldType(field);
        }

        public override String FieldClause(IDataColumn field, Boolean onlyDefine)
        {
            var sql = base.FieldClause(field, onlyDefine);
            // 加上注释
            if (!String.IsNullOrEmpty(field.Description)) sql = String.Format("{0} COMMENT '{1}'", sql, field.Description);
            return sql;
        }

        protected override String GetFieldConstraints(IDataColumn field, Boolean onlyDefine)
        {
            String str = null;
            if (!field.Nullable) str = " NOT NULL";

            if (field.Identity) str = " NOT NULL AUTO_INCREMENT";

            return str;
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
            { typeof(Decimal), new String[] { "DECIMAL" } },
            { typeof(DateTime), new String[] { "DATETIME", "DATE", "TIMESTAMP", "TIME" } },
            { typeof(String), new String[] { "NVARCHAR({0})", "TEXT", "CHAR({0})", "NCHAR({0})", "VARCHAR({0})", "SET", "ENUM", "TINYTEXT", "TEXT", "MEDIUMTEXT", "LONGTEXT" } }
        };

        #region 架构定义
        protected override Boolean DatabaseExist(String databaseName)
        {
            var dt = GetSchema(_.Databases, new String[] { databaseName });
            return dt != null && dt.Rows != null && dt.Rows.Count > 0;
        }

        public override String DropDatabaseSQL(String dbname)
        {
            return String.Format("Drop Database If Exists {0}", FormatName(dbname));
        }

        public override String CreateTableSQL(IDataTable table)
        {
            var fs = new List<IDataColumn>(table.Columns);

            var sb = new StringBuilder(32 + fs.Count * 20);
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

            return sb.ToString();
        }

        public override String AddTableDescriptionSQL(IDataTable table)
        {
            if (String.IsNullOrEmpty(table.Description)) return null;

            return String.Format("Alter Table {0} Comment '{1}'", FormatName(table.TableName), table.Description);
        }

        public override String AlterColumnSQL(IDataColumn field, IDataColumn oldfield)
        {
            return String.Format("Alter Table {0} Modify Column {1}", FormatName(field.Table.TableName), FieldClause(field, false));
        }

        public override String AddColumnDescriptionSQL(IDataColumn field)
        {
            // 返回String.Empty表示已经在别的SQL中处理
            return String.Empty;

            //if (String.IsNullOrEmpty(field.Description)) return null;

            //return String.Format("Alter Table {0} Modify {1} Comment '{2}'", FormatKeyWord(field.Table.Name), FormatKeyWord(field.Name), field.Description);
        }
        #endregion
    }
}