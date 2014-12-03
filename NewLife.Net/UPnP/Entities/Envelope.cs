using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace NewLife.Net.UPnP
{
    /// <summary>端口映射结构</summary>
    [Serializable, XmlRoot("Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public class Envelope
    {
        //private String _encodingStyle = "http://schemas.xmlsoap.org/soap/encoding/";
        ///// <summary>属性说明</summary>
        //[XmlAttribute("encodingStyle", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        //public String encodingStyle
        //{
        //    get { return _encodingStyle; }
        //    set { _encodingStyle = value; }
        //}

        private EnvelopeBody _Body;
        /// <summary>属性说明</summary>
        [XmlElement("Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        public EnvelopeBody Body
        {
            get { return _Body; }
            set { _Body = value; }
        }

        /// <summary>信封主体</summary>
        public class EnvelopeBody : IXmlSerializable
        {
            private String _Xml;
            /// <summary>Xml文档</summary>
            public String Xml
            {
                get { return _Xml; }
                set { _Xml = value; }
            }

            private String _Fault;
            /// <summary>失败</summary>
            public String Fault
            {
                get { return _Fault; }
                set { _Fault = value; }
            }

            /// <summary>获取架构</summary>
            /// <returns></returns>
            public XmlSchema GetSchema()
            {
                return null;
            }

            /// <summary>读取Xml</summary>
            /// <param name="reader"></param>
            public void ReadXml(XmlReader reader)
            {
                String prefix = reader.Prefix;

                String xml = reader.ReadInnerXml();
                if (xml.StartsWith("<Fault") || xml.StartsWith("<" + prefix + ":Fault"))
                    Fault = xml;
                else
                    Xml = xml;
            }

            /// <summary>写入Xml</summary>
            /// <param name="writer"></param>
            public void WriteXml(XmlWriter writer)
            {
                writer.WriteRaw(Xml);
            }

            /// <summary>抛出异常</summary>
            /// <returns></returns>
            public Exception ThrowException()
            {
                if (String.IsNullOrEmpty(Fault)) return null;

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(Fault);

                String msg = "UPnP Error";

                //XmlNamespaceManager mgr = new XmlNamespaceManager(doc.NameTable);
                //mgr.AddNamespace(doc.DocumentElement.Prefix, doc.DocumentElement.NamespaceURI);
                //XmlNode node = doc.SelectSingleNode("//errordescription", mgr);
                XmlNode node = doc.SelectSingleNode("/*/*/*/*[last()]");
                if (node != null) msg = node.InnerText;

                throw new XException(msg);
            }
        }
    }
}