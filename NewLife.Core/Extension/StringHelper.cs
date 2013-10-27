using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace System
{
    /// <summary>字符串助手类</summary>
    public static class StringHelper
    {
        #region 字符串扩展
        ///// <summary>忽略大小写的字符串相等比较</summary>
        ///// <param name="value">数值</param>
        ///// <param name="str">待比较字符串</param>
        ///// <returns></returns>
        //[Obsolete("=>EqualIC")]
        ////[EditorBrowsable(EditorBrowsableState.Never)]
        //public static Boolean EqualIgnoreCase(this String value, String str)
        //{
        //    return String.Equals(value, str, StringComparison.OrdinalIgnoreCase);
        //}

        ///// <summary>忽略大小写的字符串开始比较，判断是否以任意一个待比较字符串开始</summary>
        ///// <param name="value">数值</param>
        ///// <param name="str">待比较字符串</param>
        ///// <returns></returns>
        //[Obsolete("=>StartsWithIC")]
        //public static Boolean StartsWithIgnoreCase(this String value, String str)
        //{
        //    return value.StartsWith(str, StringComparison.OrdinalIgnoreCase);
        //}

        ///// <summary>忽略大小写的字符串结束比较，判断是否以任意一个待比较字符串结束</summary>
        ///// <param name="value">数值</param>
        ///// <param name="str">待比较字符串</param>
        ///// <returns></returns>
        //[Obsolete("=>EndsWithIC")]
        //public static Boolean EndsWithIgnoreCase(this String value, String str)
        //{
        //    return value.EndsWith(str, StringComparison.OrdinalIgnoreCase);
        //}

        /// <summary>忽略大小写的字符串相等比较，判断是否以任意一个待比较字符串相等</summary>
        /// <param name="value">数值</param>
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
        /// <param name="value">数值</param>
        /// <param name="strs">待比较字符串数组</param>
        /// <returns></returns>
        public static Boolean StartsWithIgnoreCase(this String value, params String[] strs)
        {
            foreach (var item in strs)
            {
                if (value.StartsWith(item, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        /// <summary>忽略大小写的字符串结束比较，判断是否以任意一个待比较字符串结束</summary>
        /// <param name="value">数值</param>
        /// <param name="strs">待比较字符串数组</param>
        /// <returns></returns>
        public static Boolean EndsWithIgnoreCase(this String value, params String[] strs)
        {
            foreach (var item in strs)
            {
                if (value.EndsWith(item, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        /// <summary>是否空或者空白字符串</summary>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static Boolean IsNullOrWhiteSpace(this String value)
        {
            if (value != null)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    if (!Char.IsWhiteSpace(value[i])) return false;
                }
            }
            return true;
        }

        /// <summary>拆分字符串</summary>
        /// <param name="value">数值</param>
        /// <param name="separators"></param>
        /// <returns></returns>
        public static String[] Split(this String value, params String[] separators)
        {
            if (String.IsNullOrEmpty(value)) return new String[0];

            return value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>拆分字符串成为名值字典</summary>
        /// <param name="str"></param>
        /// <param name="nameValueSeparator"></param>
        /// <param name="separators"></param>
        /// <returns></returns>
        public static IDictionary<String, String> SplitAsDictionary(this String str, String nameValueSeparator = "=", params String[] separators)
        {
            var dic = new Dictionary<String, String>();
            if (str.IsNullOrWhiteSpace()) return dic;

            if (String.IsNullOrEmpty(nameValueSeparator)) nameValueSeparator = "=";
            if (separators == null || separators.Length < 1) separators = new String[] { ",", ";" };

            String[] ss = str.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            if (ss == null || ss.Length < 1) return null;

            foreach (var item in ss)
            {
                Int32 p = item.IndexOf(nameValueSeparator);
                // 在前后都不行
                if (p <= 0 || p >= item.Length - 1) continue;

                String key = item.Substring(0, p).Trim();
                dic[key] = item.Substring(p + nameValueSeparator.Length).Trim();
            }

            return dic;
        }

        /// <summary>追加分隔符字符串，除了开头</summary>
        /// <param name="sb"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static StringBuilder AppendSeparate(this StringBuilder sb, String str)
        {
            if (sb == null || String.IsNullOrEmpty(str)) return sb;

            if (sb.Length > 0) sb.Append(str);

            return sb;
        }
        #endregion

        #region 截取扩展
        ///// <summary>截取左边若干长度字符串</summary>
        ///// <param name="str"></param>
        ///// <param name="length"></param>
        ///// <returns></returns>
        //public static String Left(this String str, Int32 length)
        //{
        //    if (String.IsNullOrEmpty(str) || length <= 0) return str;

        //    // 纠正长度
        //    if (str.Length <= length) return str;

        //    return str.Substring(0, length);
        //}

        ///// <summary>截取左边若干长度字符串（二进制计算长度）</summary>
        ///// <param name="str"></param>
        ///// <param name="length"></param>
        ///// <param name="strict">严格模式时，遇到截断位置位于一个字符中间时，忽略该字符，否则包括该字符</param>
        ///// <returns></returns>
        //public static String LeftBinary(this String str, Int32 length, Boolean strict = true)
        //{
        //    if (String.IsNullOrEmpty(str) || length <= 0) return str;

        //    // 纠正长度
        //    if (str.Length <= length) return str;

        //    var encoding = Encoding.Default;

        //    var buf = encoding.GetBytes(str);
        //    if (buf.Length < length) return str;

        //    // 计算截取字符长度。避免把一个字符劈开
        //    var clen = 0;
        //    while (true)
        //    {
        //        try
        //        {
        //            clen = encoding.GetCharCount(buf, 0, length);
        //            break;
        //        }
        //        catch (DecoderFallbackException)
        //        {
        //            // 发生了回退，减少len再试
        //            length--;
        //        }
        //    }
        //    // 可能过长，修正
        //    if (strict) while (encoding.GetByteCount(str.ToCharArray(), 0, clen) > length) clen--;

        //    return str.Substring(0, clen);
        //}

        ///// <summary>截取右边若干长度字符串</summary>
        ///// <param name="str"></param>
        ///// <param name="length"></param>
        ///// <returns></returns>
        //public static String Right(this String str, Int32 length)
        //{
        //    if (String.IsNullOrEmpty(str) || length <= 0) return str;

        //    // 纠正长度
        //    if (str.Length <= length) return str;

        //    return str.Substring(str.Length - length, length);
        //}

        ///// <summary>截取右边若干长度字符串（二进制计算长度）</summary>
        ///// <param name="str"></param>
        ///// <param name="length"></param>
        ///// <param name="strict">严格模式时，遇到截断位置位于一个字符中间时，忽略该字符，否则包括该字符</param>
        ///// <returns></returns>
        //public static String RightBinary(this String str, Int32 length, Boolean strict = true)
        //{
        //    if (String.IsNullOrEmpty(str) || length <= 0) return str;

        //    // 纠正长度
        //    if (str.Length <= length) return str;

        //    var encoding = Encoding.Default;

        //    var buf = encoding.GetBytes(str);
        //    if (buf.Length < length) return str;

        //    // 计算截取字符长度。避免把一个字符劈开
        //    var clen = 0;
        //    while (true)
        //    {
        //        try
        //        {
        //            clen = encoding.GetCharCount(buf, buf.Length - length, length);
        //            break;
        //        }
        //        catch (DecoderFallbackException)
        //        {
        //            // 发生了回退，减少len再试
        //            length--;
        //        }
        //    }
        //    //// 可能过长，修正
        //    //if (strict) while (encoding.GetByteCount(str.ToCharArray(), str.Length - clen, clen) > length) clen--;
        //    // 可能过短，修正
        //    if (!strict) while (encoding.GetByteCount(str.ToCharArray(), str.Length - clen, clen) < length) clen++;

        //    return str.Substring(str.Length - clen, clen);
        //}

        /// <summary>确保字符串以指定的另一字符串开始，不区分大小写</summary>
        /// <param name="str"></param>
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
        /// <param name="str"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static String EnsureEnd(this String str, String end)
        {
            if (String.IsNullOrEmpty(end)) return str;
            if (String.IsNullOrEmpty(str)) return end;

            if (str.EndsWith(end, StringComparison.OrdinalIgnoreCase)) return str;

            return str + end;
        }

        /// <summary>从当前字符串开头移除另一字符串，不区分大小写</summary>
        /// <param name="str">当前字符串</param>
        /// <param name="starts">另一字符串</param>
        /// <returns></returns>
        public static String TrimStart(this String str, params String[] starts)
        {
            if (String.IsNullOrEmpty(str)) return str;
            if (starts == null || starts.Length < 1 || String.IsNullOrEmpty(starts[0])) return str;

            //if (!str.StartsWith(start, StringComparison.OrdinalIgnoreCase)) return str;

            //return str.Substring(start.Length);

            for (int i = 0; i < starts.Length; i++)
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

        /// <summary>从当前字符串结尾移除另一字符串，不区分大小写</summary>
        /// <param name="str">当前字符串</param>
        /// <param name="ends">另一字符串</param>
        /// <returns></returns>
        public static String TrimEnd(this String str, params String[] ends)
        {
            if (String.IsNullOrEmpty(str)) return str;
            if (ends == null || ends.Length < 1 || String.IsNullOrEmpty(ends[0])) return str;

            //if (!str.EndsWith(end, StringComparison.OrdinalIgnoreCase)) return str;

            //return str.Substring(0, str.Length - end.Length);

            for (int i = 0; i < ends.Length; i++)
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

        /// <summary>根据最大长度截取字符串，并允许以指定空白填充末尾</summary>
        /// <param name="str"></param>
        /// <param name="maxLength"></param>
        /// <param name="pad"></param>
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

        /// <summary>根据最大长度截取字符串（二进制计算长度），并允许以指定空白填充末尾</summary>
        /// <param name="str"></param>
        /// <param name="maxLength"></param>
        /// <param name="pad"></param>
        /// <param name="strict">严格模式时，遇到截断位置位于一个字符中间时，忽略该字符，否则包括该字符</param>
        /// <returns></returns>
        public static String CutBinary(this String str, Int32 maxLength, String pad = null, Boolean strict = true)
        {
            if (String.IsNullOrEmpty(str) || maxLength <= 0 || str.Length < maxLength) return str;

            var encoding = Encoding.Default;

            var buf = encoding.GetBytes(str);
            if (buf.Length < maxLength) return str;

            // 计算截取字节长度
            var len = maxLength;
            if (!String.IsNullOrEmpty(pad)) len -= encoding.GetByteCount(pad);
            if (len <= 0) return pad;

            // 计算截取字符长度。避免把一个字符劈开
            var clen = 0;
            while (true)
            {
                try
                {
                    clen = encoding.GetCharCount(buf, 0, len);
                    break;
                }
                catch (DecoderFallbackException)
                {
                    // 发生了回退，减少len再试
                    len--;
                }
            }
            // 可能过长，修正
            if (strict) while (encoding.GetByteCount(str.ToCharArray(), 0, clen) > len) clen--;

            return str.Substring(0, clen) + pad;
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

            String[] keys = key.Split(new char[] { ' ', '　' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (String item in keys)
            {
                int maxDist = (item.Length - 1) / 2;

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

        /// <summary>编辑距离，又称Levenshtein距离（也叫做Edit Distance），是指两个字串之间，由一个转成另一个所需的最少编辑操作次数。许可的编辑操作包括将一个字符替换成另一个字符，插入一个字符，删除一个字符。</summary>
        /// <remarks>
        /// 算法代码由@Aimeast 独立完成。http://www.cnblogs.com/Aimeast/archive/2011/09/05/2167844.html
        /// </remarks>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <returns></returns>
        public static int LevenshteinDistance(String str1, String str2)
        {
            int n = str1.Length;
            int m = str2.Length;
            int[,] C = new int[n + 1, m + 1];
            int i, j, x, y, z;
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

            String[] keys = key
                                .Split(new char[] { ' ', '\u3000' }, StringSplitOptions.RemoveEmptyEntries)
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
        public static int LCSDistance(String word, String[] keys)
        {
            int sLength = word.Length;
            int result = sLength;
            bool[] flags = new bool[sLength];
            int[,] C = new int[sLength + 1, keys[keys.Length - 1].Length + 1];
            //int[,] C = new int[sLength + 1, words.Select(s => s.Length).Max() + 1];
            foreach (String key in keys)
            {
                int wLength = key.Length;
                int first = 0, last = 0;
                int i = 0, j = 0, LCS_L;
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
        #endregion
    }
}