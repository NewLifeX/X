using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NewLife.Serialization
{
    /// <summary>
    /// Json写入器
    /// </summary>
    public class JsonWriter : WriterBase
    {
        #region 属性
        private TextWriter _Writer;
        /// <summary>写入器</summary>
        public TextWriter Writer
        {
            get
            {
                if (_Writer == null)
                {
                    _Writer = new StreamWriter(Stream, Encoding);
                }
                return _Writer;
            }
            set
            {
                _Writer = value;
                if (Encoding != _Writer.Encoding) Encoding = _Writer.Encoding;

                StreamWriter sw = _Writer as StreamWriter;
                if (sw != null && sw.BaseStream != Stream) Stream = sw.BaseStream;
            }
        }

        /// <summary>
        /// 数据流。更改数据流后，重置Writer为空，以使用新的数据流
        /// </summary>
        public override Stream Stream
        {
            get
            {
                return base.Stream;
            }
            set
            {
                if (base.Stream != value) _Writer = null;
                base.Stream = value;
            }
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void Write(byte value)
        {
            throw new NotImplementedException();
        }

        #region 写入对象
        /// <summary>
        /// 写入对象。具体读写器可以重载该方法以修改写入对象前后的行为。
        /// </summary>
        /// <param name="value">对象</param>
        /// <param name="type">要写入的对象类型</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        public override bool WriteObject(object value, Type type, WriteObjectCallback callback)
        {
            if (value == null)
            {
                Write("null");
                return true;
            }

            Write("[");
            Boolean rs = base.WriteObject(value, type, callback);
            Write("]");

            return rs;
        }
        #endregion

        #region 方法
        /// <summary>
        /// 刷新缓存中的数据
        /// </summary>
        public override void Flush()
        {
            Writer.Flush();

            base.Flush();
        }
        #endregion
    }
}