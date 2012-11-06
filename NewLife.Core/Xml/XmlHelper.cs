using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using NewLife.Exceptions;
using NewLife.Reflection;

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
            return obj.ToXml(null, "", "");
        }

        /// <summary>序列化为Xml字符串</summary>
        /// <param name="obj">要序列化为Xml的对象</param>
        /// <param name="encoding">编码</param>
        /// <param name="prefix">前缀</param>
        /// <param name="ns">命名空间，设为0长度字符串可去掉默认命名空间xmlns:xsd和xmlns:xsi</param>
        /// <param name="includeDeclaration">是否包含Xml声明</param>
        /// <returns>Xml字符串</returns>
        public static String ToXml(this Object obj, Encoding encoding = null, String prefix = null, String ns = null, Boolean includeDeclaration = false)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (encoding == null) encoding = Encoding.UTF8;

            using (var stream = new MemoryStream())
            {
                ToXml(obj, stream, encoding, prefix, ns, includeDeclaration);
                return encoding.GetString(stream.ToArray());
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
            setting.Encoding = encoding.TrimPreamble();
            setting.Indent = true;
            // 去掉开头 <?xml version="1.0" encoding="utf-8"?>
            setting.OmitXmlDeclaration = !includeDeclaration;
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
            //if (File.Exists(file)) File.Delete(file);
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
            return xml.ToXmlEntity(typeof(TEntity)) as TEntity;
        }

        /// <summary>字符串转为Xml实体对象</summary>
        /// <param name="xml">Xml字符串</param>
        /// <param name="type">实体类型</param>
        /// <returns>Xml实体对象</returns>
        public static Object ToXmlEntity(this String xml, Type type)
        {
            if (xml.IsNullOrWhiteSpace()) throw new ArgumentNullException("xml");
            if (type == null) throw new ArgumentNullException("type");

            if (!type.IsPublic) throw new XException("类型{0}不是public，不能进行Xml序列化！", type.FullName);

            var serial = new XmlSerializer(type);
            using (var reader = new StringReader(xml))
            using (var xr = new XmlTextReader(reader))
            {
                // 必须关闭Normalization，否则字符串的\r\n会变为\n
                //xr.Normalization = true;
                return serial.Deserialize(xr);
            }
        }

        /// <summary>数据流转为Xml实体对象</summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="stream">数据流</param>
        /// <param name="encoding">编码</param>
        /// <returns>Xml实体对象</returns>
        public static TEntity ToXmlEntity<TEntity>(this Stream stream, Encoding encoding = null) where TEntity : class
        {
            return stream.ToXmlEntity(typeof(TEntity), encoding) as TEntity;
        }

        /// <summary>数据流转为Xml实体对象</summary>
        /// <param name="stream">数据流</param>
        /// <param name="type">实体类型</param>
        /// <param name="encoding">编码</param>
        /// <returns>Xml实体对象</returns>
        public static Object ToXmlEntity(this Stream stream, Type type, Encoding encoding = null)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (type == null) throw new ArgumentNullException("type");
            if (encoding == null) encoding = Encoding.UTF8;

            if (!type.IsPublic) throw new XException("类型{0}不是public，不能进行Xml序列化！", type.FullName);

            var serial = new XmlSerializer(type);
            using (var reader = new StreamReader(stream, encoding))
            using (var xr = new XmlTextReader(reader))
            {
                // 必须关闭Normalization，否则字符串的\r\n会变为\n
                //xr.Normalization = true;
                return serial.Deserialize(xr);
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

        /// <summary>删除字节序，硬编码支持utf-8、utf-32、Unicode三种</summary>
        /// <param name="encoding">原始编码</param>
        /// <returns>删除字节序后的编码</returns>
        internal static Encoding TrimPreamble(this Encoding encoding)
        {
            if (encoding == null) return encoding;

            var bts = encoding.GetPreamble();
            if (bts == null || bts.Length < 1) return encoding;

            if (encoding is UTF8Encoding) return utf8Encoding ?? (utf8Encoding = new UTF8Encoding(false));
            if (encoding is UTF32Encoding) return utf32Encoding ?? (utf32Encoding = new UTF32Encoding(false, false));
            if (encoding is UnicodeEncoding) return unicodeEncoding ?? (unicodeEncoding = new UnicodeEncoding(false, false));

            return encoding;
        }
        private static Encoding utf8Encoding;
        private static Encoding utf32Encoding;
        private static Encoding unicodeEncoding;

        internal static Boolean CanXmlConvert(this Type type)
        {
            var code = Type.GetTypeCode(type);
            if (code != TypeCode.Object) return true;

            if (!type.IsValueType) return false;

            if (type == typeof(Guid) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan)) return true;

            return false;
        }

        internal static String XmlConvertToString(Object value)
        {
            if (value == null) return null;

            var type = value.GetType();
            if (Type.GetTypeCode(type) == TypeCode.String) return value.ToString();

            var mix = MethodInfoX.Create(typeof(XmlConvert), "ToString", new Type[] { type });
            if (mix == null) throw new XException("类型{0}不支持转为Xml字符串，请先用CanXmlConvert方法判断！", type);

            return (String)mix.Invoke(null, value);
        }

        internal static T XmlConvertFromString<T>(String xml)
        {
            if (xml == null) return default(T);

            var type = typeof(T);
            if (xml == String.Empty && type == typeof(String)) return (T)(Object)xml;

            var mix = MethodInfoX.Create(typeof(XmlConvert), "To" + type.Name, new Type[] { typeof(String) });
            if (mix == null) throw new XException("类型{0}不支持从Xml字符串转换，请先用CanXmlConvert方法判断！", type);

            return (T)mix.Invoke(null, xml);
        }
    }
}