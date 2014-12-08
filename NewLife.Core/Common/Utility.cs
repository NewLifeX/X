using System;
using System.Globalization;

namespace NewLife
{
    /// <summary>工具类</summary>
    /// <remarks>
    /// 采用对象容器架构，允许外部重载工具类的各种实现
    /// </remarks>
    public static class Utility
    {
        //static Utility()
        //{
        //    _Convert = ObjectContainer.Current.AutoRegister<DefaultConvert, DefaultConvert>().Resolve<DefaultConvert>();
        //}

        #region 类型转换
        private static DefaultConvert _Convert = new DefaultConvert();
        /// <summary>类型转换提供者</summary>
        public static DefaultConvert Convert { get { return _Convert; } set { _Convert = value; } }

        ///// <summary>转为整数</summary>
        ///// <param name="value">待转换对象</param>
        ///// <returns></returns>
        //public static Int32 ToInt32(this Object value) { return _Convert.ToInt32(value, 0); }

        /// <summary>转为整数</summary>
        /// <param name="value">待转换对象</param>
        /// <param name="defaultValue">默认值。待转换对象无效时使用</param>
        /// <returns></returns>
        public static Int32 ToInt(this Object value, Int32 defaultValue = 0) { return _Convert.ToInt(value, defaultValue); }

        ///// <summary>转为布尔型</summary>
        ///// <param name="value">待转换对象</param>
        ///// <returns></returns>
        //public static Boolean ToBoolean(this Object value) { return _Convert.ToBoolean(value, false); }

        /// <summary>转为布尔型。支持大小写True/False、0和非零</summary>
        /// <param name="value">待转换对象</param>
        /// <param name="defaultValue">默认值。待转换对象无效时使用</param>
        /// <returns></returns>
        public static Boolean ToBoolean(this Object value, Boolean defaultValue = false) { return _Convert.ToBoolean(value, defaultValue); }

        /// <summary>转为时间日期</summary>
        /// <param name="value">待转换对象</param>
        /// <returns></returns>
        public static DateTime ToDateTime(this Object value) { return _Convert.ToDateTime(value, DateTime.MinValue); }

        /// <summary>转为时间日期</summary>
        /// <param name="value">待转换对象</param>
        /// <param name="defaultValue">默认值。待转换对象无效时使用</param>
        /// <returns></returns>
        public static DateTime ToDateTime(this Object value, DateTime defaultValue) { return _Convert.ToDateTime(value, defaultValue); }
        #endregion
    }

    /// <summary>默认转换</summary>
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
    }
}