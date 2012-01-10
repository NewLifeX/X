using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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

        protected override string OnResolveFile(string file)
        {
            if (String.IsNullOrEmpty(file) || String.Equals(file, MemoryDatabase, StringComparison.OrdinalIgnoreCase)) return MemoryDatabase;

            return base.OnResolveFile(file);
        }

        protected override void OnSetConnectionString(XDbConnectionStringBuilder builder)
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

        /// <summary>字符串相加</summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public override String StringConcat(String left, String right) { return (!String.IsNullOrEmpty(left) ? left : "\'\'") + "||" + (!String.IsNullOrEmpty(right) ? right : "\'\'"); }
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

        ///// <summary>
        ///// 已重载。增加锁
        ///// </summary>
        ///// <param name="sql">SQL语句</param>
        ///// <param name="type">命令类型，默认SQL文本</param>
        ///// <param name="ps">命令参数</param>
        ///// <returns></returns>
        //public override Int32 Execute(string sql, CommandType type = CommandType.Text, params DbParameter[] ps) { return Retry<String, Int32>(base.Execute, sql); }

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
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>新增行的自动编号</returns>
        public override Int64 InsertAndGetIdentity(string sql, CommandType type = CommandType.Text, params DbParameter[] ps)
        {
            return Retry<String, Int64>(delegate(String sql2)
            {
                return ExecuteScalar<Int64>(sql2 + ";Select last_insert_rowid() newid", type, ps);
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
        protected override List<IDataTable> OnGetTables()
        {
            DataTable dt = GetSchema(_.Tables, null);
            if (dt == null || dt.Rows == null || dt.Rows.Count < 1) return null;

            // 默认列出所有字段
            DataRow[] rows = dt.Select("TABLE_TYPE='table'");
            return GetTables(rows);
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

        /// <summary>
        /// 删除索引方法
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public override string DropIndexSQL(IDataIndex index)
        {
            return String.Format("Drop Index {0}", FormatName(index.Name));
        }

        protected override string CheckColumnsChange(IDataTable entitytable, IDataTable dbtable, bool onlySql)
        {
            foreach (IDataColumn item in entitytable.Columns)
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
            String sql = base.CheckColumnsChange(entitytable, dbtable, true);
            if (String.IsNullOrEmpty(sql)) return sql;

            sql = ReBuildTable(entitytable, dbtable);
            if (!String.IsNullOrEmpty(sql)) Database.CreateSession().Execute(sql);

            return sql;
        }

        //protected override bool IsColumnChanged(IDataColumn entityColumn, IDataColumn dbColumn, IDatabase entityDb)
        //{
        //    //// SQLite的自增将会被识别为64位，而实际应用一般使用32位，不需要修改
        //    //if (entityColumn.DataType == typeof(Int32) && entityColumn.Identity &&
        //    //    dbColumn.DataType == typeof(Int64) && dbColumn.Identity)
        //    //{
        //    //    // 克隆一个，修改类型
        //    //    entityColumn = entityColumn.Clone(entityColumn.Table);
        //    //    entityColumn.DataType = typeof(Int64);
        //    //}

        //    if (!base.IsColumnChanged(entityColumn, dbColumn, entityDb)) return false;

        //    // 自增字段必须是主键
        //    if (entityColumn.Identity && !entityColumn.PrimaryKey)
        //    {
        //        // 取消所有主键
        //        entityColumn.Table.Columns.ForEach(dc => dc.PrimaryKey = false);

        //        // 自增字段作为主键
        //        entityColumn.PrimaryKey = true;
        //    }

        //    return true;
        //}
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