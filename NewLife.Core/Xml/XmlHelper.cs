using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using NewLife.Reflection;

namespace NewLife.Xml
{
    /// <summary>Xml辅助类</summary>
    public static class XmlHelper
    {
        #region 实体转Xml
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
        /// <param name="attachCommit">是否附加注释，附加成员的Description和DisplayName注释</param>
        /// <returns>Xml字符串</returns>
        public static String ToXml(this Object obj, Encoding encoding = null, String prefix = null, String ns = null, Boolean includeDeclaration = false, Boolean attachCommit = false)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (encoding == null) encoding = Encoding.UTF8;
            // 删除字节序
            encoding = encoding.TrimPreamble();

            using (var stream = new MemoryStream())
            {
                ToXml(obj, stream, encoding, prefix, ns, includeDeclaration, attachCommit);
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
        /// <param name="attachCommit">是否附加注释，附加成员的Description和DisplayName注释</param>
        /// <returns>Xml字符串</returns>
        public static void ToXml(this Object obj, Stream stream, Encoding encoding = null, String prefix = null, String ns = null, Boolean includeDeclaration = false, Boolean attachCommit = false)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (encoding == null) encoding = Encoding.UTF8;
            // 删除字节序
            encoding = encoding.TrimPreamble();

            var type = obj.GetType();
            if (!type.IsPublic) throw new XException("类型{0}不是public，不能进行Xml序列化！", type.FullName);

            var serial = new XmlSerializer(type);
            var setting = new XmlWriterSettings();
            //setting.Encoding = encoding.TrimPreamble();
            setting.Encoding = encoding;
            setting.Indent = true;
            // 去掉开头 <?xml version="1.0" encoding="utf-8"?>
            setting.OmitXmlDeclaration = !includeDeclaration;

            var p = stream.Position;
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
            if (attachCommit)
            {
                if (stream is FileStream) stream.SetLength(stream.Position);
                stream.Position = p;
                var doc = new XmlDocument();
                doc.Load(stream);
                doc.DocumentElement.AttachCommit(type);

                stream.Position = p;
                //doc.Save(stream);
                using (var writer = XmlWriter.Create(stream, setting))
                {
                    doc.Save(writer);
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
        /// <param name="attachCommit">是否附加注释，附加成员的Description和DisplayName注释</param>
        /// <returns>Xml字符串</returns>
        public static void ToXmlFile(this Object obj, String file, Encoding encoding = null, String prefix = null, String ns = null, Boolean includeDeclaration = false, Boolean attachCommit = true)
        {
            if (File.Exists(file)) File.Delete(file);
            //var dir = Path.GetDirectoryName(file);
            //if (!String.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            file.EnsureDirectory(true);

            // 如果是字符串字典，直接写入文件，其它设置无效
            if (obj is IDictionary<String, String>)
            {
                var xml = (obj as IDictionary<String, String>).ToXml(prefix);
                File.WriteAllText(file, xml, encoding ?? Encoding.UTF8);
                return;
            }

            using (var stream = new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                obj.ToXml(stream, encoding, prefix, ns, includeDeclaration, attachCommit);
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
        #endregion

        #region Xml类型转换
        /// <summary>删除字节序，硬编码支持utf-8、utf-32、Unicode三种</summary>
        /// <param name="encoding">原始编码</param>
        /// <returns>删除字节序后的编码</returns>
        internal static Encoding TrimPreamble(this Encoding encoding)
        {
            if (encoding == null) return encoding;

            var bts = encoding.GetPreamble();
            if (bts == null || bts.Length < 1) return encoding;

            if (encoding is UTF8Encoding) return _utf8Encoding ?? (_utf8Encoding = new UTF8Encoding(false));
            if (encoding is UTF32Encoding) return _utf32Encoding ?? (_utf32Encoding = new UTF32Encoding(false, false));
            if (encoding is UnicodeEncoding) return _unicodeEncoding ?? (_unicodeEncoding = new UnicodeEncoding(false, false));

            return encoding;
        }
        private static Encoding _utf8Encoding;
        private static Encoding _utf32Encoding;
        private static Encoding _unicodeEncoding;

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

        #region Xml注释
        /// <summary>是否拥有注释</summary>
        private static Dictionary<Type, Boolean> typeHasCommit = new Dictionary<Type, Boolean>();

        /// <summary>附加注释</summary>
        /// <param name="node"></param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public static XmlNode AttachCommit(this XmlNode node, Type type)
        {
            if (node == null || type == null) return node;
            if (node.ChildNodes == null || node.ChildNodes.Count < 1) return node;

            // 如果没有注释
            var rs = false;
            if (typeHasCommit.TryGetValue(type, out rs) && !rs) return node;

            rs = node.AttachCommitInternal(type);
            if (!typeHasCommit.ContainsKey(type))
            {
                lock (typeHasCommit)
                {
                    if (!typeHasCommit.ContainsKey(type))
                    {
                        typeHasCommit.Add(type, rs);
                    }
                }
            }

            return node;
        }

        static Boolean AttachCommitInternal(this XmlNode node, Type type)
        {
            if (node.ChildNodes == null || node.ChildNodes.Count < 1) return false;

            var rs = false;

            // 当前节点加注释
            if (!node.PreviousSibling.IsComment())
            {
                if (SetComment(node, type)) rs = true;
            }

            #region 特殊处理数组和列表
            Type elmType = null;
            if (type.HasElementType)
                elmType = type.GetElementType();
            else if (typeof(IEnumerable).IsAssignableFrom(type) && type.IsGenericType && type.GetGenericArguments().Length == 1)
                elmType = type.GetGenericArguments()[0];

            if (elmType != null && elmType.Name.EqualIgnoreCase(node.ChildNodes[0].Name))
            {
                for (int i = 0; i < node.ChildNodes.Count; i++)
                {
                    rs |= node.ChildNodes[i].AttachCommitInternal(elmType);
                }
                return rs;
            }
            #endregion

            for (int i = 0; i < node.ChildNodes.Count; i++)
            {
                var curNode = node.ChildNodes[i];

                // 如果当前是注释，跳过两个，下一个也不处理了
                if (curNode.IsComment()) { i++; continue; }

                // 找到对应的属性
                var name = curNode.Name;
                var pi = type.GetPropertyEx(name);

                // 如果前一个是注释，跳过
                if (i <= 0 || !node.ChildNodes[i - 1].IsComment())
                {
                    if (pi != null && SetComment(curNode, pi)) { rs = true; i++; }
                }

                // 递归。因为必须依赖于Xml树，所以不用担心死循环
                if (pi != null && Type.GetTypeCode(pi.PropertyType) == TypeCode.Object) rs |= curNode.AttachCommitInternal(pi.PropertyType);
            }

            return rs;
        }

        private static Boolean SetComment(this XmlNode node, MemberInfo member)
        {
            if (node.IsComment() || node.PreviousSibling.IsComment()) return false;

            #region 从特性中获取注释
            var commit = String.Empty;
            var des = member.GetCustomAttribute<DescriptionAttribute>(true);
            var dis = member.GetCustomAttribute<DisplayNameAttribute>(true);
            if (des != null && dis == null)
                commit = des.Description;
            else if (des == null && dis != null)
                commit = dis.DisplayName;
            else if (des != null && dis != null)
            {
                // DisplayName。Description
                if (des.Description == null && !dis.DisplayName.IsNullOrWhiteSpace() || !des.Description.Contains(dis.DisplayName))
                {
                    commit = dis.DisplayName;
                    if (!commit.EndsWith(".") || commit.EndsWith("。")) commit += "。";
                }
                if (!des.Description.IsNullOrWhiteSpace()) commit += des.Description;
            }
            #endregion

            if (commit.IsNullOrWhiteSpace()) return false;

            var cm = node.OwnerDocument.CreateComment(commit);
            node.ParentNode.InsertBefore(cm, node);

            return true;
        }

        private static Boolean IsComment(this XmlNode node) { return node != null && node.NodeType == XmlNodeType.Comment; }
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