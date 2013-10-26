using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Reflection;

namespace XCode.DataAccessLayer
{
    /// <summary>数据模型扩展</summary>
    public static class ModelHelper
    {
        #region 模型扩展方法
        /// <summary>根据字段名获取字段</summary>
        /// <param name="table"></param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static IDataColumn GetColumn(this IDataTable table, String name)
        {
            if (String.IsNullOrEmpty(name)) return null;

            return table.Columns.FirstOrDefault(c => c.Is(name));
        }

        /// <summary>根据字段名数组获取字段数组</summary>
        /// <param name="table"></param>
        /// <param name="names"></param>
        /// <returns></returns>
        public static IDataColumn[] GetColumns(this IDataTable table, String[] names)
        {
            if (names == null || names.Length < 1) return new IDataColumn[0];

            return table.Columns.Where(c => names.Any(n => c.Is(n))).ToArray();
        }

        /// <summary>判断表是否等于指定名字</summary>
        /// <param name="table"></param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static Boolean Is(this IDataTable table, String name)
        {
            if (String.IsNullOrEmpty(name)) return false;

            //return table.TableName.EqualIC(name) || table.Name.EqualIC(name);
            return name.EqualIgnoreCase(table.TableName, table.Name);
        }

        /// <summary>判断字段是否等于指定名字</summary>
        /// <param name="column"></param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static Boolean Is(this IDataColumn column, String name)
        {
            if (String.IsNullOrEmpty(name)) return false;

            //return column.ColumnName.EqualIC(name) || column.Name.EqualIC(name);
            return name.EqualIgnoreCase(column.ColumnName, column.Name);
        }

        /// <summary>根据字段名找索引</summary>
        /// <param name="table"></param>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public static IDataIndex GetIndex(this IDataTable table, params String[] columnNames)
        {
            if (table == null || table.Indexes == null || table.Indexes.Count < 1 || columnNames == null || columnNames.Length < 1) return null;

            var di = table.Indexes.FirstOrDefault(
                e => e != null && e.Columns != null &&
                    e.Columns.Length == columnNames.Length &&
                    !e.Columns.Except(columnNames, StringComparer.OrdinalIgnoreCase).Any());
            if (di != null) return di;

            // 用别名再试一次
            var columns = table.GetColumns(columnNames);
            if (columns == null || columns.Length < 1) return null;
            columnNames = columns.Select(e => e.Name).ToArray();
            di = table.Indexes.FirstOrDefault(
                e => e.Columns != null &&
                    e.Columns.Length == columnNames.Length &&
                    !e.Columns.Except(columnNames, StringComparer.OrdinalIgnoreCase).Any());
            if (di != null) return di;

            return null;
        }

        /// <summary>根据字段从指定表中查找关系</summary>
        /// <param name="table"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static IDataRelation GetRelation(this IDataTable table, String columnName)
        {
            return table.Relations.FirstOrDefault(e => e.Column.EqualIgnoreCase(columnName));
            //foreach (var item in table.Relations)
            //{
            //    if (String.Equals(item.Column, columnName, StringComparison.OrdinalIgnoreCase)) return item;
            //}

            //return null;
        }

        /// <summary>根据字段、关联表、关联字段从指定表中查找关系</summary>
        /// <param name="table"></param>
        /// <param name="dr"></param>
        /// <returns></returns>
        public static IDataRelation GetRelation(this IDataTable table, IDataRelation dr)
        {
            return table.GetRelation(dr.Column, dr.RelationTable, dr.RelationColumn);
        }

        /// <summary>根据字段、关联表、关联字段从指定表中查找关系</summary>
        /// <param name="table"></param>
        /// <param name="columnName"></param>
        /// <param name="rtableName"></param>
        /// <param name="rcolumnName"></param>
        /// <returns></returns>
        public static IDataRelation GetRelation(this IDataTable table, String columnName, String rtableName, String rcolumnName)
        {
            foreach (var item in table.Relations)
            {
                if (item.Column == columnName && item.RelationTable == rtableName && item.RelationColumn == rcolumnName) return item;
            }

            return null;
        }
        #endregion

        #region 序列化扩展

        /// <summary>导出模型</summary>
        /// <param name="tables"></param>
        /// <param name="atts">附加属性</param>
        /// <returns></returns>
        public static String ToXml(IEnumerable<IDataTable> tables, IDictionary<String, String> atts = null)
        {
            var ms = new MemoryStream();

            var settings = new XmlWriterSettings();
            settings.Encoding = new UTF8Encoding(false);
            settings.Indent = true;

            var writer = XmlWriter.Create(ms, settings);
            writer.WriteStartDocument();
            writer.WriteStartElement("Tables");
            // 写入版本
            writer.WriteAttributeString("Version", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            if (atts != null && atts.Count > 0)
            {
                foreach (var item in atts)
                {
                    //writer.WriteAttributeString(item.Key, item.Value);
                    if (!String.IsNullOrEmpty(item.Value)) writer.WriteElementString(item.Key, item.Value);
                }
            }
            foreach (var item in tables)
            {
                writer.WriteStartElement("Table");
                (item as IXmlSerializable).WriteXml(writer);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        /// <summary>导入模型</summary>
        /// <param name="xml"></param>
        /// <param name="createTable">用于创建<see cref="IDataTable"/>实例的委托</param>
        /// <param name="atts">附加属性</param>
        /// <returns></returns>
        public static List<IDataTable> FromXml(String xml, Func<IDataTable> createTable, IDictionary<String, String> atts = null)
        {
            if (String.IsNullOrEmpty(xml)) return null;
            if (createTable == null) throw new ArgumentNullException("createTable");

            var settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;
            settings.IgnoreComments = true;

            var reader = XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(xml)), settings);
            while (reader.NodeType != XmlNodeType.Element) { if (!reader.Read())return null; }
            reader.ReadStartElement();
            //if (atts != null && reader.HasAttributes)
            //{
            //    reader.MoveToFirstAttribute();
            //    do
            //    {
            //        atts[reader.Name] = reader.Value;
            //    }
            //    while (reader.MoveToNextAttribute());
            //}

            var list = new List<IDataTable>();
            var id = 1;
            while (reader.IsStartElement())
            {
                if (reader.Name.EqualIgnoreCase("Table"))
                {
                    var table = createTable();
                    table.ID = id++;
                    list.Add(table);

                    //reader.ReadStartElement();
                    (table as IXmlSerializable).ReadXml(reader);
                    //if (reader.NodeType == XmlNodeType.EndElement) reader.ReadEndElement();
                }
                else if (atts != null)
                {
                    var name = reader.Name;
                    reader.ReadStartElement();
                    if (reader.NodeType == XmlNodeType.Text)
                    {
                        atts[name] = reader.ReadString();
                    }
                    reader.ReadEndElement();
                }
            }
            return list;
        }

        /// <summary>读取</summary>
        /// <param name="table"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static IDataTable ReadXml(this IDataTable table, XmlReader reader)
        {
            // 读属性
            if (reader.HasAttributes)
            {
                reader.MoveToFirstAttribute();
                ReadXml(reader, table);
            }

            reader.ReadStartElement();

            // 读字段
            reader.MoveToElement();
            // 有些数据表模型没有字段
            if (reader.NodeType == XmlNodeType.Element && reader.Name.EqualIgnoreCase("Table")) return table;

            while (reader.NodeType != XmlNodeType.EndElement)
            //while (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.Name)
                {
                    case "Columns":
                        reader.ReadStartElement();
                        var id = 1;
                        while (reader.IsStartElement())
                        {
                            var dc = table.CreateColumn();
                            dc.ID = id++;
                            var v = reader.GetAttribute("DataType");
                            if (v != null)
                            {
                                dc.DataType = Reflect.GetType(v);
                                v = reader.GetAttribute("Length");
                                var len = 0;
                                if (v != null && Int32.TryParse(v, out len)) dc.Length = len;

                                // 含有ID表示是旧的，不需要特殊处理，否则一些默认值会不对
                                v = reader.GetAttribute("ID");
                                if (v == null) dc = Fix(dc, dc);
                            }
                            (dc as IXmlSerializable).ReadXml(reader);
                            table.Columns.Add(dc);
                        }
                        reader.ReadEndElement();
                        break;
                    case "Indexes":
                        reader.ReadStartElement();
                        while (reader.IsStartElement())
                        {
                            var di = table.CreateIndex();
                            (di as IXmlSerializable).ReadXml(reader);
                            table.Indexes.Add(di);
                        }
                        reader.ReadEndElement();
                        break;
                    case "Relations":
                        reader.ReadStartElement();
                        while (reader.IsStartElement())
                        {
                            var dr = table.CreateRelation();
                            (dr as IXmlSerializable).ReadXml(reader);
                            if (table.GetRelation(dr) == null) table.Relations.Add(dr);
                        }
                        reader.ReadEndElement();
                        break;
                    default:
                        // 这里必须处理，否则加载特殊Xml文件时将会导致死循环
                        reader.Read();
                        break;
                }
            }

            //if (reader.NodeType != XmlNodeType.Element && reader.NodeType != XmlNodeType.EndElement) reader.Read();
            //reader.ReadEndElement();
            if (reader.NodeType == XmlNodeType.EndElement) reader.ReadEndElement();

            return table;
        }

        /// <summary>写入</summary>
        /// <param name="table"></param>
        /// <param name="writer"></param>
        public static IDataTable WriteXml(this IDataTable table, XmlWriter writer)
        {
            WriteXml(writer, table);

            // 写字段
            if (table.Columns != null && table.Columns.Count > 0 && table.Columns[0] is IXmlSerializable)
            {
                writer.WriteStartElement("Columns");
                foreach (IXmlSerializable item in table.Columns)
                {
                    writer.WriteStartElement("Column");
                    item.WriteXml(writer);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            if (table.Indexes != null && table.Indexes.Count > 0 && table.Indexes[0] is IXmlSerializable)
            {
                writer.WriteStartElement("Indexes");
                foreach (IXmlSerializable item in table.Indexes)
                {
                    writer.WriteStartElement("Index");
                    item.WriteXml(writer);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            if (table.Relations != null && table.Relations.Count > 0 && table.Relations[0] is IXmlSerializable)
            {
                writer.WriteStartElement("Relations");
                foreach (IXmlSerializable item in table.Relations)
                {
                    writer.WriteStartElement("Relation");
                    item.WriteXml(writer);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }

            return table;
        }

        /// <summary>读取</summary>
        /// <param name="reader"></param>
        /// <param name="value">数值</param>
        public static void ReadXml(XmlReader reader, Object value)
        {
            var pis = GetProperties(value.GetType());
            var names = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            foreach (var pi in pis)
            {
                if (!pi.CanRead) continue;
                if (AttributeX.GetCustomAttribute<XmlIgnoreAttribute>(pi, false) != null) continue;

                // 已处理的特性
                names.Add(pi.Name);

                var v = reader.GetAttribute(pi.Name);
                if (String.IsNullOrEmpty(v)) continue;

                if (pi.PropertyType == typeof(String[]))
                {
                    var ss = v.Split(new String[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    // 去除前后空格，因为手工修改xml的时候，可能在逗号后加上空格
                    for (int i = 0; i < ss.Length; i++)
                    {
                        ss[i] = ss[i].Trim();
                    }
                    value.SetValue(pi, ss);
                }
                else
                    value.SetValue(pi, v.ChangeType(pi.PropertyType));
            }
            var pi1 = pis.FirstOrDefault(e => e.Name == "Name");
            var pi2 = pis.FirstOrDefault(e => e.Name == "TableName" || e.Name == "ColumnName");
            if (pi1 != null && pi2 != null)
            {
                // 兼容旧版本
                var v2 = reader.GetAttribute("Alias");
                if (!String.IsNullOrEmpty(v2))
                {
                    //pi2.SetValue(value, pi1.GetValue(value));
                    //pi1.SetValue(value, v2);
                    value.SetValue(pi2, value.GetValue(pi1));
                    value.SetValue(pi1, v2);
                }
                // 写入的时候省略了相同的TableName/ColumnName
                //v2 = (String)pi2.GetValue(value);
                v2 = (String)value.GetValue(pi2);
                if (String.IsNullOrEmpty(v2))
                {
                    //pi2.SetValue(value, pi1.GetValue(value));
                    value.SetValue(pi2, value.GetValue(pi1));
                }
            }
            // 自增字段非空
            if (value is IDataColumn)
            {
                var dc = value as IDataColumn;
                if (dc.Identity) dc.Nullable = false;
            }
            //reader.Skip();

            // 剩余特性作为扩展属性
            if (reader.MoveToFirstAttribute())
            {
                if (value is IDataTable || value is IDataColumn)
                {
                    var dic = (value is IDataTable) ? (value as IDataTable).Properties : (value as IDataColumn).Properties;
                    do
                    {
                        if (!names.Contains(reader.Name))
                        {
                            //dic.Add(reader.Name, reader.Value);
                            dic[reader.Name] = reader.Value;
                        }
                    } while (reader.MoveToNextAttribute());
                }
            }
        }

        /// <summary>写入</summary>
        /// <param name="writer"></param>
        /// <param name="value">数值</param>
        /// <param name="writeDefaultValueMember">是否写数值为默认值的成员。为了节省空间，默认不写。</param>
        public static void WriteXml(XmlWriter writer, Object value, Boolean writeDefaultValueMember = false)
        {
            var type = value.GetType();
            var def = GetDefault(type);
            if (value is IDataColumn)
            {
                //var dc2 = def as IDataColumn;
                var value2 = value as IDataColumn;
                // 需要重新创建，因为GetDefault带有缓存
                var dc2 = type.CreateInstance() as IDataColumn;
                dc2.DataType = value2.DataType;
                dc2.Length = value2.Length;
                def = Fix(dc2, value2);
            }

            String name = null;

            // 基本类型，输出为特性
            foreach (var pi in GetProperties(type))
            {
                if (!pi.CanWrite) continue;
                if (AttributeX.GetCustomAttribute<XmlIgnoreAttribute>(pi, false) != null) continue;
                // 忽略ID
                if (pi.Name == "ID") continue;
                // IDataIndex跳过默认Name
                if (value is IDataIndex && pi.Name.EqualIgnoreCase("Name"))
                {
                    var di = value as IDataIndex;
                    if (di.Name.EqualIgnoreCase(ModelResolver.Current.GetName(di))) continue;
                }

                var code = Type.GetTypeCode(pi.PropertyType);

                var obj = value.GetValue(pi);
                // 默认值不参与序列化，节省空间
                if (!writeDefaultValueMember)
                {
                    var dobj = def.GetValue(pi);
                    if (Object.Equals(obj, dobj)) continue;
                    if (code == TypeCode.String && "" + obj == "" + dobj) continue;
                }

                if (code == TypeCode.String)
                {
                    // 如果别名与名称相同，则跳过
                    if (pi.Name == "Name")
                        name = (String)obj;
                    else if (pi.Name == "Alias" || pi.Name == "TableName" || pi.Name == "ColumnName")
                        if (name == (String)obj) continue;

                    // 如果DisplayName与Name或者Description相同，则跳过
                    if (pi.Name == "DisplayName")
                    {
                        var dis = (String)obj;
                        if (dis == name) continue;

                        var des = "";
                        if (value is IDataTable)
                            des = (value as IDataTable).Description;
                        else if (value is IDataColumn)
                            des = (value as IDataColumn).Description;

                        if (des != null && des.StartsWith(dis)) continue;
                    }
                }
                else if (code == TypeCode.Object)
                {
                    if (pi.PropertyType.IsArray || typeof(IEnumerable).IsAssignableFrom(pi.PropertyType) || obj is IEnumerable)
                    {
                        var sb = new StringBuilder();
                        var arr = obj as IEnumerable;
                        foreach (Object elm in arr)
                        {
                            if (sb.Length > 0) sb.Append(",");
                            sb.Append(elm);
                        }
                        obj = sb.ToString();
                    }
                    else if (pi.PropertyType == typeof(Type))
                    {
                        obj = (obj as Type).Name;
                    }
                    else
                    {
                        // 其它的不支持，跳过
                        if (XTrace.Debug) XTrace.WriteLine("不支持的类型[{0} {1}]！", pi.PropertyType.Name, pi.Name);

                        continue;
                    }
                    //if (item.Type == typeof(Type)) obj = (obj as Type).Name;
                }
                writer.WriteAttributeString(pi.Name, obj == null ? null : obj.ToString());
            }

            if (value is IDataTable)
            {
                var table = value as IDataTable;
                // 写入扩展属性作为特性
                if (table.Properties.Count > 0)
                {
                    foreach (var item in table.Properties)
                    {
                        writer.WriteAttributeString(item.Key, item.Value);
                    }
                }
            }
            else if (value is IDataColumn)
            {
                var column = value as IDataColumn;
                // 写入扩展属性作为特性
                if (column.Properties.Count > 0)
                {
                    foreach (var item in column.Properties)
                    {
                        writer.WriteAttributeString(item.Key, item.Value);
                    }
                }
            }
        }

        static DictionaryCache<Type, Object> cache = new DictionaryCache<Type, object>();
        static Object GetDefault(Type type)
        {
            return cache.GetItem(type, item => item.CreateInstance());
        }

        /// <summary>根据类型修正字段的一些默认值。仅考虑MSSQL</summary>
        /// <param name="dc"></param>
        /// <param name="oridc"></param>
        /// <returns></returns>
        static IDataColumn Fix(this IDataColumn dc, IDataColumn oridc)
        {
            if (dc == null || dc.DataType == null) return dc;

            var isnew = oridc == null || oridc == dc;

            var code = Type.GetTypeCode(dc.DataType);
            switch (code)
            {
                case TypeCode.Boolean:
                    dc.RawType = "bit";
                    dc.Length = 1;
                    dc.NumOfByte = 1;
                    dc.Nullable = true;
                    break;
                case TypeCode.Byte:
                case TypeCode.Char:
                case TypeCode.SByte:
                    dc.RawType = "tinyint";
                    dc.Length = 1;
                    dc.NumOfByte = 1;
                    dc.Nullable = true;
                    break;
                case TypeCode.DateTime:
                    dc.RawType = "datetime";
                    dc.Length = 3;
                    dc.NumOfByte = 8;
                    dc.Precision = 3;
                    dc.Nullable = true;
                    break;
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    dc.RawType = "smallint";
                    dc.Length = 5;
                    dc.NumOfByte = 2;
                    dc.Precision = 5;

                    // 自增字段非空
                    dc.Nullable = oridc == null || !oridc.Identity;
                    break;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    dc.RawType = "int";
                    dc.Length = 10;
                    dc.NumOfByte = 4;
                    dc.Precision = 10;

                    // 自增字段非空
                    dc.Nullable = oridc == null || !oridc.Identity;
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    dc.RawType = "bigint";
                    dc.Length = 19;
                    dc.NumOfByte = 8;
                    dc.Precision = 20;

                    // 自增字段非空
                    dc.Nullable = oridc == null || !oridc.Identity;
                    break;
                case TypeCode.Single:
                    dc.RawType = "real";
                    dc.Length = 7;
                    //dc.NumOfByte = 8;
                    //dc.Precision = 20;
                    dc.Nullable = true;
                    break;
                case TypeCode.Double:
                    dc.RawType = "double";
                    dc.Length = 53;
                    //dc.NumOfByte = 8;
                    //dc.Precision = 20;
                    dc.Nullable = true;
                    break;
                case TypeCode.Decimal:
                    dc.RawType = "money";
                    dc.Length = 19;
                    //dc.NumOfByte = 8;
                    //dc.Precision = 20;
                    dc.Nullable = true;
                    break;
                case TypeCode.String:
                    if (dc.Length >= 0 && dc.Length < 4000 || !isnew && oridc.RawType != "ntext")
                    {
                        var len = dc.Length;
                        if (len == 0) len = 50;
                        dc.RawType = String.Format("nvarchar({0})", len);
                        dc.NumOfByte = len * 2;

                        // 新建默认长度50，写入忽略50的长度，其它长度不能忽略
                        if (len == 50)
                            dc.Length = 50;
                        else
                            dc.Length = 0;
                    }
                    else
                    {
                        // 新建默认长度-1，写入忽略所有长度
                        if (isnew)
                        {
                            dc.RawType = "ntext";
                            dc.Length = -1;
                            dc.NumOfByte = 16;
                        }
                        else
                        {
                            //dc.NumOfByte = 16;
                            // 写入长度-1
                            dc.Length = 0;
                            oridc.Length = -1;

                            // 不写RawType
                            dc.RawType = oridc.RawType;
                            // 不写NumOfByte
                            dc.NumOfByte = oridc.NumOfByte;
                        }
                    }
                    dc.Nullable = true;
                    dc.IsUnicode = true;
                    break;
                default:
                    break;
            }

            dc.DataType = null;

            return dc;
        }

        static DictionaryCache<Type, PropertyInfo[]> cache2 = new DictionaryCache<Type, PropertyInfo[]>();
        static PropertyInfo[] GetProperties(Type type)
        {
            return cache2.GetItem(type, item => item.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => !p.Name.EqualIgnoreCase("Item")).ToArray());
        }
        #endregion

        #region 复制扩展方法
        /// <summary>复制数据表到另一个数据表，不复制数据列、索引和关系</summary>
        /// <param name="src"></param>
        /// <param name="des"></param>
        /// <returns></returns>
        public static IDataTable CopyFrom(this IDataTable src, IDataTable des)
        {
            src.ID = des.ID;
            src.TableName = des.TableName;
            src.Name = des.Name;
            src.Owner = des.Owner;
            src.DbType = des.DbType;
            src.IsView = des.IsView;
            src.DisplayName = des.DisplayName;
            src.Description = des.Description;

            return src;
        }

        /// <summary>复制数据表到另一个数据表，复制所有数据列、索引和关系</summary>
        /// <param name="src"></param>
        /// <param name="des"></param>
        /// <param name="resetColumnID">是否重置列ID</param>
        /// <returns></returns>
        public static IDataTable CopyAllFrom(this IDataTable src, IDataTable des, Boolean resetColumnID = false)
        {
            src.CopyFrom(des);
            src.Columns.AddRange(des.Columns.Select(i => src.CreateColumn().CopyFrom(i)));
            src.Indexes.AddRange(des.Indexes.Select(i => src.CreateIndex().CopyFrom(i)));
            src.Relations.AddRange(des.Relations.Select(i => src.CreateRelation().CopyFrom(i)));
            // 重载ID
            //if (resetColumnID) src.Columns.ForEach((it, i) => it.ID = i + 1);
            if (resetColumnID)
            {
                for (int i = 0; i < src.Columns.Count; i++)
                {
                    src.Columns[i].ID = i + 1;
                }
            }

            return src;
        }

        /// <summary>赋值数据列到另一个数据列</summary>
        /// <param name="src"></param>
        /// <param name="des"></param>
        /// <returns></returns>
        public static IDataColumn CopyFrom(this IDataColumn src, IDataColumn des)
        {
            src.ID = des.ID;
            src.ColumnName = des.ColumnName;
            src.Name = des.Name;
            src.DataType = des.DataType;
            src.RawType = des.RawType;
            src.Identity = des.Identity;
            src.PrimaryKey = des.PrimaryKey;
            src.Length = des.Length;
            src.NumOfByte = des.NumOfByte;
            src.Precision = des.Precision;
            src.Scale = des.Scale;
            src.Nullable = des.Nullable;
            src.IsUnicode = des.IsUnicode;
            src.Default = des.Default;
            src.DisplayName = des.DisplayName;
            src.Description = des.Description;

            return src.Fix();
        }

        /// <summary>赋值数据列到另一个数据列</summary>
        /// <param name="src"></param>
        /// <param name="des"></param>
        /// <returns></returns>
        public static IDataIndex CopyFrom(this IDataIndex src, IDataIndex des)
        {
            src.Name = des.Name;
            src.Columns = des.Columns;
            src.Unique = des.Unique;
            src.PrimaryKey = des.PrimaryKey;
            src.Computed = des.Computed;

            return src;
        }

        /// <summary>赋值数据列到另一个数据列</summary>
        /// <param name="src"></param>
        /// <param name="des"></param>
        /// <returns></returns>
        public static IDataRelation CopyFrom(this IDataRelation src, IDataRelation des)
        {
            src.Column = des.Column;
            src.RelationTable = des.RelationTable;
            src.RelationColumn = des.RelationColumn;
            src.Unique = des.Unique;
            src.Computed = des.Computed;

            return src;
        }
        #endregion

        #region 辅助
        /// <summary>表间连接，猜测关系</summary>
        /// <param name="tables"></param>
        public static void Connect(IEnumerable<IDataTable> tables)
        {
            // 某字段名，为另一个表的（表名+单主键名）形式时，作为关联字段处理
            foreach (var table in tables)
            {
                foreach (var rtable in tables)
                {
                    if (table != rtable) table.Connect(rtable);
                }
            }

            // 因为可能修改了表间关系，再修正一次
            foreach (var table in tables)
            {
                table.Fix();
            }
        }
        #endregion
    }
}