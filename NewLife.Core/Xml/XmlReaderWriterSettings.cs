using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Serialization;

namespace NewLife.Xml
{
    /// <summary>
    /// Xml序列化设置
    /// </summary>
    public class XmlReaderWriterSettings : TextReaderWriterSetting
    {
        #region 属性
        private Boolean _MemberAsAttribute;
        /// <summary>成员作为属性</summary>
        public Boolean MemberAsAttribute
        {
            get { return _MemberAsAttribute; }
            set { _MemberAsAttribute = value; }
        }

        private Boolean _IgnoreDefault;
        /// <summary>忽略默认</summary>
        public Boolean IgnoreDefault
        {
            get { return _IgnoreDefault; }
            set { _IgnoreDefault = value; }
        }
        #endregion
    }
}