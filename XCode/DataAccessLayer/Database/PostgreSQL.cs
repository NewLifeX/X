using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace XCode.DataAccessLayer
{
    internal class PostgreSQL : RemoteDb
    {
        #region 属性
        /// <summary>返回数据库类型。</summary>
        public override DatabaseType Type => DatabaseType.PostgreSQL;

        private static DbProviderFactory _Factory;
        /// <summary>工厂</summary>
        public override DbProviderFactory Factory
        {
            get
            {
                if (_Factory == null)
                {
                    lock (typeof(PostgreSQL))
                    {
                        if (_Factory == null) _Factory = GetProviderFactory("Npgsql.dll", "Npgsql.NpgsqlFactory");
                    }
                }

                return _Factory;
            }
        }
        #endregion

        #region 方法
        /// <summary>创建数据库会话</summary>
        /// <returns></returns>
        protected override IDbSession OnCreateSession() => new PostgreSQLSession(this);

        /// <summary>创建元数据对象</summary>
        /// <returns></returns>
        protected override IMetaData OnCreateMetaData() => new PostgreSQLMetaData();

        public override Boolean Support(String providerName)
        {
            providerName = providerName.ToLower();
            if (providerName.Contains("postgresql.data.postgresqlclient")) return true;
            if (providerName.Contains("postgresql")) return true;
            if (providerName.Contains("npgsql")) return true;

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
        public override String PageSplit(String sql, Int64 startRowIndex, Int64 maximumRows, String keyColumn) => MySql.PageSplitByLimit(sql, startRowIndex, maximumRows);

        /// <summary>构造分页SQL</summary>
        /// <param name="builder">查询生成器</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>分页SQL</returns>
        public override SelectBuilder PageSplit(SelectBuilder builder, Int64 startRowIndex, Int64 maximumRows) => MySql.PageSplitByLimit(builder, startRowIndex, maximumRows);
        #endregion

        #region 数据库特性
        ///// <summary>当前时间函数</summary>
        //public override String DateTimeNow { get { return "now()"; } }

        //protected override string ReservedWordsStr
        //{
        //    get
        //    {
        //        return "ACCESSIBLE,ADD,ALL,ALTER,ANALYZE,AND,AS,ASC,ASENSITIVE,BEFORE,BETWEEN,BIGINT,BINARY,BLOB,BOTH,BY,CALL,CASCADE,CASE,CHANGE,CHAR,CHARACTER,CHECK,COLLATE,COLUMN,CONDITION,CONNECTION,CONSTRAINT,CONTINUE,CONTRIBUTORS,CONVERT,CREATE,CROSS,CURRENT_DATE,CURRENT_TIME,CURRENT_TIMESTAMP,CURRENT_USER,CURSOR,DATABASE,DATABASES,DAY_HOUR,DAY_MICROSECOND,DAY_MINUTE,DAY_SECOND,DEC,DECIMAL,DECLARE,DEFAULT,DELAYED,DELETE,DESC,DESCRIBE,DETERMINISTIC,DISTINCT,DISTINCTROW,DIV,DOUBLE,DROP,DUAL,EACH,ELSE,ELSEIF,ENCLOSED,ESCAPED,EXISTS,EXIT,EXPLAIN,FALSE,FETCH,FLOAT,FLOAT4,FLOAT8,FOR,FORCE,FOREIGN,FROM,FULLTEXT,GRANT,GROUP,HAVING,HIGH_PRIORITY,HOUR_MICROSECOND,HOUR_MINUTE,HOUR_SECOND,IF,IGNORE,IN,INDEX,INFILE,INNER,INOUT,INSENSITIVE,INSERT,INT,INT1,INT2,INT3,INT4,INT8,INTEGER,INTERVAL,INTO,IS,ITERATE,JOIN,KEY,KEYS,KILL,LEADING,LEAVE,LEFT,LIKE,LIMIT,LINEAR,LINES,LOAD,LOCALTIME,LOCALTIMESTAMP,LOCK,LONG,LONGBLOB,LONGTEXT,LOOP,LOW_PRIORITY,MATCH,MEDIUMBLOB,MEDIUMINT,MEDIUMTEXT,MIDDLEINT,MINUTE_MICROSECOND,MINUTE_SECOND,MOD,MODIFIES,NATURAL,NOT,NO_WRITE_TO_BINLOG,NULL,NUMERIC,ON,OPTIMIZE,OPTION,OPTIONALLY,OR,ORDER,OUT,OUTER,OUTFILE,PRECISION,PRIMARY,PROCEDURE,PURGE,RANGE,READ,READS,READ_ONLY,READ_WRITE,REAL,REFERENCES,REGEXP,RELEASE,RENAME,REPEAT,REPLACE,REQUIRE,RESTRICT,RETURN,REVOKE,RIGHT,RLIKE,SCHEMA,SCHEMAS,SECOND_MICROSECOND,SELECT,SENSITIVE,SEPARATOR,SET,SHOW,SMALLINT,SPATIAL,SPECIFIC,SQL,SQLEXCEPTION,SQLSTATE,SQLWARNING,SQL_BIG_RESULT,SQL_CALC_FOUND_ROWS,SQL_SMALL_RESULT,SSL,STARTING,STRAIGHT_JOIN,TABLE,TERMINATED,THEN,TINYBLOB,TINYINT,TINYTEXT,TO,TRAILING,TRIGGER,TRUE,UNDO,UNION,UNIQUE,UNLOCK,UNSIGNED,UPDATE,UPGRADE,USAGE,USE,USING,UTC_DATE,UTC_TIME,UTC_TIMESTAMP,VALUES,VARBINARY,VARCHAR,VARCHARACTER,VARYING,WHEN,WHERE,WHILE,WITH,WRITE,X509,XOR,YEAR_MONTH,ZEROFILL";
        //    }
        //}

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

            if (keyWord.StartsWith("`") && keyWord.EndsWith("`")) return keyWord;

            return String.Format("`{0}`", keyWord);
        }

        /// <summary>格式化数据为SQL数据</summary>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public override String FormatValue(IDataColumn field, Object value)
        {
            if (field.DataType == typeof(String))
            {
                if (value == null) return field.Nullable ? "null" : "``";
                //云飞扬：这里注释掉，应该返回``而不是null字符
                //if (String.IsNullOrEmpty(value.ToString()) && field.Nullable) return "null";
                return "`" + value + "`";
            }
            else if (field.DataType == typeof(Boolean))
            {
                return (Boolean)value ? "'Y'" : "'N'";
            }

            return base.FormatValue(field, value);
        }

        /// <summary>长文本长度</summary>
        public override Int32 LongTextLength => 4000;

        protected internal override String ParamPrefix => "$";

        /// <summary>系统数据库名</summary>
        public override String SystemDatabaseName => "PostgreSQL";

        /// <summary>字符串相加</summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public override String StringConcat(String left, String right) => (!String.IsNullOrEmpty(left) ? left : "\'\'") + "||" + (!String.IsNullOrEmpty(right) ? right : "\'\'");
        #endregion
    }

    /// <summary>PostgreSQL数据库</summary>
    internal class PostgreSQLSession : RemoteDbSession
    {
        #region 构造函数
        public PostgreSQLSession(IDatabase db) : base(db) { }
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

    /// <summary>PostgreSQL元数据</summary>
    internal class PostgreSQLMetaData : RemoteDbMetaData
    {
        public PostgreSQLMetaData() => Types = _DataTypes;

        protected override void FixTable(IDataTable table, DataRow dr, IDictionary<String, DataTable> data)
        {
            // 注释
            if (TryGetDataRowValue(dr, "TABLE_COMMENT", out String comment)) table.Description = comment;

            base.FixTable(table, dr, data);
        }

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
            if (field.RawType == "enum")
            {
                // PostgreSQL中没有布尔型，这里处理YN枚举作为布尔型
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
            }

            base.FixField(field, dr);
        }

        //protected override DataRow[] FindDataType(IDataColumn field, String typeName, Boolean? isLong)
        //{
        //    DataRow[] drs = base.FindDataType(field, typeName, isLong);
        //    if (drs != null && drs.Length > 1)
        //    {
        //        // 无符号/有符号
        //        if (!String.IsNullOrEmpty(field.RawType))
        //        {
        //            Boolean IsUnsigned = field.RawType.ToLower().Contains("unsigned");

        //            foreach (DataRow dr in drs)
        //            {
        //                String format = GetDataRowValue<String>(dr, "CreateFormat");

        //                if (IsUnsigned && format.ToLower().Contains("unsigned"))
        //                    return new DataRow[] { dr };
        //                else if (!IsUnsigned && !format.ToLower().Contains("unsigned"))
        //                    return new DataRow[] { dr };
        //            }
        //        }

        //        // 字符串
        //        if (typeName == typeof(String).FullName)
        //        {
        //            foreach (var dr in drs)
        //            {
        //                var name = GetDataRowValue<String>(dr, "TypeName");
        //                if (name == "NVARCHAR" && field.Length <= Database.LongTextLength)
        //                    return new DataRow[] { dr };
        //                else if (name == "LONGTEXT" && field.Length > Database.LongTextLength)
        //                    return new DataRow[] { dr };
        //            }
        //            foreach (var dr in drs)
        //            {
        //                var name = GetDataRowValue<String>(dr, "TypeName");
        //                if (name == "VARCHAR" && field.Length <= Database.LongTextLength)
        //                    return new DataRow[] { dr };
        //            }
        //        }

        //        // 时间日期
        //        if (typeName == typeof(DateTime).FullName)
        //        {
        //            // DateTime的范围是0001到9999
        //            // Timestamp的范围是1970到2038
        //            //String d = field.Default;
        //            //CheckAndGetDefault(field, ref d);
        //            foreach (DataRow dr in drs)
        //            {
        //                var name = GetDataRowValue<String>(dr, "TypeName");
        //                if (name == "DATETIME") return new DataRow[] { dr };
        //            }
        //        }
        //    }
        //    return drs;
        //}

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
        private static readonly Dictionary<Type, String[]> _DataTypes = new Dictionary<Type, String[]>
        {
            { typeof(Byte[]), new String[] { "BLOB", "TINYBLOB", "MEDIUMBLOB", "LONGBLOB", "binary({0})", "varbinary({0})" } },
            //{ typeof(TimeSpan), new String[] { "TIME" } },
            //{ typeof(SByte), new String[] { "TINYINT" } },
            { typeof(Byte), new String[] { "TINYINT UNSIGNED" } },
            { typeof(Int16), new String[] { "SMALLINT" } },
            //{ typeof(UInt16), new String[] { "SMALLINT UNSIGNED" } },
            { typeof(Int32), new String[] { "INT", "YEAR", "MEDIUMINT" } },
            //{ typeof(UInt32), new String[] { "MEDIUMINT UNSIGNED", "INT UNSIGNED" } },
            { typeof(Int64), new String[] { "BIGINT" } },
            //{ typeof(UInt64), new String[] { "BIT", "BIGINT UNSIGNED" } },
            { typeof(Single), new String[] { "FLOAT" } },
            { typeof(Double), new String[] { "DOUBLE" } },
            { typeof(Decimal), new String[] { "DECIMAL" } },
            { typeof(DateTime), new String[] { "DATE", "DATETIME", "TIMESTAMP" } },
            { typeof(String), new String[] { "NVARCHAR({0})", "TEXT", "CHAR({0})", "NCHAR({0})", "VARCHAR({0})", "SET", "ENUM", "TINYTEXT", "TEXT", "MEDIUMTEXT", "LONGTEXT" } }
        };

        #region 架构定义
        //public override object SetSchema(DDLSchema schema, params object[] values)
        //{
        //    if (schema == DDLSchema.DatabaseExist)
        //    {
        //        IDbSession session = Database.CreateSession();

        //        DataTable dt = GetSchema(_.Databases, new String[] { values != null && values.Length > 0 ? (String)values[0] : session.DatabaseName });
        //        if (dt == null || dt.Rows == null || dt.Rows.Count < 1) return false;
        //        return true;
        //    }

        //    return base.SetSchema(schema, values);
        //}

        protected override Boolean DatabaseExist(String databaseName)
        {
            //return base.DatabaseExist(databaseName);

            var session = Database.CreateSession();
            var dt = GetSchema(_.Databases, new String[] { databaseName });
            return dt != null && dt.Rows != null && dt.Rows.Count > 0;
        }

        //public override string CreateDatabaseSQL(string dbname, string file)
        //{
        //    return String.Format("Create Database Binary {0}", FormatKeyWord(dbname));
        //}

        public override String DropDatabaseSQL(String dbname) => String.Format("Drop Database If Exists {0}", FormatName(dbname));

        public override String CreateTableSQL(IDataTable table)
        {
            var fs = new List<IDataColumn>(table.Columns);

            var sb = new StringBuilder(32 + fs.Count * 20);
            String key = null;

            sb.AppendFormat("Create Table If Not Exists {0}(", FormatName(table.TableName));
            for (var i = 0; i < fs.Count; i++)
            {
                sb.AppendLine();
                sb.Append("\t");
                sb.Append(FieldClause(fs[i], true));
                if (i < fs.Count - 1) sb.Append(",");

                if (fs[i].PrimaryKey) key = fs[i].ColumnName;
            }
            if (!String.IsNullOrEmpty(key))
            {
                sb.AppendLine(",");
                sb.AppendFormat("\tPrimary Key ({0})", FormatName(key));
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

        public override String AlterColumnSQL(IDataColumn field, IDataColumn oldfield) => String.Format("Alter Table {0} Modify Column {1}", FormatName(field.Table.TableName), FieldClause(field, false));

        public override String AddColumnDescriptionSQL(IDataColumn field)
        {
            // 返回String.Empty表示已经在别的SQL中处理
            return String.Empty;

            //if (String.IsNullOrEmpty(field.Description)) return null;

            //return String.Format("Alter Table {0} Modify {1} Comment '{2}'", FormatKeyWord(field.Table.Name), FormatKeyWord(field.Name), field.Description);
        }
        #endregion

        #region 辅助函数

        #endregion
    }
}