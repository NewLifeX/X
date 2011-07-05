using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using XCode.Exceptions;
using System.IO;
using System.Text.RegularExpressions;

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
                sql = String.Format("Select * From (Select XCode_Temp_a.*, rownum as rowNumber From ({1}) XCode_Temp_a Where rownum<={2}) XCode_Temp_b Where rowNumber>={0}", startRowIndex + 1, sql, startRowIndex + maximumRows);
            //sql = String.Format("Select * From ({1}) a Where rownum>={0} and rownum<={2}", startRowIndex, sql, startRowIndex + maximumRows - 1);
            return sql;
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

        public override string FormatValue(IDataColumn field, object value)
        {
            TypeCode code = Type.GetTypeCode(field.DataType);
            Boolean isNullable = field.Nullable;

            if (code == TypeCode.String)
            {
                if (value == null) return isNullable ? "null" : "''";
                if (String.IsNullOrEmpty(value.ToString()) && isNullable) return "null";

                if (field.IsUnicode || IsUnicode(field.RawType))
                    return "N'" + value.ToString().Replace("'", "''") + "'";
                else
                    return "'" + value.ToString().Replace("'", "''") + "'";
            }

            return base.FormatValue(field, value);
        }

        /// <summary>
        /// 格式化标识列，返回插入数据时所用的表达式，如果字段本身支持自增，则返回空
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override string FormatIdentity(IDataColumn field, Object value)
        {
            return String.Format("SEQ_{0}.nextval", field.Table.Name);
        }
        #endregion

        #region 关键字
        protected override string ReservedWordsStr
        {
            get { return "ALL,ALTER,AND,ANY,AS,ASC,BETWEEN,BY,CHAR,CHECK,CLUSTER,COMPRESS,CONNECT,CREATE,DATE,DECIMAL,DEFAULT,DELETE,DESC,DISTINCT,DROP,ELSE,EXCLUSIVE,EXISTS,FLOAT,FOR,FROM,GRANT,GROUP,HAVING,IDENTIFIED,IN,INDEX,INSERT,INTEGER,INTERSECT,INTO,IS,LIKE,LOCK,LONG,MINUS,MODE,NOCOMPRESS,NOT,NOWAIT,NULL,NUMBER,OF,ON,OPTION,OR,ORDER,PCTFREE,PRIOR,PUBLIC,RAW,RENAME,RESOURCE,REVOKE,SELECT,SET,SHARE,SIZE,SMALLINT,START,SYNONYM,TABLE,THEN,TO,TRIGGER,UNION,UNIQUE,UPDATE,VALUES,VARCHAR,VARCHAR2,VIEW,WHERE,WITH"; }
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
            DbCommand cmd = CreateCommand();
            cmd.CommandText = sql;
            if (ShowSQL) WriteLog(cmd.CommandText);
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

        static Regex reg_SEQ = new Regex(@"\b(\w+)\.nextval\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /// <summary>
        /// 执行插入语句并返回新增行的自动编号
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>新增行的自动编号</returns>
        public override Int64 InsertAndGetIdentity(String sql)
        {
            Boolean b = IsAutoClose;
            // 禁用自动关闭，保证两次在同一会话
            IsAutoClose = false;

            try
            {
                Int64 rs = base.InsertAndGetIdentity(sql);
                if (rs <= 1) return rs;

                Match m = reg_SEQ.Match(sql);
                if (m == null || m.Groups == null || m.Groups.Count < 1) return rs;

                String name = m.Groups[1].Value;
                return (Int64)ExecuteScalar(String.Format("Select {0}.currval", name));
            }
            finally
            {
                IsAutoClose = b;
                AutoClose();
            }

            //throw new NotSupportedException("Oracle数据库不支持插入后返回新增行的自动编号！");
        }
        #endregion

        ///// <summary>
        ///// 取得指定表的所有列构架
        ///// </summary>
        ///// <param name="table"></param>
        ///// <returns></returns>
        //protected override List<IDataColumn> GetFields(IDataTable table)
        //{
        //    //DataColumnCollection columns = GetColumns(xt.Name);
        //    DataTable dt = GetSchema("Columns", new String[] { table.Owner, table.Name });

        //    List<IDataColumn> list = new List<IDataColumn>();
        //    DataRow[] drs = dt.Select("", "ID");
        //    List<String> pks = GetPrimaryKeys(table);
        //    foreach (DataRow dr in drs)
        //    {
        //        IDataColumn field = table.CreateField();
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
        public override List<IDataTable> GetTables()
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

        protected override void FixTable(IDataTable table, DataRow dr)
        {
            base.FixTable(table, dr);

            // 表注释 USER_TAB_COMMENTS
            //String sql = String.Format("Select COMMENTS From USER_TAB_COMMENTS Where TABLE_NAME='{0}'", table.Name);
            //String comment = (String)Database.CreateSession().ExecuteScalar(sql);
            String comment = GetTableComment(table.Name);
            if (!String.IsNullOrEmpty(comment)) table.Description = comment;

            if (table == null || table.Columns == null || table.Columns.Length < 1) return;

            // 自增
            Boolean exists = false;
            foreach (IDataColumn field in table.Columns)
            {
                // 不管是否主键
                if (field.DataType != typeof(Int16) &&
                    field.DataType != typeof(Int32) &&
                    field.DataType != typeof(Int64)) continue;

                String name = String.Format("SEQ_{0}_{1}", table.Name, field.Name);
                if (CheckSeqExists(name))
                {
                    field.Identity = true;
                    exists = true;
                    break;
                }
            }
            if (!exists)
            {
                // 检查该表是否有序列，如有，让主键成为自增
                String name = String.Format("SEQ_{0}", table.Name);
                if (CheckSeqExists(name))
                {
                    foreach (IDataColumn field in table.Columns)
                    {
                        if (!field.PrimaryKey ||
                            (field.DataType != typeof(Int16) &&
                            field.DataType != typeof(Int32) &&
                            field.DataType != typeof(Int64))) continue;

                        field.Identity = true;
                        exists = true;
                        break;
                    }
                }
            }
            if (!exists)
            {
                // 处理自增，整型、主键、名为ID认为是自增
                foreach (IDataColumn field in table.Columns)
                {
                    TypeCode code = Type.GetTypeCode(field.DataType);
                    if (code == TypeCode.Int16 || code == TypeCode.UInt16 ||
                        code == TypeCode.Int32 || code == TypeCode.UInt32 ||
                        code == TypeCode.Int64 || code == TypeCode.UInt64
                        )
                    {
                        if (field.PrimaryKey && field.Name.ToLower().Contains("id")) field.Identity = true;
                    }
                }
            }
        }

        /// <summary>
        /// 序列
        /// </summary>
        DataTable dtSequences;
        /// <summary>
        /// 检查序列是否存在
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Boolean CheckSeqExists(String name)
        {
            if (dtSequences == null)
            {
                DataSet ds = Database.CreateSession().Query("SELECT * FROM ALL_SEQUENCES Where SEQUENCE_OWNER='" + Owner + "'");
                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dtSequences = ds.Tables[0];
                else
                    dtSequences = new DataTable();
            }
            if (dtSequences.Rows == null || dtSequences.Rows.Count < 1) return false;

            String where = String.Format("SEQUENCE_NAME='{0}' And SEQUENCE_OWNER='{1}'", name, Owner);
            DataRow[] drs = dtSequences.Select(where);
            return drs != null && drs.Length > 0;

            //String sql = String.Format("SELECT Count(*) FROM ALL_SEQUENCES Where SEQUENCE_NAME='{0}' And SEQUENCE_OWNER='{1}'", name, Owner);
            //return Convert.ToInt32(Database.CreateSession().ExecuteScalar(sql)) > 0;
        }

        DataTable dtTableComment;
        String GetTableComment(String name)
        {
            if (dtTableComment == null)
            {
                DataSet ds = Database.CreateSession().Query("SELECT * FROM USER_TAB_COMMENTS");
                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dtTableComment = ds.Tables[0];
                else
                    dtTableComment = new DataTable();
            }
            if (dtTableComment.Rows == null || dtTableComment.Rows.Count < 1) return null;

            String where = String.Format("TABLE_NAME='{0}'", name);
            DataRow[] drs = dtTableComment.Select(where);
            if (drs != null && drs.Length > 0) return Convert.ToString(drs[0]["COMMENTS"]);
            return null;

            //String sql = String.Format("Select COMMENTS From USER_TAB_COMMENTS Where TABLE_NAME='{0}'", table.Name);
            //String comment = (String)Database.CreateSession().ExecuteScalar(sql);
        }

        /// <summary>
        /// 取得指定表的所有列构架
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        protected override List<IDataColumn> GetFields(IDataTable table)
        {
            DataTable dt = GetSchema("Columns", new String[] { Owner, table.Name, null });

            DataRow[] drs = null;
            if (dt.Columns.Contains("ID"))
                drs = dt.Select("", "ID");
            else
                drs = dt.Select("");

            List<IDataColumn> list = GetFields(table, drs);

            // 字段注释
            if (list != null && list.Count > 0)
            {
                //String sql = String.Format("Select COLUMN_NAME, COMMENTS From USER_COL_COMMENTS Where TABLE_NAME='{0}'", table.Name);
                //dt = Database.CreateSession().Query(sql).Tables[0];
                foreach (IDataColumn field in list)
                {
                    //drs = dt.Select(String.Format("COLUMN_NAME='{0}'", field.Name));
                    //if (drs != null && drs.Length > 0) field.Description = GetDataRowValue<String>(drs[0], "COMMENTS");

                    field.Description = GetColumnComment(table.Name, field.Name);
                }
            }

            return list;
        }

        DataTable dtColumnComment;
        String GetColumnComment(String tableName, String columnName)
        {
            if (dtColumnComment == null)
            {
                DataSet ds = Database.CreateSession().Query("SELECT * FROM USER_COL_COMMENTS");
                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dtColumnComment = ds.Tables[0];
                else
                    dtColumnComment = new DataTable();
            }
            if (dtColumnComment.Rows == null || dtColumnComment.Rows.Count < 1) return null;

            String where = String.Format("TABLE_NAME='{0}' AND COLUMN_NAME='{1}'", tableName, columnName);
            DataRow[] drs = dtColumnComment.Select(where);
            if (drs != null && drs.Length > 0) return Convert.ToString(drs[0]["COMMENTS"]);
            return null;
        }

        protected override void FixField(IDataColumn field, DataRow drColumn, DataRow drDataType)
        {
            base.FixField(field, drColumn, drDataType);

            // 处理数字类型
            if (field.RawType.StartsWith("NUMBER"))
            {
                if (field.Scale == 0)
                {
                    // 0表示长度不限制，为了方便使用，转为最常见的Int32
                    if (field.Precision == 0)
                        field.DataType = typeof(Int32);
                    else if (field.Precision == 1)
                        field.DataType = typeof(Boolean);
                    else if (field.Precision <= 5)
                        field.DataType = typeof(Int16);
                    else if (field.Precision <= 10)
                        field.DataType = typeof(Int32);
                    else
                        field.DataType = typeof(Int64);
                }
                else
                {
                    if (field.Precision == 0)
                        field.DataType = typeof(Decimal);
                    else if (field.Precision <= 5)
                        field.DataType = typeof(Single);
                    else if (field.Precision <= 10)
                        field.DataType = typeof(Double);
                }
            }
        }

        protected override string GetFieldType(IDataColumn field)
        {
            Int32 precision = field.Precision;
            Int32 scale = field.Scale;

            switch (Type.GetTypeCode(field.DataType))
            {
                case TypeCode.Boolean:
                    return "NUMBER(1, 0)";
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    if (precision <= 0) precision = 5;
                    return String.Format("NUMBER({0}, 0)", precision);
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    //if (precision <= 0) precision = 10;
                    if (precision <= 0) return "NUMBER";
                    return String.Format("NUMBER({0}, 0)", precision);
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    if (precision <= 0) precision = 20;
                    return String.Format("NUMBER({0}, 0)", precision);
                case TypeCode.Single:
                    if (precision <= 0) precision = 5;
                    if (scale <= 0) scale = 1;
                    return String.Format("NUMBER({0}, {1})", precision, scale);
                case TypeCode.Double:
                    if (precision <= 0) precision = 10;
                    if (scale <= 0) scale = 2;
                    return String.Format("NUMBER({0}, {1})", precision, scale);
                case TypeCode.Decimal:
                    if (precision <= 0) precision = 20;
                    if (scale <= 0) scale = 4;
                    return String.Format("NUMBER({0}, {1})", precision, scale);
                default:
                    break;
            }

            return base.GetFieldType(field);
        }

        protected override DataRow[] FindDataType(IDataColumn field, string typeName, bool? isLong)
        {
            DataRow[] drs = base.FindDataType(field, typeName, isLong);
            if (drs != null && drs.Length > 1)
            {
                // 字符串
                if (typeName == typeof(String).FullName)
                {
                    foreach (DataRow dr in drs)
                    {
                        String name = GetDataRowValue<String>(dr, "TypeName");
                        if ((name == "NVARCHAR2" && field.IsUnicode || name == "VARCHAR2" && !field.IsUnicode) && field.Length <= Database.LongTextLength)
                            return new DataRow[] { dr };
                        else if ((name == "NCLOB" && field.IsUnicode || name == "LONG" && !field.IsUnicode) && field.Length > Database.LongTextLength)
                            return new DataRow[] { dr };
                    }
                }

                //// 时间日期
                //if (typeName == typeof(DateTime).FullName)
                //{
                //    // DateTime的范围是0001到9999
                //    // Timestamp的范围是1970到2038
                //    String d = CheckAndGetDefaultDateTimeNow(field.Table.DbType, field.Default);
                //    foreach (DataRow dr in drs)
                //    {
                //        String name = GetDataRowValue<String>(dr, "TypeName");
                //        if (name == "DATETIME" && String.IsNullOrEmpty(field.Default))
                //            return new DataRow[] { dr };
                //        else if (name == "TIMESTAMP" && (d == "now()" || field.Default == "CURRENT_TIMESTAMP"))
                //            return new DataRow[] { dr };
                //    }
                //}
            }
            return drs;
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

        #region 架构定义
        protected override string GetFieldConstraints(IDataColumn field, bool onlyDefine)
        {
            // 有默认值时直接返回，待会在默认值里面加约束
            // 因为Oracle的声明是先有默认值再有约束的
            if (!String.IsNullOrEmpty(field.Default)) return null;

            return base.GetFieldConstraints(field, onlyDefine);
        }

        protected override string GetFieldDefault(IDataColumn field, bool onlyDefine)
        {
            if (String.IsNullOrEmpty(field.Default)) return null;

            return base.GetFieldDefault(field, onlyDefine) + base.GetFieldConstraints(field, onlyDefine);
        }

        public override string CreateTableSQL(IDataTable table)
        {
            String sql = base.CreateTableSQL(table);
            if (String.IsNullOrEmpty(sql)) return sql;

            String sqlSeq = String.Format("Create Sequence SEQ_{0} Minvalue 1 Maxvalue 9999999999 Start With 1 Increment By 1 Cache 20", table.Name);
            return sql + ";" + Environment.NewLine + sqlSeq;
        }

        public override string DropTableSQL(IDataTable table)
        {
            String sql = base.DropTableSQL(table);
            if (String.IsNullOrEmpty(sql)) return sql;

            String sqlSeq = String.Format("Drop Sequence SEQ_{0}", table.Name);
            return sql + ";" + Environment.NewLine + sqlSeq;
        }

        public override String AlterColumnSQL(IDataColumn field, IDataColumn oldfield)
        {
            return String.Format("Alter Table {0} Modify Column {1}", FormatKeyWord(field.Table.Name), FieldClause(field, false));
        }

        public override string AddTableDescriptionSQL(IDataTable table)
        {
            //return String.Format("Update USER_TAB_COMMENTS Set COMMENTS='{0}' Where TABLE_NAME='{1}'", table.Description, table.Name);

            return String.Format("Comment On Table {0} is '{1}'", FormatKeyWord(table.Name), table.Description);
        }

        public override string DropTableDescriptionSQL(IDataTable table)
        {
            //return String.Format("Update USER_TAB_COMMENTS Set COMMENTS='' Where TABLE_NAME='{0}'", table.Name);

            return String.Format("Comment On Table {0} is ''", FormatKeyWord(table.Name));
        }

        public override string AddColumnDescriptionSQL(IDataColumn field)
        {
            //return String.Format("Update USER_COL_COMMENTS Set COMMENTS='{0}' Where TABLE_NAME='{1}' AND COLUMN_NAME='{2}'", field.Description, field.Table.Name, field.Name);

            return String.Format("Comment On Column {0}.{1} is '{2}'", FormatKeyWord(field.Table.Name), FormatKeyWord(field.Name), field.Description);
        }

        public override string DropColumnDescriptionSQL(IDataColumn field)
        {
            //return String.Format("Update USER_COL_COMMENTS Set COMMENTS='' Where TABLE_NAME='{0}' AND COLUMN_NAME='{1}'", field.Table.Name, field.Name);

            return String.Format("Comment On Column {0}.{1} is ''", FormatKeyWord(field.Table.Name), FormatKeyWord(field.Name));
        }
        #endregion
    }
}