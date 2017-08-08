using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using NewLife.Reflection;
using XCode.Common;
using XCode.Exceptions;

namespace XCode.DataAccessLayer
{
    /* 正向工程层次结构：
     *  GetTables
     *      OnGetTables
     *          GetSchema
     *          GetTables
     *              GetFields
     *                  GetFields
     *                      FixField
     *                          FindDataType
     *                          FixField
     *                              GetFormatParam
     *                                  GetFormatParamItem
     *              GetIndexes
     *                  FixIndex
     *              FixTable
     */

    partial class DbMetaData
    {
        #region 常量
        protected static class _
        {
            public static readonly String Tables = "Tables";
            public static readonly String Views = "Views";
            public static readonly String Indexes = "Indexes";
            public static readonly String IndexColumns = "IndexColumns";
            public static readonly String Databases = "Databases";
            public static readonly String Columns = "Columns";
            public static readonly String ID = "ID";
            public static readonly String OrdinalPosition = "ORDINAL_POSITION";
            public static readonly String ColumnPosition = "COLUMN_POSITION";
            public static readonly String TalbeName = "table_name";
            public static readonly String ColumnName = "COLUMN_NAME";
            public static readonly String IndexName = "INDEX_NAME";
            public static readonly String PrimaryKeys = "PrimaryKeys";
        }
        #endregion

        #region 表构架
        /// <summary>取得所有表构架</summary>
        /// <returns></returns>
        public List<IDataTable> GetTables()
        {
            try
            {
                return OnGetTables(null);
            }
            catch (DbException ex)
            {
                throw new XDbMetaDataException(this, "取得所有表构架出错！", ex);
            }
        }

        /// <summary>取得所有表构架</summary>
        /// <returns></returns>
        protected virtual List<IDataTable> OnGetTables(String[] names)
        {
            var list = new List<IDataTable>();

            var dt = GetSchema(_.Tables, null);
            if (dt?.Rows == null || dt.Rows.Count < 1) return list;

            // 默认列出所有表
            var rows = dt?.Rows.ToArray();

            return GetTables(rows, names);
        }

        /// <summary>根据数据行取得数据表</summary>
        /// <param name="rows">数据行</param>
        /// <param name="names">指定表名</param>
        /// <returns></returns>
        protected List<IDataTable> GetTables(DataRow[] rows, String[] names)
        {
            if (rows == null || rows.Length == 0) return new List<IDataTable>();

            if (_columns == null)
                try { _columns = GetSchema(_.Columns, null); }
                catch (Exception ex) { DAL.WriteDebugLog(ex.ToString()); }
            if (_indexes == null)
                try { _indexes = GetSchema(_.Indexes, null); }
                catch (Exception ex) { DAL.WriteDebugLog(ex.ToString()); }
            if (_indexColumns == null)
                try { _indexColumns = GetSchema(_.IndexColumns, null); }
                catch (Exception ex) { DAL.WriteDebugLog(ex.ToString()); }

            // 表名过滤
            if (names != null && names.Length > 0)
            {
                var hs = new HashSet<String>(names, StringComparer.OrdinalIgnoreCase);
                rows = rows.Where(dr => TryGetDataRowValue(dr, _.TalbeName, out String name) && hs.Contains(name)).ToArray();
            }

            try
            {
                var list = new List<IDataTable>();
                foreach (var dr in rows)
                {
                    #region 基本属性
                    var table = DAL.CreateTable();
                    table.TableName = GetDataRowValue<String>(dr, _.TalbeName);

                    // 描述
                    table.Description = GetDataRowValue<String>(dr, "DESCRIPTION");

                    // 拥有者
                    table.Owner = GetDataRowValue<String>(dr, "OWNER");

                    // 是否视图
                    table.IsView = "View".EqualIgnoreCase(GetDataRowValue<String>(dr, "TABLE_TYPE"));

                    table.DbType = Database.Type;
                    #endregion

                    #region 字段及修正
                    var columns = GetFields(table);
                    if (columns != null && columns.Count > 0) table.Columns.AddRange(columns);

                    var indexes = GetIndexes(table);
                    if (indexes != null && indexes.Count > 0) table.Indexes.AddRange(indexes);

                    FixTable(table, dr);

                    // 修正关系数据
                    table.Fix();

                    list.Add(table);
                    #endregion
                }

                return list;
                // 不要把这些清空。因为，多线程同时操作的时候，前面的线程有可能把后面线程的数据给清空了
            }
            finally
            {
                _columns = null;
                _indexes = null;
                _indexColumns = null;
            }
        }

        /// <summary>修正表</summary>
        /// <param name="table"></param>
        /// <param name="dr"></param>
        protected virtual void FixTable(IDataTable table, DataRow dr) { }
        #endregion

        #region 字段架构
        protected DataTable _columns;
        /// <summary>取得指定表的所有列构架</summary>
        /// <param name="table"></param>
        /// <returns></returns>
        protected virtual List<IDataColumn> GetFields(IDataTable table)
        {
            //DataTable dt = GetSchema(_.Columns, new String[] { null, null, table.Name });
            var dt = _columns;
            if (dt == null) return null;

            DataRow[] drs = null;
            var where = String.Format("{0}='{1}'", _.TalbeName, table.TableName);
            if (dt.Columns.Contains(_.OrdinalPosition))
                drs = dt.Select(where, _.OrdinalPosition);
            else if (dt.Columns.Contains(_.ID))
                drs = dt.Select(where, _.ID);
            else
                drs = dt.Select(where);

            return GetFields(table, drs);
        }

        /// <summary>获取指定表的字段</summary>
        /// <param name="table"></param>
        /// <param name="rows"></param>
        /// <returns></returns>
        protected virtual List<IDataColumn> GetFields(IDataTable table, DataRow[] rows)
        {
            var list = new List<IDataColumn>();
            foreach (var dr in rows)
            {
                var field = table.CreateColumn();

                // 名称
                field.ColumnName = GetDataRowValue<String>(dr, _.ColumnName);

                // 标识、主键
                if (TryGetDataRowValue(dr, "AUTOINCREMENT", out Boolean b))
                    field.Identity = b;

                if (TryGetDataRowValue(dr, "PRIMARY_KEY", out b))
                    field.PrimaryKey = b;

                // 原始数据类型
                if (TryGetDataRowValue(dr, "DATA_TYPE", out String str))
                    field.RawType = str;
                else if (TryGetDataRowValue(dr, "DATATYPE", out str))
                    field.RawType = str;
                else if (TryGetDataRowValue(dr, "COLUMN_DATA_TYPE", out str))
                    field.RawType = str;

                //// 是否Unicode
                //if (Database is DbBase) field.IsUnicode = (Database as DbBase).IsUnicode(field.RawType);

                var n = 0;
                var fi = field as XField;
                if (fi != null)
                {
                    // 精度
                    if (TryGetDataRowValue(dr, "NUMERIC_PRECISION", out n))
                        fi.Precision = n;
                    else if (TryGetDataRowValue(dr, "DATETIME_PRECISION", out n))
                        fi.Precision = n;
                    else if (TryGetDataRowValue(dr, "PRECISION", out n))
                        fi.Precision = n;

                    // 位数
                    if (TryGetDataRowValue(dr, "NUMERIC_SCALE", out n))
                        fi.Scale = n;
                    else if (TryGetDataRowValue(dr, "SCALE", out n))
                        fi.Scale = n;
                }

                // 长度
                if (TryGetDataRowValue(dr, "CHARACTER_MAXIMUM_LENGTH", out n))
                    field.Length = n;
                else if (TryGetDataRowValue(dr, "LENGTH", out n))
                    field.Length = n;
                else if (TryGetDataRowValue(dr, "COLUMN_SIZE", out n))
                    field.Length = n;
                else if (fi != null)
                    field.Length = fi.Precision;

                //// 字节数
                //if (TryGetDataRowValue<Int32>(dr, "CHARACTER_OCTET_LENGTH", out n))
                //    field.NumOfByte = n;
                //else
                //    field.NumOfByte = field.Length;

                // 允许空
                if (TryGetDataRowValue(dr, "IS_NULLABLE", out b))
                    field.Nullable = b;
                else if (TryGetDataRowValue(dr, "IS_NULLABLE", out str))
                {
                    if (!String.IsNullOrEmpty(str)) field.Nullable = "YES".EqualIgnoreCase(str);
                }
                else if (TryGetDataRowValue(dr, "NULLABLE", out str))
                {
                    if (!String.IsNullOrEmpty(str)) field.Nullable = "Y".EqualIgnoreCase(str);
                }

                //// 默认值
                //field.Default = GetDataRowValue<String>(dr, "COLUMN_DEFAULT");

                // 描述
                field.Description = GetDataRowValue<String>(dr, "DESCRIPTION");

                FixField(field, dr);
                // 检查是否已正确识别类型
                if (field.DataType == null)
                {
                    WriteLog("无法识别{0}.{1}的类型{2}！", table.TableName, field.ColumnName, field.RawType);
                }

                field.Fix();
                list.Add(field);
            }

            return list;
        }

        /// <summary>修正指定字段</summary>
        /// <param name="field">字段</param>
        /// <param name="dr"></param>
        protected virtual void FixField(IDataColumn field, DataRow dr)
        {
            var dt = DataTypes;
            if (dt == null) return;

            var drs = FindDataType(field, field.RawType, null);
            if (drs == null || drs.Length < 1)
                FixField(field, dr, null);
            else
                FixField(field, dr, drs[0]);
        }

        /// <summary>修正指定字段</summary>
        /// <param name="field">字段</param>
        /// <param name="drColumn">字段元数据</param>
        /// <param name="drDataType">字段匹配的数据类型</param>
        protected virtual void FixField(IDataColumn field, DataRow drColumn, DataRow drDataType)
        {
            var typeName = field.RawType;

            // 修正数据类型 +++重点+++
            if (TryGetDataRowValue(drDataType, "DataType", out typeName))
            {
                field.DataType = typeName.GetTypeEx();
            }

            // 修正长度为最大长度
            if (field.Length == 0)
            {
                if (TryGetDataRowValue(drDataType, "ColumnSize", out Int32 n))
                {
                    field.Length = n;
                    //if (field.NumOfByte == 0) field.NumOfByte = field.Length;
                }

                if (field.Length <= 0 && field.DataType == typeof(String)) field.Length = Int32.MaxValue / 2;
            }

            // 处理格式参数
            if (!String.IsNullOrEmpty(field.RawType) && !field.RawType.EndsWith(")"))
            {
                var param = GetFormatParam(field, drDataType);
                if (!String.IsNullOrEmpty(param)) field.RawType += param;
            }
        }
        #endregion

        #region 索引架构
        protected DataTable _indexes;
        protected DataTable _indexColumns;
        /// <summary>获取索引</summary>
        /// <param name="table"></param>
        /// <returns></returns>
        protected virtual List<IDataIndex> GetIndexes(IDataTable table)
        {
            if (_indexes == null) return null;

            var drs = _indexes.Select(String.Format("{0}='{1}'", _.TalbeName, table.TableName));
            if (drs == null || drs.Length < 1) return null;

            var list = new List<IDataIndex>();
            foreach (var dr in drs)
            {

                if (!TryGetDataRowValue(dr, _.IndexName, out String name)) continue;

                var di = table.CreateIndex();
                di.Name = name;

                if (TryGetDataRowValue(dr, _.ColumnName, out name) && !String.IsNullOrEmpty(name))
                    di.Columns = name.Split(new String[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                else if (_indexColumns != null)
                {
                    String orderby = null;
                    // Oracle数据库用ColumnPosition，其它数据库用OrdinalPosition
                    if (_indexColumns.Columns.Contains(_.OrdinalPosition))
                        orderby = _.OrdinalPosition;
                    else if (_indexColumns.Columns.Contains(_.ColumnPosition))
                        orderby = _.ColumnPosition;

                    var dics = _indexColumns.Select(String.Format("{0}='{1}' And {2}='{3}'", _.TalbeName, table.TableName, _.IndexName, di.Name), orderby);
                    if (dics != null && dics.Length > 0)
                    {
                        var ns = new List<String>();
                        foreach (var item in dics)
                        {
                            if (TryGetDataRowValue(item, _.ColumnName, out String dcname) &&
    !String.IsNullOrEmpty(dcname) && !ns.Contains(dcname)) ns.Add(dcname);
                        }
                        if (ns.Count < 1) DAL.WriteLog("表{0}的索引{1}无法取得字段列表！", table, di.Name);
                        di.Columns = ns.ToArray();
                    }
                }

                if (TryGetDataRowValue(dr, "UNIQUE", out Boolean b))
                    di.Unique = b;

                if (TryGetDataRowValue(dr, "PRIMARY", out b))
                    di.PrimaryKey = b;
                else if (TryGetDataRowValue(dr, "PRIMARY_KEY", out b))
                    di.PrimaryKey = b;

                FixIndex(di, dr);

                list.Add(di);
            }
            return list != null && list.Count > 0 ? list : null;
        }

        /// <summary>修正索引</summary>
        /// <param name="index"></param>
        /// <param name="dr"></param>
        protected virtual void FixIndex(IDataIndex index, DataRow dr) { }
        #endregion

        #region 数据类型
        private DataTable _DataTypes;
        /// <summary>数据类型</summary>
        public DataTable DataTypes
        {
            get { return _DataTypes ?? (_DataTypes = GetSchema(DbMetaDataCollectionNames.DataTypes, null)); }
            internal protected set { _DataTypes = value; }
        }

        protected List<KeyValuePair<Type, Type>> _FieldTypeMaps;
        /// <summary>字段类型映射</summary>
        protected virtual List<KeyValuePair<Type, Type>> FieldTypeMaps
        {
            get
            {
                if (_FieldTypeMaps == null)
                {
                    // 把不常用的类型映射到常用类型，比如数据库SByte映射到实体类Byte，UInt32映射到Int32，而不需要重新修改数据库
                    var list = new List<KeyValuePair<Type, Type>>
                    {
                        new KeyValuePair<Type, Type>(typeof(SByte), typeof(Byte)),
                        //list.Add(new KeyValuePair<Type, Type>(typeof(SByte), typeof(Int16)));
                        // 因为等价，字节需要能够互相映射
                        new KeyValuePair<Type, Type>(typeof(Byte), typeof(SByte)),

                        new KeyValuePair<Type, Type>(typeof(UInt16), typeof(Int16)),
                        new KeyValuePair<Type, Type>(typeof(Int16), typeof(UInt16)),
                        //list.Add(new KeyValuePair<Type, Type>(typeof(UInt16), typeof(Int32)));
                        //list.Add(new KeyValuePair<Type, Type>(typeof(Int16), typeof(Int32)));

                        new KeyValuePair<Type, Type>(typeof(UInt32), typeof(Int32)),
                        new KeyValuePair<Type, Type>(typeof(Int32), typeof(UInt32)),
                        //// 因为自增的原因，某些字段需要被映射到Int32里面来
                        //list.Add(new KeyValuePair<Type, Type>(typeof(SByte), typeof(Int32)));

                        new KeyValuePair<Type, Type>(typeof(UInt64), typeof(Int64)),
                        new KeyValuePair<Type, Type>(typeof(Int64), typeof(UInt64)),
                        //list.Add(new KeyValuePair<Type, Type>(typeof(UInt64), typeof(Int32)));
                        //list.Add(new KeyValuePair<Type, Type>(typeof(Int64), typeof(Int32)));

                        //// 根据常用行，从不常用到常用排序，然后配对进入映射表
                        //var types = new Type[] { typeof(SByte), typeof(Byte), typeof(UInt16), typeof(Int16), typeof(UInt64), typeof(Int64), typeof(UInt32), typeof(Int32) };

                        //for (int i = 0; i < types.Length; i++)
                        //{
                        //    for (int j = i + 1; j < types.Length; j++)
                        //    {
                        //        list.Add(new KeyValuePair<Type, Type>(types[i], types[j]));
                        //    }
                        //}
                        //// 因为自增的原因，某些字段需要被映射到Int64里面来
                        //list.Add(new KeyValuePair<Type, Type>(typeof(UInt32), typeof(Int64)));
                        //list.Add(new KeyValuePair<Type, Type>(typeof(Int32), typeof(Int64)));
                        new KeyValuePair<Type, Type>(typeof(Guid), typeof(String))
                    };
                    _FieldTypeMaps = list;
                }
                return _FieldTypeMaps;
            }
        }

        /// <summary>查找指定字段指定类型的数据类型</summary>
        /// <param name="field">字段</param>
        /// <param name="typeName"></param>
        /// <param name="isLong"></param>
        /// <returns></returns>
        protected virtual DataRow[] FindDataType(IDataColumn field, String typeName, Boolean? isLong)
        {
            DataRow[] drs = null;
            try
            {
                drs = OnFindDataType(field, typeName, isLong);
            }
            catch { }
            if (drs != null && drs.Length > 0) return drs;

            // 把Guid映射到varchar(32)去
            if (typeName == typeof(Guid).FullName || typeName.EqualIgnoreCase("Guid"))
            {
                typeName = "varchar(32)";
                try
                {
                    drs = OnFindDataType(field, typeName, isLong);
                }
                catch { }
                if (drs != null && drs.Length > 0) return drs;
            }

            // 如果该类型无法识别，则去尝试使用最接近的高阶类型
            foreach (var item in FieldTypeMaps)
            {
                if (item.Key.FullName == typeName)
                {
                    try
                    {
                        drs = OnFindDataType(field, item.Value.FullName, isLong);
                    }
                    catch { }
                    if (drs != null && drs.Length > 0) return drs;
                }
            }
            return null;
        }

        private DataRow[] OnFindDataType(IDataColumn field, String typeName, Boolean? isLong)
        {
            if (String.IsNullOrEmpty(typeName)) throw new ArgumentNullException("typeName");
            // 去掉类型中，长度等限制条件
            if (typeName.Contains("(")) typeName = typeName.Substring(0, typeName.IndexOf("("));

            var dt = DataTypes;
            if (dt == null) return null;

            DataRow[] drs = null;
            var sb = new StringBuilder();

            // 匹配TypeName，TypeName具有唯一性
            sb.AppendFormat("TypeName='{0}'", typeName);

            drs = dt.Select(sb.ToString());
            if (drs != null && drs.Length > 0)
            {
                // 找到太多，试试过滤自增等
                if (drs.Length > 1 && field.Identity && dt.Columns.Contains("IsAutoIncrementable"))
                {
                    var dr = drs.FirstOrDefault(e => (Boolean)e["IsAutoIncrementable"]);
                    if (dr != null) return new DataRow[] { dr };
                }

                return drs;
            }

            // 匹配DataType，重复的可能性很大
            sb = new StringBuilder();
            sb.AppendFormat("DataType='{0}'", typeName);

            drs = dt.Select(sb.ToString());
            if (drs != null && drs.Length > 0)
            {
                if (drs.Length == 1) return drs;
                // 找到太多，试试过滤自增等
                if (drs.Length > 1 && field.Identity && dt.Columns.Contains("IsAutoIncrementable"))
                {
                    var drs1 = drs.Where(e => (Boolean)e["IsAutoIncrementable"]).ToArray();
                    if (drs1 != null)
                    {
                        if (drs1.Length == 1) return drs1;
                        drs = drs1;
                    }
                }

                sb.AppendFormat(" And ColumnSize>={0}", field.Length);
                //if (field.DataType == typeof(String) && field.Length > Database.LongTextLength) sb.AppendFormat(" And IsLong=1");
                // 如果字段的长度为0，则也算是大文本
                if (field.DataType == typeof(String) && (field.Length > Database.LongTextLength || field.Length <= 0))
                    sb.AppendFormat(" And IsLong=1");

                var drs2 = dt.Select(sb.ToString(), "IsBestMatch Desc, ColumnSize Asc, IsFixedLength Asc, IsLong Asc");
                if (drs2 == null || drs2.Length < 1) return drs;
                if (drs2.Length == 1) return drs2;

                return drs2;
            }
            return null;
        }

        /// <summary>取字段类型</summary>
        /// <param name="field">字段</param>
        /// <returns></returns>
        protected virtual String GetFieldType(IDataColumn field)
        {
            /*
             * 首先尝试原始数据类型，因为即使是不同的数据库，相近的类型也可能采用相同的名称；
             * 然后才使用.Net类型名去匹配；
             * 两种方法都要注意处理类型参数，比如长度、精度、小数位数等
             */

            var typeName = field.RawType;
            DataRow[] drs = null;

            if (!String.IsNullOrEmpty(typeName))
            {
                if (typeName.Contains("(")) typeName = typeName.Substring(0, typeName.IndexOf("("));
                drs = FindDataType(field, typeName, null);
                if (drs != null && drs.Length > 0)
                {
                    if (TryGetDataRowValue(drs[0], "TypeName", out typeName))
                    {
                        // 处理格式参数
                        var param = GetFormatParam(field, drs[0]);
                        if (!String.IsNullOrEmpty(param) && param != "()") typeName += param;

                        return typeName;
                    }
                }
            }

            typeName = field.DataType.FullName;

            drs = FindDataType(field, typeName, null);
            if (drs != null && drs.Length > 0)
            {
                if (TryGetDataRowValue(drs[0], "TypeName", out typeName))
                {
                    // 处理格式参数
                    var param = GetFormatParam(field, drs[0]);
                    if (!String.IsNullOrEmpty(param) && param != "()") typeName += param;

                    return typeName;
                }
            }

            return null;
        }

        /// <summary>取得格式化的类型参数</summary>
        /// <param name="field">字段</param>
        /// <param name="dr"></param>
        /// <returns></returns>
        protected virtual String GetFormatParam(IDataColumn field, DataRow dr)
        {
            // 为了最大程度保证兼容性，所有数据库的Decimal和DateTime类型不指定精度，均采用数据库默认值
            //if (field.DataType == typeof(Decimal)) return null;
            if (field.DataType == typeof(DateTime)) return null;

            if (!TryGetDataRowValue(dr, "CreateParameters", out String ps) || String.IsNullOrEmpty(ps)) return null;

            var sb = new StringBuilder();
            sb.Append("(");
            var pms = ps.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < pms.Length; i++)
            {
                if (sb.Length > 1) sb.Append(",");
                sb.Append(GetFormatParamItem(field, dr, pms[i]));
            }
            sb.Append(")");

            return sb.ToString();
        }

        /// <summary>获取格式化参数项</summary>
        /// <param name="field">字段</param>
        /// <param name="dr"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        protected virtual String GetFormatParamItem(IDataColumn field, DataRow dr, String item)
        {
            if (item.Contains("length") || item.Contains("size")) return field.Length.ToString();

            //if (item.Contains("precision")) return field.Precision.ToString();

            //if (item.Contains("scale") || item.Contains("bits"))
            //{
            //    // 如果没有设置位数，则使用最大位数
            //    Int32 d = field.Scale;
            //    //if (d < 0)
            //    //{
            //    //    if (!TryGetDataRowValue<Int32>(dr, "MaximumScale", out d)) d = field.Scale;
            //    //}
            //    return d.ToString();
            //}

            return "0";
        }

        /// <summary>获取数据类型字符串</summary>
        /// <returns></returns>
        public String GetDataTypes()
        {
            var dt = DataTypes;
            var rows = dt.Select("", "DataType Asc, IsBestMatch Desc");
            var dic = new Dictionary<String, List<String>>();
            foreach (var dr in rows)
            {
                var tname = (dr["DataType"] + "").TrimStart("System.");
                if (!dic.TryGetValue(tname, out List<String> list)) dic[tname] = list = new List<String>();
                list.Add(dr["CreateFormat"] + "");
            }

            dic = dic.OrderBy(e => e.Key.GetType().GetTypeCode()).ToDictionary(e => e.Key, e => e.Value);

            var sb = new StringBuilder();
            foreach (var item in dic)
            {
                if (sb.Length > 0) sb.AppendLine(",");

                sb.Append(new String(' ', 12));
                sb.AppendFormat("{{ typeof({0}), new String[] {{ {1} }} }}", item.Key, item.Value.Select(e => "\"" + e + "\"").Join(", "));
            }

            return Environment.NewLine + sb.ToString();
        }
        #endregion
    }
}