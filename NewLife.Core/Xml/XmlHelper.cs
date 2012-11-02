using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using NewLife.Exceptions;

namespace NewLife.Xml
{
    /// <summary>Xml辅助类</summary>
    public static class XmlHelper
    {
        /// <summary>序列化为Xml</summary>
        /// <param name="obj">要序列化为Xml的对象</param>
        /// <returns>Xml字符串</returns>
        public static String ToXml(this Object obj)
        {
            // 去掉默认命名空间xmlns:xsd和xmlns:xsi
            return obj.ToXml("", "");
        }

        /// <summary>序列化为Xml字符串</summary>
        /// <param name="obj">要序列化为Xml的对象</param>
        /// <param name="prefix">前缀</param>
        /// <param name="ns">命名空间，设为0长度字符串可去掉默认命名空间xmlns:xsd和xmlns:xsi</param>
        /// <param name="includeDeclaration">是否包含Xml声明</param>
        /// <returns>Xml字符串</returns>
        public static String ToXml(this Object obj, String prefix = null, String ns = null, Boolean includeDeclaration = false)
        {
            using (var stream = new MemoryStream())
            {
                ToXml(obj, stream, Encoding.UTF8, prefix, ns, includeDeclaration);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        /// <summary>序列化为Xml数据流</summary>
        /// <param name="obj">要序列化为Xml的对象</param>
        /// <param name="stream">目标数据流</param>
        /// <param name="encoding">编码</param>
        /// <param name="prefix">前缀</param>
        /// <param name="ns">命名空间，设为0长度字符串可去掉默认命名空间xmlns:xsd和xmlns:xsi</param>
        /// <param name="includeDeclaration">是否包含Xml声明 &lt;?xml version="1.0" encoding="utf-8"?&gt;</param>
        /// <returns>Xml字符串</returns>
        public static void ToXml(this Object obj, Stream stream, Encoding encoding = null, String prefix = null, String ns = null, Boolean includeDeclaration = false)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (encoding == null) encoding = Encoding.UTF8;

            var type = obj.GetType();
            if (!type.IsPublic) throw new XException("类型{0}不是public，不能进行Xml序列化！", type.FullName);

            var serial = new XmlSerializer(type);
            var setting = new XmlWriterSettings();
            if (encoding == Encoding.UTF8)
                setting.Encoding = new UTF8Encoding(false);
            else
                setting.Encoding = encoding;
            setting.Indent = true;
            // 去掉开头 <?xml version="1.0" encoding="utf-8"?>
            setting.OmitXmlDeclaration = includeDeclaration;
            using (var writer = XmlWriter.Create(stream, setting))
            {
                if (ns == null)
                    serial.Serialize(writer, obj);
                else
                {
                    var xsns = new XmlSerializerNamespaces();
                    xsns.Add(prefix, ns);
                    serial.Serialize(writer, obj, xsns);
                }
            }
        }

        /// <summary>序列化为Xml文件</summary>
        /// <param name="obj">要序列化为Xml的对象</param>
        /// <param name="file">目标Xml文件</param>
        /// <param name="encoding">编码</param>
        /// <param name="prefix">前缀</param>
        /// <param name="ns">命名空间，设为0长度字符串可去掉默认命名空间xmlns:xsd和xmlns:xsi</param>
        /// <param name="includeDeclaration">是否包含Xml声明 &lt;?xml version="1.0" encoding="utf-8"?&gt;</param>
        /// <returns>Xml字符串</returns>
        public static void ToXmlFile(this Object obj, String file, Encoding encoding = null, String prefix = null, String ns = null, Boolean includeDeclaration = false)
        {
            if (File.Exists(file)) File.Delete(file);
            var dir = Path.GetDirectoryName(file);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using (var stream = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write))
            {
                obj.ToXml(stream, encoding, prefix, ns, includeDeclaration);
            }
        }

        /// <summary>字符串转为Xml实体对象</summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="xml">Xml字符串</param>
        /// <returns>Xml实体对象</returns>
        public static TEntity ToXmlEntity<TEntity>(this String xml) where TEntity : class
        {
            if (xml.IsNullOrWhiteSpace()) throw new ArgumentNullException("xml");

            var type = typeof(TEntity);
            if (!type.IsPublic) throw new XException("类型{0}不是public，不能进行Xml序列化！", type.FullName);

            var serial = new XmlSerializer(type);
            using (var reader = new StringReader(xml))
            {
                return serial.Deserialize(reader) as TEntity;
            }
        }

        /// <summary>数据流转为Xml实体对象</summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="stream">数据流</param>
        /// <param name="encoding">编码</param>
        /// <returns>Xml实体对象</returns>
        public static TEntity ToXmlEntity<TEntity>(this Stream stream, Encoding encoding = null) where TEntity : class
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (encoding == null) encoding = Encoding.UTF8;

            var type = typeof(TEntity);
            if (!type.IsPublic) throw new XException("类型{0}不是public，不能进行Xml序列化！", type.FullName);

            var serial = new XmlSerializer(type);
            using (var reader = new StreamReader(stream, encoding))
            {
                return serial.Deserialize(reader) as TEntity;
            }
        }

        /// <summary>Xml文件转为Xml实体对象</summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="file">Xml文件</param>
        /// <param name="encoding">编码</param>
        /// <returns>Xml实体对象</returns>
        public static TEntity ToXmlFileEntity<TEntity>(this String file, Encoding encoding = null) where TEntity : class
        {
            if (file.IsNullOrWhiteSpace()) throw new ArgumentNullException("file");
            if (!File.Exists(file)) return null;

            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                return stream.ToXmlEntity<TEntity>(encoding);
            }
        }
    }
}