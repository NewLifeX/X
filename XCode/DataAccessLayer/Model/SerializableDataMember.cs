using System.Xml;
using System.Xml.Serialization;

namespace XCode.DataAccessLayer
{
    /// <summary>可序列化数据成员</summary>
    abstract class SerializableDataMember : IXmlSerializable
    {
        #region IXmlSerializable 成员
        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema() { return null; }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            ModelHelper.ReadXml(reader, this);

            // 跳过当前节点
            reader.Skip();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            ModelHelper.WriteXml(writer, this);
        }
        #endregion
    }
}