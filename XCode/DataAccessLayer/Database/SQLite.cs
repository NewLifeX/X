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
        /// <summary>
        /// 返回数据库类型。
        /// </summary>
        public override DatabaseType DbType
        {
            get { return DatabaseType.SQLite; }
        }

        private static DbProviderFactory _dbProviderFactory;
        /// <summary>
        /// 提供者工厂
        /// </summary>
        static DbProviderFactory dbProviderFactory
        {
            get
            {
                //if (_dbProviderFactory == null) _dbProviderFactory = DbProviderFactories.GetFactory("System.Data.OracleClient");
                if (_dbProviderFactory == null) _dbProviderFactory = GetProviderFactory("System.Data.SQLite.dll", "System.Data.SQLite.SQLiteFactory");

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
            return new SQLiteSession();
        }

        /// <summary>
        /// 创建元数据对象
        /// </summary>
        /// <returns></returns>
        protected override IMetaData OnCreateMetaData()
        {
            return new SQLiteMetaData();
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

        protected override string ReservedWordsStr
        {
            get { return "ALL,ALTER,AND,AS,AUTOINCREMENT,BETWEEN,BY,CASE,CHECK,COLLATE,COMMIT,CONSTRAINT,CREATE,CROSS,DEFAULT,DEFERRABLE,DELETE,DISTINCT,DROP,ELSE,ESCAPE,EXCEPT,FOREIGN,FROM,FULL,GROUP,HAVING,IN,INDEX,INNER,INSERT,INTERSECT,INTO,IS,ISNULL,JOIN,LEFT,LIMIT,NATURAL,NOT,NOTNULL,NULL,ON,OR,ORDER,OUTER,PRIMARY,REFERENCES,RIGHT,ROLLBACK,SELECT,SET,TABLE,THEN,TO,TRANSACTION,UNION,UNIQUE,UPDATE,USING,VALUES,WHEN,WHERE"; }
        }

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

            if (keyWord.StartsWith("[") && keyWord.EndsWith("]")) return keyWord;

            return String.Format("[{0}]", keyWord);
            //return keyWord;
        }

        public override string FormatValue(XField field, object value)
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
        #region 基本方法 查询/执行
        //private ReadWriteLock _lock = null;
        //private ReadWriteLock rwLock
        //{
        //    get
        //    {
        //        // 以文件名为键创建读写锁，表示多线程将会争夺文件的写入操作
        //        return _lock ?? (_lock = ReadWriteLock.Create(FileName));
        //    }
        //}

        //public override DataSet Query(string sql)
        //{
        //    rwLock.AcquireRead();
        //    try
        //    {
        //        return base.Query(sql);
        //    }
        //    finally
        //    {
        //        rwLock.ReleaseRead();
        //    }
        //}

        //public override DataSet Query(DbCommand cmd)
        //{
        //    rwLock.AcquireRead();
        //    try
        //    {
        //        return base.Query(cmd);
        //    }
        //    finally
        //    {
        //        rwLock.ReleaseRead();
        //    }
        //}

        /// <summary>
        /// 文件锁定重试次数
        /// </summary>
        const Int32 RetryTimes = 5;

        TResult Retry<TArg, TResult>(Func<TArg, TResult> func, TArg arg)
        {
            //! 如果异常是文件锁定，则重试
            for (int i = 0; i < RetryTimes; i++)
            {
                //rwLock.AcquireWrite();
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
                //finally
                //{
                //    rwLock.ReleaseWrite();
                //}
            }
            return default(TResult);
        }

        /// <summary>
        /// 已重载。增加锁
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public override Int32 Execute(string sql)
        {
            return Retry<String, Int32>(base.Execute, sql);

            ////! 如果异常是文件锁定，则重试
            //for (int i = 0; i < RetryTimes; i++)
            //{
            //    //rwLock.AcquireWrite();
            //    try
            //    {
            //        return base.Execute(sql);
            //    }
            //    catch (Exception ex)
            //    {
            //        if (i >= RetryTimes - 1) throw;

            //        if (ex.Message == "The database file is locked") continue;

            //        throw;
            //    }
            //    //finally
            //    //{
            //    //    rwLock.ReleaseWrite();
            //    //}
            //}
            //return -1;
        }

        /// <summary>
        /// 已重载。增加锁
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public override Int32 Execute(DbCommand cmd)
        {
            return Retry<DbCommand, Int32>(base.Execute, cmd);

            ////! 如果异常是文件锁定，则重试
            //for (int i = 0; i < RetryTimes; i++)
            //{
            //    rwLock.AcquireWrite();
            //    try
            //    {
            //        return base.Execute(cmd);
            //    }
            //    catch (Exception ex)
            //    {
            //        if (i >= RetryTimes - 1) throw;

            //        if (ex.Message == "The database file is locked") continue;

            //        throw;
            //    }
            //    finally
            //    {
            //        rwLock.ReleaseWrite();
            //    }
            //}
            //return -1;
        }

        /// <summary>
        /// 执行插入语句并返回新增行的自动编号
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>新增行的自动编号</returns>
        public override Int64 InsertAndGetIdentity(string sql)
        {
            return Retry<String, Int64>(delegate(String sql2)
            {
                return Int64.Parse(ExecuteScalar(sql2 + ";Select last_insert_rowid() newid").ToString());
            }, sql);

            ////! 如果异常是文件锁定，则重试
            //for (int i = 0; i < RetryTimes; i++)
            //{
            //    rwLock.AcquireWrite();
            //    try
            //    {
            //        ExecuteTimes++;
            //        sql = sql + ";Select last_insert_rowid() newid";
            //        if (Debug) WriteLog(sql);
            //        try
            //        {
            //            DbCommand cmd = PrepareCommand();
            //            cmd.CommandText = sql;
            //            Object obj = cmd.ExecuteScalar();
            //            if (obj == null) return 0;

            //            return Int64.Parse(obj.ToString());
            //        }
            //        catch (DbException ex)
            //        {
            //            throw OnException(ex, sql);
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        if (i >= RetryTimes - 1) throw;

            //        if (ex.Message == "The database file is locked") continue;

            //        throw;
            //    }
            //    finally
            //    {
            //        AutoClose();
            //        rwLock.ReleaseWrite();
            //    }
            //}
            //return -1;
        }
        #endregion
    }

    /// <summary>
    /// SQLite元数据
    /// </summary>
    class SQLiteMetaData : FileDbMetaData
    {
        #region 构架
        public override List<XTable> GetTables()
        {
            DataTable dt = GetSchema("Tables", null);
            if (dt == null || dt.Rows == null || dt.Rows.Count < 1) return null;

            // 默认列出所有字段
            DataRow[] rows = dt.Select("TABLE_TYPE='table'");
            return GetTables(rows);
        }

        protected override DataRow[] FindDataType(XField field, string typeName, bool? isLong)
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

        protected override string GetFieldType(XField field)
        {
            String typeName = base.GetFieldType(field);

            // 自增字段必须是integer
            if (field.Identity && typeName == "int") return "integer";

            return typeName;
        }

        protected override string GetFieldConstraints(XField field, Boolean onlyDefine)
        {
            String str = base.GetFieldConstraints(field, onlyDefine);

            if (field.Identity) str += " AUTOINCREMENT";

            return str;
        }
        #endregion

        #region 数据定义
        public override string AlterColumnSQL(XField field, XField oldfield)
        {
            // SQLite的自增将会被识别为64位，而实际应用一般使用32位，不需要修改
            if (field.DataType == typeof(Int32) && field.Identity &&
                oldfield.DataType == typeof(Int64) && oldfield.Identity)
                return String.Empty;

            return ReBuildTable(field.Table, field.Table.Fields, oldfield.Table.Fields);
        }

        public override string DropColumnSQL(XField field)
        {
            XTable table = field.Table;
            List<XField> list = new List<XField>(table.Fields.ToArray());
            if (list.Contains(field)) list.Remove(field);

            return ReBuildTable(table, list, table.Fields);
        }

        String ReBuildTable(XTable table, List<XField> newFields, List<XField> oldFields)
        {
            // 通过重建表的方式修改字段
            String tableName = table.Name;
            String tempTableName = "Temp_" + tableName + "_" + new Random((Int32)DateTime.Now.Ticks).Next(0, 100).ToString("000");
            tableName = FormatKeyWord(tableName);
            tempTableName = FormatKeyWord(tempTableName);

            // 每个分号后面故意加上空格，是为了让DbMetaData执行SQL时，不要按照分号加换行来拆分这个SQL语句
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("BEGIN TRANSACTION; ");
            sb.AppendFormat("Alter Table {0} Rename To {1};", tableName, tempTableName);
            sb.AppendLine("; ");
            sb.Append(CreateTableSQL(table));
            sb.AppendLine("; ");

            // 如果指定了新列和旧列，则构建两个集合
            if (newFields != null && newFields.Count > 0 && oldFields != null && oldFields.Count > 0)
            {
                StringBuilder sbName = new StringBuilder();
                StringBuilder sbValue = new StringBuilder();
                foreach (XField item in newFields)
                {
                    String name = item.Name;
                    XField field = oldFields.Find(f => f.Name == name);
                    if (field == null)
                    {
                        // 如果新增了不允许空的列，则处理一下默认值
                        if (!item.Nullable)
                        {
                            if (item.DataType == typeof(String))
                            {
                                if (sbName.Length > 0) sbName.Append(", ");
                                if (sbValue.Length > 0) sbValue.Append(", ");
                                sbName.Append(FormatKeyWord(name));
                                sbValue.Append("''");
                            }
                            else if (item.DataType == typeof(Int16) || item.DataType == typeof(Int32) || item.DataType == typeof(Int64) ||
                                item.DataType == typeof(Single) || item.DataType == typeof(Double) || item.DataType == typeof(Decimal))
                            {
                                if (sbName.Length > 0) sbName.Append(", ");
                                if (sbValue.Length > 0) sbValue.Append(", ");
                                sbName.Append(FormatKeyWord(name));
                                sbValue.Append("0");
                            }
                            else if (item.DataType == typeof(DateTime))
                            {
                                if (sbName.Length > 0) sbName.Append(", ");
                                if (sbValue.Length > 0) sbValue.Append(", ");
                                sbName.Append(FormatKeyWord(name));
                                sbValue.Append(Database.FormatDateTime(Database.DateTimeMin));
                            }
                        }
                    }
                    else
                    {
                        if (sbName.Length > 0) sbName.Append(", ");
                        if (sbValue.Length > 0) sbValue.Append(", ");
                        sbName.Append(FormatKeyWord(name));
                        sbValue.Append(FormatKeyWord(name));

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
        public override string AddTableDescriptionSQL(XTable table)
        {
            // 返回Empty，告诉反向工程，该数据库类型不支持该功能，请不要输出日志
            return String.Empty;
        }

        public override string DropTableDescriptionSQL(XTable table)
        {
            return String.Empty;
        }

        public override string AddColumnDescriptionSQL(XField field)
        {
            return String.Empty;
        }

        public override string DropColumnDescriptionSQL(XField field)
        {
            return String.Empty;
        }
        #endregion

        #region 默认值
        public override string AddDefaultSQL(XField field)
        {
            return String.Empty;
        }

        public override string DropDefaultSQL(XField field)
        {
            return String.Empty;
        }
        #endregion
    }
}