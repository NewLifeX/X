using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using NewLife.Reflection;

namespace NewLife.Xml
{
    /// <summary>Xml辅助类</summary>
    public static class XmlHelper
    {
        #region 实体转Xml
        /// <summary>序列化为Xml字符串</summary>
        /// <param name="obj">要序列化为Xml的对象</param>
        /// <param name="encoding">编码</param>
        /// <param name="attachComment">是否附加注释，附加成员的Description和DisplayName注释</param>
        /// <returns>Xml字符串</returns>
        public static String ToXml(this Object obj, Encoding encoding = null, Boolean attachComment = false)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (encoding == null) encoding = Encoding.UTF8;
            // 删除字节序
            //encoding = encoding.TrimPreamble();

            using (var stream = new MemoryStream())
            {
                ToXml(obj, stream, encoding, attachComment);
                return encoding.GetString(stream.ToArray());
            }
        }

        /// <summary>序列化为Xml数据流</summary>
        /// <param name="obj">要序列化为Xml的对象</param>
        /// <param name="stream">目标数据流</param>
        /// <param name="encoding">编码</param>
        /// <param name="attachComment">是否附加注释，附加成员的Description和DisplayName注释</param>
        /// <returns>Xml字符串</returns>
        public static void ToXml(this Object obj, Stream stream, Encoding encoding = null, Boolean attachComment = false)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (encoding == null) encoding = Encoding.UTF8;
            // 删除字节序
            //encoding = encoding.TrimPreamble();

            var xml = new NewLife.Serialization.Xml
            {
                Stream = stream,
                Encoding = encoding,
                UseAttribute = false,
                UseComment = attachComment
            };
            xml.Write(obj);
        }

        /// <summary>序列化为Xml文件</summary>
        /// <param name="obj">要序列化为Xml的对象</param>
        /// <param name="file">目标Xml文件</param>
        /// <param name="encoding">编码</param>
        /// <param name="attachComment">是否附加注释，附加成员的Description和DisplayName注释</param>
        /// <returns>Xml字符串</returns>
        public static void ToXmlFile(this Object obj, String file, Encoding encoding = null, Boolean attachComment = true)
        {
            if (File.Exists(file)) File.Delete(file);
            file.EnsureDirectory(true);

            // 如果是字符串字典，直接写入文件，其它设置无效
            if (obj is IDictionary<String, String>)
            {
                var xml = (obj as IDictionary<String, String>).ToXml();
                File.WriteAllText(file, xml, encoding ?? Encoding.UTF8);
                return;
            }

            using (var stream = new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                obj.ToXml(stream, encoding, attachComment);
                // 必须通过设置文件流长度来实现截断，否则后面可能会多一截旧数据
                stream.SetLength(stream.Position);
            }
        }
        #endregion

        #region Xml转实体
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

            var x = new NewLife.Serialization.Xml
            {
                Stream = new MemoryStream(xml.GetBytes())
            };

            return x.Read(type);

            //if (!type.IsPublic) throw new XException("类型{0}不是public，不能进行Xml序列化！", type.FullName);

            //var serial = new XmlSerializer(type);
            //using (var reader = new StringReader(xml))
            //using (var xr = new XmlTextReader(reader))
            //{
            //    // 必须关闭Normalization，否则字符串的\r\n会变为\n
            //    //xr.Normalization = true;
            //    return serial.Deserialize(xr);
            //}
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

            var x = new NewLife.Serialization.Xml
            {
                Stream = stream,
                Encoding = encoding
            };

            return x.Read(type);

            //if (!type.IsPublic) throw new XException("类型{0}不是public，不能进行Xml序列化！", type.FullName);

            //var serial = new XmlSerializer(type);
            //using (var reader = new StreamReader(stream, encoding))
            //using (var xr = new XmlTextReader(reader))
            //{
            //    // 必须关闭Normalization，否则字符串的\r\n会变为\n
            //    //xr.Normalization = true;
            //    return serial.Deserialize(xr);
            //}
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

            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return stream.ToXmlEntity<TEntity>(encoding);
            }
        }
        #endregion

        #region Xml类型转换
        ///// <summary>删除字节序，硬编码支持utf-8、utf-32、Unicode三种</summary>
        ///// <param name="encoding">原始编码</param>
        ///// <returns>删除字节序后的编码</returns>
        //internal static Encoding TrimPreamble(this Encoding encoding)
        //{
        //    if (encoding == null) return encoding;

        //    var bts = encoding.GetPreamble();
        //    if (bts == null || bts.Length < 1) return encoding;

        //    if (encoding is UTF8Encoding) return _utf8Encoding ?? (_utf8Encoding = new UTF8Encoding(false));
        //    if (encoding is UTF32Encoding) return _utf32Encoding ?? (_utf32Encoding = new UTF32Encoding(false, false));
        //    if (encoding is UnicodeEncoding) return _unicodeEncoding ?? (_unicodeEncoding = new UnicodeEncoding(false, false));

        //    return encoding;
        //}
        //private static Encoding _utf8Encoding;
        //private static Encoding _utf32Encoding;
        //private static Encoding _unicodeEncoding;

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
            var code = Type.GetTypeCode(type);
            if (code == TypeCode.String) return value.ToString();
            if (code == TypeCode.DateTime) return XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.RoundtripKind);

            //var method = Reflect.GetMethodEx(typeof(XmlConvert), "ToString", type);
            var method = typeof(XmlConvert).GetMethodEx("ToString", type);
            if (method == null) throw new XException("类型{0}不支持转为Xml字符串，请先用CanXmlConvert方法判断！", type);

            return (String)"".Invoke(method, value);
        }

        internal static T XmlConvertFromString<T>(String xml) { return (T)XmlConvertFromString(typeof(T), xml); }

        internal static Object XmlConvertFromString(Type type, String xml)
        {
            if (xml == null) return null;

            var code = Type.GetTypeCode(type);
            if (code == TypeCode.String) return xml;
            if (code == TypeCode.DateTime) return XmlConvert.ToDateTime(xml, XmlDateTimeSerializationMode.RoundtripKind);

            //var method = Reflect.GetMethodEx(typeof(XmlConvert), "To" + type.Name, typeof(String));
            var method = typeof(XmlConvert).GetMethodEx("To" + type.Name, typeof(String));
            if (method == null) throw new XException("类型{0}不支持从Xml字符串转换，请先用CanXmlConvert方法判断！", type);

            return "".Invoke(method, xml);
        }
        #endregion

        #region Xml转字典
        /// <summary>简单Xml转为字符串字典</summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static Dictionary<String, String> ToXmlDictionary(this String xml)
        {
            if (String.IsNullOrEmpty(xml)) return null;

            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var root = doc.DocumentElement;

            var dic = new Dictionary<String, String>();

            if (root.ChildNodes != null && root.ChildNodes.Count > 0)
            {
                foreach (XmlNode item in root.ChildNodes)
                {
                    if (item.ChildNodes != null && (item.ChildNodes.Count > 1 ||
                        item.ChildNodes.Count == 1 && !(item.FirstChild is XmlText) && !(item.FirstChild is XmlCDataSection)))
                    {
                        dic[item.Name] = item.InnerXml;
                    }
                    else
                    {
                        dic[item.Name] = item.InnerText;
                    }
                }
            }

            return dic;
        }

        /// <summary>字符串字典转为Xml</summary>
        /// <param name="dic"></param>
        /// <param name="rootName"></param>
        /// <returns></returns>
        public static String ToXml(this IDictionary<String, String> dic, String rootName = null)
        {
            if (String.IsNullOrEmpty(rootName)) rootName = "xml";

            var doc = new XmlDocument();
            var root = doc.CreateElement(rootName);
            doc.AppendChild(root);

            if (dic != null && dic.Count > 0)
            {
                foreach (var item in dic)
                {
                    var elm = doc.CreateElement(item.Key);
                    elm.InnerText = item.Value;
                    root.AppendChild(elm);
                }
            }

            return doc.OuterXml;
        }
        #endregion
    }
}