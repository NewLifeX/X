using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Data;
using XCode;
using XCode.Cache;
using XCode.Statistics;

namespace XCode.Membership
{
    /// <summary>访问统计模型</summary>
    public class VisitStatModel : StatModel<VisitStatModel>
    {
        #region 属性
        /// <summary>页面</summary>
        public String Page { get; set; }

        /// <summary>标题</summary>
        public String Title { get; set; }

        /// <summary>耗时</summary>
        public Int32 Cost { get; set; }

        /// <summary>用户</summary>
        public String User { get; set; }

        /// <summary>IP地址</summary>
        public String IP { get; set; }

        /// <summary>错误</summary>
        public String Error { get; set; }
        #endregion

        #region 相等比较
        /// <summary>相等</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override Boolean Equals(Object obj)
        {
            if (!base.Equals(obj)) return false;

            if (obj is VisitStatModel model) return Page + "" == model.Page + "";

            return false;
        }

        /// <summary>获取哈希</summary>
        /// <returns></returns>
        public override Int32 GetHashCode() => base.GetHashCode() ^ Page.GetHashCode();
        #endregion
    }

    /// <summary>访问统计</summary>
    public partial class VisitStat : Entity<VisitStat>, IStat
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

            //// 单对象缓存从键
            //var sc = Meta.SingleCache;
            //if (sc.Expire < 20 * 60) sc.Expire = 20 * 60;
            //sc.FindSlaveKeyMethod = k => FindByModel(GetModel(k), false);
            //sc.GetSlaveKeyMethod = e => GetKey(e.ToModel());

#if !DEBUG
            // 关闭SQL日志
            Meta.Session.Dal.Db.ShowSQL = false;
#endif
        }
        #endregion

        #region 扩展属性
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id">编号</param>
        /// <returns>实体对象</returns>
        public static VisitStat FindByID(Int32 id)
        {
            if (id <= 0) return null;

            //// 实体缓存
            //if (Meta.Count < 1000) return Meta.Cache.Find(e => e.ID == id);

            // 单对象缓存
            return Meta.SingleCache[id];

            //return Find(_.ID == id);
        }

        private static DictionaryCache<VisitStatModel, VisitStat> _cache = new DictionaryCache<VisitStatModel, VisitStat> { Expire = 20 * 60, Period = 60 };
        /// <summary>根据模型查找</summary>
        /// <param name="model"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        public static VisitStat FindByModel(VisitStatModel model, Boolean cache)
        {
            if (model == null) return null;

            //if (cache) return Meta.SingleCache.GetItemWithSlaveKey(GetKey(model)) as VisitStat;
            if (cache)
            {
                if (_cache.FindMethod == null) _cache.FindMethod = m => FindByModel(m, false);

                return _cache[model];
            }

            var exp = new WhereExpression();
            exp &= _.Level == model.Level;
            if (model.Level > 0 && model.Time > DateTime.MinValue) exp &= _.Time == model.GetDate(model.Level);
            exp &= _.Page == model.Page;

            return Find(exp);
        }
        #endregion

        #region 高级查询
        /// <summary>高级查询访问统计</summary>
        /// <param name="model"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static IList<VisitStat> Search(VisitStatModel model, DateTime start, DateTime end, PageParameter param)
        {
            var exp = new WhereExpression();
            if (model.Level >= 0) exp &= _.Level == model.Level;
            if (model.Level > 0 && model.Time > DateTime.MinValue) exp &= _.Time == model.GetDate(model.Level);
            if (!model.Page.IsNullOrEmpty()) exp &= _.Page == model.Page;

            exp &= _.Time.Between(start, end);

            return FindAll(exp, param);
        }

        static FieldCache<VisitStat> PageCache = new FieldCache<VisitStat>(_.Page);

        /// <summary>查找所有</summary>
        /// <returns></returns>
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
        /// <summary>业务统计</summary>
        /// <param name="model">模型</param>
        /// <param name="levels">要统计的层级</param>
        /// <returns></returns>
        public static void Process(VisitStatModel model, params StatLevels[] levels)
        {
            model = model.Clone();

            if (levels == null || levels.Length == 0) levels = new[] { StatLevels.Day, StatLevels.Month, StatLevels.Year };

            // 当前
            var list = model.Split(levels);

            // 全局
            if (!model.Page.IsNullOrEmpty())
            {
                model.Page = "全部";

                list.AddRange(model.Split(levels));
            }

            // 并行处理
            Parallel.ForEach(list, m => ProcessItem(m as VisitStatModel));
        }

        private static VisitStat ProcessItem(VisitStatModel model)
        {
            var st = StatHelper.GetOrAdd(model, FindByModel, e =>
            {
                e.Page = model.Page;
            });
            if (st == null) return null;

            // 历史平均
            if (st.Cost > 0)
                st.Cost = (Int32)Math.Round(((Double)st.Cost * st.Times + model.Cost) / (st.Times + 1));
            else
                st.Cost = model.Cost;
            if (model.Cost > st.MaxCost) st.MaxCost = model.Cost;

            if (!model.Title.IsNullOrEmpty()) st.Title = model.Title;
            //st.Times++;
            Interlocked.Increment(ref st._Times);

            if (!model.Error.IsNullOrEmpty())
            {
                //st.Error++;
                Interlocked.Increment(ref st._Error);
            }

            var user = model.User;
            var ip = model.IP;
            if (!user.IsNullOrEmpty() || !ip.IsNullOrEmpty())
            {
                // 计算用户和IP，合并在Remark里面
                var ss = new HashSet<String>((st.Remark + "").Split(","));
                if (!user.IsNullOrEmpty() && !ss.Contains(user))
                {
                    //st.Users++;
                    Interlocked.Increment(ref st._Users);
                    ss.Add(user + "");
                }
                if (!ip.IsNullOrEmpty() && !ss.Contains(ip))
                {
                    //st.IPs++;
                    Interlocked.Increment(ref st._IPs);
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

            st.SaveAsync(5_000);

            return st;
        }
        #endregion

        #region 辅助
        ///// <summary>实体转模型</summary>
        ///// <returns></returns>
        //public VisitStatModel ToModel()
        //{
        //    var model = new VisitStatModel
        //    {
        //        Page = Page,
        //        Level = Level,
        //        Time = Time,
        //    };

        //    return model;
        //}

        //private static String GetKey(VisitStatModel model)
        //{
        //    return $"{model.Page}_{(Int32)model.Level}_{model.Time.ToFullString()}";
        //}

        //private static VisitStatModel GetModel(String key)
        //{
        //    var ks = key.Split("_");
        //    if (ks.Length < 3) return null;

        //    var model = new VisitStatModel
        //    {
        //        Page = ks[0],
        //        Level = (StatLevels)ks[1].ToInt(),
        //        Time = ks[2].ToDateTime(),
        //    };

        //    return model;
        //}
        #endregion
    }
}