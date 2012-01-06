using System;
using System.Globalization;
using System.IO;
using System.Text;
using NewLife.Exceptions;

namespace NewLife.Serialization
{
    /// <summary>
    /// Json 原子元素读取器
    /// </summary>
    /// <remarks>
    /// 原子元素是指在Json中不可嵌套的元素,包括
    ///   基础类型: 数字(整型,浮点型) 字符串 null true/false
    ///   复合类型起始符号: {} []
    ///   分割符号: , :
    ///   结束符
    ///   非法符号:无法识别的字面值
    ///
    /// 这个类不检查Json复合格式是否有误,所以可以用来解析类似Json格式的字符串
    /// </remarks>
    public class JsonAtomStringReader
    {
        /// <summary>
        /// 所有的数字类型,包括整数和浮点数
        /// </summary>
        public static readonly JsonAtomType[] NUMBER_TYPES = { JsonAtomType.NUMBER, JsonAtomType.NUMBER_EXP, JsonAtomType.FLOAT, JsonAtomType.FLOAT_EXP };
        /// <summary>
        /// 所有的整数类型
        /// </summary>
        public static readonly JsonAtomType[] INTEGER_TYPES = { JsonAtomType.NUMBER, JsonAtomType.NUMBER_EXP };

        TextReader Reader;

        /// <summary>
        /// 当前读取到的行号,从1开始
        /// </summary>
        public long Line { get; protected set; }

        /// <summary>
        /// 当前读取到的列号,从1开始
        /// </summary>
        public long Column { get; protected set; }

        /// <summary>
        /// 是否允许单引号字符串,单引号字符串不符合JSON标准,默认false
        /// </summary>
        public bool SingleQuotesString { get; set; }

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="reader"></param>
        public JsonAtomStringReader(TextReader reader)
        {
            Line = Column = 1;
            Reader = reader;
        }

        /// <summary>
        /// 读取下一个原子元素,返回原子元素的类型,输出参数str表示读到的字符串
        /// </summary>
        /// <remarks>
        /// 一般情况下isDetect可以为false,如果需要探测下一个可读元素,则需要给isDetect参数为true
        /// </remarks>
        /// <param name="isDetect">为true表示探测,探测将只会读取一个字符,但不会移动流位置,直接重复探测将始终返回一样的结果</param>
        /// <param name="str">读取到的原始字符串</param>
        /// <returns></returns>
        public JsonAtomType Read(bool isDetect, out string str)
        {
            str = null;
            while (true)
            {
                int c = Reader.Peek();
                if (c != -1) str = "" + (char)c;
                switch (c)
                {
                    case -1:
                        if (!isDetect) MoveNext();
                        return JsonAtomType.NONE;
                    case '\t':
                        Column += 3; // 假定tab字符宽度是4
                        goto case ' ';
                    case ' ':
                        MoveNext();
                        continue;
                    case '\r':
                    case '\n':
                        MoveNext();
                        if (c == '\r' && Reader.Peek() == '\n') MoveNext();
                        Column = 1;
                        Line++;
                        continue;
                    case '{':
                        if (!isDetect) MoveNext();
                        return JsonAtomType.BRACE_OPEN;
                    case '}':
                        if (!isDetect) MoveNext();
                        return JsonAtomType.BRACE_CLOSE;
                    case '[':
                        if (!isDetect) MoveNext();
                        return JsonAtomType.BRACKET_OPEN;
                    case ']':
                        if (!isDetect) MoveNext();
                        return JsonAtomType.BRACKET_CLOSE;
                    case ':':
                        if (!isDetect) MoveNext();
                        return JsonAtomType.COLON;
                    case ',':
                        if (!isDetect) MoveNext();
                        return JsonAtomType.COMMA;
                    case '\'':
                        if (SingleQuotesString) goto case '"';
                        goto default;
                    case '"':
                        if (!isDetect) MoveNext();
                        else
                        {
                            return JsonAtomType.STRING;
                        }
                        return ReadString((char)c, out str);
                    default:
                        return ReadLiteral(isDetect, out str);
                }
            }
        }

        /// <summary>
        /// 将当前输入流位置向后移动一个字符,并返回读取到的字符
        /// </summary>
        /// <returns></returns>
        private int MoveNext()
        {
            Column++;
            return Reader.Read();
        }

        /// <summary>
        /// 读取字符串,流位置以处于"之后,读到的字符串不包含结尾的",但流会被移动到"之后
        /// </summary>
        /// <param name="quotesChar"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        private JsonAtomType ReadString(char quotesChar, out string str)
        {
            StringBuilder sb = new StringBuilder();
            int c = 0;
            while (true)
            {
                c = Reader.Peek();
                switch (c)
                {
                    case '"':
                        MoveNext();
                        goto EndOfString;
                    case '\\':
                        MoveNext();
                        sb.Append(ReadEscapeChar());
                        break;
                    case '\b':
                        MoveNext();
                        sb.Append('\b');
                        break;
                    case '\f':
                        MoveNext();
                        sb.Append('\f');
                        break;
                    case '\t':
                        MoveNext();
                        Column += 3;
                        sb.Append('\t');
                        break;
                    default:
                        if (c < 32)
                        {
                            throw new JsonReaderParseException(Line, Column, "字符串未正确的结束");
                        }
                        MoveNext();
                        sb.Append((char)c);
                        if (c > 2042) Column++; //宽字符
                        break;
                }
            }
        EndOfString:
            str = sb.ToString();
            return JsonAtomType.STRING;
        }

        /// <summary>
        /// 读取下一个转义字符,流已处于转义符\之后
        /// </summary>
        /// <returns></returns>
        private string ReadEscapeChar()
        {
            int c = MoveNext();
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
                    char[] uniChar = new char[4];
                    int n = Reader.ReadBlock(uniChar, 0, 4);
                    string str = new string(uniChar, 0, n);
                    if (n == 4)
                    {
                        UInt16 charCode;
                        if (UInt16.TryParse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out charCode))
                        {
                            Column += 4;
                            return "" + (char)charCode;
                        }
                    }
                    throw new JsonReaderParseException(Line, Column, "无效的Unicode字符转义\\u" + str);
                default:
                    if (c > 2042) Column++; // 宽字符
                    return "" + (char)c;
            }
        }

        /// <summary>
        /// 读取下一个字面值,可能是true false null 数字 无法识别
        ///
        /// isDetect为true时将不确保实际结果是返回的类型,因为仅仅预读一个字符无法确定上述字面值
        /// </summary>
        /// <param name="isDetect">为true表示探测,探测将只会读取一个字符,但不会移动流位置</param>
        /// <param name="str"></param>
        /// <returns></returns>
        private JsonAtomType ReadLiteral(bool isDetect, out string str)
        {
            StringBuilder sb = new StringBuilder();
            bool hasDigit = false, // 是否有数字
                hasLiteral = false, // 是否有字面值 即无法识别的
                hasDot = false, // 是否有小数点
                hasExp = false, // 是否有科学计数法的e E符号
                hasMinusExp = false; // 科学计数法的e符号后跟随的是否是减号
            int c = 0, lastChar = -1;
            while (true)
            {
                c = Reader.Peek();
                switch (c)
                {
                    case '-':
                    case '+':
                        if (!isDetect) MoveNext();
                        sb.Append((char)c);
                        // json标准中允许负号出现在第一位和e符号后,正号只允许出现在e符号后,这里忽略这样的差异,正号或负号都可以出现在第一位
                        if (sb.Length == 1 || // 第一个字符
                            (sb.Length > 2 && hasDigit && !hasLiteral && hasExp && (lastChar == 'e' || lastChar == 'E')) // 科学计数法e符号后的正负符号
                            )
                        {
                            if (sb.Length > 2 && c == '-' && (lastChar == 'e' || lastChar == 'E')) hasMinusExp = true;
                            hasDigit = true;
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
                    case '9': // 数字
                        if (!isDetect) MoveNext();
                        sb.Append((char)c);
                        hasDigit = true;
                        break;
                    case '.': // 浮点数
                        if (!isDetect) MoveNext();
                        sb.Append((char)c);
                        if (!hasDot)
                        {
                            hasDot = true;
                        }
                        else
                        {
                            hasLiteral = true;
                        }
                        break;
                    case 'e':
                    case 'E':
                        if (!isDetect) MoveNext();
                        sb.Append((char)c);
                        if (!hasExp)
                        {
                            hasExp = true;
                        }
                        else
                        {
                            hasLiteral = true;
                        }
                        break;
                    default:
                        if (c < 32 || " \t,{}[]:".IndexOf((char)c) != -1) // 结束符号
                        {
                            break;
                        }
                        else // 其它符号
                        {
                            if (!isDetect)
                            {
                                if (c > 2042) Column++; // 宽字符
                                MoveNext();
                            }
                            sb.Append((char)c);
                            hasLiteral = true;
                        }
                        break;
                }
                lastChar = c;
                if (isDetect) break;
            }
            str = sb.ToString();

            if (hasDigit && !hasDot && !hasLiteral && !hasMinusExp || isDetect && hasDigit)
            {
                return hasExp ? JsonAtomType.NUMBER_EXP : JsonAtomType.NUMBER;
            }
            else if (hasDigit && (hasDot || hasMinusExp) && !hasLiteral || isDetect && hasDot)
            {
                return hasExp ? JsonAtomType.FLOAT_EXP : JsonAtomType.FLOAT;
            }
            else
            {
                string lit = str.ToLower();
                if (!hasDigit && lit == "true" || isDetect && lit == "t")
                {
                    return JsonAtomType.TRUE;
                }
                else if (!hasDigit && lit == "false" || isDetect && lit == "f")
                {
                    return JsonAtomType.FALSE;
                }
                else if (!hasDigit && lit == "null" || isDetect && lit == "n")
                {
                    return JsonAtomType.NULL;
                }
                else
                {
                    return JsonAtomType.LITERAL;
                }
            }
        }

        /// <summary>
        /// 跳过下一个读到的值,包括复合格式{...} [...]
        /// </summary>
        /// <returns></returns>
        public JsonAtomStringReader Skip()
        {
            return Skip(0);
        }

        /// <summary>
        /// 跳过接下来读到的值,可指定要跳过的复合对象深度
        /// </summary>
        /// <remarks>
        /// 复合对象深度值是指当流位置和目标处于以下位置时
        ///
        ///   [[1,2/*当前流位置*/,3]]/*跳到的目标*/
        ///
        /// 调用Skip(1)将会将当前流位置移动到目标位置
        /// </remarks>
        /// <param name="initDepth">复合对象深度值,为0时表示不跳过复合对象,小于0时不做任何操作</param>
        /// <returns></returns>
        public JsonAtomStringReader Skip(int initDepth)
        {
            if (initDepth < 0) return this;
            int skipDepth = initDepth;
            string str;
            do
            {
                switch (Read(false, out str))
                {
                    case JsonAtomType.NONE:
                        skipDepth = 0; // 立即跳出
                        break;
                    case JsonAtomType.BRACE_OPEN:
                    case JsonAtomType.BRACKET_OPEN:
                        skipDepth++;
                        break;
                    case JsonAtomType.BRACE_CLOSE:
                    case JsonAtomType.BRACKET_CLOSE:
                        skipDepth--;
                        break;
                    default:
                        break;
                }
            } while (skipDepth > 0);
            return this;
        }
    }

    /// <summary>原子元素类型</summary>
    public enum JsonAtomType
    {
        /// <summary>
        /// 无 一般表示结尾
        /// </summary>
        NONE,
        /// <summary>
        /// 大括号开始 {
        /// </summary>
        BRACE_OPEN,
        /// <summary>
        /// 大括号结束 }
        /// </summary>
        BRACE_CLOSE,
        /// <summary>
        /// 方括号开始 [
        /// </summary>
        BRACKET_OPEN,
        /// <summary>
        /// 方括号结束 ]
        /// </summary>
        BRACKET_CLOSE,
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

    /// <summary>json reader解析异常,主要是信息格式不正确</summary>
    public class JsonReaderParseException : XException
    {
        /// <summary>构造一个解析异常</summary>
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
        public override string Message { get { return string.Format("在解析行{0}:字符{1}时发生了异常:{2}", Line, Column, base.Message); } }
    }
}