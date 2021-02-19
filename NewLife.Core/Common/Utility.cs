using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NewLife;

namespace System
{
    /// <summary>工具类</summary>
    /// <remarks>
    /// 文档 https://www.yuque.com/smartstone/nx/utility
    /// 
    /// 采用静态架构，允许外部重载工具类的各种实现<seealso cref="DefaultConvert"/>。
    /// 所有类型转换均支持默认值，默认值为该default(T)，在转换失败时返回默认值。
    /// </remarks>
    public static class Utility
    {
        #region 类型转换
        /// <summary>类型转换提供者</summary>
        /// <remarks>重载默认提供者<seealso cref="DefaultConvert"/>并赋值给<see cref="Convert"/>可改变所有类型转换的行为</remarks>
        public static DefaultConvert Convert { get; set; } = new DefaultConvert();

        /// <summary>转为整数，转换失败时返回默认值。支持字符串、全角、字节数组（小端）、时间（Unix秒）</summary>
        /// <remarks>Int16/UInt32/Int64等，可以先转为最常用的Int32后再二次处理</remarks>
        /// <param name="value">待转换对象</param>
        /// <param name="defaultValue">默认值。待转换对象无效时使用</param>
        /// <returns></returns>
        public static Int32 ToInt(this Object value, Int32 defaultValue = 0) => Convert.ToInt(value, defaultValue);

        /// <summary>转为长整数，转换失败时返回默认值。支持字符串、全角、字节数组（小端）、时间（Unix毫秒）</summary>
        /// <remarks></remarks>
        /// <param name="value">待转换对象</param>
        /// <param name="defaultValue">默认值。待转换对象无效时使用</param>
        /// <returns></returns>
        public static Int64 ToLong(this Object value, Int64 defaultValue = 0) => Convert.ToLong(value, defaultValue);

        /// <summary>转为浮点数，转换失败时返回默认值。支持字符串、全角、字节数组（小端）</summary>
        /// <remarks>Single可以先转为最常用的Double后再二次处理</remarks>
        /// <param name="value">待转换对象</param>
        /// <param name="defaultValue">默认值。待转换对象无效时使用</param>
        /// <returns></returns>
        public static Double ToDouble(this Object value, Double defaultValue = 0) => Convert.ToDouble(value, defaultValue);

        /// <summary>转为布尔型，转换失败时返回默认值。支持大小写True/False、0和非零</summary>
        /// <param name="value">待转换对象</param>
        /// <param name="defaultValue">默认值。待转换对象无效时使用</param>
        /// <returns></returns>
        public static Boolean ToBoolean(this Object value, Boolean defaultValue = false) => Convert.ToBoolean(value, defaultValue);

        /// <summary>转为时间日期，转换失败时返回最小时间。支持字符串、整数（Unix秒）</summary>
        /// <param name="value">待转换对象</param>
        /// <returns></returns>
        public static DateTime ToDateTime(this Object value) => Convert.ToDateTime(value, DateTime.MinValue);

        /// <summary>转为时间日期，转换失败时返回默认值</summary>
        /// <remarks><see cref="DateTime.MinValue"/>不是常量无法做默认值</remarks>
        /// <param name="value">待转换对象</param>
        /// <param name="defaultValue">默认值。待转换对象无效时使用</param>
        /// <returns></returns>
        public static DateTime ToDateTime(this Object value, DateTime defaultValue) => Convert.ToDateTime(value, defaultValue);

        /// <summary>转为时间日期，转换失败时返回最小时间。支持字符串、整数（Unix秒）</summary>
        /// <param name="value">待转换对象</param>
        /// <returns></returns>
        public static DateTimeOffset ToDateTimeOffset(this Object value) => Convert.ToDateTimeOffset(value, DateTimeOffset.MinValue);

        /// <summary>转为时间日期，转换失败时返回默认值</summary>
        /// <remarks><see cref="DateTimeOffset.MinValue"/>不是常量无法做默认值</remarks>
        /// <param name="value">待转换对象</param>
        /// <param name="defaultValue">默认值。待转换对象无效时使用</param>
        /// <returns></returns>
        public static DateTimeOffset ToDateTimeOffset(this Object value, DateTimeOffset defaultValue) => Convert.ToDateTimeOffset(value, defaultValue);

        /// <summary>去掉时间日期秒后面部分，可指定毫秒</summary>
        /// <param name="value">时间日期</param>
        /// <param name="format">格式字符串，默认s格式化到秒，ms格式化到毫秒</param>
        /// <returns></returns>
        public static DateTime Trim(this DateTime value, String format = "s")
        {
            if (format == "s") return new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Kind);
            if (format == "ms") return new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Millisecond, value.Kind);

            return value;
        }

        /// <summary>去掉时间日期秒后面部分，可指定毫秒</summary>
        /// <param name="value">时间日期</param>
        /// <param name="format">格式字符串，默认s格式化到秒，ms格式化到毫秒</param>
        /// <returns></returns>
        public static DateTimeOffset Trim(this DateTimeOffset value, String format = "s") => new DateTimeOffset(value.DateTime.Trim(format), value.Offset);

        /// <summary>时间日期转为yyyy-MM-dd HH:mm:ss完整字符串，对UTC时间加后缀</summary>
        /// <remarks>最常用的时间日期格式，可以无视各平台以及系统自定义的时间格式</remarks>
        /// <param name="value">待转换对象</param>
        /// <returns></returns>
        public static String ToFullString(this DateTime value) => Convert.ToFullString(value);

        /// <summary>时间日期转为yyyy-MM-dd HH:mm:ss完整字符串，支持指定最小时间的字符串</summary>
        /// <remarks>最常用的时间日期格式，可以无视各平台以及系统自定义的时间格式</remarks>
        /// <param name="value">待转换对象</param>
        /// <param name="emptyValue">字符串空值时（DateTime.MinValue）显示的字符串，null表示原样显示最小时间，String.Empty表示不显示</param>
        /// <returns></returns>
        public static String ToFullString(this DateTime value, String emptyValue = null) => Convert.ToFullString(value, emptyValue);

        /// <summary>时间日期转为yyyy-MM-dd HH:mm:ss +08:00完整字符串，支持指定最小时间的字符串</summary>
        /// <remarks>最常用的时间日期格式，可以无视各平台以及系统自定义的时间格式</remarks>
        /// <param name="value">待转换对象</param>
        /// <param name="emptyValue">字符串空值时（DateTimeOffset.MinValue）显示的字符串，null表示原样显示最小时间，String.Empty表示不显示</param>
        /// <returns></returns>
        public static String ToFullString(this DateTimeOffset value, String emptyValue = null) => Convert.ToFullString(value, emptyValue);

        /// <summary>时间日期转为指定格式字符串</summary>
        /// <param name="value">待转换对象</param>
        /// <param name="format">格式化字符串</param>
        /// <param name="emptyValue">字符串空值时显示的字符串，null表示原样显示最小时间，String.Empty表示不显示</param>
        /// <returns></returns>
        public static String ToString(this DateTime value, String format, String emptyValue) => Convert.ToString(value, format, emptyValue);

        /// <summary>字节单位字符串</summary>
        /// <param name="value">数值</param>
        /// <param name="format">格式化字符串</param>
        /// <returns></returns>
        public static String ToGMK(this UInt64 value, String format = null) => Convert.ToGMK(value, format);

        /// <summary>字节单位字符串</summary>
        /// <param name="value">数值</param>
        /// <param name="format">格式化字符串</param>
        /// <returns></returns>
        public static String ToGMK(this Int64 value, String format = null) => value < 0 ? value + "" : Convert.ToGMK((UInt64)value, format);
        #endregion

        #region 异常处理
        /// <summary>获取内部真实异常</summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static Exception GetTrue(this Exception ex) => Convert.GetTrue(ex);

        /// <summary>获取异常消息</summary>
        /// <param name="ex">异常</param>
        /// <returns></returns>
        public static String GetMessage(this Exception ex) => Convert.GetMessage(ex);
        #endregion
    }

    /// <summary>默认转换</summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public class DefaultConvert
    {
        private static readonly DateTime _dt1970 = new DateTime(1970, 1, 1);
        private static readonly DateTimeOffset _dto1970 = new DateTimeOffset(new DateTime(1970, 1, 1));

        /// <summary>转为整数，转换失败时返回默认值。支持字符串、全角、字节数组（小端）、时间（Unix秒）</summary>
        /// <param name="value">待转换对象</param>
        /// <param name="defaultValue">默认值。待转换对象无效时使用</param>
        /// <returns></returns>
        public virtual Int32 ToInt(Object value, Int32 defaultValue)
        {
            if (value == null || value == DBNull.Value) return defaultValue;

            // 特殊处理字符串，也是最常见的
            if (value is String str)
            {
                // 拷贝而来的逗号分隔整数
                str = str.Replace(",", null);
                str = ToDBC(str).Trim();
                if (str.IsNullOrEmpty()) return defaultValue;

                if (Int32.TryParse(str, out var n)) return n;
                return defaultValue;
            }

            // 特殊处理时间，转Unix秒
            if (value is DateTime dt)
            {
                if (dt == DateTime.MinValue) return 0;
                if (dt == DateTime.MaxValue) return -1;

                //// 先转UTC时间再相减，以得到绝对时间差
                //return (Int32)(dt.ToUniversalTime() - _dt1970).TotalSeconds;
                return (Int32)(dt - _dt1970).TotalSeconds;
            }
            if (value is DateTimeOffset dto)
            {
                if (dto == DateTimeOffset.MinValue) return 0;

                return (Int32)(dto - _dto1970).TotalSeconds;
            }

            if (value is Byte[] buf)
            {
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

            try
            {
                return Convert.ToInt32(value);
            }
            catch { return defaultValue; }
        }

        /// <summary>转为长整数。支持字符串、全角、字节数组（小端）、时间（Unix毫秒）</summary>
        /// <param name="value">待转换对象</param>
        /// <param name="defaultValue">默认值。待转换对象无效时使用</param>
        /// <returns></returns>
        public virtual Int64 ToLong(Object value, Int64 defaultValue)
        {
            if (value == null || value == DBNull.Value) return defaultValue;

            // 特殊处理字符串，也是最常见的
            if (value is String str)
            {
                // 拷贝而来的逗号分隔整数
                str = str.Replace(",", null);
                str = ToDBC(str).Trim();
                if (str.IsNullOrEmpty()) return defaultValue;

                if (Int64.TryParse(str, out var n)) return n;
                return defaultValue;
            }

            // 特殊处理时间，转Unix毫秒
            if (value is DateTime dt)
            {
                if (dt == DateTime.MinValue) return 0;

                //// 先转UTC时间再相减，以得到绝对时间差
                //return (Int32)(dt.ToUniversalTime() - _dt1970).TotalSeconds;
                return (Int64)(dt - _dt1970).TotalMilliseconds;
            }
            if (value is DateTimeOffset dto)
            {
                if (dto == DateTimeOffset.MinValue) return 0;

                return (Int64)(dto - _dto1970).TotalMilliseconds;
            }

            if (value is Byte[] buf)
            {
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
                    case 8:
                        return BitConverter.ToInt64(buf, 0);
                    default:
                        break;
                }
            }

            //暂时不做处理  先处理异常转换
            try
            {
                return Convert.ToInt64(value);
            }
            catch { return defaultValue; }
        }

        /// <summary>转为浮点数</summary>
        /// <param name="value">待转换对象</param>
        /// <param name="defaultValue">默认值。待转换对象无效时使用</param>
        /// <returns></returns>
        public virtual Double ToDouble(Object value, Double defaultValue)
        {
            if (value == null || value == DBNull.Value) return defaultValue;

            // 特殊处理字符串，也是最常见的
            if (value is String str)
            {
                str = ToDBC(str).Trim();
                if (str.IsNullOrEmpty()) return defaultValue;

                if (Double.TryParse(str, out var n)) return n;
                return defaultValue;
            }
            else if (value is Byte[] buf)
            {
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
                        // 凑够8字节
                        if (buf.Length < 8)
                        {
                            var bts = new Byte[8];
                            Buffer.BlockCopy(buf, 0, bts, 0, buf.Length);
                            buf = bts;
                        }
                        return BitConverter.ToDouble(buf, 0);
                }
            }

            try
            {
                return Convert.ToDouble(value);
            }
            catch { return defaultValue; }
        }

        /// <summary>转为布尔型。支持大小写True/False、0和非零</summary>
        /// <param name="value">待转换对象</param>
        /// <param name="defaultValue">默认值。待转换对象无效时使用</param>
        /// <returns></returns>
        public virtual Boolean ToBoolean(Object value, Boolean defaultValue)
        {
            if (value == null || value == DBNull.Value) return defaultValue;

            // 特殊处理字符串，也是最常见的
            if (value is String str)
            {
                //str = ToDBC(str).Trim();
                str = str.Trim();
                if (str.IsNullOrEmpty()) return defaultValue;

                if (Boolean.TryParse(str, out var b)) return b;

                if (String.Equals(str, Boolean.TrueString, StringComparison.OrdinalIgnoreCase)) return true;
                if (String.Equals(str, Boolean.FalseString, StringComparison.OrdinalIgnoreCase)) return false;

                // 特殊处理用数字0和1表示布尔型
                str = ToDBC(str);
                if (Int32.TryParse(str, out var n)) return n > 0;

                return defaultValue;
            }

            try
            {
                return Convert.ToBoolean(value);
            }
            catch { return defaultValue; }
        }

        /// <summary>转为时间日期，转换失败时返回最小时间。支持字符串、整数（Unix秒）</summary>
        /// <param name="value">待转换对象</param>
        /// <param name="defaultValue">默认值。待转换对象无效时使用</param>
        /// <returns></returns>
        public virtual DateTime ToDateTime(Object value, DateTime defaultValue)
        {
            if (value == null || value == DBNull.Value) return defaultValue;

            // 特殊处理字符串，也是最常见的

            if (value is String str)
            {
                //str = ToDBC(str).Trim();
                str = str.Trim();
                if (str.IsNullOrEmpty()) return defaultValue;

                // 处理UTC
                var utc = false;
                if (str.EndsWithIgnoreCase(" UTC"))
                {
                    utc = true;
                    str = str.Substring(0, str.Length - 4);
                }

                if (!DateTime.TryParse(str, out var dt) &&
                    !str.Contains("-") && DateTime.TryParseExact(str, "yyyy-M-d", null, DateTimeStyles.None, out dt) &&
                    !str.Contains("/") && DateTime.TryParseExact(str, "yyyy/M/d", null, DateTimeStyles.None, out dt) &&
                    !DateTime.TryParse(str, out dt))
                {
                    dt = defaultValue;
                }

                // 处理UTC
                if (utc) dt = new DateTime(dt.Ticks, DateTimeKind.Utc);

                return dt;
            }
            // 特殊处理整数，Unix秒，绝对时间差，不考虑UTC时间和本地时间。
            if (value is Int32 k) return k == 0 ? DateTime.MinValue : _dt1970.AddSeconds(k);
            if (value is Int64 m)
            {
                if (m > 100 * 365 * 24 * 3600L)
                    return _dt1970.AddMilliseconds(m);
                else
                    return _dt1970.AddSeconds(m);
            }

            try
            {
                return Convert.ToDateTime(value);
            }
            catch { return defaultValue; }
        }

        /// <summary>转为时间日期，转换失败时返回最小时间。支持字符串、整数（Unix秒）</summary>
        /// <param name="value">待转换对象</param>
        /// <param name="defaultValue">默认值。待转换对象无效时使用</param>
        /// <returns></returns>
        public virtual DateTimeOffset ToDateTimeOffset(Object value, DateTimeOffset defaultValue)
        {
            if (value == null || value == DBNull.Value) return defaultValue;

            // 特殊处理字符串，也是最常见的

            if (value is String str)
            {
                str = str.Trim();
                if (str.IsNullOrEmpty()) return defaultValue;

                if (DateTimeOffset.TryParse(str, out var dt)) return dt;
                if (str.Contains("-") && DateTimeOffset.TryParseExact(str, "yyyy-M-d", null, DateTimeStyles.None, out dt)) return dt;
                if (str.Contains("/") && DateTimeOffset.TryParseExact(str, "yyyy/M/d", null, DateTimeStyles.None, out dt)) return dt;

                return defaultValue;
            }
            // 特殊处理整数，Unix秒，绝对时间差，不考虑UTC时间和本地时间。
            if (value is Int32 k) return k == 0 ? DateTimeOffset.MinValue : _dto1970.AddSeconds(k);
            if (value is Int64 m)
            {
                if (m > 100 * 365 * 24 * 3600L)
                    return _dto1970.AddMilliseconds(m);
                else
                    return _dto1970.AddSeconds(m);
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
        private static String ToDBC(String str)
        {
            var ch = str.ToCharArray();
            for (var i = 0; i < ch.Length; i++)
            {
                // 全角空格
                if (ch[i] == 0x3000)
                    ch[i] = (Char)0x20;
                else if (ch[i] > 0xFF00 && ch[i] < 0xFF5F)
                    ch[i] = (Char)(ch[i] - 0xFEE0);
            }
            return new String(ch);
        }

        /// <summary>时间日期转为yyyy-MM-dd HH:mm:ss完整字符串</summary>
        /// <param name="value">待转换对象</param>
        /// <param name="emptyValue">字符串空值时显示的字符串，null表示原样显示最小时间，String.Empty表示不显示</param>
        /// <returns></returns>
        public virtual String ToFullString(DateTime value, String emptyValue = null)
        {
            if (emptyValue != null && value <= DateTime.MinValue) return emptyValue;

            //return value.ToString("yyyy-MM-dd HH:mm:ss");

            //var dt = value;
            //var sb = new StringBuilder();
            //sb.Append(dt.Year.ToString().PadLeft(4, '0'));
            //sb.Append("-");
            //sb.Append(dt.Month.ToString().PadLeft(2, '0'));
            //sb.Append("-");
            //sb.Append(dt.Day.ToString().PadLeft(2, '0'));
            //sb.Append(" ");

            //sb.Append(dt.Hour.ToString().PadLeft(2, '0'));
            //sb.Append(":");
            //sb.Append(dt.Minute.ToString().PadLeft(2, '0'));
            //sb.Append(":");
            //sb.Append(dt.Second.ToString().PadLeft(2, '0'));

            //return sb.ToString();

            var cs = "yyyy-MM-dd HH:mm:ss".ToCharArray();

            var k = 0;
            var y = value.Year;
            cs[k++] = (Char)('0' + (y / 1000));
            y %= 1000;
            cs[k++] = (Char)('0' + (y / 100));
            y %= 100;
            cs[k++] = (Char)('0' + (y / 10));
            y %= 10;
            cs[k++] = (Char)('0' + y);
            k++;

            var m = value.Month;
            cs[k++] = (Char)('0' + (m / 10));
            cs[k++] = (Char)('0' + (m % 10));
            k++;

            m = value.Day;
            cs[k++] = (Char)('0' + (m / 10));
            cs[k++] = (Char)('0' + (m % 10));
            k++;

            m = value.Hour;
            cs[k++] = (Char)('0' + (m / 10));
            cs[k++] = (Char)('0' + (m % 10));
            k++;

            m = value.Minute;
            cs[k++] = (Char)('0' + (m / 10));
            cs[k++] = (Char)('0' + (m % 10));
            k++;

            m = value.Second;
            cs[k++] = (Char)('0' + (m / 10));
            cs[k++] = (Char)('0' + (m % 10));

            var str = new String(cs);

            // 此格式不受其它工具识别只存不包含时区的格式
            // 取出后，业务上存的是utc取出来再当utc即可
            //if (value.Kind == DateTimeKind.Utc) str += " UTC";

            return str;
        }

        /// <summary>时间日期转为yyyy-MM-dd HH:mm:ss完整字符串</summary>
        /// <param name="value">待转换对象</param>
        /// <param name="emptyValue">字符串空值时显示的字符串，null表示原样显示最小时间，String.Empty表示不显示</param>
        /// <returns></returns>
        public virtual String ToFullString(DateTimeOffset value, String emptyValue = null)
        {
            if (emptyValue != null && value <= DateTimeOffset.MinValue) return emptyValue;

            var cs = "yyyy-MM-dd HH:mm:ss +08:00".ToCharArray();

            var k = 0;
            var y = value.Year;
            cs[k++] = (Char)('0' + (y / 1000));
            y %= 1000;
            cs[k++] = (Char)('0' + (y / 100));
            y %= 100;
            cs[k++] = (Char)('0' + (y / 10));
            y %= 10;
            cs[k++] = (Char)('0' + y);
            k++;

            var m = value.Month;
            cs[k++] = (Char)('0' + (m / 10));
            cs[k++] = (Char)('0' + (m % 10));
            k++;

            m = value.Day;
            cs[k++] = (Char)('0' + (m / 10));
            cs[k++] = (Char)('0' + (m % 10));
            k++;

            m = value.Hour;
            cs[k++] = (Char)('0' + (m / 10));
            cs[k++] = (Char)('0' + (m % 10));
            k++;

            m = value.Minute;
            cs[k++] = (Char)('0' + (m / 10));
            cs[k++] = (Char)('0' + (m % 10));
            k++;

            m = value.Second;
            cs[k++] = (Char)('0' + (m / 10));
            cs[k++] = (Char)('0' + (m % 10));
            k++;

            // 时区
            var offset = value.Offset;
            cs[k++] = offset.TotalSeconds >= 0 ? '+' : '-';
            m = Math.Abs(offset.Hours);
            cs[k++] = (Char)('0' + (m / 10));
            cs[k++] = (Char)('0' + (m % 10));
            k++;
            m = Math.Abs(offset.Minutes);
            cs[k++] = (Char)('0' + (m / 10));
            cs[k++] = (Char)('0' + (m % 10));

            return new String(cs);
        }

        /// <summary>时间日期转为指定格式字符串</summary>
        /// <param name="value">待转换对象</param>
        /// <param name="format">格式化字符串</param>
        /// <param name="emptyValue">字符串空值时显示的字符串，null表示原样显示最小时间，String.Empty表示不显示</param>
        /// <returns></returns>
        public virtual String ToString(DateTime value, String format, String emptyValue)
        {
            if (emptyValue != null && value <= DateTime.MinValue) return emptyValue;

            //return value.ToString(format ?? "yyyy-MM-dd HH:mm:ss");

            if (format == null || format == "yyyy-MM-dd HH:mm:ss") return ToFullString(value, emptyValue);

            return value.ToString(format);
        }

        /// <summary>获取内部真实异常</summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public virtual Exception GetTrue(Exception ex)
        {
            if (ex == null) return null;

            if (ex is AggregateException)
                return GetTrue((ex as AggregateException).Flatten().InnerException);

            if (ex is TargetInvocationException)
                return GetTrue((ex as TargetInvocationException).InnerException);

            if (ex is TypeInitializationException)
                return GetTrue((ex as TypeInitializationException).InnerException);

            return ex.GetBaseException() ?? ex;
        }

        /// <summary>获取异常消息</summary>
        /// <param name="ex">异常</param>
        /// <returns></returns>
        public virtual String GetMessage(Exception ex)
        {
            var msg = ex + "";
            if (msg.IsNullOrEmpty()) return null;

            var ss = msg.Split(Environment.NewLine);
            var ns = ss.Where(e =>
            !e.StartsWith("---") &&
            !e.Contains("System.Runtime.ExceptionServices") &&
            !e.Contains("System.Runtime.CompilerServices"));

            msg = ns.Join(Environment.NewLine);

            return msg;
        }

        /// <summary>字节单位字符串</summary>
        /// <param name="value">数值</param>
        /// <param name="format">格式化字符串</param>
        /// <returns></returns>
        public virtual String ToGMK(UInt64 value, String format = null)
        {
            if (value < 1024) return $"{value:n0}";

            if (format.IsNullOrEmpty()) format = "n2";

            var val = value / 1024d;
            if (val < 1024) return val.ToString(format) + "K";

            val /= 1024;
            if (val < 1024) return val.ToString(format) + "M";

            val /= 1024;
            if (val < 1024) return val.ToString(format) + "G";

            val /= 1024;
            if (val < 1024) return val.ToString(format) + "T";

            val /= 1024;
            if (val < 1024) return val.ToString(format) + "P";

            val /= 1024;
            return val.ToString(format) + "E";
        }
    }
}