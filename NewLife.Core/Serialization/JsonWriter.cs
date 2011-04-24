using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections;
using System.Globalization;

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

        private Boolean _Indent;
        /// <summary>缩进</summary>
        public Boolean Indent
        {
            get { return _Indent; }
            set { _Indent = value; }
        }

        private Boolean _JsDateTimeFormat;
        /// <summary>Js时间日期格式</summary>
        public Boolean JsDateTimeFormat
        {
            get { return _JsDateTimeFormat; }
            set { _JsDateTimeFormat = value; }
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

        /// <summary>
        /// 将字节数组部分写入当前流。
        /// </summary>
        /// <param name="buffer">包含要写入的数据的字节数组。</param>
        /// <param name="index">buffer 中开始写入的起始点。</param>
        /// <param name="count">要写入的字节数。</param>
        public override void Write(byte[] buffer, int index, int count)
        {
            Write(Encoding.GetString(buffer, index, count));
        }

        /// <summary>
        /// 将单字节 Boolean 值写入
        /// </summary>
        /// <param name="value">要写入的 Boolean 值</param>
        public override void Write(bool value)
        {
            Writer.Write(value ? "true" : "false");
        }

        /// <summary>
        /// 将一个时间日期写入
        /// </summary>
        /// <param name="value"></param>
        public override void Write(DateTime value)
        {
            String str = String.Format("Date({0})", (Int64)(value - BaseDateTime).TotalMilliseconds);
            if (JsDateTimeFormat)
                Write("new " + str);
            else
                Write("/" + str + "/");
        }
        #endregion

        #region 数字
        void WriteNumber(Double value)
        {
            Writer.Write(value);
        }

        /// <summary>
        /// 将 2 字节有符号整数写入当前流，并将流的位置提升 2 个字节。
        /// </summary>
        /// <param name="value">要写入的 2 字节有符号整数。</param>
        public override void Write(short value)
        {
            WriteNumber(value);
        }

        /// <summary>
        /// 将 4 字节有符号整数写入当前流，并将流的位置提升 4 个字节。
        /// </summary>
        /// <param name="value">要写入的 4 字节有符号整数。</param>
        public override void Write(int value)
        {
            WriteNumber(value);
        }

        /// <summary>
        /// 将 8 字节有符号整数写入当前流，并将流的位置提升 8 个字节。
        /// </summary>
        /// <param name="value">要写入的 8 字节有符号整数。</param>
        public override void Write(long value)
        {
            WriteNumber(value);
        }

        /// <summary>
        /// 将 4 字节浮点值写入当前流，并将流的位置提升 4 个字节。
        /// </summary>
        /// <param name="value">要写入的 4 字节浮点值。</param>
        public override void Write(float value)
        {
            WriteNumber(value);
        }

        /// <summary>
        /// 将 8 字节浮点值写入当前流，并将流的位置提升 8 个字节。
        /// </summary>
        /// <param name="value">要写入的 8 字节浮点值。</param>
        public override void Write(double value)
        {
            WriteNumber(value);
        }

        /// <summary>
        /// 将一个十进制值写入当前流，并将流位置提升十六个字节。
        /// </summary>
        /// <param name="value">要写入的十进制值。</param>
        public override void Write(decimal value)
        {
            Writer.Write(value);
        }
        #endregion

        #region 字符串
        /// <summary>
        /// 将 Unicode 字符写入当前流，并根据所使用的 Encoding 和向流中写入的特定字符，提升流的当前位置。
        /// </summary>
        /// <param name="ch">要写入的非代理项 Unicode 字符。</param>
        public override void Write(char ch)
        {
            //Writer.Write(ch);

            if (ch == '\0')
                Writer.Write("null");
            else
                Write(ch.ToString());
        }

        /// <summary>
        /// 写入字符串
        /// </summary>
        /// <param name="value">要写入的值。</param>
        public override void Write(string value)
        {
            value = Encode(value);

            Writer.Write("\"" + value + "\"");
        }

        static string Encode(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            StringBuilder builder = null;
            int startIndex = 0;
            int count = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                // 拥有特殊字符时才编码处理
                if (c == '\r' || c == '\t' || c == '"' || c == '\'' || c == '<' || c == '>' ||
                    c == '\\' || c == '\n' || c == '\b' || c == '\f' || c < ' ')
                {
                    if (builder == null) builder = new StringBuilder(value.Length + 5);

                    if (count > 0) builder.Append(value, startIndex, count);

                    startIndex = i + 1;
                    count = 0;
                }
                switch (c)
                {
                    case '<':
                    case '>':
                    case '\'':
                        builder.Append(@"\u");
                        builder.Append(((Int32)c).ToString("x4", CultureInfo.InvariantCulture));
                        continue;
                    case '\\':
                        builder.Append(@"\\");
                        continue;
                    case '\b':
                        builder.Append(@"\b");
                        continue;
                    case '\t':
                        builder.Append(@"\t");
                        continue;
                    case '\n':
                        builder.Append(@"\n");
                        continue;
                    case '\f':
                        builder.Append(@"\f");
                        continue;
                    case '\r':
                        builder.Append(@"\r");
                        continue;
                    case '"':
                        builder.Append("\\\"");
                        continue;
                }
                if (c < ' ')
                {
                    builder.Append(@"\u");
                    builder.Append(((Int32)c).ToString("x4", CultureInfo.InvariantCulture));
                }
                else
                    count++;
            }
            if (builder == null) return value;
            if (count > 0) builder.Append(value, startIndex, count);
            return builder.ToString();
        }
        #endregion

        #region 枚举
        /// <summary>
        /// 写入枚举数据，复杂类型使用委托方法进行处理
        /// </summary>
        /// <param name="value">对象</param>
        /// <param name="type">类型</param>
        /// <param name="callback">使用指定委托方法处理复杂数据</param>
        /// <returns>是否写入成功</returns>
        public override bool WriteEnumerable(IEnumerable value, Type type, WriteObjectCallback callback)
        {
            Writer.Write("[");
            Boolean rs = base.WriteEnumerable(value, type, callback);
            Writer.Write("]");

            return rs;
        }

        /// <summary>
        /// 写入枚举项
        /// </summary>
        /// <param name="value">对象</param>
        /// <param name="type">类型</param>
        /// <param name="index">成员索引</param>
        /// <param name="callback">使用指定委托方法处理复杂数据</param>
        /// <returns>是否写入成功</returns>
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

        /// <summary>
        /// 写对象成员
        /// </summary>
        /// <param name="value">要写入的对象</param>
        /// <param name="type">要写入的对象类型</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        public override bool WriteCustomObject(object value, Type type, WriteObjectCallback callback)
        {
            Writer.Write("{");
            Writer.WriteLine();
            Boolean rs = base.WriteCustomObject(value, type, callback);
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