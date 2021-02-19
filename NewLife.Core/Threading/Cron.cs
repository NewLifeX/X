using System;
using System.Collections.Generic;
using System.Linq;

namespace NewLife.Threading
{
    /// <summary>Cron表达式</summary>
    public class Cron
    {
        #region 属性
        /// <summary>秒数集合</summary>
        public Int32[] Seconds;

        /// <summary>分钟集合</summary>
        public Int32[] Minutes;

        /// <summary>小时集合</summary>
        public Int32[] Hours;

        /// <summary>日期集合</summary>
        public Int32[] DaysOfMonth;

        /// <summary>月份集合</summary>
        public Int32[] Months;

        /// <summary>星期集合</summary>
        public Int32[] DaysOfWeek;

        private String _expression;
        #endregion

        #region 构造
        /// <summary>实例化Cron表达式</summary>
        public Cron() { }

        /// <summary>实例化Cron表达式</summary>
        /// <param name="expression"></param>
        public Cron(String expression) => Parse(expression);

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => _expression;
        #endregion

        #region 方法
        /// <summary>指定时间是否位于表达式之内</summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public Boolean IsTime(DateTime time)
        {
            return Seconds.Contains(time.Second) &&
                   Minutes.Contains(time.Minute) &&
                   Hours.Contains(time.Hour) &&
                   DaysOfMonth.Contains(time.Day) &&
                   Months.Contains(time.Month) &&
                   DaysOfWeek.Contains((Int32)time.DayOfWeek);
        }

        /// <summary>分析表达式</summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public Boolean Parse(String expression)
        {
            var ss = expression.Split(' ');
            if (ss.Length == 0) return false;

            if (!TryParse(ss[0], 0, 60, out var vs)) return false;
            Seconds = vs;
            if (!TryParse(ss.Length > 1 ? ss[1] : "*", 0, 60, out vs)) return false;
            Minutes = vs;
            if (!TryParse(ss.Length > 2 ? ss[2] : "*", 0, 24, out vs)) return false;
            Hours = vs;
            if (!TryParse(ss.Length > 3 ? ss[3] : "*", 1, 32, out vs)) return false;
            DaysOfMonth = vs;
            if (!TryParse(ss.Length > 4 ? ss[4] : "*", 1, 13, out vs)) return false;
            Months = vs;
            if (!TryParse(ss.Length > 5 ? ss[5] : "*", 0, 7, out vs)) return false;
            DaysOfWeek = vs;

            _expression = expression;

            return true;
        }

        private Boolean TryParse(String value, Int32 start, Int32 max, out Int32[] vs)
        {
            // 固定值，最为常见，优先计算
            if (Int32.TryParse(value, out var n)) { vs = new Int32[] { n }; return true; }

            var rs = new List<Int32>();
            vs = null;

            // 递归处理混合值
            if (value.Contains(','))
            {
                foreach (var item in value.Split(','))
                {
                    if (!TryParse(item, start, max, out var arr)) return false;
                    if (arr.Length > 0) rs.AddRange(arr);
                }
                vs = rs.ToArray();
                return true;
            }

            // 步进值
            var step = 1;
            var p = value.IndexOf('/');
            if (p > 0)
            {
                step = value.Substring(p + 1).ToInt();
                value = value.Substring(0, p);
            }

            // 连续范围
            var s = start;
            if (value == "*" || value == "?")
            {
                s = 0;
            }
            else if ((p = value.IndexOf('-')) > 0)
            {
                s = value.Substring(0, p).ToInt();
                max = value.Substring(p + 1).ToInt() + 1;
            }
            else if (Int32.TryParse(value, out n))
            {
                s = n;
            }
            else
                return false;

            for (var i = s; i < max; i += step)
            {
                if (i >= start) rs.Add(i);
            }

            vs = rs.ToArray();
            return true;
        }

        /// <summary>获得指定时间之后的下一次执行时间，不含指定时间</summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public DateTime GetNext(DateTime time)
        {
            // 设置末尾，避免死循环越界
            var end = time.AddYears(1);
            for (var dt = time.Trim().AddSeconds(1); dt < end; dt = dt.AddSeconds(1))
            {
                if (IsTime(dt)) return dt;
            }

            return DateTime.MinValue;
        }
        #endregion
    }
}