using System;
using System.Text;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XCode
{
    /// <summary>字段扩展</summary>
    public static class FieldExtension
    {
        #region 时间复杂运算
        /// <summary>时间专用区间函数</summary>
        /// <param name="fi"></param>
        /// <param name="start">起始时间，大于等于</param>
        /// <param name="end">结束时间，小于。如果是日期，则加一天</param>
        /// <returns></returns>
        public static Expression Between(this FieldItem fi, DateTime start, DateTime end)
        {
            if (fi.Type != typeof(DateTime)) throw new NotSupportedException($"[{nameof(Between)}]函数仅支持时间日期字段！");

            var exp = new WhereExpression();
            if (fi == null) return exp;

            if (start <= DateTime.MinValue || start >= DateTime.MaxValue)
            {
                if (end <= DateTime.MinValue || end >= DateTime.MaxValue) return exp;

                // 如果只有日期，则加一天，表示包含这一天
                if (end == end.Date) end = end.AddDays(1);

                return fi < end;
            }
            else
            {
                exp &= fi >= start;
                if (end <= DateTime.MinValue || end >= DateTime.MaxValue) return exp;

                // 如果只有日期，则加一天，表示包含这一天
                if (start == start.Date && end == end.Date) end = end.AddDays(1);

                return exp & fi < end;
            }
        }

        #region 天
        /// <summary>当天范围</summary>
        /// <param name="field">字段</param>
        /// <returns></returns>
        public static Expression Today(this FieldItem field)
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
        public static Expression Yesterday(this FieldItem field)
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
        public static Expression Tomorrow(this FieldItem field)
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
        public static Expression LastDays(this FieldItem field, Int32 days)
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
        public static Expression NextDays(this FieldItem field, Int32 days)
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
        public static Expression ThisWeek(this FieldItem field)
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
        public static Expression LastWeek(this FieldItem field)
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
        public static Expression NextWeek(this FieldItem field)
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
        public static Expression ThisMonth(this FieldItem field)
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
        public static Expression LastMonth(this FieldItem field)
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
        public static Expression NextMonth(this FieldItem field)
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
        public static Expression ThisQuarter(this FieldItem field)
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
        public static Expression LastQuarter(this FieldItem field)
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
        public static Expression NextQuarter(this FieldItem field)
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

        #region 字符串复杂运算
        /// <summary>包含所有关键字</summary>
        /// <param name="field"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        public static Expression ContainsAll(this FieldItem field, String keys)
        {
            if (field.Type != typeof(String)) throw new NotSupportedException($"[{nameof(ContainsAll)}]函数仅支持字符串字段！");

            var exp = new WhereExpression();
            if (String.IsNullOrEmpty(keys)) return exp;

            var ks = keys.Split(" ");

            for (var i = 0; i < ks.Length; i++)
            {
                if (!ks[i].IsNullOrWhiteSpace()) exp &= field.Contains(ks[i].Trim());
            }

            return exp;
        }

        /// <summary>包含任意关键字</summary>
        /// <param name="field"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        public static Expression ContainsAny(this FieldItem field, String keys)
        {
            if (field.Type != typeof(String)) throw new NotSupportedException($"[{nameof(ContainsAny)}]函数仅支持字符串字段！");

            var exp = new WhereExpression();
            if (String.IsNullOrEmpty(keys)) return exp;

            var ks = keys.Split(" ");

            for (var i = 0; i < ks.Length; i++)
            {
                if (!ks[i].IsNullOrWhiteSpace()) exp |= field.Contains(ks[i].Trim());
            }

            return exp;
        }
        #endregion

        #region 排序
        /// <summary>升序</summary>
        /// <param name="field">字段</param>
        /// <returns></returns>
        public static ConcatExpression Asc(this FieldItem field)
        {
            if (field == null || field.FormatedName.IsNullOrEmpty()) return null;

            return new ConcatExpression(field.FormatedName);
        }

        /// <summary>降序</summary>
        /// <param name="field">字段</param>
        /// <remarks>感谢 树懒（303409914）发现这里的错误</remarks>
        /// <returns></returns>
        public static ConcatExpression Desc(this FieldItem field)
        {
            if (field == null || field.FormatedName.IsNullOrEmpty()) return null;

            return new ConcatExpression(field.FormatedName + " Desc");
        }

        /// <summary>通过参数置顶升序降序</summary>
        /// <param name="field">字段</param>
        /// <param name="isdesc">是否降序</param>
        /// <returns></returns>
        public static ConcatExpression Sort(this FieldItem field, Boolean isdesc) => isdesc ? Desc(field) : Asc(field);
        #endregion

        #region 分组选择
        /// <summary>分组。有条件的分组请使用WhereExpression.GroupBy</summary>
        /// <returns></returns>
        public static ConcatExpression GroupBy(this FieldItem field) => field == null ? null : new ConcatExpression(String.Format("Group By {0}", field.FormatedName));

        ///// <summary>按照指定若干个字段分组。没有条件时使用分组请用FieldItem的GroupBy</summary>
        ///// <param name="where"></param>
        ///// <param name="fields"></param>
        ///// <returns>返回条件语句加上分组语句</returns>
        //public static ConcatExpression GroupBy(this WhereExpression where, params FieldItem[] fields)
        //{
        //    var exp = new ConcatExpression();
        //    var sb = exp.Builder;
        //    where.GetString(sb, null);

        //    if (sb.Length > 0) sb.Append(" Group By ");

        //    for (var i = 0; i < fields.Length; i++)
        //    {
        //        if (i > 0) sb.Append(", ");
        //        sb.Append(fields[i].FormatedName);
        //    }

        //    return exp;
        //}

        /// <summary>按照指定若干个字段分组。没有条件时使用分组请用FieldItem的GroupBy</summary>
        /// <param name="where"></param>
        /// <param name="fields"></param>
        /// <returns>将需要分组的字段作为ConcatExpression类型添加到whereExpression尾部</returns>
        public static WhereExpression GroupBy(this WhereExpression where, params FieldItem[] fields)
        {
            var exp = new ConcatExpression();

            for (var i = 0; i < fields.Length; i++)
            {
                if (i == 0) exp &= fields[i].GroupBy();

                exp.And(fields[i]);
            }

            return new WhereExpression(where, Operator.Space, exp);
        }

        /// <summary>聚合</summary>
        /// <param name="field">字段</param>
        /// <param name="action"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        public static ConcatExpression Aggregate(this FieldItem field, String action, String newName)
        {
            if (field == null) return null;

            var name = field.FormatedName;
            if (String.IsNullOrEmpty(newName))
                newName = name;
            else
                newName = field.Factory.FormatName(newName);

            return new ConcatExpression(String.Format("{2}({0}) as {1}", name, newName, action));
        }

        /// <summary>作为新的列</summary>
        /// <param name="field"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        public static ConcatExpression As(this FieldItem field, String newName)
        {
            if (field == null) return null;

            var name = field.FormatedName;
            if (String.IsNullOrEmpty(newName))
                newName = name;
            else
                newName = field.Factory.FormatName(newName);

            return new ConcatExpression(String.Format("{0} as {1}", name, newName));
        }

        /// <summary>数量</summary>
        /// <param name="field">字段</param>
        /// <param name="newName">聚合后as的新名称，默认空，表示跟前面字段名一致</param>
        /// <returns></returns>
        public static ConcatExpression Count(this FieldItem field, String newName = null) => Aggregate(field, "Count", newName);

        /// <summary>求和</summary>
        /// <param name="field">字段</param>
        /// <param name="newName">聚合后as的新名称，默认空，表示跟前面字段名一致</param>
        /// <returns></returns>
        public static ConcatExpression Sum(this FieldItem field, String newName = null) => Aggregate(field, "Sum", newName);

        /// <summary>最小值</summary>
        /// <param name="field">字段</param>
        /// <param name="newName">聚合后as的新名称，默认空，表示跟前面字段名一致</param>
        /// <returns></returns>
        public static ConcatExpression Min(this FieldItem field, String newName = null) => Aggregate(field, "Min", newName);

        /// <summary>最大值</summary>
        /// <param name="field">字段</param>
        /// <param name="newName">聚合后as的新名称，默认空，表示跟前面字段名一致</param>
        /// <returns></returns>
        public static ConcatExpression Max(this FieldItem field, String newName = null) => Aggregate(field, "Max", newName);
        #endregion
    }
}