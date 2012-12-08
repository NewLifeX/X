using System;

namespace NewLife.Serialization
{
    /// <summary>名值读写器设置</summary>
    public class NameValueSetting : TextReaderWriterSetting
    {
        private String _Separator = "=";
        /// <summary>分隔符</summary>
        public String Separator
        {
            get { return _Separator; }
            set { _Separator = value; }
        }

        private String _Connector = "&";
        /// <summary>连接符</summary>
        public String Connector
        {
            get { return _Connector; }
            set { _Connector = value; }
        }
    }
}