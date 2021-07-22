﻿using System;
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

        const String Server_Key = "Server";
        protected override void OnSetConnectionString(ConnectionStringBuilder builder)
        {
            base.OnSetConnectionString(builder);

            var key = builder[Server_Key];
            if (key.EqualIgnoreCase(".", "localhost"))
            {
                //builder[Server_Key] = "127.0.0.1";
                builder[Server_Key] = IPAddress.Loopback.ToString();
            }

            if (builder.TryGetValue("Database", out var db) && db != db.ToLower()) builder["Database"] = db.ToLower();
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
        protected override String ReservedWordsStr
        {
            get
            {
                return "ACCESSIBLE,ADD,ALL,ALTER,ANALYZE,AND,AS,ASC,ASENSITIVE,BEFORE,BETWEEN,BIGINT,BINARY,BLOB,BOTH,BY,CALL,CASCADE,CASE,CHANGE,CHAR,CHARACTER,CHECK,COLLATE,COLUMN,CONDITION,CONNECTION,CONSTRAINT,CONTINUE,CONTRIBUTORS,CONVERT,CREATE,CROSS,CURRENT_DATE,CURRENT_TIME,CURRENT_TIMESTAMP,CURRENT_USER,CURSOR,DATABASE,DATABASES,DAY_HOUR,DAY_MICROSECOND,DAY_MINUTE,DAY_SECOND,DEC,DECIMAL,DECLARE,DEFAULT,DELAYED,DELETE,DESC,DESCRIBE,DETERMINISTIC,DISTINCT,DISTINCTROW,DIV,DOUBLE,DROP,DUAL,EACH,ELSE,ELSEIF,ENCLOSED,ESCAPED,EXISTS,EXIT,EXPLAIN,FALSE,FETCH,FLOAT,FLOAT4,FLOAT8,FOR,FORCE,FOREIGN,FROM,FULLTEXT,GRANT,GROUP,HAVING,HIGH_PRIORITY,HOUR_MICROSECOND,HOUR_MINUTE,HOUR_SECOND,IF,IGNORE,IN,INDEX,INFILE,INNER,INOUT,INSENSITIVE,INSERT,INT,INT1,INT2,INT3,INT4,INT8,INTEGER,INTERVAL,INTO,IS,ITERATE,JOIN,KEY,KEYS,KILL,LEADING,LEAVE,LEFT,LIKE,LIMIT,LINEAR,LINES,LOAD,LOCALTIME,LOCALTIMESTAMP,LOCK,LONG,LONGBLOB,LONGTEXT,LOOP,LOW_PRIORITY,MATCH,MEDIUMBLOB,MEDIUMINT,MEDIUMTEXT,MIDDLEINT,MINUTE_MICROSECOND,MINUTE_SECOND,MOD,MODIFIES,NATURAL,NOT,NO_WRITE_TO_BINLOG,NULL,NUMERIC,ON,OPTIMIZE,OPTION,OPTIONALLY,OR,ORDER,OUT,OUTER,OUTFILE,PRECISION,PRIMARY,PROCEDURE,PURGE,RANGE,READ,READS,READ_ONLY,READ_WRITE,REAL,REFERENCES,REGEXP,RELEASE,RENAME,REPEAT,REPLACE,REQUIRE,RESTRICT,RETURN,REVOKE,RIGHT,RLIKE,SCHEMA,SCHEMAS,SECOND_MICROSECOND,SELECT,SENSITIVE,SEPARATOR,SET,SHOW,SMALLINT,SPATIAL,SPECIFIC,SQL,SQLEXCEPTION,SQLSTATE,SQLWARNING,SQL_BIG_RESULT,SQL_CALC_FOUND_ROWS,SQL_SMALL_RESULT,SSL,STARTING,STRAIGHT_JOIN,TABLE,TERMINATED,THEN,TINYBLOB,TINYINT,TINYTEXT,TO,TRAILING,TRIGGER,TRUE,UNDO,UNION,UNIQUE,UNLOCK,UNSIGNED,UPDATE,UPGRADE,USAGE,USE,USING,UTC_DATE,UTC_TIME,UTC_TIMESTAMP,VALUES,VARBINARY,VARCHAR,VARCHARACTER,VARYING,WHEN,WHERE,WHILE,WITH,WRITE,X509,XOR,YEAR_MONTH,ZEROFILL," +
                    "LOG,User,Role,Admin,Rank,Member";
            }
        }

        /// <summary>格式化关键字</summary>
        /// <param name="keyWord">关键字</param>
        /// <returns></returns>
        public override String FormatKeyWord(String keyWord)
        {
            //if (String.IsNullOrEmpty(keyWord)) throw new ArgumentNullException("keyWord");
            if (String.IsNullOrEmpty(keyWord)) return keyWord;

            if (keyWord.StartsWith("\"") && keyWord.EndsWith("\"")) return keyWord;

            return $"\"{keyWord}\"";
        }

        /// <summary>格式化数据为SQL数据</summary>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public override String FormatValue(IDataColumn field, Object value)
        {
            if (field.DataType == typeof(String))
            {
                if (value == null) return field.Nullable ? "null" : "''";
                //云飞扬：这里注释掉，应该返回``而不是null字符
                //if (String.IsNullOrEmpty(value.ToString()) && field.Nullable) return "null";
                return "'" + value + "'";
            }
            else if (field.DataType == typeof(Boolean))
            {
                return (Boolean)value ? "true" : "false";
            }

            return base.FormatValue(field, value);
        }

        /// <summary>长文本长度</summary>
        public override Int32 LongTextLength => 4000;

        protected internal override String ParamPrefix => "$";

        /// <summary>系统数据库名</summary>
        public override String SystemDatabaseName => "postgres";

        /// <summary>字符串相加</summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public override String StringConcat(String left, String right) => (!String.IsNullOrEmpty(left) ? left : "''") + "||" + (!String.IsNullOrEmpty(right) ? right : "''");
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
            sql += " RETURNING id";
            return base.InsertAndGetIdentity(sql, type, ps);
        }

#if !NET40
        public override Task<Int64> InsertAndGetIdentityAsync(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            sql += " RETURNING id";
            return base.InsertAndGetIdentityAsync(sql, type, ps);
        }
#endif
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
            //if (columns == null) columns = table.Columns.ToArray();
            sb.AppendFormat("{0} {1}(", action, db.FormatName(table));
            foreach (var dc in columns)
            {
                // 取消对主键的过滤，避免列名和值无法一一对应的问题
                //if (dc.Identity) continue;

                sb.Append(db.FormatName(dc));
                sb.Append(',');
            }
            sb.Length--;
            sb.Append(')');

            // 值列表
            sb.Append(" Values");

            // 优化支持DbTable
            if (list.FirstOrDefault() is DbRow)
            {
                // 提前把列名转为索引，然后根据索引找数据
                DbTable dt = null;
                Int32[] ids = null;
                foreach (DbRow dr in list)
                {
                    if (dr.Table != dt)
                    {
                        dt = dr.Table;
                        var cs = new List<Int32>();
                        foreach (var dc in columns)
                        {
                            if (dc.Identity)
                                cs.Add(0);
                            else
                                cs.Add(dt.GetColumn(dc.ColumnName));
                        }
                        ids = cs.ToArray();
                    }

                    sb.Append('(');
                    var row = dt.Rows[dr.Index];
                    for (var i = 0; i < columns.Length; i++)
                    {
                        var dc = columns[i];
                        //if (dc.Identity) continue;

                        var value = row[ids[i]];
                        sb.Append(db.FormatValue(dc, value));
                        sb.Append(',');
                    }
                    sb.Length--;
                    sb.Append("),");
                }
            }
            else
            {
                foreach (var entity in list)
                {
                    sb.Append('(');
                    foreach (var dc in columns)
                    {
                        //if (dc.Identity) continue;

                        var value = entity[dc.Name];
                        sb.Append(db.FormatValue(dc, value));
                        sb.Append(',');
                    }
                    sb.Length--;
                    sb.Append("),");
                }
            }
            sb.Length--;

            // 重复键执行update
            if ((updateColumns != null && updateColumns.Count > 0) || (addColumns != null && addColumns.Count > 0))
            {
                sb.Append(" On Duplicate Key Update ");
                if (updateColumns != null && updateColumns.Count > 0)
                {
                    foreach (var dc in columns)
                    {
                        if (dc.Identity || dc.PrimaryKey) continue;

                        if (updateColumns.Contains(dc.Name) && (addColumns == null || !addColumns.Contains(dc.Name)))
                            sb.AppendFormat("{0}=Values({0}),", db.FormatName(dc));
                    }
                    sb.Length--;
                }
                if (addColumns != null && addColumns.Count > 0)
                {
                    sb.Append(',');
                    foreach (var dc in columns)
                    {
                        if (dc.Identity || dc.PrimaryKey) continue;

                        if (addColumns.Contains(dc.Name))
                            sb.AppendFormat("{0}={0}+Values({0}),", db.FormatName(dc));
                    }
                    sb.Length--;
                }
            }

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
    }

    /// <summary>PostgreSQL元数据</summary>
    internal class PostgreSQLMetaData : RemoteDbMetaData
    {
        public PostgreSQLMetaData() => Types = _DataTypes;

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
            { typeof(Byte[]), new String[] { "bytea" } },
            { typeof(Boolean), new String[] { "boolean" } },
            { typeof(Int16), new String[] { "smallint" } },
            { typeof(Int32), new String[] { "integer" } },
            { typeof(Int64), new String[] { "bigint" } },
            { typeof(Single), new String[] { "float" } },
            { typeof(Double), new String[] { "float8", "double precision" } },
            { typeof(Decimal), new String[] { "decimal" } },
            { typeof(DateTime), new String[] { "timestamp", "timestamp without time zone", "date" } },
            { typeof(String), new String[] { "varchar({0})", "character varying", "text" } },
        };
        #endregion

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

        public override String FieldClause(IDataColumn field, Boolean onlyDefine)
        {
            if (field.Identity) return $"{field.Name} serial NOT NULL";

            var sql = base.FieldClause(field, onlyDefine);

            //// 加上注释
            //if (!String.IsNullOrEmpty(field.Description)) sql = $"{sql} COMMENT '{field.Description}'";

            return sql;
        }

        protected override String GetFieldConstraints(IDataColumn field, Boolean onlyDefine)
        {
            String str = null;
            if (!field.Nullable) str = " NOT NULL";

            if (field.Identity) str = " serial NOT NULL";

            return str;
        }

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
            var dt = GetSchema(_.Databases, new String[] { databaseName.ToLower() });
            return dt != null && dt.Rows != null && dt.Rows.Count > 0;
        }

        //public override string CreateDatabaseSQL(string dbname, string file)
        //{
        //    return String.Format("Create Database Binary {0}", FormatKeyWord(dbname));
        //}

        public override String DropDatabaseSQL(String dbname) => $"Drop Database If Exists {Database.FormatName(dbname)}";

        public override String CreateTableSQL(IDataTable table)
        {
            var fs = new List<IDataColumn>(table.Columns);

            var sb = new StringBuilder(32 + fs.Count * 20);

            sb.AppendFormat("Create Table {0}(", FormatName(table));
            for (var i = 0; i < fs.Count; i++)
            {
                sb.AppendLine();
                sb.Append('\t');
                sb.Append(FieldClause(fs[i], true));
                if (i < fs.Count - 1) sb.Append(',');
            }
            if (table.PrimaryKeys.Length > 0) sb.AppendFormat(",\r\n\tPrimary Key ({0})", table.PrimaryKeys.Join(",", FormatName));
            sb.AppendLine();
            sb.Append(')');

            return sb.ToString();
        }

        public override String AddTableDescriptionSQL(IDataTable table) => $"Comment On Table {FormatName(table)} is '{table.Description}'";

        public override String DropTableDescriptionSQL(IDataTable table) => $"Comment On Table {FormatName(table)} is ''";

        public override String AddColumnDescriptionSQL(IDataColumn field) => $"Comment On Column {FormatName(field.Table)}.{FormatName(field)} is '{field.Description}'";

        public override String DropColumnDescriptionSQL(IDataColumn field) => $"Comment On Column {FormatName(field.Table)}.{FormatName(field)} is ''";
        #endregion

        #region 辅助函数

        #endregion
    }
}