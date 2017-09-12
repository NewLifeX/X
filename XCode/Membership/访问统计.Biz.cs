using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NewLife.Data;
using XCode;
using XCode.Cache;

namespace XCode.Membership
{
    /// <summary>访问统计</summary>
    public partial class VisitStat : Entity<VisitStat>
    {
        #region 对象操作
        static VisitStat()
        {
            // 累加字段
            var df = Meta.Factory.AdditionalFields;
            df.Add(__.Times);
            df.Add(__.Users);
            df.Add(__.IPs);
            df.Add(__.Error);

            // 过滤器 UserModule、TimeModule、IPModule
            Meta.Modules.Add<TimeModule>();

            // 单对象缓存从键
            var sc = Meta.SingleCache;
            sc.FindSlaveKeyMethod = k =>
            {
                var ss = k.Split(new Char[] { '#' }, StringSplitOptions.None);
                var ds = ss[1].SplitAsInt("_");
                var exp = _.Year == ds[0] & _.Month == ds[1] & _.Day == ds[2];
                if (ss[0].IsNullOrEmpty())
                    exp &= _.Page.IsNullOrEmpty();
                else
                    exp &= _.Page == ss[0];

                return Find(exp);
            };
            sc.GetSlaveKeyMethod = e => "{0}#{1}_{2}_{3}".F(e.Page, e.Year, e.Month, e.Day);
        }

        /// <summary>验证数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew">是否插入</param>
        public override void Valid(Boolean isNew)
        {
            // 如果没有脏数据，则不需要进行任何处理
            if (!HasDirty) return;

            // 在新插入数据或者修改了指定字段时进行修正
            //if (isNew && !Dirtys[nameof(CreateTime)]) nameof(CreateTime) = DateTime.Now;
            //if (!Dirtys[nameof(UpdateTime)]) nameof(UpdateTime) = DateTime.Now;

            // 检查唯一索引
            // CheckExist(isNew, __.Year, __.Month, __.Day);
        }
        #endregion

        #region 扩展属性
        /// <summary>平均耗时</summary>
        [Map(__.Cost)]
        public Int32 AvgCost { get { return (Int32)(Times == 0 ? 0 : Cost / Times); } }
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id">编号</param>
        /// <returns>实体对象</returns>
        public static VisitStat FindByID(Int32 id)
        {
            if (id <= 0) return null;

            //// 实体缓存
            //if (Meta.Count < 1000) return Meta.Cache.Entities.FirstOrDefault(e => e.ID == id);

            // 单对象缓存
            return Meta.SingleCache[id];

            //return Find(_.ID == id);
        }

        /// <summary>根据年、月、日查找</summary>
        /// <param name="page">页</param>
        /// <param name="year">年</param>
        /// <param name="month">月</param>
        /// <param name="day">日</param>
        /// <returns>实体对象</returns>
        public static VisitStat FindByPage(String page, Int32 year, Int32 month, Int32 day)
        {
            //// 实体缓存
            //if (Meta.Count < 1000) return Meta.Cache.Entities.FirstOrDefault(e => e.Year == year && e.Month == month && e.Day == day);

            //return Find(_.Year == year & _.Month == month & _.Day == day);

            var key = "{0}#{1}_{2}_{3}".F(page, year, month, day);
            return Meta.SingleCache.GetItemWithSlaveKey(key) as VisitStat;
        }
        #endregion

        #region 高级查询
        /// <summary>高级查询访问统计</summary>
        /// <param name="page"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static IList<VisitStat> Search(String page, Int32 year, Int32 month, Int32 day, DateTime start, DateTime end, PageParameter param)
        {
            var exp = new WhereExpression();
            if (year >= 0) exp &= _.Year == year;
            if (month >= 0) exp &= _.Month == month;
            if (day >= 0) exp &= _.Day == day;

            if (!page.IsNullOrEmpty()) exp &= _.Page == page;
            exp &= _.CreateTime.Between(start, end);

            return FindAll(exp, param);
        }

        static FieldCache<VisitStat> PageCache = new FieldCache<VisitStat>(_.Page);

        /// <summary>查找所有</summary>
        /// <returns></returns>
        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public static IList<VisitStat> FindAllPage()
        {
            return PageCache.Entities;
        }

        /// <summary>获取所有名称</summary>
        /// <returns></returns>
        public static IDictionary<String, String> FindAllPageName()
        {
            return PageCache.FindAllName();
        }
        #endregion

        #region 业务操作
        /// <summary>添加统计记录</summary>
        /// <param name="page"></param>
        /// <param name="title"></param>
        /// <param name="cost"></param>
        /// <param name="userid"></param>
        /// <param name="ip"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public static VisitStat Add(String page, String title, Int32 cost, Int32 userid, String ip, String err)
        {
            var now = DateTime.Now;

            // 今天
            var st = Add(page, now.Year, now.Month, now.Day, title, cost, userid, ip, err);
            Add(null, now.Year, now.Month, now.Day, null, cost, userid, ip, err);

            // 本月
            Add(page, now.Year, now.Month, 0, title, cost, userid, ip, err);
            Add(null, now.Year, now.Month, 0, null, cost, userid, ip, err);

            // 今年
            Add(page, now.Year, 0, 0, title, cost, userid, ip, err);
            Add(null, now.Year, 0, 0, null, cost, userid, ip, err);

            // 全部
            Add(page, 0, 0, 0, title, cost, userid, ip, err);
            Add(null, 0, 0, 0, null, cost, userid, ip, err);

            return st;
        }

        /// <summary>添加统计记录</summary>
        /// <param name="page"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="title"></param>
        /// <param name="cost"></param>
        /// <param name="userid"></param>
        /// <param name="ip"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public static VisitStat Add(String page, Int32 year, Int32 month, Int32 day, String title, Int32 cost, Int32 userid, String ip, String err)
        {
            var st = FindByPage(page, year, month, day);
            if (st == null)
            {
                st = new VisitStat
                {
                    Page = page,
                    Year = year,
                    Month = month,
                    Day = day,
                };

                st.Insert();
            }

            st.Title = title;
            st.Times++;
            st.Cost += cost;
            if (!err.IsNullOrEmpty()) st.Error++;

            if (userid > 0 || !ip.IsNullOrEmpty())
            {
                // 计算用户和IP，合并在Remark里面
                var ss = new HashSet<String>((st.Remark + "").Split(","));
                if (userid > 0 && !ss.Contains(userid + ""))
                {
                    st.Users++;
                    ss.Add(userid + "");
                }
                if (!ip.IsNullOrEmpty() && !ss.Contains(ip))
                {
                    st.IPs++;
                    ss.Add(ip);
                }
                // 如果超长，砍掉前面
                var ds = ss as IEnumerable<String>;
                var k = 1;
                while (true)
                {
                    var str = ds.Join(",");
                    if (str.Length <= _.Remark.Length)
                    {
                        st.Remark = str;
                        break;
                    }

                    ds = ss.Skip(k++);
                }
            }

            st.SaveAsync(1000);

            return st;
        }
        #endregion
    }
}