using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace XCode.DataAccessLayer
{
    class PostgreSQL : RemoteDb
    {
        #region 属性
        /// <summary>
        /// 返回数据库类型。
        /// </summary>
        public override DatabaseType DbType
        {
            get { return DatabaseType.PostgreSQL; }
        }

        private static DbProviderFactory _dbProviderFactory;
        /// <summary>
        /// 提供者工厂
        /// </summary>
        static DbProviderFactory dbProviderFactory
        {
            get
            {
                //if (_dbProviderFactory == null) _dbProviderFactory = DbProviderFactories.GetFactory("PostgreSQL.Data.PostgreSQLClient");
                if (_dbProviderFactory == null) _dbProviderFactory = GetProviderFactory("Npgsql.dll", "Npgsql.NpgsqlFactory");

                return _dbProviderFactory;
            }
        }

        /// <summary>工厂</summary>
        public override DbProviderFactory Factory
        {
            get { return dbProviderFactory; }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 创建数据库会话
        /// </summary>
        /// <returns></returns>
        protected override IDbSession OnCreateSession()
        {
            return new PostgreSQLSession();
        }

        /// <summary>
        /// 创建元数据对象
        /// </summary>
        /// <returns></returns>
        protected override IMetaData OnCreateMetaData()
        {
            return new PostgreSQLMetaData();
        }
        #endregion

        #region 分页
        /// <summary>
        /// 已重写。获取分页
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
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
                sql = String.Format("{0} limit {1} offset {2}", sql, startRowIndex, maximumRows);
            return sql;
        }
        #endregion

        #region 数据库特性
        /// <summary>
        /// 当前时间函数
        /// </summary>
        public override String DateTimeNow { get { return "now()"; } }

        //protected override string ReservedWordsStr
        //{
        //    get
        //    {
        //        return "ACCESSIBLE,ADD,ALL,ALTER,ANALYZE,AND,AS,ASC,ASENSITIVE,BEFORE,BETWEEN,BIGINT,BINARY,BLOB,BOTH,BY,CALL,CASCADE,CASE,CHANGE,CHAR,CHARACTER,CHECK,COLLATE,COLUMN,CONDITION,CONNECTION,CONSTRAINT,CONTINUE,CONTRIBUTORS,CONVERT,CREATE,CROSS,CURRENT_DATE,CURRENT_TIME,CURRENT_TIMESTAMP,CURRENT_USER,CURSOR,DATABASE,DATABASES,DAY_HOUR,DAY_MICROSECOND,DAY_MINUTE,DAY_SECOND,DEC,DECIMAL,DECLARE,DEFAULT,DELAYED,DELETE,DESC,DESCRIBE,DETERMINISTIC,DISTINCT,DISTINCTROW,DIV,DOUBLE,DROP,DUAL,EACH,ELSE,ELSEIF,ENCLOSED,ESCAPED,EXISTS,EXIT,EXPLAIN,FALSE,FETCH,FLOAT,FLOAT4,FLOAT8,FOR,FORCE,FOREIGN,FROM,FULLTEXT,GRANT,GROUP,HAVING,HIGH_PRIORITY,HOUR_MICROSECOND,HOUR_MINUTE,HOUR_SECOND,IF,IGNORE,IN,INDEX,INFILE,INNER,INOUT,INSENSITIVE,INSERT,INT,INT1,INT2,INT3,INT4,INT8,INTEGER,INTERVAL,INTO,IS,ITERATE,JOIN,KEY,KEYS,KILL,LEADING,LEAVE,LEFT,LIKE,LIMIT,LINEAR,LINES,LOAD,LOCALTIME,LOCALTIMESTAMP,LOCK,LONG,LONGBLOB,LONGTEXT,LOOP,LOW_PRIORITY,MATCH,MEDIUMBLOB,MEDIUMINT,MEDIUMTEXT,MIDDLEINT,MINUTE_MICROSECOND,MINUTE_SECOND,MOD,MODIFIES,NATURAL,NOT,NO_WRITE_TO_BINLOG,NULL,NUMERIC,ON,OPTIMIZE,OPTION,OPTIONALLY,OR,ORDER,OUT,OUTER,OUTFILE,PRECISION,PRIMARY,PROCEDURE,PURGE,RANGE,READ,READS,READ_ONLY,READ_WRITE,REAL,REFERENCES,REGEXP,RELEASE,RENAME,REPEAT,REPLACE,REQUIRE,RESTRICT,RETURN,REVOKE,RIGHT,RLIKE,SCHEMA,SCHEMAS,SECOND_MICROSECOND,SELECT,SENSITIVE,SEPARATOR,SET,SHOW,SMALLINT,SPATIAL,SPECIFIC,SQL,SQLEXCEPTION,SQLSTATE,SQLWARNING,SQL_BIG_RESULT,SQL_CALC_FOUND_ROWS,SQL_SMALL_RESULT,SSL,STARTING,STRAIGHT_JOIN,TABLE,TERMINATED,THEN,TINYBLOB,TINYINT,TINYTEXT,TO,TRAILING,TRIGGER,TRUE,UNDO,UNION,UNIQUE,UNLOCK,UNSIGNED,UPDATE,UPGRADE,USAGE,USE,USING,UTC_DATE,UTC_TIME,UTC_TIMESTAMP,VALUES,VARBINARY,VARCHAR,VARCHARACTER,VARYING,WHEN,WHERE,WHILE,WITH,WRITE,X509,XOR,YEAR_MONTH,ZEROFILL";
        //    }
        //}

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
            //if (String.IsNullOrEmpty(keyWord)) throw new ArgumentNullException("keyWord");
            if (String.IsNullOrEmpty(keyWord)) return keyWord;

            if (keyWord.StartsWith("`") && keyWord.EndsWith("`")) return keyWord;

            return String.Format("`{0}`", keyWord);
        }

        /// <summary>
        /// 格式化数据为SQL数据
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override string FormatValue(IDataColumn field, object value)
        {
            if (field.DataType == typeof(String))
            {
                if (value == null) return field.Nullable ? "null" : "``";
                if (String.IsNullOrEmpty(value.ToString()) && field.Nullable) return "null";
                return "`" + value + "`";
            }
            else if (field.DataType == typeof(Boolean))
            {
                return (Boolean)value ? "'Y'" : "'N'";
            }

            return base.FormatValue(field, value);
        }

        /// <summary>
        /// 长文本长度
        /// </summary>
        public override int LongTextLength { get { return 4000; } }

        /// <summary>系统数据库名</summary>
        public override String SystemDatabaseName { get { return "PostgreSQL"; } }
        #endregion
    }

    /// <summary>
    /// PostgreSQL数据库
    /// </summary>
    internal class PostgreSQLSession : RemoteDbSession
    {
        #region 基本方法 查询/执行
        /// <summary>
        /// 执行插入语句并返回新增行的自动编号
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>新增行的自动编号</returns>
        public override Int64 InsertAndGetIdentity(string sql) { return ExecuteScalar<Int64>(sql + ";Select LAST_INSERT_ID()"); }
        #endregion
    }

    /// <summary>
    /// PostgreSQL元数据
    /// </summary>
    class PostgreSQLMetaData : RemoteDbMetaData
    {
        protected override void FixTable(IDataTable table, DataRow dr)
        {
            // 注释
            String comment = null;
            if (TryGetDataRowValue<String>(dr, "TABLE_COMMENT", out comment)) table.Description = comment;

            base.FixTable(table, dr);
        }

        protected override void FixField(IDataColumn field, DataRow dr)
        {
            // 修正原始类型
            String rawType = null;
            if (TryGetDataRowValue<String>(dr, "COLUMN_TYPE", out rawType)) field.RawType = rawType;

            // 修正自增字段
            String extra = null;
            if (TryGetDataRowValue<String>(dr, "EXTRA", out extra) && extra == "auto_increment") field.Identity = true;

            // 修正主键
            String key = null;
            if (TryGetDataRowValue<String>(dr, "COLUMN_KEY", out key)) field.PrimaryKey = key == "PRI";

            // 注释
            String comment = null;
            if (TryGetDataRowValue<String>(dr, "COLUMN_COMMENT", out comment)) field.Description = comment;

            // 布尔类型
            if (field.RawType == "enum")
            {
                // PostgreSQL中没有布尔型，这里处理YN枚举作为布尔型
                if (field.RawType == "enum('N','Y')" || field.RawType == "enum('Y','N')")
                {
                    field.DataType = typeof(Boolean);
                    // 处理默认值
                    if (!String.IsNullOrEmpty(field.Default))
                    {
                        if (field.Default == "Y")
                            field.Default = "true";
                        else if (field.Default == "N")
                            field.Default = "false";
                    }
                    return;
                }
            }

            base.FixField(field, dr);
        }

        protected override DataRow[] FindDataType(IDataColumn field, string typeName, bool? isLong)
        {
            DataRow[] drs = base.FindDataType(field, typeName, isLong);
            if (drs != null && drs.Length > 1)
            {
                // 无符号/有符号
                if (!String.IsNullOrEmpty(field.RawType))
                {
                    Boolean IsUnsigned = field.RawType.ToLower().Contains("unsigned");

                    foreach (DataRow dr in drs)
                    {
                        String format = GetDataRowValue<String>(dr, "CreateFormat");

                        if (IsUnsigned && format.ToLower().Contains("unsigned"))
                            return new DataRow[] { dr };
                        else if (!IsUnsigned && !format.ToLower().Contains("unsigned"))
                            return new DataRow[] { dr };
                    }
                }

                // 字符串
                if (typeName == typeof(String).FullName)
                {
                    foreach (DataRow dr in drs)
                    {
                        String name = GetDataRowValue<String>(dr, "TypeName");
                        if ((name == "NVARCHAR" && field.IsUnicode || name == "VARCHAR" && !field.IsUnicode) && field.Length <= Database.LongTextLength)
                            return new DataRow[] { dr };
                        else if (name == "LONGTEXT" && field.Length > Database.LongTextLength)
                            return new DataRow[] { dr };
                    }
                }

                // 时间日期
                if (typeName == typeof(DateTime).FullName)
                {
                    // DateTime的范围是0001到9999
                    // Timestamp的范围是1970到2038
                    String d = CheckAndGetDefaultDateTimeNow(field.Table.DbType, field.Default);
                    foreach (DataRow dr in drs)
                    {
                        String name = GetDataRowValue<String>(dr, "TypeName");
                        if (name == "DATETIME" && String.IsNullOrEmpty(field.Default))
                            return new DataRow[] { dr };
                        else if (name == "TIMESTAMP" && (d == "now()" || field.Default == "CURRENT_TIMESTAMP"))
                            return new DataRow[] { dr };
                    }
                }
            }
            return drs;
        }

        //protected override void SetFieldType(IDataColumn field, string typeName)
        //{
        //    if (typeName == "enum")
        //    {
        //        // PostgreSQL中没有布尔型，这里处理YN枚举作为布尔型
        //        if (field.RawType == "enum('N','Y')" || field.RawType == "enum('Y','N')")
        //        {
        //            field.DataType = typeof(Boolean);
        //            // 处理默认值
        //            if (!String.IsNullOrEmpty(field.Default))
        //            {
        //                if (field.Default == "Y")
        //                    field.Default = "true";
        //                else if (field.Default == "N")
        //                    field.Default = "false";
        //            }
        //            return;
        //        }
        //    }

        //    base.SetFieldType(field, typeName);
        //}

        protected override string GetFieldType(IDataColumn field)
        {
            if (field.DataType == typeof(Boolean)) return "enum('N','Y')";

            return base.GetFieldType(field);
        }

        public override string FieldClause(IDataColumn field, bool onlyDefine)
        {
            String sql = base.FieldClause(field, onlyDefine);
            // 加上注释
            if (!String.IsNullOrEmpty(field.Description)) sql = String.Format("{0} COMMENT '{1}'", sql, field.Description);
            return sql;
        }

        protected override string GetFieldConstraints(IDataColumn field, Boolean onlyDefine)
        {
            String str = null;
            if (!field.Nullable) str = " NOT NULL";

            if (field.Identity) str = " NOT NULL AUTO_INCREMENT";

            return str;
        }

        protected override string GetFieldDefault(IDataColumn field, bool onlyDefine)
        {
            if (String.IsNullOrEmpty(field.Default)) return null;

            if (field.DataType == typeof(Boolean))
            {
                if (field.Default == "true")
                    return " Default 'Y'";
                else if (field.Default == "false")
                    return " Default 'N'";
            }
            //else if (field.DataType == typeof(DateTime))
            //{
            //    String d = CheckAndGetDefaultDateTimeNow(field.Table.DbType, field.Default);
            //    if (d == "now()") d = "CURRENT_TIMESTAMP";
            //    return String.Format(" Default {0}", d);
            //}

            return base.GetFieldDefault(field, onlyDefine);
        }

        //protected override void SetFieldType(IDataColumn field, string typeName)
        //{
        //    DataTable dt = DataTypes;
        //    if (dt == null) return;

        //    DataRow[] drs = FindDataType(field, typeName, null);
        //    if (drs == null || drs.Length < 1) return;

        //    // 修正原始类型
        //    String rawType = null;
        //    if (TryGetDataRowValue<String>(drs[0], "COLUMN_TYPE", out rawType)) field.RawType = rawType;

        //    base.SetFieldType(field, typeName);
        //}

        #region 架构定义
        public override object SetSchema(DDLSchema schema, params object[] values)
        {
            if (schema == DDLSchema.DatabaseExist)
            {
                IDbSession session = Database.CreateSession();

                DataTable dt = GetSchema("Databases", new String[] { values != null && values.Length > 0 ? (String)values[0] : session.DatabaseName });
                if (dt == null || dt.Rows == null || dt.Rows.Count < 1) return false;
                return true;
            }

            return base.SetSchema(schema, values);
        }

        //public override string CreateDatabaseSQL(string dbname, string file)
        //{
        //    return String.Format("Create Database Binary {0}", FormatKeyWord(dbname));
        //}

        public override string DropDatabaseSQL(string dbname)
        {
            return String.Format("Drop Database If Exists {0}", FormatKeyWord(dbname));
        }

        public override String CreateTableSQL(IDataTable table)
        {
            List<IDataColumn> Fields = new List<IDataColumn>(table.Columns);
            Fields.Sort(delegate(IDataColumn item1, IDataColumn item2) { return item1.ID.CompareTo(item2.ID); });

            StringBuilder sb = new StringBuilder();
            String key = null;

            sb.AppendFormat("Create Table If Not Exists {0}(", FormatKeyWord(table.Name));
            for (Int32 i = 0; i < Fields.Count; i++)
            {
                sb.AppendLine();
                sb.Append("\t");
                sb.Append(FieldClause(Fields[i], true));
                if (i < Fields.Count - 1) sb.Append(",");

                if (Fields[i].PrimaryKey) key = Fields[i].Name;
            }
            if (!String.IsNullOrEmpty(key))
            {
                sb.AppendLine(",");
                sb.AppendFormat("\tPrimary Key ({0})", FormatKeyWord(key));
            }
            sb.AppendLine();
            sb.Append(")");

            return sb.ToString();
        }

        public override string AddTableDescriptionSQL(IDataTable table)
        {
            if (String.IsNullOrEmpty(table.Description)) return null;

            return String.Format("Alter Table {0} Comment '{1}'", FormatKeyWord(table.Name), table.Description);
        }

        public override string AlterColumnSQL(IDataColumn field, IDataColumn oldfield)
        {
            return String.Format("Alter Table {0} Modify Column {1}", FormatKeyWord(field.Table.Name), FieldClause(field, false));
        }

        public override string AddColumnDescriptionSQL(IDataColumn field)
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