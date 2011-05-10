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

        #region 字符串
        /// <summary>
        /// 从当前流中读取一个字符串。字符串有长度前缀，一次 7 位地被编码为整数。
        /// </summary>
        /// <returns></returns>
        public override string ReadString()
        {
            String str = Reader.ReadLine();
            WriteLog("ReadString", str);
            return str;
        }
        #endregion

        #region 读取json的原子操作
        #region 原子元素类型分类
        /// <summary>
        /// 成员值类型
        /// </summary>
        public static readonly AtomElementType[] MEMBERVALUE_TYPES = { AtomElementType.TRUE, AtomElementType.FALSE,
                                                                         AtomElementType.NUMBER,AtomElementType.NUMBER_EXP,
                                                                         AtomElementType.FLOAT,AtomElementType.FLOAT_EXP,
                                                                         AtomElementType.STRING,AtomElementType.NULL, 
                                                                         //对象类型以{开始,数组类型以[开始
                                                                         AtomElementType.CURLY_OPEN,AtomElementType.SQUARED_OPEN};
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
        /// 布尔型
        /// </summary>
        public static readonly AtomElementType[] BOOLEAN_TYPES = { AtomElementType.TRUE, AtomElementType.FALSE };
        #endregion
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

        protected override bool OnReadObject(Type type, ref object value, ReadObjectCallback callback)
        {
            return base.OnReadObject(type, ref value, callback);
        }
        public override bool ReadCustomObject(Type type, ref object value, ReadObjectCallback callback)
        {
            AssertReadNextAtomElement("期望对象开始", AtomElementType.CURLY_OPEN);
            bool ret = base.ReadCustomObject(type, ref value, callback);
            AssertReadNextAtomElement("期望对象结束", AtomElementType.CURLY_CLOSE);
            return ret;
        }
        protected override IObjectMemberInfo GetMemberBeforeRead(Type type, object value, IObjectMemberInfo[] members, int index)
        {
            IObjectMemberInfo ret = null;
            AtomElementType atype;
            string name;
            while (true)
            {
                atype = AssertReadNextAtomElement("期望成员名称", out name, AtomElementType.COMMA, AtomElementType.STRING);
                if (atype == AtomElementType.COMMA)
                {
                    atype = AssertReadNextAtomElement("期望成员名称", out name, AtomElementType.STRING);
                }
                ret = GetMemberByName(members, name);
                if (ret != null)
                {
                    break;
                }
                //SkipMemberValue(); // TODO 考虑如何简单实现跳过成员值
            }
            return ret;
        }
        protected override bool OnReadMember(Type type, ref object value, IObjectMemberInfo member, int index, ReadObjectCallback callback)
        {
            // TODO 待实现
            return base.OnReadMember(type, ref value, member, index, callback);
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
        /// json reader解析异常,用于在遇到无法处理时抛出异常
        /// </summary>
        public class JsonReaderParseException : Exception
        {
            public JsonReaderParseException(long line, long column, string message)
                : base(message)
            {
                this.Line = line;
                this.Column = column;
            }

            public long Line { get; protected set; }

            public long Column { get; protected set; }

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
            public string MessageInfo { get; protected set; }
            public AtomElementType[] Expected { get; protected set; }
            public AtomElementType Actual { get; protected set; }

            public JsonReaderAssertException(long line, long column, AtomElementType[] expected, AtomElementType actual, string messageInfo)
                : base(line, column, null)
            {
                this.Expected = expected;
                this.expectedMessage = string.Join(",", Array.ConvertAll<AtomElementType, string>(expected, e => GetAtomElementTypeMessageString(e)));
                this.Actual = actual;
                this.MessageInfo = messageInfo;
            }
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
}