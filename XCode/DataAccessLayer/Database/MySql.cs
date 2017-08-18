﻿using System;
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
        public override DatabaseType Type
        {
            get { return DatabaseType.MySql; }
        }

        private static DbProviderFactory _dbProviderFactory;
        /// <summary>提供者工厂</summary>
        static DbProviderFactory dbProviderFactory
        {
            get
            {
                //if (_dbProviderFactory == null) _dbProviderFactory = DbProviderFactories.GetFactory("MySql.Data.MySqlClient");
                if (_dbProviderFactory == null)
                {
                    lock (typeof(MySql))
                    {
                        if (_dbProviderFactory == null) _dbProviderFactory = GetProviderFactory("MySql.Data.dll", "MySql.Data.MySqlClient.MySqlClientFactory");
                    }
                }

                return _dbProviderFactory;
            }
        }

        /// <summary>工厂</summary>
        public override DbProviderFactory Factory
        {
            get { return dbProviderFactory; }
        }

        const String Server_Key = "Server";
        const String CharSet = "CharSet";
        const String AllowZeroDatetime = "Allow Zero Datetime";
        protected override void OnSetConnectionString(XDbConnectionStringBuilder builder)
        {
            base.OnSetConnectionString(builder);

            if (builder.ContainsKey(Server_Key) && (builder[Server_Key] == "." || builder[Server_Key] == "localhost"))
            {
                //builder[Server_Key] = "127.0.0.1";
                builder[Server_Key] = IPAddress.Loopback.ToString();
            }
            if (!builder.ContainsKey(CharSet)) builder[CharSet] = "utf8";
            if (!builder.ContainsKey(AllowZeroDatetime)) builder[AllowZeroDatetime] = "True";
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
                if (maximumRows > 0) builder.Limit += String.Format(" limit {0}", maximumRows);
                return builder;
            }
            if (maximumRows < 1) throw new NotSupportedException("不支持取第几条数据之后的所有数据！");

            builder.Limit += String.Format(" limit {0}, {1}", startRowIndex, maximumRows);
            return builder;
        }
        #endregion

        #region 数据库特性
        /// <summary>当前时间函数</summary>
        public override String DateTimeNow
        {
            get
            {
                // MySql默认值不能用函数，所以不能用now()
                return null;
            }
        }

        /// <summary>获取Guid的函数</summary>
        public override String NewGuid { get { return "uuid()"; } }

        protected override String ReservedWordsStr
        {
            get
            {
                //return "ACCESSIBLE,ADD,ALL,ALTER,ANALYZE,AND,AS,ASC,ASENSITIVE,BEFORE,BETWEEN,BIGINT,BINARY,BLOB,BOTH,BY,CALL,CASCADE,CASE,CHANGE,CHAR,CHARACTER,CHECK,COLLATE,COLUMN,CONDITION,CONNECTION,CONSTRAINT,CONTINUE,CONTRIBUTORS,CONVERT,CREATE,CROSS,CURRENT_DATE,CURRENT_TIME,CURRENT_TIMESTAMP,CURRENT_USER,CURSOR,DATABASE,DATABASES,DAY_HOUR,DAY_MICROSECOND,DAY_MINUTE,DAY_SECOND,DEC,DECIMAL,DECLARE,DEFAULT,DELAYED,DELETE,DESC,DESCRIBE,DETERMINISTIC,DISTINCT,DISTINCTROW,DIV,DOUBLE,DROP,DUAL,EACH,ELSE,ELSEIF,ENCLOSED,ESCAPED,EXISTS,EXIT,EXPLAIN,FALSE,FETCH,FLOAT,FLOAT4,FLOAT8,FOR,FORCE,FOREIGN,FROM,FULLTEXT,GRANT,GROUP,HAVING,HIGH_PRIORITY,HOUR_MICROSECOND,HOUR_MINUTE,HOUR_SECOND,IF,IGNORE,IN,INDEX,INFILE,INNER,INOUT,INSENSITIVE,INSERT,INT,INT1,INT2,INT3,INT4,INT8,INTEGER,INTERVAL,INTO,IS,ITERATE,JOIN,KEY,KEYS,KILL,LEADING,LEAVE,LEFT,LIKE,LIMIT,LINEAR,LINES,LOAD,LOCALTIME,LOCALTIMESTAMP,LOCK,LONG,LONGBLOB,LONGTEXT,LOOP,LOW_PRIORITY,MATCH,MEDIUMBLOB,MEDIUMINT,MEDIUMTEXT,MIDDLEINT,MINUTE_MICROSECOND,MINUTE_SECOND,MOD,MODIFIES,NATURAL,NOT,NO_WRITE_TO_BINLOG,NULL,NUMERIC,ON,OPTIMIZE,OPTION,OPTIONALLY,OR,ORDER,OUT,OUTER,OUTFILE,PRECISION,PRIMARY,PROCEDURE,PURGE,RANGE,READ,READS,READ_ONLY,READ_WRITE,REAL,REFERENCES,REGEXP,RELEASE,RENAME,REPEAT,REPLACE,REQUIRE,RESTRICT,RETURN,REVOKE,RIGHT,RLIKE,SCHEMA,SCHEMAS,SECOND_MICROSECOND,SELECT,SENSITIVE,SEPARATOR,SET,SHOW,SMALLINT,SPATIAL,SPECIFIC,SQL,SQLEXCEPTION,SQLSTATE,SQLWARNING,SQL_BIG_RESULT,SQL_CALC_FOUND_ROWS,SQL_SMALL_RESULT,SSL,STARTING,STRAIGHT_JOIN,TABLE,TERMINATED,THEN,TINYBLOB,TINYINT,TINYTEXT,TO,TRAILING,TRIGGER,TRUE,UNDO,UNION,UNIQUE,UNLOCK,UNSIGNED,UPDATE,UPGRADE,USAGE,USE,USING,UTC_DATE,UTC_TIME,UTC_TIMESTAMP,VALUES,VARBINARY,VARCHAR,VARCHARACTER,VARYING,WHEN,WHERE,WHILE,WITH,WRITE,X509,XOR,YEAR_MONTH,ZEROFILL";
                return "LOG";
            }
        }

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
        public override Int32 LongTextLength { get { return 255; } }

        internal protected override String ParamPrefix { get { return "?"; } }

        /// <summary>创建参数</summary>
        /// <param name="name">名称</param>
        /// <param name="value">值</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public override IDataParameter CreateParameter(String name, Object value, Type type = null)
        {
            var dp = base.CreateParameter(name, value, type);

            if (type == null) type = value?.GetType();
            if (type == typeof(Boolean))
            {
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

            //var n = 0L;
            //if (QueryIndex().TryGetValue(tableName, out n)) return n;

            var db = DatabaseName;
            var sql = String.Format("select table_rows from information_schema.tables where table_schema='{1}' and table_name='{0}'", tableName, db);
            return ExecuteScalar<Int64>(sql);
        }

        //Dictionary<String, Int64> _index;
        //DateTime _next;

        //Dictionary<String, Int64> QueryIndex()
        //{
        //    // 检查更新
        //    if (_index == null || _next < DateTime.Now)
        //    {
        //        _index = QueryIndex_();
        //        _next = DateTime.Now.AddSeconds(10);
        //    }

        //    return _index;
        //}

        //Dictionary<String, Int64> QueryIndex_()
        //{
        //    var db = DatabaseName;
        //    var ds = Query("select table_name,table_rows from information_schema.tables where table_schema='{0}'".F(db));
        //    var dic = new Dictionary<String, Int64>(StringComparer.OrdinalIgnoreCase);
        //    foreach (DataRow dr in ds.Tables[0].Rows)
        //    {
        //        dic.Add(dr[0] + "", Convert.ToInt64(dr[1]));
        //    }
        //    return dic;
        //}
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
            return ExecuteScalar<Int64>(CreateCommand(sql, type, ps));
        }
        #endregion
    }

    /// <summary>MySql元数据</summary>
    class MySqlMetaData : RemoteDbMetaData
    {
        protected override void FixTable(IDataTable table, DataRow dr)
        {
            // 注释
            String comment = null;
            if (TryGetDataRowValue(dr, "TABLE_COMMENT", out comment)) table.Description = comment;

            base.FixTable(table, dr);
        }

        protected override Boolean IsColumnChanged(IDataColumn entityColumn, IDataColumn dbColumn, IDatabase entityDb)
        {
            return base.IsColumnChanged(entityColumn, dbColumn, entityDb);
        }

        protected override void FixField(IDataColumn field, DataRow dr)
        {
            // 修正原始类型
            String rawType = null;
            if (TryGetDataRowValue(dr, "COLUMN_TYPE", out rawType)) field.RawType = rawType;

            // 修正自增字段
            String extra = null;
            if (TryGetDataRowValue(dr, "EXTRA", out extra) && extra == "auto_increment") field.Identity = true;

            // 修正主键
            String key = null;
            if (TryGetDataRowValue(dr, "COLUMN_KEY", out key)) field.PrimaryKey = key == "PRI";

            // 注释
            String comment = null;
            if (TryGetDataRowValue(dr, "COLUMN_COMMENT", out comment)) field.Description = comment;

            // 布尔类型
            // MySql中没有布尔型，这里处理YN枚举作为布尔型
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
            base.FixField(field, dr);
        }

        protected override DataRow[] FindDataType(IDataColumn field, String typeName, Boolean? isLong)
        {
            // MySql没有ntext，映射到text
            if (typeName.EqualIgnoreCase("ntext")) typeName = "text";
            //2017-08-19 siery修改 MySql没有money，映射到decimal
            if (typeName.EqualIgnoreCase("money")) typeName = "decimal";
            //field.Table.DbType == DatabaseType.MySql
            if (field.DataType == typeof(String) && field.Length == -1)
            {
                //这里修正为longtext
                return this.DataTypes.Select("TypeName='LONGTEXT'");
            }
            var dbType = field.Table.DbType;
            if (typeName == "text" && (dbType == DatabaseType.SqlServer || dbType == DatabaseType.SQLite))
            {
                //SQL Server 中的text容量要远远大于MySQL中的Text，所以要改为LongText。
                return this.DataTypes.Select("TypeName='LONGTEXT'");
            }
            //if (dbType == DatabaseType.SqlServer || dbType == DatabaseType.SQLite)
            //{

            //}

            // MySql的默认值不能使用函数，所以无法设置当前时间作为默认值，但是第一个Timestamp类型字段会有当前时间作为默认值效果
            //2016年3月1日 去掉这个特性，因为： 
            // timestamp 类型的列还有个特性：默认情况下，在 insert, update 数据时，timestamp 列会自动以当前时间（CURRENT_TIMESTAMP）填充/更新。“自动”的意思就是，你不去管它，MySQL 会替你去处理。 
            //意思就是说，如果执行Update操作的话，就算不涉及这个字段，这个字段的值还是会改变的。
            //如果此字段用作创建时间，就悲剧了。
            //if (typeName.EqualIgnoreCase("datetime"))
            //{
            //    String d = field.Default; ;
            //    if (CheckAndGetDefault(field, ref d) && String.IsNullOrEmpty(d)) typeName = "timestamp";
            //}

            DataRow[] drs = base.FindDataType(field, typeName, isLong);
            if (drs != null && drs.Length > 0)
            {
                // 无符号/有符号
                if (!String.IsNullOrEmpty(field.RawType))
                {
                    if (!typeName.Contains("char") && !typeName.Contains("String"))
                    {
                        Boolean IsUnsigned = field.RawType.ToLower().Contains("unsigned");

                        foreach (DataRow dr in drs)
                        {
                            var format = GetDataRowValue<String>(dr, "CreateFormat");
                            if (!format.IsNullOrEmpty())
                            {
                                if (IsUnsigned && format.ToLower().Contains("unsigned"))
                                    return new DataRow[] { dr };
                                else if (!IsUnsigned && !format.ToLower().Contains("unsigned"))
                                    return new DataRow[] { dr };
                            }
                        }
                    }
                }

                // 字符串
                //2016-02-23 @宁波-小董 同步数据库架构到Oracle，报错，CHAR长度1000，要改用text
                //if (typeName == typeof(String).FullName || typeName.EqualIgnoreCase("varchar") || typeName.Contains("char"))
                //{
                //    foreach (DataRow dr in drs)
                //    {
                //        String name = GetDataRowValue<String>(dr, "TypeName");
                //        if ((name == "CHAR" && field.IsUnicode || name == "NVARCHAR" &&
                //            field.IsUnicode || name == "VARCHAR" && !field.IsUnicode) && field.Length >= Database.LongTextLength)
                //        {
                //            dr["TypeName"] = "text";
                //            return new DataRow[] { dr };
                //        }
                //        else if (name == "LONGTEXT" && field.Length > Database.LongTextLength)
                //            return new DataRow[] { dr };
                //    }
                //}

                // 时间日期
                //2016年3月1日 去掉这个特性，因为： 
                // timestamp 类型的列还有个特性：默认情况下，在 insert, update 数据时，timestamp 列会自动以当前时间（CURRENT_TIMESTAMP）填充/更新。“自动”的意思就是，你不去管它，MySQL 会替你去处理。 
                //意思就是说，如果执行Update操作的话，就算不涉及这个字段，这个字段的值还是会改变的。
                //如果此字段用作创建时间，就悲剧了。
                //if (typeName == typeof(DateTime).FullName || typeName.EqualIgnoreCase("DateTime"))
                //{
                //    // DateTime的范围是0001到9999
                //    // Timestamp的范围是1970到2038
                //    // MySql的默认值不能使用函数，所以无法设置当前时间作为默认值，但是第一个Timestamp类型字段会有当前时间作为默认值效果
                //    String d = field.Default; ;
                //    CheckAndGetDefault(field, ref d);
                //    //String d = CheckAndGetDefault(field, field.Default);
                //    foreach (DataRow dr in drs)
                //    {
                //        String name = GetDataRowValue<String>(dr, "TypeName");
                //        if (name == "DATETIME" && String.IsNullOrEmpty(field.Default))
                //            return new DataRow[] { dr };
                //        else if (name == "TIMESTAMP" && String.IsNullOrEmpty(d))
                //            return new DataRow[] { dr };
                //    }
                //}
            }
            return drs;
        }

        protected override String GetFieldType(IDataColumn field)
        {
            if (field.DataType == typeof(Boolean)) return "enum('N','Y')";

            return base.GetFieldType(field);
        }

        public override String FieldClause(IDataColumn field, Boolean onlyDefine)
        {
            String sql = base.FieldClause(field, onlyDefine);
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

        protected override String GetFieldDefault(IDataColumn field, Boolean onlyDefine)
        {
            if (String.IsNullOrEmpty(field.Default)) return null;

            if (field.DataType == typeof(Boolean))
            {
                if (field.Default == "true")
                    return " Default 'Y'";
                else if (field.Default == "false")
                    return " Default 'N'";
            }
            else if (field.DataType == typeof(String))
            {
                // 大文本不能有默认值
                if (field.Length <= 0 || field.Length >= Database.LongTextLength) return null;
            }
            //else if (field.DataType == typeof(DateTime))
            //{
            //    String d = CheckAndGetDefaultDateTimeNow(field.Table.DbType, field.Default);
            //    if (d == "now()") d = "CURRENT_TIMESTAMP";
            //    return String.Format(" Default {0}", d);
            //}

            return base.GetFieldDefault(field, onlyDefine);
        }

        //protected override void FixIndex(IDataIndex index, DataRow dr)
        //{
        //    base.FixIndex(index, dr);

        //    Boolean b;
        //    if (TryGetDataRowValue<Boolean>(dr, "UNIQUE", out b)) index.Unique = b;
        //    if (TryGetDataRowValue<Boolean>(dr, "PRIMARY", out b)) index.PrimaryKey = b;
        //}

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

            // var session = Database.CreateSession(); //这里的Session被创建了可是没有使用，先注释掉，看看是否会有问题。
            var dt = GetSchema(_.Databases, new String[] { databaseName });
            return dt != null && dt.Rows != null && dt.Rows.Count > 0;
        }

        //public override string CreateDatabaseSQL(string dbname, string file)
        //{
        //    return String.Format("Create Database Binary {0}", FormatKeyWord(dbname));
        //}

        public override String DropDatabaseSQL(String dbname)
        {
            return String.Format("Drop Database If Exists {0}", FormatName(dbname));
        }

        public override String CreateTableSQL(IDataTable table)
        {
            var Fields = new List<IDataColumn>(table.Columns);
            //Fields.Sort(delegate(IDataColumn item1, IDataColumn item2) { return item1.ID.CompareTo(item2.ID); });
            Fields.OrderBy(dc => dc.ID);

            var sb = new StringBuilder(32 + Fields.Count * 20);
            var pks = new List<String>();

            sb.AppendFormat("Create Table If Not Exists {0}(", FormatName(table.TableName));
            for (var i = 0; i < Fields.Count; i++)
            {
                sb.AppendLine();
                sb.Append("\t");
                sb.Append(FieldClause(Fields[i], true));
                if (i < Fields.Count - 1) sb.Append(",");

                if (Fields[i].PrimaryKey) pks.Add(FormatName(Fields[i].ColumnName));
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

        #region 辅助函数
        protected override String GetFormatParam(IDataColumn field, DataRow dr)
        {
            String str = base.GetFormatParam(field, dr);
            if (String.IsNullOrEmpty(str)) return str;

            if (str == "(-1)" && field.DataType == typeof(String))
            {
                return String.Format("({0})", Database.LongTextLength);
            }
            if (field.DataType == typeof(Guid)) return "(36)";

            return str;
        }
        #endregion
    }
}