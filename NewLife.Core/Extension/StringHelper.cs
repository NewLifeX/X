using System;
using System.Collections.Generic;
using System.ComponentModel;
using NewLife.Linq;

namespace System
{
    /// <summary>字符串助手类</summary>
    public static class StringHelper
    {
        #region 字符串扩展
        /// <summary>忽略大小写的字符串比较</summary>
        /// <param name="value"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Boolean EqualIgnoreCase(this String value, String str)
        {
            return String.Equals(value, str, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>是否空或者空白字符串</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Boolean IsNullOrWhiteSpace(this String value)
        {
            if (value != null)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    if (!char.IsWhiteSpace(value[i])) return false;
                }
            }
            return true;
        }

        /// <summary>拆分字符串</summary>
        /// <param name="value"></param>
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
            IDictionary<String, String> dic = new Dictionary<String, String>();
            if (str.IsNullOrWhiteSpace()) return dic;

            if (String.IsNullOrEmpty(nameValueSeparator)) nameValueSeparator = "=";
            if (separators == null || separators.Length < 1) separators = new String[] { ",", ";" };

            String[] ss = str.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            if (ss == null || ss.Length < 1) return null;

            foreach (String item in ss)
            {
                Int32 p = item.IndexOf(nameValueSeparator);
                // 在前后都不行
                if (p <= 0 || p >= item.Length - 1) continue;

                String key = item.Substring(0, p).Trim();
                dic[key] = item.Substring(p + 1).Trim();
            }

            return dic;
        }
        #endregion

        #region LD编辑距离算法
        /// <summary>
        /// 编辑距离搜索，从词组中找到最接近关键字的若干匹配项
        /// </summary>
        /// <param name="key">关键字</param>
        /// <param name="words">词组</param>
        /// <returns></returns>
        public static string[] LevenshteinSearch(string key, string[] words)
        {
            if (IsNullOrWhiteSpace(key)) return new string[0];

            string[] keys = key.Split(new char[] { ' ', '　' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string item in keys)
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

        /// <summary>
        /// 编辑距离，又称Levenshtein距离（也叫做Edit Distance），是指两个字串之间，由一个转成另一个所需的最少编辑操作次数。许可的编辑操作包括将一个字符替换成另一个字符，插入一个字符，删除一个字符。 
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <returns></returns>
        public static int LevenshteinDistance(string str1, string str2)
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
        /// <summary>
        /// 最长公共子序列搜索，从词组中找到最接近关键字的若干匹配项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="words"></param>
        /// <returns></returns>
        public static string[] LCSSearch(string key, string[] words)
        {
            if (IsNullOrWhiteSpace(key) || words == null || words.Length == 0) return new string[0];

            string[] keys = key
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
        /// <param name="word"></param>
        /// <param name="keys">多个关键字。长度必须大于0，必须按照字符串长度升序排列。</param>
        /// <returns></returns>
        public static int LCSDistance(string word, string[] keys)
        {
            int sLength = word.Length;
            int result = sLength;
            bool[] flags = new bool[sLength];
            int[,] C = new int[sLength + 1, keys[keys.Length - 1].Length + 1];
            //int[,] C = new int[sLength + 1, words.Select(s => s.Length).Max() + 1];
            foreach (string key in keys)
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