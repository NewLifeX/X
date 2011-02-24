using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using XCode.Exceptions;
using System.IO;

namespace XCode.DataAccessLayer
{
    class Oracle : RemoteDb
    {
        #region 属性
        /// <summary>
        /// 返回数据库类型。外部DAL数据库类请使用Other
        /// </summary>
        public override DatabaseType DbType
        {
            get { return DatabaseType.Oracle; }
        }

        private static DbProviderFactory _dbProviderFactory;
        /// <summary>
        /// 提供者工厂
        /// </summary>
        static DbProviderFactory dbProviderFactory
        {
            get
            {
                // 首先尝试使用Oracle.DataAccess
                if (_dbProviderFactory == null)
                {
                    try
                    {
                        _dbProviderFactory = GetProviderFactory("Oracle.DataAccess.dll", "Oracle.DataAccess.Client.OracleClientFactory");
                    }
                    catch (FileNotFoundException) { }
                    catch (Exception ex)
                    {
                        if (Debug) WriteLog(ex.ToString());
                    }
                }

                // 以下三种方式都可以加载，前两种只是为了减少对程序集的引用，第二种是为了避免第一种中没有注册
                if (_dbProviderFactory == null) _dbProviderFactory = DbProviderFactories.GetFactory("System.Data.OracleClient");
                if (_dbProviderFactory == null) _dbProviderFactory = GetProviderFactory("System.Data.OracleClient.dll", "System.Data.OracleClient.OracleClientFactory");
                //if (_dbProviderFactory == null) _dbProviderFactory = OracleClientFactory.Instance;

                return _dbProviderFactory;
            }
        }

        /// <summary>工厂</summary>
        public override DbProviderFactory Factory
        {
            //get { return OracleClientFactory.Instance; }
            get { return dbProviderFactory; }
        }

        private String _UserID;
        /// <summary>
        /// 用户名UserID
        /// </summary>
        public String UserID
        {
            get
            {
                if (_UserID != null) return _UserID;
                _UserID = String.Empty;

                String connStr = ConnectionString;
                if (String.IsNullOrEmpty(connStr)) return null;

                DbConnectionStringBuilder ocsb = Factory.CreateConnectionStringBuilder();
                ocsb.ConnectionString = connStr;

                if (ocsb.ContainsKey("User ID")) _UserID = (String)ocsb["User ID"];

                return _UserID;
            }
        }

        /// <summary>拥有者</summary>
        public override String Owner
        {
            get
            {
                // 利用null和Empty的区别来判断是否已计算
                if (base.Owner == null)
                {
                    base.Owner = UserID;
                    if (String.IsNullOrEmpty(base.Owner)) base.Owner = String.Empty;
                }
                return base.Owner;
            }
            set { base.Owner = value; }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 创建数据库会话
        /// </summary>
        /// <returns></returns>
        protected override IDbSession OnCreateSession()
        {
            return new OracleSession();
        }

        /// <summary>
        /// 创建元数据对象
        /// </summary>
        /// <returns></returns>
        protected override IMetaData OnCreateMetaData()
        {
            return new OracleMeta();
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
        public override String PageSplit(String sql, Int32 startRowIndex, Int32 maximumRows, String keyColumn)
        {
            // return base.Query(sql, startRowIndex, maximumRows, key);
            // 从第一行开始，不需要分页
            if (startRowIndex <= 0)
            {
                if (maximumRows < 1)
                    return sql;
                else
                    return String.Format("Select * From ({1}) XCode_Temp_a Where rownum<={0}", maximumRows + 1, sql);
            }
            if (maximumRows < 1)
                sql = String.Format("Select * From ({1}) XCode_Temp_a Where rownum>={0}", startRowIndex + 1, sql);
            else
                sql = String.Format("Select * From (Select XCode_Temp_a.*, rownum as my_rownum From ({1}) XCode_Temp_a Where rownum<={2}) XCode_Temp_b Where my_rownum>={0}", startRowIndex + 1, sql, startRowIndex + maximumRows);
            //sql = String.Format("Select * From ({1}) a Where rownum>={0} and rownum<={2}", startRowIndex, sql, startRowIndex + maximumRows - 1);
            return sql;
        }

        public override string PageSplit(SelectBuilder builder, int startRowIndex, int maximumRows, string keyColumn)
        {
            return PageSplit(builder.ToString(), startRowIndex, maximumRows, keyColumn);
        }
        #endregion

        #region 数据库特性
        /// <summary>
        /// 当前时间函数
        /// </summary>
        public override string DateTimeNow { get { return "sysdate"; } }

        /// <summary>
        /// 已重载。格式化时间
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public override string FormatDateTime(DateTime dateTime)
        {
            return String.Format("To_Date('{0}', 'YYYY-MM-DD HH24:MI:SS')", dateTime.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        /// <summary>
        /// 格式化关键字
        /// </summary>
        /// <param name="keyWord">表名</param>
        /// <returns></returns>
        public override String FormatKeyWord(String keyWord)
        {
            //return String.Format("\"{0}\"", keyWord);

            //if (String.IsNullOrEmpty(keyWord)) throw new ArgumentNullException("keyWord");
            if (String.IsNullOrEmpty(keyWord)) return keyWord;

            Int32 pos = keyWord.LastIndexOf(".");

            if (pos < 0) return "\"" + keyWord + "\"";

            String tn = keyWord.Substring(pos + 1);
            if (tn.StartsWith("\"")) return keyWord;

            return keyWord.Substring(0, pos + 1) + "\"" + tn + "\"";
        }

        public override string FormatValue(XField field, object value)
        {
            TypeCode code = Type.GetTypeCode(field.DataType);
            Boolean isNullable = field.Nullable;

            if (code == TypeCode.String)
            {
                // 热心网友 Hannibal 在处理日文网站时发现插入的日文为乱码，这里加上N前缀
                if (value == null) return isNullable ? "null" : "''";
                if (String.IsNullOrEmpty(value.ToString()) && isNullable) return "null";

                if (field.RawType == "NCLOB" || field.RawType.StartsWith("NCHAR") || field.RawType.StartsWith("NVARCHAR2"))
                    return "N'" + value.ToString().Replace("'", "''") + "'";
                else
                    return "'" + value.ToString().Replace("'", "''") + "'";
            }

            return base.FormatValue(field, value);
        }
        #endregion
    }

    /// <summary>
    /// Oracle数据库
    /// </summary>
    internal class OracleSession : RemoteDbSession
    {
        #region 基本方法 查询/执行
        /// <summary>
        /// 快速查询单表记录数，稍有偏差
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public override int QueryCountFast(string tableName)
        {
            String sql = String.Format("select NUM_ROWS from sys.all_indexes where TABLE_OWNER='{0}' and TABLE_NAME='{1}'", (Database as Oracle).Owner.ToUpper(), tableName);

            QueryTimes++;
            DbCommand cmd = PrepareCommand();
            cmd.CommandText = sql;
            if (Debug) WriteLog(cmd.CommandText);
            try
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (DbException ex)
            {
                throw OnException(ex, cmd.CommandText);
            }
            finally { AutoClose(); }
        }

        ///// <summary>
        ///// 执行插入语句并返回新增行的自动编号
        ///// </summary>
        ///// <param name="sql">SQL语句</param>
        ///// <returns>新增行的自动编号</returns>
        //public override Int64 InsertAndGetIdentity(String sql)
        //{
        //    throw new NotSupportedException("Oracle数据库不支持插入后返回新增行的自动编号！");
        //}
        #endregion

        ///// <summary>
        ///// 取得指定表的所有列构架
        ///// </summary>
        ///// <param name="table"></param>
        ///// <returns></returns>
        //protected override List<XField> GetFields(XTable table)
        //{
        //    //DataColumnCollection columns = GetColumns(xt.Name);
        //    DataTable dt = GetSchema("Columns", new String[] { table.Owner, table.Name });

        //    List<XField> list = new List<XField>();
        //    DataRow[] drs = dt.Select("", "ID");
        //    List<String> pks = GetPrimaryKeys(table);
        //    foreach (DataRow dr in drs)
        //    {
        //        XField field = table.CreateField();
        //        field.ID = Int32.Parse(dr["ID"].ToString());
        //        field.Name = dr["COLUMN_NAME"].ToString();
        //        field.RawType = dr["DATA_TYPE"].ToString();
        //        //xf.DataType = FieldTypeToClassType(dr["DATATYPE"].ToString());
        //        //field.DataType = FieldTypeToClassType(field);
        //        field.Identity = false;

        //        //if (columns != null && columns.Contains(xf.Name))
        //        //{
        //        //    DataColumn dc = columns[xf.Name];
        //        //    xf.DataType = dc.DataType;
        //        //}

        //        field.Length = dr["LENGTH"] == DBNull.Value ? 0 : Int32.Parse(dr["LENGTH"].ToString());
        //        field.Digit = dr["SCALE"] == DBNull.Value ? 0 : Int32.Parse(dr["SCALE"].ToString());

        //        field.PrimaryKey = pks != null && pks.Contains(field.Name);

        //        if (Type.GetTypeCode(field.DataType) == TypeCode.Int32 && field.Digit > 0)
        //        {
        //            field.DataType = typeof(Double);
        //        }
        //        else if (Type.GetTypeCode(field.DataType) == TypeCode.DateTime)
        //        {
        //            //xf.Length = dr["DATETIME_PRECISION"] == DBNull.Value ? 0 : Int32.Parse(dr["DATETIME_PRECISION"].ToString());
        //            field.NumOfByte = 0;
        //            field.Digit = 0;
        //        }
        //        else
        //        {
        //            //if (dr["DATA_TYPE"].ToString() == "130" && dr["COLUMN_FLAGS"].ToString() == "234") //备注类型
        //            //{
        //            //    xf.Length = Int32.MaxValue;
        //            //    xf.NumOfByte = Int32.MaxValue;
        //            //}
        //            //else
        //            {
        //                field.Length = dr["LENGTH"] == DBNull.Value ? 0 : Int32.Parse(dr["LENGTH"].ToString());
        //                field.NumOfByte = 0;
        //            }
        //            field.Digit = 0;
        //        }

        //        try
        //        {
        //            field.Nullable = Boolean.Parse(dr["NULLABLE"].ToString());
        //        }
        //        catch
        //        {
        //            field.Nullable = dr["NULLABLE"].ToString() == "Y";
        //        }

        //        list.Add(field);
        //    }

        //    return list;
        //}

        #region 字段类型到数据类型对照表
        ///// <summary>
        ///// 字段类型到数据类型对照表
        ///// </summary>
        ///// <param name="type"></param>
        ///// <returns></returns>
        //public override Type FieldTypeToClassType(String type)
        //{
        //    switch (type)
        //    {
        //        case "CHAR":
        //        case "VARCHAR2":
        //        case "NCHAR":
        //        case "NVARCHAR2":
        //        case "CLOB":
        //        case "NCLOB":
        //            return typeof(String);
        //        case "NUMBER":
        //            return typeof(Int32);
        //        case "FLOAT":
        //            return typeof(Double);
        //        case "DATE":
        //        case "TIMESTAMP":
        //        case "TIMESTAMP(6)":
        //            return typeof(DateTime);
        //        case "LONG":
        //        case "LOB":
        //        case "RAW":
        //        case "BLOB":
        //            return typeof(Byte[]);
        //        default:
        //            return typeof(String);
        //    }
        //}
        #endregion
    }

    /// <summary>
    /// Oracle元数据
    /// </summary>
    class OracleMeta : RemoteDbMetaData
    {
        /// <summary>拥有者</summary>
        public String Owner { get { return (Database as Oracle).Owner.ToUpper(); } }

        /// <summary>
        /// 取得所有表构架
        /// </summary>
        /// <returns></returns>
        public override List<XTable> GetTables()
        {
            try
            {
                //- 不要空，否则会死得很惨，列表所有数据表，实在太多了
                //if (String.Equals(user, "system")) user = null;

                DataTable dt = GetSchema("Tables", new String[] { Owner });

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

        /// <summary>
        /// 取得指定表的所有列构架
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        protected override List<XField> GetFields(XTable table)
        {
            DataTable dt = GetSchema("Columns", new String[] { Owner, table.Name, null });

            DataRow[] drs = null;
            if (dt.Columns.Contains("ID"))
                drs = dt.Select("", "ID");
            else
                drs = dt.Select("");

            List<XField> list = GetFields(table, drs);

            return list;
        }

        /// <summary>
        /// 已重载。主键构架
        /// </summary>
        protected override DataTable PrimaryKeys
        {
            get
            {
                if (_PrimaryKeys == null)
                {
                    DataTable pks = GetSchema("IndexColumns", new String[] { Owner, null, null, null, null });
                    if (pks == null) return null;

                    _PrimaryKeys = pks;
                }
                return _PrimaryKeys;
            }
        }
    }
}