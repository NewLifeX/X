using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

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
                        column = 0;
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
                        column += str.Length + 1;
                        return sret;
                    default:
                        AtomElementType lret = ReadNextLiteral(out str);
                        column += str.Length;
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
            throw new NotImplementedException(); // TODO 读取字符串 处理字符串中的\开头的字符,包括\u,遇到单独的"时字符串结束,其它情况下遇到
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
            bool hasDigit = false, hasLiteral = false;
            while (isContinue)
            {
                c = Reader.Peek();
                switch (c)
                {
                    case -1:
                    case ' ':
                    case ',':
                    case '\t':
                    case ':':
                    case '{':
                    case '}':
                    case '[':
                    case ']':
                    case '\r':
                    case '\n':
                        isContinue = false;//避免将流位置移动到\r之后,使行列号计算始终在ReadNextAtomElement中
                        break;
                    case '-':
                    case '+':
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case '.':
                    case 'e':
                    case 'E':
                        hasDigit = true;
                        sb.Append((char)c);
                        Reader.Read();
                        break;
                    default:
                        hasLiteral = true;
                        sb.Append((char)c);
                        Reader.Read();
                        break;
                }
            }
            str = sb.ToString();
            if (hasDigit && !hasLiteral)
            {
                // TODO 整型 浮点型解析

                hasLiteral = true;// TODO 无法解析到有效的数字,作为字面字符
            }
            
            if (hasLiteral)
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
            return AtomElementType.LITERAL;
        }
        #endregion

        #region 读取对象

        public override bool ReadCustomObject(Type type, ref object value, ReadObjectCallback callback)
        {
            // TODO 待实现 将读取流位置移动到{之后
            return base.ReadCustomObject(type, ref value, callback);
        }
        protected override bool OnReadObject(Type type, ref object value, ReadObjectCallback callback)
        {
            // TODO 根据请求的类型和读取到的内容分发处理读取到的数据



            return base.OnReadObject(type, ref value, callback);
        }
        #endregion
        /// <summary>
        /// 原子元素类型
        /// </summary>
        enum AtomElementType
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
            /// <summary>
            /// 字面值 无法识别的字面值
            /// </summary>
            LITERAL,
            /// <summary>
            /// 字面值true
            /// </summary>
            TRUE,
            /// <summary>
            /// 字面值false
            /// </summary>
            FALSE,
            /// <summary>
            /// 字面值null
            /// </summary>
            NULL,
            /// <summary>
            /// 字面值整型数字
            /// </summary>
            INTEGER,
            /// <summary>
            /// 字面值浮点数字
            /// </summary>
            FLOAT
        }
    }
}