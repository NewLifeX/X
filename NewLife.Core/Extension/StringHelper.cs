using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace NewLife
{
    /// <summary>
    /// 字符串助手类
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class StringHelper
    {
        /// <summary>
        /// 忽略大小写的字符串比较
        /// </summary>
        /// <param name="value"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Boolean EqualIgnoreCase(this String value, String str)
        {
            return String.Equals(value, str, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 是否空或者空白字符串
        /// </summary>
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

        //public static String[] Split(this String value)
        //{
        //    if (String.IsNullOrEmpty(value)) return null;

        //    return value.Split(new String[] { "" }, StringSplitOptions.RemoveEmptyEntries);
        //}
    }
}