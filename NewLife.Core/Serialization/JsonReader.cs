using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NewLife.Serialization
{
    /// <summary>
    /// Json读取器
    /// </summary>
    public class JsonReader : ReaderBase<SerialSettings>
    {
        #region 属性
        private TextReader _Reader;
        /// <summary>读取器</summary>
        public TextReader Reader
        {
            get
            {
                if (_Reader == null)
                {
                    _Reader = new StreamReader(Stream, Settings.Encoding);
                }
                return _Reader;
            }
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

        /// <summary>
        /// 数据流。更改数据流后，重置Reader为空，以使用新的数据流
        /// </summary>
        public override Stream Stream
        {
            get
            {
                return base.Stream;
            }
            set
            {
                if (base.Stream != value) _Reader = null;
                base.Stream = value;
            }
        }

        #endregion

        #region 字节
        /// <summary>
        /// 读取字节
        /// </summary>
        /// <returns></returns>
        public override byte ReadByte()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 读取字节数组
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public override byte[] ReadBytes(int count)
        {
            return base.ReadBytes(count);
        }
        #endregion

        #region 数字
        #endregion
    }
}