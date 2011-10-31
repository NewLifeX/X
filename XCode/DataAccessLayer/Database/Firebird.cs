using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text.RegularExpressions;
using NewLife.Reflection;
using XCode.Exceptions;

namespace XCode.DataAccessLayer
{
    class Firebird : FileDbBase
    {
        #region 属性
        /// <summary>
        /// 返回数据库类型。
        /// </summary>
        public override DatabaseType DbType
        {
            get { return DatabaseType.Firebird; }
        }

        private static DbProviderFactory _dbProviderFactory;
        /// <summary>
        /// 提供者工厂
        /// </summary>
        static DbProviderFactory dbProviderFactory
        {
            get
            {
                //if (_dbProviderFactory == null) _dbProviderFactory = DbProviderFactories.GetFactory("FirebirdSql.Data.FirebirdClient");
                if (_dbProviderFactory == null)
                {
                    lock (typeof(Firebird))
                    {
                        if (_dbProviderFactory == null) _dbProviderFactory = GetProviderFactory("FirebirdSql.Data.FirebirdClient.dll", "FirebirdSql.Data.FirebirdClient.FirebirdClientFactory");
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

        protected override void OnSetConnectionString(XDbConnectionStringBuilder builder)
        {
            base.OnSetConnectionString(builder);

            String file;
            if (!builder.TryGetValue("Database", out file)) return;

            file = ResolveFile(file);
            builder["Database"] = file;
            FileName = file;
        }
        #endregion

        #region 方法
        /// <summary>
        /// 创建数据库会话
        /// </summary>
        /// <returns></returns>
        protected override IDbSession OnCreateSession()
        {
            return new FirebirdSession();
        }

        /// <summary>
        /// 创建元数据对象
        /// </summary>
        /// <returns></returns>
        protected override IMetaData OnCreateMetaData()
        {
            return new FirebirdMetaData();
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
                    return String.Format("{0} rows 1 to {1}", sql, maximumRows);
            }
            if (maximumRows < 1)
                throw new NotSupportedException("不支持取第几条数据之后的所有数据！");
            else
                sql = String.Format("{0} rows {1} to {2}", sql, startRowIndex + 1, maximumRows);
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
        //        return "ACTION,ACTIVE,ADD,ADMIN,AFTER,ALL,ALTER,AND,ANY,AS,ASC,ASCENDING,AT,AUTO,AVG,BASE_NAME,BEFORE,BEGIN,BETWEEN,BIGINT,BLOB,BREAK,BY,CACHE,CASCADE,CASE,CAST,CHAR,CHARACTER,CHECK,CHECK_POINT_LENGTH,COALESCE,COLLATE,COLUMN,COMMIT,COMMITTED,COMPUTED,CONDITIONAL,CONNECTION_ID,CONSTRAINT,CONTAINING,COUNT,CREATE,CSTRING,CURRENT,CURRENT_DATE,CURRENT_ROLE,CURRENT_TIME,CURRENT_TIMESTAMP,CURRENT_USER,CURSOR,DATABASE,DATE,DAY,DEBUG,DEC,DECIMAL,DECLARE,DEFAULT,DELETE,DESC,DESCENDING,DESCRIPTOR,DISTINCT,DO,DOMAIN,DOUBLE,DROP,ELSE,END,ENTRY_POINT,ESCAPE,EXCEPTION,EXECUTE,EXISTS,EXIT,EXTERNAL,EXTRACT,FILE,FILTER,FIRST,FLOAT,FOR,FOREIGN,FREE_IT,FROM,FULL,FUNCTION,GDSCODE,GENERATOR,GEN_ID,GRANT,GROUP,GROUP_COMMIT_WAIT_TIME,HAVING,HOUR,IF,IN,INACTIVE,INDEX,INNER,INPUT_TYPE,INSERT,INT,INTEGER,INTO,IS,ISOLATION,JOIN,KEY,LAST,LEFT,LENGTH,LEVEL,LIKE,LOGFILE,LOG_BUFFER_SIZE,LONG,MANUAL,MAX,MAXIMUM_SEGMENT,MERGE,MESSAGE,MIN,MINUTE,MODULE_NAME,MONTH,NAMES,NATIONAL,NATURAL,NCHAR,NO,NOT,NULLIF,NULL,NULLS,LOCK,NUMERIC,NUM_LOG_BUFFERS,OF,ON,ONLY,OPTION,OR,ORDER,OUTER,OUTPUT_TYPE,OVERFLOW,PAGE,PAGES,PAGE_SIZE,PARAMETER,PASSWORD,PLAN,POSITION,POST_EVENT,PRECISION,PRIMARY,PRIVILEGES,PROCEDURE,PROTECTED,RAW_PARTITIONS,RDB$DB_KEY,READ,REAL,RECORD_VERSION,RECREATE,REFERENCES,RESERV,RESERVING,RESTRICT,RETAIN,RETURNING_VALUES,RETURNS,REVOKE,RIGHT,ROLE,ROLLBACK,ROWS_AFFECTED,SAVEPOINT,SCHEMA,SECOND,SEGMENT,SELECT,SET,SHADOW,SHARED,SINGULAR,SIZE,SKIP,SMALLINT,SNAPSHOT,SOME,SORT,SQLCODE,STABILITY,STARTING,STARTS,STATISTICS,SUBSTRING,SUB_TYPE,SUM,SUSPEND,TABLE,THEN,TIME,TIMESTAMP,TO,TRANSACTION,TRANSACTION_ID,TRIGGER,TYPE,UNCOMMITTED,UNION,UNIQUE,UPDATE,UPPER,USER,USING,VALUE,VALUES,VARCHAR,VARIABLE,VARYING,VIEW,WAIT,WEEKDAY,WHEN,WHERE,WHILE,WITH,WORK,WRITE,YEAR,YEARDAY";
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

            if (keyWord.StartsWith("\"") && keyWord.EndsWith("\"")) return keyWord;

            return String.Format("\"{0}\"", keyWord);
        }

        ///// <summary>
        ///// 格式化数据为SQL数据
        ///// </summary>
        ///// <param name="field"></param>
        ///// <param name="value"></param>
        ///// <returns></returns>
        //public override string FormatValue(IDataColumn field, object value)
        //{
        //    if (field.DataType == typeof(String))
        //    {
        //        if (value == null) return field.Nullable ? "null" : "``";
        //        if (String.IsNullOrEmpty(value.ToString()) && field.Nullable) return "null";
        //        return "`" + value + "`";
        //    }
        //    else if (field.DataType == typeof(Boolean))
        //    {
        //        return (Boolean)value ? "'Y'" : "'N'";
        //    }

        //    return base.FormatValue(field, value);
        //}

        /// <summary>
        /// 长文本长度
        /// </summary>
        public override int LongTextLength { get { return 32767; } }

        /// <summary>
        /// 格式化标识列，返回插入数据时所用的表达式，如果字段本身支持自增，则返回空
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override string FormatIdentity(IDataColumn field, Object value)
        {
            return String.Format("GEN_ID(GEN_{0}, 1)", field.Table.Name);
        }

        ///// <summary>系统数据库名</summary>
        //public override String SystemDatabaseName { get { return "Firebird"; } }
        #endregion
    }

    /// <summary>
    /// Firebird数据库
    /// </summary>
    internal class FirebirdSession : FileDbSession
    {
        #region 基本方法 查询/执行
        static Regex reg_SEQ = new Regex(@"\bGEN_ID\((\w+)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /// <summary>
        /// 执行插入语句并返回新增行的自动编号
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>新增行的自动编号</returns>
        public override Int64 InsertAndGetIdentity(string sql)
        {
            Boolean b = IsAutoClose;
            // 禁用自动关闭，保证两次在同一会话
            IsAutoClose = false;

            BeginTransaction();
            try
            {
                Int64 rs = base.InsertAndGetIdentity(sql);
                if (rs > 0)
                {
                    Match m = reg_SEQ.Match(sql);
                    if (m != null && m.Groups != null && m.Groups.Count > 0)
                        rs = ExecuteScalar<Int64>(String.Format("Select {0}.currval", m.Groups[1].Value));
                }
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

    /// <summary>
    /// Firebird元数据
    /// </summary>
    class FirebirdMetaData : FileDbMetaData
    {
        /// <summary>
        /// 取得所有表构架
        /// </summary>
        /// <returns></returns>
        protected override List<IDataTable> OnGetTables()
        {
            try
            {
                //- 不要空，否则会死得很惨，列表所有数据表，实在太多了
                //if (String.Equals(user, "system")) user = null;

                DataTable dt = GetSchema(_.Tables, new String[] { null, null, null, "TABLE" });

                // 默认列出所有字段
                DataRow[] rows = new DataRow[dt.Rows.Count];
                dt.Rows.CopyTo(rows, 0);
                return GetTables(rows);

            }
            catch (DbException ex)
            {
                throw new XDbMetaDataException(this, "取得所有表构架出错！", ex);
            }
        }

        protected override string GetFieldType(IDataColumn field)
        {
            if (field.DataType == typeof(Boolean)) return "smallint";

            return base.GetFieldType(field);
        }

        #region 架构定义
        protected override void CreateDatabase()
        {
            //base.CreateDatabase();

            if (String.IsNullOrEmpty(FileName) || File.Exists(FileName)) return;

            //The miminum you must specify:

            //Hashtable parameters = new Hashtable();
            //parameters.Add("User", "SYSDBA");
            //parameters.Add("Password", "masterkey");
            //parameters.Add("Database", @"c:\database.fdb");
            //FbConnection.CreateDatabase(parameters);

            DbConnection conn = Database.Factory.CreateConnection();
            MethodInfoX method = MethodInfoX.Create(conn.GetType(), "CreateDatabase", new Type[] { typeof(String) });
            if (method == null) return;

            method.Invoke(null, Database.ConnectionString);
        }

        public override string CreateDatabaseSQL(string dbname, string file)
        {
            return String.Empty;
        }

        //public override string DropDatabaseSQL(string dbname)
        //{
        //    return String.Format("Drop Database If Exists {0}", FormatKeyWord(dbname));
        //}

        protected override string GetFieldConstraints(IDataColumn field, bool onlyDefine)
        {
            if (field.Nullable)
                return "";
            else
                return " not null";
        }

        public override string CreateTableSQL(IDataTable table)
        {
            String sql = base.CreateTableSQL(table);
            if (String.IsNullOrEmpty(sql)) return sql;

            String sqlSeq = String.Format("Create GENERATOR GEN_{0}", table.Name);
            return sql + "; " + Environment.NewLine + sqlSeq;
        }

        public override string DropTableSQL(String tableName)
        {
            String sql = base.DropTableSQL(tableName);
            if (String.IsNullOrEmpty(sql)) return sql;

            String sqlSeq = String.Format("Drop GENERATOR GEN_{0}", tableName);
            return sql + "; " + Environment.NewLine + sqlSeq;
        }

        //public override string AddTableDescriptionSQL(IDataTable table)
        //{
        //    if (String.IsNullOrEmpty(table.Description)) return null;

        //    return String.Format("Alter Table {0} Comment '{1}'", FormatKeyWord(table.Name), table.Description);
        //}

        //public override string AlterColumnSQL(IDataColumn field)
        //{
        //    return String.Format("Alter Table {0} Modify Column {1}", FormatKeyWord(field.Table.Name), FieldClause(field, false));
        //}

        //public override string AddColumnDescriptionSQL(IDataColumn field)
        //{
        //    // 返回String.Empty表示已经在别的SQL中处理
        //    return String.Empty;

        //    //if (String.IsNullOrEmpty(field.Description)) return null;

        //    //return String.Format("Alter Table {0} Modify {1} Comment '{2}'", FormatKeyWord(field.Table.Name), FormatKeyWord(field.Name), field.Description);
        //}
        #endregion
    }
}