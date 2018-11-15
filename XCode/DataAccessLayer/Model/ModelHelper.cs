using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
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

        /// <summary>获取全部字段，包括继承的父类</summary>
        /// <param name="table"></param>
        /// <param name="tables">在该表集合里面找父类</param>
        /// <param name="baseFirst">是否父类字段在前</param>
        /// <returns></returns>
        public static List<IDataColumn> GetAllColumns(this IDataTable table, IEnumerable<IDataTable> tables, Boolean baseFirst = true)
        {
            var list = new List<List<IDataColumn>>();

            var dt = table;
            while (dt != null)
            {
                list.Add(dt.Columns);

                var baseType = dt.BaseType;
                if (baseType.IsNullOrWhiteSpace()) break;

                dt = tables.FirstOrDefault(e => baseType.EqualIgnoreCase(e.Name, e.TableName));
            }

            if (baseFirst) list.Reverse();

            var dts = new List<IDataColumn>();
            foreach (var item in list)
            {
                dts.AddRange(item);
            }
            return dts;
        }

        /// <summary>判断表是否等于指定名字</summary>
        /// <param name="table"></param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static Boolean Is(this IDataTable table, String name)
        {
            if (String.IsNullOrEmpty(name)) return false;

            return name.EqualIgnoreCase(table.TableName, table.Name);
        }

        /// <summary>判断字段是否等于指定名字</summary>
        /// <param name="column"></param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static Boolean Is(this IDataColumn column, String name)
        {
            if (String.IsNullOrEmpty(name)) return false;

            return name.EqualIgnoreCase(column.ColumnName, column.Name);
        }

        static Boolean EqualIgnoreCase(this String[] src, String[] des)
        {
            if (src == null || src.Length == 0) return des == null || des.Length == 0;
            if (des == null || des.Length == 0) return false;

            if (src.Length != des.Length) return false;

            //return !src.Except(des, StringComparer.OrdinalIgnoreCase).Any();
            return src.SequenceEqual(des, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>根据字段名找索引</summary>
        /// <param name="table"></param>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public static IDataIndex GetIndex(this IDataTable table, params String[] columnNames)
        {
            var dis = table?.Indexes;
            if (dis == null || dis.Count < 1 || columnNames == null || columnNames.Length < 1) return null;

            var di = dis.FirstOrDefault(e => e != null && e.Columns.EqualIgnoreCase(columnNames));
            if (di != null) return di;

            // 用别名再试一次
            var columns = table.GetColumns(columnNames);
            if (columns.Length != columnNames.Length) return null;

            var names = columns.Select(e => e.Name).ToArray();
            return dis.FirstOrDefault(e => e.Columns.EqualIgnoreCase(names));
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

            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true
            };

            var writer = XmlWriter.Create(ms, settings);
            writer.WriteStartDocument();

            var hasAttr = atts != null && atts.Count > 0;
            // 如果含有命名空间则添加
            if (hasAttr && atts.TryGetValue("xmlns", out var xmlns)) { writer.WriteStartElement("Tables", xmlns); }
            else writer.WriteStartElement("Tables");

            // 写入版本
            writer.WriteAttributeString("Version", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            if (hasAttr)
            {
                foreach (var item in atts)
                {
                    // 处理命名空间
                    if (item.Key.EqualIgnoreCase("xmlns")) continue;
                    if (item.Key.Contains(':'))
                    {
                        var keys = item.Key.Split(':');
                        if (keys.Length != 2) continue;
                        var frefix = keys[0];
                        var localName = keys[1];
                        writer.WriteAttributeString(frefix, localName, null, item.Value);
                    }
                    else if (!item.Key.EqualIgnoreCase("Version")) writer.WriteAttributeString(item.Key, item.Value);
                    //if (!String.IsNullOrEmpty(item.Value)) writer.WriteElementString(item.Key, item.Value);
                    //writer.WriteElementString(item.Key, item.Value);
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
            if (xml.IsNullOrEmpty()) return null;
            if (createTable == null) throw new ArgumentNullException(nameof(createTable));

            var settings = new XmlReaderSettings
            {
                IgnoreWhitespace = true,
                IgnoreComments = true
            };

            var reader = XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(xml)), settings);
            while (reader.NodeType != XmlNodeType.Element) { if (!reader.Read()) return null; }

            if (atts != null && reader.HasAttributes)
            {
                reader.MoveToFirstAttribute();
                do
                {
                    atts[reader.Name] = reader.Value;
                } while (reader.MoveToNextAttribute());
            }

            reader.ReadStartElement();

            var list = new List<IDataTable>();
            while (reader.IsStartElement())
            {
                if (reader.Name.EqualIgnoreCase("Table"))
                {
                    var table = createTable();
                    (table as IXmlSerializable).ReadXml(reader);
                    list.Add(table);
                }
                else if (atts != null)
                {
                    var name = reader.Name;
                    reader.ReadStartElement();
                    if (reader.NodeType == XmlNodeType.Text)
                    {
                        atts[name] = reader.ReadContentAsString();
                    }
                    if (reader.NodeType == XmlNodeType.EndElement) reader.ReadEndElement();
                }
                else
                {
                    // 这里必须处理，否则加载特殊Xml文件时将会导致死循环
                    reader.Read();
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
                        while (reader.IsStartElement())
                        {
                            var dc = table.CreateColumn();
                            var v = reader.GetAttribute("DataType");
                            if (v != null)
                            {
                                dc.DataType = v.GetTypeEx(false);
                                v = reader.GetAttribute("Length");
                                if (v != null && Int32.TryParse(v, out var len)) dc.Length = len;

                                dc = Fix(dc, dc);
                            }
                            (dc as IXmlSerializable).ReadXml(reader);
                            table.Columns.Add(dc);
                        }
                        reader.ReadEndElement();

                        // 修正可能的主字段
                        if (!table.Columns.Any(e => e.Master))
                        {
                            var f = table.Columns.FirstOrDefault(e => e.Name.EqualIgnoreCase("Name", "Title"));
                            if (f != null) f.Master = true;
                        }
                        break;
                    case "Indexes":
                        reader.ReadStartElement();
                        while (reader.IsStartElement())
                        {
                            var di = table.CreateIndex();
                            (di as IXmlSerializable).ReadXml(reader);
                            di.Fix();
                            table.Indexes.Add(di);
                        }
                        reader.ReadEndElement();
                        break;
                    case "Relations":
                        reader.ReadStartElement();
                        reader.Skip();
                        reader.ReadEndElement();
                        break;
                    default:
                        // 这里必须处理，否则加载特殊Xml文件时将会导致死循环
                        reader.Read();
                        break;
                }
            }

            if (reader.NodeType == XmlNodeType.EndElement) reader.ReadEndElement();

            // 修正
            table.Fix();

            return table;
        }

        /// <summary>写入</summary>
        /// <param name="table"></param>
        /// <param name="writer"></param>
        public static IDataTable WriteXml(this IDataTable table, XmlWriter writer)
        {
            WriteXml(writer, table);

            // 写字段
            if (table.Columns.Count > 0)
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
            if (table.Indexes.Count > 0)
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

            return table;
        }

        /// <summary>读取</summary>
        /// <param name="reader"></param>
        /// <param name="value">数值</param>
        public static void ReadXml(XmlReader reader, Object value)
        {
            var pis = value.GetType().GetProperties(true);
            var names = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            foreach (var pi in pis)
            {
                if (!pi.CanRead || !pi.CanWrite) continue;
                if (pi.GetCustomAttribute<XmlIgnoreAttribute>(false) != null) continue;

                // 已处理的特性
                names.Add(pi.Name);

                var v = reader.GetAttribute(pi.Name);
                if (v.IsNullOrEmpty()) continue;

                if (pi.PropertyType == typeof(String[]))
                {
                    var ss = v.Split(new String[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    // 去除前后空格，因为手工修改xml的时候，可能在逗号后加上空格
                    for (var i = 0; i < ss.Length; i++)
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
                // 写入的时候省略了相同的TableName/ColumnName
                var v2 = (String)value.GetValue(pi2);
                if (String.IsNullOrEmpty(v2))
                {
                    value.SetValue(pi2, value.GetValue(pi1));
                }
            }
            // 自增字段非空
            if (value is IDataColumn)
            {
                var dc = value as IDataColumn;
                if (dc.Identity) dc.Nullable = false;

                // 优化字段名
                //dc.Fix();
                if (dc.Name.IsNullOrEmpty())
                    dc.Name = ModelResolver.Current.GetName(dc.ColumnName);
                else if (dc.ColumnName.IsNullOrEmpty() || dc.ColumnName == dc.Name)
                {
                    dc.ColumnName = dc.Name;
                    dc.Name = ModelResolver.Current.GetName(dc.ColumnName);
                }
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
            foreach (var pi in type.GetProperties(true))
            {
                if (!pi.CanWrite) continue;
                //if (pi.GetCustomAttribute<XmlIgnoreAttribute>(false) != null) continue;
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
                    if (Equals(obj, dobj)) continue;
                    if (code == TypeCode.String && "" + obj == "" + dobj) continue;
                }

                if (code == TypeCode.String)
                {
                    // 如果别名与名称相同，则跳过，不区分大小写
                    if (pi.Name == "Name")
                        name = (String)obj;
                    else if (pi.Name == "TableName" || pi.Name == "ColumnName")
                        if (name.EqualIgnoreCase((String)obj)) continue;
                }
                else if (code == TypeCode.Object)
                {
                    var ptype = pi.PropertyType;
                    if (ptype.IsArray || ptype.As<IEnumerable>() || obj is IEnumerable)
                    {
                        var sb = new StringBuilder();
                        var arr = obj as IEnumerable;
                        foreach (var elm in arr)
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
                writer.WriteAttributeString(pi.Name, obj?.ToString());
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
                        if (!item.Key.EqualIgnoreCase("DisplayName", "NumOfByte")) writer.WriteAttributeString(item.Key, item.Value);
                    }
                }
            }
        }

        static ConcurrentDictionary<Type, Object> cache = new ConcurrentDictionary<Type, Object>();
        static Object GetDefault(Type type)
        {
            return cache.GetOrAdd(type, item => item.CreateInstance());
        }
        #endregion

        #region 修正连接
        /// <summary>根据类型修正字段的一些默认值</summary>
        /// <param name="dc"></param>
        /// <param name="oridc"></param>
        /// <returns></returns>
        static IDataColumn Fix(this IDataColumn dc, IDataColumn oridc)
        {
            if (dc?.DataType == null) return dc;

            var isnew = oridc == null || oridc == dc;

            switch (dc.DataType.GetTypeCode())
            {
                case TypeCode.Boolean:
                    dc.RawType = "bit";
                    dc.Nullable = false;
                    break;
                case TypeCode.Byte:
                case TypeCode.Char:
                case TypeCode.SByte:
                    dc.RawType = "tinyint";
                    dc.Nullable = false;
                    break;
                case TypeCode.DateTime:
                    dc.RawType = "datetime";
                    dc.Nullable = true;
                    break;
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    dc.RawType = "smallint";
                    dc.Nullable = false;
                    break;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    dc.RawType = "int";
                    dc.Nullable = false;
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    dc.RawType = "bigint";
                    dc.Nullable = false;
                    break;
                case TypeCode.Single:
                    dc.RawType = "real";
                    dc.Nullable = false;
                    break;
                case TypeCode.Double:
                    dc.RawType = "double";
                    dc.Nullable = false;
                    break;
                case TypeCode.Decimal:
                    dc.RawType = "money";
                    dc.Nullable = false;
                    break;
                case TypeCode.String:
                    if (dc.Length >= 0 && dc.Length < 4000 || !isnew && oridc.RawType != "ntext")
                    {
                        var len = dc.Length;
                        if (len == 0) len = 50;
                        dc.RawType = String.Format("nvarchar({0})", len);

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
                        }
                        else
                        {
                            // 写入长度-1
                            dc.Length = 0;
                            oridc.Length = -1;

                            // 不写RawType
                            dc.RawType = oridc.RawType;
                        }
                    }
                    dc.Nullable = true;
                    break;
                default:
                    break;
            }

            dc.DataType = null;
            if (oridc.Table.DbType != DatabaseType.SqlServer) dc.RawType = null;

            return dc;
        }
        #endregion
    }
}