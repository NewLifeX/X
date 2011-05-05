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
    }
}