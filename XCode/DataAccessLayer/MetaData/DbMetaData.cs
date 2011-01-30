using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using NewLife;
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
        public virtual List<XTable> GetTables()
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
        protected virtual List<XTable> GetTables(DataRow[] rows)
        {
            try
            {
                List<XTable> list = new List<XTable>();
                foreach (DataRow dr in rows)
                {
                    XTable table = new XTable();
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
                        table.Fields = GetFields(table);
                    }
                    catch (Exception ex)
                    {
                        if (Debug) WriteLog(ex.ToString());
                    }

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
        /// 取得指定表的所有列构架
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        protected virtual List<XField> GetFields(XTable table)
        {
            //DataColumnCollection columns = GetColumns(table.Name);

            DataTable dt = GetSchema("Columns", new String[] { null, null, table.Name });

            DataRow[] drs = null;
            if (dt.Columns.Contains("ORDINAL_POSITION"))
                drs = dt.Select("", "ORDINAL_POSITION");
            else
                drs = dt.Select("");

            List<XField> list = GetFields(table, drs);

            return list;
        }

        /// <summary>
        /// 获取指定表的字段
        /// </summary>
        /// <param name="table"></param>
        /// <param name="rows"></param>
        /// <returns></returns>
        protected virtual List<XField> GetFields(XTable table, DataRow[] rows)
        {
            Dictionary<DataRow, String> pks = GetPrimaryKeys(table.Name);

            List<XField> list = new List<XField>();
            // 开始序号
            Int32 startIndex = 0;
            foreach (DataRow dr in rows)
            {
                XField field = table.CreateField();

                // 序号
                field.ID = GetDataRowValue<Int32>(dr, "ORDINAL_POSITION");
                // 如果从0开始，则所有需要同步增加；如果所有字段序号都是0，则按照先后顺序
                if (field.ID == 0)
                {
                    startIndex++;
                    field.ID = startIndex;
                }
                //if (startIndex > 0) field.ID += startIndex;

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
                field.RawType = GetDataRowValue<String>(dr, "DATA_TYPE");

                // 长度
                Int32 n = 0;
                if (TryGetDataRowValue<Int32>(dr, "NUMERIC_PRECISION", out n))
                    field.Length = n;
                else if (TryGetDataRowValue<Int32>(dr, "DATETIME_PRECISION", out n))
                    field.Length = n;
                else if (TryGetDataRowValue<Int32>(dr, "CHARACTER_MAXIMUM_LENGTH", out n))
                    field.Length = n;

                // 位数
                field.Digit = GetDataRowValue<Int32>(dr, "NUMERIC_SCALE");

                // 字节数
                field.NumOfByte = GetDataRowValue<Int32>(dr, "CHARACTER_OCTET_LENGTH");

                // 允许空
                if (TryGetDataRowValue<Boolean>(dr, "IS_NULLABLE", out  b))
                    field.Nullable = b;
                else
                {
                    String str = GetDataRowValue<String>(dr, "IS_NULLABLE");
                    if (!String.IsNullOrEmpty(str)) field.Nullable = String.Equals("YES", str, StringComparison.OrdinalIgnoreCase);
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
        protected virtual void FixField(XField field, DataRow dr)
        {
            String typeName = GetDataRowValue<String>(dr, "DATA_TYPE");
            SetFieldType(field, typeName);
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

        /// <summary>
        /// 使用DataTable获取架构信息
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        protected DataColumnCollection GetColumns(String tableName)
        {
            //return Query(PageSplit("Select * from " + tableName, 0, 1, null)).Tables[0].Columns;
            try
            {
                return (Database.CreateSession() as DbSession).QueryWithKey(Database.PageSplit("Select * from " + FormatKeyWord(tableName), 0, 1, null)).Tables[0].Columns;
            }
            catch (Exception ex)
            {
                if (Debug) WriteLog(ex.ToString());
                return null;
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

        ///// <summary>
        ///// 字段类型到数据类型对照表
        ///// </summary>
        ///// <param name="typeName"></param>
        ///// <returns></returns>
        //public virtual Type FieldTypeToClassType(String typeName)
        //{
        //    DataTable dt = DataTypes;
        //    if (dt == null) return null;

        //    DataRow[] drs = dt.Select(String.Format("TypeName='{0}'", typeName));
        //    //if (drs == null || drs.Length < 1) drs = dt.Select(String.Format("ProviderDbType={0}", type));
        //    if (drs == null || drs.Length < 1) return null;

        //    if (!TryGetDataRowValue<String>(drs[0], "DataType", out typeName)) return null;
        //    return Type.GetType(typeName);
        //}

        ///// <summary>
        ///// 数据类型到数据库类型
        ///// </summary>
        ///// <param name="type"></param>
        ///// <returns></returns>
        //public virtual String ClassTypeToFieldType(Type type)
        //{
        //    DataTable dt = DataTypes;
        //    if (dt == null) return null;

        //    String typeName = type.Name;

        //    DataRow[] drs = dt.Select(String.Format("DataType='{0}'", typeName));
        //    if (drs == null || drs.Length < 1) return null;

        //    if (!TryGetDataRowValue<String>(drs[0], "TypeName", out typeName)) return null;
        //    return typeName;
        //}

        /// <summary>
        /// 查找指定字段指定类型的数据类型
        /// </summary>
        /// <param name="field"></param>
        /// <param name="typeName"></param>
        /// <param name="isLong"></param>
        /// <returns></returns>
        protected virtual DataRow[] FindDataType(XField field, String typeName, Boolean? isLong)
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

                drs2 = dt.Select(String.Format("DataType='{0}' And ColumnSize>={1}", typeName, field.Length), "IsBestMatch Desc, ColumnSize Asc, IsFixedLength Asc, IsLong Asc");
                if (drs2 == null || drs2.Length < 1) return drs;
                if (drs2.Length == 1) return drs2;

                //// 开始排除
                //List<DataRow> list = new List<DataRow>(drs2);
                //// 排除非最佳匹配
                //if (list.Count > 1) list.RemoveAll(delegate(DataRow dr) { return !GetDataRowValue<Boolean>(dr, "IsBestMatch"); });
                //// 排除非最佳匹配
                //if (list.Count > 1) list.RemoveAll(delegate(DataRow dr) { return !GetDataRowValue<Boolean>(dr, "IsBestMatch"); });
                //// 排除非最佳匹配
                //if (list.Count > 1) list.RemoveAll(delegate(DataRow dr) { return !GetDataRowValue<Boolean>(dr, "IsBestMatch"); });
                //// 排除非最佳匹配
                //if (list.Count > 1) list.RemoveAll(delegate(DataRow dr) { return !GetDataRowValue<Boolean>(dr, "IsBestMatch"); });
                //if (drs2.Length > 1) throw new XDbSessionException(this, "无法唯一识别类型！");
                return drs2;
            }
            return null;
        }

        /// <summary>
        /// 设置字段类型
        /// </summary>
        /// <param name="field"></param>
        /// <param name="typeName"></param>
        protected virtual void SetFieldType(XField field, String typeName)
        {
            DataTable dt = DataTypes;
            if (dt == null) return;

            DataRow[] drs = FindDataType(field, typeName, null);
            if (drs == null || drs.Length < 1) return;

            // 处理格式参数
            if (!String.IsNullOrEmpty(field.RawType) && !field.RawType.EndsWith(")"))
            {
                String param = GetFormatParam(field, drs[0]);
                if (!String.IsNullOrEmpty(param)) field.RawType += param;
            }

            if (!TryGetDataRowValue<String>(drs[0], "DataType", out typeName)) return;
            field.DataType = Type.GetType(typeName);
        }

        /// <summary>
        /// 取字段类型
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        protected virtual String GetFieldType(XField field)
        {
            String typeName = field.DataType.FullName;

            DataRow[] drs = FindDataType(field, typeName, null);
            if (drs == null || drs.Length < 1) return null;

            if (TryGetDataRowValue<String>(drs[0], "TypeName", out typeName))
            {
                // 处理格式参数
                String param = GetFormatParam(field, drs[0]);
                if (!String.IsNullOrEmpty(param)) typeName += param;

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
        protected virtual String GetFormatParam(XField field, DataRow dr)
        {
            String ps = null;
            if (!TryGetDataRowValue<String>(dr, "CreateParameters", out ps)) return null;

            String param = String.Empty;
            param += "(";
            String[] pms = ps.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < pms.Length; i++)
            {
                if (pms[i].Contains("length") || pms[i].Contains("size") || pms[i].Contains("precision"))
                {
                    if (!param.EndsWith("(")) param += ",";
                    param += field.Length;
                    continue;
                }
                if (pms[i].Contains("scale") || pms[i].Contains("bits"))
                {
                    if (!param.EndsWith("(")) param += ",";
                    // 如果没有设置位数，则使用最大位数
                    Int32 d = field.Digit;
                    if (d <= 0)
                    {
                        if (!TryGetDataRowValue<Int32>(dr, "MaximumScale", out d)) d = field.Digit;
                    }
                    param += d;
                    continue;
                }
            }
            param += ")";

            return param;
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
                    return CreateTableSQL((XTable)values[0]);
                case DDLSchema.DropTable:
                    return DropTableSQL((String)values[0]);
                case DDLSchema.TableExist:
                    return TableExistSQL((String)values[0]);
                case DDLSchema.AddTableDescription:
                    return AddTableDescriptionSQL((String)values[0], (String)values[1]);
                case DDLSchema.DropTableDescription:
                    return DropTableDescriptionSQL((String)values[0]);
                case DDLSchema.AddColumn:
                    return AddColumnSQL((String)values[0], (XField)values[1]);
                case DDLSchema.AlterColumn:
                    return AlterColumnSQL((String)values[0], (XField)values[1]);
                case DDLSchema.DropColumn:
                    return DropColumnSQL((String)values[0], (String)values[1]);
                case DDLSchema.AddColumnDescription:
                    return AddColumnDescriptionSQL((String)values[0], (String)values[1], (String)values[2]);
                case DDLSchema.DropColumnDescription:
                    return DropColumnDescriptionSQL((String)values[0], (String)values[1]);
                case DDLSchema.AddDefault:
                    return AddDefaultSQL((String)values[0], (XField)values[1]);
                case DDLSchema.DropDefault:
                    return DropDefaultSQL((String)values[0], (String)values[1]);
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
        public virtual String FieldClause(XField field, Boolean onlyDefine)
        {
            StringBuilder sb = new StringBuilder();

            //字段名
            sb.AppendFormat("{0} ", FormatKeyWord(field.Name));

            String typeName = null;
            // 如果还是原来的数据库类型，则直接使用
            if (Database.DbType == field.Table.DbType) typeName = field.RawType;

            if (String.IsNullOrEmpty(typeName)) typeName = GetFieldType(field);

            sb.Append(typeName);

            if (field.PrimaryKey)
            {
                sb.Append(" Primary Key");
            }
            else
            {
                //是否为空
                //if (!field.Nullable) sb.Append(" NOT NULL");
                if (field.Nullable)
                    sb.Append(" NULL");
                else
                {
                    sb.Append(" NOT NULL");
                }
            }

            //默认值
            if (onlyDefine && !String.IsNullOrEmpty(field.Default))
            {
                TypeCode tc = Type.GetTypeCode(field.DataType);
                if (tc == TypeCode.String)
                    sb.AppendFormat(" DEFAULT '{0}'", field.Default);
                else if (tc == TypeCode.DateTime)
                {
                    String d = field.Default;
                    //if (String.Equals(d, "getdate()", StringComparison.OrdinalIgnoreCase)) d = "now()";
                    if (String.Equals(d, "getdate()", StringComparison.OrdinalIgnoreCase)) d = Database.DateTimeNow;
                    sb.AppendFormat(" DEFAULT {0}", d);
                }
                else
                    sb.AppendFormat(" DEFAULT {0}", field.Default);
            }
            //else if (onlyDefine && !field.PrimaryKey && !field.Nullable)
            //{
            //    //该字段不允许空，而又没有默认值时，设置默认值
            //    if (!includeDefault || String.IsNullOrEmpty(field.Default))
            //    {
            //        if (tc == TypeCode.String)
            //            sb.AppendFormat(" DEFAULT ('{0}')", "");
            //        else if (tc == TypeCode.DateTime)
            //        {
            //            String d = SqlDateTime.MinValue.Value.ToString("yyyy-MM-dd HH:mm:ss");
            //            sb.AppendFormat(" DEFAULT {0}", d);
            //        }
            //        else
            //            sb.AppendFormat(" DEFAULT {0}", "''");
            //    }
            //}

            return sb.ToString();
        }
        #endregion

        #region 数据定义语句
        public virtual String CreateDatabaseSQL(String dbname, String file)
        {
            return null;
        }

        public virtual String DropDatabaseSQL(String dbname)
        {
            return String.Format("Drop Database {0}", FormatKeyWord(dbname));
        }

        public virtual String DatabaseExistSQL(String dbname)
        {
            throw new NotSupportedException("该功能未实现！");
        }

        public virtual String CreateTableSQL(XTable table)
        {
            List<XField> Fields = new List<XField>(table.Fields);
            Fields.Sort(delegate(XField item1, XField item2) { return item1.ID.CompareTo(item2.ID); });

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

            ////注释
            //if (!String.IsNullOrEmpty(table.Description))
            //{
            //    String sql = AddTableDescriptionSQL(table.Name, table.Description);
            //    if (!String.IsNullOrEmpty(sql))
            //    {
            //        sb.AppendLine(";");
            //        sb.Append(sql);
            //    }
            //}

            return sb.ToString();
        }

        public virtual String DropTableSQL(String tablename)
        {
            return String.Format("Drop Table {0}", FormatKeyWord(tablename));
        }

        public virtual String TableExistSQL(String tablename)
        {
            throw new NotSupportedException("该功能未实现！");
        }

        public virtual String AddTableDescriptionSQL(String tablename, String description)
        {
            return null;
        }

        public virtual String DropTableDescriptionSQL(String tablename)
        {
            return null;
        }

        public virtual String AddColumnSQL(String tablename, XField field)
        {
            return String.Format("Alter Table {0} Add {1}", FormatKeyWord(tablename), FieldClause(field, true));
        }

        public virtual String AlterColumnSQL(String tablename, XField field)
        {
            return String.Format("Alter Table {0} Alter Column {1}", FormatKeyWord(tablename), FieldClause(field, false));
        }

        public virtual String DropColumnSQL(String tablename, String columnname)
        {
            return String.Format("Alter Table {0} Drop Column {1}", FormatKeyWord(tablename), columnname);
        }

        public virtual String AddColumnDescriptionSQL(String tablename, String columnname, String description)
        {
            return null;
        }

        public virtual String DropColumnDescriptionSQL(String tablename, String columnname)
        {
            return null;
        }

        public virtual String AddDefaultSQL(String tablename, XField field)
        {
            return null;
        }

        public virtual String DropDefaultSQL(String tablename, String columnname)
        {
            return null;
        }
        #endregion

        #region 辅助函数
        /// <summary>
        /// 格式化关键字
        /// </summary>
        /// <param name="keyWord"></param>
        /// <returns></returns>
        protected String FormatKeyWord(String keyWord)
        {
            return Database.FormatKeyWord(keyWord);
        }
        #endregion

        #region Sql日志输出
        /// <summary>
        /// 是否调试
        /// </summary>
        public static Boolean Debug { get { return DAL.Debug; } }

        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="msg"></param>
        public static void WriteLog(String msg) { DAL.WriteLog(msg); }

        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLog(String format, params Object[] args) { DAL.WriteLog(format, args); }
        #endregion
    }
}
