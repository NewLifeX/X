using System;
using System.Collections;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Reflection;

namespace XCode.DataAccessLayer
{
    /// <summary>可序列化数据成员</summary>
    abstract class SerializableDataMember : IXmlSerializable
    {
        #region IXmlSerializable 成员
        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            ModelHelper.ReadXml(reader, this);

            // 跳过当前节点
            reader.Skip();
        }

        static DictionaryCache<Type, Object> cache = new DictionaryCache<Type, object>();
        static Object GetDefault(Type type)
        {
            return cache.GetItem(type, item => TypeX.CreateInstance(item));
        }

        /// <summary>是否写数值为默认值的成员。为了节省空间，默认不写。</summary>
        protected virtual Boolean WriteDefaultValueMember { get { return false; } }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            ModelHelper.WriteXml(writer, this, WriteDefaultValueMember);
        }
        #endregion
    }
}