using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Reflection;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 可序列化数据成员
    /// </summary>
    abstract class SerializableDataMember : IXmlSerializable
    {
        #region IXmlSerializable 成员
        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            PropertyInfo[] pis = this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (PropertyInfoX item in pis)
            {
                if (!item.Property.CanRead) continue;
                if (AttributeX.GetCustomAttribute<XmlIgnoreAttribute>(item.Member, false) != null) continue;

                String v = reader.GetAttribute(item.Name);
                if (String.IsNullOrEmpty(v)) continue;

                if (item.Type == typeof(String[]))
                {
                    String[] ss = v.Split(new String[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    item.SetValue(this, ss);
                }
                else
                    item.SetValue(this, TypeX.ChangeType(v, item.Type));
            }
            reader.Skip();
        }

        static DictionaryCache<Type, Object> cache = new DictionaryCache<Type, object>();
        static Object GetDefault(Type type)
        {
            return cache.GetItem(type, item => TypeX.CreateInstance(item));
        }

        /// <summary>
        /// 是否写数值为默认值的成员。为了节省空间，默认不写。
        /// </summary>
        protected virtual Boolean WriteDefaultValueMember { get { return false; } }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            Object def = GetDefault(this.GetType());

            String name = null;

            PropertyInfo[] pis = this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            //List<PropertyInfo> list = TypeX.Create(this.GetType()).Properties;
            // 基本类型，输出为特性
            foreach (PropertyInfoX item in pis)
            {
                if (!item.Property.CanWrite) continue;
                if (AttributeX.GetCustomAttribute<XmlIgnoreAttribute>(item.Member, false) != null) continue;

                TypeCode code = Type.GetTypeCode(item.Type);

                Object obj = item.GetValue(this);
                // 默认值不参与序列化，节省空间
                if (!WriteDefaultValueMember)
                {
                    Object dobj = item.GetValue(def);
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
                        StringBuilder sb = new StringBuilder();
                        IEnumerable arr = obj as IEnumerable;
                        foreach (Object elm in arr)
                        {
                            if (sb.Length > 0) sb.Append(",");
                            sb.Append(elm);
                        }
                        obj = sb.ToString();
                    }
                    if (item.Type == typeof(Type)) obj = (obj as Type).Name;
                }
                writer.WriteAttributeString(item.Name, obj == null ? null : obj.ToString());
            }
        }
        #endregion

        #region IAccessor 成员
        //[NonSerialized]
        //Boolean isAtt = false;

        //bool IAccessor.Read(IReader reader)
        //{
        //    if (reader is XmlReaderX)
        //    {
        //        XmlReaderX rx = reader as XmlReaderX;
        //        isAtt = rx.Settings.MemberAsAttribute;
        //        rx.Settings.MemberAsAttribute = true;
        //    }
        //    return false;
        //}

        //bool IAccessor.ReadComplete(IReader reader, bool success)
        //{
        //    if (reader is XmlReaderX)
        //    {
        //        XmlReaderX rx = reader as XmlReaderX;
        //        rx.Settings.MemberAsAttribute = isAtt;
        //    }
        //    return success;
        //}

        //bool IAccessor.Write(IWriter writer)
        //{
        //    if (writer is XmlWriterX)
        //    {
        //        XmlWriterX rx = writer as XmlWriterX;
        //        isAtt = rx.Settings.MemberAsAttribute;
        //        rx.Settings.MemberAsAttribute = true;
        //    }
        //    return false;
        //}

        //bool IAccessor.WriteComplete(IWriter writer, bool success)
        //{
        //    if (writer is XmlWriterX)
        //    {
        //        XmlWriterX rx = writer as XmlWriterX;
        //        rx.Settings.MemberAsAttribute = isAtt;
        //    }
        //    return success;
        //}
        #endregion
    }
}