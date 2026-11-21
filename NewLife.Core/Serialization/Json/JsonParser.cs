using System.Globalization;
using System.Text;
using NewLife.Collections;

namespace NewLife.Serialization;

/// <summary>Json分析器</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/json
/// </remarks>
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

        /// <summary>单行注释</summary>
        Comment,

        /// <summary>多行注释开始</summary>
        Comment_Open,

        /// <summary>多行注释结束</summary>
        Comment_Close,

        /// <summary>空值</summary>
        Null
    }
    #endregion

    #region 属性
    readonly String _json;
    Token _Ahead = Token.None;
    Int32 index;
    #endregion

    /// <summary>实例化</summary>
    /// <param name="json"></param>
    public JsonParser(String json) => _json = json;

    /// <summary>解码</summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static IDictionary<String, Object?>? Decode(String json)
    {
        var parser = new JsonParser(json);
        //try
        {
            return parser.Decode() as IDictionary<String, Object?>;
        }
        //catch (XException ex)
        //{
        //    throw new XException($"解析Json出错：{json}", ex);
        //}
    }

    /// <summary>解码</summary>
    /// <returns></returns>
    public Object? Decode()
    {
        if (_json.IsNullOrEmpty()) return null;

        // 普通 JSON 走快速路径；注释、未加引号键、尾随逗号等非标准写法回退到兼容解析器
        if (index == 0 && _Ahead == Token.None && JsonParserFast.TryParse(_json, out var value))
        {
            // 标记已消费，保持重复调用 Decode 的既有行为
            index = _json.Length;
            return value;
        }

        return DecodeSlow();
    }

    private Object? DecodeSlow()
    {
        // 找到第一个非空白字符
        var ch = _json[0];
        while (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r')
        {
            if (++index == _json.Length) return null;

            ch = _json[index];
        }

        if (ch != '{' && ch != '[')
        {
            var len = _json.Length;
            if (len > 128) len = 128;

            throw new XException("Non standard Json string [{0}]", _json.Substring(0, len));
        }

        return ParseValue();
    }

    private Dictionary<String, Object?> ParseObject()
    {
        var dic = new NullableDictionary<String, Object?>(StringComparer.OrdinalIgnoreCase);

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

                case Token.Comment:
                    ParseSingleComment();
                    SkipToken();
                    break;

                case Token.Comment_Open:
                    ParseComment();
                    SkipToken();
                    break;

                case Token.Comment_Close:
                    SkipToken();
                    break;

                default:
                    {
                        // 如果名称是数字，需要退回去
                        if (token == Token.Number) index = old;

                        // 名称
                        var name = ParseString(true);

                        // :
                        if (NextToken() != Token.Colon)
                        {
                            // "//"开头的是注释，跳过
                            if (name.TrimStart().StartsWith("//"))
                            {
                                break;
                            }

                            throw new XException("A colon is required after {0}", name);
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

                case Token.Comment:
                    ParseSingleComment();
                    SkipToken();
                    break;

                case Token.Comment_Open:
                    ParseComment();
                    SkipToken();
                    break;

                case Token.Comment_Close:
                    SkipToken();
                    break;

                default:
                    var v = ParseValue();
                    // 保留 null 元素，避免数组元素位置错位
                    arr.Add(v!);
                    break;
            }
        }
    }

    private Object? ParseValue()
    {
        switch (LookAhead())
        {
            case Token.Number:
                return ParseNumber();

            case Token.String:
                var str = ParseString(false);
                if (str.IsNullOrEmpty()) return str;

                // 有可能是字符串或时间日期
                if (str[0] == '/' && str[^1] == '/' && str.StartsWithIgnoreCase("/Date(") && str.EndsWithIgnoreCase(")/"))
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

        throw new XException("Unrecognized identifier in {0}", index);
    }

    private String ParseString(Boolean isName)
    {
        // 识别名称时，如果以双引号开头，则把冒号当作名称一部分
        if (isName && index > 0 && _json[index - 1] == '"') isName = false;

        SkipToken(); // "

        // 仅在遇到转义时才借用 StringBuilder，未转义字符串直接截取，避免每次取还池对象
        StringBuilder? sb = null;

        var runIndex = -1;

        while (index < _json.Length)
        {
            var c = _json[index++];

            if (c == '"')
            {
                if (runIndex != -1)
                {
                    if (sb == null) return _json.Substring(runIndex, index - runIndex - 1);

                    sb.Append(_json, runIndex, index - runIndex - 1);
                }
                return sb == null ? String.Empty : sb.Return(true);
            }
            else if (isName && c == ':')
            {
                // 如果是没有双引号的名字，则退回一个字符
                index--;

                if (runIndex != -1)
                {
                    if (sb == null) return _json.Substring(runIndex, index - runIndex);

                    sb.Append(_json, runIndex, index - runIndex);
                }
                return sb == null ? String.Empty : sb.Return(true);
            }

            if (c != '\\')
            {
                if (runIndex == -1) runIndex = index - 1;

                continue;
            }

            if (index == _json.Length) break;

            if (runIndex != -1)
            {
                sb ??= Pool.StringBuilder.Get();
                sb.Append(_json, runIndex, index - runIndex - 1);
                runIndex = -1;
            }
            else
            {
                sb ??= Pool.StringBuilder.Get();
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

        if (runIndex >= 0)
        {
            var len = index - runIndex;
            if (len > 32) len = 32;

            throw new XException("Reached the end of the string while parsing it [{0}]", _json.Substring(runIndex, len));
        }

        throw new XException("Reached the end of the string while parsing it");
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

    private Object ParseNumber()
    {
        SkipToken();

        // 需要回滚1个位置，因为第一个数字也是Toekn，可能被跳过了
        var startIndex = index - 1;
        var dec = false;
        var neg = false;
        var num = 0L;
        var length = _json.Length;
        var i = startIndex;

        // 首字符已被 NextTokenCore 消费；旧实现从第二个字符开始扫描，
        // 因此这里首字符不参与 dec 判断，保持 '.' 开头数字的既有行为
        if (i < length)
        {
            var first = _json[i++];
            if (first >= '0' && first <= '9') num = first - '0';
            else if (first == '-') neg = true;
            else if (first == '+') neg = false;
            else num = first - '0'; // 保持旧 CreateLong 对非法首字符（如 '.'）的既有处理
        }

        while (i < length)
        {
            var c = _json[i];
            if (c >= '0' && c <= '9')
            {
                num = unchecked(num * 10 + (c - '0'));
                i++;
                continue;
            }
            if (c == '.' || c == 'e' || c == 'E')
            {
                dec = true;
                i++;
                continue;
            }
            if (c == '-')
            {
                neg = true;
                i++;
                continue;
            }
            if (c == '+')
            {
                neg = false;
                i++;
                continue;
            }
            break;
        }
        index = i;

        if (dec)
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
            return Double.Parse(_json.AsSpan(startIndex, index - startIndex), NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.InvariantInfo);
#else
            var s = _json[startIndex..index];
            return Double.Parse(s, NumberFormatInfo.InvariantInfo);
#endif
        }

        if (neg) num = -num;
        if (num < Int32.MaxValue && num > Int32.MinValue) return (Int32)num;

        return num;
    }

    private void ParseSingleComment()
    {
        while (index < _json.Length)
        {
            var ch = _json[index++];
            if (ch == '\r' || ch == '\n')
            {
                index--;
                break;
            }
        }
    }

    private void ParseComment()
    {
        while (index < _json.Length)
        {
            var ch = _json[index++];
            if (ch == '*')
            {
                ch = _json[index];
                if (ch == '/')
                {
                    index--;
                    break;
                }
            }
        }
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

        if (index == _json.Length)
        {
            if (_json.Length >= 0)
            {
                var len = _json.Length;
                if (len > 32) len = 32;

                throw new XException("End of string reached while parsing token [{0}]", _json.Substring(_json.Length - len, len));
            }

            throw new XException("End of string reached while parsing token");
        }

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

            case '/':
                if (_json.Length - index >= 1 &&
                    _json[index + 0] == '/')
                {
                    index += 1;
                    return Token.Comment;
                }
                if (_json.Length - index >= 1 &&
                    _json[index + 0] == '*')
                {
                    index += 1;
                    return Token.Comment_Open;
                }
                break;

            case '*':
                if (_json.Length - index >= 1 &&
                    _json[index + 0] == '/')
                {
                    index += 1;
                    return Token.Comment_Close;
                }
                break;

            // 默认是没有双引号的key
            default:
                index--;
                return Token.String;
        }
        throw new XException("Unable to find Token at {0}", --index);
    }
}