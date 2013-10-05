using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Model;

namespace NewLife
{
    /// <summary>工具类</summary>
    /// <remarks>
    /// 采用对象容器架构，允许外部重载工具类的各种实现
    /// </remarks>
    public static class Utility
    {
        static Utility()
        {
            _covnert = ObjectContainer.Current.AutoRegister<DefaultConvert, DefaultConvert>().Resolve<DefaultConvert>();
        }

        #region 类型转换
        static DefaultConvert _covnert;

        /// <summary>转为整数</summary>
        /// <param name="value">待转换对象</param>
        /// <returns></returns>
        public static Int32 ToInt32(this Object value) { return _covnert.ToInt32(value, 0); }

        /// <summary>转为整数</summary>
        /// <param name="value">待转换对象</param>
        /// <param name="defaultValue">默认值。待转换对象无效时使用</param>
        /// <returns></returns>
        public static Int32 ToInt32(this Object value, Int32 defaultValue) { return _covnert.ToInt32(value, defaultValue); }

        /// <summary>转为布尔型</summary>
        /// <param name="value">待转换对象</param>
        /// <returns></returns>
        public static Boolean ToBoolean(this Object value) { return _covnert.ToBoolean(value, false); }

        /// <summary>转为布尔型</summary>
        /// <param name="value">待转换对象</param>
        /// <param name="defaultValue">默认值。待转换对象无效时使用</param>
        /// <returns></returns>
        public static Boolean ToBoolean(this Object value, Boolean defaultValue) { return _covnert.ToBoolean(value, defaultValue); }

        /// <summary>转为时间日期</summary>
        /// <param name="value">待转换对象</param>
        /// <returns></returns>
        public static DateTime ToDateTime(this Object value) { return _covnert.ToDateTime(value, DateTime.MinValue); }

        /// <summary>转为时间日期</summary>
        /// <param name="value">待转换对象</param>
        /// <param name="defaultValue">默认值。待转换对象无效时使用</param>
        /// <returns></returns>
        public static DateTime ToDateTime(this Object value, DateTime defaultValue) { return _covnert.ToDateTime(value, defaultValue); }
        #endregion
    }

    /// <summary>默认转换</summary>
    public class DefaultConvert
    {
        /// <summary>转为整数</summary>
        /// <param name="value">待转换对象</param>
        /// <param name="defaultValue">默认值。待转换对象无效时使用</param>
        /// <returns></returns>
        public virtual Int32 ToInt32(Object value, Int32 defaultValue)
        {
            if (value == null) return defaultValue;

            // 特殊处理字符串，也是最常见的
            if (value is String)
            {
                var str = value as String;
                if (String.IsNullOrEmpty(str)) return defaultValue;

                var n = defaultValue;
                if (Int32.TryParse(str, out n)) return n;
                return defaultValue;
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

        /// <summary>转为布尔型</summary>
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
                if (String.IsNullOrEmpty(str)) return defaultValue;

                var b = defaultValue;
                if (Boolean.TryParse(str, out b)) return b;

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
                if (String.IsNullOrEmpty(str)) return defaultValue;

                var n = defaultValue;
                if (DateTime.TryParse(str, out n)) return n;
                return defaultValue;
            }

            try
            {
                return Convert.ToDateTime(value);
            }
            catch { return defaultValue; }
        }
    }
}