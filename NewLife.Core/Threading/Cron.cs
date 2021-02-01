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
        public List<Int32> Seconds;

        /// <summary>分钟集合</summary>
        public List<Int32> Minutes;

        /// <summary>小时集合</summary>
        public List<Int32> Hours;

        /// <summary>日期集合</summary>
        public List<Int32> DaysOfMonth;

        /// <summary>月份集合</summary>
        public List<Int32> Months;

        /// <summary>星期集合</summary>
        public List<Int32> DaysOfWeek;
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
            var ss = expressions.Split(" ");
            if (ss.Length == 0) return false;

            Seconds = BuildValues(ss[0], 0, 60);
            Minutes = BuildValues(ss.Length > 1 ? ss[1] : "*", 0, 60);
            Hours = BuildValues(ss.Length > 2 ? ss[2] : "*", 0, 24);
            DaysOfMonth = BuildValues(ss.Length > 3 ? ss[3] : "*", 1, 32);
            Months = BuildValues(ss.Length > 4 ? ss[4] : "*", 1, 13);
            DaysOfWeek = BuildValues(ss.Length > 5 ? ss[5] : "*", 0, 7);

            return true;
        }

        private List<Int32> BuildValues(String value, Int32 start, Int32 max)
        {
            if (Int32.TryParse(value, out var n)) return new List<Int32> { n };
            if (value.Contains(',')) return value.SplitAsInt(",").ToList();

            var p = value.IndexOf('/');
            var divisor = p > 0 ? value.Substring(p + 1).ToInt() : 0;

            var p2 = value.IndexOf('-');
            if (p2 > 0)
            {
                start = value.Substring(0, p2).ToInt();
                max = p > 0 ? value.Substring(p2 + 1, p - p2 - 1).ToInt() : value.Substring(p2 + 1).ToInt();
            }

            return divisor > 0
                ? Enumerable.Range(start, max - start).Where(e => e % divisor == 0).ToList()
                : Enumerable.Range(start, max - start).ToList();
        }
        #endregion
    }
}