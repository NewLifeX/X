using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NewLife.Net.Protocols
{
    /// <summary>文本行分析器</summary>
    public class LineParser
    {
        #region 属性
        private Encoding _Encoding;
        /// <summary>编码</summary>
        public Encoding Encoding { get { return _Encoding; } set { _Encoding = value; } }

        private Stream _Stream;
        /// <summary>数据流</summary>
        public Stream Stream { get { return _Stream; } set { _Stream = value; } }
        #endregion

        #region 构造
        /// <summary>实例化一个文本行分析器</summary>
        public LineParser() : this(null) { }

        /// <summary>实例化一个文本行分析器</summary>
        /// <param name="stream"></param>
        public LineParser(Stream stream) : this(stream, Encoding.UTF8) { }

        /// <summary>实例化一个文本行分析器</summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        public LineParser(Stream stream, Encoding encoding)
        {
            Stream = stream;
            Encoding = encoding;
        }
        #endregion
    }
}