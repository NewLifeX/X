using System;
using NewLife.Serialization;
using System.Xml;

namespace NewLife.Xml
{
    /// <summary>Xml序列化设置</summary>
    public class XmlReaderWriterSettings : TextReaderWriterSetting
    {
        #region 属性
        private Boolean _MemberAsAttribute;
        /// <summary>成员作为属性</summary>
        public Boolean MemberAsAttribute { get { return _MemberAsAttribute; } set { _MemberAsAttribute = value; } }

        private Boolean _IgnoreDefault;
        /// <summary>忽略默认</summary>
        public Boolean IgnoreDefault { get { return _IgnoreDefault; } set { _IgnoreDefault = value; } }

        private XmlDateTimeSerializationMode _DateTimeMode = XmlDateTimeSerializationMode.RoundtripKind;
        /// <summary>指定日期时间输出成什么时间,本地还是UTC时间,默认是UTC时间</summary>
        public XmlDateTimeSerializationMode DateTimeMode { get { return _DateTimeMode; } set { _DateTimeMode = value; } }
        #endregion

        /// <summary>实例化Xml序列化设置</summary>
        public XmlReaderWriterSettings()
        {
            // 默认用Base64
            UseBase64 = true;
        }
    }
}