using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
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
     *                          GetDataType
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
        /// <param name="data">扩展</param>
        /// <returns></returns>
        protected List<IDataTable> GetTables(DataRow[] rows, String[] names, IDictionary<String, DataTable> data = null)
        {
            if (rows == null || rows.Length == 0) return new List<IDataTable>();

            // 表名过滤
            if (names != null && names.Length > 0)
            {
                var hs = new HashSet<String>(names, StringComparer.OrdinalIgnoreCase);
                rows = rows.Where(dr => TryGetDataRowValue(dr, _.TalbeName, out String name) && hs.Contains(name)).ToArray();
            }

            var columns = data?["Columns"];
            var indexes = data?["Indexes"];
            var indexColumns = data?["IndexColumns"];

            if (columns == null) columns = GetSchema(_.Columns, null);
            if (indexes == null) indexes = GetSchema(_.Indexes, null);
            if (indexColumns == null) indexColumns = GetSchema(_.IndexColumns, null);

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
                var cs = GetFields(table, columns, data);
                if (cs != null && cs.Count > 0) table.Columns.AddRange(cs);

                var dis = GetIndexes(table, indexes, indexColumns);
                if (dis != null && dis.Count > 0) table.Indexes.AddRange(dis);

                FixTable(table, dr, data);

                // 修正关系数据
                table.Fix();

                list.Add(table);
                #endregion
            }

            return list;
        }

        /// <summary>修正表</summary>
        /// <param name="table"></param>
        /// <param name="dr"></param>
        /// <param name="data"></param>
        protected virtual void FixTable(IDataTable table, DataRow dr, IDictionary<String, DataTable> data) { }
        #endregion

        #region 字段架构
        /// <summary>取得指定表的所有列构架</summary>
        /// <param name="table"></param>
        /// <param name="columns">列</param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual List<IDataColumn> GetFields(IDataTable table, DataTable columns, IDictionary<String, DataTable> data)
        {
            var dt = columns;
            if (dt == null) return null;

            // 找到该表所有字段，注意排序
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
                field.RawType = GetDataRowValue<String>(dr, "DATA_TYPE", "DATATYPE", "COLUMN_DATA_TYPE");
                // 长度
                field.Length = GetDataRowValue<Int32>(dr, "CHARACTER_MAXIMUM_LENGTH", "LENGTH", "COLUMN_SIZE");

                if (field is XField fi)
                {
                    // 精度 与 位数
                    fi.Precision = GetDataRowValue<Int32>(dr, "NUMERIC_PRECISION", "DATETIME_PRECISION", "PRECISION");
                    fi.Scale = GetDataRowValue<Int32>(dr, "NUMERIC_SCALE", "SCALE");
                    if (field.Length == 0) field.Length = fi.Precision;
                }

                // 允许空
                if (TryGetDataRowValue(dr, "IS_NULLABLE", out b))
                    field.Nullable = b;
                else if (TryGetDataRowValue(dr, "IS_NULLABLE", out String str))
                {
                    if (!String.IsNullOrEmpty(str)) field.Nullable = "YES".EqualIgnoreCase(str);
                }
                else if (TryGetDataRowValue(dr, "NULLABLE", out str))
                {
                    if (!String.IsNullOrEmpty(str)) field.Nullable = "Y".EqualIgnoreCase(str);
                }

                // 描述
                field.Description = GetDataRowValue<String>(dr, "DESCRIPTION");

                FixField(field, dr);

                // 检查是否已正确识别类型
                if (field.DataType == null)
                    WriteLog("无法识别{0}.{1}的类型{2}！", table.TableName, field.ColumnName, field.RawType);
                // 非字符串字段，长度没有意义
                //else if (field.DataType != typeof(String))
                //    field.Length = 0;

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
            // 修正数据类型 +++重点+++
            if (field.DataType == null) field.DataType = GetDataType(field);
        }
        #endregion

        #region 索引架构
        /// <summary>获取索引</summary>
        /// <param name="table"></param>
        /// <param name="indexes">索引</param>
        /// <param name="indexColumns">索引列</param>
        /// <returns></returns>
        protected virtual List<IDataIndex> GetIndexes(IDataTable table, DataTable indexes, DataTable indexColumns)
        {
            if (indexes == null) return null;

            var drs = indexes.Select(String.Format("{0}='{1}'", _.TalbeName, table.TableName));
            if (drs == null || drs.Length < 1) return null;

            var list = new List<IDataIndex>();
            foreach (var dr in drs)
            {
                if (!TryGetDataRowValue(dr, _.IndexName, out String name)) continue;

                var di = table.CreateIndex();
                di.Name = name;

                if (TryGetDataRowValue(dr, _.ColumnName, out name) && !String.IsNullOrEmpty(name))
                    di.Columns = name.Split(",");
                else if (indexColumns != null)
                {
                    String orderby = null;
                    // Oracle数据库用ColumnPosition，其它数据库用OrdinalPosition
                    if (indexColumns.Columns.Contains(_.OrdinalPosition))
                        orderby = _.OrdinalPosition;
                    else if (indexColumns.Columns.Contains(_.ColumnPosition))
                        orderby = _.ColumnPosition;

                    var dics = indexColumns.Select(String.Format("{0}='{1}' And {2}='{3}'", _.TalbeName, table.TableName, _.IndexName, di.Name), orderby);
                    if (dics != null && dics.Length > 0)
                    {
                        var ns = new List<String>();
                        foreach (var item in dics)
                        {
                            if (TryGetDataRowValue(item, _.ColumnName, out String dcname) && !dcname.IsNullOrEmpty() && !ns.Contains(dcname)) ns.Add(dcname);
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
        /// <summary>类型映射</summary>
        protected IDictionary<Type, String[]> Types { get; set; }

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

        /// <summary>取字段类型</summary>
        /// <param name="field">字段</param>
        /// <returns></returns>
        protected virtual String GetFieldType(IDataColumn field)
        {
            var type = field.DataType;
            if (type == null) return null;

            // 处理枚举
            if (type.IsEnum) type = typeof(Int32);

            if (!Types.TryGetValue(type, out var ns)) return null;

            var typeName = ns.FirstOrDefault();
            // 大文本选第二个类型
            if (ns.Length > 1 && type == typeof(String) && (field.Length <= 0 || field.Length >= Database.LongTextLength)) typeName = ns[1];
            if (typeName.Contains("{0}"))
            {
                if (typeName.Contains("{1}"))
                    typeName = typeName.F(field.Precision, field.Scale);
                else
                    typeName = typeName.F(field.Length);
            }

            return typeName;
        }

        /// <summary>获取数据类型</summary>
        /// <param name="field"></param>
        /// <returns></returns>
        protected virtual Type GetDataType(IDataColumn field)
        {
            var rawType = field.RawType;
            if (rawType.Contains("(")) rawType = rawType.Substring(null, "(");
            var rawType2 = rawType + "(";

            foreach (var item in Types)
            {
                String dbtype = null;
                if (rawType.EqualIgnoreCase(item.Value))
                {
                    dbtype = item.Value[0];

                    // 大文本选第二个类型
                    if (item.Value.Length > 1 && item.Key == typeof(String) && (field.Length <= 0 || field.Length >= Database.LongTextLength)) dbtype = item.Value[1];
                }
                else
                {
                    dbtype = item.Value.FirstOrDefault(e => e.StartsWithIgnoreCase(rawType2));
                }
                if (!dbtype.IsNullOrEmpty())
                {
                    // 修正原始类型
                    if (dbtype.Contains("{0}"))
                    {
                        // 某些字段有精度需要格式化
                        if (dbtype.Contains("{1}"))
                        {
                            if (field is XField xf)
                                field.RawType = dbtype.F(xf.Precision, xf.Scale);
                        }
                        else
                            field.RawType = dbtype.F(field.Length);
                    }

                    return item.Key;
                }
            }

            return null;
        }

        /// <summary>获取数据类型</summary>
        /// <param name="rawType"></param>
        /// <returns></returns>
        public virtual Type GetDataType(String rawType)
        {
            if (rawType.Contains("(")) rawType = rawType.Substring(null, "(");
            var rawType2 = rawType + "(";

            foreach (var item in Types)
            {
                String dbtype = null;
                if (rawType.EqualIgnoreCase(item.Value))
                {
                    dbtype = item.Value[0];
                }
                else
                {
                    dbtype = item.Value.FirstOrDefault(e => e.StartsWithIgnoreCase(rawType2));
                }
                if (!dbtype.IsNullOrEmpty()) return item.Key;
            }

            return null;
        }
        #endregion
    }
}