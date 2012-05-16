using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using Microsoft.CSharp;
#if NET4
using System.Linq;
#else
using NewLife.Linq;
#endif
using XCode.DataAccessLayer.Model;
using System.Xml;
using System.Text;
using System.Collections;
using NewLife.Reflection;
using System.Reflection;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Log;

namespace XCode.DataAccessLayer
{
    /// <summary>数据模型扩展</summary>
    public static class ModelHelper
    {
        #region 模型扩展方法
        /// <summary>根据字段名获取字段</summary>
        /// <param name="table"></param>
        /// <param name="name"></param>
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
            if (names == null || names.Length < 1) return null;

            return table.Columns.Where(c => names.Any(n => c.Is(n))).ToArray();
        }

        /// <summary>判断表是否等于指定名字</summary>
        /// <param name="table"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Boolean Is(this IDataTable table, String name)
        {
            if (String.IsNullOrEmpty(name)) return false;

            return table.Name.EqualIgnoreCase(name) || table.Alias.EqualIgnoreCase(name);
        }

        /// <summary>判断字段是否等于指定名字</summary>
        /// <param name="column"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Boolean Is(this IDataColumn column, String name)
        {
            if (String.IsNullOrEmpty(name)) return false;

            return column.Name.EqualIgnoreCase(name) || column.Alias.EqualIgnoreCase(name);
        }

        /// <summary>根据字段名找索引</summary>
        /// <param name="table"></param>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public static IDataIndex GetIndex(this IDataTable table, params String[] columnNames)
        {
            if (table == null || table.Indexes == null || table.Indexes.Count < 1) return null;

            var di = table.Indexes.FirstOrDefault(
                e => e.Columns != null &&
                    e.Columns.Length == columnNames.Length &&
                    !e.Columns.Except(columnNames, StringComparer.OrdinalIgnoreCase).Any());
            if (di != null) return di;

            // 用别名再试一次
            var columns = table.GetColumns(columnNames);
            if (columns == null || columns.Length < 1) return null;
            columnNames = columns.Select(e => e.Alias).ToArray();
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
                //do
                //{
                //    switch (reader.Name)
                //    {
                //        case "ID":
                //            table.ID = reader.ReadContentAsInt();
                //            break;
                //        case "Name":
                //            table.Name = reader.ReadContentAsString();
                //            break;
                //        case "Alias":
                //            table.Alias = reader.ReadContentAsString();
                //            break;
                //        case "Owner":
                //            table.Owner = reader.ReadContentAsString();
                //            break;
                //        case "DbType":
                //            table.DbType = (DatabaseType)Enum.Parse(typeof(DatabaseType), reader.ReadContentAsString());
                //            break;
                //        case "IsView":
                //            table.IsView = Boolean.Parse(reader.ReadContentAsString());
                //            break;
                //        case "Description":
                //            table.Description = reader.ReadContentAsString();
                //            break;
                //        default:
                //            break;
                //    }
                //} while (reader.MoveToNextAttribute());
                ReadXml(reader, table);
            }

            reader.ReadStartElement();

            // 读字段
            reader.MoveToElement();
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                switch (reader.Name)
                {
                    case "Columns":
                        reader.ReadStartElement();
                        while (reader.IsStartElement())
                        {
                            var dc = table.CreateColumn();
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
                            table.Relations.Add(dr);
                        }
                        reader.ReadEndElement();
                        break;
                    default:
                        break;
                }
            }

            //reader.ReadEndElement();
            if (reader.NodeType == XmlNodeType.EndElement) reader.ReadEndElement();

            return table;
        }

        /// <summary>写入</summary>
        /// <param name="table"></param>
        /// <param name="writer"></param>
        public static IDataTable WriteXml(this IDataTable table, XmlWriter writer)
        {
            // 写属性
            //writer.WriteAttributeString("ID", table.ID.ToString());
            //writer.WriteAttributeString("Name", table.Name);
            //writer.WriteAttributeString("Alias", table.Alias);
            //if (!String.IsNullOrEmpty(table.Owner)) writer.WriteAttributeString("Owner", table.Owner);
            //writer.WriteAttributeString("DbType", table.DbType.ToString());
            //writer.WriteAttributeString("IsView", table.IsView.ToString());
            //if (!String.IsNullOrEmpty(table.Description)) writer.WriteAttributeString("Description", table.Description);
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
        /// <param name="value"></param>
        public static void ReadXml(XmlReader reader, Object value)
        {
            foreach (var item in GetProperties(value.GetType()))
            {
                if (!item.Property.CanRead) continue;
                if (AttributeX.GetCustomAttribute<XmlIgnoreAttribute>(item.Member, false) != null) continue;

                var v = reader.GetAttribute(item.Name);
                if (String.IsNullOrEmpty(v)) continue;

                if (item.Type == typeof(String[]))
                {
                    var ss = v.Split(new String[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    item.SetValue(value, ss);
                }
                else
                    item.SetValue(value, TypeX.ChangeType(v, item.Type));
            }
            //reader.Skip();
        }

        /// <summary>写入</summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="writeDefaultValueMember">是否写数值为默认值的成员。为了节省空间，默认不写。</param>
        public static void WriteXml(XmlWriter writer, Object value, Boolean writeDefaultValueMember = false)
        {
            var type = value.GetType();
            Object def = GetDefault(type);

            String name = null;

            // 基本类型，输出为特性
            foreach (var item in GetProperties(type))
            {
                if (!item.Property.CanWrite) continue;
                if (AttributeX.GetCustomAttribute<XmlIgnoreAttribute>(item.Member, false) != null) continue;

                var code = Type.GetTypeCode(item.Type);

                var obj = item.GetValue(value);
                // 默认值不参与序列化，节省空间
                if (!writeDefaultValueMember)
                {
                    var dobj = item.GetValue(def);
                    if (Object.Equals(obj, dobj)) continue;
                    if (code == TypeCode.String && "" + obj == "" + dobj) continue;
                }

                if (code == TypeCode.String)
                {
                    // 如果别名与名称相同，则跳过
                    if (item.Name == "Name")
                        name = (String)obj;
                    else if (item.Name == "Alias")
                        if (name == (String)obj) continue;
                }
                else if (code == TypeCode.Object)
                {
                    if (item.Type.IsArray || typeof(IEnumerable).IsAssignableFrom(item.Type) || obj is IEnumerable)
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
                    else if (item.Type == typeof(Type))
                    {
                        obj = (obj as Type).Name;
                    }
                    else
                    {
                        // 其它的不支持，跳过
                        if (XTrace.Debug) XTrace.WriteLine("不支持的类型[{0} {1}]！", item.Type.Name, item.Name);

                        continue;
                    }
                    //if (item.Type == typeof(Type)) obj = (obj as Type).Name;
                }
                writer.WriteAttributeString(item.Name, obj == null ? null : obj.ToString());
            }
        }

        static DictionaryCache<Type, Object> cache = new DictionaryCache<Type, object>();
        static Object GetDefault(Type type)
        {
            return cache.GetItem(type, item => TypeX.CreateInstance(item));
        }

        static DictionaryCache<Type, PropertyInfoX[]> cache2 = new DictionaryCache<Type, PropertyInfoX[]>();
        static PropertyInfoX[] GetProperties(Type type)
        {
            return cache2.GetItem(type, item => item.GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(p => PropertyInfoX.Create(p)).ToArray());
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
            src.Name = des.Name;
            src.Alias = des.Alias;
            src.Owner = des.Owner;
            src.DbType = des.DbType;
            src.IsView = des.IsView;
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
            src.Name = des.Name;
            src.Alias = des.Alias;
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