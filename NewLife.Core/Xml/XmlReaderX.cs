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
    /// <summary>Xml读取器</summary>
    public class XmlReaderX : TextReaderBase<XmlReaderWriterSettings>
    {
        #region 属性
        private XmlReader _Reader;
        /// <summary>读取器</summary>
        public XmlReader Reader
        {
            get
            {
                if (_Reader == null)
                {
                    var settings = new XmlReaderSettings();
                    settings.IgnoreWhitespace = true;
                    settings.IgnoreComments = true;
                    _Reader = XmlReader.Create(Stream, settings);
                }
                return _Reader;
            }
            set
            {
                _Reader = value;

                var xr = _Reader as XmlTextReader;
                if (xr != null && Settings.Encoding != xr.Encoding) Settings.Encoding = xr.Encoding;
            }
        }

        /// <summary>数据流。更改数据流后，重置Reader为空，以使用新的数据流</summary>
        public override Stream Stream
        {
            get { return base.Stream; }
            set
            {
                if (base.Stream != value) _Reader = null;
                base.Stream = value;
            }
        }

        /// <summary>获取一个值，该值表示当前的流位置是否在流的末尾。</summary>
        /// <returns>如果当前的流位置在流的末尾，则为 true；否则为 false。</returns>
        public override bool EndOfStream
        {
            get
            {
                var r = Reader;
                if (r != null) return r.EOF;

                var s = Stream;
                if (s != null) return s.Position == s.Length;

                return false;
            }
        }

        /// <summary>初始化</summary>
        public XmlReaderX() { }

        /// <summary>使用xml字符串初始化</summary>
        /// <param name="xml"></param>
        public XmlReaderX(String xml)
        {
            Stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        }

        private String _RootName;
        /// <summary>根元素名</summary>
        public String RootName { get { return _RootName; } set { _RootName = value; } }

        private String _Lengths;
        /// <summary>多维数组长度</summary>
        public String Lengths { get { return _Lengths; } set { _Lengths = value; } }
        #endregion

        #region 基础元数据
        /// <summary>从当前流中读取一个字符串。字符串有长度前缀，一次 7 位地被编码为整数。</summary>
        /// <returns></returns>
        public override string ReadString()
        {
            Boolean isElement = Reader.NodeType == XmlNodeType.Element;
            if (isElement)
            {
                if (SkipEmpty()) return null;
                Reader.ReadStartElement();
            }

            String str = Reader.ReadContentAsString();
            if (isElement && Reader.NodeType == XmlNodeType.EndElement) Reader.ReadEndElement();

            WriteLog(1, "ReadString", str);
            return str;
        }
        #endregion

        #region 扩展类型
        /// <summary>读对象类型</summary>
        /// <returns></returns>
        protected override Type OnReadType()
        {
            if (Reader.MoveToAttribute("Type"))
            {
                var t = base.OnReadType();
                if (Reader.NodeType == XmlNodeType.Attribute) Reader.MoveToElement();
                return t;
            }

            return base.OnReadType();
        }
        #endregion

        #region 字典
        /// <summary>尝试读取字典类型对象</summary>
        /// <param name="type">类型</param>
        /// <param name="value">对象</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        public override bool ReadDictionary(Type type, ref object value, ReadObjectCallback callback)
        {
            if (SkipEmpty()) return true;

            return base.ReadDictionary(type, ref value, callback);
        }

        /// <summary>读取字典项</summary>
        /// <param name="keyType">键类型</param>
        /// <param name="valueType">值类型</param>
        /// <param name="value">字典项</param>
        /// <param name="index">元素序号</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        protected override bool ReadDictionaryEntry(Type keyType, Type valueType, ref DictionaryEntry value, Int32 index, ReadObjectCallback callback)
        {
            if (Reader.NodeType == XmlNodeType.EndElement) return false;

            Object key = null;
            Object val = null;

            // <Item>
            Reader.ReadStartElement();

            // <Key>
            Reader.ReadStartElement();
            if (!ReadObject(keyType, ref key)) return false;
            // </Key>
            if (Reader.NodeType == XmlNodeType.EndElement) Reader.ReadEndElement();

            // <Value>
            if (!ReadObject(valueType, ref val)) return false;
            // </Value>

            value.Key = key;
            value.Value = val;

            // </Item>
            if (Reader.NodeType == XmlNodeType.EndElement) Reader.ReadEndElement();

            return true;
        }
        #endregion

        #region 枚举
        /// <summary>尝试读取枚举</summary>
        /// <remarks>重点和难点在于如果得知枚举元素类型，这里假设所有元素类型一致，否则实在无法处理</remarks>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        public override bool ReadEnumerable(Type type, ref object value, ReadObjectCallback callback)
        {
            Type t = type.GetElementType();

            #region 锯齿二维数组处理
            Int32 length = 1;
            while (typeof(IEnumerable).IsAssignableFrom(t))
            {
                length++;
                t = t.GetElementType();
            }

            if (length > 1)
            {
                Array array = TypeX.CreateInstance(type, length) as Array;
                t = type.GetElementType();
                for (int j = 0; j < length - 1; j++)
                {
                    //开始循环之前已赋值，所以第一次循环时跳过
                    if (j > 0) t = t.GetElementType();

                    for (int i = 0; i < array.Length; i++)
                    {
                        if (value != null) value = null;
                        if (base.ReadEnumerable(t, ref value, callback) && value != null)
                        {
                            array.SetValue(value, i);
                        }
                    }
                }
                if (array != null && array.Length > 0) value = array;
                return true;
            }
            #endregion

            return base.ReadEnumerable(type, ref value, callback);
        }

        /// <summary>读取枚举项</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="index">元素序号</param>
        /// <param name="callback">处理元素的方法</param>
        /// <returns></returns>
        protected override bool ReadItem(Type type, ref object value, Int32 index, ReadObjectCallback callback)
        {
            String name = type.GetCustomAttributeValue<XmlRootAttribute, String>(true);
            if (String.IsNullOrEmpty(name) && type != null) name = type.Name;

            if (Reader.NodeType == XmlNodeType.EndElement || Reader.Name != name) return false;

            Boolean rs = base.ReadItem(type, ref value, index, callback);

            if (Reader.NodeType == XmlNodeType.EndElement) Reader.ReadEndElement();

            return rs;
        }
        #endregion

        #region 读取对象
        /// <summary>尝试读取目标对象指定成员的值，通过委托方法递归处理成员</summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        protected override bool OnReadObject(Type type, ref object value, ReadObjectCallback callback)
        {
            if (Depth > 1) return base.OnReadObject(type, ref value, callback);

            while (Reader.NodeType != XmlNodeType.Element) { if (!Reader.Read())return false; }
            if (String.IsNullOrEmpty(RootName)) RootName = Reader.Name;

            return base.OnReadObject(type, ref value, callback);
        }

        /// <summary>尝试读取引用对象</summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        protected override bool ReadRefObject(Type type, ref object value, ReadObjectCallback callback)
        {
            Boolean isElement = Reader.NodeType == XmlNodeType.Element;
            Boolean isAtt = Settings.MemberAsAttribute && XmlWriterX.IsAttributeType(type);
            if (isElement && !isAtt)
            {
                if (SkipEmpty()) return true;
                if (Reader.MoveToAttribute("Lengths"))
                {
                    WriteLog("ReadLengths");
                    Lengths = ReadString();
                    if (Reader.NodeType == XmlNodeType.Attribute) Reader.MoveToElement();
                }
                Reader.ReadStartElement();
            }

            Boolean b = base.ReadRefObject(type, ref value, callback);

            if (isElement && !isAtt && Reader.NodeType == XmlNodeType.EndElement) Reader.ReadEndElement();

            return b;
        }

        /// <summary>读取成员之前获取要读取的成员，默认是index处的成员，实现者可以重载，改变当前要读取的成员，如果当前成员不在数组里面，则实现者自己跳到下一个可读成员。</summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="members">可匹配成员数组</param>
        /// <param name="index">索引</param>
        /// <returns></returns>
        protected override IObjectMemberInfo GetMemberBeforeRead(Type type, object value, IObjectMemberInfo[] members, int index)
        {
            String name = String.Empty;

            while (Reader.NodeType != XmlNodeType.None && Reader.IsStartElement())
            {
                name = Reader.Name;

                IObjectMemberInfo member = GetMemberByName(members, name);
                if (member != null) return member;

                if (SkipEmpty()) continue;

                Reader.ReadStartElement();

                if (!SkipEmpty()) Reader.Read();

                if (Reader.NodeType == XmlNodeType.EndElement) Reader.ReadEndElement();
            }

            return null;
        }

        /// <summary>读取对象成员</summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="member">成员</param>
        /// <param name="index">成员索引</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        protected override bool OnReadMember(Type type, ref object value, IObjectMemberInfo member, Int32 index, ReadObjectCallback callback)
        {
            Boolean isAtt = Settings.MemberAsAttribute && XmlWriterX.IsAttributeType(member.Type);
            if (isAtt) Reader.MoveToAttribute(member.Name);

            return base.OnReadMember(type, ref value, member, index, callback);
        }

        /// <summary>读取对象引用计数</summary>
        /// <returns></returns>
        protected override int OnReadObjRefIndex()
        {
            if (Reader.MoveToAttribute("ObjRef"))
            {
                Int32 rs = ReadInt32();

                // 从特性移到元素，方便后续读取操作，如果后续还需要读取特性，则应该自己移到特性处
                if (Reader.NodeType == XmlNodeType.Attribute) Reader.MoveToElement();

                // 跳过空元素，可能该元素已经读取了对象引用
                SkipEmpty();

                return rs;
            }

            return -1;
        }
        #endregion

        #region 未知对象
        /// <summary>读取未知对象（其它所有方法都无法识别的对象），采用BinaryFormatter或者XmlSerialization</summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        public override bool ReadUnKnown(Type type, ref object value, ReadObjectCallback callback)
        {
            try
            {
                WriteLog("XmlSerializer", type.Name);
                XmlSerializer serializer = new XmlSerializer(type);
                String str = Reader.ReadString();
                if (!String.IsNullOrEmpty(str))
                {
                    MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(str));
                    value = serializer.Deserialize(ms);
                }
                return true;
            }
            catch
            {
                //只能处理公共类型,Type因其保护级别而不可访问。
            }
            return base.ReadUnKnown(type, ref value, callback);
        }
        #endregion

        #region 方法
        /// <summary>当前节点是否空。如果是空节点，则读一次，让指针移到下一个元素</summary>
        Boolean SkipEmpty()
        {
            // 空元素直接返回
            if (Reader.IsEmptyElement)
            {
                // 读一次，把指针移到下一个元素上
                Reader.Read();
                return true;
            }

            return false;
        }

        /// <summary>读取多维数组相关参数</summary>
        /// <returns></returns>
        protected override string ReadLengths() { return Lengths; }

        /// <summary>备份当前环境，用于临时切换数据流等</summary>
        /// <returns>本次备份项集合</returns>
        public override IDictionary<String, Object> Backup()
        {
            var dic = base.Backup();
            dic["Reader"] = Reader;

            return dic;
        }

        /// <summary>恢复最近一次备份</summary>
        /// <returns>本次还原项集合</returns>
        public override IDictionary<String, Object> Restore()
        {
            var dic = base.Restore();
            Reader = dic["Reader"] as XmlReader;

            return dic;
        }
        #endregion

        #region 序列化接口
        /// <summary>读取实现了可序列化接口的对象</summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        public override bool ReadSerializable(Type type, ref object value, ReadObjectCallback callback)
        {
            if (!typeof(IXmlSerializable).IsAssignableFrom(type))
                return base.ReadSerializable(type, ref value, callback);

            if (value == null) value = TypeX.CreateInstance(type);
            ((IXmlSerializable)value).ReadXml(Reader);
            return true;
        }
        #endregion
    }
}