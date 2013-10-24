using System;
using System.Xml;
using System.Xml.Serialization;
using NewLife.Reflection;
using NewLife.Xml;

namespace NewLife.Net.UPnP
{
    /// <summary>UPnP操作</summary>
    /// <typeparam name="TEntity"></typeparam>
    public class UPnPAction<TEntity> : UPnPAction where TEntity : UPnPAction<TEntity>, new()
    {
        /// <summary>XML反序列化为实体</summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static TEntity FromXml(String xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            TEntity entity = new TEntity();

            //TypeX tx = TypeX.Create(typeof(TEntity));
            foreach (var item in typeof(TEntity).GetProperties())
            {
                if (AttributeX.GetCustomAttribute<XmlIgnoreAttribute>(item, true) != null) continue;

                var elmName = item.Name;
                var att = AttributeX.GetCustomAttribute<XmlElementAttribute>(item, true);
                if (att != null && !String.IsNullOrEmpty(att.ElementName)) elmName = att.ElementName;

                var node = doc.SelectSingleNode("//" + elmName);
                if (node == null) continue;

                //item.SetValue(entity, Convert.ChangeType(node.InnerText, item.PropertyType));
                entity.SetValue(item, Convert.ChangeType(node.InnerText, item.PropertyType));
            }
            return entity;

            //XmlSerializer serial = new XmlSerializer(typeof(TEntity));
            //using (StringReader reader = new StringReader(xml))
            //{
            //    return serial.Deserialize(reader) as TEntity;
            //}
        }
    }

    /// <summary>UPnP操作</summary>
    public abstract class UPnPAction
    {
        private String _Name;
        /// <summary>名称</summary>
        [XmlIgnore]
        public virtual String Name
        {
            get { return _Name ?? (_Name = this.GetType().Name); }
            set { _Name = value; }
        }

        /// <summary>序列化实体为Xml</summary>
        /// <param name="xmlns"></param>
        /// <returns></returns>
        public virtual String ToXml(String xmlns)
        {
            var doc = new XmlDocument();
            var root = doc.CreateElement("u", Name, xmlns);
            doc.AppendChild(root);

            //TypeX tx = TypeX.Create(this.GetType());
            foreach (var item in this.GetType().GetProperties())
            {
                if (AttributeX.GetCustomAttribute<XmlIgnoreAttribute>(item, true) != null) continue;

                var elmName = item.Name;
                var att = AttributeX.GetCustomAttribute<XmlElementAttribute>(item, true);
                if (att != null && !String.IsNullOrEmpty(att.ElementName)) elmName = att.ElementName;

                var elm = doc.CreateElement(elmName);
                var v = this.GetValue(item);
                var str = v == null ? "" : v.ToString();

                var text = doc.CreateTextNode(str);
                elm.AppendChild(text);

                root.AppendChild(elm);
            }

            return doc.InnerXml;

            //XmlSerializer serial = new XmlSerializer(this.GetType());
            //MemoryStream ms = new MemoryStream();
            ////serial.Serialize(ms, this);

            //XmlWriterSettings setting = new XmlWriterSettings();
            //setting.Encoding = Encoding.UTF8;
            //// 去掉开头 <?xml version="1.0" encoding="utf-8"?>
            //setting.OmitXmlDeclaration = true;

            //using (XmlWriter writer = XmlWriter.Create(ms, setting))
            //{
            //    // 去掉默认命名空间xmlns:xsd和xmlns:xsi
            //    XmlSerializerNamespaces xsns = new XmlSerializerNamespaces();
            //    if (!String.IsNullOrEmpty(xmlns)) xsns.Add("u", xmlns);

            //    serial.Serialize(writer, this, xsns);
            //    byte[] bts = ms.ToArray();
            //    String xml = Encoding.UTF8.GetString(bts);

            //    if (!String.IsNullOrEmpty(xml)) xml = xml.Trim();

            //    return xml;
            //}
        }

        /// <summary>序列化实体为Soap</summary>
        /// <param name="xmlns"></param>
        /// <returns></returns>
        public virtual String ToSoap(String xmlns)
        {
            var xml = ToXml(xmlns);

            var env = new Envelope();
            env.Body = new Envelope.EnvelopeBody();
            env.Body.Xml = xml;

            //var xsn = new XmlSerializerNamespaces();
            //xsn.Add("s", "http://schemas.xmlsoap.org/soap/envelope/");

            //var serial = new XmlSerializer(typeof(Envelope));
            //var ms = new MemoryStream();

            //using (var writer = XmlWriter.Create(ms))
            //{
            //    serial.Serialize(writer, env, xsn);
            //}

            //xml = Encoding.UTF8.GetString(ms.ToArray());

            xml = env.ToXml(null, "s", "http://schemas.xmlsoap.org/soap/envelope/", true);
            return xml;
        }
    }
}
