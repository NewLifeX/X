using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NewLife;
using NewLife.Collections;
using NewLife.Log;

namespace System
{
    /// <summary>字符串助手类</summary>
    public static class StringHelper
    {
        #region 字符串扩展
        /// <summary>忽略大小写的字符串相等比较，判断是否以任意一个待比较字符串相等</summary>
        /// <param name="value">字符串</param>
        /// <param name="strs">待比较字符串数组</param>
        /// <returns></returns>
        public static Boolean EqualIgnoreCase(this String value, params String[] strs)
        {
            foreach (var item in strs)
            {
                if (String.Equals(value, item, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        /// <summary>忽略大小写的字符串开始比较，判断是否以任意一个待比较字符串开始</summary>
        /// <param name="value">字符串</param>
        /// <param name="strs">待比较字符串数组</param>
        /// <returns></returns>
        public static Boolean StartsWithIgnoreCase(this String value, params String[] strs)
        {
            if (String.IsNullOrEmpty(value)) return false;

            foreach (var item in strs)
            {
                if (value.StartsWith(item, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        /// <summary>忽略大小写的字符串结束比较，判断是否以任意一个待比较字符串结束</summary>
        /// <param name="value">字符串</param>
        /// <param name="strs">待比较字符串数组</param>
        /// <returns></returns>
        public static Boolean EndsWithIgnoreCase(this String value, params String[] strs)
        {
            if (String.IsNullOrEmpty(value)) return false;

            foreach (var item in strs)
            {
                if (value.EndsWith(item, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        /// <summary>指示指定的字符串是 null 还是 String.Empty 字符串</summary>
        /// <param name="value">字符串</param>
        /// <returns></returns>
        public static Boolean IsNullOrEmpty(this String value) { return value == null || value.Length <= 0; }

        /// <summary>是否空或者空白字符串</summary>
        /// <param name="value">字符串</param>
        /// <returns></returns>
        public static Boolean IsNullOrWhiteSpace(this String value)
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
        public static String[] Split(this String value, params String[] separators)
        {
            if (String.IsNullOrEmpty(value)) return new String[0];
            if (separators == null || separators.Length < 1 || separators.Length == 1 && separators[0].IsNullOrEmpty()) separators = new String[] { ",", ";" };

            return value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>拆分字符串成为整型数组，默认逗号分号分隔，无效时返回空数组</summary>
        /// <remarks>过滤空格、过滤无效、不过滤重复</remarks>
        /// <param name="value">字符串</param>
        /// <param name="separators">分组分隔符，默认逗号分号</param>
        /// <returns></returns>
        public static Int32[] SplitAsInt(this String value, params String[] separators)
        {
            if (String.IsNullOrEmpty(value)) return new Int32[0];
            if (separators == null || separators.Length < 1) separators = new String[] { ",", ";" };

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

        /// <summary>拆分字符串成为不区分大小写的可空名值字典。逗号分号分组，等号分隔</summary>
        /// <param name="value">字符串</param>
        /// <param name="nameValueSeparator">名值分隔符，默认等于号</param>
        /// <param name="separators">分组分隔符，默认逗号分号</param>
        /// <returns></returns>
        [Obsolete]
        public static IDictionary<String, String> SplitAsDictionary(this String value, String nameValueSeparator = "=", params String[] separators)
        {
            var dic = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            if (value.IsNullOrWhiteSpace()) return dic;

            if (String.IsNullOrEmpty(nameValueSeparator)) nameValueSeparator = "=";
            if (separators == null || separators.Length < 1) separators = new String[] { ",", ";" };

            var ss = value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            if (ss == null || ss.Length < 1) return null;

            foreach (var item in ss)
            {
                var p = item.IndexOf(nameValueSeparator);
                // 在前后都不行
                if (p <= 0 || p >= item.Length - 1) continue;

                var key = item.Substring(0, p).Trim();
                dic[key] = item.Substring(p + nameValueSeparator.Length).Trim();
            }

            return dic;
        }

        /// <summary>拆分字符串成为不区分大小写的可空名值字典。逗号分组，等号分隔</summary>
        /// <param name="value">字符串</param>
        /// <param name="nameValueSeparator">名值分隔符，默认等于号</param>
        /// <param name="separator">分组分隔符，默认分号</param>
        /// <param name="trimQuotation">去掉括号</param>
        /// <returns></returns>
        public static IDictionary<String, String> SplitAsDictionary(this String value, String nameValueSeparator = "=", String separator = ";", Boolean trimQuotation = false)
        {
            var dic = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            if (value.IsNullOrWhiteSpace()) return dic;

            if (nameValueSeparator.IsNullOrEmpty()) nameValueSeparator = "=";
            //if (separator == null || separator.Length < 1) separator = new String[] { ",", ";" };

            var ss = value.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
            if (ss == null || ss.Length < 1) return null;

            foreach (var item in ss)
            {
                var p = item.IndexOf(nameValueSeparator);
                if (p <= 0) continue;

                var key = item.Substring(0, p).Trim();
                var val = item.Substring(p + nameValueSeparator.Length).Trim();

                // 处理单引号双引号
                if (trimQuotation && !val.IsNullOrEmpty())
                {
                    if (val[0] == '\'' && val[val.Length - 1] == '\'') val = val.Trim('\'');
                    if (val[0] == '"' && val[val.Length - 1] == '"') val = val.Trim('"');
                }

                dic[key] = val;
            }

            return dic;
        }

        /// <summary>
        /// 在.netCore需要区分该部分内容
        /// </summary>
        /// <param name="value"></param>
        /// <param name="nameValueSeparator"></param>
        /// <param name="separator"></param>
        /// <param name="trimQuotation"></param>
        /// <returns></returns>
        public static IDictionary<String, String> SplitAsDictionaryT(this String value, Char nameValueSeparator = '=', Char separator = ';', Boolean trimQuotation = false)
        {
            var dic = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            if (value.IsNullOrWhiteSpace()) return dic;

            //if (nameValueSeparator == null) nameValueSeparator = '=';
            //if (separator == null || separator.Length < 1) separator = new String[] { ",", ";" };

            var ss = value.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
            if (ss == null || ss.Length < 1) return null;

            foreach (var item in ss)
            {
                var p = item.IndexOf(nameValueSeparator);
                if (p <= 0) continue;

                var key = item.Substring(0, p).Trim();
                var val = item.Substring(p + 1).Trim();

                // 处理单引号双引号
                if (trimQuotation && !val.IsNullOrEmpty())
                {
                    if (val[0] == '\'' && val[val.Length - 1] == '\'') val = val.Trim('\'');
                    if (val[0] == '"' && val[val.Length - 1] == '"') val = val.Trim('"');
                }

                dic[key] = val;
            }

            return dic;
        }

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
            return sb.Put(true);
        }

        /// <summary>把一个列表组合成为一个字符串，默认逗号分隔</summary>
        /// <param name="value"></param>
        /// <param name="separator">组合分隔符，默认逗号</param>
        /// <param name="func">把对象转为字符串的委托</param>
        /// <returns></returns>
        //[Obsolete]
        public static String Join<T>(this IEnumerable<T> value, String separator, Func<T, String> func)
        {
            var sb = Pool.StringBuilder.Get();
            if (value != null)
            {
                if (func == null) func = obj => "{0}".F(obj);
                foreach (var item in value)
                {
                    sb.Separate(separator).Append(func(item));
                }
            }
            return sb.Put(true);
        }

        /// <summary>把一个列表组合成为一个字符串，默认逗号分隔</summary>
        /// <param name="value"></param>
        /// <param name="separator">组合分隔符，默认逗号</param>
        /// <param name="func">把对象转为字符串的委托</param>
        /// <returns></returns>
        public static String Join<T>(this IEnumerable<T> value, String separator = ",", Func<T, Object> func = null)
        {
            var sb = Pool.StringBuilder.Get();
            if (value != null)
            {
                if (func == null) func = obj => "{0}".F(obj);
                foreach (var item in value)
                {
                    sb.Separate(separator).Append(func(item));
                }
            }
            return sb.Put(true);
        }

        /// <summary>追加分隔符字符串，忽略开头，常用于拼接</summary>
        /// <param name="sb">字符串构造者</param>
        /// <param name="separator">分隔符</param>
        /// <returns></returns>
        public static StringBuilder Separate(this StringBuilder sb, String separator)
        {
            if (sb == null || String.IsNullOrEmpty(separator)) return sb;

            if (sb.Length > 0) sb.Append(separator);

            return sb;
        }

        /// <summary>字符串转数组</summary>
        /// <param name="value">字符串</param>
        /// <param name="encoding">编码，默认utf-8无BOM</param>
        /// <returns></returns>
        public static Byte[] GetBytes(this String value, Encoding encoding = null)
        {
            if (value == null) return null;
            if (value == String.Empty) return new Byte[0];

            if (encoding == null) encoding = Encoding.UTF8;
            return encoding.GetBytes(value);
        }

        /// <summary>格式化字符串。特别支持无格式化字符串的时间参数</summary>
        /// <param name="value">格式字符串</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public static String F(this String value, params Object[] args)
        {
            if (String.IsNullOrEmpty(value)) return value;

            // 特殊处理时间格式化。这些年，无数项目实施因为时间格式问题让人发狂
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] is DateTime)
                {
                    // 没有写格式化字符串的时间参数，一律转为标准时间字符串
                    if (value.Contains("{" + i + "}")) args[i] = ((DateTime)args[i]).ToFullString();
                }
            }

            return String.Format(value, args);
        }
        #endregion

        #region 截取扩展
        /// <summary>确保字符串以指定的另一字符串开始，不区分大小写</summary>
        /// <param name="str">字符串</param>
        /// <param name="start"></param>
        /// <returns></returns>
        public static String EnsureStart(this String str, String start)
        {
            if (String.IsNullOrEmpty(start)) return str;
            if (String.IsNullOrEmpty(str)) return start;

            if (str.StartsWith(start, StringComparison.OrdinalIgnoreCase)) return str;

            return start + str;
        }

        /// <summary>确保字符串以指定的另一字符串结束，不区分大小写</summary>
        /// <param name="str">字符串</param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static String EnsureEnd(this String str, String end)
        {
            if (String.IsNullOrEmpty(end)) return str;
            if (String.IsNullOrEmpty(str)) return end;

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
            if (starts == null || starts.Length < 1 || String.IsNullOrEmpty(starts[0])) return str;

            for (var i = 0; i < starts.Length; i++)
            {
                if (str.StartsWith(starts[i], StringComparison.OrdinalIgnoreCase))
                {
                    str = str.Substring(starts[i].Length);
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
            if (ends == null || ends.Length < 1 || String.IsNullOrEmpty(ends[0])) return str;

            for (var i = 0; i < ends.Length; i++)
            {
                if (str.EndsWith(ends[i], StringComparison.OrdinalIgnoreCase))
                {
                    str = str.Substring(0, str.Length - ends[i].Length);
                    if (String.IsNullOrEmpty(str)) break;

                    // 从头开始
                    i = -1;
                }
            }
            return str;
        }

        /// <summary>从字符串中检索子字符串，在指定头部字符串之后，指定尾部字符串之前</summary>
        /// <remarks>常用于截取xml某一个元素等操作</remarks>
        /// <param name="str">目标字符串</param>
        /// <param name="after">头部字符串，在它之后</param>
        /// <param name="before">尾部字符串，在它之前</param>
        /// <param name="startIndex">搜索的开始位置</param>
        /// <param name="positions">位置数组，两个元素分别记录头尾位置</param>
        /// <returns></returns>
        public static String Substring(this String str, String after, String before = null, Int32 startIndex = 0, Int32[] positions = null)
        {
            if (String.IsNullOrEmpty(str)) return str;
            if (String.IsNullOrEmpty(after) && String.IsNullOrEmpty(before)) return str;

            /*
             * 1，只有start，从该字符串之后部分
             * 2，只有end，从开头到该字符串之前
             * 3，同时start和end，取中间部分
             */

            var p = -1;
            if (!String.IsNullOrEmpty(after))
            {
                p = str.IndexOf(after, startIndex);
                if (p < 0) return null;
                p += after.Length;

                // 记录位置
                if (positions != null && positions.Length > 0) positions[0] = p;
            }

            if (String.IsNullOrEmpty(before)) return str.Substring(p);

            var f = str.IndexOf(before, p >= 0 ? p : startIndex);
            if (f < 0) return null;

            // 记录位置
            if (positions != null && positions.Length > 1) positions[1] = f;

            if (p >= 0)
                return str.Substring(p, f - p);
            else
                return str.Substring(0, f);
        }

        /// <summary>根据最大长度截取字符串，并允许以指定空白填充末尾</summary>
        /// <param name="str">字符串</param>
        /// <param name="maxLength">截取后字符串的最大允许长度，包含后面填充</param>
        /// <param name="pad">需要填充在后面的字符串，比如几个圆点</param>
        /// <returns></returns>
        public static String Cut(this String str, Int32 maxLength, String pad = null)
        {
            if (String.IsNullOrEmpty(str) || maxLength <= 0 || str.Length < maxLength) return str;

            // 计算截取长度
            var len = maxLength;
            if (!String.IsNullOrEmpty(pad)) len -= pad.Length;
            if (len <= 0) return pad;

            return str.Substring(0, len) + pad;
        }

        ///// <summary>根据最大长度截取字符串（二进制计算长度），并允许以指定空白填充末尾</summary>
        ///// <remarks>默认采用Default编码进行处理，其它编码请参考本函数代码另外实现</remarks>
        ///// <param name="str">字符串</param>
        ///// <param name="maxLength">截取后字符串的最大允许长度，包含后面填充</param>
        ///// <param name="pad">需要填充在后面的字符串，比如几个圆点</param>
        ///// <param name="strict">严格模式时，遇到截断位置位于一个字符中间时，忽略该字符，否则包括该字符。默认true</param>
        ///// <returns></returns>
        //public static String CutBinary(this String str, Int32 maxLength, String pad = null, Boolean strict = true)
        //{
        //    if (String.IsNullOrEmpty(str) || maxLength <= 0 || str.Length < maxLength) return str;

        //    var encoding = Encoding.UTF8;

        //    var buf = encoding.GetBytes(str);
        //    if (buf.Length < maxLength) return str;

        //    // 计算截取字节长度
        //    var len = maxLength;
        //    if (!String.IsNullOrEmpty(pad)) len -= encoding.GetByteCount(pad);
        //    if (len <= 0) return pad;

        //    // 计算截取字符长度。避免把一个字符劈开
        //    var clen = 0;
        //    while (true)
        //    {
        //        try
        //        {
        //            clen = encoding.GetCharCount(buf, 0, len);
        //            break;
        //        }
        //        catch (DecoderFallbackException)
        //        {
        //            // 发生了回退，减少len再试
        //            len--;
        //        }
        //    }
        //    // 可能过长，修正
        //    if (strict) while (encoding.GetByteCount(str.ToCharArray(), 0, clen) > len) clen--;

        //    return str.Substring(0, clen) + pad;
        //}

        /// <summary>从当前字符串开头移除另一字符串以及之前的部分</summary>
        /// <param name="str">当前字符串</param>
        /// <param name="starts">另一字符串</param>
        /// <returns></returns>
        public static String CutStart(this String str, params String[] starts)
        {
            if (String.IsNullOrEmpty(str)) return str;
            if (starts == null || starts.Length < 1 || String.IsNullOrEmpty(starts[0])) return str;

            for (var i = 0; i < starts.Length; i++)
            {
                var p = str.IndexOf(starts[i]);
                if (p >= 0)
                {
                    str = str.Substring(p + starts[i].Length);
                    if (String.IsNullOrEmpty(str)) break;
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
            if (ends == null || ends.Length < 1 || String.IsNullOrEmpty(ends[0])) return str;

            for (var i = 0; i < ends.Length; i++)
            {
                var p = str.LastIndexOf(ends[i]);
                if (p >= 0)
                {
                    str = str.Substring(0, p);
                    if (String.IsNullOrEmpty(str)) break;
                }
            }
            return str;
        }
        #endregion

        #region LD编辑距离算法
        /// <summary>编辑距离搜索，从词组中找到最接近关键字的若干匹配项</summary>
        /// <remarks>
        /// 算法代码由@Aimeast 独立完成。http://www.cnblogs.com/Aimeast/archive/2011/09/05/2167844.html
        /// </remarks>
        /// <param name="key">关键字</param>
        /// <param name="words">词组</param>
        /// <returns></returns>
        public static String[] LevenshteinSearch(String key, String[] words)
        {
            if (IsNullOrWhiteSpace(key)) return new String[0];

            var keys = key.Split(new Char[] { ' ', '　' }, StringSplitOptions.RemoveEmptyEntries);

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
        /// <summary>最长公共子序列搜索，从词组中找到最接近关键字的若干匹配项</summary>
        /// <remarks>
        /// 算法代码由@Aimeast 独立完成。http://www.cnblogs.com/Aimeast/archive/2011/09/05/2167844.html
        /// </remarks>
        /// <param name="key"></param>
        /// <param name="words"></param>
        /// <returns></returns>
        public static String[] LCSSearch(String key, String[] words)
        {
            if (IsNullOrWhiteSpace(key) || words == null || words.Length == 0) return new String[0];

            var keys = key
                                .Split(new Char[] { ' ', '\u3000' }, StringSplitOptions.RemoveEmptyEntries)
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
            var C = new Int32[sLength + 1, keys[keys.Length - 1].Length + 1];
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

            var ks = keys.Split(" ").OrderBy(_ => _.Length).ToArray();

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
        public static IEnumerable<KeyValuePair<T, Double>> Match<T>(this IEnumerable<T> list, String keys, Func<T, String> keySelector)
        {
            var rs = new List<KeyValuePair<T, Double>>();

            if (list == null || !list.Any()) return rs;
            if (keys.IsNullOrWhiteSpace()) return rs;
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            var ks = keys.Split(" ").OrderBy(_ => _.Length).ToArray();

            // 计算每个项到关键字的权重
            foreach (var item in list)
            {
                var name = keySelector(item);
                if (name.IsNullOrEmpty()) continue;

                var dist = ks.Sum(e =>
                {
                    var kv = Match(name, e, e.Length);
                    return kv.Key - kv.Value * 0.5;
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
#if __MOBILE__
#elif __CORE__
#else
        private static NewLife.Extension.SpeakProvider _provider;
        //private static System.Speech.Synthesis.SpeechSynthesizer _provider;
        static void Init()
        {
            if (_provider == null)
            {
                //_provider = new Speech.Synthesis.SpeechSynthesizer();
                //_provider.SetOutputToDefaultAudioDevice();
                _provider = new NewLife.Extension.SpeakProvider();
            }
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
#endif
        #endregion

        #region 执行命令行
        /// <summary>以隐藏窗口执行命令行</summary>
        /// <param name="cmd">文件名</param>
        /// <param name="arguments">命令参数</param>
        /// <param name="msWait">等待毫秒数</param>
        /// <param name="output">进程输出内容。默认为空时输出到日志</param>
        /// <param name="onExit">进程退出时执行</param>
        /// <returns>进程退出代码</returns>
        public static Int32 Run(this String cmd, String arguments = null, Int32 msWait = 0, Action<String> output = null, Action<Process> onExit = null)
        {
            if (XTrace.Debug) XTrace.WriteLine("Run {0} {1} {2}", cmd, arguments, msWait);

            var p = new Process();
            var si = p.StartInfo;
            si.FileName = cmd;
            si.Arguments = arguments;
#if __CORE__
#else
            si.WindowStyle = ProcessWindowStyle.Hidden;
#endif

            // 对于控制台项目，这里需要捕获输出
            if (msWait > 0)
            {
                si.RedirectStandardOutput = true;
                si.RedirectStandardError = true;
                si.UseShellExecute = false;
                if (output != null)
                {
                    p.OutputDataReceived += (s, e) => output(e.Data);
                    p.ErrorDataReceived += (s, e) => output(e.Data);
                }
                else if (NewLife.Runtime.IsConsole)
                {
                    p.OutputDataReceived += (s, e) => XTrace.WriteLine(e.Data);
                    p.ErrorDataReceived += (s, e) => XTrace.Log.Error(e.Data);
                }
            }
            if (onExit != null) p.Exited += (s, e) => onExit(s as Process);

            p.Start();
            if (msWait > 0 && (output != null || NewLife.Runtime.IsConsole))
            {
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
            }

            if (msWait <= 0) return -1;

            // 如果未退出，则不能拿到退出代码
            if (!p.WaitForExit(msWait)) return -1;

            return p.ExitCode;
        }
        #endregion
    }
}