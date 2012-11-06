using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace NewLife.Xml
{
    /// <summary>支持Xml序列化的泛型字典类 </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [XmlRoot("Dictionary")]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
    {
        /// <summary></summary>
        public SerializableDictionary() : base() { }

        /// <summary></summary>
        /// <param name="dictionary"></param>
        public SerializableDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary) { }

        //public SerializableDictionary(IEqualityComparer<TKey> comparer) : base(comparer) { }

        //public SerializableDictionary(int capacity) : base(capacity) { }

        //public SerializableDictionary(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer) { }

        //protected SerializableDictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }

        #region IXmlSerializable Members

        XmlSchema IXmlSerializable.GetSchema() { return null; }

        /// <summary>读取Xml</summary>
        /// <param name="reader">Xml读取器</param>
        public void ReadXml(XmlReader reader)
        {
            var ks = new XmlSerializer(typeof(TKey));
            var vs = new XmlSerializer(typeof(TValue));
            if (reader.IsEmptyElement || !reader.Read()) return;

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                reader.ReadStartElement("Item");

                reader.ReadStartElement("Key");
                var key = (TKey)ks.Deserialize(reader);
                reader.ReadEndElement();

                reader.ReadStartElement("Value");
                var value = (TValue)vs.Deserialize(reader);
                reader.ReadEndElement();

                reader.ReadEndElement();

                this.Add(key, value);
                reader.MoveToContent();
            }
            reader.ReadEndElement();
        }

        /// <summary>写入Xml</summary>
        /// <param name="writer">Xml写入器</param>
        public void WriteXml(System.Xml.XmlWriter writer)
        {
            var ks = new XmlSerializer(typeof(TKey));
            var vs = new XmlSerializer(typeof(TValue));
            foreach (var kv in this)
            {
                writer.WriteStartElement("Item");

                writer.WriteStartElement("Key");
                ks.Serialize(writer, kv.Key);
                writer.WriteEndElement();

                writer.WriteStartElement("Value");
                vs.Serialize(writer, kv.Value);
                writer.WriteEndElement();

                writer.WriteEndElement();
            }
        }
        #endregion
    }
}