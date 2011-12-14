using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NewLife.Serialization
{
    /// <summary>
    /// 名值读取器。用于Http请求、Http接口响应、Cookie值等读写操作。
    /// </summary>
    public class NameValueReader : TextReaderBase<NameValueSetting>
    {
        #region 属性
        private TextReader _Reader;
        /// <summary>读取器</summary>
        public TextReader Reader
        {
            get { return _Reader ?? (_Reader = new StreamReader(Stream, Settings.Encoding)); }
            set
            {
                _Reader = value;

                StreamReader sr = _Reader as StreamReader;
                if (sr != null)
                {
                    if (Settings.Encoding != sr.CurrentEncoding) Settings.Encoding = sr.CurrentEncoding;
                    if (Stream != sr.BaseStream) Stream = sr.BaseStream;
                }
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
        #endregion
    }
}