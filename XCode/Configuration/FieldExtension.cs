using System;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XCode
{
    /// <summary>字段扩展</summary>
    public static class FieldExtension
    {
        #region 时间复杂运算
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
            var exp = new WhereExpression();
            if (String.IsNullOrEmpty(keys)) return exp;

            var ks = keys.Split(" ");

            for (int i = 0; i < ks.Length; i++)
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
            var exp = new WhereExpression();
            if (String.IsNullOrEmpty(keys)) return exp;

            var ks = keys.Split(" ");

            for (int i = 0; i < ks.Length; i++)
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
        public static ConcatExpression Asc(this FieldItem field) { return field == null ? null : new ConcatExpression(field.FormatedName); }

        /// <summary>降序</summary>
        /// <param name="field">字段</param>
        /// <remarks>感谢 树懒（303409914）发现这里的错误</remarks>
        /// <returns></returns>
        public static ConcatExpression Desc(this FieldItem field) { return field == null ? null : new ConcatExpression(field.FormatedName + " Desc"); }

        /// <summary>通过参数置顶升序降序</summary>
        /// <param name="field">字段</param>
        /// <param name="isdesc">是否降序</param>
        /// <returns></returns>
        public static ConcatExpression Sort(this FieldItem field, Boolean isdesc) { return isdesc ? Desc(field) : Asc(field); }
        #endregion

        #region 分组选择
        /// <summary>分组。有条件的分组请使用WhereExpression.GroupBy</summary>
        /// <returns></returns>
        public static ConcatExpression GroupBy(this FieldItem field) { return field == null ? null : new ConcatExpression(String.Format("Group By {0}", field.FormatedName)); }

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

        /// <summary>数量</summary>
        /// <param name="field">字段</param>
        /// <param name="newName">聚合后as的新名称，默认空，表示跟前面字段名一致</param>
        /// <returns></returns>
        public static ConcatExpression Count(this FieldItem field, String newName = null) { return Aggregate(field, "Count", newName); }

        /// <summary>求和</summary>
        /// <param name="field">字段</param>
        /// <param name="newName">聚合后as的新名称，默认空，表示跟前面字段名一致</param>
        /// <returns></returns>
        public static ConcatExpression Sum(this FieldItem field, String newName = null) { return Aggregate(field, "Sum", newName); }

        /// <summary>最小值</summary>
        /// <param name="field">字段</param>
        /// <param name="newName">聚合后as的新名称，默认空，表示跟前面字段名一致</param>
        /// <returns></returns>
        public static ConcatExpression Min(this FieldItem field, String newName = null) { return Aggregate(field, "Min", newName); }

        /// <summary>最大值</summary>
        /// <param name="field">字段</param>
        /// <param name="newName">聚合后as的新名称，默认空，表示跟前面字段名一致</param>
        /// <returns></returns>
        public static ConcatExpression Max(this FieldItem field, String newName = null) { return Aggregate(field, "Max", newName); }
        #endregion
    }
}