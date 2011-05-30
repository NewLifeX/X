using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace NewLife.Serialization
{
    /// <summary>
    /// Json写入器
    /// </summary>
    public class JsonWriter : TextWriterBase<JsonSettings>
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
                    _Writer = new StreamWriter(Stream, Settings.Encoding);
                }
                return _Writer;
            }
            set
            {
                _Writer = value;
                if (Settings.Encoding != _Writer.Encoding) Settings.Encoding = _Writer.Encoding;

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

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        public JsonWriter()
            : base()
        {
            Settings.DepthLimit = 16;
#if DEBUG
            Settings.DepthLimit = 5;
#endif
        }
        #endregion

        #region 字节/字节数组
        /// <summary>
        /// 以数字的格式写入字节
        /// </summary>
        /// <param name="value"></param>
        public override void Write(byte value)
        {
            WriteLiteral(string.Format("{0}", value)); //json数字不包括16进制和8进制表示
        }
        /// <summary>
        /// 将字节数组以[0xff,0xff,0xff]的格式写入
        /// </summary>
        /// <param name="buffer"></param>
        public override void Write(byte[] buffer)
        {
            if (buffer == null)
            {
                WriteLiteral("null");
            }
            else
            {
                Write(buffer, 0, buffer.Length);
            }
        }
        /// <summary>
        /// 将字节数组部分写入当前流。
        /// </summary>
        /// <param name="buffer">包含要写入的数据的字节数组。</param>
        /// <param name="index">buffer 中开始写入的起始点。</param>
        /// <param name="count">要写入的字节数。</param>
        public override void Write(byte[] buffer, int index, int count)
        {
            WriteEnumerable(Slice(buffer, index, count), typeof(Byte[]), BaseWriteMember);
        }
        #endregion

        #region 布尔
        /// <summary>
        /// 将单字节 Boolean 值写入
        /// </summary>
        /// <param name="value">要写入的 Boolean 值</param>
        public override void Write(bool value)
        {
            Depth++;
            WriteLiteral(value ? "true" : "false");
            Depth--;
        }
        #endregion

        #region 时间
        /// <summary>
        /// 将一个时间日期写入
        /// </summary>
        /// <param name="value"></param>
        public override void Write(DateTime value)
        {
            Depth++;
            WriteLog("WriteValue", "DateTime", value);

            if (Settings.JsDateTimeKind != DateTimeKind.Unspecified && value.Kind != Settings.JsDateTimeKind)
            {
                if (Settings.JsDateTimeKind == DateTimeKind.Local)
                {
                    value = value.ToLocalTime();
                }
                else
                {
                    value = value.ToUniversalTime();
                }
            }

            switch (Settings.JsDateTimeFormat)
            {
                case JsDateTimeFormats.ISO8601:
                    Write(value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
                    break;
                case JsDateTimeFormats.DotnetDateTick:
                    Write(string.Format("\\/Date({0})\\/", (long)(value - Settings.BaseDateTime).TotalMilliseconds));
                    break;
                case JsDateTimeFormats.Tick:
                    Write(Settings.ConvertDateTimeToInt64(value));
                    break;
            }
            Depth--;
        }
        #endregion

        #region 数字
        /// <summary>
        /// 将 2 字节有符号整数写入当前流
        /// </summary>
        /// <param name="value">要写入的 2 字节有符号整数。</param>
        public override void Write(short value)
        {
            WriteLiteral(value.ToString());
        }

        /// <summary>
        /// 将 4 字节有符号整数写入当前流
        /// </summary>
        /// <param name="value">要写入的 4 字节有符号整数。</param>
        public override void Write(int value)
        {
            WriteLiteral(value.ToString());
        }

        /// <summary>
        /// 将 8 字节有符号整数写入当前流
        /// </summary>
        /// <param name="value">要写入的 8 字节有符号整数。</param>
        public override void Write(long value)
        {
            WriteLiteral(value.ToString());
        }

        /// <summary>
        /// 将 4 字节浮点值写入当前流
        /// </summary>
        /// <param name="value">要写入的 4 字节浮点值。</param>
        public override void Write(float value)
        {
            WriteLiteral(value.ToString());
        }

        /// <summary>
        /// 将 8 字节浮点值写入当前流
        /// </summary>
        /// <param name="value">要写入的 8 字节浮点值。</param>
        public override void Write(double value)
        {
            WriteLiteral(value.ToString());
        }

        /// <summary>
        /// 将一个十进制值写入当前流，并将流位置提升十六个字节。
        /// </summary>
        /// <param name="value">要写入的十进制值。</param>
        public override void Write(decimal value)
        {
            WriteLiteral(value.ToString());
        }
        #endregion

        #region 字符串
        /// <summary>
        /// 输出字符串字面值,不做编码处理
        /// </summary>
        /// <param name="value"></param>
        void WriteLiteral(string value)
        {
            Depth++;
            WriteLog("WriteValue", "Literal", value);
            Writer.Write(value);
            Depth--;
        }
        void WriteLine()
        {
            if (Settings.JsMultiline)
            {
                Writer.WriteLine();
            }
        }
        /// <summary>
        /// 将 Unicode 字符写入当前流，并根据所使用的 Encoding 和向流中写入的特定字符，提升流的当前位置。
        /// </summary>
        /// <param name="ch">要写入的非代理项 Unicode 字符。</param>
        public override void Write(char ch)
        {
            if (ch == '\0')
            {
                WriteLiteral("null");
            }
            else
            {
                Write(ch.ToString());
            }
        }
        /// <summary>
        /// 将 Unicode 字符写入当前流，并根据所使用的 Encoding 和向流中写入的特定字符，提升流的当前位置。
        /// </summary>
        /// <param name="chars"></param>
        public override void Write(char[] chars)
        {
            if (chars == null)
            {
                WriteLiteral("null");
            }
            else
            {
                Write(chars, 0, chars.Length);
            }
        }
        /// <summary>
        /// 将 Unicode 字符写入当前流，并根据所使用的 Encoding 和向流中写入的特定字符，提升流的当前位置。
        /// </summary>
        /// <param name="chars"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        public override void Write(char[] chars, int index, int count)
        {
            WriteEnumerable(Slice(chars, index, count), typeof(char[]), BaseWriteMember);
        }

        /// <summary>
        /// 写入字符串
        /// </summary>
        /// <param name="value">要写入的值。</param>
        public override void Write(string value)
        {
            value = JavascriptStringEncode(value, this.Settings.JsEncodeUnicode);
            Depth++;
            WriteLog("WriteValue", "String", value);
            Writer.Write("\"" + value + "\"");
            Depth--;
        }
        /// <summary>
        /// 将指定字符串编码成json中表示的字符串,将编码Unicode字符为\uXXXX
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string JavascriptStringEncode(string value)
        {
            return JavascriptStringEncode(value, true);
        }
        /// <summary>
        /// 将指定字符串编码成javascript的字面字符串(即写入到js代码中的和value内容相同的代码),开始和结尾不包含双引号
        /// </summary>
        /// <param name="value">要编码的字符串,value为null时返回""</param>
        /// <param name="encodeUnicode">是否将Unicode字符编码为\uXXXX的格式</param>
        /// <returns></returns>
        public static string JavascriptStringEncode(string value, bool encodeUnicode)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            StringBuilder builder = null;
            int startIndex = 0;
            int count = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                string estr = null;
                // 拥有特殊字符时才编码处理
                switch (c) //根据json.org定义的string规范
                {
                    case '"':
                        estr = "\\\""; break;
                    case '\\':
                        estr = "\\\\"; break;
                    case '/':
                        estr = "/"; break;
                    case '\b':
                        estr = "\\b"; break;
                    case '\f':
                        estr = "\\f"; break;
                    case '\n':
                        estr = "\\n"; break;
                    case '\r':
                        estr = "\\r"; break;
                    case '\t':
                        estr = "\\t"; break;
                    default:
                        if (c < ' ' || (encodeUnicode && c > 0x7e)) // 避免json直接输出中文乱码的情况
                        {
                            estr = string.Format("\\u{0:x4}", ((UInt16)c));
                        }
                        break;
                }
                if (estr != null)
                {
                    if (builder == null) builder = new StringBuilder(value.Length + 5);

                    if (count > 0) builder.Append(value, startIndex, count);

                    startIndex = i + 1;

                    count = 0;
                    builder.Append(estr);
                }
                else
                {
                    count++;
                }
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
            Boolean rs;
            Writer.Write("[");
            ComplexObjectDepth++;
            if (!ComplexObjectDepthIsOverflow())
            {
                rs = base.WriteEnumerable(value, type, callback);
            }
            else
            {
                Depth++;
                WriteLog("WriteSkip", "ComplexObjectDepthIsOverflow");
                Depth--;
                rs = true;
            }
            ComplexObjectDepth--;
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
        protected override bool OnWriteItem(object value, Type type, int index, WriteObjectCallback callback)
        {
            WriteLog("WriteEnumerableItem", index, type != null ? type.FullName : "Unknown type");
            if (index > 0)
            {
                Writer.Write(",");
            }
            if (value != null && !IsCanCreateInstance(type))
            {
                type = value.GetType(); //避免base.OnWriteItem中写入value.GetType()
                writeValueType = value;
            }
            bool ret = base.OnWriteItem(value, type, index, callback);
            writeValueType = null;
            return ret;
        }
        /// <summary>
        /// 返回指定数组的一个片段,始终返回的是array参数的一个副本
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static T[] Slice<T>(T[] array, int index, int count)
        {
            T[] ret = new T[count];
            if (count > 0)
            {
                Array.Copy(array, index, ret, 0, count);
            }
            return ret;
        }
        #endregion

        #region 字典
        /// <summary>
        /// 将字典类型数据写入到当前流位置
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public override bool WriteDictionary(IDictionary value, Type type, WriteObjectCallback callback)
        {
            if (value == null)
            {
                WriteLiteral("null");
                return true;
            }
            bool ret;
            Writer.Write("{");

            ComplexObjectDepth++;
            if (!ComplexObjectDepthIsOverflow())
            {
                WriteLine();
                ret = base.WriteDictionary(value, type, callback);
                WriteLine();
            }
            else
            {
                Depth++;
                WriteLog("WriteSkip", "ComplexObjectDepthIsOverflow");
                Depth--;
                ret = true;
            }
            ComplexObjectDepth--;
            Writer.Write("}");
            return ret;
        }
        /// <summary>
        /// 写入字典键和值
        /// </summary>
        /// <param name="value"></param>
        /// <param name="keyType"></param>
        /// <param name="valueType"></param>
        /// <param name="index"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        protected override bool OnWriteKeyValue(DictionaryEntry value, Type keyType, Type valueType, int index, WriteObjectCallback callback)
        {
            if (index > 0)
            {
                Writer.Write(",");
                WriteLine();
            }
            WriteLog("WriteDictionaryEntry", "Key");
            Write(value.Key.ToString());//json标准要求key必须是字符串
            Writer.Write(":");
            WriteLog("WriteDictionaryEntry", "Value");
            if (value.Value != null && !IsCanCreateInstance(valueType)) //无法取得字典项的值类型
            {
                writeValueType = value.Value; //valueType会在WriteObject内部被重新赋值,所以不做额外处理
            }
            bool ret = WriteObject(value.Value, valueType, callback);
            writeValueType = null;
            return ret;
        }
        #endregion

        #region 写入对象
        /// <summary>
        /// 是否需要写入值类型信息的标志,为null时表示不需要,非null时并且等于待写入的值时写入值类型
        /// </summary>
        object writeValueType = null;
        /// <summary>
        /// 写入的复合对象深度,指使用{} []包括的深度
        /// </summary>
        int ComplexObjectDepth = 0;
        /// <summary>
        /// 是否写入成员的计数器,用于控制换行输出
        /// </summary>
        int WriteMemberCount = 0;
        /// <summary>
        /// JsonWriter的对象类型由writeValueType写入,作为第一个成员,所以不需要
        /// </summary>
        /// <param name="type"></param>
        protected override void WriteObjectType(Type type)
        {
        }
        /// <summary>
        /// 写入对象。具体读写器可以重载该方法以修改写入对象前后的行为。
        /// </summary>
        /// <param name="value">对象</param>
        /// <param name="type">要写入的对象类型</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        protected override bool OnWriteObject(object value, Type type, WriteObjectCallback callback)
        {
            if (value == null)
            {
                WriteLiteral("null");
                return true;
            }
            return base.OnWriteObject(value, type, callback);
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
            Boolean rs, writedType = false;
            Writer.Write("{");
            if (value != null && writeValueType == value) //写入明确的类型
            {
                WriteLine();
                string fullname = value.GetType().FullName;
                Depth++;
                WriteLog("WriteType", "__type", fullname);
                WriteLiteral(string.Format("\"__type\":\"{0}\"", JavascriptStringEncode(fullname, this.Settings.JsEncodeUnicode)));
                //后续的逗号和换行符由WriteCustomObject中OnWriteMember输出,并将writeValueType置为null 因为后续可能没有任何成员
                Depth--;
                writedType = true;
            }

            ComplexObjectDepth++;
            if (!ComplexObjectDepthIsOverflow())
            {
                int i = WriteMemberCount;
                rs = base.WriteCustomObject(value, type, callback);
                writeValueType = null;
                if (WriteMemberCount > i)
                {
                    WriteLine();
                }
                WriteMemberCount = i;
            }
            else
            {
                if (writedType) WriteLine();
                Depth++;
                WriteLog("WriteSkip", "ComplexObjectDepthIsOverflow");
                Depth--;
                rs = true;
            }
            ComplexObjectDepth--;
            Writer.Write("}");

            return rs;
        }
        /// <summary>
        /// 写入成员
        /// </summary>
        /// <param name="value">要写入的对象</param>
        /// <param name="memberType">要写入的成员类型</param>
        /// <param name="member">要写入的成员信息,可以通过[value]取得成员值</param>
        /// <param name="index">成员索引</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        protected override bool OnWriteMember(object value, Type memberType, IObjectMemberInfo member, Int32 index, WriteObjectCallback callback)
        {
            if (index > 0 || writeValueType != null)
            {
                writeValueType = null;
                Writer.Write(",");
            }
            WriteMemberCount++;

            WriteLine();

            Writer.Write("\"" + JavascriptStringEncode(member.Name) + "\":");

            object obj = member[value];
            if (obj != null && !IsCanCreateInstance(memberType))
            {
                memberType = obj.GetType(); //避免base.OnWriteMember中写入obj.GetType()
                writeValueType = obj;
            }
            bool ret = base.OnWriteMember(value, memberType, member, index, callback);
            writeValueType = null;
            return ret;
        }
        /// <summary>
        /// 当前解析复合对象深度是否超出,用于避免循环引用可能引起的堆栈溢出,仅在Settings.RepeatedActionType是RepeatedAction.DepthLimit时才可能返回true
        /// </summary>
        /// <returns></returns>
        public bool ComplexObjectDepthIsOverflow()
        {
            return Settings.RepeatedActionType == RepeatedAction.DepthLimit && ComplexObjectDepth > Settings.DepthLimit;
        }
        /// <summary>
        /// 返回指定类型是否是可以实例化的,即反序列化时是否是可以实例化的类型,一般用于处理未知类型前
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsCanCreateInstance(Type type)
        {
            return type != null && type != typeof(object) && !type.IsInterface && !type.IsAbstract;
        }
        /// <summary>
        /// 模仿父类的WriteMember实现
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        protected static bool BaseWriteMember(IWriter writer, Object value, Type type, WriteObjectCallback callback)
        {
            return writer.WriteObject(value, type, callback); //模仿基类中的Boolean WriteMember(IWriter writer, Object value, Type type, WriteObjectCallback callback)
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