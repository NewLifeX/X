using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Threading;
using NewLife.Web;
using XCode;
using XCode.Cache;
using XCode.Configuration;
using XCode.DataAccessLayer;
using XCode.Membership;
using XCode.Shards;

namespace XCode.Membership
{
    /// <summary>用户统计</summary>
    public partial class UserStat : Entity<UserStat>
    {
        #region 对象操作
        static UserStat()
        {
            // 累加字段，生成 Update xx Set Count=Count+1234 Where xxx
            var df = Meta.Factory.AdditionalFields;
            df.Add(nameof(Total));

            // 过滤器 UserModule、TimeModule、IPModule
            Meta.Modules.Add<TimeModule>();

            var sc = Meta.SingleCache;
            if (sc.Expire < 20 * 60) sc.Expire = 20 * 60;
            sc.FindSlaveKeyMethod = k => Find(__.Date, k.ToDateTime());
            sc.GetSlaveKeyMethod = e => e.Date.ToFullString();
        }

        /// <summary>验证并修补数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew">是否插入</param>
        public override void Valid(Boolean isNew)
        {
            // 如果没有脏数据，则不需要进行任何处理
            if (!HasDirty) return;

            // 建议先调用基类方法，基类方法会做一些统一处理
            base.Valid(isNew);

            // 在新插入数据或者修改了指定字段时进行修正
            //if (isNew && !Dirtys[nameof(CreateTime)]) CreateTime = DateTime.Now;
            //if (!Dirtys[nameof(UpdateTime)]) UpdateTime = DateTime.Now;

            // 检查唯一索引
            // CheckExist(isNew, nameof(Date));
        }
        #endregion

        #region 扩展属性
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id">编号</param>
        /// <returns>实体对象</returns>
        public static UserStat FindByID(Int32 id)
        {
            if (id <= 0) return null;

            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.ID == id);

            // 单对象缓存
            return Meta.SingleCache[id];

            //return Find(_.ID == id);
        }

        /// <summary>
        /// 根据日期查找
        /// </summary>
        /// <param name="date"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        public static UserStat FindByDate(DateTime date, Boolean cache = true)
        {
            if (date.Year < 2000) return null;

            date = date.Date;
            if (cache)
                return Meta.SingleCache.GetItemWithSlaveKey(date.ToFullString()) as UserStat;
            else
                return Find(_.Date == date);
        }
        #endregion

        #region 高级查询
        /// <summary>高级查询</summary>
        /// <param name="start">统计日期开始</param>
        /// <param name="end">统计日期结束</param>
        /// <param name="key">关键字</param>
        /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
        /// <returns>实体列表</returns>
        public static IList<UserStat> SearchByDate(DateTime start, DateTime end, String key, PageParameter page)
        {
            var exp = new WhereExpression();

            exp &= _.Date.Between(start, end);
            if (!key.IsNullOrEmpty()) exp &= _.Remark.Contains(key);

            return FindAll(exp, page);
        }

        // Select Count(ID) as ID,Category From UserStat Where CreateTime>'2020-01-24 00:00:00' Group By Category Order By ID Desc limit 20
        //static readonly FieldCache<UserStat> _CategoryCache = new FieldCache<UserStat>(nameof(Category))
        //{
        //Where = _.CreateTime > DateTime.Today.AddDays(-30) & Expression.Empty
        //};

        ///// <summary>获取类别列表，字段缓存10分钟，分组统计数据最多的前20种，用于魔方前台下拉选择</summary>
        ///// <returns></returns>
        //public static IDictionary<String, String> GetCategoryList() => _CategoryCache.FindAllName();
        #endregion

        #region 业务操作
        /// <summary>
        /// 获取或添加指定天的统计
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static UserStat GetOrAdd(DateTime date)
        {
            var entity = GetOrAdd(date, FindByDate, k => new UserStat { Date = k, CreateTime = DateTime.Now });

            entity.SaveAsync(5_000);

            return entity;
        }
        #endregion
    }
}