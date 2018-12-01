using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NewLife;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Reflection;

namespace XCode.DataAccessLayer
{
    class SQLite : FileDbBase
    {
        #region 属性
        /// <summary>返回数据库类型。</summary>
        public override DatabaseType Type => DatabaseType.SQLite;

        private static DbProviderFactory _Factory;
        /// <summary>工厂</summary>
        public override DbProviderFactory Factory
        {
            get
            {
                if (_Factory == null)
                {
                    lock (typeof(SQLite))
                    {
                        if (_Factory == null)
                        {
                            // Mono有自己的驱动，因为SQLite是混合编译，里面的C++代码与平台相关，不能通用;注意大小写问题
                            if (Runtime.Mono)
                                _Factory = GetProviderFactory("Mono.Data.Sqlite.dll", "Mono.Data.Sqlite.SqliteFactory");
                            else
                            {
                                //_Factory = GetProviderFactory(null, "System.Data.SQLite.SQLiteFactory", true);
#if __CORE__
                                _Factory = GetProviderFactory(null, "System.Data.SQLite.SQLiteFactory", true);
                                if (_Factory != null)
                                {
                                    // 加载子目录
                                    var plugin = NewLife.Setting.Current.GetPluginPath();
                                }
                                else
                                {
                                    _Factory = GetProviderFactory("Microsoft.Data.Sqlite.dll", "Microsoft.Data.Sqlite.SqliteFactory", true);
                                }
#else
                                //_Factory = GetProviderFactory(null, "Microsoft.Data.Sqlite.SqliteFactory");
#endif
                                if (_Factory == null) _Factory = GetProviderFactory("System.Data.SQLite.dll", "System.Data.SQLite.SQLiteFactory");
                            }

#if !__CORE__
                            // 设置线程安全模式
                            if (_Factory != null) SetThreadSafe();
#endif
                        }
                    }
                }

                return _Factory;
            }
        }

        /// <summary>是否内存数据库</summary>
        public Boolean IsMemoryDatabase => DatabaseName.EqualIgnoreCase(MemoryDatabase);

        /// <summary>自动收缩数据库</summary>
        /// <remarks>
        /// 当一个事务从数据库中删除了数据并提交后，数据库文件的大小保持不变。
        /// 即使整页的数据都被删除，该页也会变成“空闲页”等待再次被使用，而不会实际地被从数据库文件中删除。
        /// 执行vacuum操作，可以通过重建数据库文件来清除数据库内所有的未用空间，使数据库文件变小。
        /// 但是，如果一个数据库在创建时被指定为auto_vacuum数据库，当删除事务提交时，数据库文件会自动缩小。
        /// 使用auto_vacuum数据库可以节省空间，但却会增加数据库操作的时间。
        /// </remarks>
        public Boolean AutoVacuum { get; set; }

        static readonly String MemoryDatabase = ":memory:";

        protected override String OnResolveFile(String file)
        {
            if (String.IsNullOrEmpty(file) || file.EqualIgnoreCase(MemoryDatabase)) return MemoryDatabase;

            return base.OnResolveFile(file);
        }

        protected override void OnSetConnectionString(ConnectionStringBuilder builder)
        {
            base.OnSetConnectionString(builder);

            var flag = Factory.GetType().FullName.StartsWith("System.Data");
            if (flag)
            {
                //// 正常情况下INSERT, UPDATE和DELETE语句不返回数据。 当开启count-changes，以上语句返回一行含一个整数值的数据——该语句插入，修改或删除的行数。
                //if (!builder.ContainsKey("count_changes")) builder["count_changes"] = "1";

                // 优化SQLite，如果原始字符串里面没有这些参数，就设置这些参数
                //builder.TryAdd("Pooling", "true");
                //if (!builder.ContainsKey("Cache Size")) builder["Cache Size"] = "5000";
                builder.TryAdd("Cache Size", (512 * 1024 * 1024 / -1024) + "");
                // 加大Page Size会导致磁盘IO大大加大，性能反而有所下降
                //if (!builder.ContainsKey("Page Size")) builder["Page Size"] = "32768";
                // 这两个设置可以让SQLite拥有数十倍的极限性能，但同时又加大了风险，如果系统遭遇突然断电，数据库会出错，而导致系统无法自动恢复
                if (!Readonly) builder.TryAdd("Synchronous", "Off");
                // Journal Mode的内存设置太激进了，容易出事，关闭
                //if (!builder.ContainsKey("Journal Mode")) builder["Journal Mode"] = "Memory";
                // 数据库中一种高效的日志算法，对于非内存数据库而言，磁盘I/O操作是数据库效率的一大瓶颈。
                // 在相同的数据量下，采用WAL日志的数据库系统在事务提交时，磁盘写操作只有传统的回滚日志的一半左右，大大提高了数据库磁盘I/O操作的效率，从而提高了数据库的性能。
                if (!Readonly) builder.TryAdd("Journal Mode", "WAL");
                // 绝大多数情况下，都是小型应用，发生数据损坏的几率微乎其微，而多出来的问题让人觉得很烦，所以还是采用内存设置
                // 将来可以增加自动恢复数据的功能
                //if (!builder.ContainsKey("Journal Mode")) builder["Journal Mode"] = "Memory";

                if (Readonly) builder.TryAdd("Read Only", "true");

                // 自动清理数据
                if (builder.TryGetAndRemove("autoVacuum", out var vac)) AutoVacuum = vac.ToBoolean();
            }
            //else
            //    SupportSchema = false;

            // 默认超时时间
            //if (!builder.ContainsKey("Default Timeout")) builder["Default Timeout"] = 5 + "";

            // 繁忙超时
            //var busy = Setting.Current.CommandTimeout;
            //if (busy > 0)
            {
                // SQLite内部和.Net驱动都有Busy重试机制，多次重试仍然失败，则会出现dabase is locked。通过加大重试次数，减少高峰期出现locked的几率
                // 繁忙超时时间。出现Busy时，SQLite内部会在该超时时间内多次尝试
                //if (!builder.ContainsKey("BusyTimeout")) builder["BusyTimeout"] = 50 + "";
                // 重试次数。SQLite.Net驱动在遇到Busy时会多次尝试，每次随机等待1~150ms
                //if (!builder.ContainsKey("PrepareRetries")) builder["PrepareRetries"] = 10 + "";
            }

            DAL.WriteLog(builder.ToString());
        }
        #endregion

        #region 构造
        public SQLite()
        {
            // SQLite不使用自动关闭，以提升性能
            AutoClose = false;
        }

        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            // 不用Factory属性，为了避免触发加载SQLite驱动
            if (_Factory != null)
            {
                try
                {
                    // 清空连接池
                    var type = _Factory.CreateConnection().GetType();
                    type.Invoke("ClearAllPools");
                }
                catch (ObjectDisposedException) { }
                catch (Exception ex) { XTrace.WriteException(ex); }
            }
        }
        #endregion

        #region 方法
        /// <summary>创建数据库会话</summary>
        /// <returns></returns>
        protected override IDbSession OnCreateSession() => new SQLiteSession(this);

        /// <summary>创建元数据对象</summary>
        /// <returns></returns>
        protected override IMetaData OnCreateMetaData() => new SQLiteMetaData();

#if !__CORE__
        private void SetThreadSafe()
        {
            var asm = _Factory?.GetType().Assembly;
            if (asm == null) return;

            var type = asm.GetTypes().FirstOrDefault(e => e.Name == "UnsafeNativeMethods");
            var mi = type?.GetMethod("sqlite3_config_none", BindingFlags.Static | BindingFlags.NonPublic);
            if (mi == null) return;

            /*
             * SQLiteConfigOpsEnum
             * 	SQLITE_CONFIG_SINGLETHREAD = 1,
             * 	SQLITE_CONFIG_MULTITHREAD = 2,
             * 	SQLITE_CONFIG_SERIALIZED = 3,
             */

            mi.Invoke(this, new Object[] { 2 });
        }
#endif
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
            if (maximumRows < 1) throw new NotSupportedException("不支持取第几条数据之后的所有数据！");

            return String.Format("{0} limit {1}, {2}", sql, startRowIndex, maximumRows);
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

            // 不要修改原来的查询语句
            builder = builder.Clone();
            builder.Limit += String.Format(" limit {0}, {1}", startRowIndex, maximumRows);
            return builder;
        }
        #endregion

        #region 数据库特性
        protected override String ReservedWordsStr
        {
            get { return "ABORT,ACTION,ADD,AFTER,ALL,ALTER,ANALYZE,AND,AS,ASC,ATTACH,AUTOINCREMENT,BEFORE,BEGIN,BETWEEN,BY,CASCADE,CASE,CAST,CHECK,COLLATE,COLUMN,COMMIT,CONFLICT,CONSTRAINT,CREATE,CROSS,CURRENT_DATE,CURRENT_TIME,CURRENT_TIMESTAMP,DATABASE,DEFAULT,DEFERRABLE,DEFERRED,DELETE,DESC,DETACH,DISTINCT,DROP,EACH,ELSE,END,ESCAPE,EXCEPT,EXCLUSIVE,EXISTS,EXPLAIN,FAIL,FOR,FOREIGN,FROM,FULL,GLOB,GROUP,HAVING,IF,IGNORE,IMMEDIATE,IN,INDEX,INDEXED,INITIALLY,INNER,INSERT,INSTEAD,INTERSECT,INTO,IS,ISNULL,JOIN,KEY,LEFT,LIKE,LIMIT,MATCH,NATURAL,NO,NOT,NOTNULL,NULL,OF,OFFSET,ON,OR,ORDER,OUTER,PLAN,PRAGMA,PRIMARY,QUERY,RAISE,RECURSIVE,REFERENCES,REGEXP,REINDEX,RELEASE,RENAME,REPLACE,RESTRICT,RIGHT,ROLLBACK,ROW,SAVEPOINT,SELECT,SET,TABLE,TEMP,TEMPORARY,THEN,TO,TRANSACTION,TRIGGER,UNION,UNIQUE,UPDATE,USING,VACUUM,VALUES,VIEW,VIRTUAL,WHEN,WHERE,WITH,WITHOUT"; }
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
            //return keyWord;
        }

        public override String FormatValue(IDataColumn field, Object value)
        {
            if (field.DataType == typeof(Byte[]))
            {
                var bts = (Byte[])value;
                if (bts == null || bts.Length < 1) return "0x0";

                return "X'" + BitConverter.ToString(bts).Replace("-", null) + "'";
            }

            return base.FormatValue(field, value);
        }

        /// <summary>字符串相加</summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public override String StringConcat(String left, String right) => (!left.IsNullOrEmpty() ? left : "\'\'") + "||" + (!right.IsNullOrEmpty() ? right : "\'\'");
        #endregion
    }

    /// <summary>SQLite数据库</summary>
    internal class SQLiteSession : FileDbSession
    {
        #region 构造函数
        public SQLiteSession(IDatabase db) : base(db) { }
        #endregion

        #region 方法
        protected override void CreateDatabase()
        {
            // 内存数据库不需要创建
            if ((Database as SQLite).IsMemoryDatabase) return;

            base.CreateDatabase();

            // 打开自动清理数据库模式，此条命令必须放在创建表之前使用
            // 当从SQLite中删除数据时，数据文件大小不会减小，当重新插入数据时，
            // 将使用那块“空白”空间，打开自动清理后，删除数据后，会自动清理“空白”空间
            if ((Database as SQLite).AutoVacuum) Execute("PRAGMA auto_vacuum = 1");
        }
        #endregion

        #region 基本方法 查询/执行
        protected override void OnFill(DbTable ds, DbDataReader dr)
        {
            var count = dr.FieldCount;
            var md = Database.CreateMetaData() as DbMetaData;

            // 字段
            var ts = new Type[count];
            var tns = new String[count];
            for (var i = 0; i < count; i++)
            {
                tns[i] = dr.GetDataTypeName(i);
                ts[i] = md.GetDataType(tns[i]);
            }
            ds.Types = ts;
            //ds.TypeNames = tns;
        }

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>新增行的自动编号</returns>
        public override Int64 InsertAndGetIdentity(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            sql += ";Select last_insert_rowid() newid";
            return base.InsertAndGetIdentity(sql, type, ps);
        }
        #endregion

        #region 高级
        /// <summary>清空数据表，标识归零</summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public override Int32 Truncate(String tableName)
        {
            // 先删除数据再收缩
            var sql = "Delete From {0}".F(Database.FormatName(tableName));
            var rs = Execute(sql);

            // 该数据库没有任何表用到自增时，序列表不存在
            try
            {
                Execute("Update sqlite_sequence Set seq=0 where name='{0}'".F(Database.FormatName(tableName)));
            }
            catch (Exception ex) { XTrace.WriteException(ex); }

            rs += Execute("PRAGMA auto_vacuum = 1");
            //rs += Execute("VACUUM");

            return rs;
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
                sb.Append(" On Conflict");

                // 先找唯一索引，再用主键
                var table = columns.FirstOrDefault()?.Table;
                var di = table.Indexes?.FirstOrDefault(e => e.Unique);
                if (di != null && di.Columns != null && di.Columns.Length > 0)
                {
                    sb.AppendFormat("({0})", di.Columns.Join(",", e => db.FormatName(e)));
                }
                else
                {
                    var pks = table.PrimaryKeys;
                    if (pks != null && pks.Length > 0)
                        sb.AppendFormat("({0})", pks.Join(",", e => db.FormatName(e.ColumnName)));
                }

                sb.Append(" Do Update Set ");
                if (updateColumns != null)
                {
                    foreach (var dc in columns)
                    {
                        if (dc.Identity || dc.PrimaryKey) continue;

                        if (updateColumns.Contains(dc.Name) && (addColumns == null || !addColumns.Contains(dc.Name)))
                            sb.AppendFormat("{0}=excluded.{0},", db.FormatName(dc.ColumnName));
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
                            sb.AppendFormat("{0}={0}+excluded.{0},", db.FormatName(dc.ColumnName));
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

    /// <summary>SQLite元数据</summary>
    class SQLiteMetaData : FileDbMetaData
    {
        public SQLiteMetaData() => Types = _DataTypes;

        #region 数据类型
        protected override List<KeyValuePair<Type, Type>> FieldTypeMaps
        {
            get
            {
                if (_FieldTypeMaps == null)
                {
                    var list = base.FieldTypeMaps;
                    // SQLite自增字段有时是Int64，需要到Int32的映射
                    if (!list.Any(e => e.Key == typeof(Int64) && e.Value == typeof(Int32)))
                        list.Add(new KeyValuePair<Type, Type>(typeof(Int64), typeof(Int32)));
                }
                return base.FieldTypeMaps;
            }
        }

        /// <summary>数据类型映射</summary>
        private static Dictionary<Type, String[]> _DataTypes = new Dictionary<Type, String[]>
        {
            { typeof(Byte[]), new String[] { "binary", "varbinary", "blob", "image", "general", "oleobject" } },
            { typeof(Guid), new String[] { "uniqueidentifier", "guid" } },
            { typeof(Boolean), new String[] { "bit", "yesno", "logical", "bool", "boolean" } },
            { typeof(Byte), new String[] { "tinyint" } },
            { typeof(Int16), new String[] { "smallint" } },
            { typeof(Int32), new String[] { "int" } },
            { typeof(Int64), new String[] { "integer", "counter", "autoincrement", "identity", "long", "bigint" } },
            { typeof(Single), new String[] { "single" } },
            { typeof(Double), new String[] { "real", "float", "double" } },
            { typeof(Decimal), new String[] { "money", "decimal", "currency", "numeric" } },
            { typeof(DateTime), new String[] { "datetime", "smalldate", "timestamp", "date", "time" } },
            { typeof(String), new String[] { "nvarchar({0})", "ntext", "varchar({0})", "memo({0})", "longtext({0})", "note({0})", "text({0})", "string({0})", "char({0})", "char({0})" } }
        };
        #endregion

        #region 构架
        protected override List<IDataTable> OnGetTables(String[] names)
        {
            // 特殊处理内存数据库
            if ((Database as SQLite).IsMemoryDatabase) return memoryTables.Where(t => names.Contains(t.TableName)).ToList();

            //var dt = GetSchema(_.Tables, null);
            //if (dt?.Rows == null || dt.Rows.Count < 1) return null;

            //// 默认列出所有字段
            //var rows = dt.Select("TABLE_TYPE='table'");
            //if (rows == null || rows.Length < 1) return null;

            //return GetTables(rows, names);

            var ss = Database.CreateSession();

            var sql = "select * from sqlite_master";
            var ds = ss.Query(sql, null);
            if (ds.Rows.Count == 0) return null;

            var list = new List<IDataTable>();
            var hs = new HashSet<String>(names ?? new String[0], StringComparer.OrdinalIgnoreCase);

            var dts = Select(ds, "type", "table");
            var dis = Select(ds, "type", "index");

            for (var dr = 0; dr < dts.Rows.Count; dr++)
            {
                var name = dts.Get<String>(dr, "tbl_name");
                if (hs.Count > 0 && !hs.Contains(name)) continue;

                var table = DAL.CreateTable();
                table.TableName = name;

                sql = dts.Get<String>(dr, "sql");
                var sqls = sql.Split("\r\n").ToList();

                #region 字段
                //var dcs = ss.Query($"select * from {Database.FormatName(name)} limit 0,1", null);
                //for (var i = 0; i < dcs.Columns.Length; i++)
                foreach (var line in sqls)
                {
                    if (line.IsNullOrEmpty() || line[0] != '\t') continue;

                    var fs = line.Trim().Split(" ");
                    var field = table.CreateColumn();

                    field.ColumnName = fs[0].TrimStart('[').TrimEnd(']');
                    //field.DataType = dcs.Types[i];
                    //field.RawType = dcs.TypeNames[i];

                    if (line.Contains("AUTOINCREMENT")) field.Identity = true;
                    if (line.Contains("Primary Key")) field.PrimaryKey = true;

                    if (line.Contains("NOT NULL"))
                        field.Nullable = false;
                    else if (line.Contains(" NULL "))
                        field.Nullable = true;

                    field.RawType = fs[1];
                    field.Length = field.RawType.Substring("(", ")").ToInt();

                    field.DataType = GetDataType(field.RawType);

                    // SQLite的字段长度、精度等，都是由类型决定，固定值

                    // 如果数据库里面是integer或者autoincrement，识别类型是Int64，又是自增，则改为Int32，保持与大多数数据库的兼容
                    if (field.Identity && field.DataType == typeof(Int64) && field.RawType.EqualIgnoreCase("integer", "autoincrement"))
                    {
                        field.DataType = typeof(Int32);
                    }

                    if (field.DataType == null)
                    {
                        if (field.RawType.StartsWithIgnoreCase("varchar2", "nvarchar2")) field.DataType = typeof(String);
                    }

                    table.Columns.Add(field);
                }
                #endregion

                #region 索引
                var dis2 = Select(dis, "tbl_name", name);
                for (var i = 0; dis2?.Rows != null && i < dis2.Rows.Count; i++)
                {
                    var di = table.CreateIndex();
                    di.Name = dis2.Get<String>(i, "name");

                    sql = dis2.Get<String>(i, "sql");
                    if (sql.IsNullOrEmpty()) continue;

                    if (sql.Contains(" UNIQUE ")) di.Unique = true;

                    di.Columns = sql.Substring("(", ")").Split(",").Select(e => e.Trim()).ToArray();

                    table.Indexes.Add(di);
                }
                #endregion

                //FixTable(table, dr, data);

                // 修正关系数据
                table.Fix();

                list.Add(table);
            }

            return list;
        }

        protected override String GetFieldType(IDataColumn field)
        {
            var typeName = base.GetFieldType(field);

            // 自增字段必须是integer
            // 云飞扬2017-07-19 修改为也支持长整型转成integer
            if (field.Identity && typeName.Contains("int")) return "integer";
            //云飞扬 2017-07-05
            //因为SQLite的text长度比较小，这里设置为默认值
            if (typeName.Contains("text")) return "text";
            return typeName;
        }

        protected override String GetFieldConstraints(IDataColumn field, Boolean onlyDefine)
        {
            // SQLite要求自增必须是主键
            if (field.Identity && !field.PrimaryKey)
            {
                // 取消所有主键
                field.Table.Columns.ForEach(dc => dc.PrimaryKey = false);

                // 自增字段作为主键
                field.PrimaryKey = true;
            }

            var str = base.GetFieldConstraints(field, onlyDefine);

            if (field.Identity) str += " AUTOINCREMENT";

            // 给字符串字段加上忽略大小写，否则admin和Admin是查不出来的
            if (field.DataType == typeof(String)) str += " COLLATE NOCASE";

            return str;
        }
        #endregion

        #region 数据定义
        protected override void CreateDatabase()
        {
            if (!(Database as SQLite).IsMemoryDatabase) base.CreateDatabase();
        }

        protected override void DropDatabase()
        {
            if (!(Database as SQLite).IsMemoryDatabase) base.DropDatabase();
        }

        /// <summary>备份文件到目标文件</summary>
        /// <param name="dbname"></param>
        /// <param name="bakfile"></param>
        /// <param name="compressed"></param>
        public override String Backup(String dbname, String bakfile, Boolean compressed)
        {
            var dbfile = FileName;

            // 备份文件
            var bf = bakfile;
            if (bf.IsNullOrEmpty())
            {
                var name = dbname;
                if (name.IsNullOrEmpty()) name = Path.GetFileNameWithoutExtension(dbfile);

                var ext = Path.GetExtension(dbfile);
                if (ext.IsNullOrEmpty()) ext = ".db";

                if (compressed)
                    bf = "{0}{1}".F(name, ext);
                else
                    bf = "{0}_{1:yyyyMMddHHmmss}{2}".F(name, DateTime.Now, ext);
            }
            if (!Path.IsPathRooted(bf)) bf = Setting.Current.BackupPath.CombinePath(bf).GetFullPath();

            bf = bf.EnsureDirectory(true);

            WriteLog("{0}备份SQLite数据库 {1} 到 {2}", Database.ConnName, dbfile, bf);

            var sw = Stopwatch.StartNew();

            // 删除已有文件
            if (File.Exists(bf)) File.Delete(bf);

            using (var conn = Database.Factory.CreateConnection())
            {
                var conn2 = Database.Pool.Get();
                try
                {
                    conn.ConnectionString = "Data Source={0}".F(bf);
                    conn.Open();

                    //var method = conn.GetType().GetMethodEx("BackupDatabase");
                    // 借助BackupDatabase函数可以实现任意两个SQLite之间倒数据，包括内存数据库
                    conn2.Invoke("BackupDatabase", conn, "main", "main", -1, null, 0);
                }
                finally
                {
                    Database.Pool.Put(conn2);
                }
            }

            // 压缩
            WriteLog("备份文件大小：{0:n0}字节", bf.AsFile().Length);
            if (compressed)
            {
                var zipfile = Path.ChangeExtension(bf, "zip");
                if (bakfile.IsNullOrEmpty()) zipfile = zipfile.TrimEnd(".zip") + "_{0:yyyyMMddHHmmss}.zip".F(DateTime.Now);

                var fi = bf.AsFile();
                fi.Compress(zipfile);
                WriteLog("压缩后大小：{0:n0}字节，{1}", zipfile.AsFile().Length, zipfile);

                // 删除未备份
                File.Delete(bf);

                bf = zipfile;
            }

            sw.Stop();
            WriteLog("备份完成，耗时{0:n0}ms", sw.ElapsedMilliseconds);

            return bf;
        }

        public override String CreateIndexSQL(IDataIndex index)
        {
            var sb = new StringBuilder(32 + index.Columns.Length * 20);
            if (index.Unique)
                sb.Append("Create Unique Index ");
            else
                sb.Append("Create Index ");

            // SQLite索引优先采用自带索引名
            if (!String.IsNullOrEmpty(index.Name) && index.Name.Contains(index.Table.TableName))
                sb.Append(FormatName(index.Name));
            else
            {
                // SQLite中不同表的索引名也不能相同
                sb.Append("IX_");
                sb.Append(FormatName(index.Table.TableName));
                foreach (var item in index.Columns)
                {
                    sb.AppendFormat("_{0}", item);
                }
            }

            sb.AppendFormat(" On {0} ({1})", FormatName(index.Table.TableName), index.Columns.Select(e => FormatName(e)).Join(", "));

            return sb.ToString();
        }

        /// <summary>删除索引方法</summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public override String DropIndexSQL(IDataIndex index) => $"Drop Index {FormatName(index.Name)}";

        protected override String CheckColumnsChange(IDataTable entitytable, IDataTable dbtable, Boolean onlySql, Boolean noDelete)
        {
            foreach (var item in entitytable.Columns)
            {
                // 自增字段必须是主键
                if (item.Identity && !item.PrimaryKey)
                {
                    // 取消所有主键
                    item.Table.Columns.ForEach(dc => dc.PrimaryKey = false);

                    // 自增字段作为主键
                    item.PrimaryKey = true;
                    break;
                }
            }

            // 把onlySql设为true，让基类只产生语句而不执行
            var sql = base.CheckColumnsChange(entitytable, dbtable, true, false);
            if (sql.IsNullOrEmpty()) return sql;

            // 只有修改字段、删除字段需要重建表
            if (!sql.Contains("Alter Column") && !sql.Contains("Drop Column"))
            {
                if (onlySql) return sql;

                Database.CreateSession().Execute(sql);

                return null;
            }

            var sql2 = sql;

            sql = ReBuildTable(entitytable, dbtable);
            if (sql.IsNullOrEmpty() || onlySql) return sql;

            // 输出日志，说明重建表的理由
            WriteLog("SQLite需要重建表，因无法执行：{0}", sql2);

            var flag = true;
            // 如果设定不允许删
            if (noDelete)
            {
                // 看看有没有数据库里面有而实体库里没有的
                foreach (var item in dbtable.Columns)
                {
                    var dc = entitytable.GetColumn(item.ColumnName);
                    if (dc == null)
                    {
                        flag = false;
                        break;
                    }
                }
            }
            if (!flag) return sql;

            Database.CreateSession().Execute(sql);

            return null;
        }
        #endregion

        #region 表和字段备注
        /// <summary>添加描述</summary>
        /// <remarks>返回Empty，告诉反向工程，该数据库类型不支持该功能，请不要输出日志</remarks>
        /// <param name="table"></param>
        /// <returns></returns>
        public override String AddTableDescriptionSQL(IDataTable table) => String.Empty;

        public override String DropTableDescriptionSQL(IDataTable table) => String.Empty;

        public override String AddColumnDescriptionSQL(IDataColumn field) => String.Empty;

        public override String DropColumnDescriptionSQL(IDataColumn field) => String.Empty;
        #endregion

        #region 反向工程
        private List<IDataTable> memoryTables = new List<IDataTable>();
        /// <summary>已重载。因为内存数据库无法检测到架构，不知道表是否已存在，所以需要自己维护</summary>
        /// <param name="entitytable"></param>
        /// <param name="dbtable"></param>
        /// <param name="mode"></param>
        protected override void CheckTable(IDataTable entitytable, IDataTable dbtable, Migration mode)
        {
            if (dbtable == null && (Database as SQLite).IsMemoryDatabase)
            {
                if (memoryTables.Any(t => t.TableName.EqualIgnoreCase(entitytable.TableName))) return;

                memoryTables.Add(entitytable);
            }

            base.CheckTable(entitytable, dbtable, mode);
        }

        public override Int32 CompactDatabase() => Database.CreateSession().Execute("VACUUM");
        #endregion
    }
}