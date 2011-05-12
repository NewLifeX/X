using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;

namespace NewLife.Serialization
{
    /// <summary>
    /// Json读取器
    /// </summary>
    public class JsonReader : TextReaderBase<JsonSettings>
    {
        #region 属性
        long line = 1, column = 1;

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

        #region 字节/字节数组
        /// <summary>
        /// 读取字节
        /// </summary>
        /// <returns></returns>
        public override byte ReadByte()
        {
            string str;
            byte ret;
            AssertReadNextAtomElement("期望是0-255的数字", out str, AtomElementType.NUMBER);
            if (!Byte.TryParse(str, out ret))
            {
                throw new JsonReaderAssertException(line, column, new AtomElementType[] { AtomElementType.NUMBER }, AtomElementType.NUMBER, "期望是0-255的数字,而实际是:" + str);
            }
            return ret;
        }

        /// <summary>
        /// 读取字节数组
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public override byte[] ReadBytes(int count)
        {
            return base.ReadBytes(count);
            // TODO 待实现
        }
        #endregion

        #region 布尔
        /// <summary>
        /// 从当前流位置读取一个布尔型数据
        /// </summary>
        /// <returns></returns>
        public override bool ReadBoolean()
        {
            switch (AssertReadNextAtomElement("期望是true或者false", AtomElementType.TRUE, AtomElementType.FALSE))
            {
                case AtomElementType.TRUE:
                    return true;
                case AtomElementType.FALSE:
                    return false;
                default:
                    return false; //实际执行不到,只是因为代码编译不通过
            }
        }
        #endregion

        #region 时间
        /// <summary>
        /// 从当前流位置读取一个日期时间型数据,支持的格式参考ParseDateTimeString的说明
        /// </summary>
        /// <returns></returns>
        public override DateTime ReadDateTime()
        {
            string str;
            AtomElementType atype = AssertReadNextAtomElement("期望是包含日期时间内容的字符串", out str, AtomElementType.STRING, AtomElementType.LITERAL);
            DateTime dt = ParseDateTimeString(str, atype);
            return dt;
        }
        /// <summary>
        /// 解析日期时间字符串,可以处理多种日期时间格式,包括JsDateTimeFormats枚举中的格式,以及js中toGMTString()的格式
        /// </summary>
        /// <param name="str"></param>
        /// <param name="atype">用于无法解析时,异常信息中包含str所属的原子元素类型</param>
        /// <returns></returns>
        public DateTime ParseDateTimeString(string str, AtomElementType atype)
        {
            DateTime ret;
            if (str.Length > 10 && str.Length < 26)
            // 处理System.Web.Script.Serialization.JavaScriptSerializer日期时间格式,类似 \/Date(12345678)\/
            // 因为MinDateTime和MaxDateTime的十进制毫秒数是15位长度的字符串,最少是1位长度字符串,所以预期长度是11位到25位
            {
                string[] s = str.Split('(', ')');
                long ms;
                if (s.Length >= 3 && s[0] == @"\/Date" && s[2] == @"\/" && long.TryParse(s[1], out ms))
                {
                    return Settings.BaseDateTime.AddMilliseconds(ms);
                }
            }
            string[] formats = { "yyyy-MM-ddTHH:mm:ss.fffZ", //包含毫秒部分的ISO8601格式
                                   "yyyy-MM-ddTHH:mm:ssZ", //不包含毫秒部分的ISO8601格式,和json2.js的toJSON()格式相同(ff3.5 ie8已原生实现Date.toJSON())
                                   "ddd, dd MMM yyyy HH:mm:ss GMT", //js中toGMTString()返回的格式
                                   "yyyy-MM-dd HH:mm:ss", //一般是测试用途的手写格式,不建议使用,下同
                                   "yyyy-MM-dd"
                               };
            if (DateTime.TryParseExact(str, formats, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out ret))
            {
                return ret;
            }
            long ticks;
            if (long.TryParse(str, out ticks)) //dotnet中的Ticks,不建议
            {
                return new DateTime(ticks);
            }
            throw new JsonReaderAssertException(line, column, new AtomElementType[] { AtomElementType.LITERAL, AtomElementType.STRING }, atype, "期望是日期时间格式的内容,实际是" + str);
        }
        #endregion

        #region 数字
        /// <summary>
        /// 数字类型 包括整型和浮点型
        /// </summary>
        public static readonly AtomElementType[] NUMBER_TYPES = { AtomElementType.NUMBER, AtomElementType.NUMBER_EXP,
                                                                    AtomElementType.FLOAT, AtomElementType.FLOAT_EXP };
        /// <summary>
        /// 整型类型
        /// </summary>
        public static readonly AtomElementType[] INTEGER_TYPES = { AtomElementType.NUMBER, AtomElementType.NUMBER_EXP };
        /// <summary>
        /// 尝试解析数字的委托
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="str"></param>
        /// <param name="styles"></param>
        /// <param name="format"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        delegate bool TryParseNumber<T>(string str, NumberStyles styles, IFormatProvider format, out T ret);
        /// <summary>
        /// 从指定的信息中返回尝试解析数字时使用的数字格式
        /// </summary>
        /// <param name="str"></param>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <returns></returns>
        delegate NumberStyles GetNumberStyles(string str, AtomElementType[] expected, AtomElementType actual);
        /// <summary>
        /// 从当前流位置读取一个指定节点类型的数字,并尝试解析为T类型数字
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="getNumStyles">根据读取到的节点信息决定使用什么数字格式解析字符串</param>
        /// <param name="tryParse">将读取到的字符串解析为T类型数字的方法</param>
        /// <param name="exceptMsg">解析失败抛出异常时的附加信息</param>
        /// <param name="expected">期望返回的节点类型,如果类型不匹配将返回异常信息</param>
        /// <returns></returns>
        T ReadNumber<T>(GetNumberStyles getNumStyles, TryParseNumber<T> tryParse, string exceptMsg, params AtomElementType[] expected)
        {
            string str;
            AtomElementType actual = AssertReadNextAtomElement(exceptMsg, out str, expected);
            NumberStyles numStyles = getNumStyles(str, expected, actual);
            T ret;
            if (!tryParse(str, numStyles, CultureInfo.InvariantCulture, out ret))
            {
                throw new JsonReaderAssertException(line, column, expected, actual, exceptMsg);
            }
            return ret;
        }
        NumberStyles GetExponentOrNotStyle(string str, AtomElementType[] expected, AtomElementType actual)
        {
            return NumberStyles.AllowLeadingSign |
                NumberStyles.AllowDecimalPoint |
                (actual == AtomElementType.NUMBER_EXP || actual == AtomElementType.FLOAT_EXP ? NumberStyles.AllowExponent : NumberStyles.None);
        }


        static readonly string Int16AssertMsg = string.Format("期望是在{0}和{1}之间的数字", Int16.MinValue, Int16.MaxValue);
        /// <summary>
        /// 从当前流位置读取一个16位长度的整型数字
        /// </summary>
        /// <returns></returns>
        public override short ReadInt16()
        {
            return ReadNumber<short>(GetExponentOrNotStyle, short.TryParse, Int16AssertMsg, INTEGER_TYPES);
        }
        static readonly string Int32AssertMsg = string.Format("期望是在{0}和{1}之间的数字", Int32.MinValue, Int32.MaxValue);
        /// <summary>
        /// 从当前流位置读取一个32位长度的整型数字
        /// </summary>
        /// <returns></returns>
        public override int ReadInt32()
        {
            return ReadNumber<int>(GetExponentOrNotStyle, int.TryParse, Int32AssertMsg, INTEGER_TYPES);
        }
        static readonly string Int64AssertMsg = string.Format("期望是在{0}和{1}之间的数字", Int32.MinValue, Int32.MaxValue);
        /// <summary>
        /// 从当前流位置读取一个64位长度的整型数字
        /// </summary>
        /// <returns></returns>
        public override long ReadInt64()
        {
            return ReadNumber<long>(GetExponentOrNotStyle, long.TryParse, Int64AssertMsg, INTEGER_TYPES);
        }
        static readonly string SingleAssertMsg = string.Format("期望是在{0}和{1}之间的单精度浮点数", Single.MinValue, Single.MaxValue);
        /// <summary>
        /// 从当前流位置读取一个单精度浮点数
        /// </summary>
        /// <returns></returns>
        public override float ReadSingle()
        {
            return ReadNumber<float>(GetExponentOrNotStyle, float.TryParse, SingleAssertMsg, NUMBER_TYPES);
        }
        static readonly string DoubleAssertMsg = string.Format("期望是在{0}和{1}之间的双精度浮点数", Double.MinValue, Double.MaxValue);
        /// <summary>
        /// 从当前流位置读取一个双精度浮点数
        /// </summary>
        /// <returns></returns>
        public override double ReadDouble()
        {
            return ReadNumber<double>(GetExponentOrNotStyle, double.TryParse, DoubleAssertMsg, NUMBER_TYPES);
        }
        static readonly string DecimalAssertMsg = string.Format("期望是在{0}的{1}之间的十进制数", Decimal.MinValue, Decimal.MaxValue);
        /// <summary>
        /// 从当前流位置读取一个十进制数
        /// </summary>
        /// <returns></returns>
        public override decimal ReadDecimal()
        {
            return ReadNumber<decimal>(GetExponentOrNotStyle, decimal.TryParse, DecimalAssertMsg, NUMBER_TYPES);
        }
        #endregion

        #region 字符串
        /// <summary>
        /// 从当前流位置读取一个字符串
        /// </summary>
        /// <returns></returns>
        public override string ReadString()
        {
            string ret;
            if (AtomElementType.NULL == AssertReadNextAtomElement("期望字符串值", out ret, AtomElementType.STRING, AtomElementType.NULL))
            {
                return null;
            }
            return ret;
        }
        static readonly string CharAssertMsg = "期望字符字符串或数字,将转换成字符";
        /// <summary>
        /// 从当前流位置读取一个字符,如果读到的是字符串,将取第一个字符;如果读到的是数字,将作为Unicode字符处理;或者读到null
        /// </summary>
        /// <returns></returns>
        public override char ReadChar()
        {
            string str;
            AtomElementType t = AssertReadNextAtomElement(CharAssertMsg, out str, AtomElementType.NULL, AtomElementType.STRING, AtomElementType.NUMBER);
            switch (t)
            {
                case AtomElementType.NULL:
                case AtomElementType.STRING:
                    if (str != null && str.Length > 0)
                    {
                        return str[0];
                    }
                    else
                    {
                        return '\0';
                    }
                case AtomElementType.NUMBER:
                    int n;
                    if (Int32.TryParse(str, out n))
                    {
                        return (char)n;
                    }
                    else
                    {
                        throw new JsonReaderAssertException(line, column, new AtomElementType[] { AtomElementType.STRING, AtomElementType.NUMBER }, t, "期望的字符Unicode代码超出预期,实际是:" + str);
                    }

                default:
                    goto case AtomElementType.NUMBER; //实际执行不到
            }
        }
        /// <summary>
        /// 从当前流位置读取字符数组
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public override char[] ReadChars(int count)
        {
            // TODO 未实现
            return base.ReadChars(count);
        }
        #endregion

        #region 枚举类型
        /// <summary>
        /// 从当前流位置读取一个枚举类型
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override bool ReadEnumerable(Type type, ref object value)
        {
            AssertReadNextAtomElement("期望是数组声明开始符号[", AtomElementType.SQUARED_OPEN);
            return base.ReadEnumerable(type, ref value);
            // TODO 已读到]之后 是否需要做什么处理?
        }
        //public override bool ReadEnumerable(Type type, ref object value, ReadObjectCallback callback)
        //{
        //    return base.ReadEnumerable(type, ref value, callback);
        //}
        //public override bool ReadEnumerable(Type type, Type elementType, ref object value, ReadObjectCallback callback)
        //{
        //    return base.ReadEnumerable(type, elementType, ref value, callback);
        //}
        /// <summary>
        /// 从当前流位置读取枚举项目
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="index"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        protected override bool OnReadItem(Type type, ref object value, int index, ReadObjectCallback callback)
        {
            if (index > 0)
            {
                if (AtomElementType.SQUARED_CLOSE == AssertReadNextAtomElement("期望是数组元素分割符号,", AtomElementType.COMMA, AtomElementType.SQUARED_CLOSE))
                {
                    // TODO 已读到]之后
                    return false;
                }
            }
            return base.OnReadItem(type, ref value, index, callback);
        }
        #endregion
        #region 字典
        /// <summary>
        /// 从当前流位置读取一个字典类型
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override bool ReadDictionary(Type type, ref object value)
        {
            // TODO 未实现
            return base.ReadDictionary(type, ref value);
        }
        //public override bool ReadDictionary(Type type, ref object value, ReadObjectCallback callback)
        //{
        //    return base.ReadDictionary(type, ref value, callback);
        //}
        //protected override IEnumerable<System.Collections.DictionaryEntry> ReadDictionary(Type keyType, Type valueType, int count, ReadObjectCallback callback)
        //{
        //    return base.ReadDictionary(keyType, valueType, count, callback);
        //}
        /// <summary>
        /// 从当前流位置读取一个字典项
        /// </summary>
        /// <param name="keyType"></param>
        /// <param name="valueType"></param>
        /// <param name="value"></param>
        /// <param name="index"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        protected override bool OnReadDictionaryEntry(Type keyType, Type valueType, ref System.Collections.DictionaryEntry value, int index, ReadObjectCallback callback)
        {
            // TODO 未实现
            return base.OnReadDictionaryEntry(keyType, valueType, ref value, index, callback);
        }

        #endregion

        #region 读取json的原子操作
        /// <summary>
        /// 断言读取下一个原子元素,返回实际读到的原子元素类型,一般用于断言{}[]:,
        /// 
        /// 要得到具体读取到的值应使用另外一个重载
        /// </summary>
        /// <param name="msg">断言失败时的附加异常信息</param>
        /// <param name="expected">期望的原子元素类型</param>
        /// <exception cref="JsonReaderAssertException">如果断言失败</exception>
        /// <returns></returns>
        AtomElementType AssertReadNextAtomElement(string msg, params AtomElementType[] expected)
        {
            string s;
            return AssertReadNextAtomElement(msg, out s, expected);
        }
        /// <summary>
        /// 断言读取下一个原子元素,返回实际读到的原子元素类型
        /// 
        /// </summary>
        /// <param name="msg">断言失败时的附加异常信息</param>
        /// <param name="str">实际读到的内容,字面值是直接的字符串,字符串类型也是实际的字符串(不包括字符串头尾的双引号)</param>
        /// <param name="expected">期望的原子元素类型</param>
        /// <exception cref="JsonReaderAssertException">如果断言失败</exception>
        /// <returns></returns>
        AtomElementType AssertReadNextAtomElement(string msg, out string str, params AtomElementType[] expected)
        {
            AtomElementType t = ReadNextAtomElement(out str);
            if (Array.IndexOf(expected, t) == -1)
            {
                throw new JsonReaderAssertException(line, column, expected, t, msg);
            }
            return t;
        }

        /// <summary>
        /// 读取下一个原子元素,非{} []这类复合元素
        /// </summary>
        /// <returns></returns>
        AtomElementType ReadNextAtomElement(out string str)
        {
            str = null;
            while (true)
            {
                int c = Reader.Peek();
                column++;
                switch (c)
                {
                    case -1:
                        Reader.Read();
                        return AtomElementType.NONE;
                    case ' ':
                    case '\t':
                        Reader.Read();
                        continue;
                    case '\r':
                    case '\n':
                        Reader.Read();
                        if (c == '\r' && Reader.Peek() == '\n')
                        {
                            Reader.Read();
                        }
                        column = 1;
                        line++;
                        continue;
                    case '{':
                        Reader.Read();
                        return AtomElementType.CURLY_OPEN;
                    case '}':
                        Reader.Read();
                        return AtomElementType.CURLY_CLOSE;
                    case '[':
                        Reader.Read();
                        return AtomElementType.SQUARED_OPEN;
                    case ']':
                        Reader.Read();
                        return AtomElementType.SQUARED_CLOSE;
                    case ':':
                        Reader.Read();
                        return AtomElementType.COLON;
                    case ',':
                        Reader.Read();
                        return AtomElementType.COMMA;
                    case '"':
                        Reader.Read();
                        AtomElementType sret = ReadNextString(out str);
                        return sret;
                    default:
                        column--; //因为没有调用Read() 所以实际的列应该-1
                        AtomElementType lret = ReadNextLiteral(out str);
                        return lret;
                }
            }
        }
        /// <summary>
        /// 读取下一个字符串,当前reader流已经在"之后,读取到的字符串应该是不包含结尾的双引号
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        AtomElementType ReadNextString(out string str)
        {
            StringBuilder sb = new StringBuilder();
            bool isContinue = true;
            int c = 0;
            while (isContinue)
            {
                c = Reader.Peek();
                column++;
                switch (c)
                {
                    case '"':
                        Reader.Read();
                        isContinue = false;
                        break;
                    case '\\':
                        Reader.Read();
                        sb.Append(ReadNextEscapeChar());
                        break;
                    case '\b':
                        Reader.Read();
                        sb.Append('\b');
                        break;
                    case '\f':
                        Reader.Read();
                        sb.Append('\f');
                        break;
                    case '\t':
                        Reader.Read();
                        sb.Append('\t');
                        break;
                    default:
                        if (c < 32)
                        {
                            column--;
                            throw new JsonReaderParseException(line, column, "字符串未正确的结束");
                        }
                        Reader.Read();
                        sb.Append((char)c);
                        break;
                }
            }
            str = sb.ToString();
            return AtomElementType.STRING;
        }
        /// <summary>
        /// 读取下一个转义字符,流已处于转义符\后
        /// </summary>
        /// <returns></returns>
        string ReadNextEscapeChar()
        {
            int c = Reader.Read();
            column++;
            switch (c)
            {
                case 'b':
                    return "\b";
                case 'f':
                    return "\f";
                case 'n':
                    return "\n";
                case 'r':
                    return "\r";
                case 't':
                    return "\t";
                case 'u':
                    char[] unicodeChar = new char[4];
                    int n = Reader.ReadBlock(unicodeChar, 0, 4);
                    column += n;
                    if (n != 4)
                    {
                        throw new JsonReaderParseException(line, column, "Unicode转义字符长度应该是4");
                    }
                    string str = new string(unicodeChar);
                    UInt16 charCode;
                    if (UInt16.TryParse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out charCode))
                    {
                        return "" + (char)charCode;
                    }
                    return "u" + str;//无法识别的将作为原始字符串输出
                default:
                    return "" + (char)c;
            }
        }
        /// <summary>
        /// 读取下一个字面值,可能是true false null 数字 无法识别,调用时第一个字符一定是一个字面值
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        AtomElementType ReadNextLiteral(out string str)
        {
            StringBuilder sb = new StringBuilder();
            bool isContinue = true;
            int c = 0;
            bool hasDigit = false, hasLiteral = false, hasDot = false, hasExp = false;
            int lastChar = -1;
            while (isContinue)
            {
                c = Reader.Peek();
                column++;
                switch (c)
                {
                    case '-':
                    case '+':
                        // json.org中规定-能在第一位和e符号后出现,而+仅仅只能在e符号后出现,这里忽略了这样的差异,允许+出现在第一位
                        Reader.Read();
                        sb.Append((char)c);
                        if (sb.Length == 1 || //第一个字符
                            (sb.Length > 2 && hasDigit && !hasLiteral && hasExp && (lastChar == 'e' || lastChar == 'E')) //科学计数法e符号后
                            )
                        {
                            hasDigit = true; //作为数字
                        }
                        else
                        {
                            hasLiteral = true;
                        }
                        break;
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9': //数字
                        Reader.Read();
                        sb.Append((char)c);
                        hasDigit = true;
                        break;
                    case '.': //浮点数
                        Reader.Read();
                        sb.Append((char)c);
                        if (!hasDot) //仅出现一次的.符号
                        {
                            hasDot = true;
                        }
                        else
                        {
                            hasLiteral = true;
                        }
                        break;
                    case 'e':
                    case 'E': //科学计数法的e符号
                        Reader.Read();
                        sb.Append((char)c);
                        if (!hasExp) //仅出现一次的e符号
                        {
                            hasExp = true;
                        }
                        else
                        {
                            hasLiteral = true;
                        }
                        break;
                    default:
                        if (c < 32 || " ,{}[]:".IndexOf((char)c) != -1) //结束符号
                        {
                            isContinue = false;
                            column--;
                        }
                        else //其它符号
                        {
                            Reader.Read();
                            sb.Append((char)c);
                            hasLiteral = true;
                        }
                        break;
                }
                lastChar = c;
            }
            str = sb.ToString();

            if (hasDigit && !hasDot && !hasLiteral)
            {
                return hasExp ? AtomElementType.NUMBER_EXP : AtomElementType.NUMBER;
            }
            else if (hasDigit && hasDot && !hasLiteral)
            {
                return hasExp ? AtomElementType.FLOAT_EXP : AtomElementType.FLOAT;
            }
            else
            {
                if (!hasDigit && "true".Equals(str, StringComparison.OrdinalIgnoreCase))
                {
                    return AtomElementType.TRUE;
                }
                else if (!hasDigit && "false".Equals(str, StringComparison.OrdinalIgnoreCase))
                {
                    return AtomElementType.FALSE;
                }
                else if (!hasDigit && "null".Equals(str, StringComparison.OrdinalIgnoreCase))
                {
                    return AtomElementType.NULL;
                }
                else
                {
                    return AtomElementType.LITERAL;
                }
            }
        }
        #endregion

        #region 读取对象
        /// <summary>
        /// 从当前流位置读取一个对象
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        protected override bool OnReadObject(Type type, ref object value, ReadObjectCallback callback)
        {
            return base.OnReadObject(type, ref value, callback);
        }
        /// <summary>
        /// 从当前流位置读取一个自定义对象,即{}包括的数据
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public override bool ReadCustomObject(Type type, ref object value, ReadObjectCallback callback)
        {
            AssertReadNextAtomElement("期望对象开始", AtomElementType.CURLY_OPEN);
            bool ret = base.ReadCustomObject(type, ref value, callback);
            // TODO json成员比数量比类成员数量多(ReadCustomObject中的循环提前结束了)或者少(ReadCustomObject中的循环提前读取到}符号了) 正好(未读到} 正好下一个是})
            return ret;
        }
        /// <summary>
        /// 读取当前成员名称
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="members"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        protected override IObjectMemberInfo GetMemberBeforeRead(Type type, object value, IObjectMemberInfo[] members, int index)
        {
            IObjectMemberInfo ret = null;
            AtomElementType atype;
            string name;
            while (true)
            {
                atype = AssertReadNextAtomElement("期望成员名称或者逗号分割符", out name, AtomElementType.COMMA, AtomElementType.STRING, AtomElementType.CURLY_CLOSE);
                if (atype == AtomElementType.COMMA)
                {
                    atype = AssertReadNextAtomElement("期望成员名称", out name, AtomElementType.STRING);
                }
                else if (atype == AtomElementType.CURLY_CLOSE)
                {
                    return null; //提前结束
                }
                AssertReadNextAtomElement("期望成员名值分割符:冒号", AtomElementType.COLON);
                ret = GetMemberByName(members, name);
                if (ret != null)
                {
                    break;
                }
                SkipNextValue();
            }
            return ret;
        }
        /// <summary>
        /// 从当前流位置读取成员值
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="member"></param>
        /// <param name="index"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        protected override bool OnReadMember(Type type, ref object value, IObjectMemberInfo member, int index, ReadObjectCallback callback)
        {
            return base.OnReadMember(type, ref value, member, index, callback);
        }
        /// <summary>
        /// 跳过下一个值,可以是跳过对象声明(以及对象成员名称 成员值声明),数组声明,以及基础类型
        /// </summary>
        void SkipNextValue()
        {
            // TODO 考虑skipDepth可能初始值不为0时的需求,自定义对象未结束
            int skipDepth = 0;
            string s;
            do
            {
                switch (ReadNextAtomElement(out s))
                {
                    case AtomElementType.NONE:
                        skipDepth = 0;//直接跳出
                        break;
                    case AtomElementType.CURLY_OPEN:
                        skipDepth++;
                        break;
                    case AtomElementType.CURLY_CLOSE:
                        skipDepth--;
                        break;
                    case AtomElementType.SQUARED_OPEN:
                        skipDepth++;
                        break;
                    case AtomElementType.SQUARED_CLOSE:
                        skipDepth--;
                        break;
                    default:
                        break;
                }
            } while (skipDepth > 0);
        }
        #endregion

        /// <summary>
        /// 原子元素类型
        /// </summary>
        public enum AtomElementType
        {
            /// <summary>
            /// 无 一般表示结尾
            /// </summary>
            NONE,
            /// <summary>
            /// 大括号开始 {
            /// </summary>
            CURLY_OPEN,
            /// <summary>
            /// 大括号结束 }
            /// </summary>
            CURLY_CLOSE,
            /// <summary>
            /// 方括号开始 [
            /// </summary>
            SQUARED_OPEN,
            /// <summary>
            /// 方括号结束 ]
            /// </summary>
            SQUARED_CLOSE,
            /// <summary>
            /// 冒号 :
            /// </summary>
            COLON,
            /// <summary>
            /// 逗号 ,
            /// </summary>
            COMMA,
            /// <summary>
            /// 字符串 "包含的
            /// </summary>
            STRING,
            #region 字面值部分
            /// <summary>
            /// 字面值 无法识别的字面值
            /// </summary>
            LITERAL,
            /// <summary>
            /// 字面值 true
            /// </summary>
            TRUE,
            /// <summary>
            /// 字面值 false
            /// </summary>
            FALSE,
            /// <summary>
            /// 字面值 null
            /// </summary>
            NULL,
            /// <summary>
            /// 字面值 数字,非科学计数法表示的
            /// </summary>
            NUMBER,
            /// <summary>
            /// 字面值 数字,科学计数发表示的
            /// </summary>
            NUMBER_EXP,
            /// <summary>
            /// 字面值 浮点数,非科学计数法表示的浮点数
            /// </summary>
            FLOAT,
            /// <summary>
            /// 字面值 浮点数,科学计数法表示的浮点数
            /// </summary>
            FLOAT_EXP
            #endregion
        }
        /// <summary>
        /// json reader解析异常,主要是信息格式不正确
        /// </summary>
        public class JsonReaderParseException : Exception
        {
            /// <summary>
            /// 构造一个解析异常
            /// </summary>
            /// <param name="line">行</param>
            /// <param name="column">列</param>
            /// <param name="message">额外的异常信息</param>
            public JsonReaderParseException(long line, long column, string message)
                : base(message)
            {
                this.Line = line;
                this.Column = column;
            }
            /// <summary>
            /// 解析异常的行
            /// </summary>
            public long Line { get; protected set; }
            /// <summary>
            /// 解析异常的列
            /// </summary>
            public long Column { get; protected set; }
            /// <summary>
            /// 解析异常的详细信息
            /// </summary>
            public override string Message
            {
                get
                {
                    return string.Format("在解析行{0}:字符{1}时发生了异常:{2}", Line, Column, base.Message);
                }
            }
        }
        /// <summary>
        /// json reader断言异常,属于解析异常的一部分,主要是提供的数据不符合约定
        /// </summary>
        public class JsonReaderAssertException : JsonReaderParseException
        {
            private string expectedMessage;
            /// <summary>
            /// 断言异常的额外异常信息
            /// </summary>
            public string MessageInfo { get; protected set; }
            /// <summary>
            /// 断言期望的元素类型
            /// </summary>
            public AtomElementType[] Expected { get; protected set; }
            /// <summary>
            /// 断言实际得到的类型,如果期望类型中包含这个类型,即表示错误是非元素基础类型错误,而是由于元素格式不符合理想,比如期望是日期时间格式的字符串
            /// </summary>
            public AtomElementType Actual { get; protected set; }
            /// <summary>
            /// 构造一个断言异常
            /// </summary>
            /// <param name="line"></param>
            /// <param name="column"></param>
            /// <param name="expected">期望的节点类型</param>
            /// <param name="actual">实际节点类型</param>
            /// <param name="messageInfo">额外的描述信息</param>
            public JsonReaderAssertException(long line, long column, AtomElementType[] expected, AtomElementType actual, string messageInfo)
                : base(line, column, null)
            {
                this.Expected = expected;
                this.expectedMessage = string.Join(",", Array.ConvertAll<AtomElementType, string>(expected, e => GetAtomElementTypeMessageString(e)));
                this.Actual = actual;
                this.MessageInfo = messageInfo;
            }
            /// <summary>
            /// 异常信息,包含额外信息
            /// </summary>
            public override string Message
            {
                get
                {
                    return string.Format("在行{0},字符{1}期望是{2} 实际是{3} 额外信息:{4}", Line, Column,
                        expectedMessage,
                        GetAtomElementTypeMessageString(Actual),
                        MessageInfo
                        );
                }
            }
            static string GetAtomElementTypeMessageString(AtomElementType t)
            {
                switch (t)
                {
                    case AtomElementType.NONE:
                        return "未知";
                    case AtomElementType.CURLY_OPEN:
                        return "{";
                    case AtomElementType.CURLY_CLOSE:
                        return "}";
                    case AtomElementType.SQUARED_OPEN:
                        return "[";
                    case AtomElementType.SQUARED_CLOSE:
                        return "]";
                    case AtomElementType.COLON:
                        return ":";
                    case AtomElementType.COMMA:
                        return ",";
                    case AtomElementType.STRING:
                        return "字符串";
                    case AtomElementType.LITERAL:
                        return "字面值";
                    case AtomElementType.TRUE:
                        return "true";
                    case AtomElementType.FALSE:
                        return "false";
                    case AtomElementType.NULL:
                        return "null";
                    case AtomElementType.NUMBER:
                        return "数字";
                    case AtomElementType.NUMBER_EXP:
                        return "数字(科学计数法)";
                    case AtomElementType.FLOAT:
                        return "浮点数";
                    case AtomElementType.FLOAT_EXP:
                        return "浮点数(科学计数法)";
                    default:
                        goto case AtomElementType.NONE;
                }

            }
        }

    }
};