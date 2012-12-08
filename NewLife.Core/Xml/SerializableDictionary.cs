using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using NewLife.Reflection;

#if NET4
using System;
#endif

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

        #region IXmlSerializable 成员

        XmlSchema IXmlSerializable.GetSchema() { return null; }

        /// <summary>读取Xml</summary>
        /// <param name="reader">Xml读取器</param>
        public void ReadXml(XmlReader reader)
        {
            if (reader.IsEmptyElement || !reader.Read()) return;

            var kfunc = CreateReader<TKey>();
            var vfunc = CreateReader<TValue>();
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                reader.ReadStartElement("Item");

                reader.ReadStartElement("Key");
                var key = kfunc(reader);
                reader.ReadEndElement();

                reader.ReadStartElement("Value");
                var value = vfunc(reader);
                reader.ReadEndElement();

                reader.ReadEndElement();

                this.Add(key, value);
                reader.MoveToContent();
            }
            reader.ReadEndElement();
        }

        /// <summary>写入Xml</summary>
        /// <param name="writer">Xml写入器</param>
        public void WriteXml(XmlWriter writer)
        {
            var kfunc = CreateWriter<TKey>();
            var vfunc = CreateWriter<TValue>();
            foreach (var kv in this)
            {
                writer.WriteStartElement("Item");

                writer.WriteStartElement("Key");
                kfunc(writer, kv.Key);
                writer.WriteEndElement();

                writer.WriteStartElement("Value");
                vfunc(writer, kv.Value);
                writer.WriteEndElement();

                writer.WriteEndElement();
            }
        }

        static Func<XmlReader, T> CreateReader<T>()
        {
            var type = typeof(T);
            if (type.CanXmlConvert()) return r => XmlHelper.XmlConvertFromString<T>(r.ReadString());

            // 因为一个委托将会被调用多次，因此把序列化对象声明在委托外面，让其生成匿名类，便于重用
            var xs = new XmlSerializer(type);
            return r => (T)xs.Deserialize(r);
        }

        static Action<XmlWriter, T> CreateWriter<T>()
        {
            var type = typeof(T);
            if (type.CanXmlConvert()) return (w, v) => w.WriteString(XmlHelper.XmlConvertToString(v));

            // 因为一个委托将会被调用多次，因此把序列化对象声明在委托外面，让其生成匿名类，便于重用
            var xs = new XmlSerializer(type);
            var xsns = new XmlSerializerNamespaces();
            xsns.Add("", "");
            return (w, v) => xs.Serialize(w, v, xsns);
        }
        #endregion
    }
}