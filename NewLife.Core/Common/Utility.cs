using System;
using System.ComponentModel;
using System.Globalization;

namespace System
{
    /// <summary>工具类</summary>
    /// <remarks>
    /// 采用静态架构，允许外部重载工具类的各种实现<seealso cref="DefaultConvert"/>。
    /// 所有类型转换均支持默认值，默认值为该default(T)，在转换失败时返回默认值。
    /// </remarks>
    public static class Utility
    {
        #region 类型转换
        private static DefaultConvert _Convert = new DefaultConvert();
        /// <summary>类型转换提供者</summary>
        /// <remarks>重载默认提供者<seealso cref="DefaultConvert"/>并赋值给<see cref="Convert"/>可改变所有类型转换的行为</remarks>
        public static DefaultConvert Convert { get { return _Convert; } set { _Convert = value; } }

        /// <summary>转为整数，转换失败时返回默认值。支持字符串、全角、字节数组（小端）</summary>
        /// <remarks>Int16/UInt32/Int64等，可以先转为最常用的Int32后再二次处理</remarks>
        /// <param name="value">待转换对象</param>
        /// <param name="defaultValue">默认值。待转换对象无效时使用</param>
        /// <returns></returns>
        public static Int32 ToInt(this Object value, Int32 defaultValue = 0) { return _Convert.ToInt(value, defaultValue); }

        /// <summary>转为布尔型，转换失败时返回默认值。支持大小写True/False、0和非零</summary>
        /// <param name="value">待转换对象</param>
        /// <param name="defaultValue">默认值。待转换对象无效时使用</param>
        /// <returns></returns>
        public static Boolean ToBoolean(this Object value, Boolean defaultValue = false) { return _Convert.ToBoolean(value, defaultValue); }

        /// <summary>转为时间日期，转换失败时返回最小时间</summary>
        /// <param name="value">待转换对象</param>
        /// <returns></returns>
        public static DateTime ToDateTime(this Object value) { return _Convert.ToDateTime(value, DateTime.MinValue); }

        /// <summary>转为时间日期，转换失败时返回默认值</summary>
        /// <remarks><see cref="DateTime.MinValue"/>不是常量无法做默认值</remarks>
        /// <param name="value">待转换对象</param>
        /// <param name="defaultValue">默认值。待转换对象无效时使用</param>
        /// <returns></returns>
        public static DateTime ToDateTime(this Object value, DateTime defaultValue) { return _Convert.ToDateTime(value, defaultValue); }

        /// <summary>时间日期转为yyyy-MM-dd HH:mm:ss完整字符串</summary>
        /// <remarks>最常用的时间日期格式，可以无视各平台以及系统自定义的时间格式</remarks>
        /// <param name="value">待转换对象</param>
        /// <returns></returns>
        public static String ToFullString(this DateTime value) { return _Convert.ToFullString(value); }
        #endregion
    }

    /// <summary>默认转换</summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public class DefaultConvert
    {
        /// <summary>转为整数</summary>
        /// <param name="value">待转换对象</param>
        /// <param name="defaultValue">默认值。待转换对象无效时使用</param>
        /// <returns></returns>
        public virtual Int32 ToInt(Object value, Int32 defaultValue)
        {
            if (value == null) return defaultValue;

            // 特殊处理字符串，也是最常见的
            if (value is String)
            {
                var str = value as String;
                str = ToDBC(str).Trim();
                if (String.IsNullOrEmpty(str)) return defaultValue;

                var n = defaultValue;
                if (Int32.TryParse(str, out n)) return n;
                return defaultValue;
            }
            else if (value is Byte[])
            {
                var buf = (Byte[])value;
                if (buf == null || buf.Length < 1) return defaultValue;

                switch (buf.Length)
                {
                    case 1:
                        return buf[0];
                    case 2:
                        return BitConverter.ToInt16(buf, 0);
                    case 3:
                        return BitConverter.ToInt32(new Byte[] { buf[0], buf[1], buf[2], 0 }, 0);
                    case 4:
                        return BitConverter.ToInt32(buf, 0);
                    default:
                        break;
                }
            }

            //var tc = Type.GetTypeCode(value.GetType());
            //if (tc >= TypeCode.Char && tc <= TypeCode.Decimal) return Convert.ToInt32(value);

            try
            {
                return Convert.ToInt32(value);
            }
            catch { return defaultValue; }
        }

        //static readonly String[] trueStr = new String[] { "True", "Y", "Yes", "On" };
        //static readonly String[] falseStr = new String[] { "False", "N", "N", "Off" };

        /// <summary>转为布尔型。支持大小写True/False、0和非零</summary>
        /// <param name="value">待转换对象</param>
        /// <param name="defaultValue">默认值。待转换对象无效时使用</param>
        /// <returns></returns>
        public virtual Boolean ToBoolean(Object value, Boolean defaultValue)
        {
            if (value == null) return defaultValue;

            // 特殊处理字符串，也是最常见的
            if (value is String)
            {
                var str = value as String;
                str = ToDBC(str).Trim();
                if (String.IsNullOrEmpty(str)) return defaultValue;

                var b = defaultValue;
                if (Boolean.TryParse(str, out b)) return b;

                if (String.Equals(str, Boolean.TrueString, StringComparison.OrdinalIgnoreCase)) return true;
                if (String.Equals(str, Boolean.FalseString, StringComparison.OrdinalIgnoreCase)) return false;

                // 特殊处理用数字0和1表示布尔型
                var n = 0;
                if (Int32.TryParse(str, out n)) return n > 0;

                return defaultValue;
            }

            try
            {
                return Convert.ToBoolean(value);
            }
            catch { return defaultValue; }
        }

        /// <summary>转为时间日期</summary>
        /// <param name="value">待转换对象</param>
        /// <param name="defaultValue">默认值。待转换对象无效时使用</param>
        /// <returns></returns>
        public virtual DateTime ToDateTime(Object value, DateTime defaultValue)
        {
            if (value == null) return defaultValue;

            // 特殊处理字符串，也是最常见的
            if (value is String)
            {
                var str = value as String;
                str = ToDBC(str).Trim();
                if (String.IsNullOrEmpty(str)) return defaultValue;

                var n = defaultValue;
                if (DateTime.TryParse(str, out n)) return n;
                if (str.Contains("-") && DateTime.TryParseExact(str, "yyyy-M-d", null, DateTimeStyles.None, out n)) return n;
                if (str.Contains("/") && DateTime.TryParseExact(str, "yyyy/M/d", null, DateTimeStyles.None, out n)) return n;
                if (DateTime.TryParse(str, out n)) return n;
                return defaultValue;
            }

            try
            {
                return Convert.ToDateTime(value);
            }
            catch { return defaultValue; }
        }

        /// <summary>全角为半角</summary>
        /// <remarks>全角半角的关系是相差0xFEE0</remarks>
        /// <param name="str"></param>
        /// <returns></returns>
        String ToDBC(String str)
        {
            var ch = str.ToCharArray();
            for (int i = 0; i < ch.Length; i++)
            {
                // 全角空格
                if (ch[i] == 0x3000)
                    ch[i] = (char)0x20;
                else if (ch[i] > 0xFF00 && ch[i] < 0xFF5F)
                    ch[i] = (char)(ch[i] - 0xFEE0);
            }
            return new string(ch);
        }

        /// <summary>时间日期转为yyyy-MM-dd HH:mm:ss完整字符串</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual String ToFullString(DateTime value) { return value.ToString("yyyy-MM-dd HH:mm:ss"); }
    }
}