using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using XCode.Exceptions;

namespace XCode.DataAccessLayer
{
    partial class DbSession
    {
        #region 构架
        /// <summary>
        /// 返回数据源的架构信息
        /// </summary>
        /// <param name="collectionName">指定要返回的架构的名称。</param>
        /// <param name="restrictionValues">为请求的架构指定一组限制值。</param>
        /// <returns></returns>
        public virtual DataTable GetSchema(string collectionName, string[] restrictionValues)
        {
            if (!Opened) Open();

            try
            {
                DataTable dt;
                if (restrictionValues == null || restrictionValues.Length < 1)
                {
                    if (String.IsNullOrEmpty(collectionName))
                        dt = Conn.GetSchema();
                    else
                        dt = Conn.GetSchema(collectionName);
                }
                else
                    dt = Conn.GetSchema(collectionName, restrictionValues);

                return dt;
            }
            catch (DbException ex)
            {
                throw new XDbSessionException(this, "取得所有表构架出错！", ex);
            }
            finally
            {
                AutoClose();
            }
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
                throw new XDbSessionException(this, "取得所有表构架出错！", ex);
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

                    table.DbType = DbType;

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
                throw new XDbSessionException(this, "取得所有表构架出错！", ex);
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

            DataRow[] drs = dt.Select("", "ORDINAL_POSITION");

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
                {
                    field.PrimaryKey = pks != null && pks.ContainsValue(field.Name);
                }

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
            field.DataType = FieldTypeToClassType(field.RawType);
        }

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
                return QueryWithKey(Database.PageSplit("Select * from " + FormatKeyWord(tableName), 0, 1, null)).Tables[0].Columns;
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

        /// <summary>
        /// 字段类型到数据类型对照表
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public virtual Type FieldTypeToClassType(String typeName)
        {
            DataTable dt = DataTypes;
            if (dt == null) return null;

            DataRow[] drs = dt.Select(String.Format("TypeName='{0}'", typeName));
            //if (drs == null || drs.Length < 1) drs = dt.Select(String.Format("ProviderDbType={0}", type));
            if (drs == null || drs.Length < 1) return null;

            if (!TryGetDataRowValue<String>(drs[0], "DataType", out typeName)) return null;
            return Type.GetType(typeName);
        }

        /// <summary>
        /// 数据类型到数据库类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual String ClassTypeToFieldType(Type type)
        {
            DataTable dt = DataTypes;
            if (dt == null) return null;

            String typeName = type.Name;

            DataRow[] drs = dt.Select(String.Format("DataType='{0}'", typeName));
            if (drs == null || drs.Length < 1) return null;

            if (!TryGetDataRowValue<String>(drs[0], "TypeName", out typeName)) return null;
            return typeName;
        }
        #endregion
    }
}