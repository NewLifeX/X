using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife;
using NewLife.Reflection;
using NewLife.Security;

namespace XCode.DataAccessLayer
{
    class SQLite : FileDbBase
    {
        #region 属性
        /// <summary>返回数据库类型。</summary>
        public override DatabaseType Type { get { return DatabaseType.SQLite; } }

        private static DbProviderFactory _dbProviderFactory;
        /// <summary>提供者工厂</summary>
        static DbProviderFactory DbProviderFactory
        {
            get
            {
                if (_dbProviderFactory == null)
                {
                    lock (typeof(SQLite))
                    {
                        // Mono有自己的驱动，因为SQLite是混合编译，里面的C++代码与平台相关，不能通用;注意大小写问题
                        if (Runtime.Mono)
                        {
                            if (_dbProviderFactory == null) _dbProviderFactory = GetProviderFactory("Mono.Data.Sqlite.dll", "Mono.Data.Sqlite.SqliteFactory");
                        }
                        else
                        {
                            if (_dbProviderFactory == null) _dbProviderFactory = GetProviderFactory("System.Data.SQLite.dll", "System.Data.SQLite.SQLiteFactory");
                        }
                    }
                }

                return _dbProviderFactory;
            }
        }

        /// <summary>工厂</summary>
        public override DbProviderFactory Factory { get { return DbProviderFactory; } }

        /// <summary>是否内存数据库</summary>
        public Boolean IsMemoryDatabase { get { return FileName.EqualIgnoreCase(MemoryDatabase); } }

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

        protected override void OnSetConnectionString(XDbConnectionStringBuilder builder)
        {
            base.OnSetConnectionString(builder);

            //// 正常情况下INSERT, UPDATE和DELETE语句不返回数据。 当开启count-changes，以上语句返回一行含一个整数值的数据——该语句插入，修改或删除的行数。
            //if (!builder.ContainsKey("count_changes")) builder["count_changes"] = "1";

            // 优化SQLite，如果原始字符串里面没有这些参数，就设置这些参数
            if (!builder.ContainsKey("Pooling")) builder["Pooling"] = "true";
            //if (!builder.ContainsKey("Cache Size")) builder["Cache Size"] = "5000";
            if (!builder.ContainsKey("Cache Size")) builder["Cache Size"] = (512 * 1024 * 1024 / -1024) + "";
            // 加大Page Size会导致磁盘IO大大加大，性能反而有所下降
            //if (!builder.ContainsKey("Page Size")) builder["Page Size"] = "32768";
            // 这两个设置可以让SQLite拥有数十倍的极限性能，但同时又加大了风险，如果系统遭遇突然断电，数据库会出错，而导致系统无法自动恢复
            if (!builder.ContainsKey("Synchronous")) builder["Synchronous"] = "Off";
            // Journal Mode的内存设置太激进了，容易出事，关闭
            //if (!builder.ContainsKey("Journal Mode")) builder["Journal Mode"] = "Memory";
            // 数据库中一种高效的日志算法，对于非内存数据库而言，磁盘I/O操作是数据库效率的一大瓶颈。
            // 在相同的数据量下，采用WAL日志的数据库系统在事务提交时，磁盘写操作只有传统的回滚日志的一半左右，大大提高了数据库磁盘I/O操作的效率，从而提高了数据库的性能。
            if (!builder.ContainsKey("Journal Mode")) builder["Journal Mode"] = "WAL";
            // 绝大多数情况下，都是小型应用，发生数据损坏的几率微乎其微，而多出来的问题让人觉得很烦，所以还是采用内存设置
            // 将来可以增加自动恢复数据的功能
            //if (!builder.ContainsKey("Journal Mode")) builder["Journal Mode"] = "Memory";

            // 自动清理数据
            if (builder.ContainsKey("autoVacuum"))
            {
                AutoVacuum = builder["autoVacuum"].ToBoolean();
                builder.Remove("autoVacuum");
            }

            // 默认超时时间
            if (!builder.ContainsKey("Default Timeout")) builder["Default Timeout"] = 5 + "";

            DAL.WriteLog(builder.ToString());
        }
        #endregion

        #region 构造
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            // 不用Factory属性，为了避免触发加载SQLite驱动
            if (_dbProviderFactory != null)
            {
                try
                {
                    // 清空连接池
                    var type = _dbProviderFactory.CreateConnection().GetType();
                    type.Invoke("ClearAllPools");
                }
                catch { }
            }
        }
        #endregion

        #region 方法
        /// <summary>创建数据库会话</summary>
        /// <returns></returns>
        protected override IDbSession OnCreateSession() { return new SQLiteSession(this); }

        /// <summary>创建元数据对象</summary>
        /// <returns></returns>
        protected override IMetaData OnCreateMetaData() { return new SQLiteMetaData(); }
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

            builder.Limit += String.Format(" limit {0}, {1}", startRowIndex, maximumRows);
            return builder;
        }
        #endregion

        #region 数据库特性
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
        public override String StringConcat(String left, String right) { return (!String.IsNullOrEmpty(left) ? left : "\'\'") + "||" + (!String.IsNullOrEmpty(right) ? right : "\'\'"); }

        private Boolean _inited;
        public void Init()
        {
            if (_inited) return;
            _inited = true;

            Task.Run(() =>
            {
                var ss = CreateSession();
                ss.Execute("PRAGMA temp_store=memory");
                //ss.Execute("PRAGMA temp_store_directory='{0}'".F(".".GetFullPath()));
            });
        }
        #endregion
    }

    /// <summary>SQLite数据库</summary>
    internal class SQLiteSession : FileDbSession
    {
        #region 构造函数
        public SQLiteSession(IDatabase db) : base(db) { }
        #endregion

        #region 方法
        public override void Open()
        {
            try
            {
                base.Open();

                (Database as SQLite).Init();
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains(" malformed")) throw;

                throw new XCodeException("数据库文件损坏 {0}".F((Database as SQLite).FileName), ex);
            }
        }

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
        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>新增行的自动编号</returns>
        public override Int64 InsertAndGetIdentity(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            sql += ";Select last_insert_rowid() newid";
            return ExecuteScalar<Int64>(CreateCommand(sql, type, ps));
        }
        #endregion

        #region 事务
        public override Int32 Commit()
        {
            lock (Database)
            {
                return base.Commit();
            }
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
            catch { }

            rs += Execute("PRAGMA auto_vacuum = 1");
            rs += Execute("VACUUM");

            return rs;
        }
        #endregion
    }

    /// <summary>SQLite元数据</summary>
    class SQLiteMetaData : FileDbMetaData
    {
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
            if ((Database as SQLite).IsMemoryDatabase)
            {
                return memoryTables.Where(t => names.Contains(t.TableName)).ToList();
            }

            var dt = GetSchema(_.Tables, null);
            if (dt?.Rows == null || dt.Rows.Count < 1) return null;

            // 默认列出所有字段
            var rows = dt.Select("TABLE_TYPE='table'");
            if (rows == null || rows.Length < 1) return null;

            return GetTables(rows, names);
        }

        protected override void FixField(IDataColumn field, DataRow dr)
        {
            base.FixField(field, dr);

            // 如果数据库里面是integer或者autoincrement，识别类型是Int64，又是自增，则改为Int32，保持与大多数数据库的兼容
            if (field.Identity && field.DataType == typeof(Int64) && field.RawType.EqualIgnoreCase("integer", "autoincrement"))
            {
                field.DataType = typeof(Int32);
            }

            if (field.DataType == null)
            {
                if (field.RawType.EqualIgnoreCase("varchar2", "nvarchar2")) field.DataType = typeof(String);
            }
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

        protected override DataRow[] FindDataType(IDataColumn field, String typeName, Boolean? isLong)
        {
            var drs = base.FindDataType(field, typeName, isLong);
            if (drs != null && drs.Length > 1)
            {
                // 字符串
                if (typeName == typeof(String).FullName)
                {
                    foreach (var dr in drs)
                    {
                        var name = GetDataRowValue<String>(dr, "TypeName");
                        if (name == "nvarchar" && field.Length <= Database.LongTextLength)
                            return new DataRow[] { dr };
                        else if (name == "ntext" && field.Length > Database.LongTextLength)
                            return new DataRow[] { dr };
                    }
                    foreach (var dr in drs)
                    {
                        var name = GetDataRowValue<String>(dr, "TypeName");
                        if (name == "varchar" && field.Length <= Database.LongTextLength)
                            return new DataRow[] { dr };
                        else if (name == "text" && field.Length > Database.LongTextLength)
                            return new DataRow[] { dr };
                    }
                }
            }
            else
            {
                // 字符串
                if (typeName.IndexOf("int", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var name = typeName.ToLower();
                    if (name == "int16")
                        name = "smallint";
                    else if (name == "int32")
                        name = "int";
                    else if (name == "int64")
                        name = "bigint";

                    if (name != typeName.ToLower()) return base.FindDataType(field, name, isLong);
                }
            }
            return drs;
        }

        protected override String GetFieldConstraints(IDataColumn field, Boolean onlyDefine)
        {
            String str = null;

            //Boolean b = field.PrimaryKey;
            // SQLite要求自增必须是主键
            if (field.Identity && !field.PrimaryKey)
            {
                // 取消所有主键
                field.Table.Columns.ForEach(dc => dc.PrimaryKey = false);

                // 自增字段作为主键
                field.PrimaryKey = true;
            }
            //try
            {
                str = base.GetFieldConstraints(field, onlyDefine);
            }
            //finally { if (field.Identity)field.PrimaryKey = b; }

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
        /// <param name="bakfile"></param>
        public void Backup(String bakfile)
        {
            bakfile = bakfile.GetFullPath().EnsureDirectory();

            WriteLog("{0}备份SQLite数据库{1}到{2}", Database.ConnName, (Database as SQLite).FileName, bakfile);

            var sw = new Stopwatch();
            sw.Start();

            // 删除已有文件
            if (File.Exists(bakfile)) File.Delete(bakfile);

            using (var session = Database.CreateSession())
            using (var conn = Database.Factory.CreateConnection())
            {
                session.Open();

                conn.ConnectionString = "Data Source={0}".F(bakfile);
                conn.Open();

                //var method = conn.GetType().GetMethodEx("BackupDatabase");
                // 借助BackupDatabase函数可以实现任意两个SQLite之间倒数据，包括内存数据库
                session.Conn.Invoke("BackupDatabase", conn, "main", "main", -1, null, 0);
            }

            // 压缩
            WriteLog("备份文件大小：{0:n0}字节", bakfile.AsFile().Length);
            if (bakfile.EndsWithIgnoreCase(".zip"))
            {
                //var rnd = new Random();
                var tmp = Path.GetDirectoryName(bakfile).CombinePath(Rand.Next() + ".tmp");
                File.Move(bakfile, tmp);
                tmp.AsFile().Compress(bakfile);
                File.Delete(tmp);
                WriteLog("压缩后大小：{0:n0}字节", bakfile.AsFile().Length);
            }

            sw.Stop();
            WriteLog("备份完成，耗时{0:n0}ms", sw.ElapsedMilliseconds);
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

            sb.AppendFormat(" On {0} (", FormatName(index.Table.TableName));
            for (var i = 0; i < index.Columns.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(FormatName(index.Columns[i]));
                //else
                //    sb.AppendFormat("{0} {1}", FormatKeyWord(index.Columns[i].Name), isAscs[i].Value ? "Asc" : "Desc");
            }
            //foreach (var item in index.Columns)
            //{
            //    sb.Append(FormatName(item));
            //    sb.Append(", ");
            //}
            //sb.Remove(sb.Length - 2, 2);
            sb.Append(")");

            return sb.ToString();
        }

        /// <summary>删除索引方法</summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public override String DropIndexSQL(IDataIndex index)
        {
            return String.Format("Drop Index {0}", FormatName(index.Name));
        }

        protected override String CheckColumnsChange(IDataTable entitytable, IDataTable dbtable, NegativeSetting setting)
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

            //String sql = base.CheckColumnsChange(entitytable, dbtable, onlySql);
            // 把onlySql设为true，让基类只产生语句而不执行
            var set = new NegativeSetting()
            {
                CheckOnly = true,
                NoDelete = setting.NoDelete
            };
            var sql = base.CheckColumnsChange(entitytable, dbtable, set);
            if (String.IsNullOrEmpty(sql)) return sql;

            // 只有修改字段、删除字段需要重建表
            if (!sql.Contains("Alter Column") && !sql.Contains("Drop Column"))
            {
                if (!setting.CheckOnly) Database.CreateSession().Execute(sql);
                return sql;
            }

            var sql2 = sql;

            sql = ReBuildTable(entitytable, dbtable);
            if (String.IsNullOrEmpty(sql) || setting.CheckOnly) return sql;

            // 输出日志，说明重建表的理由
            WriteLog("SQLite需要重建表，因无法执行：{0}", sql2);

            var flag = true;
            // 如果设定不允许删
            if (setting.NoDelete)
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
            if (flag) Database.CreateSession().Execute(sql);

            return sql;
        }
        #endregion

        #region 表和字段备注
        public override String AddTableDescriptionSQL(IDataTable table)
        {
            // 返回Empty，告诉反向工程，该数据库类型不支持该功能，请不要输出日志
            return String.Empty;
        }

        public override String DropTableDescriptionSQL(IDataTable table)
        {
            return String.Empty;
        }

        public override String AddColumnDescriptionSQL(IDataColumn field)
        {
            return String.Empty;
        }

        public override String DropColumnDescriptionSQL(IDataColumn field)
        {
            return String.Empty;
        }
        #endregion

        #region 反向工程
        private List<IDataTable> memoryTables = new List<IDataTable>();
        /// <summary>已重载。因为内存数据库无法检测到架构，不知道表是否已存在，所以需要自己维护</summary>
        /// <param name="entitytable"></param>
        /// <param name="dbtable"></param>
        /// <param name="setting"></param>
        protected override void CheckTable(IDataTable entitytable, IDataTable dbtable, NegativeSetting setting)
        {
            if (dbtable == null && (Database as SQLite).IsMemoryDatabase)
            {
                if (memoryTables.Any(t => t.TableName.EqualIgnoreCase(entitytable.TableName))) return;

                memoryTables.Add(entitytable);
            }

            base.CheckTable(entitytable, dbtable, setting);
        }

        public override String CompactDatabaseSQL() { return "VACUUM"; }
        #endregion
    }
}