using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>
    /// 字符串读写器设置
    /// </summary>
    public class TextReaderWriterSetting : ReaderWriterSetting
    {
        private Boolean _UseBase64;
        /// <summary>使用Base64编码字符串，否则使用十六进制字符串</summary>
        public Boolean UseBase64
        {
            get { return _UseBase64; }
            set { _UseBase64 = value; }
        }

        private Boolean _UseEnumName = true;
        /// <summary>是否使用名称表示枚举类型，默认使用名称</summary>
        public Boolean UseEnumName
        {
            get { return _UseEnumName; }
            set { _UseEnumName = value; }
        }

        private Boolean _WriteType = true;
        /// <summary>是否输出类型</summary>
        public Boolean WriteType
        {
            get { return _WriteType; }
            set { _WriteType = value; }
        }
    }
}