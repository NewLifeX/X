using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Xml
{
    /// <summary>Xml写入器</summary>
    public class XmlWriterX : TextWriterBase<XmlReaderWriterSettings>
    {
        #region 属性
        private XmlWriter _Writer;
        /// <summary>写入器</summary>
        public XmlWriter Writer
        {
            get
            {
                if (_Writer == null)
                {
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Encoding = Settings.Encoding;
                    settings.Indent = true;
                    _Writer = XmlWriter.Create(Stream, settings);
                }
                return _Writer;
            }
            set
            {
                _Writer = value;
                if (Settings.Encoding != _Writer.Settings.Encoding) Settings.Encoding = _Writer.Settings.Encoding;

                XmlTextWriter xw = _Writer as XmlTextWriter;
                if (xw != null && Stream != xw.BaseStream) Stream = xw.BaseStream;
            }
        }

        /// <summary>数据流。更改数据流后，重置Writer为空，以使用新的数据流</summary>
        public override Stream Stream
        {
            get { return base.Stream; }
            set
            {
                if (base.Stream != value) _Writer = null;
                base.Stream = value;
            }
        }

        private String _RootName;
        /// <summary>根元素名</summary>
        public String RootName { get { return _RootName; } set { _RootName = value; } }
        #endregion

        #region 基础元数据
        #region 字符串
        /// <summary>
        /// 写入字符串
        /// </summary>
        /// <param name="value">要写入的值。</param>
        public override void Write(string value)
        {
            Writer.WriteString(value);

            AutoFlush();
        }
        #endregion
        #endregion

        #region 扩展类型
        /// <summary>写对象类型</summary>
        /// <param name="type"></param>
        protected override void WriteObjectType(Type type) { if (Settings.WriteType) Writer.WriteAttributeString("Type", type.FullName); }
        #endregion

        #region 字典
        /// <summary>写入字典项 </summary>
        /// <param name="value">对象</param>
        /// <param name="keyType">键类型</param>
        /// <param name="valueType">值类型</param>
        /// <param name="index">成员索引</param>
        /// <param name="callback">使用指定委托方法处理复杂数据</param>
        /// <returns>是否写入成功</returns>
        protected override bool OnWriteKeyValue(DictionaryEntry value, Type keyType, Type valueType, int index, WriteObjectCallback callback)
        {
            // 如果无法取得字典项类型，则每个键值都单独写入类型
            Writer.WriteStartElement("Item");

            {
                Writer.WriteStartElement("Key");
                if (!WriteObject(value.Key, keyType, callback)) return false;
                Writer.WriteEndElement();
            }

            {
                Writer.WriteStartElement("Value");
                if (!WriteObject(value.Value, valueType, callback)) return false;
                Writer.WriteEndElement();
            }

            Writer.WriteEndElement();

            return true;
        }
        #endregion

        #region 枚举
        /// <summary>写入枚举项</summary>
        /// <param name="value">对象</param>
        /// <param name="type">类型</param>
        /// <param name="index">成员索引</param>
        /// <param name="callback">使用指定委托方法处理复杂数据</param>
        /// <returns>是否写入成功</returns>
        protected override bool OnWriteItem(Object value, Type type, Int32 index, WriteObjectCallback callback)
        {
            Type t = type;
            if (value != null) t = value.GetType();
            String name = AttributeX.GetCustomAttributeValue<XmlRootAttribute, String>(t, true);
            if (String.IsNullOrEmpty(name) && t != null) name = t.Name;

            Writer.WriteStartElement(name);

            AutoFlush();

            Boolean rs = base.OnWriteItem(value, type, index, callback);

            AutoFlush();

            Writer.WriteEndElement();

            AutoFlush();

            return rs;
        }

        /// <summary>写入枚举数据，复杂类型使用委托方法进行处理</summary>
        /// <param name="value">对象</param>
        /// <param name="type">类型</param>
        /// <param name="callback">使用指定委托方法处理复杂数据</param>
        /// <returns>是否写入成功</returns>
        public override bool WriteEnumerable(IEnumerable value, Type type, WriteObjectCallback callback)
        {
            if (value == null) return true;

            Type t = value.GetType();
            Type elementType = null;
            if (t.HasElementType) elementType = t.GetElementType();
            Boolean result = false;
            if (typeof(IEnumerable).IsAssignableFrom(elementType))
            {
                if (typeof(IEnumerable).IsAssignableFrom(elementType.GetElementType()))
                {
                    elementType = elementType.GetElementType();
                    WriteEnumerable(value as IEnumerable, elementType, callback);
                }
                foreach (Object item in value)
                {
                    WriteLog("WriteEnumerable", elementType.Name);
                    Writer.WriteStartElement("Item");
                    result = base.WriteEnumerable(item as IEnumerable, elementType, callback);
                    Writer.WriteEndElement();
                }
                return result;
            }

            if (value.GetType().IsArray && value.GetType().GetArrayRank() > 1)
            {
                Array arr = value as Array;
                List<String> lengths = new List<String>();
                for (int j = 0; j < value.GetType().GetArrayRank(); j++)
                {
                    lengths.Add(arr.GetLength(j).ToString());
                }
                WriteLengths(String.Join(",", lengths.ToArray()));
            }

            return base.WriteEnumerable(value, type, callback);
        }
        #endregion

        #region 写入对象
        /// <summary>已重载。写入文档的开头和结尾</summary>
        /// <param name="value">要写入的对象</param>
        /// <param name="type">要写入的对象类型</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        protected override bool OnWriteObject(object value, Type type, WriteObjectCallback callback)
        {
            if (Depth > 1) return base.OnWriteObject(value, type, callback);

            Type t = type;
            if (t == null && value != null) t = value.GetType();
            String name = RootName;

            if (String.IsNullOrEmpty(name))
            {
                // 优先采用类型上的XmlRoot特性
                if (type != null) name = AttributeX.GetCustomAttributeValue<XmlRootAttribute, String>(type, true);
                if (String.IsNullOrEmpty(name) && t != null) name = AttributeX.GetCustomAttributeValue<XmlRootAttribute, String>(t, true);
                if (String.IsNullOrEmpty(name))
                {
                    if (t != null) name = GetName(t);

                    if (String.IsNullOrEmpty(RootName)) RootName = name;
                }
            }

            if (Depth == 1) Writer.WriteStartDocument();
            Writer.WriteStartElement(name);

            AutoFlush();

            Boolean rs = base.OnWriteObject(value, type, callback);

            AutoFlush();

            if (Writer.WriteState != WriteState.Start)
            {
                Writer.WriteEndElement();
                if (Depth == 1) Writer.WriteEndDocument();
            }
            AutoFlush();

            return rs;
        }

        /// <summary>写入对象成员</summary>
        /// <param name="value">要写入的对象</param>
        /// <param name="type">要写入的对象类型</param>
        /// <param name="member">成员</param>
        /// <param name="index">成员索引</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        protected override bool OnWriteMember(object value, Type type, IObjectMemberInfo member, Int32 index, WriteObjectCallback callback)
        {
            // 检查成员的值，如果是默认值，则不输出
            if (value != null && Settings.IgnoreDefault && IsDefault(value, member)) return true;

            // 特殊处理特性，只有普通值类型才能输出为特性
            Boolean isAtt = Settings.MemberAsAttribute && IsAttributeType(member.Type);
            if (isAtt)
                Writer.WriteStartAttribute(member.Name);
            else
                Writer.WriteStartElement(member.Name);

            AutoFlush();

            Boolean rs = base.OnWriteMember(value, type, member, index, callback);

            if (!isAtt) Writer.WriteEndElement();

            AutoFlush();

            return rs;
        }

        /// <summary>写对象引用计数</summary>
        /// <param name="index"></param>
        protected override void OnWriteObjRefIndex(int index) { if (index > 0) Writer.WriteAttributeString("ObjRef", index.ToString()); }
        #endregion

        #region 未知对象
        /// <summary>
        /// 写入未知对象（其它所有方法都无法识别的对象），采用BinaryFormatter或者XmlSerialization
        /// </summary>
        /// <param name="value">要写入的对象</param>
        /// <param name="type">要写入的对象类型</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        public override bool WriteUnKnown(object value, Type type, WriteObjectCallback callback)
        {
            try
            {
                WriteLog("WriteUnKnown", type.Name);
                XmlSerializer serial = new XmlSerializer(type);
                MemoryStream ms = new MemoryStream();
                serial.Serialize(ms, value);

                String xml = Encoding.UTF8.GetString(ms.ToArray());
                Write(xml);

                return true;
            }
            catch
            {
                //只能处理公共类型,Type因其保护级别而不可访问。
            }
            return base.WriteUnKnown(value, type, callback);
        }
        #endregion

        #region 方法
        /// <summary>刷新缓存中的数据</summary>
        public override void Flush()
        {
            Writer.Flush();

            base.Flush();
        }

        /// <summary>写入长度。多维数组用</summary>
        /// <param name="lengths"></param>
        protected override void WriteLengths(string lengths) { Writer.WriteAttributeString("Lengths", lengths); }
        #endregion

        #region 序列化接口
        /// <summary>写入实现了可序列化接口的对象</summary>
        /// <param name="value">要写入的对象</param>
        /// <param name="type">要写入的对象类型，如果type等于DataTable，需设置DataTable的名称</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        public override bool WriteSerializable(object value, Type type, WriteObjectCallback callback)
        {
            if (!typeof(IXmlSerializable).IsAssignableFrom(type)) return base.WriteSerializable(value, type, callback);

            try
            {
                IXmlSerializable xml = value as IXmlSerializable;
                // 这里必须额外写一对标记，否则读取的时候只能读取得到模式而得不到数据
                Boolean b = xml.GetSchema() != null;
                if (b) Writer.WriteStartElement("Data");
                xml.WriteXml(Writer);
                if (b) Writer.WriteEndElement();

                return true;
            }
            catch
            {
                return base.WriteSerializable(value, type, callback);
            }
        }
        #endregion

        #region 辅助方法
        static String GetName(Type type)
        {
            if (type.HasElementType) return "ArrayOf" + GetName(type.GetElementType());

            String name = TypeX.Create(type).Name;
            name = name.Replace("<", "_");
            //name = name.Replace(">", "_");
            name = name.Replace(",", "_");
            name = name.Replace(">", "");
            return name;
        }

        /// <summary>是否可以作为属性写入Xml的类型</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static Boolean IsAttributeType(Type type)
        {
            if (typeof(Type).IsAssignableFrom(type)) return true;

            TypeCode code = Type.GetTypeCode(type);
            return code != TypeCode.Object;
        }
        #endregion
    }
}