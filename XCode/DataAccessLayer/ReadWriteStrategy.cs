using System;
using System.Collections.Generic;
using NewLife;

namespace XCode.DataAccessLayer
{
    /// <summary>时间区间</summary>
    public struct TimeRegion
    {
        /// <summary>开始时间</summary>
        public TimeSpan Start;

        /// <summary>结束时间</summary>
        public TimeSpan End;
    }

    /// <summary>读写分离策略。忽略时间区间和表名</summary>
    public class ReadWriteStrategy
    {
        /// <summary>要忽略的时间区间</summary>
        public IList<TimeRegion> IgnoreTimes { get; set; } = new List<TimeRegion>();

        /// <summary>要忽略的表名</summary>
        public ICollection<String> IgnoreTables { get; set; } = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

        /// <summary>设置不走读写分离的时间段，如00:30-00:50，多段区间逗号分开</summary>
        /// <param name="regions"></param>
        public void AddIgnoreTimes(String regions)
        {
            var rs = regions.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in rs)
            {
                var ss = item.Split('-');
                if (ss.Length == 2)
                {
                    if (TimeSpan.TryParse(ss[0], out var start) &&
                        TimeSpan.TryParse(ss[1], out var end) &&
                        start < end)
                    {
                        IgnoreTimes.Add(new TimeRegion { Start = start, End = end });
                    }
                }
            }
        }

        /// <summary>检查是否支持读写分离</summary>
        /// <param name="dal"></param>
        /// <param name="sql"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public virtual Boolean Validate(DAL dal, String sql, String action)
        {
            // 事务中不支持分离
            if (dal.ReadOnly == null) return false;
            if (dal.Session.Transaction != null) return false;

            if (!action.EqualIgnoreCase("Select", "SelectCount", "Query")) return false;
            if (action == "ExecuteScalar" && !sql.TrimStart().StartsWithIgnoreCase("select ")) return false;

            // 判断是否忽略的时间区间
            var span = DateTime.Now - DateTime.Today;
            foreach (var item in IgnoreTimes)
            {
                if (span >= item.Start && span < item.End) return false;
            }

            // 是否忽略的表名
            if (!sql.IsNullOrEmpty())
            {
                var tables = DAL.GetTables(sql);
                foreach (var item in tables)
                {
                    if (IgnoreTables.Contains(item)) return false;
                }
            }

            return true;
        }
    }
}