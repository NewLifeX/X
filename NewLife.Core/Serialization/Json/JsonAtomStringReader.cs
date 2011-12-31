using System;
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
        /// 读取下一个原子元素
        /// </summary>
        /// <remarks>
        /// 一般情况下isDetect可以为false,如果需要探测下一个可读元素,则需要给isDetect参数为true
        ///
        /// </remarks>
        /// <param name="isDetect">为true表示探测,探测将不会真正读取数据,str也最多只有当前流位置的下一个元素,并且流位置不会后移</param>
        /// <param name="str">读取到的原始字符串</param>
        /// <returns></returns>
        public JsonAtomType Read(bool isDetect, out string str)
        {
            str = null;
            while (true)
            {
                int c = Reader.Peek();
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
                            str = "" + (char)c;
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

        private string ReadEscapeChar()
        {
            // TODO
            throw new Exception();
        }

        private JsonAtomType ReadLiteral(bool isDetect, out string str)
        {
            // TODO
            throw new Exception();
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