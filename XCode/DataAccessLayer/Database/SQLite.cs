using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web;
using NewLife.Threading;

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
        /// <param name="startRowIndex">开始行，0开始</param>
        /// <param name="maximumRows">最大返回行数</param>
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

        /// <summary>
        /// 已重写。获取分页
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="startRowIndex"></param>
        /// <param name="maximumRows"></param>
        /// <param name="keyColumn"></param>
        /// <returns></returns>
        public override string PageSplit(SelectBuilder builder, int startRowIndex, int maximumRows, string keyColumn)
        {
            return PageSplit(builder.ToString(), startRowIndex, maximumRows, keyColumn);
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
            if (String.IsNullOrEmpty(keyWord)) throw new ArgumentNullException("keyWord");

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
        private ReadWriteLock _lock = null;
        private ReadWriteLock rwLock
        {
            get
            {
                // 以文件名为键创建读写锁，表示多线程将会争夺文件的写入操作
                return _lock ?? (_lock = ReadWriteLock.Create(FileName));
            }
        }

        public override DataSet Query(string sql)
        {
            rwLock.AcquireRead();
            try
            {
                return base.Query(sql);
            }
            finally
            {
                rwLock.ReleaseRead();
            }
        }

        public override DataSet Query(DbCommand cmd)
        {
            rwLock.AcquireRead();
            try
            {
                return base.Query(cmd);
            }
            finally
            {
                rwLock.ReleaseRead();
            }
        }

        /// <summary>
        /// 文件锁定重试次数
        /// </summary>
        const Int32 RetryTimes = 2;

        /// <summary>
        /// 已重载。增加锁
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public override int Execute(string sql)
        {
            //! 如果异常是文件锁定，则重试
            for (int i = 0; i < RetryTimes; i++)
            {
                rwLock.AcquireWrite();
                try
                {
                    return base.Execute(sql);
                }
                catch (Exception ex)
                {
                    if (i >= RetryTimes - 1) throw;

                    if (ex.Message == "The database file is locked") continue;

                    throw;
                }
                finally
                {
                    rwLock.ReleaseWrite();
                }
            }
            return -1;
        }

        /// <summary>
        /// 已重载。增加锁
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public override int Execute(DbCommand cmd)
        {
            //! 如果异常是文件锁定，则重试
            for (int i = 0; i < RetryTimes; i++)
            {
                rwLock.AcquireWrite();
                try
                {
                    return base.Execute(cmd);
                }
                catch (Exception ex)
                {
                    if (i >= RetryTimes - 1) throw;

                    if (ex.Message == "The database file is locked") continue;

                    throw;
                }
                finally
                {
                    rwLock.ReleaseWrite();
                }
            }
            return -1;
        }

        /// <summary>
        /// 执行插入语句并返回新增行的自动编号
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>新增行的自动编号</returns>
        public override Int64 InsertAndGetIdentity(string sql)
        {
            //! 如果异常是文件锁定，则重试
            for (int i = 0; i < RetryTimes; i++)
            {
                rwLock.AcquireWrite();
                try
                {
                    ExecuteTimes++;
                    sql = sql + ";Select last_insert_rowid() newid";
                    if (Debug) WriteLog(sql);
                    try
                    {
                        DbCommand cmd = PrepareCommand();
                        cmd.CommandText = sql;
                        Object obj = cmd.ExecuteScalar();
                        if (obj == null) return 0;

                        return Int64.Parse(obj.ToString());
                    }
                    catch (DbException ex)
                    {
                        throw OnException(ex, sql);
                    }
                }
                catch (Exception ex)
                {
                    if (i >= RetryTimes - 1) throw;

                    if (ex.Message == "The database file is locked") continue;

                    throw;
                }
                finally
                {
                    AutoClose();
                    rwLock.ReleaseWrite();
                }
            }
            return -1;
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
        ///// <summary>
        ///// 设置数据定义模式
        ///// </summary>
        ///// <param name="schema"></param>
        ///// <param name="values"></param>
        ///// <returns></returns>
        //public override object SetSchema(DDLSchema schema, object[] values)
        //{
        //    Object obj = null;
        //    switch (schema)
        //    {
        //        //case DDLSchema.CreateDatabase:
        //        //    CreateDatabase();
        //        //    return null;
        //        //case DDLSchema.DropDatabase:
        //        //    //首先关闭数据库
        //        //    Database.CreateSession().Close();

        //        //    if (File.Exists(FileName)) File.Delete(FileName);
        //        //    return null;
        //        //case DDLSchema.DatabaseExist:
        //        //    return File.Exists(FileName);
        //        //case DDLSchema.CreateTable:
        //        //    obj = base.SetSchema(DDLSchema.CreateTable, values);
        //        //    //XTable table = values[0] as XTable;
        //        //    //if (!String.IsNullOrEmpty(table.Description)) AddTableDescription(table.Name, table.Description);
        //        //    //foreach (XField item in table.Fields)
        //        //    //{
        //        //    //    if (!String.IsNullOrEmpty(item.Description)) AddColumnDescription(table.Name, item.Name, item.Description);
        //        //    //}
        //        //    return obj;
        //        //case DDLSchema.DropTable:
        //        //    break;
        //        //case DDLSchema.TableExist:
        //        //    DataTable dt = GetSchema("Tables", new String[] { null, null, (String)values[0], "TABLE" });
        //        //    if (dt == null || dt.Rows == null || dt.Rows.Count < 1) return false;
        //        //    return true;
        //        //case DDLSchema.AddTableDescription:
        //        //    return AddTableDescription((String)values[0], (String)values[1]);
        //        //case DDLSchema.DropTableDescription:
        //        //    return DropTableDescription((String)values[0]);
        //        case DDLSchema.AddColumn:
        //            obj = base.SetSchema(DDLSchema.AddColumn, values);
        //            //AddColumnDescription((String)values[0], ((XField)values[1]).Name, ((XField)values[1]).Description);
        //            return obj;
        //        case DDLSchema.AlterColumn:
        //            break;
        //        case DDLSchema.DropColumn:
        //            break;
        //        //case DDLSchema.AddColumnDescription:
        //        //    return AddColumnDescription((String)values[0], (String)values[1], (String)values[2]);
        //        //case DDLSchema.DropColumnDescription:
        //        //    return DropColumnDescription((String)values[0], (String)values[1]);
        //        //case DDLSchema.AddDefault:
        //        //    return AddDefault((String)values[0], (String)values[1], (String)values[2]);
        //        //case DDLSchema.DropDefault:
        //        //    return DropDefault((String)values[0], (String)values[1]);
        //        default:
        //            break;
        //    }
        //    return base.SetSchema(schema, values);
        //}

        public override string AlterColumnSQL(XField field)
        {
            return null;
        }

        //public override string FieldClause(XField field, bool onlyDefine)
        //{
        //    StringBuilder sb = new StringBuilder();

        //    //字段名
        //    sb.AppendFormat("{0} ", FormatKeyWord(field.Name));

        //    //类型
        //    TypeCode tc = Type.GetTypeCode(field.DataType);
        //    switch (tc)
        //    {
        //        case TypeCode.Boolean:
        //            sb.Append("BOOLEAN");
        //            break;
        //        case TypeCode.Byte:
        //            sb.Append("byte");
        //            break;
        //        case TypeCode.Char:
        //            sb.Append("bit");
        //            break;
        //        case TypeCode.DBNull:
        //            break;
        //        case TypeCode.DateTime:
        //            sb.Append("datetime");
        //            break;
        //        case TypeCode.Decimal:
        //            sb.AppendFormat("decimal");
        //            break;
        //        case TypeCode.Double:
        //            sb.Append("double");
        //            break;
        //        case TypeCode.Empty:
        //            break;
        //        case TypeCode.Int16:
        //        case TypeCode.UInt16:
        //        //sb.Append("smallint");
        //        //break;
        //        case TypeCode.Int32:
        //        case TypeCode.UInt32:
        //        //sb.Append("interger");
        //        //break;
        //        case TypeCode.Int64:
        //        case TypeCode.UInt64:
        //            sb.Append("INTEGER");
        //            break;
        //        case TypeCode.Object:
        //            break;
        //        case TypeCode.SByte:
        //            sb.Append("byte");
        //            break;
        //        case TypeCode.Single:
        //            sb.Append("float");
        //            break;
        //        case TypeCode.String:
        //            Int32 len = field.Length;
        //            if (len < 1) len = 50;
        //            if (len > 4000)
        //                sb.Append("TEXT");
        //            else
        //                sb.AppendFormat("nvarchar({0})", len);
        //            break;
        //        default:
        //            break;
        //    }

        //    if (field.PrimaryKey)
        //    {
        //        sb.Append(" primary key");

        //        // 如果不加这个关键字，SQLite会利用最大值加1的方式，所以，最大行被删除后，它的编号将会被重用
        //        if (onlyDefine && field.Identity) sb.Append(" autoincrement");
        //    }
        //    else
        //    {
        //        //是否为空
        //        //if (!field.Nullable) sb.Append(" NOT NULL");
        //        if (field.Nullable)
        //            sb.Append(" NULL");
        //        else
        //        {
        //            sb.Append(" NOT NULL");
        //        }
        //    }

        //    //默认值
        //    if (onlyDefine && !String.IsNullOrEmpty(field.Default))
        //    {
        //        if (tc == TypeCode.String)
        //            sb.AppendFormat(" DEFAULT '{0}'", field.Default);
        //        else if (tc == TypeCode.DateTime)
        //        {
        //            String d = field.Default;
        //            //if (String.Equals(d, "getdate()", StringComparison.OrdinalIgnoreCase)) d = "now()";
        //            if (String.Equals(d, "getdate()", StringComparison.OrdinalIgnoreCase)) d = Database.DateTimeNow;
        //            if (String.Equals(d, "now()", StringComparison.OrdinalIgnoreCase)) d = Database.DateTimeNow;
        //            sb.AppendFormat(" DEFAULT {0}", d);
        //        }
        //        else
        //            sb.AppendFormat(" DEFAULT {0}", field.Default);
        //    }

        //    return sb.ToString();
        //}
        #endregion

        #region 表和字段备注
        //public Boolean AddTableDescription(String tablename, String description)
        //{
        //    try
        //    {
        //        using (ADOTabe table = new ADOTabe(ConnectionString, FileName, tablename))
        //        {
        //            table.Description = description;
        //            return true;
        //        }
        //    }
        //    catch { return false; }
        //}

        //public Boolean DropTableDescription(String tablename)
        //{
        //    return AddTableDescription(tablename, null);
        //}

        //public Boolean AddColumnDescription(String tablename, String columnname, String description)
        //{
        //    try
        //    {
        //        using (ADOTabe table = new ADOTabe(ConnectionString, FileName, tablename))
        //        {
        //            if (table.Supported && table.Columns != null)
        //            {
        //                foreach (ADOColumn item in table.Columns)
        //                {
        //                    if (item.Name == columnname)
        //                    {
        //                        item.Description = description;
        //                        return true;
        //                    }
        //                }
        //            }
        //            return false;
        //        }
        //    }
        //    catch { return false; }
        //}

        //public Boolean DropColumnDescription(String tablename, String columnname)
        //{
        //    return AddColumnDescription(tablename, columnname, null);
        //}
        #endregion

        #region 默认值
        //public virtual Boolean AddDefault(String tablename, String columnname, String value)
        //{
        //    try
        //    {
        //        using (ADOTabe table = new ADOTabe(ConnectionString, FileName, tablename))
        //        {
        //            if (table.Supported && table.Columns != null)
        //            {
        //                foreach (ADOColumn item in table.Columns)
        //                {
        //                    if (item.Name == columnname)
        //                    {
        //                        item.Default = value;
        //                        return true;
        //                    }
        //                }
        //            }
        //            return false;
        //        }
        //    }
        //    catch { return false; }
        //}

        //public virtual Boolean DropDefault(String tablename, String columnname)
        //{
        //    return AddDefault(tablename, columnname, null);
        //}
        #endregion
    }
}
