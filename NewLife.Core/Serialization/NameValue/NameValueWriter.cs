using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NewLife.Serialization
{
    /// <summary>
    /// 名值写入器。用于Http请求、Http接口响应、Cookie值等读写操作。
    /// </summary>
    public class NameValueWriter : TextWriterBase<NameValueSetting>
    {
        #region 属性
        private TextWriter _Writer;
        /// <summary>写入器</summary>
        public TextWriter Writer
        {
            get { return _Writer ?? (_Writer = new StreamWriter(Stream, Settings.Encoding)); }
            set
            {
                _Writer = value;
                if (Settings.Encoding != _Writer.Encoding) Settings.Encoding = _Writer.Encoding;

                StreamWriter sw = _Writer as StreamWriter;
                if (sw != null && sw.BaseStream != Stream) Stream = sw.BaseStream;
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
        #endregion
    }
}