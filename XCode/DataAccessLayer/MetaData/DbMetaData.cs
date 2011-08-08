using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using NewLife;
using NewLife.Reflection;
using XCode.Exceptions;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 数据库元数据
    /// </summary>
    abstract class DbMetaData : DisposeBase, IMetaData
    {
        #region 属性
        private IDatabase _Database;
        /// <summary>数据库</summary>
        public virtual IDatabase Database { get { return _Database; } set { _Database = value; } }
        #endregion

        #region 构架
        /// <summary>
        /// 返回数据源的架构信息
        /// </summary>
        /// <param name="collectionName">指定要返回的架构的名称。</param>
        /// <param name="restrictionValues">为请求的架构指定一组限制值。</param>
        /// <returns></returns>
        public DataTable GetSchema(string collectionName, string[] restrictionValues)
        {
            return Database.CreateSession().GetSchema(collectionName, restrictionValues);
        }

        /// <summary>
        /// 取得所有表构架
        /// </summary>
        /// <returns></returns>
        public virtual List<IDataTable> GetTables()
        {
            try
            {
                DataTable dt = GetSchema("Tables", null);
                if (dt == null || dt.Rows == null || dt.Rows.Count < 1) return null;

                // 默认列出所有表
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
        /// 根据数据行取得数据表
        /// </summary>
        /// <param name="rows">数据行</param>
        /// <returns></returns>
        protected List<IDataTable> GetTables(DataRow[] rows)
        {
            try
            {
                List<IDataTable> list = new List<IDataTable>();
                foreach (DataRow dr in rows)
                {
                    IDataTable table = DAL.CreateTable();
                    table.Name = GetDataRowValue<String>(dr, "TABLE_NAME");

                    // 顺序、编号
                    Int32 id = 0;
                    if (TryGetDataRowValue<Int32>(dr, "TABLE_ID", out id))
                        table.ID = id;
                    else
                        table.ID = list.Count + 1;

                    // 描述
                    table.Description = GetDataRowValue<String>(dr, "DESCRIPTION");

                    // 拥有者
                    table.Owner = GetDataRowValue<String>(dr, "OWNER");

                    // 是否视图
                    table.IsView = String.Equals("View", GetDataRowValue<String>(dr, "TABLE_TYPE"), StringComparison.OrdinalIgnoreCase);

                    table.DbType = Database.DbType;

                    // 字段的获取可能有异常，但不应该影响整体架构的获取
                    try
                    {
                        //table.Columns = GetFields(table).ToArray();
                        List<IDataColumn> columns = GetFields(table);
                        if (columns != null && columns.Count > 0) table.Columns.AddRange(columns);
                    }
                    catch (Exception ex)
                    {
                        if (DAL.Debug) DAL.WriteLog(ex.ToString());
                    }

                    FixTable(table, dr);

                    list.Add(table);
                }

                return list;
            }
            catch (DbException ex)
            {
                throw new XDbMetaDataException(this, "取得所有表构架出错！", ex);
            }
        }

        /// <summary>
        /// 修正表
        /// </summary>
        /// <param name="table"></param>
        /// <param name="dr"></param>
        protected virtual void FixTable(IDataTable table, DataRow dr) { }

        /// <summary>
        /// 取得指定表的所有列构架
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        protected virtual List<IDataColumn> GetFields(IDataTable table)
        {
            DataTable dt = GetSchema("Columns", new String[] { null, null, table.Name });

            DataRow[] drs = null;
            if (dt.Columns.Contains("ORDINAL_POSITION"))
                drs = dt.Select("", "ORDINAL_POSITION");
            else
                drs = dt.Select("");

            List<IDataColumn> list = GetFields(table, drs);

            return list;
        }

        /// <summary>
        /// 获取指定表的字段
        /// </summary>
        /// <param name="table"></param>
        /// <param name="rows"></param>
        /// <returns></returns>
        protected virtual List<IDataColumn> GetFields(IDataTable table, DataRow[] rows)
        {
            Dictionary<DataRow, String> pks = GetPrimaryKeys(table.Name);

            List<IDataColumn> list = new List<IDataColumn>();
            // 开始序号
            Int32 startIndex = 0;
            foreach (DataRow dr in rows)
            {
                //IDataColumn field = table.CreateColumn();
                IDataColumn field = table.CreateColumn();

                // 序号
                Int32 n = 0;
                if (TryGetDataRowValue<Int32>(dr, "ORDINAL_POSITION", out n))
                    field.ID = n;
                else if (TryGetDataRowValue<Int32>(dr, "ID", out n))
                    field.ID = n;
                // 如果从0开始，则所有需要同步增加；如果所有字段序号都是0，则按照先后顺序
                if (field.ID == 0)
                {
                    startIndex++;
                    //field.ID = startIndex;
                }
                if (startIndex > 0) field.ID += startIndex;

                // 名称
                field.Name = GetDataRowValue<String>(dr, "COLUMN_NAME");

                // 标识、主键
                Boolean b;
                if (TryGetDataRowValue<Boolean>(dr, "AUTOINCREMENT", out  b))
                    field.Identity = b;

                if (TryGetDataRowValue<Boolean>(dr, "PRIMARY_KEY", out  b))
                    field.PrimaryKey = b;
                else
                    field.PrimaryKey = pks != null && pks.ContainsValue(field.Name);

                // 原始数据类型
                //field.RawType = GetDataRowValue<String>(dr, "DATA_TYPE");
                String str;
                if (TryGetDataRowValue<String>(dr, "DATA_TYPE", out str))
                    field.RawType = str;
                else if (TryGetDataRowValue<String>(dr, "DATATYPE", out str))
                    field.RawType = str;
                else if (TryGetDataRowValue<String>(dr, "COLUMN_DATA_TYPE", out str))
                    field.RawType = str;

                // 是否Unicode
                if (Database is DbBase) field.IsUnicode = (Database as DbBase).IsUnicode(field.RawType);

                // 精度
                if (TryGetDataRowValue<Int32>(dr, "NUMERIC_PRECISION", out n))
                    field.Precision = n;
                else if (TryGetDataRowValue<Int32>(dr, "DATETIME_PRECISION", out n))
                    field.Precision = n;
                else if (TryGetDataRowValue<Int32>(dr, "PRECISION", out n))
                    field.Precision = n;

                // 位数
                //field.Digit = GetDataRowValue<Int32>(dr, "NUMERIC_SCALE");
                if (TryGetDataRowValue<Int32>(dr, "NUMERIC_SCALE", out n))
                    field.Scale = n;
                else if (TryGetDataRowValue<Int32>(dr, "SCALE", out n))
                    field.Scale = n;

                // 长度
                if (TryGetDataRowValue<Int32>(dr, "CHARACTER_MAXIMUM_LENGTH", out n))
                    field.Length = n;
                else if (TryGetDataRowValue<Int32>(dr, "LENGTH", out n))
                    field.Length = n;
                else if (TryGetDataRowValue<Int32>(dr, "COLUMN_SIZE", out n))
                    field.Length = n;
                else
                    field.Length = field.Precision;

                // 字节数
                //field.NumOfByte = GetDataRowValue<Int32>(dr, "CHARACTER_OCTET_LENGTH");
                if (TryGetDataRowValue<Int32>(dr, "CHARACTER_OCTET_LENGTH", out n))
                    field.NumOfByte = n;
                else
                    field.NumOfByte = field.Length;

                // 允许空
                if (TryGetDataRowValue<Boolean>(dr, "IS_NULLABLE", out  b))
                    field.Nullable = b;
                else if (TryGetDataRowValue<String>(dr, "IS_NULLABLE", out  str))
                {
                    if (!String.IsNullOrEmpty(str)) field.Nullable = String.Equals("YES", str, StringComparison.OrdinalIgnoreCase);
                }
                else if (TryGetDataRowValue<String>(dr, "NULLABLE", out  str))
                {
                    if (!String.IsNullOrEmpty(str)) field.Nullable = String.Equals("Y", str, StringComparison.OrdinalIgnoreCase);
                }

                // 默认值
                field.Default = GetDataRowValue<String>(dr, "COLUMN_DEFAULT");

                // 描述
                field.Description = GetDataRowValue<String>(dr, "DESCRIPTION");

                FixField(field, dr);

                list.Add(field);
            }

            return list;
        }

        /// <summary>
        /// 修正指定字段
        /// </summary>
        /// <param name="field"></param>
        /// <param name="dr"></param>
        protected virtual void FixField(IDataColumn field, DataRow dr)
        {
            ////String typeName = GetDataRowValue<String>(dr, "DATA_TYPE");
            //SetFieldType(field, field.RawType);

            DataTable dt = DataTypes;
            if (dt == null) return;

            DataRow[] drs = FindDataType(field, field.RawType, null);
            if (drs == null || drs.Length < 1)
                FixField(field, dr, null);
            else
                FixField(field, dr, drs[0]);
        }

        /// <summary>
        /// 修正指定字段
        /// </summary>
        /// <param name="field">字段</param>
        /// <param name="drColumn">字段元数据</param>
        /// <param name="drDataType">字段匹配的数据类型</param>
        protected virtual void FixField(IDataColumn field, DataRow drColumn, DataRow drDataType)
        {
            String typeName = field.RawType;

            // 修正数据类型 +++重点+++
            if (TryGetDataRowValue<String>(drDataType, "DataType", out typeName))
            {
                field.DataType = TypeX.GetType(typeName);
            }

            // 修正长度为最大长度
            if (field.Length == 0)
            {
                Int32 n = 0;
                if (TryGetDataRowValue<Int32>(drDataType, "ColumnSize", out n))
                {
                    field.Length = n;
                    if (field.NumOfByte == 0) field.NumOfByte = field.Length;
                }

                if (field.Length <= 0 && field.DataType == typeof(String)) field.Length = Int32.MaxValue / 2;
            }

            // 处理格式参数
            if (!String.IsNullOrEmpty(field.RawType) && !field.RawType.EndsWith(")"))
            {
                String param = GetFormatParam(field, drDataType);
                if (!String.IsNullOrEmpty(param)) field.RawType += param;
            }
        }
        #endregion

        #region 主键构架
        /// <summary>
        /// 取得指定表的所有主键构架
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        protected virtual Dictionary<DataRow, String> GetPrimaryKeys(String tableName)
        {
            DataTable dt = PrimaryKeys;
            if (dt == null) return null;
            try
            {
                DataRow[] drs = dt.Select("TABLE_NAME='" + tableName + "'");
                if (drs == null || drs.Length < 1) return null;
                Dictionary<DataRow, String> list = new Dictionary<DataRow, String>();
                foreach (DataRow dr in drs)
                {
                    String name = null;

                    if (TryGetDataRowValue<String>(dr, "COLUMN_NAME", out name)) list.Add(dr, name);
                    //list.Add(dr["COLUMN_NAME"] == DBNull.Value ? "" : dr["COLUMN_NAME"].ToString());
                }
                return list;
            }
            catch { return null; }
        }

        protected DataTable _PrimaryKeys;
        /// <summary>
        /// 主键构架
        /// </summary>
        protected virtual DataTable PrimaryKeys
        {
            get
            {
                if (_PrimaryKeys == null) _PrimaryKeys = GetSchema("Indexes", new String[] { null, null, null });
                return _PrimaryKeys;
            }
        }
        #endregion

        #region 数据类型
        private DataTable _DataTypes;
        /// <summary>数据类型</summary>
        public DataTable DataTypes
        {
            get { return _DataTypes ?? (_DataTypes = GetSchema(DbMetaDataCollectionNames.DataTypes, null)); }
        }

        /// <summary>
        /// 查找指定字段指定类型的数据类型
        /// </summary>
        /// <param name="field"></param>
        /// <param name="typeName"></param>
        /// <param name="isLong"></param>
        /// <returns></returns>
        protected virtual DataRow[] FindDataType(IDataColumn field, String typeName, Boolean? isLong)
        {
            if (String.IsNullOrEmpty(typeName)) throw new ArgumentNullException("typeName");
            // 去掉类型中，长度等限制条件
            if (typeName.Contains("(")) typeName = typeName.Substring(0, typeName.IndexOf("("));

            DataTable dt = DataTypes;
            if (dt == null) return null;

            DataRow[] drs = null;

            // 匹配TypeName，TypeName具有唯一性
            drs = dt.Select(String.Format("TypeName='{0}'", typeName));
            if (drs != null && drs.Length > 0)
            {
                //if (drs.Length > 1) throw new XDbMetaDataException(this, "TypeName具有唯一性！");
                return drs;
            }
            // 匹配DataType，重复的可能性很大
            DataRow[] drs2 = null;
            drs = dt.Select(String.Format("DataType='{0}'", typeName));
            if (drs != null && drs.Length > 0)
            {
                if (drs.Length == 1) return drs;

                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("DataType='{0}' And ColumnSize>={1}", typeName, field.Length);
                //if (field.DataType == typeof(String) && field.Length > Database.LongTextLength) sb.AppendFormat(" And IsLong=1");
                // 如果字段的长度为0，则也算是大文本
                if (field.DataType == typeof(String) && (field.Length > Database.LongTextLength || field.Length <= 0))
                    sb.AppendFormat(" And IsLong=1");

                drs2 = dt.Select(sb.ToString(), "IsBestMatch Desc, ColumnSize Asc, IsFixedLength Asc, IsLong Asc");
                if (drs2 == null || drs2.Length < 1) return drs;
                if (drs2.Length == 1) return drs2;

                return drs2;
            }
            return null;
        }

        ///// <summary>
        ///// 设置字段类型
        ///// </summary>
        ///// <param name="field"></param>
        ///// <param name="typeName"></param>
        //protected virtual void SetFieldType(IDataColumn field, String typeName)
        //{
        //    DataTable dt = DataTypes;
        //    if (dt == null) return;

        //    DataRow[] drs = FindDataType(field, typeName, null);
        //    if (drs == null || drs.Length < 1) return;

        //    // 处理格式参数
        //    if (!String.IsNullOrEmpty(field.RawType) && !field.RawType.EndsWith(")"))
        //    {
        //        String param = GetFormatParam(field, drs[0]);
        //        if (!String.IsNullOrEmpty(param)) field.RawType += param;
        //    }

        //    if (!TryGetDataRowValue<String>(drs[0], "DataType", out typeName)) return;
        //    field.DataType = TypeX.GetType(typeName);
        //}

        /// <summary>
        /// 取字段类型
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        protected virtual String GetFieldType(IDataColumn field)
        {
            String typeName = field.DataType.FullName;

            DataRow[] drs = FindDataType(field, typeName, null);
            if (drs == null || drs.Length < 1) return null;

            if (TryGetDataRowValue<String>(drs[0], "TypeName", out typeName))
            {
                // 处理格式参数
                String param = GetFormatParam(field, drs[0]);
                if (!String.IsNullOrEmpty(param) && param != "()") typeName += param;

                return typeName;
            }

            return null;
        }

        /// <summary>
        /// 取得格式化的类型参数
        /// </summary>
        /// <param name="field"></param>
        /// <param name="dr"></param>
        /// <returns></returns>
        protected virtual String GetFormatParam(IDataColumn field, DataRow dr)
        {
            String ps = null;
            if (!TryGetDataRowValue<String>(dr, "CreateParameters", out ps) || String.IsNullOrEmpty(ps)) return null;

            String param = String.Empty;
            param += "(";
            String[] pms = ps.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < pms.Length; i++)
            {
                Int32 item = GetFormatParamItem(field, dr, pms[i]);

                if (!param.EndsWith("(")) param += ",";
                param += item;
            }
            param += ")";

            return param;
        }

        /// <summary>
        /// 获取格式化参数项
        /// </summary>
        /// <param name="field"></param>
        /// <param name="dr"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        protected virtual Int32 GetFormatParamItem(IDataColumn field, DataRow dr, String item)
        {
            if (item.Contains("length") || item.Contains("size")) return field.Length;

            if (item.Contains("precision")) return field.Precision;

            if (item.Contains("scale") || item.Contains("bits"))
            {
                // 如果没有设置位数，则使用最大位数
                Int32 d = field.Scale;
                //if (d < 0)
                //{
                //    if (!TryGetDataRowValue<Int32>(dr, "MaximumScale", out d)) d = field.Scale;
                //}
                return d;
            }

            return 0;
        }
        #endregion

        #region 数据定义
        /// <summary>
        /// 获取数据定义语句
        /// </summary>
        /// <param name="schema">数据定义模式</param>
        /// <param name="values">其它信息</param>
        /// <returns></returns>
        public virtual String GetSchemaSQL(DDLSchema schema, params Object[] values)
        {
            switch (schema)
            {
                case DDLSchema.CreateDatabase:
                    return CreateDatabaseSQL((String)values[0], (String)values[1]);
                case DDLSchema.DropDatabase:
                    return DropDatabaseSQL((String)values[0]);
                case DDLSchema.DatabaseExist:
                    return DatabaseExistSQL(values == null || values.Length < 1 ? null : (String)values[0]);
                case DDLSchema.CreateTable:
                    return CreateTableSQL((IDataTable)values[0]);
                case DDLSchema.DropTable:
                    if (values[0] is IDataTable)
                        return DropTableSQL((IDataTable)values[0]);
                    else
                        return DropTableSQL(values[0].ToString());
                case DDLSchema.TableExist:
                    if (values[0] is IDataTable)
                        return TableExistSQL((IDataTable)values[0]);
                    else
                        return TableExistSQL(values[0].ToString());
                case DDLSchema.AddTableDescription:
                    return AddTableDescriptionSQL((IDataTable)values[0]);
                case DDLSchema.DropTableDescription:
                    return DropTableDescriptionSQL((IDataTable)values[0]);
                case DDLSchema.AddColumn:
                    return AddColumnSQL((IDataColumn)values[0]);
                case DDLSchema.AlterColumn:
                    return AlterColumnSQL((IDataColumn)values[0], values.Length > 1 ? (IDataColumn)values[1] : null);
                case DDLSchema.DropColumn:
                    return DropColumnSQL((IDataColumn)values[0]);
                case DDLSchema.AddColumnDescription:
                    return AddColumnDescriptionSQL((IDataColumn)values[0]);
                case DDLSchema.DropColumnDescription:
                    return DropColumnDescriptionSQL((IDataColumn)values[0]);
                case DDLSchema.AddDefault:
                    return AddDefaultSQL((IDataColumn)values[0]);
                case DDLSchema.DropDefault:
                    return DropDefaultSQL((IDataColumn)values[0]);
                case DDLSchema.CreateIndex:
                    return CreateIndexSQL((IDataIndex)values[0]);
                case DDLSchema.DropIndex:
                    return DropIndexSQL((IDataIndex)values[0]);
                default:
                    break;
            }

            throw new NotSupportedException("不支持该操作！");
        }

        /// <summary>
        /// 设置数据定义模式
        /// </summary>
        /// <param name="schema">数据定义模式</param>
        /// <param name="values">其它信息</param>
        /// <returns></returns>
        public virtual Object SetSchema(DDLSchema schema, params Object[] values)
        {
            //Object obj = null;
            switch (schema)
            {
                //case DDLSchema.CreateTable:
                //    CreateTable((IDataTable)values[0]);
                //    return true;
                case DDLSchema.TableExist:
                    String name;
                    if (values[0] is IDataTable)
                        name = (values[0] as IDataTable).Name;
                    else
                        name = values[0].ToString();

                    DataTable dt = GetSchema("Tables", new String[] { null, null, name, "TABLE" });
                    if (dt == null || dt.Rows == null || dt.Rows.Count < 1) return false;
                    return true;
                default:
                    break;
            }

            String sql = GetSchemaSQL(schema, values);
            if (String.IsNullOrEmpty(sql)) return null;

            IDbSession session = Database.CreateSession();

            if (schema == DDLSchema.TableExist || schema == DDLSchema.DatabaseExist)
            {
                return session.QueryCount(sql) > 0;
            }
            else
            {
                String[] ss = sql.Split(new String[] { ";" + Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                if (ss == null || ss.Length < 1)
                    return session.Execute(sql);
                else
                {
                    foreach (String item in ss)
                    {
                        session.Execute(item);
                    }
                    return 0;
                }
            }
        }

        /// <summary>
        /// 字段片段
        /// </summary>
        /// <param name="field"></param>
        /// <param name="onlyDefine">仅仅定义。定义操作才允许设置自增和使用默认值</param>
        /// <returns></returns>
        public virtual String FieldClause(IDataColumn field, Boolean onlyDefine)
        {
            StringBuilder sb = new StringBuilder();

            //字段名
            sb.AppendFormat("{0} ", FormatKeyWord(field.Name));

            String typeName = null;
            // 如果还是原来的数据库类型，则直接使用
            //if (Database.DbType == field.Table.DbType) typeName = field.RawType;
            // 每种数据库的自增差异太大，理应由各自处理，而不采用原始值
            if (Database.DbType == field.Table.DbType && !field.Identity) typeName = field.RawType;

            if (String.IsNullOrEmpty(typeName)) typeName = GetFieldType(field);

            sb.Append(typeName);

            // 约束
            sb.Append(GetFieldConstraints(field, onlyDefine));

            //默认值
            sb.Append(GetFieldDefault(field, onlyDefine));

            return sb.ToString();
        }

        /// <summary>
        /// 取得字段约束
        /// </summary>
        /// <param name="field"></param>
        /// <param name="onlyDefine">仅仅定义</param>
        /// <returns></returns>
        protected virtual String GetFieldConstraints(IDataColumn field, Boolean onlyDefine)
        {
            if (field.PrimaryKey)
            {
                return " Primary Key";
            }
            else
            {
                //是否为空
                //if (!field.Nullable) sb.Append(" NOT NULL");
                if (field.Nullable)
                    return " NULL";
                else
                    return " NOT NULL";
            }
        }

        /// <summary>
        /// 取得字段默认值
        /// </summary>
        /// <param name="field"></param>
        /// <param name="onlyDefine">仅仅定义</param>
        /// <returns></returns>
        protected virtual String GetFieldDefault(IDataColumn field, Boolean onlyDefine)
        {
            if (String.IsNullOrEmpty(field.Default)) return null;

            TypeCode tc = Type.GetTypeCode(field.DataType);
            if (tc == TypeCode.String)
                return String.Format(" Default '{0}'", field.Default);
            else if (tc == TypeCode.DateTime)
            {
                String d = CheckAndGetDefaultDateTimeNow(field.Table.DbType, field.Default);
                //if (String.Equals(d, "getdate()", StringComparison.OrdinalIgnoreCase)) d = "now()";
                //if (String.Equals(d, "getdate()", StringComparison.OrdinalIgnoreCase)) d = Database.DateTimeNow;
                return String.Format(" Default {0}", d);
            }
            else
                return String.Format(" Default {0}", field.Default);
        }
        #endregion

        #region 数据定义语句
        public virtual String CreateDatabaseSQL(String dbname, String file)
        {
            return String.Format("Create Database {0}", FormatKeyWord(dbname));
        }

        public virtual String DropDatabaseSQL(String dbname)
        {
            return String.Format("Drop Database {0}", FormatKeyWord(dbname));
        }

        public virtual String DatabaseExistSQL(String dbname)
        {
            throw new NotSupportedException("该功能未实现！");
        }

        public virtual String CreateTableSQL(IDataTable table)
        {
            List<IDataColumn> Fields = new List<IDataColumn>(table.Columns);
            Fields.Sort(delegate(IDataColumn item1, IDataColumn item2) { return item1.ID.CompareTo(item2.ID); });

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("Create Table {0}(", FormatKeyWord(table.Name));
            for (Int32 i = 0; i < Fields.Count; i++)
            {
                sb.AppendLine();
                sb.Append("\t");
                sb.Append(FieldClause(Fields[i], true));
                if (i < Fields.Count - 1) sb.Append(",");
            }
            sb.AppendLine();
            sb.Append(")");

            return sb.ToString();
        }

        String DropTableSQL(IDataTable table) { return DropTableSQL(table.Name); }

        public virtual String DropTableSQL(String tableName) { return String.Format("Drop Table {0}", FormatKeyWord(tableName)); }

        String TableExistSQL(IDataTable table) { return TableExistSQL(table.Name); }

        public virtual String TableExistSQL(String tableName) { throw new NotSupportedException("该功能未实现！"); }

        public virtual String AddTableDescriptionSQL(IDataTable table) { return null; }

        public virtual String DropTableDescriptionSQL(IDataTable table) { return null; }

        public virtual String AddColumnSQL(IDataColumn field) { return String.Format("Alter Table {0} Add {1}", FormatKeyWord(field.Table.Name), FieldClause(field, true)); }

        public virtual String AlterColumnSQL(IDataColumn field, IDataColumn oldfield) { return String.Format("Alter Table {0} Alter Column {1}", FormatKeyWord(field.Table.Name), FieldClause(field, false)); }

        public virtual String DropColumnSQL(IDataColumn field) { return String.Format("Alter Table {0} Drop Column {1}", FormatKeyWord(field.Table.Name), field.Name); }

        public virtual String AddColumnDescriptionSQL(IDataColumn field) { return null; }

        public virtual String DropColumnDescriptionSQL(IDataColumn field) { return null; }

        public virtual String AddDefaultSQL(IDataColumn field) { return null; }

        public virtual String DropDefaultSQL(IDataColumn field) { return null; }

        public virtual String CreateIndexSQL(IDataIndex index)
        {
            StringBuilder sb = new StringBuilder();
            if (index.Unique)
                sb.Append("Create Unique Index ");
            else
                sb.Append("Create Index ");

            sb.Append(FormatKeyWord(index.Name));
            sb.AppendFormat(" On {0} (", FormatKeyWord(index.Table.Name));
            for (int i = 0; i < index.Columns.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(index.Columns[i]);
                //else
                //    sb.AppendFormat("{0} {1}", FormatKeyWord(index.Columns[i].Name), isAscs[i].Value ? "Asc" : "Desc");
            }
            sb.Append(")");

            return sb.ToString();
        }

        public virtual String DropIndexSQL(IDataIndex index)
        {
            return String.Format("Drop Index {0} On {1}", FormatKeyWord(index.Name), FormatKeyWord(index.Table.Name));
        }
        #endregion

        #region 辅助函数
        /// <summary>
        /// 尝试从指定数据行中读取指定名称列的数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dr"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static Boolean TryGetDataRowValue<T>(DataRow dr, String name, out T value)
        {
            value = default(T);
            if (!dr.Table.Columns.Contains(name) || dr.IsNull(name)) return false;

            Object obj = dr[name];

            // 特殊处理布尔类型
            if (Type.GetTypeCode(typeof(T)) == TypeCode.Boolean && obj != null)
            {
                if (obj is Boolean)
                {
                    value = (T)obj;
                    return true;
                }

                if (String.Equals("YES", obj.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    value = (T)(Object)true;
                    return true;
                }
                if (String.Equals("NO", obj.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    value = (T)(Object)false;
                    return true;
                }
            }

            try
            {
                if (obj is T)
                    value = (T)obj;
                else
                    value = (T)Convert.ChangeType(obj, typeof(T));
            }
            catch { return false; }

            return true;
        }

        /// <summary>
        /// 获取指定数据行指定字段的值，不存在时返回空
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dr"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        protected static T GetDataRowValue<T>(DataRow dr, String name)
        {
            T value = default(T);
            if (TryGetDataRowValue<T>(dr, name, out value)) return value;
            return default(T);
        }

        ///// <summary>
        ///// 使用DataTable获取架构信息
        ///// </summary>
        ///// <param name="tableName"></param>
        ///// <returns></returns>
        //protected DataColumnCollection GetColumns(String tableName)
        //{
        //    //return Query(PageSplit("Select * from " + tableName, 0, 1, null)).Tables[0].Columns;
        //    try
        //    {
        //        return (Database.CreateSession() as DbSession).QueryWithKey(Database.PageSplit("Select * from " + FormatKeyWord(tableName), 0, 1, null)).Tables[0].Columns;
        //    }
        //    catch (Exception ex)
        //    {
        //        if (DAL.Debug) DAL.WriteLog(ex.ToString());
        //        return null;
        //    }
        //}

        /// <summary>
        /// 格式化关键字
        /// </summary>
        /// <param name="keyWord"></param>
        /// <returns></returns>
        protected String FormatKeyWord(String keyWord)
        {
            //return Database.FormatKeyWord(keyWord);
            return Database.FormatName(keyWord);
        }

        /// <summary>
        /// 检查并获取当前数据库的默认时间
        /// </summary>
        /// <param name="oriDbType"></param>
        /// <param name="oriDefault"></param>
        /// <returns></returns>
        protected virtual String CheckAndGetDefaultDateTimeNow(DatabaseType oriDbType, String oriDefault)
        {
            // 原始数据库类型
            IDatabase db = DbFactory.Create(oriDbType);
            if (db == null) return oriDefault;

            // 原始默认值是否是原始时间
            if (!oriDefault.Equals(db.DateTimeNow, StringComparison.OrdinalIgnoreCase)) return oriDefault;

            return Database.DateTimeNow;
        }
        #endregion
    }
}