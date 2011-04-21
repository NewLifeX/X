using System;
using System.Collections.Generic;
using System.Text;
using NewLife.IO;
using NewLife.Serialization;
using System.Xml;

namespace NewLife.Xml
{
    /// <summary>
    /// Xml读取器
    /// </summary>
    public class XmlReaderX : ReaderBase
    {
        #region 属性
        private XmlReader _Reader;
        /// <summary>读取器</summary>
        public XmlReader Reader
        {
            get { return _Reader; }
            set { _Reader = value; }
        }

        private String _RootName;
        /// <summary>根元素名</summary>
        public String RootName
        {
            get { return _RootName; }
            set { _RootName = value; }
        }

        private XmlMemberStyle _MemberStyle;
        /// <summary>成员样式</summary>
        public XmlMemberStyle MemberStyle
        {
            get { return _MemberStyle; }
            set { _MemberStyle = value; }
        }
        #endregion

        /// <summary>
        /// 读取字节
        /// </summary>
        /// <returns></returns>
        public override byte ReadByte()
        {
            throw new NotImplementedException();
        }
    }
}