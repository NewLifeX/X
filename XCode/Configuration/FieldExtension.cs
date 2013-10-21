using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.Configuration
{
    /// <summary>字段扩展</summary>
    public static class FieldExtension
    {
        #region 时间复杂运算
        #region 天
        /// <summary>当天范围</summary>
        /// <param name="field">字段</param>
        /// <returns></returns>
        public static WhereExpression Today(this FieldItem field)
        {
            //var fromDateStart = DateTime.Parse(String.Format("{0:yyyy-MM-dd 00:00:00}", DateTime.Now));
            //var fromDateEnd = DateTime.Parse(String.Format("{0:yyyy-MM-dd 00:00:00}", DateTime.Now));
            //return field.Between(fromDateStart, fromDateEnd);

            var date = DateTime.Now.Date;
            return field.Between(date, date);
        }

        /// <summary>昨天范围</summary>
        /// <param name="field">字段</param>
        /// <returns></returns>
        public static WhereExpression Yesterday(this FieldItem field)
        {
            //var fromDateStart = DateTime.Parse(String.Format("{0:yyyy-MM-dd 00:00:00}", DateTime.Now.AddDays(-1)));
            //var fromDateEnd = DateTime.Parse(String.Format("{0:yyyy-MM-dd 00:00:00}", DateTime.Now.AddDays(-1)));
            //return field.Between(fromDateStart, fromDateEnd);

            var date = DateTime.Now.Date.AddDays(-1);
            return field.Between(date, date);
        }

        /// <summary>明天范围</summary>
        /// <param name="field">字段</param>
        /// <returns></returns>
        public static WhereExpression Tomorrow(this FieldItem field)
        {
            //var fromDateStart = DateTime.Parse(String.Format("{0:yyyy-MM-dd 00:00:00}", DateTime.Now.AddDays(1)));
            //var fromDateEnd = DateTime.Parse(String.Format("{0:yyyy-MM-dd 00:00:00}", DateTime.Now.AddDays(1)));
            //return field.Between(fromDateStart, fromDateEnd);

            var date = DateTime.Now.Date.AddDays(1);
            return field.Between(date, date);
        }

        /// <summary>过去天数范围</summary>
        /// <param name="field">字段</param>
        /// <param name="days"></param>
        /// <returns></returns>
        public static WhereExpression LastDays(this FieldItem field, Int32 days)
        {
            //var fromDateStart = DateTime.Parse(String.Format("{0:yyyy-MM-dd} 00:00:00", DateTime.Now.AddDays(-days)));
            //var fromDateEnd = DateTime.Parse(String.Format("{0:yyyy-MM-dd} 00:00:00", DateTime.Now.AddDays(-1)));
            //return field.Between(fromDateStart, fromDateEnd);

            var date = DateTime.Now.Date;
            return field.Between(date.AddDays(-1 * days), date);
        }

        /// <summary>未来天数范围</summary>
        /// <param name="field">字段</param>
        /// <param name="days"></param>
        /// <returns></returns>
        public static WhereExpression NextDays(this FieldItem field, Int32 days)
        {
            //var fromDateStart = DateTime.Parse(String.Format("{0:yyyy-MM-dd} 00:00:00", DateTime.Now.AddDays(1)));
            //var fromDateEnd = DateTime.Parse(String.Format("{0:yyyy-MM-dd} 00:00:00", DateTime.Now.AddDays(days)));
            //return field.Between(fromDateStart, fromDateEnd);

            var date = DateTime.Now.Date;
            return field.Between(date, date.AddDays(days));
        }
        #endregion

        #region 周
        /// <summary>本周范围</summary>
        /// <param name="field">字段</param>
        /// <returns></returns>
        public static WhereExpression ThisWeek(this FieldItem field)
        {
            //var fromDateStart = DateTime.Parse(String.Format("{0:yyyy-MM-dd} 00:00:00", DateTime.Now.AddDays(Convert.ToDouble((0 - Convert.ToInt16(DateTime.Now.DayOfWeek))))));
            //var fromDateEnd = DateTime.Parse(String.Format("{0:yyyy-MM-dd} 00:00:00", DateTime.Now.AddDays(Convert.ToDouble((6 - Convert.ToInt16(DateTime.Now.DayOfWeek))))));
            //return field.Between(fromDateStart, fromDateEnd);

            var date = DateTime.Now.Date;
            var day = (Int32)date.DayOfWeek;
            return field.Between(date.AddDays(-1 * day), date.AddDays(6 - day));
        }

        /// <summary>上周范围</summary>
        /// <param name="field">字段</param>
        /// <returns></returns>
        public static WhereExpression LastWeek(this FieldItem field)
        {
            //var fromDateStart = DateTime.Parse(String.Format("{0:yyyy-MM-dd} 00:00:00", DateTime.Now.AddDays(Convert.ToDouble((0 - Convert.ToInt16(DateTime.Now.DayOfWeek))) - 7)));
            //var fromDateEnd = DateTime.Parse(String.Format("{0:yyyy-MM-dd} 00:00:00", DateTime.Now.AddDays(Convert.ToDouble((6 - Convert.ToInt16(DateTime.Now.DayOfWeek))) - 7)));
            //return field.Between(fromDateStart, fromDateEnd);

            var date = DateTime.Now.Date;
            var day = (Int32)date.DayOfWeek;
            return field.Between(date.AddDays(-1 * day - 7), date.AddDays(6 - day - 7));
        }

        /// <summary>下周范围</summary>
        /// <param name="field">字段</param>
        /// <returns></returns>
        public static WhereExpression NextWeek(this FieldItem field)
        {
            //var fromDateStart = DateTime.Parse(String.Format("{0:yyyy-MM-dd} 00:00:00", DateTime.Now.AddDays(Convert.ToDouble((0 - Convert.ToInt16(DateTime.Now.DayOfWeek))) + 7)));
            //var fromDateEnd = DateTime.Parse(String.Format("{0:yyyy-MM-dd} 00:00:00", DateTime.Now.AddDays(Convert.ToDouble((6 - Convert.ToInt16(DateTime.Now.DayOfWeek))) + 7)));
            //return field.Between(fromDateStart, fromDateEnd);

            var date = DateTime.Now.Date;
            var day = (Int32)date.DayOfWeek;
            return field.Between(date.AddDays(-1 * day + 7), date.AddDays(6 - day + 7));
        }
        #endregion

        #region 月
        /// <summary>本月范围</summary>
        /// <param name="field">字段</param>
        /// <returns></returns>
        public static WhereExpression ThisMonth(this FieldItem field)
        {
            //var fromDateStart = DateTime.Parse(String.Format("{0:yyyy-MM}-01 00:00:00", DateTime.Now));
            //var fromDateEnd = DateTime.Parse(String.Format("{0:yyyy-MM}-01 00:00:00", DateTime.Now.AddMonths(1))).AddDays(-1);
            //return field.Between(fromDateStart, fromDateEnd);

            var now = DateTime.Now;
            var month = new DateTime(now.Year, now.Month, 1);
            return field.Between(month, month.AddMonths(1).AddDays(-1));
        }

        /// <summary>上月范围</summary>
        /// <param name="field">字段</param>
        /// <returns></returns>
        public static WhereExpression LastMonth(this FieldItem field)
        {
            //var fromDateStart = DateTime.Parse(String.Format("{0:yyyy-MM}-01 00:00:00", DateTime.Now.AddMonths(-1)));
            //var fromDateEnd = DateTime.Parse(String.Format("{0:yyyy-MM}-01 00:00:00", DateTime.Now)).AddDays(-1);
            //return field.Between(fromDateStart, fromDateEnd);

            var now = DateTime.Now;
            var month = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
            return field.Between(month, month.AddMonths(1).AddDays(-1));
        }

        /// <summary>下月范围</summary>
        /// <param name="field">字段</param>
        /// <returns></returns>
        public static WhereExpression NextMonth(this FieldItem field)
        {
            //var fromDateStart = DateTime.Parse(String.Format("{0:yyyy-MM}-01 00:00:00", DateTime.Now.AddMonths(1)));
            //var fromDateEnd = DateTime.Parse(String.Format("{0:yyyy-MM}-01 00:00:00", DateTime.Now.AddMonths(2))).AddDays(-1);
            //return field.Between(fromDateStart, fromDateEnd);

            var now = DateTime.Now;
            var month = new DateTime(now.Year, now.Month, 1).AddMonths(1);
            return field.Between(month, month.AddMonths(1).AddDays(-1));
        }
        #endregion

        #region 季度
        /// <summary>本季度范围</summary>
        /// <param name="field">字段</param>
        /// <returns></returns>
        public static WhereExpression ThisQuarter(this FieldItem field)
        {
            //var fromDateStart = DateTime.Parse(String.Format("{0:yyyy-MM}-01 00:00:00", DateTime.Now.AddMonths(0 - ((DateTime.Now.Month - 1) % 3))));
            //var fromDateEnd = DateTime.Parse(String.Format("{0:yyyy-MM-dd} 00:00:00", DateTime.Parse(DateTime.Now.AddMonths(3 - ((DateTime.Now.Month - 1) % 3)).ToString("yyyy-MM-01")))).AddDays(-1);
            //return field.Between(fromDateStart, fromDateEnd);

            var now = DateTime.Now;
            var month = new DateTime(now.Year, now.Month - (now.Month - 1) % 3, 1);
            return field.Between(month, month.AddMonths(3).AddDays(-1));
        }

        /// <summary>上季度范围</summary>
        /// <param name="field">字段</param>
        /// <returns></returns>
        public static WhereExpression LastQuarter(this FieldItem field)
        {
            //var fromDateStart = DateTime.Parse(String.Format("{0:yyyy-MM}-01 00:00:00", DateTime.Now.AddMonths(-3 - ((DateTime.Now.Month - 1) % 3))));
            //var fromDateEnd = DateTime.Parse(String.Format("{0:yyyy-MM-dd} 00:00:00", DateTime.Parse(DateTime.Now.AddMonths(0 - ((DateTime.Now.Month - 1) % 3)).ToString("yyyy-MM-01")))).AddDays(-1);
            //return field.Between(fromDateStart, fromDateEnd);

            var now = DateTime.Now;
            var month = new DateTime(now.Year, now.Month - (now.Month - 1) % 3, 1).AddMonths(-3);
            return field.Between(month, month.AddMonths(3).AddDays(-1));
        }

        /// <summary>下季度范围</summary>
        /// <param name="field">字段</param>
        /// <returns></returns>
        public static WhereExpression NextQuarter(this FieldItem field)
        {
            //var fromDateStart = DateTime.Parse(String.Format("{0:yyyy-MM}-01 00:00:00", DateTime.Now.AddMonths(3 - ((DateTime.Now.Month - 1) % 3))));
            //var fromDateEnd = DateTime.Parse(String.Format("{0:yyyy-MM-dd} 00:00:00", DateTime.Parse(DateTime.Now.AddMonths(6 - ((DateTime.Now.Month - 1) % 3)).ToString("yyyy-MM-01")))).AddDays(-1);
            //return field.Between(fromDateStart, fromDateEnd);

            var now = DateTime.Now;
            var month = new DateTime(now.Year, now.Month - (now.Month - 1) % 3, 1).AddMonths(3);
            return field.Between(month, month.AddMonths(3).AddDays(-1));
        }
        #endregion
        #endregion
    }
}