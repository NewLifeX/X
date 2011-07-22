using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Xml;
using NewLife.Reflection;
using NewLife.Collections;
using NewLife.Serialization;
using NewLife.Xml;
using System.Reflection;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 可序列化数据成员
    /// </summary>
    public abstract class SerializableDataMember : IXmlSerializable
    {
        #region IXmlSerializable 成员
        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            foreach (PropertyInfoX item in TypeX.Create(this.GetType()).Properties)
            {
                if (!item.Property.CanRead) continue;
                if (AttributeX.GetCustomAttribute<XmlIgnoreAttribute>(item.Member, false) != null) continue;

                String v = reader.GetAttribute(item.Name);
                if (String.IsNullOrEmpty(v)) continue;

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

            List<PropertyInfo> list = TypeX.Create(this.GetType()).Properties;
            // 基本类型，输出为特性
            foreach (PropertyInfoX item in list)
            {
                if (!item.Property.CanWrite) continue;
                if (Type.GetTypeCode(item.Type) == TypeCode.Object) continue;
                if (AttributeX.GetCustomAttribute<XmlIgnoreAttribute>(item.Member, false) != null) continue;

                Object obj = item.GetValue(this);
                // 默认值不参与序列化，节省空间
                if (!WriteDefaultValueMember && Object.Equals(obj, item.GetValue(def))) continue;

                if (item.Type == typeof(Type)) obj = (obj as Type).Name;
                writer.WriteAttributeString(item.Name, obj == null ? null : obj.ToString());
            }
            // 非基本类型，输出为子节点
            foreach (PropertyInfoX item in list)
            {
                if (!item.Property.CanWrite) continue;
                if (Type.GetTypeCode(item.Type) != TypeCode.Object) continue;
                if (AttributeX.GetCustomAttribute<XmlIgnoreAttribute>(item.Member, false) != null) continue;

                Object obj = item.GetValue(this);
                // 默认值不参与序列化，节省空间
                if (!WriteDefaultValueMember && Object.Equals(obj, item.GetValue(def))) continue;

                if (item.Type == typeof(Type)) obj = (obj as Type).Name;
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