using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
                    var settings = new XmlWriterSettings();
                    settings.Encoding = Settings.Encoding.TrimPreamble();
                    settings.Indent = true;
                    _Writer = XmlWriter.Create(Stream, settings);
                }
                return _Writer;
            }
            set
            {
                _Writer = value;
                if (Settings.Encoding != _Writer.Settings.Encoding) Settings.Encoding = _Writer.Settings.Encoding;

                var xw = _Writer as XmlTextWriter;
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
        /// <summary>输出字符串字面值,不做编码处理</summary>
        protected override void OnWriteLiteral(string value)
        {
            Writer.WriteString(value);

            AutoFlush();
        }
        #endregion

        #region 时间
        /// <summary>将一个时间日期写入</summary>
        /// <param name="value"></param>
        public override void Write(DateTime value) { Write(XmlConvert.ToString(value, Settings.DateTimeMode)); }
        #endregion

        /// <summary>写入值类型，只能识别基础类型，对于不能识别的类型，方法返回false</summary>
        /// <param name="value">要写入的对象</param>
        /// <param name="type">要写入的对象类型</param>
        /// <returns>是否写入成功</returns>
        protected override bool WriteValue(object value, Type type)
        {
            if (type == null)
            {
                if (value == null) return false;
                type = value.GetType();
            }

            var code = Type.GetTypeCode(type);
            // XmlConvert特殊处理时间，输出Char时按字符输出，不同于Xml序列化的数字，所以忽略
            if (code != TypeCode.Char && code != TypeCode.String && code != TypeCode.DateTime)
            {
                // XmlConvert也支持这三种值类型
                if (type.CanXmlConvert())
                {
                    var str = XmlHelper.XmlConvertToString(value);
                    Write(str);
                    return true;
                }
            }

            return base.WriteValue(value, type);
        }
        #endregion

        #region 扩展类型
        /// <summary>写对象类型</summary>
        /// <param name="type">类型</param>
        protected override void WriteObjectType(Type type)
        {
            if (Settings.WriteType)
            {
                Writer.WriteAttributeString("type", "http://www.w3.org/2001/XMLSchema-instance", type.Name);
            }
        }
        #endregion

        #region 字典
        /// <summary>写入字典项 </summary>
        /// <param name="value">对象</param>
        /// <param name="keyType">键类型</param>
        /// <param name="valueType">值类型</param>
        /// <param name="index">成员索引</param>
        /// <param name="callback">使用指定委托方法处理复杂数据</param>
        /// <returns>是否写入成功</returns>
        protected override bool WriteKeyValue(DictionaryEntry value, Type keyType, Type valueType, int index, WriteObjectCallback callback)
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
        protected override bool WriteItem(Object value, Type type, Int32 index, WriteObjectCallback callback)
        {
            Type t = type;
            if (value != null) t = value.GetType();
            String name = t.GetCustomAttributeValue<XmlRootAttribute, String>(true);
            if (String.IsNullOrEmpty(name) && t != null) name = t.Name;

            Writer.WriteStartElement(name);

            AutoFlush();

            var rs = true;
            if (value != null)
                rs = base.WriteItem(value, type, index, callback);
            else
                Writer.WriteAttributeString("p3", "nil", "http://www.w3.org/2001/XMLSchema-instance", "true");

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
                if (type != null) name = type.GetCustomAttributeValue<XmlRootAttribute, String>(true);
                if (String.IsNullOrEmpty(name) && t != null) name = t.GetCustomAttributeValue<XmlRootAttribute, String>(true);
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
        /// <param name="name">成员名字</param>
        /// <param name="value">要写入的对象</param>
        /// <param name="type">要写入的对象类型</param>
        /// <param name="index">成员索引</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        protected override bool OnWriteMember(String name, Object value, Type type, Int32 index, WriteObjectCallback callback)
        {
            // 检查成员的值，如果是默认值，则不输出
            //if (value != null && Settings.IgnoreDefault && IsDefault(value, member)) return true;

            // 特殊处理特性，只有普通值类型才能输出为特性
            var isAtt = Settings.MemberAsAttribute && IsAttributeType(type);
            if (isAtt)
                Writer.WriteStartAttribute(name);
            else
                Writer.WriteStartElement(name);

            AutoFlush();

            var rs = base.OnWriteMember(name, value, type, index, callback);

            if (!isAtt) Writer.WriteEndElement();

            AutoFlush();

            return rs;
        }

        /// <summary>写对象引用计数</summary>
        /// <param name="index"></param>
        protected override void OnWriteObjRefIndex(int index) { if (index > 0) Writer.WriteAttributeString("ObjRef", index.ToString()); }
        #endregion

        #region 未知对象
        /// <summary>写入未知对象（其它所有方法都无法识别的对象），采用BinaryFormatter或者XmlSerialization</summary>
        /// <param name="value">要写入的对象</param>
        /// <param name="type">要写入的对象类型</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        public override bool WriteUnKnown(object value, Type type, WriteObjectCallback callback)
        {
            try
            {
                WriteLog("WriteUnKnown", type.Name);
                //XmlSerializer serial = new XmlSerializer(type);
                //MemoryStream ms = new MemoryStream();
                //serial.Serialize(ms, value);

                //String xml = Encoding.UTF8.GetString(ms.ToArray());
                Write(value.ToXml(Settings.Encoding, "", "", false));

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

        /// <summary>备份当前环境，用于临时切换数据流等</summary>
        /// <returns>本次备份项集合</returns>
        public override IDictionary<String, Object> Backup()
        {
            var dic = base.Backup();
            dic["Writer"] = Writer;

            return dic;
        }

        /// <summary>恢复最近一次备份</summary>
        /// <returns>本次还原项集合</returns>
        public override IDictionary<String, Object> Restore()
        {
            var dic = base.Restore();
            Writer = dic["Writer"] as XmlWriter;

            return dic;
        }
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

        #region 获取成员
        /// <summary>获取需要序列化的成员（属性或字段）。在序列化为属性时，需要排列成员，先拍属性，否则会有问题</summary>
        /// <param name="type">指定类型</param>
        /// <param name="value">对象</param>
        /// <returns>需要序列化的成员</returns>
        protected override IObjectMemberInfo[] OnGetMembers(Type type, object value)
        {
            var mis = base.OnGetMembers(type, value);
            if (!Settings.MemberAsAttribute) return mis;

            var list = new List<IObjectMemberInfo>();
            var list2 = new List<IObjectMemberInfo>();
            foreach (var item in mis)
            {
                if (IsAttributeType(item.Type))
                    list.Add(item);
                else
                    list2.Add(item);
            }
            list.AddRange(list2);
            return list.ToArray();
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
        /// <param name="type">类型</param>
        /// <returns></returns>
        internal static Boolean IsAttributeType(Type type)
        {
            //if (typeof(Type).IsAssignableFrom(type)) return true;

            var code = Type.GetTypeCode(type);
            return code != TypeCode.Object;
        }
        #endregion
    }
}