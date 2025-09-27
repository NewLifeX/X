using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Runtime.CompilerServices;
using NewLife.Collections;

namespace NewLife;

/// <summary>字符串助手类</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/string_helper
/// </remarks>
public static class StringHelper
{
    #region 字符串扩展
    /// <summary>忽略大小写的字符串相等比较，判断是否与任意一个待比较字符串相等</summary>
    /// <param name="value">字符串</param>
    /// <param name="strs">待比较字符串数组</param>
    /// <returns>若任一候选（忽略大小写）相等则返回 true</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Boolean EqualIgnoreCase(this String? value, params String?[] strs)
    {
        if (strs == null || strs.Length == 0) return false;
        foreach (var item in strs)
        {
            if (String.Equals(value, item, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    /// <summary>忽略大小写的字符串开始比较，判断是否与任意一个待比较字符串开始</summary>
    /// <param name="value">字符串</param>
    /// <param name="strs">待比较字符串数组</param>
    /// <returns>任一前缀匹配时 true</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Boolean StartsWithIgnoreCase(this String? value, params String?[] strs)
    {
        if (value.IsNullOrEmpty()) return false;
        if (strs == null || strs.Length == 0) return false;
        foreach (var item in strs)
        {
            if (!item.IsNullOrEmpty() && value.StartsWith(item, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    /// <summary>忽略大小写的字符串结束比较，判断是否以任意一个待比较字符串结束</summary>
    /// <param name="value">字符串</param>
    /// <param name="strs">待比较字符串数组</param>
    /// <returns>任一后缀匹配时 true</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Boolean EndsWithIgnoreCase(this String? value, params String?[] strs)
    {
        if (value.IsNullOrEmpty()) return false;
        if (strs == null || strs.Length == 0) return false;
        foreach (var item in strs)
        {
            if (!item.IsNullOrEmpty() && value.EndsWith(item, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    /// <summary>指示指定的字符串是 null 还是 String.Empty 字符串</summary>
    /// <param name="value">字符串</param>
    /// <returns>true 表示 null 或空串</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Boolean IsNullOrEmpty([NotNullWhen(false)] this String? value) => value == null || value.Length == 0;

    /// <summary>是否空或者空白字符串</summary>
    /// <param name="value">字符串</param>
    /// <returns>true 表示 null / 空 / 全空白</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Boolean IsNullOrWhiteSpace([NotNullWhen(false)] this String? value)
    {
        if (value != null)
        {
            for (var i = 0; i < value.Length; i++)
            {
                if (!Char.IsWhiteSpace(value[i])) return false;
            }
        }
        return true;
    }

    /// <summary>拆分字符串，过滤空格，无效时返回空数组</summary>
    /// <param name="value">字符串</param>
    /// <param name="separators">分组分隔符，默认逗号分号</param>
    /// <returns>分割后非空条目数组</returns>
    public static String[] Split(this String? value, params String[] separators)
    {
        //!! netcore3.0中新增Split(String? separator, StringSplitOptions options = StringSplitOptions.None)，优先于StringHelper扩展
        if (value.IsNullOrEmpty()) return [];
        if (separators == null || separators.Length == 0 || (separators.Length == 1 && separators[0].IsNullOrEmpty())) return [value];
        return value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>拆分字符串成为整型数组，默认逗号分号分隔，无效时返回空数组</summary>
    /// <remarks>过滤空格、过滤无效、不过滤重复</remarks>
    /// <param name="value">字符串</param>
    /// <param name="separators">分组分隔符，默认逗号分号</param>
    /// <returns>整型数组</returns>
    public static Int32[] SplitAsInt(this String? value, params String[] separators)
    {
        if (value.IsNullOrEmpty()) return [];
        if (separators == null || separators.Length == 0) separators = [",", ";"];

        var ss = value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        var list = new List<Int32>(ss.Length);
        foreach (var item in ss)
        {
            if (!Int32.TryParse(item.Trim(), out var id)) continue;

            // 本意只是拆分字符串然后转为数字，不应该过滤重复项
            //if (!list.Contains(id))
            list.Add(id);
        }

        return list.ToArray();
    }

    /// <summary>拆分字符串成为不区分大小写的可空名值字典。逗号分组，等号分隔</summary>
    /// <param name="value">字符串</param>
    /// <param name="nameValueSeparator">名值分隔符，默认等于号</param>
    /// <param name="separator">分组分隔符，默认分号</param>
    /// <param name="trimQuotation">是否去掉值两端引号</param>
    /// <returns>大小写不敏感字典</returns>
    public static IDictionary<String, String> SplitAsDictionary(this String? value, String nameValueSeparator = "=", String separator = ";", Boolean trimQuotation = false)
    {
        var dic = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        if (value.IsNullOrWhiteSpace()) return dic;

        if (nameValueSeparator.IsNullOrEmpty()) nameValueSeparator = "=";
        //if (separator == null || separator.Length <= 0) separator = new String[] { ",", ";" };

        var ss = value.Split([separator], StringSplitOptions.RemoveEmptyEntries);
        if (ss == null || ss.Length == 0) return dic;

        var k = 0;
        foreach (var item in ss)
        {
            // 如果分隔符是 \u0001，则必须使用Ordinal，否则无法分割直接返回0。在RocketMQ中有这种情况
            var p = item.IndexOf(nameValueSeparator, StringComparison.Ordinal);
            if (p <= 0)
            {
                dic[$"[{k}]"] = item; // 未包含名值分隔符，按序号占位
                k++;
                continue;
            }

            var key = item[..p].Trim();
            var val = item[(p + nameValueSeparator.Length)..].Trim();

            // 处理单引号双引号
            if (trimQuotation && !val.IsNullOrEmpty())
            {
                if (val[0] == '\'' && val[^1] == '\'') val = val.Trim('\'');
                if (val[0] == '"' && val[^1] == '"') val = val.Trim('"');
            }

            //k++;
            //dic[key] = val;
#if NETFRAMEWORK || NETSTANDARD2_0
            if (!dic.ContainsKey(key)) dic.Add(key, val);
#else
            dic.TryAdd(key, val);
#endif
        }

        return dic;
    }

    /// <summary>把一个列表组合成为一个字符串，默认逗号分隔</summary>
    /// <param name="value">序列</param>
    /// <param name="separator">组合分隔符，默认逗号</param>
    /// <returns>拼接后的字符串</returns>
    public static String Join(this IEnumerable value, String separator = ",")
    {
        var sb = Pool.StringBuilder.Get();
        if (value != null)
        {
            foreach (var item in value)
            {
                sb.Separate(separator).Append(item + "");
            }
        }
        return sb.Return(true);
    }

    /// <summary>把一个列表组合成为一个字符串，默认逗号分隔</summary>
    /// <param name="value">序列</param>
    /// <param name="separator">组合分隔符，默认逗号</param>
    /// <param name="func">对象转字符串委托，默认直接 ToString()</param>
    /// <returns>拼接后的字符串</returns>
    public static String Join<T>(this IEnumerable<T> value, String separator = ",", Func<T, Object?>? func = null)
    {
        var sb = Pool.StringBuilder.Get();
        if (value != null)
        {
            func ??= obj => obj;
            foreach (var item in value)
            {
                sb.Separate(separator).Append(func(item));
            }
        }
        return sb.Return(true);
    }

    /// <summary>追加分隔符字符串，忽略开头，常用于拼接</summary>
    /// <param name="sb">字符串构造者</param>
    /// <param name="separator">分隔符</param>
    /// <returns>同一个 <paramref name="sb"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringBuilder Separate(this StringBuilder sb, String separator)
    {
        if (/*sb == null ||*/ String.IsNullOrEmpty(separator)) return sb;

        if (sb.Length > 0) sb.Append(separator);

        return sb;
    }

    /// <summary>字符串转字节数组</summary>
    /// <param name="value">字符串</param>
    /// <param name="encoding">编码，默认utf-8无BOM</param>
    /// <returns>字节数组，空串返回空数组</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Byte[] GetBytes(this String? value, Encoding? encoding = null)
    {
        if (value.IsNullOrEmpty()) return [];
        encoding ??= Encoding.UTF8;
        return encoding.GetBytes(value);
    }

    /// <summary>格式化字符串。特别支持无格式化字符串的时间参数</summary>
    /// <param name="value">格式字符串</param>
    /// <param name="args">参数</param>
    /// <returns>格式化结果</returns>
    [Obsolete("建议使用插值字符串")]
    public static String F(this String value, params Object?[] args)
    {
        if (String.IsNullOrEmpty(value)) return value;

        // 特殊处理时间格式化。这些年，无数项目实施因为时间格式问题让人发狂
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] is DateTime dt)
            {
                // 没有写格式化字符串的时间参数，一律转为标准时间字符串
                if (value.Contains("{" + i + "}")) args[i] = dt.ToFullString();
            }
        }

        return String.Format(value, args);
    }

    /// <summary>指定输入是否匹配目标表达式，支持 * 和 ? 通配符</summary>
    /// <param name="pattern">匹配表达式。* 匹配任意长度（含0）任意字符，? 匹配任意单个字符</param>
    /// <param name="input">输入字符串</param>
    /// <param name="comparisonType">字符串比较方式</param>
    /// <returns>匹配结果</returns>
    /// <remarks>
    /// 采用单指针 + 回溯（记录最近一次 * 位置）线性匹配算法，时间复杂度 O(n) ~ O(n*m) 之间，
    /// 在常见场景较 Regex 具备更低开销。仅支持 * 与 ?，无需构造正则对象。
    /// </remarks>
    public static Boolean IsMatch(this String pattern, String input, StringComparison comparisonType = StringComparison.CurrentCulture)
    {
        if (pattern.IsNullOrEmpty()) return false;
        // 单独 * 匹配所有（含空串）
        if (pattern == "*") return true;
        if (input.IsNullOrEmpty()) return false;

        // 普通表达式，直接相等（避免进入通配逻辑）
        var hasStar = pattern.IndexOf('*') >= 0;
        var hasQm = pattern.IndexOf('?') >= 0;
        if (!hasStar && !hasQm) return String.Equals(input, pattern, comparisonType);

        // 通用通配符匹配（支持 * 和 ? ）
        var i = 0; // pattern 指针
        var j = 0; // input 指针
        var starIdx = -1; // 最近一次出现 *的位置
        var match = 0; // 当存在*时，记录在 input 中回溯匹配的起始位置

        while (j < input.Length)
        {
            if (i < pattern.Length && (pattern[i] == '?' || CharEquals(pattern[i], input[j], comparisonType)))
            {
                // 普通字符或?逐个前进
                i++;
                j++;
            }
            else if (i < pattern.Length && pattern[i] == '*')
            {
                // 记录*位置，先让*匹配空串，后续不匹配再回溯
                starIdx = i++;
                match = j; // 先假设 * 匹配空串
            }
            else if (starIdx != -1)
            {
                // 回溯：让之前的*多吞一个字符
                i = starIdx + 1;
                match++;
                j = match;
            }
            else
            {
                return false;
            }
        }

        // 处理结尾多余的*
        while (i < pattern.Length && pattern[i] == '*') i++;

        return i == pattern.Length;

        static Boolean CharEquals(Char a, Char b, StringComparison comparisonType)
        {
            if (a == b) return true;

            return comparisonType switch
            {
                StringComparison.Ordinal or StringComparison.CurrentCulture or StringComparison.InvariantCulture => false,
                StringComparison.OrdinalIgnoreCase => Char.ToUpperInvariant(a) == Char.ToUpperInvariant(b),
                StringComparison.CurrentCultureIgnoreCase => Char.ToUpper(a, System.Globalization.CultureInfo.CurrentCulture) == Char.ToUpper(b, System.Globalization.CultureInfo.CurrentCulture),
                StringComparison.InvariantCultureIgnoreCase => Char.ToUpper(a, System.Globalization.CultureInfo.InvariantCulture) == Char.ToUpper(b, System.Globalization.CultureInfo.InvariantCulture),
                _ => false,
            };
        }
    }

    //#if NETFRAMEWORK || NETSTANDARD2_0
    /// <summary>Returns a value indicating whether a specified character occurs within this string.</summary>
    /// <param name="value">字符串</param>
    /// <param name="inputChar">要查找的字符</param>
    /// <returns>找到返回 true</returns>
    public static Boolean Contains(this String value, Char inputChar) => value.IndexOf(inputChar) >= 0;

    /// <summary>Splits a string into substrings based on a single character separator.</summary>
    /// <param name="value">字符串</param>
    /// <param name="separator">分隔字符</param>
    /// <param name="options">是否移除空条目</param>
    /// <returns>结果数组</returns>
    public static String[] Split(this String value, Char separator, StringSplitOptions options = StringSplitOptions.None) => value.Split([separator], options);
    //#endif
    #endregion

    #region 截取扩展
    /// <summary>确保字符串以指定的另一字符串开始，不区分大小写</summary>
    /// <param name="str">字符串</param>
    /// <param name="start">期望前缀</param>
    /// <returns>处理后字符串</returns>
    public static String EnsureStart(this String? str, String start)
    {
        if (start.IsNullOrEmpty()) return str + "";
        if (str.IsNullOrEmpty()) return start + "";
        if (str.StartsWith(start, StringComparison.OrdinalIgnoreCase)) return str;

        return start + str;
    }

    /// <summary>确保字符串以指定的另一字符串结束，不区分大小写</summary>
    /// <param name="str">字符串</param>
    /// <param name="end">期望后缀</param>
    /// <returns>处理后字符串</returns>
    public static String EnsureEnd(this String? str, String end)
    {
        if (end.IsNullOrEmpty()) return str + "";
        if (str.IsNullOrEmpty()) return end + "";
        if (str.EndsWith(end, StringComparison.OrdinalIgnoreCase)) return str;

        return str + end;
    }

    /// <summary>从当前字符串开头移除另一字符串，不区分大小写，循环多次匹配前缀</summary>
    /// <param name="str">当前字符串</param>
    /// <param name="starts">前缀集合</param>
    /// <returns>移除后的字符串</returns>
    public static String TrimStart(this String str, params String[] starts)
    {
        if (str.IsNullOrEmpty()) return str;
        if (starts == null || starts.Length == 0 || starts[0].IsNullOrEmpty()) return str;
        for (var i = 0; i < starts.Length; i++)
        {
            if (str.StartsWith(starts[i], StringComparison.OrdinalIgnoreCase))
            {
                str = str[starts[i].Length..];
                if (String.IsNullOrEmpty(str)) break;

                // 从头开始
                i = -1;
            }
        }
        return str;
    }

    /// <summary>从当前字符串结尾移除另一字符串，不区分大小写，循环多次匹配后缀</summary>
    /// <param name="str">当前字符串</param>
    /// <param name="ends">后缀集合</param>
    /// <returns>移除后的字符串</returns>
    public static String TrimEnd(this String str, params String[] ends)
    {
        if (str.IsNullOrEmpty()) return str;
        if (ends == null || ends.Length == 0 || ends[0].IsNullOrEmpty()) return str;
        for (var i = 0; i < ends.Length; i++)
        {
            if (str.EndsWith(ends[i], StringComparison.OrdinalIgnoreCase))
            {
                str = str[..^ends[i].Length];
                if (String.IsNullOrEmpty(str)) break;

                // 从头开始
                i = -1;
            }
        }
        return str;
    }

    /// <summary>修剪不可见字符。仅修剪ASCII控制字符，不包含 Unicode 其它类别</summary>
    /// <param name="value">字符串</param>
    /// <returns>处理后字符串（若未发现不可见字符返回原引用）</returns>
    public static String? TrimInvisible(this String? value)
    {
        if (value.IsNullOrEmpty()) return value;
        // 先快速扫描，如没有控制字符直接返回原字符串，避免额外分配
        if (!value.Any(e => e <= 31 || e == 127)) return value;

        var builder = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            // 可见字符。ASCII码中，第0～31号及第127号(共33个)是控制字符或通讯专用字符
            var c = value[i];
            if (c > 31 && c != 127) builder.Append(c);
        }

        return builder.ToString();
    }

    /// <summary>从字符串中检索子字符串，在指定头部字符串之后，指定尾部字符串之前</summary>
    /// <remarks>常用于截取 xml 某一个元素等操作</remarks>
    /// <param name="str">目标字符串</param>
    /// <param name="after">头部字符串，在它之后</param>
    /// <param name="before">尾部字符串，在它之前</param>
    /// <param name="startIndex">搜索的开始位置</param>
    /// <param name="positions">位置数组，两个元素分别记录头尾位置（内容起始与结束前一位）</param>
    /// <returns>匹配的子串，未命中返回空串</returns>
    public static String Substring(this String str, String? after, String? before = null, Int32 startIndex = 0, Int32[]? positions = null)
    {
        if (str.IsNullOrEmpty()) return str;
        if (after.IsNullOrEmpty() && before.IsNullOrEmpty()) return str;

        /*
         * 1，只有start，从该字符串之后部分
         * 2，只有end，从开头到该字符串之前
         * 3，同时start和end，取中间部分
         */

        var p = -1;
        if (!after.IsNullOrEmpty())
        {
            p = str.IndexOf(after, startIndex, StringComparison.Ordinal);
            if (p < 0) return String.Empty;
            p += after.Length;

            // 记录位置
            if (positions != null && positions.Length > 0) positions[0] = p;
        }

        if (before.IsNullOrEmpty()) return str[p..];
        var f = str.IndexOf(before, p >= 0 ? p : startIndex, StringComparison.Ordinal);
        if (f < 0) return String.Empty;

        // 记录位置
        if (positions != null && positions.Length > 1) positions[1] = f;

        return p >= 0 ? str[p..f] : str[..f];
    }

    /// <summary>根据最大长度截取字符串，并允许以指定空白填充末尾</summary>
    /// <param name="str">字符串</param>
    /// <param name="maxLength">截取后字符串的最大允许长度，包含后面填充</param>
    /// <param name="pad">需要填充在后面的字符串，比如几个圆点</param>
    /// <returns>截取结果（不足长度直接返回原串）</returns>
    public static String Cut(this String str, Int32 maxLength, String? pad = null)
    {
        if (str.IsNullOrEmpty() || maxLength <= 0 || str.Length < maxLength) return str;
        var len = maxLength;
        if (!pad.IsNullOrEmpty()) len -= pad.Length;
        if (len <= 0) throw new ArgumentOutOfRangeException(nameof(maxLength));

        return str[..len] + pad;
    }

    /// <summary>从当前字符串开头移除另一字符串以及之前的部分</summary>
    /// <param name="str">当前字符串</param>
    /// <param name="starts">另一字符串集合</param>
    /// <returns>处理后结果（顺序逐个，命中后继续后面的起始位置）</returns>
    public static String CutStart(this String str, params String[] starts)
    {
        if (str.IsNullOrEmpty()) return str;
        if (starts == null || starts.Length == 0 || starts[0].IsNullOrEmpty()) return str;
        for (var i = 0; i < starts.Length; i++)
        {
            var p = str.IndexOf(starts[i], StringComparison.Ordinal);
            if (p >= 0)
            {
                str = str[(p + starts[i].Length)..];
                if (str.IsNullOrEmpty()) break;
            }
        }
        return str;
    }

    /// <summary>从当前字符串结尾移除另一字符串以及之后的部分</summary>
    /// <param name="str">当前字符串</param>
    /// <param name="ends">另一字符串集合</param>
    /// <returns>处理后结果</returns>
    public static String CutEnd(this String str, params String[] ends)
    {
        if (str.IsNullOrEmpty()) return str;
        if (ends == null || ends.Length == 0 || ends[0].IsNullOrEmpty()) return str;
        for (var i = 0; i < ends.Length; i++)
        {
            var p = str.LastIndexOf(ends[i], StringComparison.Ordinal);
            if (p >= 0)
            {
                str = str[..p];
                if (str.IsNullOrEmpty()) break;
            }
        }
        return str;
    }
    #endregion

    #region LD编辑距离算法
    private static readonly Char[] _separator = [' ', '　'];
    /// <summary>编辑距离搜索，从词组中找到最接近关键字的若干匹配项</summary>
    /// <remarks>
    /// 算法代码由@Aimeast 独立完成。http://www.cnblogs.com/Aimeast/archive/2011/09/05/2167844.html
    /// </remarks>
    /// <param name="key">关键字（可含空格分词）</param>
    /// <param name="words">词组</param>
    /// <returns>过滤后的候选集</returns>
    public static String[] LevenshteinSearch(String key, String[] words)
    {
        if (IsNullOrWhiteSpace(key)) return [];
        if (words == null || words.Length == 0) return [];

        var keys = key.Split(_separator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var item in keys)
        {
            var maxDist = (item.Length - 1) / 2;
            var q = from str in words
                    where item.Length <= str.Length
                        && Enumerable.Range(0, maxDist + 1).Any(dist =>
                            Enumerable.Range(0, Math.Max(str.Length - item.Length - dist + 1, 0))
                                .Any(f => LevenshteinDistance(item, str.Substring(f, item.Length + dist)) <= maxDist))
                    orderby str
                    select str;
            words = q.ToArray();
        }

        return words;
    }

    /// <summary>Levenshtein 编辑距离</summary>
    /// <remarks>
    /// 又称Levenshtein距离（也叫做Edit Distance），是指两个字串之间，由一个转成另一个所需的最少编辑操作次数。
    /// 许可的编辑操作包括将一个字符替换成另一个字符，插入一个字符，删除一个字符。
    /// 算法代码由@Aimeast 独立完成。http://www.cnblogs.com/Aimeast/archive/2011/09/05/2167844.html
    /// </remarks>
    /// <param name="str1">字符串1</param>
    /// <param name="str2">字符串2</param>
    /// <returns>最少编辑操作次数</returns>
    public static Int32 LevenshteinDistance(String str1, String str2)
    {
        var n = str1.Length;
        var m = str2.Length;
        var C = new Int32[n + 1, m + 1];
        for (var i = 0; i <= n; i++) C[i, 0] = i;
        for (var i = 1; i <= m; i++) C[0, i] = i;
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < m; j++)
            {
                var x = C[i, j + 1] + 1;
                var y = C[i + 1, j] + 1;
                var z = C[i, j] + (str1[i] == str2[j] ? 0 : 1);
                C[i + 1, j + 1] = Math.Min(Math.Min(x, y), z);
            }
        }
        return C[n, m];
    }
    #endregion

    #region LCS算法
    private static readonly Char[] _separator2 = [' ', '\u3000'];
    /// <summary>最长公共子序列搜索，从词组中找到最接近关键字的若干匹配项</summary>
    /// <remarks>
    /// 算法代码由@Aimeast 独立完成。http://www.cnblogs.com/Aimeast/archive/2011/09/05/2167844.html
    /// </remarks>
    /// <param name="key">关键字</param>
    /// <param name="words">候选词</param>
    /// <returns>匹配集合</returns>
    public static String[] LCSSearch(String key, String[] words)
    {
        if (IsNullOrWhiteSpace(key) || words == null || words.Length == 0) return [];
        var keys = key.Split(_separator2, StringSplitOptions.RemoveEmptyEntries).OrderBy(s => s.Length).ToArray();
        var q = from word in words
                let MLL = LCSDistance(word, keys)
                where MLL >= 0
                orderby (MLL + 0.5) / word.Length, word
                select word;

        return q.ToArray();
    }

    /// <summary>计算多个关键字到指定单词的加权 LCS 距离</summary>
    /// <remarks>
    /// 算法代码由@Aimeast 独立完成。http://www.cnblogs.com/Aimeast/archive/2011/09/05/2167844.html

    /// 最长公共子序列问题是寻找两个或多个已知数列最长的子序列。
    /// 一个数列 S，如果分别是两个或多个已知数列的子序列，且是所有符合此条件序列中最长的，则 S 称为已知序列的最长公共子序列。
    /// The longest common subsequence (LCS) problem is to find the longest subsequence common to all sequences in a set of sequences (often just two). Note that subsequence is different from a substring, see substring vs. subsequence. It is a classic computer science problem, the basis of diff (a file comparison program that outputs the differences between two files), and has applications in bioinformatics.
    /// </remarks>
    /// <param name="word">被匹配单词</param>
    /// <param name="keys">多个关键字（长度升序）</param>
    /// <returns>距离（-1 表示排除）</returns>
    public static Int32 LCSDistance(String word, String[] keys)
    {
        var sLength = word.Length;
        var result = sLength;
        var flags = new Boolean[sLength];
        var C = new Int32[sLength + 1, keys[^1].Length + 1];
        //int[,] C = new int[sLength + 1, words.Select(s => s.Length).Max() + 1];
        foreach (var key in keys)
        {
            var wLength = key.Length;
            Int32 first = 0, last = 0;
            Int32 i = 0, j = 0, LCS_L;
            //foreach 速度会有所提升，还可以加剪枝
            for (i = 0; i < sLength; i++)
            {
                for (j = 0; j < wLength; j++)
                {
                    if (word[i] == key[j])
                    {
                        C[i + 1, j + 1] = C[i, j] + 1;
                        if (first < C[i, j]) { last = i; first = C[i, j]; }
                    }
                    else
                        C[i + 1, j + 1] = Math.Max(C[i, j + 1], C[i + 1, j]);
                }
            }
            LCS_L = C[i, j];
            if (LCS_L <= wLength >> 1) return -1;
            while (i > 0 && j > 0)
            {
                if (C[i - 1, j - 1] + 1 == C[i, j])
                {
                    i--; j--;
                    if (!flags[i]) { flags[i] = true; result--; }
                    first = i;
                }
                else if (C[i - 1, j] == C[i, j])
                    i--;
                else
                    j--;
            }
            if (LCS_L <= (last - first + 1) >> 1) return -1;
        }

        return result;
    }

    /// <summary>根据列表项成员计算距离</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="keys"></param>
    /// <param name="keySelector"></param>
    /// <returns></returns>
    public static IEnumerable<KeyValuePair<T, Double>> LCS<T>(this IEnumerable<T> list, String keys, Func<T, String> keySelector)
    {
        var rs = new List<KeyValuePair<T, Double>>();

        if (list == null || !list.Any()) return rs;
        if (keys.IsNullOrWhiteSpace()) return rs;
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

        var ks = keys.Split(' ').OrderBy(_ => _.Length).ToArray();

        // 计算每个项到关键字的距离
        foreach (var item in list)
        {
            var name = keySelector(item);
            if (name.IsNullOrEmpty()) continue;

            var dist = LCSDistance(name, ks);
            if (dist >= 0)
            {
                var val = (Double)dist / name.Length;
                rs.Add(new KeyValuePair<T, Double>(item, val));
            }
        }

        //return rs.OrderBy(e => e.Value);
        return rs;
    }

    /// <summary>在列表项中进行模糊搜索</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="keys"></param>
    /// <param name="keySelector"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public static IEnumerable<T> LCSSearch<T>(this IEnumerable<T> list, String keys, Func<T, String> keySelector, Int32 count = -1)
    {
        var rs = LCS(list, keys, keySelector);
        rs = count >= 0 ? rs.OrderBy(e => e.Value).Take(count) : rs.OrderBy(e => e.Value);
        return rs.Select(e => e.Key);
    }
    #endregion

    #region 字符串模糊匹配
    /// <summary>模糊匹配</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="keys"></param>
    /// <param name="keySelector"></param>
    /// <returns></returns>
    public static IList<KeyValuePair<T, Double>> Match<T>(this IEnumerable<T> list, String keys, Func<T, String> keySelector)
    {
        var rs = new List<KeyValuePair<T, Double>>();

        if (list == null || !list.Any()) return rs;
        if (keys.IsNullOrWhiteSpace()) return rs;
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

        var ks = keys.Split(' ').OrderBy(_ => _.Length).ToArray();

        // 计算每个项到关键字的权重
        foreach (var item in list)
        {
            var name = keySelector(item);
            if (name.IsNullOrEmpty()) continue;

            var dist = ks.Sum(e =>
            {
                var kv = Match(name, e, e.Length);
                return kv.Key - kv.Value * 0.1; // 命中数 - (跳过数 * 惩罚系数)
            });
            if (dist > 0)
            {
                var val = dist / keys.Length;
                //var val = dist;
                rs.Add(new KeyValuePair<T, Double>(item, val));
            }
        }

        return rs;
    }

    /// <summary>模糊匹配</summary>
    /// <param name="str"></param>
    /// <param name="key"></param>
    /// <param name="maxError"></param>
    /// <returns></returns>
    public static KeyValuePair<Int32, Int32> Match(String str, String key, Int32 maxError = 0)
    {
        /*
         * 字符串 abcdef
         * 少字符 ace      (3, 0)
         * 多字符 abkcd    (4, 1)
         * 改字符 abmd     (3, 1)
         */

        // str下一次要匹配的位置
        var m = 0;
        // key下一次要匹配的位置
        var k = 0;
        var match = 0; // 总匹配数
        var skip = 0;  // 跳过次数
        while (skip <= maxError && k < key.Length)
        {
            // 向前逐个匹配
            for (var i = m; i < str.Length; i++)
            {
                if (str[i] == key[k])
                {
                    k++;
                    m = i + 1;
                    match++;

                    // 如果已完全匹配，则结束
                    if (k == key.Length) break;
                }
            }

            // 如果已完全匹配，则结束
            if (k == key.Length) break;

            // 没有完全匹配，跳过关键字中的一个字符串，从上一次匹配后面继续找
            k++;
            skip++;
        }

        return new KeyValuePair<Int32, Int32>(match, skip);
    }

    /// <summary>模糊匹配</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list">列表项</param>
    /// <param name="keys">关键字</param>
    /// <param name="keySelector">匹配字符串选择</param>
    /// <param name="count">获取个数</param>
    /// <param name="confidence">权重阀值</param>
    /// <returns></returns>
    public static IEnumerable<T> Match<T>(this IEnumerable<T> list, String keys, Func<T, String> keySelector, Int32 count, Double confidence = 0.5)
    {
        var rs = Match(list, keys, keySelector).Where(e => e.Value >= confidence);
        rs = count >= 0 ? rs.OrderByDescending(e => e.Value).Take(count) : rs.OrderByDescending(e => e.Value);
        return rs.Select(e => e.Key);
    }
    #endregion

    #region 文字转语音
    private static NewLife.Extension.SpeakProvider? _provider; // 延迟初始化，避免未使用时加载语音组件
    [MemberNotNull(nameof(_provider))]
    static void Init()
    {
        _provider ??= new NewLife.Extension.SpeakProvider();
    }

    /// <summary>调用语音引擎说出指定话</summary>
    /// <param name="value"></param>
    public static void Speak(this String value)
    {
        Init();

        _provider.Speak(value);
    }

    /// <summary>异步调用语音引擎说出指定话。可能导致后来的调用打断前面的语音</summary>
    /// <param name="value"></param>
    public static void SpeakAsync(this String value)
    {
        Init();

        _provider.SpeakAsync(value);
    }

    /// <summary>启用语音提示</summary>
    public static Boolean EnableSpeechTip { get; set; } = true;

    /// <summary>语音提示操作</summary>
    /// <param name="value"></param>
    public static void SpeechTip(this String value)
    {
        if (!EnableSpeechTip) return;
        try { SpeakAsync(value); } catch { }
    }

    /// <summary>停止所有语音播报</summary>
    public static String SpeakAsyncCancelAll(this String value)
    {
        Init();

        _provider.SpeakAsyncCancelAll();

        return value;
    }
    #endregion
}