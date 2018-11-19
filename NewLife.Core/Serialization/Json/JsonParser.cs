using System;
using System.Collections.Generic;
using System.Globalization;
using NewLife.Collections;

namespace NewLife.Serialization
{

    /// <summary>Json分析器</summary>
    public class JsonParser
    {
        #region 内部
        /// <summary>标识符</summary>
        enum Token
        {
            None = -1,

            /// <summary>左大括号</summary>
            Curly_Open,

            /// <summary>右大括号</summary>
            Curly_Close,

            /// <summary>左方括号</summary>
            Squared_Open,

            /// <summary>右方括号</summary>
            Squared_Close,

            /// <summary>冒号</summary>
            Colon,

            /// <summary>逗号</summary>
            Comma,

            /// <summary>字符串</summary>
            String,

            /// <summary>数字</summary>
            Number,

            /// <summary>布尔真</summary>
            True,

            /// <summary>布尔真</summary>
            False,

            /// <summary>空值</summary>
            Null
        }
        #endregion

        #region 属性
        readonly String _json;
        //readonly StringBuilder _builder = new StringBuilder();
        Token _Ahead = Token.None;
        Int32 index;
        #endregion

        /// <summary>实例化</summary>
        /// <param name="json"></param>
        public JsonParser(String json) => _json = json;

        /// <summary>解码</summary>
        /// <returns></returns>
        public Object Decode() => ParseValue();

        private Dictionary<String, Object> ParseObject()
        {
            var dic = new NullableDictionary<String, Object>(StringComparer.OrdinalIgnoreCase);

            SkipToken(); // {

            while (true)
            {
                var old = index;
                var token = LookAhead();
                switch (token)
                {

                    case Token.Comma:
                        SkipToken();
                        break;

                    case Token.Curly_Close:
                        SkipToken();
                        return dic;

                    default:
                        {
                            // 如果名称是数字，需要退回去
                            if (token == Token.Number) index = old;

                            // 名称
                            var name = ParseName();

                            // :
                            if (NextToken() != Token.Colon)
                            {
                                // "//"开头的是注释，跳过
                                if (name.TrimStart().StartsWith("//"))
                                {
                                    break;
                                }

                                throw new XException("在 {0} 后需要冒号", name);
                            }

                            // 值
                            dic[name] = ParseValue();
                        }
                        break;
                }
            }
        }

        private List<Object> ParseArray()
        {
            var arr = new List<Object>();
            SkipToken(); // [

            while (true)
            {
                switch (LookAhead())
                {
                    case Token.Comma:
                        SkipToken();
                        break;

                    case Token.Squared_Close:
                        SkipToken();
                        return arr;

                    default:
                        arr.Add(ParseValue());
                        break;
                }
            }
        }

        private Object ParseValue()
        {
            switch (LookAhead())
            {
                case Token.Number:
                    return ParseNumber();

                case Token.String:
                    var str = ParseString();
                    if (str.IsNullOrEmpty()) return str;

                    // 有可能是字符串或时间日期
                    if (str[0] == '/' && str[str.Length - 1] == '/' && str.StartsWithIgnoreCase("/Date(") && str.EndsWithIgnoreCase(")/"))
                    {
                        str = str.Substring(6, str.Length - 6 - 2);
                        return str.ToLong().ToDateTime();
                    }

                    return str;

                case Token.Curly_Open:
                    return ParseObject();

                case Token.Squared_Open:
                    return ParseArray();

                case Token.True:
                    SkipToken();
                    return true;

                case Token.False:
                    SkipToken();
                    return false;

                case Token.Null:
                    SkipToken();
                    return null;
            }

            throw new XException("在 {0} 的标识符无法识别", index);
        }

        private String ParseName()
        {
            SkipToken(); // "

            //_builder.Length = 0;
            var sb = Pool.StringBuilder.Get();

            var runIndex = -1;

            while (index < _json.Length)
            {
                var c = _json[index++];

                if (c == '"')
                {
                    if (runIndex != -1)
                    {
                        if (sb.Length == 0) return _json.Substring(runIndex, index - runIndex - 1);

                        sb.Append(_json, runIndex, index - runIndex - 1);
                    }
                    return sb.Put(true);
                }
                else if (c == ':')
                {
                    // 如果是没有双引号的名字，则退回一个字符
                    index--;

                    if (runIndex != -1)
                    {
                        if (sb.Length == 0) return _json.Substring(runIndex, index + 1 - runIndex - 1);

                        sb.Append(_json, runIndex, index + 1 - runIndex - 1);
                    }
                    return sb.Put(true);
                }

                if (c != '\\')
                {
                    if (runIndex == -1) runIndex = index - 1;

                    continue;
                }

                if (index == _json.Length) break;

                if (runIndex != -1)
                {
                    sb.Append(_json, runIndex, index - runIndex - 1);
                    runIndex = -1;
                }

                switch (_json[index++])
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        {
                            var remainingLength = _json.Length - index;
                            if (remainingLength < 4) break;

                            // 分析32位十六进制数字
                            var codePoint = ParseUnicode(_json[index], _json[index + 1], _json[index + 2], _json[index + 3]);
                            sb.Append((Char)codePoint);

                            index += 4;
                        }
                        break;
                }
            }

            throw new Exception("已到达字符串结尾");
        }

        private String ParseString()
        {
            SkipToken(); // "

            //_builder.Length = 0;
            var sb = Pool.StringBuilder.Get();

            var runIndex = -1;

            while (index < _json.Length)
            {
                var c = _json[index++];

                if (c == '"')
                {
                    if (runIndex != -1)
                    {
                        if (sb.Length == 0) return _json.Substring(runIndex, index - runIndex - 1);

                        sb.Append(_json, runIndex, index - runIndex - 1);
                    }
                    return sb.Put(true);
                }

                if (c != '\\')
                {
                    if (runIndex == -1) runIndex = index - 1;

                    continue;
                }

                if (index == _json.Length) break;

                if (runIndex != -1)
                {
                    sb.Append(_json, runIndex, index - runIndex - 1);
                    runIndex = -1;
                }

                switch (_json[index++])
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        {
                            var remainingLength = _json.Length - index;
                            if (remainingLength < 4) break;

                            // 分析32位十六进制数字
                            var codePoint = ParseUnicode(_json[index], _json[index + 1], _json[index + 2], _json[index + 3]);
                            sb.Append((Char)codePoint);

                            index += 4;
                        }
                        break;
                }
            }

            throw new Exception("已到达字符串结尾");
        }

        private UInt32 ParseSingleChar(Char c1, UInt32 multipliyer)
        {
            UInt32 p1 = 0;
            if (c1 >= '0' && c1 <= '9')
                p1 = (UInt32)(c1 - '0') * multipliyer;
            else if (c1 >= 'A' && c1 <= 'F')
                p1 = (UInt32)((c1 - 'A') + 10) * multipliyer;
            else if (c1 >= 'a' && c1 <= 'f')
                p1 = (UInt32)((c1 - 'a') + 10) * multipliyer;
            return p1;
        }

        private UInt32 ParseUnicode(Char c1, Char c2, Char c3, Char c4)
        {
            var p1 = ParseSingleChar(c1, 0x1000);
            var p2 = ParseSingleChar(c2, 0x100);
            var p3 = ParseSingleChar(c3, 0x10);
            var p4 = ParseSingleChar(c4, 1);

            return p1 + p2 + p3 + p4;
        }

        private Int64 CreateLong(String s)
        {
            Int64 num = 0;
            var neg = false;
            foreach (var cc in s)
            {
                if (cc == '-')
                    neg = true;
                else if (cc == '+')
                    neg = false;
                else
                {
                    num *= 10;
                    num += cc - '0';
                }
            }

            return neg ? -num : num;
        }

        private Object ParseNumber()
        {
            SkipToken();

            // 需要回滚1个位置，因为第一个数字也是Toekn，可能被跳过了
            var startIndex = index - 1;
            var dec = false;
            do
            {
                if (index == _json.Length)
                    break;
                var c = _json[index];

                if ((c >= '0' && c <= '9') || c == '.' || c == '-' || c == '+' || c == 'e' || c == 'E')
                {
                    if (c == '.' || c == 'e' || c == 'E')
                        dec = true;
                    if (++index == _json.Length) break;

                    continue;
                }
                break;
            } while (true);

            if (dec)
            {
                var s = _json.Substring(startIndex, index - startIndex);
                return Double.Parse(s, NumberFormatInfo.InvariantInfo);
            }

            return CreateLong(out var num, _json, startIndex, index - startIndex);
        }

        private Token LookAhead()
        {
            if (_Ahead != Token.None) return _Ahead;

            return _Ahead = NextTokenCore();
        }

        /// <summary>读取一个Token</summary>
        private void SkipToken() => _Ahead = Token.None;

        private Token NextToken()
        {
            var rs = _Ahead != Token.None ? _Ahead : NextTokenCore();

            _Ahead = Token.None;

            return rs;
        }

        private Token NextTokenCore()
        {
            Char ch;

            // 跳过空白符
            do
            {
                ch = _json[index];

                if (ch > ' ') break;
                if (ch != ' ' && ch != '\t' && ch != '\n' && ch != '\r') break;

            } while (++index < _json.Length);

            if (index == _json.Length) throw new Exception("已到达字符串结尾");

            ch = _json[index];

            index++;

            switch (ch)
            {
                case '{':
                    return Token.Curly_Open;

                case '}':
                    return Token.Curly_Close;

                case '[':
                    return Token.Squared_Open;

                case ']':
                    return Token.Squared_Close;

                case ',':
                    return Token.Comma;

                case '"':
                    return Token.String;

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
                case '-':
                case '+':
                case '.':
                    return Token.Number;

                case ':':
                    return Token.Colon;

                case 'f':
                    if (_json.Length - index >= 4 &&
                        _json[index + 0] == 'a' &&
                        _json[index + 1] == 'l' &&
                        _json[index + 2] == 's' &&
                        _json[index + 3] == 'e')
                    {
                        index += 4;
                        return Token.False;
                    }
                    break;

                case 't':
                    if (_json.Length - index >= 3 &&
                        _json[index + 0] == 'r' &&
                        _json[index + 1] == 'u' &&
                        _json[index + 2] == 'e')
                    {
                        index += 3;
                        return Token.True;
                    }
                    break;

                case 'n':
                    if (_json.Length - index >= 3 &&
                        _json[index + 0] == 'u' &&
                        _json[index + 1] == 'l' &&
                        _json[index + 2] == 'l')
                    {
                        index += 3;
                        return Token.Null;
                    }
                    break;

                // 默认是没有双引号的key
                default: index--; return Token.String;
            }
            throw new XException("无法在 {0} 找到Token", --index);
        }

        static Int64 CreateLong(out Int64 num, String s, Int32 index, Int32 count)
        {
            num = 0;
            var neg = false;
            for (var x = 0; x < count; x++, index++)
            {
                var cc = s[index];

                if (cc == '-')
                    neg = true;
                else if (cc == '+')
                    neg = false;
                else
                {
                    num *= 10;
                    num += cc - '0';
                }
            }
            if (neg) num = -num;

            return num;
        }
    }
}