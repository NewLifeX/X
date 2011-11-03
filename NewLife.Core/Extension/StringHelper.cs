using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NewLife
{
    /// <summary>字符串助手类</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class StringHelper
    {
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
    }
}