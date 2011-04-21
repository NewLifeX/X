using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Serialization;

namespace NewLife.Xml
{
    /// <summary>
    /// Xml读写器设置
    /// </summary>
    class XmlReaderWriterConfig : ReaderWriterConfig
    {
        private XmlMemberStyle _MemberStyle;
        /// <summary>成员样式</summary>
        public XmlMemberStyle MemberStyle
        {
            get { return _MemberStyle; }
            set { _MemberStyle = value; }
        }

        private Boolean _IgnoreDefault;
        /// <summary>忽略默认</summary>
        public Boolean IgnoreDefault
        {
            get { return _IgnoreDefault; }
            set { _IgnoreDefault = value; }
        }

        ///// <summary>
        ///// 克隆
        ///// </summary>
        ///// <returns></returns>
        //public override ReaderWriterConfig Clone()
        //{
        //    XmlReaderWriterConfig config = base.Clone() as XmlReaderWriterConfig;
        //    config.MemberStyle = MemberStyle;
        //    return config;
        //}
    }
}