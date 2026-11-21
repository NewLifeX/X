using System.Buffers;
using System.Globalization;
using System.Text;
using NewLife.Collections;

namespace NewLife.Serialization;

/// <summary>普通 JSON 快速解析器</summary>
/// <remarks>
/// 只处理标准 JSON：对象、数组、字符串、数字、true/false/null。
/// 注释、未加引号键、尾随逗号、非标准转义等写法返回 <c>false</c>，由 <see cref="JsonParser"/> 回退到兼容解析器。
/// </remarks>
internal static class JsonParserFast
{
    /// <summary>尝试快速解析</summary>
    /// <param name="json">JSON 文本</param>
    /// <param name="value">解析结果</param>
    /// <returns>遇到非标准写法时返回 <c>false</c></returns>
    public static Boolean TryParse(String json, out Object? value)
    {
        value = null;
        if (json.IsNullOrEmpty()) return true;

        var parser = new Parser(json);
        return parser.TryParse(out value);
    }

    private ref struct Parser
    {
        private const Int32 MaxDepth = 64;

        private readonly String _text;
        private readonly ReadOnlySpan<Char> _json;
        private Int32 _index;

        public Parser(String text)
        {
            _text = text;
            _json = text.AsSpan();
            _index = 0;
        }

        public Boolean TryParse(out Object? value)
        {
            value = null;
            SkipWhiteSpace();
            if (_index >= _json.Length) return true;

            var ch = _json[_index];
            if (ch != '{' && ch != '[') return false;

            if (!TryParseValue(0, out value)) return false;

            SkipWhiteSpace();
            return _index == _json.Length;
        }

        private Boolean TryParseValue(Int32 depth, out Object? value)
        {
            value = null;
            if (depth > MaxDepth) return false;

            SkipWhiteSpace();
            if (_index >= _json.Length) return false;

            var ch = _json[_index];
            switch (ch)
            {
                case '{':
                    return TryParseObject(depth, out value);
                case '[':
                    return TryParseArray(depth, out value);
                case '"':
                    if (!TryParseString(out var str)) return false;

                    value = ConvertDate(str);
                    return true;
                case 't':
                    if (!Match("true")) return false;

                    value = true;
                    return true;
                case 'f':
                    if (!Match("false")) return false;

                    value = false;
                    return true;
                case 'n':
                    if (!Match("null")) return false;

                    value = null;
                    return true;
                default:
                    if (ch == '-' || (ch >= '0' && ch <= '9')) return TryParseNumber(out value);

                    return false;
            }
        }

        private Boolean TryParseObject(Int32 depth, out Object? value)
        {
            value = null;
            _index++; // {

            var dic = new NullableDictionary<String, Object?>(StringComparer.OrdinalIgnoreCase);

            SkipWhiteSpace();
            if (TryConsume('}'))
            {
                value = dic;
                return true;
            }

            while (true)
            {
                SkipWhiteSpace();
                if (_index >= _json.Length || _json[_index] != '"') return false;
                if (!TryParseString(out var name)) return false;

                SkipWhiteSpace();
                if (!TryConsume(':')) return false;
                if (!TryParseValue(depth + 1, out var item)) return false;

                dic[name] = item;

                SkipWhiteSpace();
                if (TryConsume('}'))
                {
                    value = dic;
                    return true;
                }
                if (!TryConsume(',')) return false;
            }
        }

        private Boolean TryParseArray(Int32 depth, out Object? value)
        {
            value = null;
            _index++; // [

            SkipWhiteSpace();
            if (TryConsume(']'))
            {
                value = new List<Object>();
                return true;
            }

            // 小数组先写入池化缓冲，按实际数量一次性创建 List；超过阈值后转普通 List，避免大数组反复租还
            var buffer = ArrayPool<Object>.Shared.Rent(8);
            var count = 0;
            List<Object>? list = null;
            try
            {
                while (true)
                {
                    if (!TryParseValue(depth + 1, out var item)) return false;

                    if (list != null)
                    {
                        list.Add(item!);
                    }
                    else if (count < buffer.Length)
                    {
                        buffer[count++] = item!;
                    }
                    else
                    {
                        // 超过 8 个元素，后续走 List 正常扩容，避免大数组多一次完整复制
                        list = new List<Object>(count);
                        for (var i = 0; i < count; i++) list.Add(buffer[i]);
                        list.Add(item!);

                        Array.Clear(buffer, 0, count);
                        count = 0;
                    }

                    SkipWhiteSpace();
                    if (TryConsume(']')) break;
                    if (!TryConsume(',')) return false;
                }

                if (list == null)
                {
                    list = new List<Object>(count);
                    for (var i = 0; i < count; i++) list.Add(buffer[i]);
                }

                value = list;
                return true;
            }
            finally
            {
                if (count > 0) Array.Clear(buffer, 0, count);
                ArrayPool<Object>.Shared.Return(buffer);
            }
        }

        private Boolean TryParseString(out String value)
        {
            value = String.Empty;
            _index++; // 起始引号

            var start = _index;

            // 普通 JSON 字符串通常很短，使用单趟扫描比 IndexOfAny 更稳定
            var i = start;
            while (i < _json.Length)
            {
                var ch = _json[i];
                if (ch == '"')
                {
                    value = _text.Substring(start, i - start);
                    _index = i + 1;
                    return true;
                }
                if (ch == '\\') break;

                i++;
            }
            if (i >= _json.Length) return false;

            // 有转义，走完整解码
            var sb = Pool.StringBuilder.Get();
            var runStart = start;
            _index = i;

            while (_index < _json.Length)
            {
                var ch = _json[_index++];
                if (ch == '"')
                {
                    if (_index - 1 > runStart) sb.Append(_text, runStart, _index - 1 - runStart);

                    value = sb.Return(true);
                    return true;
                }
                if (ch != '\\') continue;

                if (_index - 1 > runStart) sb.Append(_text, runStart, _index - 1 - runStart);

                if (_index >= _json.Length)
                {
                    Pool.StringBuilder.Return(sb);
                    return false;
                }

                var esc = _json[_index++];
                switch (esc)
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
                        if (_index + 4 > _json.Length || !TryParseHex4(_index, out var code))
                        {
                            Pool.StringBuilder.Return(sb);
                            return false;
                        }
                        sb.Append((Char)code);
                        _index += 4;
                        break;
                    default:
                        Pool.StringBuilder.Return(sb);
                        return false;
                }

                runStart = _index;
            }

            Pool.StringBuilder.Return(sb);
            return false;
        }

        private Boolean TryParseNumber(out Object? value)
        {
            value = null;
            var start = _index;
            var negative = false;
            if (_json[_index] == '-')
            {
                negative = true;
                _index++;
            }

            if (_index >= _json.Length || _json[_index] < '0' || _json[_index] > '9') return false;

            var num = 0L;
            while (_index < _json.Length)
            {
                var ch = _json[_index];
                if (ch < '0' || ch > '9') break;

                num = unchecked(num * 10 + (ch - '0'));
                _index++;
            }

            var dec = false;
            if (_index < _json.Length && _json[_index] == '.')
            {
                dec = true;
                _index++;
                if (_index >= _json.Length || _json[_index] < '0' || _json[_index] > '9') return false;

                while (_index < _json.Length && _json[_index] >= '0' && _json[_index] <= '9') _index++;
            }
            if (_index < _json.Length && (_json[_index] == 'e' || _json[_index] == 'E'))
            {
                dec = true;
                _index++;
                if (_index < _json.Length && (_json[_index] == '+' || _json[_index] == '-')) _index++;
                if (_index >= _json.Length || _json[_index] < '0' || _json[_index] > '9') return false;

                while (_index < _json.Length && _json[_index] >= '0' && _json[_index] <= '9') _index++;
            }

            if (_index < _json.Length && !IsValueDelimiter(_json[_index])) return false;

            if (dec)
            {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
                value = Double.Parse(_json.Slice(start, _index - start), NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.InvariantInfo);
#else
                value = Double.Parse(_text.Substring(start, _index - start), NumberFormatInfo.InvariantInfo);
#endif
                return true;
            }

            if (negative) num = -num;
            value = num < Int32.MaxValue && num > Int32.MinValue ? (Object)(Int32)num : num;
            return true;
        }

        private static Object ConvertDate(String str)
        {
            if (str.IsNullOrEmpty()) return str;

            if (str[0] == '/' && str[^1] == '/' && str.StartsWithIgnoreCase("/Date(") && str.EndsWithIgnoreCase(")/"))
            {
                var number = str.Substring(6, str.Length - 6 - 2);
                return number.ToLong().ToDateTime();
            }

            return str;
        }

        private Boolean Match(String literal)
        {
            if (_index + literal.Length > _json.Length) return false;

            for (var i = 0; i < literal.Length; i++)
            {
                if (_json[_index + i] != literal[i]) return false;
            }

            var end = _index + literal.Length;
            if (end < _json.Length && !IsValueDelimiter(_json[end])) return false;

            _index = end;
            return true;
        }

        private Boolean TryParseHex4(Int32 index, out Int32 value)
        {
            value = 0;
            for (var i = 0; i < 4; i++)
            {
                var ch = _json[index + i];
                Int32 digit;
                if (ch >= '0' && ch <= '9') digit = ch - '0';
                else if (ch >= 'a' && ch <= 'f') digit = ch - 'a' + 10;
                else if (ch >= 'A' && ch <= 'F') digit = ch - 'A' + 10;
                else return false;

                value = (value << 4) | digit;
            }

            return true;
        }

        private Boolean TryConsume(Char ch)
        {
            if (_index < _json.Length && _json[_index] == ch)
            {
                _index++;
                return true;
            }

            return false;
        }

        private void SkipWhiteSpace()
        {
            while (_index < _json.Length)
            {
                var ch = _json[_index];
                if (ch != ' ' && ch != '\t' && ch != '\r' && ch != '\n') return;

                _index++;
            }
        }

        private static Boolean IsValueDelimiter(Char ch) => ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n' || ch == ',' || ch == '}' || ch == ']';
    }
}
