using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace NewLife.Xml
{
    /// <summary>Xml实体基类</summary>
    /// <remarks>主要提供数据实体和XML文件之间的映射功能</remarks>
    public abstract class XmlEntity<TEntity> where TEntity : XmlEntity<TEntity>, new()
    {
        /// <summary>从一段XML文本中加载对象</summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static TEntity Load(String xml)
        {
            if (String.IsNullOrEmpty(xml)) return null;

            xml = xml.Trim();

            XmlSerializer serial = new XmlSerializer(typeof(TEntity));
            using (StringReader reader = new StringReader(xml))
            {
                return (serial.Deserialize(reader) as TEntity);
            }
        }

        /// <summary>从一个XML文件中加载对象</summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static TEntity LoadFile(String filename)
        {
            if (String.IsNullOrEmpty(filename) || !File.Exists(filename)) return null;

            XmlSerializer serial = new XmlSerializer(typeof(TEntity));
            using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return (serial.Deserialize(reader) as TEntity);
                }
            }
        }

        /// <summary>输出XML</summary>
        /// <returns></returns>
        public virtual String ToXml()
        {
            XmlSerializer serial = new XmlSerializer(typeof(TEntity));
            using (MemoryStream stream = new MemoryStream())
            {
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    serial.Serialize((TextWriter)writer, this);
                    byte[] bts = stream.ToArray();
                    String xml = Encoding.UTF8.GetString(bts);

                    if (!String.IsNullOrEmpty(xml)) xml = xml.Trim();

                    return xml;
                }
            }
        }

        /// <summary>输出Xml</summary>
        /// <returns></returns>
        public virtual String ToXml(String prefix, String ns)
        {
            XmlSerializer serial = new XmlSerializer(typeof(TEntity));
            using (MemoryStream stream = new MemoryStream())
            {
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    if (String.IsNullOrEmpty(ns))
                        serial.Serialize((TextWriter)writer, this);
                    else
                    {
                        XmlSerializerNamespaces xsns = new XmlSerializerNamespaces();
                        xsns.Add(prefix, ns);
                        serial.Serialize((TextWriter)writer, this, xsns);
                    }
                    byte[] bts = stream.ToArray();
                    String xml = Encoding.UTF8.GetString(bts);

                    if (!String.IsNullOrEmpty(xml)) xml = xml.Trim();

                    return xml;
                }
            }
        }

        /// <summary>输出内部XML</summary>
        /// <returns></returns>
        public virtual String ToInnerXml()
        {
            XmlSerializer serial = new XmlSerializer(typeof(TEntity));
            using (MemoryStream stream = new MemoryStream())
            {
                XmlWriterSettings setting = new XmlWriterSettings();
                setting.Encoding = Encoding.UTF8;
                // 去掉开头 <?xml version="1.0" encoding="utf-8"?>
                setting.OmitXmlDeclaration = true;

                using (XmlWriter writer = XmlWriter.Create(stream, setting))
                {
                    // 去掉默认命名空间xmlns:xsd和xmlns:xsi
                    XmlSerializerNamespaces xsns = new XmlSerializerNamespaces();
                    xsns.Add("", "");

                    serial.Serialize(writer, this, xsns);
                    byte[] bts = stream.ToArray();
                    String xml = Encoding.UTF8.GetString(bts);

                    if (!String.IsNullOrEmpty(xml)) xml = xml.Trim();

                    return xml;
                }
            }
        }

        /// <summary>保存到文件中</summary>
        /// <param name="filename"></param>
        public virtual void Save(String filename)
        {
            if (String.IsNullOrEmpty(filename)) return;

            XmlSerializer serial = new XmlSerializer(typeof(TEntity));
            using (FileStream stream = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    serial.Serialize((TextWriter)writer, this);
                }
            }
        }
    }
}