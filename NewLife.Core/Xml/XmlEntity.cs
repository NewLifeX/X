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
            if (String.IsNullOrEmpty(xml)) return null;

            var serial = new XmlSerializer(typeof(TEntity));
            using (var reader = new StringReader(xml))
            {
                return serial.Deserialize(reader) as TEntity;
            }
        }

        /// <summary>从一个XML文件中加载对象</summary>
        /// <param name="filename">若为空，则默认为类名加xml后缀</param>
        /// <returns></returns>
        public static TEntity LoadFile(String filename)
        {
            if (String.IsNullOrEmpty(filename)) filename = typeof(TEntity).Name + ".xml";
            if (!File.Exists(filename)) return new TEntity();

            var serial = new XmlSerializer(typeof(TEntity));
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return serial.Deserialize(reader) as TEntity;
                }
            }
        }

        /// <summary>输出XML</summary>
        /// <returns></returns>
        public virtual String ToXml()
        {
            // 去掉默认命名空间xmlns:xsd和xmlns:xsi
            return ToXml("", "");
        }

        /// <summary>输出XML</summary>
        /// <param name="prefix">前缀</param>
        /// <param name="ns">命名空间</param>
        /// <param name="includeDeclaration">是否包含Xml声明</param>
        /// <returns></returns>
        public virtual String ToXml(String prefix, String ns, Boolean includeDeclaration = false)
        {
            var serial = new XmlSerializer(typeof(TEntity));
            using (var stream = new MemoryStream())
            {
                var setting = new XmlWriterSettings();
                setting.Encoding = new UTF8Encoding(false);
                setting.Indent = true;
                // 去掉开头 <?xml version="1.0" encoding="utf-8"?>
                setting.OmitXmlDeclaration = includeDeclaration;
                using (var writer = XmlWriter.Create(stream, setting))
                {
                    if (ns == null)
                        serial.Serialize(writer, this);
                    else
                    {
                        var xsns = new XmlSerializerNamespaces();
                        xsns.Add(prefix, ns);
                        serial.Serialize(writer, this, xsns);
                    }
                    return Encoding.UTF8.GetString(stream.ToArray());
                }
            }
        }

        /// <summary>输出内部XML</summary>
        /// <returns></returns>
        public virtual String ToInnerXml() { return ToXml("", "", true); }

        /// <summary>保存到文件中</summary>
        /// <param name="filename">若为空，则默认为类名加xml后缀</param>
        public virtual void Save(String filename)
        {
            //if (String.IsNullOrEmpty(filename)) return;
            if (String.IsNullOrEmpty(filename)) filename = typeof(TEntity).Name + ".xml";

            if (File.Exists(filename)) File.Delete(filename);
            var serial = new XmlSerializer(typeof(TEntity));
            using (var stream = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    serial.Serialize((TextWriter)writer, this);
                }
            }
        }
    }
}