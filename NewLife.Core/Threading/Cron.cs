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

        /// <summary>是否最后一天</summary>
        public Boolean LastDay { get; set; }

        /// <summary>是否工作日</summary>
        public Boolean Workday { get; set; }

        /// <summary>是否最后一个星期</summary>
        public Boolean LastWeekday { get; set; }

        /// <summary>第几个星期</summary>
        public Int32 WeekdayIndex { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化Cron表达式</summary>
        public Cron() { }

        /// <summary>实例化Cron表达式</summary>
        /// <param name="expressions"></param>
        public Cron(String expressions) => Parse(expressions);
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
        /// <param name="expressions"></param>
        /// <returns></returns>
        public Boolean Parse(String expressions)
        {
            var ss = expressions.Split(' ');
            if (ss.Length == 0) return false;

            if (!TryParse(ss[0], 0, 60, out var vs)) return false;
            Seconds = vs;
            if (!TryParse(ss.Length > 1 ? ss[1] : "*", 0, 60, out vs)) return false;
            Minutes = vs;
            if (!TryParse(ss.Length > 2 ? ss[2] : "*", 0, 24, out vs)) return false;
            Hours = vs;

            var value = ss.Length > 3 ? ss[3] : "*";
            if (value.EndsWithIgnoreCase("L", "LW")) LastDay = true;
            if (value.EndsWithIgnoreCase("W")) Workday = true;
            value = value.TrimEnd('L', 'W');
            if (!TryParse(!value.IsNullOrEmpty() ? value : "*", 1, 32, out vs)) return false;
            DaysOfMonth = vs;

            if (!TryParse(ss.Length > 4 ? ss[4] : "*", 1, 13, out vs)) return false;
            Months = vs;

            value = ss.Length > 5 ? ss[5] : "*";
            if (value.EndsWithIgnoreCase("L")) LastWeekday = true;
            if (value.Contains("#")) Workday = true;
            value = value.TrimEnd('L', 'W');
            if (!TryParse(value, 0, 7, out vs)) return false;
            DaysOfWeek = vs;

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
            if (value == "*" || value == "?")
            {
                start = 0;
            }
            else if ((p = value.IndexOf('-')) > 0)
            {
                start = value.Substring(0, p).ToInt();
                max = value.Substring(p + 1).ToInt() + 1;
            }
            else if (Int32.TryParse(value, out n))
            {
                start = n;
            }
            else if (value.EqualIgnoreCase("L"))
            {
                if (max == 7)
                    LastWeekday = true;
                else
                    LastDay = true;
            }
            else
                return false;

            for (var i = start; i < max; i += step)
                rs.Add(i);

            vs = rs.ToArray();
            return true;
        }
        #endregion
    }
}