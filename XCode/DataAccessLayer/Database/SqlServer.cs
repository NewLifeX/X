using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NewLife;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Reflection;

namespace XCode.DataAccessLayer
{
    class SqlServer : RemoteDb
    {
        #region 属性
        /// <summary>返回数据库类型。外部DAL数据库类请使用Other</summary>
        public override DatabaseType Type => DatabaseType.SqlServer;

        private static DbProviderFactory _Factory;
        /// <summary>工厂</summary>
        public override DbProviderFactory Factory
        {
            get
            {
                if (_Factory == null)
                {
                    lock (typeof(SqlServer))
                    {
                        if (_Factory == null) _Factory = GetProviderFactory("System.Data.SqlClient.dll", "System.Data.SqlClient.SqlClientFactory");
                    }
                }

                return _Factory;
            }
        }

        /// <summary>是否SQL2012及以上</summary>
        public Boolean IsSQL2012 => Version.Major > 11;

        private Version _Version;
        /// <summary>是否SQL2005及以上</summary>
        public Version Version
        {
            get
            {
                if (_Version == null)
                {
                    _Version = new Version(ServerVersion);

                    //var session = CreateSession();
                    //try
                    //{
                    //    // 取数据库版本
                    //    if (!session.Opened) session.Open();
                    //    var ver = session.Conn.ServerVersion;
                    //    session.AutoClose();

                    //    _Version = new Version(ver);
                    //}
                    //catch (Exception ex)
                    //{
                    //    XTrace.WriteLine("查询[{0}]的版本时出错，将按MSSQL2000进行分页处理！{1}", ConnName, ex);
                    //    _Version = new Version();
                    //}
                    //finally { session.Dispose(); }
                }
                return _Version;
            }
        }

        /// <summary>数据目录</summary>
        public String DataPath { get; set; }

        const String Application_Name = "Application Name";
        protected override void OnSetConnectionString(ConnectionStringBuilder builder)
        {
            // 获取数据目录，用于反向工程创建数据库
            if (builder.TryGetAndRemove("DataPath", out var str) && !str.IsNullOrEmpty()) DataPath = str;

            base.OnSetConnectionString(builder);

            if (builder[Application_Name] == null)
            {
#if !__CORE__
                var name = Runtime.IsWeb ? System.Web.Hosting.HostingEnvironment.SiteName : AppDomain.CurrentDomain.FriendlyName;
#else
                var name = AppDomain.CurrentDomain.FriendlyName;
#endif
                builder[Application_Name] = String.Format("XCode_{0}_{1}", name, ConnName);
            }
        }
        #endregion

        #region 方法
        /// <summary>创建数据库会话</summary>
        /// <returns></returns>
        protected override IDbSession OnCreateSession() => new SqlServerSession(this);

        /// <summary>创建元数据对象</summary>
        /// <returns></returns>
        protected override IMetaData OnCreateMetaData() => new SqlServerMetaData();

        public override Boolean Support(String providerName)
        {
            providerName = providerName.ToLower();
            if (providerName.Contains("system.data.sqlclient")) return true;
            if (providerName.Contains("sql2012")) return true;
            if (providerName.Contains("sql2008")) return true;
            if (providerName.Contains("sql2005")) return true;
            if (providerName.Contains("sql2000")) return true;
            if (providerName == "sqlclient") return true;
            if (providerName.Contains("mssql")) return true;
            if (providerName.Contains("sqlserver")) return true;

            return false;
        }
        #endregion

        #region 分页
        /// <summary>构造分页SQL</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <param name="keyColumn">唯一键。用于not in分页</param>
        /// <returns>分页SQL</returns>
        public override String PageSplit(String sql, Int64 startRowIndex, Int64 maximumRows, String keyColumn)
        {
            // 从第一行开始，不需要分页
            if (startRowIndex <= 0 && maximumRows < 1) return sql;

            if (startRowIndex > 0)
            {
                // 指定了起始行，并且是SQL2005及以上版本，使用MS SQL 2012特有的分页算法
                if (IsSQL2012)
                {
                    // 从第一行开始，不需要分页
                    if (startRowIndex <= 0)
                    {
                        if (maximumRows < 1) return sql;

                        var sql_ = FormatSqlserver2012SQL(sql);
                        return $"{sql_} offset 1 rows fetch next {maximumRows} rows only ";
                    }
                    if (maximumRows < 1) throw new NotSupportedException("不支持取第几条数据之后的所有数据！");

                    var sql__ = FormatSqlserver2012SQL(sql);
                    return $"{sql__} offset {startRowIndex} rows fetch next {maximumRows} rows only ";
                }

                // 指定了起始行，并且是SQL2005及以上版本，使用RowNumber算法
                //if (IsSQL2005)
                {
                    //return PageSplitRowNumber(sql, startRowIndex, maximumRows, keyColumn);
                    var builder = new SelectBuilder();
                    builder.Parse(sql);
                    //return MSPageSplit.PageSplit(builder, startRowIndex, maximumRows, IsSQL2005).ToString();

                    return PageSplit(builder, startRowIndex, maximumRows).ToString();
                }
            }

            // 如果没有Order By，直接调用基类方法
            // 先用字符串判断，命中率高，这样可以提高处理效率
            if (!sql.Contains(" Order "))
            {
                if (!sql.ToLower().Contains(" order ")) return base.PageSplit(sql, startRowIndex, maximumRows, keyColumn);
            }
            //// 使用正则进行严格判断。必须包含Order By，并且它右边没有右括号)，表明有order by，且不是子查询的，才需要特殊处理
            //MatchCollection ms = Regex.Matches(sql, @"\border\s*by\b([^)]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            //if (ms == null || ms.Count < 1 || ms[0].Index < 1)
            var sql2 = sql;
            var orderBy = CheckOrderClause(ref sql2);
            if (String.IsNullOrEmpty(orderBy))
            {
                return base.PageSplit(sql, startRowIndex, maximumRows, keyColumn);
            }
            // 已确定该sql最外层含有order by，再检查最外层是否有top。因为没有top的order by是不允许作为子查询的
            if (Regex.IsMatch(sql, @"^[^(]+\btop\b", RegexOptions.Compiled | RegexOptions.IgnoreCase))
            {
                return base.PageSplit(sql, startRowIndex, maximumRows, keyColumn);
            }
            //String orderBy = sql.Substring(ms[0].Index);

            // 从第一行开始，不需要分页
            if (startRowIndex <= 0)
            {
                if (maximumRows < 1)
                    return sql;
                else
                    return String.Format("Select Top {0} * From {1} {2}", maximumRows, CheckSimpleSQL(sql2), orderBy);
                //return String.Format("Select Top {0} * From {1} {2}", maximumRows, CheckSimpleSQL(sql.Substring(0, ms[0].Index)), orderBy);
            }

            #region Max/Min分页
            // 如果要使用max/min分页法，首先keyColumn必须有asc或者desc
            var kc = keyColumn.ToLower();
            if (kc.EndsWith(" desc") || kc.EndsWith(" asc") || kc.EndsWith(" unknown"))
            {
                var str = PageSplitMaxMin(sql, startRowIndex, maximumRows, keyColumn);
                if (!String.IsNullOrEmpty(str)) return str;
                keyColumn = keyColumn.Substring(0, keyColumn.IndexOf(" "));
            }
            #endregion

            sql = CheckSimpleSQL(sql2);

            if (String.IsNullOrEmpty(keyColumn)) throw new ArgumentNullException("keyColumn", "分页要求指定主键列或者排序字段！");

            if (maximumRows < 1)
                sql = String.Format("Select * From {1} Where {2} Not In(Select Top {0} {2} From {1} {3}) {3}", startRowIndex, sql, keyColumn, orderBy);
            else
                sql = String.Format("Select Top {0} * From {1} Where {2} Not In(Select Top {3} {2} From {1} {4}) {4}", maximumRows, sql, keyColumn, startRowIndex, orderBy);
            return sql;
        }

        /// <summary>
        /// 格式化SQL SERVER 2012分页前半部分SQL语句
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        private String FormatSqlserver2012SQL(String sql)
        {
            var builder = new SelectBuilder();
            builder.Parse(sql);

            var sb = NewLife.Collections.Pool.StringBuilder.Get();
            sb.Append("Select ");
            sb.Append(builder.ColumnOrDefault);
            sb.Append(" From ");
            sb.Append(builder.Table);
            if (!String.IsNullOrEmpty(builder.Where))
            {
                sb.Append(" Where type='p' and " + builder.Where);
            }
            else
            {
                sb.Append(" Where type='p' ");
            }
            if (!String.IsNullOrEmpty(builder.GroupBy)) sb.Append(" Group By " + builder.GroupBy);
            if (!String.IsNullOrEmpty(builder.Having)) sb.Append(" Having " + builder.Having);
            if (!String.IsNullOrEmpty(builder.OrderBy)) sb.Append(" Order By " + builder.OrderBy);

            return sb.Put(true);
        }

        public override SelectBuilder PageSplit(SelectBuilder builder, Int64 startRowIndex, Int64 maximumRows)
        {
            //return MSPageSplit.PageSplit(builder, startRowIndex, maximumRows, IsSQL2005, b => CreateSession().QueryCount(b));

            if (startRowIndex <= 0)
            {
                if (maximumRows < 1)
                {
                    return builder;
                }
                else if (builder.KeyIsOrderBy)
                {
                    return builder.Clone().Top(maximumRows);
                }
            }

            if (builder.Keys == null || builder.Keys.Length < 1) throw new XCodeException("分页算法要求指定排序列！" + builder.ToString());
            // 如果包含分组，则必须作为子查询
            var builder1 = builder.CloneWithGroupBy("XCode_T0", true);
            //builder1.Column = String.Format("{0}, row_number() over(Order By {1}) as rowNumber", builder.ColumnOrDefault, builder.OrderBy ?? builder.KeyOrder);
            // 不必追求极致，把所有列放出来
            builder1.Column = "*, row_number() over(Order By {0}) as rowNumber".F(builder.OrderBy ?? builder.KeyOrder);

            var builder2 = builder1.AsChild("XCode_T1", true);
            // 结果列处理
            //builder2.Column = builder.Column;
            //// 如果结果列包含有“.”，即有形如tab1.id、tab2.name之类的列时设为获取子查询的全部列
            //if ((!string.IsNullOrEmpty(builder2.Column)) && builder2.Column.Contains("."))
            //{
            //    builder2.Column = "*";
            //}
            // 不必追求极致，把所有列放出来
            builder2.Column = "*";

            // row_number()直接影响了排序，这里不再需要
            builder2.OrderBy = null;
            if (maximumRows < 1)
                builder2.Where = String.Format("rowNumber>={0}", startRowIndex + 1);
            else
                builder2.Where = String.Format("rowNumber Between {0} And {1}", startRowIndex + 1, startRowIndex + maximumRows);

            return builder2;
        }
        #endregion

        #region 数据库特性
        protected override String ReservedWordsStr
        {
            get
            {
                return "ADD,EXCEPT,PERCENT,ALL,EXEC,PLAN,ALTER,EXECUTE,PRECISION,AND,EXISTS,PRIMARY,ANY,EXIT,PRINT,AS,FETCH,PROC,ASC,FILE,PROCEDURE,AUTHORIZATION,FILLFACTOR,PUBLIC,BACKUP,FOR,RAISERROR,BEGIN,FOREIGN,READ,BETWEEN,FREETEXT,READTEXT,BREAK,FREETEXTTABLE,RECONFIGURE,BROWSE,FROM,REFERENCES,BULK,FULL,REPLICATION,BY,FUNCTION,RESTORE,CASCADE,GOTO,RESTRICT,CASE,GRANT,RETURN,CHECK,GROUP,REVOKE,CHECKPOINT,HAVING,RIGHT,CLOSE,HOLDLOCK,ROLLBACK,CLUSTERED,IDENTITY,ROWCOUNT,COALESCE,IDENTITY_INSERT,ROWGUIDCOL,COLLATE,IDENTITYCOL,RULE,COLUMN,IF,SAVE,COMMIT,IN,SCHEMA,COMPUTE,INDEX,SELECT,CONSTRAINT,INNER,SESSION_USER,CONTAINS,INSERT,SET,CONTAINSTABLE,INTERSECT,SETUSER,CONTINUE,INTO,SHUTDOWN,CONVERT,IS,SOME,CREATE,JOIN,STATISTICS,CROSS,KEY,SYSTEM_USER,CURRENT,KILL,TABLE,CURRENT_DATE,LEFT,TEXTSIZE,CURRENT_TIME,LIKE,THEN,CURRENT_TIMESTAMP,LINENO,TO,CURRENT_USER,LOAD,TOP,CURSOR,NATIONAL ,TRAN,DATABASE,NOCHECK,TRANSACTION,DBCC,NONCLUSTERED,TRIGGER,DEALLOCATE,NOT,TRUNCATE,DECLARE,NULL,TSEQUAL,DEFAULT,NULLIF,UNION,DELETE,OF,UNIQUE,DENY,OFF,UPDATE,DESC,OFFSETS,UPDATETEXT,DISK,ON,USE,DISTINCT,OPEN,USER,DISTRIBUTED,OPENDATASOURCE,VALUES,DOUBLE,OPENQUERY,VARYING,DROP,OPENROWSET,VIEW,DUMMY,OPENXML,WAITFOR,DUMP,OPTION,WHEN,ELSE,OR,WHERE,END,ORDER,WHILE,ERRLVL,OUTER,WITH,ESCAPE,OVER,WRITETEXT,ABSOLUTE,FOUND,PRESERVE,ACTION,FREE,PRIOR,ADMIN,GENERAL,PRIVILEGES,AFTER,GET,READS,AGGREGATE,GLOBAL,REAL,ALIAS,GO,RECURSIVE,ALLOCATE,GROUPING,REF,ARE,HOST,REFERENCING,ARRAY,HOUR,RELATIVE,ASSERTION,IGNORE,RESULT,AT,IMMEDIATE,RETURNS,BEFORE,INDICATOR,ROLE,BINARY,INITIALIZE,ROLLUP,BIT,INITIALLY,ROUTINE,BLOB,INOUT,ROW,BOOLEAN,INPUT,ROWS,BOTH,INT,SAVEPOINT,BREADTH,INTEGER,SCROLL,CALL,INTERVAL,SCOPE,CASCADED,ISOLATION,SEARCH,CAST,ITERATE,SECOND,CATALOG,LANGUAGE,SECTION,CHAR,LARGE,SEQUENCE,CHARACTER,LAST,SESSION,CLASS,LATERAL,SETS,CLOB,LEADING,SIZE,COLLATION,LESS,SMALLINT,COMPLETION,LEVEL,SPACE,CONNECT,LIMIT,SPECIFIC,CONNECTION,LOCAL,SPECIFICTYPE,CONSTRAINTS,LOCALTIME,SQL,CONSTRUCTOR,LOCALTIMESTAMP,SQLEXCEPTION,CORRESPONDING,LOCATOR,SQLSTATE,CUBE,MAP,SQLWARNING,CURRENT_PATH,MATCH,START,CURRENT_ROLE,MINUTE,STATE,CYCLE,MODIFIES,STATEMENT,DATA,MODIFY,STATIC,DATE,MODULE,STRUCTURE,DAY,MONTH,TEMPORARY,DEC,NAMES,TERMINATE,DECIMAL,NATURAL,THAN,DEFERRABLE,NCHAR,TIME,DEFERRED,NCLOB,TIMESTAMP,DEPTH,NEW,TIMEZONE_HOUR,DEREF,NEXT,TIMEZONE_MINUTE,DESCRIBE,NO,TRAILING,DESCRIPTOR,NONE,TRANSLATION,DESTROY,NUMERIC,TREAT,DESTRUCTOR,OBJECT,TRUE,DETERMINISTIC,OLD,UNDER,DICTIONARY,ONLY,UNKNOWN,DIAGNOSTICS,OPERATION,UNNEST,DISCONNECT,ORDINALITY,USAGE,DOMAIN,OUT,USING,DYNAMIC,OUTPUT,VALUE,EACH,PAD,VARCHAR,END-EXEC,PARAMETER,VARIABLE,EQUALS,PARAMETERS,WHENEVER,EVERY,PARTIAL,WITHOUT,EXCEPTION,PATH,WORK,EXTERNAL,POSTFIX,WRITE,FALSE,PREFIX,YEAR,FIRST,PREORDER,ZONE,FLOAT,PREPARE,ADA,AVG,BIT_LENGTH,CHAR_LENGTH,CHARACTER_LENGTH,COUNT,EXTRACT,FORTRAN,INCLUDE,INSENSITIVE,LOWER,MAX,MIN,OCTET_LENGTH,OVERLAPS,PASCAL,POSITION,SQLCA,SQLCODE,SQLERROR,SUBSTRING,SUM,TRANSLATE,TRIM,UPPER," +
                  "Sort,Level,User,Online";
            }
        }

        /// <summary>长文本长度</summary>
        public override Int32 LongTextLength => 4000;

        /// <summary>格式化时间为SQL字符串</summary>
        /// <param name="dateTime">时间值</param>
        /// <returns></returns>
        public override String FormatDateTime(DateTime dateTime) => "{ts'" + dateTime.ToFullString() + "'}";

        /// <summary>格式化名称，如果是关键字，则格式化后返回，否则原样返回</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public override String FormatName(String name)
        {
            // SqlServer数据库名和表名可以用横线。。。
            if (name.Contains("-")) return "[{0}]".F(name);

            return base.FormatName(name);
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
        }

        /// <summary>系统数据库名</summary>
        public override String SystemDatabaseName => "master";

        public override String FormatValue(IDataColumn field, Object value)
        {
            var code = System.Type.GetTypeCode(field.DataType);
            var isNullable = field.Nullable;

            if (code == TypeCode.String)
            {
                // 热心网友 Hannibal 在处理日文网站时发现插入的日文为乱码，这里加上N前缀
                if (value == null) return isNullable ? "null" : "''";

                // 为了兼容旧版本实体类
                if (field.RawType.StartsWithIgnoreCase("n"))
                    return "N'" + value.ToString().Replace("'", "''") + "'";
                else
                    return "'" + value.ToString().Replace("'", "''") + "'";
            }

            return base.FormatValue(field, value);
        }
        #endregion
    }

    /// <summary>SqlServer数据库</summary>
    internal class SqlServerSession : RemoteDbSession
    {
        #region 构造函数
        public SqlServerSession(IDatabase db) : base(db) { }
        #endregion

        #region 查询
        /// <summary>快速查询单表记录数，稍有偏差</summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public override Int64 QueryCountFast(String tableName)
        {
            tableName = tableName.Trim().Trim('[', ']').Trim();

            //var n = 0L;
            //if (QueryIndex().TryGetValue(tableName, out n)) return n;

            var sql = String.Format("select rows from sysindexes where id = object_id('{0}') and indid in (0,1)", tableName);
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
        //    var ds = Query("select object_name(id) as objname,rows from sysindexes where indid in (0,1) and status in (0,2066)");
        //    var dic = new Dictionary<String, Int64>(StringComparer.OrdinalIgnoreCase);
        //    foreach (DataRow dr in ds.Tables[0].Rows)
        //    {
        //        dic.Add(dr[0] + "", (Int32)dr[1]);
        //    }
        //    return dic;
        //}

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>新增行的自动编号</returns>
        public override Int64 InsertAndGetIdentity(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            sql = "SET NOCOUNT ON;" + sql + ";Select SCOPE_IDENTITY()";
            return base.InsertAndGetIdentity(sql, type, ps);
        }
        #endregion

        #region 批量操作
        public override Int32 Insert(IDataColumn[] columns, IEnumerable<IIndexAccessor> list)
        {

#if !__CORE__
            //重写批量插入方法
            var ps = new HashSet<String>();
            var sql = GetInsertSql(columns, ps);
            var dpsList = GetParametersList(columns, ps, list);

            return BatchExecute(sql, dpsList);
#else
            //Core仍使用原来的方法（有问题）
            var ps = new HashSet<String>();
            var sql = GetInsertSql(columns, ps);
            var dps = GetParameters(columns, ps, list);

            return Execute(sql, CommandType.Text, dps);
#endif
        }

        private String GetInsertSql(IDataColumn[] columns, ICollection<String> ps)
        {
            var table = columns.FirstOrDefault().Table;
            var sb = Pool.StringBuilder.Get();
            var db = Database as DbBase;

            // 字段列表
            sb.AppendFormat("Insert Into {0}(", db.FormatTableName(table.TableName));
            foreach (var dc in columns)
            {
                if (dc.Identity) continue;

                sb.Append(db.FormatName(dc.ColumnName));
                sb.Append(",");
            }
            sb.Length--;
            sb.Append(")");

            // 值列表
            sb.Append(" Values(");
            foreach (var dc in columns)
            {
                if (dc.Identity) continue;

                sb.Append(db.FormatParameterName(dc.Name));
                sb.Append(",");

                if (!ps.Contains(dc.Name)) ps.Add(dc.Name);
            }
            sb.Length--;
            sb.Append(")");

            return sb.Put(true);
        }

        private IDataParameter[] GetParameters(IDataColumn[] columns, ICollection<String> ps, IEnumerable<IIndexAccessor> list)
        {
            var db = Database;
            var dps = new List<IDataParameter>();
            foreach (var dc in columns)
            {
                if (dc.Identity) continue;
                if (!ps.Contains(dc.Name)) continue;

                var vs = new List<Object>();
                foreach (var entity in list)
                {
                    vs.Add(entity[dc.Name]);
                }
                var dp = db.CreateParameter(dc.Name, vs.ToArray(), dc);

                dps.Add(dp);
            }

            return dps.ToArray();
        }

        public override Int32 InsertOrUpdate(IDataColumn[] columns, ICollection<String> updateColumns, ICollection<String> addColumns, IEnumerable<IIndexAccessor> list)
        {
            var ps = new HashSet<String>();
            var insert = GetInsertSql(columns, ps);
            var update = GetUpdateSql(columns, updateColumns, addColumns, ps);

            // 先更新，根据更新结果影响的条目数判断是否需要插入
            var sb = Pool.StringBuilder.Get();
            sb.Append(update);
            sb.AppendLine(";");
            sb.AppendLine("IF(@@ROWCOUNT = 0)");
            sb.AppendLine("BEGIN");
            sb.Append(insert);
            sb.AppendLine(";");
            sb.AppendLine("END;");
            var sql = sb.Put(true);


#if !__CORE__
            // 重写
            var dpsList = GetParametersList(columns, ps, list, true);
            return BatchExecute(sql, dpsList);
#else
            // Core仍使用原来的版本（有问题）
            var dps = GetParameters(columns, ps, list);
            return Execute(sql, CommandType.Text, dps);
#endif

        }

        private String GetUpdateSql(IDataColumn[] columns, ICollection<String> updateColumns, ICollection<String> addColumns, ICollection<String> ps)
        {
            var table = columns.FirstOrDefault().Table;
            var sb = Pool.StringBuilder.Get();
            var db = Database as DbBase;

            // 字段列表
            sb.AppendFormat("Update {0} Set ", db.FormatTableName(table.TableName));
            foreach (var dc in columns)
            {
                if (dc.Identity || dc.PrimaryKey) continue;

                // 修复当columns看存在updateColumns不存在列时构造出来的Sql语句会出现连续逗号的问题
                if (updateColumns != null && updateColumns.Contains(dc.Name))
                {
                    sb.AppendFormat("{0}={1},", db.FormatName(dc.ColumnName), db.FormatParameterName(dc.Name));

                    if (!ps.Contains(dc.Name)) ps.Add(dc.Name);
                }
                else if (addColumns != null && addColumns.Contains(dc.Name))
                {
                    sb.AppendFormat("{0}={0}+{1},", db.FormatName(dc.ColumnName), db.FormatParameterName(dc.Name));

                    if (!ps.Contains(dc.Name)) ps.Add(dc.Name);
                }
                //sb.Append(",");
            }
            sb.Length--;
            //sb.Append(")");

            // 条件
            sb.Append(" Where ");
            foreach (var dc in columns)
            {
                if (!dc.PrimaryKey) continue;

                sb.AppendFormat("{0}={1}", db.FormatName(dc.ColumnName), db.FormatParameterName(dc.Name));
                sb.Append(" And ");

                if (!ps.Contains(dc.Name)) ps.Add(dc.Name);
            }
            sb.Length -= " And ".Length;

            return sb.Put(true);
        }
        #endregion

#if !__CORE__

        #region 修复实现SqlServer批量操作增添方法

        private int BatchExecute(String sql, List<IDataParameter[]> psList)
        {
            //获取连接对象
            var conn = Database.Pool.Get();

            // 准备
            var mBatcher = new SqlBatcher();
            mBatcher.StartBatch(conn);

            // 创建并添加Command
            foreach (var dps in psList)
            {
                if (dps != null)
                {
                    var cmd = OnCreateCommand(sql, CommandType.Text, dps);
                    mBatcher.AddToBatch(cmd);
                    //XTrace.WriteLine(base.GetSql(cmd));
                }
            }

            // 执行批量操作
            try
            {
                BeginTrace();
                int ret = mBatcher.ExecuteBatch();
                mBatcher.EndBatch();
                return ret;
            }
            catch (DbException ex)
            {
                throw OnException(ex);
            }
            finally
            {
                if (conn != null) Database.Pool.Put(conn);
                EndTrace(OnCreateCommand(sql, CommandType.Text));
            }
        }


        private List<IDataParameter[]> GetParametersList(IDataColumn[] columns, ICollection<String> ps, IEnumerable<IIndexAccessor> list, bool isInsertOrUpdate = false)
        {
            var db = Database;
            var dpsList = new List<IDataParameter[]>();

            foreach (var entity in list)
            {
                var dps = new List<IDataParameter>();
                foreach (var dc in columns)
                {
                    if (isInsertOrUpdate)
                    {
                        if (dc.Identity || dc.PrimaryKey)
                        {
                            //更新时添加主键做为查询条件
                            dps.Add(db.CreateParameter(dc.Name, entity[dc.Name], dc));
                            continue;
                        }
                    }
                    else
                    {
                        if (dc.Identity) continue;
                    }
                    if (!ps.Contains(dc.Name)) continue;

                    // 逐列创建参数对象
                    dps.Add(db.CreateParameter(dc.Name, entity[dc.Name], dc));
                }

                dpsList.Add(dps.ToArray());
            }

            return dpsList;
        }

        /// <summary>
        /// 批量操作帮助类
        /// </summary>
        class SqlBatcher
        {
            private System.Reflection.MethodInfo mAddToBatch;
            private System.Reflection.MethodInfo mClearBatch;
            private System.Reflection.MethodInfo mInitializeBatching;
            private System.Reflection.MethodInfo mExecuteBatch;
            private System.Data.SqlClient.SqlDataAdapter mAdapter;
            private bool isStarted;

            public SqlBatcher()
            {
                var type = typeof(System.Data.SqlClient.SqlDataAdapter);
                mAddToBatch = type.GetMethod("AddToBatch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                mClearBatch = type.GetMethod("ClearBatch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                mInitializeBatching = type.GetMethod("InitializeBatching", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                mExecuteBatch = type.GetMethod("ExecuteBatch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            }

            /// <summary>
            /// 获得批处理是否正在批处理状态。
            /// </summary>
            public bool IsStarted
            {
                get { return isStarted; }
            }

            /**/
            /// <summary>
            /// 开始批处理。
            /// </summary>
            /// <param name="connection">连接。</param>
            public void StartBatch(DbConnection connection)
            {
                if (isStarted) return;
                var command = new System.Data.SqlClient.SqlCommand();
                command.Connection = (System.Data.SqlClient.SqlConnection)connection;
                mAdapter = new System.Data.SqlClient.SqlDataAdapter();
                mAdapter.InsertCommand = command;
                mInitializeBatching.Invoke(mAdapter, null);
                isStarted = true;
            }

            /// <summary>
            /// 添加批命令。
            /// </summary>
            /// <param name="command">命令</param>
            public void AddToBatch(IDbCommand command)
            {
                if (!isStarted) throw new InvalidOperationException();
                mAddToBatch.Invoke(mAdapter, new object[1] { command });
            }

            /// <summary>
            /// 执行批处理。
            /// </summary>
            /// <returns>影响的数据行数。</returns>
            public int ExecuteBatch()
            {
                if (!isStarted) throw new InvalidOperationException();
                return (int)mExecuteBatch.Invoke(mAdapter, null);
            }

            /// <summary>
            /// 结束批处理。
            /// </summary>
            public void EndBatch()
            {
                if (isStarted)
                {
                    ClearBatch();
                    mAdapter.Dispose();
                    mAdapter = null;
                    isStarted = false;
                }
            }

            /// <summary>
            /// 清空保存的批命令。
            /// </summary>
            public void ClearBatch()
            {
                if (!isStarted) throw new InvalidOperationException();
                mClearBatch.Invoke(mAdapter, null);
            }
        }
        #endregion

#endif
    }

    /// <summary>SqlServer元数据</summary>
    class SqlServerMetaData : RemoteDbMetaData
    {
        public SqlServerMetaData()
        {
            Types = _DataTypes;
        }

        #region 属性
        ///// <summary>是否SQL2005</summary>
        //public Boolean IsSQL2005 { get { return (Database as SqlServer).IsSQL2005; } }

        public Version Version => (Database as SqlServer).Version;

        ///// <summary>0级类型</summary>
        //public String Level0type { get { return IsSQL2005 ? "SCHEMA" : "USER"; } }
        #endregion

        #region 构架
        /// <summary>取得所有表构架</summary>
        /// <returns></returns>
        protected override List<IDataTable> OnGetTables(String[] names)
        {
            #region 查表说明、字段信息、索引信息
            var session = Database.CreateSession();

            //一次性把所有的表说明查出来
            DataTable DescriptionTable = null;

            //var old = session.ShowSQL;
            //session.ShowSQL = false;
            try
            {
                var sql = "select b.name n, a.value v from sys.extended_properties a inner join sysobjects b on a.major_id=b.id and a.minor_id=0 and a.name = 'MS_Description'";
                DescriptionTable = session.Query(sql).Tables[0];
            }
            catch (Exception ex) { XTrace.WriteException(ex); }
            //session.ShowSQL = old;

            var dt = GetSchema(_.Tables, null);
            if (dt == null || dt.Rows == null || dt.Rows.Count < 1) return null;

            //session.ShowSQL = false;
            try
            {
                AllFields = session.Query(SchemaSql).Tables[0];
                AllIndexes = session.Query(IndexSql).Tables[0];
            }
            catch (Exception ex) { XTrace.WriteException(ex); }
            //session.ShowSQL = old;
            #endregion

            // 列出用户表
            var rows = dt.Select(String.Format("({0}='BASE TABLE' Or {0}='VIEW') AND TABLE_NAME<>'Sysdiagrams'", "TABLE_TYPE"));
            if (rows == null || rows.Length < 1) return null;

            var list = GetTables(rows, names);
            if (list == null || list.Count < 1) return list;

            // 修正备注
            foreach (var item in list)
            {
                var drs = DescriptionTable?.Select("n='" + item.TableName + "'");
                item.Description = drs == null || drs.Length < 1 ? "" : drs[0][1].ToString();
            }

            return list;
        }

        private DataTable AllFields = null;
        private DataTable AllIndexes = null;

        protected override void FixField(IDataColumn field, DataRow dr)
        {
            base.FixField(field, dr);

            var rows = AllFields?.Select("表名='" + field.Table.TableName + "' And 字段名='" + field.ColumnName + "'", null);
            if (rows != null && rows.Length > 0)
            {
                var dr2 = rows[0];

                field.Identity = GetDataRowValue<Boolean>(dr2, "标识");
                field.PrimaryKey = GetDataRowValue<Boolean>(dr2, "主键");
                //field.NumOfByte = GetDataRowValue<Int32>(dr2, "占用字节数");
                field.Description = GetDataRowValue<String>(dr2, "字段说明");
                field.Precision = GetDataRowValue<Int32>(dr2, "精度");
                field.Scale = GetDataRowValue<Int32>(dr2, "小数位数");
            }
        }

        protected override List<IDataIndex> GetIndexes(IDataTable table, DataTable _indexes, DataTable _indexColumns)
        {
            var list = base.GetIndexes(table, _indexes, _indexColumns);
            if (list != null && list.Count > 0)
            {
                foreach (var item in list)
                {
                    var drs = AllIndexes?.Select("name='" + item.Name + "'");
                    if (drs != null && drs.Length > 0)
                    {
                        item.Unique = GetDataRowValue<Boolean>(drs[0], "is_unique");
                        item.PrimaryKey = GetDataRowValue<Boolean>(drs[0], "is_primary_key");
                    }
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
            var sb = new StringBuilder();
            foreach (var item in pks)
            {
                if (sb.Length > 0) sb.Append(",");
                sb.Append(FormatName(item.ColumnName));
            }
            sql += "; " + Environment.NewLine;
            sql += String.Format("Alter Table {0} Add Constraint PK_{1} Primary Key Clustered({2})", FormatName(table.TableName), table.TableName, sb.ToString());
            return sql;
        }

        public override String FieldClause(IDataColumn field, Boolean onlyDefine)
        {
            if (!String.IsNullOrEmpty(field.RawType) && field.RawType.Contains("char(-1)"))
            {
                //if (IsSQL2005)
                field.RawType = field.RawType.Replace("char(-1)", "char(MAX)");
                //else
                //    field.RawType = field.RawType.Replace("char(-1)", "char(" + (Int32.MaxValue / 2) + ")");
            }

            //chenqi 2017-3-28
            //增加处理decimal类型精度和小数位数处理
            //此处只针对Sql server进行处理
            //严格来说，应该修改的地方是
            if (!field.RawType.IsNullOrEmpty() && field.RawType.StartsWithIgnoreCase("decimal"))
            {
                field.RawType = $"decimal({field.Precision},{field.Scale})";
            }

            return base.FieldClause(field, onlyDefine);
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

        //protected override String GetFormatParam(IDataColumn field, DataRow dr)
        //{
        //    var str = base.GetFormatParam(field, dr);
        //    if (String.IsNullOrEmpty(str)) return str;

        //    // 这个主要来自于float，因为无法取得其精度
        //    if (str == "(0)") return null;
        //    return str;
        //}

        //protected override String GetFormatParamItem(IDataColumn field, DataRow dr, String item)
        //{
        //    var pi = base.GetFormatParamItem(field, dr, item);
        //    if (field.DataType == typeof(String) && pi == "-1" && IsSQL2005) return "MAX";
        //    return pi;
        //}
        #endregion

        #region 取得字段信息的SQL模版
        private String _SchemaSql = "";
        /// <summary>构架SQL</summary>
        public virtual String SchemaSql
        {
            get
            {
                if (String.IsNullOrEmpty(_SchemaSql))
                {
                    var sb = new StringBuilder();
                    sb.Append("SELECT ");
                    sb.Append("表名=d.name,");
                    sb.Append("字段序号=a.colorder,");
                    sb.Append("字段名=a.name,");
                    sb.Append("标识=case when COLUMNPROPERTY( a.id,a.name,'IsIdentity')=1 then Convert(Bit,1) else Convert(Bit,0) end,");
                    sb.Append("主键=case when exists(SELECT 1 FROM sysobjects where xtype='PK' and name in (");
                    sb.Append("SELECT name FROM sysindexes WHERE id = a.id AND indid in(");
                    sb.Append("SELECT indid FROM sysindexkeys WHERE id = a.id AND colid=a.colid");
                    sb.Append("))) then Convert(Bit,1) else Convert(Bit,0) end,");
                    sb.Append("类型=b.name,");
                    sb.Append("占用字节数=a.length,");
                    sb.Append("长度=COLUMNPROPERTY(a.id,a.name,'PRECISION'),");
                    sb.Append("小数位数=isnull(COLUMNPROPERTY(a.id,a.name,'Scale'),0),");
                    sb.Append("允许空=case when a.isnullable=1 then Convert(Bit,1)else Convert(Bit,0) end,");
                    sb.Append("默认值=isnull(e.text,''),");
                    sb.Append("字段说明=isnull(g.[value],'')");
                    sb.Append("FROM syscolumns a ");
                    sb.Append("left join systypes b on a.xtype=b.xusertype ");
                    sb.Append("inner join sysobjects d on a.id=d.id  and d.xtype='U' ");
                    sb.Append("left join syscomments e on a.cdefault=e.id ");
                    //if (IsSQL2005)
                    //{
                    sb.Append("left join sys.extended_properties g on a.id=g.major_id and a.colid=g.minor_id and g.name = 'MS_Description'  ");
                    //}
                    //else
                    //{
                    //    sb.Append("left join sysproperties g on a.id=g.id and a.colid=g.smallid  ");
                    //}
                    sb.Append("order by a.id,a.colorder");
                    _SchemaSql = sb.ToString();
                }
                return _SchemaSql;
            }
        }

        private String _IndexSql;
        public virtual String IndexSql
        {
            get
            {
                if (_IndexSql == null)
                {
                    //if (IsSQL2005)
                    _IndexSql = "select ind.* from sys.indexes ind inner join sys.objects obj on ind.object_id = obj.object_id where obj.type='u'";
                    //else
                    //    _IndexSql = "select IndexProperty(obj.id, ind.name,'IsUnique') as is_unique, ObjectProperty(object_id(ind.name),'IsPrimaryKey') as is_primary_key,ind.* from sysindexes ind inner join sysobjects obj on ind.id = obj.id where obj.type='u'";
                }
                return _IndexSql;
            }
        }

        //private readonly String _DescriptionSql2000 = "select b.name n, a.value v from sysproperties a inner join sysobjects b on a.id=b.id where a.smallid=0";
        //private readonly String _DescriptionSql2005 = "select b.name n, a.value v from sys.extended_properties a inner join sysobjects b on a.major_id=b.id and a.minor_id=0 and a.name = 'MS_Description'";
        ///// <summary>取表说明SQL</summary>
        //public virtual String DescriptionSql { get { return IsSQL2005 ? _DescriptionSql2005 : _DescriptionSql2000; } }
        #endregion

        #region 数据定义
        public override String CreateDatabaseSQL(String dbname, String file)
        {
            var dp = (Database as SqlServer).DataPath;

            if (String.IsNullOrEmpty(file))
            {
                if (String.IsNullOrEmpty(dp)) return String.Format("CREATE DATABASE [{0}]", FormatName(dbname));

                file = dbname + ".mdf";
            }

            var logfile = String.Empty;

            if (!Path.IsPathRooted(file))
            {
                if (!String.IsNullOrEmpty(dp)) file = Path.Combine(dp, file);

                if (!Path.IsPathRooted(file)) file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file);
            }
            if (String.IsNullOrEmpty(Path.GetExtension(file))) file += ".mdf";
            file = new FileInfo(file).FullName;

            logfile = Path.ChangeExtension(file, ".ldf");
            logfile = new FileInfo(logfile).FullName;

            var dir = Path.GetDirectoryName(file);
            if (!String.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var sb = new StringBuilder();

            sb.AppendFormat("CREATE DATABASE {0} ON  PRIMARY", FormatName(dbname));
            sb.AppendLine();
            sb.AppendFormat(@"( NAME = N'{0}', FILENAME = N'{1}', SIZE = 10 , MAXSIZE = UNLIMITED, FILEGROWTH = 10%)", dbname, file);
            sb.AppendLine();
            sb.Append("LOG ON ");
            sb.AppendLine();
            sb.AppendFormat(@"( NAME = N'{0}_Log', FILENAME = N'{1}', SIZE = 10 , MAXSIZE = UNLIMITED, FILEGROWTH = 10%)", dbname, logfile);
            sb.AppendLine();

            return sb.ToString();
        }

        public override String DatabaseExistSQL(String dbname) => $"SELECT * FROM sysdatabases WHERE name = N'{dbname}'";

        /// <summary>使用数据架构确定数据库是否存在，因为使用系统视图可能没有权限</summary>
        /// <param name="dbname"></param>
        /// <returns></returns>
        protected override Boolean DatabaseExist(String dbname)
        {
            var dt = GetSchema(_.Databases, new String[] { dbname });
            return dt != null && dt.Rows != null && dt.Rows.Count > 0;
        }

        //protected override Boolean DropDatabase(String databaseName)
        //{
        //    //return base.DropDatabase(databaseName);

        //    // SQL语句片段，断开该数据库所有链接
        //    var sb = new StringBuilder();
        //    sb.AppendLine("use master");
        //    sb.AppendLine(";");
        //    sb.AppendLine("declare   @spid   varchar(20),@dbname   varchar(20)");
        //    sb.AppendLine("declare   #spid   cursor   for");
        //    sb.AppendFormat("select   spid=cast(spid   as   varchar(20))   from   master..sysprocesses   where   dbid=db_id('{0}')", databaseName);
        //    sb.AppendLine();
        //    sb.AppendLine("open   #spid");
        //    sb.AppendLine("fetch   next   from   #spid   into   @spid");
        //    sb.AppendLine("while   @@fetch_status=0");
        //    sb.AppendLine("begin");
        //    sb.AppendLine("exec('kill   '+@spid)");
        //    sb.AppendLine("fetch   next   from   #spid   into   @spid");
        //    sb.AppendLine("end");
        //    sb.AppendLine("close   #spid");
        //    sb.AppendLine("deallocate   #spid");

        //    var count = 0;
        //    var session = Database.CreateSession();
        //    try
        //    {
        //        count = session.Execute(sb.ToString());
        //    }
        //    catch (Exception ex) { XTrace.WriteException(ex); }
        //    return session.Execute(String.Format("Drop Database {0}", FormatName(databaseName))) > 0;
        //}

        public override String TableExistSQL(IDataTable table) => $"select * from sysobjects where xtype='U' and name='{table.TableName}'";

        /// <summary>使用数据架构确定数据表是否存在，因为使用系统视图可能没有权限</summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public Boolean TableExist(IDataTable table)
        {
            var dt = GetSchema(_.Tables, new String[] { null, null, table.TableName, null });
            return dt != null && dt.Rows != null && dt.Rows.Count > 0;
        }

        protected override String RenameTable(String tableName, String tempTableName)
        {
            if (Version.Major >= 8)
                return String.Format("EXECUTE sp_rename N'{0}', N'{1}', 'OBJECT' ", tableName, tempTableName);
            else
                return base.RenameTable(tableName, tempTableName);
        }

        protected override String ReBuildTable(IDataTable entitytable, IDataTable dbtable)
        {
            var sql = base.ReBuildTable(entitytable, dbtable);
            if (String.IsNullOrEmpty(sql)) return sql;

            // 特殊处理带标识列的表，需要增加SET IDENTITY_INSERT
            if (!entitytable.Columns.Any(e => e.Identity)) return sql;

            var tableName = Database.FormatName(entitytable.TableName);
            var ss = sql.Split("; " + Environment.NewLine);
            for (var i = 0; i < ss.Length; i++)
            {
                if (ss[i].StartsWithIgnoreCase("Insert Into"))
                {
                    ss[i] = String.Format("SET IDENTITY_INSERT {1} ON;{0};SET IDENTITY_INSERT {1} OFF", ss[i], tableName);
                    break;
                }
            }
            return String.Join("; " + Environment.NewLine, ss);
        }

        public override String AddTableDescriptionSQL(IDataTable table)
        {
            return String.Format("EXEC dbo.sp_addextendedproperty @name=N'MS_Description', @value=N'{1}' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'{0}'", table.TableName, table.Description);
        }

        public override String DropTableDescriptionSQL(IDataTable table)
        {
            return String.Format("EXEC dbo.sp_dropextendedproperty @name=N'MS_Description', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'{0}'", table.TableName);
        }

        public override String AddColumnSQL(IDataColumn field)
        {
            return String.Format("Alter Table {0} Add {1}", FormatName(field.Table.TableName), FieldClause(field, true));
        }

        public override String AlterColumnSQL(IDataColumn field, IDataColumn oldfield)
        {
            // 创建为自增，重建表
            if (field.Identity && !oldfield.Identity)
            {
                //return DropColumnSQL(oldfield) + ";" + Environment.NewLine + AddColumnSQL(field);
                return ReBuildTable(field.Table, oldfield.Table);
            }
            // 类型改变，必须重建表
            if (IsColumnTypeChanged(field, oldfield)) return ReBuildTable(field.Table, oldfield.Table);

            var sql = String.Format("Alter Table {0} Alter Column {1}", FormatName(field.Table.TableName), FieldClause(field, false));
            var pk = DeletePrimaryKeySQL(field);
            if (field.PrimaryKey)
            {
                // 如果没有主键删除脚本，表明没有主键
                //if (String.IsNullOrEmpty(pk))
                if (!oldfield.PrimaryKey)
                {
                    // 增加主键约束
                    pk = String.Format("Alter Table {0} ADD CONSTRAINT PK_{0} PRIMARY KEY {2}({1}) ON [PRIMARY]", FormatName(field.Table.TableName), FormatName(field.ColumnName), field.Identity ? "CLUSTERED" : "");
                    sql += ";" + Environment.NewLine + pk;
                }
            }
            else
            {
                // 字段声明没有主键，但是主键实际存在，则删除主键
                //if (!String.IsNullOrEmpty(pk))
                if (oldfield.PrimaryKey)
                {
                    sql += ";" + Environment.NewLine + pk;
                }
            }

            //// 需要提前删除相关默认值
            //if (oldfield.Default != null)
            //{
            //    var df = DropDefaultSQL(oldfield);
            //    if (!String.IsNullOrEmpty(df))
            //    {
            //        sql = df + ";" + Environment.NewLine + sql;

            //        // 如果还有默认值，加上
            //        if (field.Default != null)
            //        {
            //            df = AddDefaultSQLWithNoCheck(field);
            //            if (!String.IsNullOrEmpty(df)) sql += ";" + Environment.NewLine + df;
            //        }
            //    }
            //}
            // 需要提前删除相关索引
            foreach (var di in oldfield.Table.Indexes)
            {
                // 如果包含该字段
                if (di.Columns.Contains(oldfield.ColumnName, StringComparer.OrdinalIgnoreCase))
                {
                    var dis = DropIndexSQL(di);
                    if (!String.IsNullOrEmpty(dis)) sql = dis + ";" + Environment.NewLine + sql;
                }
            }
            // 如果还有索引，则加上
            foreach (var di in field.Table.Indexes)
            {
                // 如果包含该字段
                if (di.Columns.Contains(field.ColumnName, StringComparer.OrdinalIgnoreCase))
                {
                    var cis = CreateIndexSQL(di);
                    if (!String.IsNullOrEmpty(cis)) sql += ";" + Environment.NewLine + cis;
                }
            }

            return sql;
        }

        public override String DropIndexSQL(IDataIndex index)
        {
            return String.Format("Drop Index {1}.{0}", FormatName(index.Name), FormatName(index.Table.TableName));
        }

        public override String DropColumnSQL(IDataColumn field)
        {
            ////删除默认值
            //String sql = DropDefaultSQL(field);
            //if (!String.IsNullOrEmpty(sql)) sql += ";" + Environment.NewLine;

            //删除主键
            var sql = DeletePrimaryKeySQL(field);
            if (!String.IsNullOrEmpty(sql)) sql += ";" + Environment.NewLine;

            sql += base.DropColumnSQL(field);
            return sql;
        }

        public override String AddColumnDescriptionSQL(IDataColumn field)
        {
            var sql = String.Format("EXEC dbo.sp_addextendedproperty @name=N'MS_Description', @value=N'{1}' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'{0}', @level2type=N'COLUMN',@level2name=N'{2}'", field.Table.TableName, field.Description, field.ColumnName);
            return sql;
        }

        public override String DropColumnDescriptionSQL(IDataColumn field)
        {
            return String.Format("EXEC dbo.sp_dropextendedproperty @name=N'MS_Description', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'{0}', @level2type=N'COLUMN',@level2name=N'{1}'", field.Table.TableName, field.ColumnName);
        }

        String DeletePrimaryKeySQL(IDataColumn field)
        {
            if (!field.PrimaryKey) return String.Empty;

            var dis = field.Table.Indexes;
            if (dis == null || dis.Count < 1) return String.Empty;

            var di = dis.FirstOrDefault(e => e.Columns.Any(x => x.EqualIgnoreCase(field.ColumnName, field.Name)));
            if (di == null) return String.Empty;

            return String.Format("Alter Table {0} Drop CONSTRAINT {1}", FormatName(field.Table.TableName), di.Name);
        }

        public override String DropDatabaseSQL(String dbname)
        {
            var sb = new StringBuilder();
            sb.AppendLine("use master");
            sb.AppendLine(";");
            sb.AppendLine("declare   @spid   varchar(20),@dbname   varchar(20)");
            sb.AppendLine("declare   #spid   cursor   for");
            sb.AppendFormat("select   spid=cast(spid   as   varchar(20))   from   master..sysprocesses   where   dbid=db_id('{0}')", dbname);
            sb.AppendLine();
            sb.AppendLine("open   #spid");
            sb.AppendLine("fetch   next   from   #spid   into   @spid");
            sb.AppendLine("while   @@fetch_status=0");
            sb.AppendLine("begin");
            sb.AppendLine("exec('kill   '+@spid)");
            sb.AppendLine("fetch   next   from   #spid   into   @spid");
            sb.AppendLine("end");
            sb.AppendLine("close   #spid");
            sb.AppendLine("deallocate   #spid");
            sb.AppendLine(";");
            sb.AppendFormat("Drop Database {0}", FormatName(dbname));
            return sb.ToString();
        }
        #endregion

        /// <summary>数据类型映射</summary>
        private static Dictionary<Type, String[]> _DataTypes = new Dictionary<Type, String[]>
        {
            { typeof(Byte[]), new String[] { "binary({0})", "image", "varbinary({0})", "timestamp" } },
            //{ typeof(DateTimeOffset), new String[] { "datetimeoffset({0})" } },
            { typeof(Guid), new String[] { "uniqueidentifier" } },
            //{ typeof(Object), new String[] { "sql_variant" } },
            //{ typeof(TimeSpan), new String[] { "time({0})" } },
            { typeof(Boolean), new String[] { "bit" } },
            { typeof(Byte), new String[] { "tinyint" } },
            { typeof(Int16), new String[] { "smallint" } },
            { typeof(Int32), new String[] { "int" } },
            { typeof(Int64), new String[] { "bigint" } },
            { typeof(Single), new String[] { "real" } },
            { typeof(Double), new String[] { "float" } },
            { typeof(Decimal), new String[] { "money", "decimal({0}, {1})", "numeric({0}, {1})", "smallmoney" } },
            { typeof(DateTime), new String[] { "datetime", "smalldatetime", "datetime2({0})", "date" } },
            { typeof(String), new String[] { "nvarchar({0})", "ntext", "text", "varchar({0})", "char({0})", "nchar({0})", "xml" } }
        };

        #region 辅助函数
        /// <summary>除去字符串两端成对出现的符号</summary>
        /// <param name="str"></param>
        /// <param name="prefix"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public static String Trim(String str, String prefix, String suffix)
        {
            while (!String.IsNullOrEmpty(str))
            {
                if (!str.StartsWith(prefix)) return str;
                if (!str.EndsWith(suffix)) return str;

                str = str.Substring(prefix.Length, str.Length - suffix.Length - prefix.Length);
            }
            return str;
        }
        #endregion
    }
}