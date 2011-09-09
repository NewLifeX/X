using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading;
using NewLife.Reflection;

namespace XCode.DataAccessLayer
{
    class SQLite : FileDbBase
    {
        #region 属性
        /// <summary>返回数据库类型。</summary>
        public override DatabaseType DbType { get { return DatabaseType.SQLite; } }

        private static DbProviderFactory _dbProviderFactory;
        /// <summary>提供者工厂</summary>
        static DbProviderFactory dbProviderFactory
        {
            get
            {
                if (_dbProviderFactory == null)
                {
                    lock (typeof(SQLite))
                    {
                        if (_dbProviderFactory == null) _dbProviderFactory = GetProviderFactory("System.Data.SQLite.dll", "System.Data.SQLite.SQLiteFactory");
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

        /// <summary>是否内存数据库</summary>
        public Boolean IsMemoryDatabase { get { return String.Equals(FileName, MemoryDatabase, StringComparison.OrdinalIgnoreCase); } }

        static readonly String MemoryDatabase = ":memory:";

        protected internal override string ResoleFile(string file)
        {
            if (String.IsNullOrEmpty(file) || String.Equals(file, MemoryDatabase, StringComparison.OrdinalIgnoreCase)) return MemoryDatabase;

            return base.ResoleFile(file);
        }

        protected internal override void OnSetConnectionString(XDbConnectionStringBuilder builder)
        {
            base.OnSetConnectionString(builder);

            // 优化SQLite，如果原始字符串里面没有这些参数，就设置这些参数
            if (!builder.ContainsKey("Pooling")) builder["Pooling"] = "true";
            if (!builder.ContainsKey("Cache Size")) builder["Cache Size"] = "50000";
            if (!builder.ContainsKey("Synchronous")) builder["Synchronous"] = "Off";
            if (!builder.ContainsKey("Journal Mode")) builder["Journal Mode"] = "Memory";
        }
        #endregion

        #region 方法
        /// <summary>
        /// 创建数据库会话
        /// </summary>
        /// <returns></returns>
        protected override IDbSession OnCreateSession() { return new SQLiteSession(); }

        /// <summary>
        /// 创建元数据对象
        /// </summary>
        /// <returns></returns>
        protected override IMetaData OnCreateMetaData() { return new SQLiteMetaData(); }
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
                sql = String.Format("{0} limit {1}, {2}", sql, startRowIndex, maximumRows);
            return sql;
        }
        #endregion

        #region 数据库特性
        /// <summary>
        /// 当前时间函数
        /// </summary>
        public override String DateTimeNow { get { return "CURRENT_TIMESTAMP"; } }

        /// <summary>
        /// 最小时间
        /// </summary>
        public override DateTime DateTimeMin { get { return DateTime.MinValue; } }

        /// <summary>
        /// 格式化时间为SQL字符串
        /// </summary>
        /// <param name="dateTime">时间值</param>
        /// <returns></returns>
        public override String FormatDateTime(DateTime dateTime) { return String.Format("'{0:yyyy-MM-dd HH:mm:ss}'", dateTime); }

        /// <summary>
        /// 格式化关键字
        /// </summary>
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

        public override string FormatValue(IDataColumn field, object value)
        {
            if (field.DataType == typeof(Byte[]))
            {
                Byte[] bts = (Byte[])value;
                if (bts == null || bts.Length < 1) return "0x0";

                return "X'" + BitConverter.ToString(bts).Replace("-", null) + "'";
            }

            return base.FormatValue(field, value);
        }

        #endregion
    }

    /// <summary>
    /// SQLite数据库
    /// </summary>
    internal class SQLiteSession : FileDbSession
    {
        #region 方法
        protected override void CreateDatabase()
        {
            // 内存数据库不需要创建
            if ((Database as SQLite).IsMemoryDatabase) return;

            base.CreateDatabase();
        }
        #endregion

        #region 基本方法 查询/执行
        /// <summary>
        /// 文件锁定重试次数
        /// </summary>
        const Int32 RetryTimes = 5;

        TResult Retry<TArg, TResult>(Func<TArg, TResult> func, TArg arg)
        {
            //! 如果异常是文件锁定，则重试
            for (int i = 0; i < RetryTimes; i++)
            {
                try
                {
                    return func(arg);
                }
                catch (Exception ex)
                {
                    if (i >= RetryTimes - 1) throw;

                    if (ex.Message == "The database file is locked")
                    {
                        Thread.Sleep(300);
                        continue;
                    }

                    throw;
                }
            }
            return default(TResult);
        }

        /// <summary>
        /// 已重载。增加锁
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public override Int32 Execute(string sql) { return Retry<String, Int32>(base.Execute, sql); }

        /// <summary>
        /// 已重载。增加锁
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public override Int32 Execute(DbCommand cmd) { return Retry<DbCommand, Int32>(base.Execute, cmd); }

        /// <summary>
        /// 执行插入语句并返回新增行的自动编号
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>新增行的自动编号</returns>
        public override Int64 InsertAndGetIdentity(string sql)
        {
            return Retry<String, Int64>(delegate(String sql2)
            {
                return ExecuteScalar<Int64>(sql2 + ";Select last_insert_rowid() newid");
            }, sql);
        }
        #endregion
    }

    /// <summary>
    /// SQLite元数据
    /// </summary>
    class SQLiteMetaData : FileDbMetaData
    {
        #region 构架
        public override List<IDataTable> GetTables()
        {
            DataTable dt = GetSchema("Tables", null);
            if (dt == null || dt.Rows == null || dt.Rows.Count < 1) return null;

            // 默认列出所有字段
            DataRow[] rows = dt.Select("TABLE_TYPE='table'");
            return GetTables(rows);
        }

        protected override DataRow[] FindDataType(IDataColumn field, string typeName, bool? isLong)
        {
            if (!String.IsNullOrEmpty(typeName))
            {
                Type type = Type.GetType(typeName);
                if (type != null)
                {
                    switch (Type.GetTypeCode(type))
                    {
                        case TypeCode.UInt16:
                            type = typeof(Int16);
                            break;
                        case TypeCode.UInt32:
                            type = typeof(Int32);
                            break;
                        case TypeCode.UInt64:
                            type = typeof(Int64);
                            break;
                        default:
                            break;
                    }
                    typeName = type.FullName;
                }
            }

            return base.FindDataType(field, typeName, isLong);
        }

        protected override string GetFieldType(IDataColumn field)
        {
            String typeName = base.GetFieldType(field);

            // 自增字段必须是integer
            if (field.Identity && typeName == "int") return "integer";

            return typeName;
        }

        protected override string GetFieldConstraints(IDataColumn field, Boolean onlyDefine)
        {
            String str = base.GetFieldConstraints(field, onlyDefine);

            if (field.Identity) str += " AUTOINCREMENT";

            return str;
        }
        #endregion

        #region 数据定义
        public override string AlterColumnSQL(IDataColumn field, IDataColumn oldfield)
        {
            // SQLite的自增将会被识别为64位，而实际应用一般使用32位，不需要修改
            if (field.DataType == typeof(Int32) && field.Identity &&
                oldfield.DataType == typeof(Int64) && oldfield.Identity)
                return String.Empty;

            // 重新拿字段，得到最新的
            List<IDataColumn> list = GetFields(field.Table);
            Int32 n = -1;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Name == oldfield.Name)
                {
                    n = i;
                    break;
                }
            }
            if (n < 0) return null;

            IDataColumn[] oldColumns = list.ToArray();
            list[n] = field;
            IDataColumn[] newColumns = list.ToArray();

            return ReBuildTable(field.Table, newColumns, oldColumns);
        }

        public override string DropColumnSQL(IDataColumn field)
        {
            IDataTable table = field.Table;
            //List<IDataColumn> list = new List<IDataColumn>(table.Columns);
            //if (list.Contains(field)) list.Remove(field);

            // 重新拿字段，得到最新的
            List<IDataColumn> list = GetFields(table);
            Int32 n = -1;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Name == field.Name)
                {
                    n = i;
                    break;
                }
            }
            if (n < 0) return null;

            IDataColumn[] oldColumns = list.ToArray();
            list.RemoveAt(n);
            IDataColumn[] newColumns = list.ToArray();

            return ReBuildTable(table, newColumns, oldColumns);
        }

        /// <summary>
        /// 删除索引方法
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public override string DropIndexSQL(IDataIndex index)
        {
            return String.Format("Drop Index {0}", FormatName(index.Name));
        }

        String ReBuildTable(IDataTable table, IDataColumn[] newFields, IDataColumn[] oldFields)
        {
            // 通过重建表的方式修改字段
            String tableName = table.Name;
            String tempTableName = "Temp_" + tableName + "_" + new Random((Int32)DateTime.Now.Ticks).Next(0, 100).ToString("000");
            tableName = FormatName(tableName);
            tempTableName = FormatName(tempTableName);

            // 每个分号后面故意加上空格，是为了让DbMetaData执行SQL时，不要按照分号加换行来拆分这个SQL语句
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("BEGIN TRANSACTION; ");
            sb.AppendFormat("Alter Table {0} Rename To {1};", tableName, tempTableName);
            sb.AppendLine("; ");
            sb.Append(CreateTableSQL(table));
            sb.AppendLine("; ");

            // 如果指定了新列和旧列，则构建两个集合
            if (newFields != null && newFields.Length > 0 && oldFields != null && oldFields.Length > 0)
            {
                StringBuilder sbName = new StringBuilder();
                StringBuilder sbValue = new StringBuilder();
                foreach (IDataColumn item in newFields)
                {
                    String name = item.Name;
                    //IDataColumn field = oldFields.Find(f => f.Name == name);
                    IDataColumn field = null;
                    foreach (IDataColumn dc in oldFields)
                    {
                        if (dc.Name == name)
                        {
                            field = dc;
                            break;
                        }
                    }
                    if (field == null)
                    {
                        // 如果新增了不允许空的列，则处理一下默认值
                        if (!item.Nullable)
                        {
                            if (item.DataType == typeof(String))
                            {
                                if (sbName.Length > 0) sbName.Append(", ");
                                if (sbValue.Length > 0) sbValue.Append(", ");
                                sbName.Append(FormatName(name));
                                sbValue.Append("''");
                            }
                            else if (item.DataType == typeof(Int16) || item.DataType == typeof(Int32) || item.DataType == typeof(Int64) ||
                                item.DataType == typeof(Single) || item.DataType == typeof(Double) || item.DataType == typeof(Decimal))
                            {
                                if (sbName.Length > 0) sbName.Append(", ");
                                if (sbValue.Length > 0) sbValue.Append(", ");
                                sbName.Append(FormatName(name));
                                sbValue.Append("0");
                            }
                            else if (item.DataType == typeof(DateTime))
                            {
                                if (sbName.Length > 0) sbName.Append(", ");
                                if (sbValue.Length > 0) sbValue.Append(", ");
                                sbName.Append(FormatName(name));
                                sbValue.Append(Database.FormatDateTime(Database.DateTimeMin));
                            }
                        }
                    }
                    else
                    {
                        if (sbName.Length > 0) sbName.Append(", ");
                        if (sbValue.Length > 0) sbValue.Append(", ");
                        sbName.Append(FormatName(name));
                        sbValue.Append(FormatName(name));

                        // 处理字符串不允许空
                        if (item.DataType == typeof(String) && !item.Nullable) sbValue.Append("+''");
                    }
                }
                sb.AppendFormat("Insert Into {0}({2}) Select {3} From {1}", tableName, tempTableName, sbName.ToString(), sbValue.ToString());
            }
            else
            {
                sb.AppendFormat("Insert Into {0} Select * From {1}", tableName, tempTableName);
            }
            sb.AppendLine("; ");
            sb.AppendFormat("Drop Table {0}", tempTableName);
            sb.AppendLine("; ");
            sb.Append("COMMIT;");

            return sb.ToString();
        }
        #endregion

        #region 表和字段备注
        public override string AddTableDescriptionSQL(IDataTable table)
        {
            // 返回Empty，告诉反向工程，该数据库类型不支持该功能，请不要输出日志
            return String.Empty;
        }

        public override string DropTableDescriptionSQL(IDataTable table)
        {
            return String.Empty;
        }

        public override string AddColumnDescriptionSQL(IDataColumn field)
        {
            return String.Empty;
        }

        public override string DropColumnDescriptionSQL(IDataColumn field)
        {
            return String.Empty;
        }
        #endregion

        #region 默认值
        public override string AddDefaultSQL(IDataColumn field)
        {
            return String.Empty;
        }

        public override string DropDefaultSQL(IDataColumn field)
        {
            return String.Empty;
        }
        #endregion
    }
}