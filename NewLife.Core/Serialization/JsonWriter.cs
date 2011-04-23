using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections;

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

        #region 已重载
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void Write(byte value)
        {
            Writer.Write(value);
        }

        public override void Write(bool value)
        {
            Writer.Write(value ? "true" : "false");
        }

        public override void Write(DateTime value)
        {
            Write(String.Format("/Date({0})/", (Int64)(value - BaseDateTime).TotalMilliseconds));
        }
        #endregion

        #region 数字
        void WriteNumber(Double value)
        {
            Writer.Write(value);
        }

        public override void Write(short value)
        {
            WriteNumber(value);
        }

        public override void Write(int value)
        {
            WriteNumber(value);
        }

        public override void Write(long value)
        {
            WriteNumber(value);
        }

        public override void Write(float value)
        {
            WriteNumber(value);
        }

        public override void Write(double value)
        {
            WriteNumber(value);
        }

        public override void Write(decimal value)
        {
            Writer.Write(value);
        }
        #endregion

        #region 字符串
        public override void Write(char ch)
        {
            Writer.Write(ch);
        }

        public override void Write(string value)
        {
            value = Encode(value);

            Writer.Write("\"" + value + "\"");
        }

        String Encode(String str)
        {
            if (String.IsNullOrEmpty(str)) return str;

            return str.Replace("\"", "\\\"")
                .Replace("\\", "\\\\")
                .Replace("/", "\\/")
                .Replace(" ", "\\b")
                .Replace("\f", "\\f")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
        #endregion

        #region 枚举
        public override bool Write(IEnumerable value, Type type, WriteObjectCallback callback)
        {
            Writer.Write("[");
            Boolean rs = base.Write(value, type, callback);
            Writer.Write("]");

            return rs;
        }

        public override bool WriteItem(object value, Type type, int index, WriteObjectCallback callback)
        {
            if (index > 0)
            {
                Writer.Write(",");
            }

            return base.WriteItem(value, type, index, callback);
        }
        #endregion

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
                Writer.Write("null");
                return true;
            }

            return base.WriteObject(value, type, callback);
        }

        public override bool WriteMembers(object value, Type type, WriteObjectCallback callback)
        {
            Writer.Write("{");
            Writer.WriteLine();
            Boolean rs = base.WriteMembers(value, type, callback);
            Writer.WriteLine();
            Writer.Write("}");

            return rs;
        }

        /// <summary>
        /// 写入成员
        /// </summary>
        /// <param name="value">要写入的对象</param>
        /// <param name="member">成员</param>
        /// <param name="index">成员索引</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        protected override bool WriteMember(object value, IObjectMemberInfo member, Int32 index, WriteObjectCallback callback)
        {
            if (index > 0)
            {
                Writer.Write(",");
                Writer.WriteLine();
            }

            Writer.Write("\"" + member.Name + "\": ");

            return base.WriteMember(value, member, index, callback);
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