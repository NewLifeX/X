﻿using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;
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
    /// <returns></returns>
    public static Boolean EqualIgnoreCase(this String? value, params String?[] strs)
    {
        foreach (var item in strs)
        {
            if (String.Equals(value, item, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    /// <summary>忽略大小写的字符串开始比较，判断是否与任意一个待比较字符串开始</summary>
    /// <param name="value">字符串</param>
    /// <param name="strs">待比较字符串数组</param>
    /// <returns></returns>
    public static Boolean StartsWithIgnoreCase(this String? value, params String?[] strs)
    {
        if (value == null || String.IsNullOrEmpty(value)) return false;

        foreach (var item in strs)
        {
            if (!String.IsNullOrEmpty(item) && value.StartsWith(item, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    /// <summary>忽略大小写的字符串结束比较，判断是否以任意一个待比较字符串结束</summary>
    /// <param name="value">字符串</param>
    /// <param name="strs">待比较字符串数组</param>
    /// <returns></returns>
    public static Boolean EndsWithIgnoreCase(this String? value, params String?[] strs)
    {
        if (value == null || String.IsNullOrEmpty(value)) return false;

        foreach (var item in strs)
        {
            if (item != null && value.EndsWith(item, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    /// <summary>指示指定的字符串是 null 还是 String.Empty 字符串</summary>
    /// <param name="value">字符串</param>
    /// <returns></returns>
    public static Boolean IsNullOrEmpty([NotNullWhen(false)] this String? value) => value == null || value.Length <= 0;

    /// <summary>是否空或者空白字符串</summary>
    /// <param name="value">字符串</param>
    /// <returns></returns>
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
    /// <returns></returns>
    public static String[] Split(this String? value, params String[] separators)
    {
        //!! netcore3.0中新增Split(String? separator, StringSplitOptions options = StringSplitOptions.None)，优先于StringHelper扩展
        if (value == null || String.IsNullOrEmpty(value)) return [];
        if (separators == null || separators.Length <= 0 || separators.Length == 1 && separators[0].IsNullOrEmpty()) separators = [",", ";"];

        return value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>拆分字符串成为整型数组，默认逗号分号分隔，无效时返回空数组</summary>
    /// <remarks>过滤空格、过滤无效、不过滤重复</remarks>
    /// <param name="value">字符串</param>
    /// <param name="separators">分组分隔符，默认逗号分号</param>
    /// <returns></returns>
    public static Int32[] SplitAsInt(this String? value, params String[] separators)
    {
        if (value == null || String.IsNullOrEmpty(value)) return [];
        if (separators == null || separators.Length <= 0) separators = [",", ";"];

        var ss = value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        var list = new List<Int32>();
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
    /// <param name="trimQuotation">去掉括号</param>
    /// <returns></returns>
    public static IDictionary<String, String> SplitAsDictionary(this String? value, String nameValueSeparator = "=", String separator = ";", Boolean trimQuotation = false)
    {
        var dic = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        if (value == null || value.IsNullOrWhiteSpace()) return dic;

        if (nameValueSeparator.IsNullOrEmpty()) nameValueSeparator = "=";
        //if (separator == null || separator.Length <= 0) separator = new String[] { ",", ";" };

        var ss = value.Split([separator], StringSplitOptions.RemoveEmptyEntries);
        if (ss == null || ss.Length <= 0) return dic;

        var k = 0;
        foreach (var item in ss)
        {
            // 如果分隔符是 \u0001，则必须使用Ordinal，否则无法分割直接返回0。在RocketMQ中有这种情况
            var p = item.IndexOf(nameValueSeparator, StringComparison.Ordinal);
            if (p <= 0)
            {
                dic[$"[{k}]"] = item;
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

            k++;
            //dic[key] = val;
#if NETFRAMEWORK || NETSTANDARD2_0
            if (!dic.ContainsKey(key)) dic.Add(key, val);
#else
            dic.TryAdd(key, val);
#endif
        }

        return dic;
    }

    ///// <summary>
    ///// 在.netCore需要区分该部分内容
    ///// </summary>
    ///// <param name="value"></param>
    ///// <param name="nameValueSeparator"></param>
    ///// <param name="separator"></param>
    ///// <param name="trimQuotation"></param>
    ///// <returns></returns>
    //public static IDictionary<String, String> SplitAsDictionaryT(this String? value, Char nameValueSeparator = '=', Char separator = ';', Boolean trimQuotation = false)
    //{
    //    var dic = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);
    //    if (value == null || value.IsNullOrWhiteSpace()) return dic;

    //    //if (nameValueSeparator == null) nameValueSeparator = '=';
    //    //if (separator == null || separator.Length <= 0) separator = new String[] { ",", ";" };

    //    var ss = value.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
    //    if (ss == null || ss.Length <= 0) return dic;

    //    foreach (var item in ss)
    //    {
    //        var p = item.IndexOf(nameValueSeparator);
    //        if (p <= 0) continue;

    //        var key = item[..p].Trim();
    //        var val = item[(p + 1)..].Trim();


    //        // 处理单引号双引号
    //        if (trimQuotation && !val.IsNullOrEmpty())
    //        {
    //            if (val[0] == '\'' && val[^1] == '\'') val = val.Trim('\'');
    //            if (val[0] == '"' && val[^1] == '"') val = val.Trim('"');
    //        }

    //        dic[key] = val;
    //    }

    //    return dic;
    //}

    /// <summary>把一个列表组合成为一个字符串，默认逗号分隔</summary>
    /// <param name="value"></param>
    /// <param name="separator">组合分隔符，默认逗号</param>
    /// <returns></returns>
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

    ///// <summary>把一个列表组合成为一个字符串，默认逗号分隔</summary>
    ///// <param name="value"></param>
    ///// <param name="separator">组合分隔符，默认逗号</param>
    ///// <param name="func">把对象转为字符串的委托</param>
    ///// <returns></returns>
    //[Obsolete]
    //public static String Join<T>(this IEnumerable<T> value, String separator, Func<T, String>? func)
    //{
    //    var sb = Pool.StringBuilder.Get();
    //    if (value != null)
    //    {
    //        if (func == null) func = obj => obj + "";
    //        foreach (var item in value)
    //        {
    //            sb.Separate(separator).Append(func(item));
    //        }
    //    }
    //    return sb.Put(true);
    //}

    /// <summary>把一个列表组合成为一个字符串，默认逗号分隔</summary>
    /// <param name="value"></param>
    /// <param name="separator">组合分隔符，默认逗号</param>
    /// <param name="func">把对象转为字符串的委托</param>
    /// <returns></returns>
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
    /// <returns></returns>
    public static StringBuilder Separate(this StringBuilder sb, String separator)
    {
        if (/*sb == null ||*/ String.IsNullOrEmpty(separator)) return sb;

        if (sb.Length > 0) sb.Append(separator);

        return sb;
    }

    /// <summary>字符串转数组</summary>
    /// <param name="value">字符串</param>
    /// <param name="encoding">编码，默认utf-8无BOM</param>
    /// <returns></returns>
    public static Byte[] GetBytes(this String? value, Encoding? encoding = null)
    {
        //if (value == null) return null;
        if (String.IsNullOrEmpty(value)) return [];

        encoding ??= Encoding.UTF8;
        return encoding.GetBytes(value);
    }

    /// <summary>格式化字符串。特别支持无格式化字符串的时间参数</summary>
    /// <param name="value">格式字符串</param>
    /// <param name="args">参数</param>
    /// <returns></returns>
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

    /// <summary>指定输入是否匹配目标表达式，支持*匹配</summary>
    /// <param name="pattern">匹配表达式</param>
    /// <param name="input">输入字符串</param>
    /// <param name="comparisonType">字符串比较方式</param>
    /// <returns></returns>
    public static Boolean IsMatch(this String pattern, String input, StringComparison comparisonType = StringComparison.CurrentCulture)
    {
        if (pattern.IsNullOrEmpty()) return false;

        // 单独*匹配所有，即使输入字符串为空
        if (pattern == "*") return true;
        if (input.IsNullOrEmpty()) return false;

        // 普通表达式，直接包含
        var p = pattern.IndexOf('*');
        if (p < 0) return String.Equals(input, pattern, comparisonType);

        // 表达式分组
        var ps = pattern.Split('*');

        // 头尾专用匹配
        if (ps.Length == 2)
        {
            if (p == 0) return input.EndsWith(ps[1], comparisonType);
            if (p == pattern.Length - 1) return input.StartsWith(ps[0], comparisonType);
        }

        // 逐项跳跃式匹配
        p = 0;
        for (var i = 0; i < ps.Length; i++)
        {
            // 最后一组反向匹配
            if (i == ps.Length - 1)
                p = input.LastIndexOf(ps[i], input.Length - 1, input.Length - p, comparisonType);
            else
                p = input.IndexOf(ps[i], p, comparisonType);
            if (p < 0) return false;

            // 第一组必须开头
            if (i == 0 && p > 0) return false;

            p += ps[i].Length;
        }

        // 最后一组*允许不到边界
        if (ps[^1].IsNullOrEmpty()) return p <= input.Length;

        // 最后一组必须结尾
        return p == input.Length;
    }

#if NETFRAMEWORK || NETSTANDARD2_0
    /// <summary>Returns a value indicating whether a specified character occurs within this string.</summary>
    /// <param name="value"></param>
    /// <param name="inputChar">The character to seek.</param>
    /// <returns>
    /// <see langword="true" /> if the <paramref name="inputChar" /> parameter occurs within this string; otherwise, <see langword="false" />.</returns>
    public static Boolean Contains(this String value, Char inputChar) => value.IndexOf(inputChar) >= 0;

    /// <summary>Splits a string into substrings based on the characters in an array. You can specify whether the substrings include empty array elements.</summary>
    /// <param name="value"></param>
    /// <param name="separator">A character array that delimits the substrings in this string, an empty array that contains no delimiters, or <see langword="null" />.</param>
    /// <param name="options">
    /// <see cref="F:System.StringSplitOptions.RemoveEmptyEntries" /> to omit empty array elements from the array returned; or <see cref="F:System.StringSplitOptions.None" /> to include empty array elements in the array returned.</param>
    /// <returns>An array whose elements contain the substrings in this string that are delimited by one or more characters in <paramref name="separator" />. For more information, see the Remarks section.</returns>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="options" /> is not one of the <see cref="T:System.StringSplitOptions" /> values.</exception>
    public static String[] Split(this String value, Char separator, StringSplitOptions options = StringSplitOptions.None) => value.Split(new Char[] { separator }, options);
#endif
    #endregion

    #region 截取扩展
    /// <summary>确保字符串以指定的另一字符串开始，不区分大小写</summary>
    /// <param name="str">字符串</param>
    /// <param name="start"></param>
    /// <returns></returns>
    public static String EnsureStart(this String? str, String start)
    {
        if (String.IsNullOrEmpty(start)) return str + "";
        if (String.IsNullOrEmpty(str) || str == null) return start + "";

        if (str.StartsWith(start, StringComparison.OrdinalIgnoreCase)) return str;

        return start + str;
    }

    /// <summary>确保字符串以指定的另一字符串结束，不区分大小写</summary>
    /// <param name="str">字符串</param>
    /// <param name="end"></param>
    /// <returns></returns>
    public static String EnsureEnd(this String? str, String end)
    {
        if (String.IsNullOrEmpty(end)) return str + "";
        if (String.IsNullOrEmpty(str) || str == null) return end + "";

        if (str.EndsWith(end, StringComparison.OrdinalIgnoreCase)) return str;

        return str + end;
    }

    /// <summary>从当前字符串开头移除另一字符串，不区分大小写，循环多次匹配前缀</summary>
    /// <param name="str">当前字符串</param>
    /// <param name="starts">另一字符串</param>
    /// <returns></returns>
    public static String TrimStart(this String str, params String[] starts)
    {
        if (String.IsNullOrEmpty(str)) return str;
        if (starts == null || starts.Length <= 0 || String.IsNullOrEmpty(starts[0])) return str;

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
    /// <param name="ends">另一字符串</param>
    /// <returns></returns>
    public static String TrimEnd(this String str, params String[] ends)
    {
        if (String.IsNullOrEmpty(str)) return str;
        if (ends == null || ends.Length <= 0 || String.IsNullOrEmpty(ends[0])) return str;

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

    /// <summary>修剪不可见字符。仅修剪ASCII，不包含Unicode</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static String? TrimInvisible(this String? value)
    {
        if (value.IsNullOrEmpty()) return value;

        var builder = new StringBuilder();

        for (var i = 0; i < value.Length; i++)
        {
            // 可见字符。ASCII码中，第0～31号及第127号(共33个)是控制字符或通讯专用字符
            if (value[i] is > (Char)31 and not (Char)127)
                builder.Append(value[i]);
        }

        return builder.ToString();
    }

    /// <summary>从字符串中检索子字符串，在指定头部字符串之后，指定尾部字符串之前</summary>
    /// <remarks>常用于截取xml某一个元素等操作</remarks>
    /// <param name="str">目标字符串</param>
    /// <param name="after">头部字符串，在它之后</param>
    /// <param name="before">尾部字符串，在它之前</param>
    /// <param name="startIndex">搜索的开始位置</param>
    /// <param name="positions">位置数组，两个元素分别记录头尾位置</param>
    /// <returns></returns>
    public static String Substring(this String str, String? after, String? before = null, Int32 startIndex = 0, Int32[]? positions = null)
    {
        if (String.IsNullOrEmpty(str)) return str;
        if (String.IsNullOrEmpty(after) && String.IsNullOrEmpty(before)) return str;

        /*
         * 1，只有start，从该字符串之后部分
         * 2，只有end，从开头到该字符串之前
         * 3，同时start和end，取中间部分
         */

        var p = -1;
        if (!after.IsNullOrEmpty())
        {
            p = str.IndexOf(after, startIndex);
            if (p < 0) return String.Empty;
            p += after.Length;

            // 记录位置
            if (positions != null && positions.Length > 0) positions[0] = p;
        }

        if (String.IsNullOrEmpty(before)) return str[p..];

        var f = str.IndexOf(before, p >= 0 ? p : startIndex);
        if (f < 0) return String.Empty;

        // 记录位置
        if (positions != null && positions.Length > 1) positions[1] = f;

        if (p >= 0)
            return str[p..f];
        else
            return str[..f];
    }

    /// <summary>根据最大长度截取字符串，并允许以指定空白填充末尾</summary>
    /// <param name="str">字符串</param>
    /// <param name="maxLength">截取后字符串的最大允许长度，包含后面填充</param>
    /// <param name="pad">需要填充在后面的字符串，比如几个圆点</param>
    /// <returns></returns>
    public static String Cut(this String str, Int32 maxLength, String? pad = null)
    {
        if (String.IsNullOrEmpty(str) || maxLength <= 0 || str.Length < maxLength) return str;

        // 计算截取长度
        var len = maxLength;
        if (pad != null && !String.IsNullOrEmpty(pad)) len -= pad.Length;
        if (len <= 0) throw new ArgumentOutOfRangeException(nameof(maxLength));

        return str[..len] + pad;
    }

    /// <summary>从当前字符串开头移除另一字符串以及之前的部分</summary>
    /// <param name="str">当前字符串</param>
    /// <param name="starts">另一字符串</param>
    /// <returns></returns>
    public static String CutStart(this String str, params String[] starts)
    {
        if (str.IsNullOrEmpty()) return str;
        if (starts == null || starts.Length <= 0 || starts[0].IsNullOrEmpty()) return str;

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
    /// <param name="ends">另一字符串</param>
    /// <returns></returns>
    public static String CutEnd(this String str, params String[] ends)
    {
        if (String.IsNullOrEmpty(str)) return str;
        if (ends == null || ends.Length <= 0 || String.IsNullOrEmpty(ends[0])) return str;

        for (var i = 0; i < ends.Length; i++)
        {
            var p = str.LastIndexOf(ends[i], StringComparison.Ordinal);
            if (p >= 0)
            {
                str = str[..p];
                if (String.IsNullOrEmpty(str)) break;
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
    /// <param name="key">关键字</param>
    /// <param name="words">词组</param>
    /// <returns></returns>
    public static String[] LevenshteinSearch(String key, String[] words)
    {
        if (IsNullOrWhiteSpace(key)) return [];

        var keys = key.Split(_separator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var item in keys)
        {
            var maxDist = (item.Length - 1) / 2;

            var q = from str in words
                    where item.Length <= str.Length
                        && Enumerable.Range(0, maxDist + 1)
                        .Any(dist =>
                        {
                            return Enumerable.Range(0, Math.Max(str.Length - item.Length - dist + 1, 0))
                                .Any(f =>
                                {
                                    return LevenshteinDistance(item, str.Substring(f, item.Length + dist)) <= maxDist;
                                });
                        })
                    orderby str
                    select str;
            words = q.ToArray();
        }

        return words;
    }

    /// <summary>编辑距离</summary>
    /// <remarks>
    /// 又称Levenshtein距离（也叫做Edit Distance），是指两个字串之间，由一个转成另一个所需的最少编辑操作次数。
    /// 许可的编辑操作包括将一个字符替换成另一个字符，插入一个字符，删除一个字符。
    /// 算法代码由@Aimeast 独立完成。http://www.cnblogs.com/Aimeast/archive/2011/09/05/2167844.html
    /// </remarks>
    /// <param name="str1"></param>
    /// <param name="str2"></param>
    /// <returns></returns>
    public static Int32 LevenshteinDistance(String str1, String str2)
    {
        var n = str1.Length;
        var m = str2.Length;
        var C = new Int32[n + 1, m + 1];
        Int32 i, j, x, y, z;
        for (i = 0; i <= n; i++)
            C[i, 0] = i;
        for (i = 1; i <= m; i++)
            C[0, i] = i;
        for (i = 0; i < n; i++)
            for (j = 0; j < m; j++)
            {
                x = C[i, j + 1] + 1;
                y = C[i + 1, j] + 1;
                if (str1[i] == str2[j])
                    z = C[i, j];
                else
                    z = C[i, j] + 1;
                C[i + 1, j + 1] = Math.Min(Math.Min(x, y), z);
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
    /// <param name="key"></param>
    /// <param name="words"></param>
    /// <returns></returns>
    public static String[] LCSSearch(String key, String[] words)
    {
        if (IsNullOrWhiteSpace(key) || words == null || words.Length == 0) return [];

        var keys = key
            .Split(_separator2, StringSplitOptions.RemoveEmptyEntries)
            .OrderBy(s => s.Length)
            .ToArray();

        //var q = from sentence in items.AsParallel()
        var q = from word in words
                let MLL = LCSDistance(word, keys)
                where MLL >= 0
                orderby (MLL + 0.5) / word.Length, word
                select word;

        return q.ToArray();
    }

    /// <summary>
    /// 最长公共子序列问题是寻找两个或多个已知数列最长的子序列。
    /// 一个数列 S，如果分别是两个或多个已知数列的子序列，且是所有符合此条件序列中最长的，则 S 称为已知序列的最长公共子序列。
    /// The longest common subsequence (LCS) problem is to find the longest subsequence common to all sequences in a set of sequences (often just two). Note that subsequence is different from a substring, see substring vs. subsequence. It is a classic computer science problem, the basis of diff (a file comparison program that outputs the differences between two files), and has applications in bioinformatics.
    /// </summary>
    /// <remarks>
    /// 算法代码由@Aimeast 独立完成。http://www.cnblogs.com/Aimeast/archive/2011/09/05/2167844.html
    /// </remarks>
    /// <param name="word"></param>
    /// <param name="keys">多个关键字。长度必须大于0，必须按照字符串长度升序排列。</param>
    /// <returns></returns>
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
                for (j = 0; j < wLength; j++)
                    if (word[i] == key[j])
                    {
                        C[i + 1, j + 1] = C[i, j] + 1;
                        if (first < C[i, j])
                        {
                            last = i;
                            first = C[i, j];
                        }
                    }
                    else
                        C[i + 1, j + 1] = Math.Max(C[i, j + 1], C[i + 1, j]);

            LCS_L = C[i, j];
            if (LCS_L <= wLength >> 1)
                return -1;

            while (i > 0 && j > 0)
            {
                if (C[i - 1, j - 1] + 1 == C[i, j])
                {
                    i--;
                    j--;
                    if (!flags[i])
                    {
                        flags[i] = true;
                        result--;
                    }
                    first = i;
                }
                else if (C[i - 1, j] == C[i, j])
                    i--;
                else// if (C[i, j - 1] == C[i, j])
                    j--;
            }

            if (LCS_L <= (last - first + 1) >> 1)
                return -1;
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

        if (count >= 0)
            rs = rs.OrderBy(e => e.Value).Take(count);
        else
            rs = rs.OrderBy(e => e.Value);

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
                return kv.Key - kv.Value * 0.1;
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

        // 总匹配数
        var match = 0;
        // 跳过次数
        var skip = 0;

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

        if (count >= 0)
            rs = rs.OrderByDescending(e => e.Value).Take(count);
        else
            rs = rs.OrderByDescending(e => e.Value);

        return rs.Select(e => e.Key);
    }
    #endregion

    #region 文字转语音
    private static NewLife.Extension.SpeakProvider? _provider;
    //private static System.Speech.Synthesis.SpeechSynthesizer _provider;
    [MemberNotNull(nameof(_provider))]
    static void Init()
    {
        //_provider = new Speech.Synthesis.SpeechSynthesizer();
        //_provider.SetOutputToDefaultAudioDevice();
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

        try
        {
            SpeakAsync(value);
        }
        catch { }
    }

    /// <summary>
    /// 停止所有语音播报
    /// </summary>
    /// <param name="value"></param>
    public static String SpeakAsyncCancelAll(this String value)
    {
        Init();

        _provider.SpeakAsyncCancelAll();

        return value;
    }
    #endregion
}