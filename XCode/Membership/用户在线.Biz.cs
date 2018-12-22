/*
 * XCoder v6.9.6383.25987
 * 作者：nnhy/STONE-PC
 * 时间：2017-07-05 15:34:41
 * 版权：版权所有 (C) 新生命开发团队 2002~2017
*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using NewLife.Data;
using NewLife.Model;
using NewLife.Threading;

namespace XCode.Membership
{
    /// <summary>用户在线</summary>
    public partial class UserOnline : Entity<UserOnline>
    {
        #region 对象操作
        static UserOnline()
        {
            // 用于引发基类的静态构造函数，所有层次的泛型实体类都应该有一个
            var entity = new UserOnline();

            Meta.Modules.Add<TimeModule>();
            Meta.Modules.Add<IPModule>();

            var df = Meta.Factory.AdditionalFields;
            df.Add(__.Times);
            df.Add(__.OnlineTime);

            var sc = Meta.SingleCache;
            if (sc.Expire < 20 * 60) sc.Expire = 20 * 60;
            sc.FindSlaveKeyMethod = k => Find(__.SessionID, k);
            sc.GetSlaveKeyMethod = e => e.SessionID;

#if !DEBUG
            // 关闭SQL日志
            Meta.Session.Dal.Db.ShowSQL = false;
#endif
        }

        /// <summary>验证数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew"></param>
        public override void Valid(Boolean isNew)
        {
        }
        #endregion

        #region 扩展属性
        /// <summary>物理地址</summary>
        [DisplayName("物理地址")]
        //[Map(__.CreateIP)]
        public String CreateAddress => CreateIP.IPToAddress();
        #endregion

        #region 扩展查询
        /// <summary>根据会话编号查找</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static UserOnline FindByID(Int32 id)
        {
            if (id <= 0) return null;

            return Meta.SingleCache[id];
            //return Find(__.ID, id);
        }

        /// <summary>根据会话编号查找</summary>
        /// <param name="sessionid"></param>
        /// <returns></returns>
        public static UserOnline FindBySessionID(String sessionid)
        {
            if (sessionid.IsNullOrEmpty()) return null;

            return Meta.SingleCache.GetItemWithSlaveKey(sessionid) as UserOnline;
            //return Find(__.SessionID, sessionid);
        }

        /// <summary>根据用户编号查找</summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        public static IList<UserOnline> FindAllByUserID(Int32 userid)
        {
            if (userid <= 0) return new List<UserOnline>();

            return FindAll(__.UserID, userid);
        }
        #endregion

        #region 高级查询
        // 以下为自定义高级查询的例子

        /// <summary>查询满足条件的记录集，分页、排序</summary>
        /// <param name="userid">用户编号</param>
        /// <param name="start">开始时间</param>
        /// <param name="end">结束时间</param>
        /// <param name="key">关键字</param>
        /// <param name="param">分页排序参数，同时返回满足条件的总记录数</param>
        /// <returns>实体集</returns>
        public static IList<UserOnline> Search(Int32 userid, DateTime start, DateTime end, String key, PageParameter param)
        {
            // WhereExpression重载&和|运算符，作为And和Or的替代
            // SearchWhereByKeys系列方法用于构建针对字符串字段的模糊搜索，第二个参数可指定要搜索的字段
            var exp = SearchWhereByKeys(key, null, null);

            // 以下仅为演示，Field（继承自FieldItem）重载了==、!=、>、<、>=、<=等运算符
            //if (userid > 0) exp &= _.OperatorID == userid;
            //if (isSign != null) exp &= _.IsSign == isSign.Value;
            //exp &= _.OccurTime.Between(start, end); // 大于等于start，小于end，当start/end大于MinValue时有效

            return FindAll(exp, param);
        }
        #endregion

        #region 扩展操作
        #endregion

        #region 业务
        /// <summary>设置会话状态</summary>
        /// <param name="sessionid"></param>
        /// <param name="page"></param>
        /// <param name="status"></param>
        /// <param name="userid"></param>
        /// <param name="name"></param>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static UserOnline SetStatus(String sessionid, String page, String status, Int32 userid = 0, String name = null, String ip = null)
        {
            var entity = FindBySessionID(sessionid) ?? new UserOnline();
            entity.SessionID = sessionid;
            entity.Page = page;
            entity.Status = status;

            entity.Times++;
            if (userid > 0) entity.UserID = userid;
            if (!name.IsNullOrEmpty()) entity.Name = name;

            // 累加在线时间
            entity.UpdateTime = DateTime.Now;
            if (entity.ID == 0)
            {
                entity.CreateIP = ip;
                entity.CreateTime = DateTime.Now;
                entity.Insert();
            }
            else
            {
                entity.OnlineTime = (Int32)(entity.UpdateTime - entity.CreateTime).TotalSeconds;
                entity.SaveAsync();
            }

            return entity;
        }

        private static TimerX _timer;

        /// <summary>设置网页会话状态</summary>
        /// <param name="sessionid"></param>
        /// <param name="page"></param>
        /// <param name="status"></param>
        /// <param name="user"></param>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static UserOnline SetWebStatus(String sessionid, String page, String status, IManageUser user, String ip)
        {
            // 网页使用一个定时器来清理过期
            if (_timer == null)
            {
                lock (typeof(UserOnline))
                {
                    if (_timer == null) _timer = new TimerX(s => ClearExpire(), null, 0, 30 * 1000) { Async = true };
                }
            }

            if (user == null) return SetStatus(sessionid, page, status, 0, null, ip);

            if (user is IAuthUser user2) user2.Online = true;
            (user as IEntity).SaveAsync(1000);

            return SetStatus(sessionid, page, status, user.ID, user + "", ip);
        }

        /// <summary>删除过期，指定过期时间</summary>
        /// <param name="secTimeout">超时时间，20 * 60秒</param>
        /// <returns></returns>
        public static IList<UserOnline> ClearExpire(Int32 secTimeout = 20 * 60)
        {
            if (Meta.Count == 0) return new List<UserOnline>();

            // 10分钟不活跃将会被删除
            var exp = _.UpdateTime < DateTime.Now.AddSeconds(-secTimeout);
            var list = FindAll(exp, null, null, 0, 0);
            list.Delete();

            // 设置离线
            foreach (var item in list)
            {
                var user = ManageProvider.Provider.FindByID(item.UserID);
                if (user is IAuthUser user2)
                {
                    user2.Online = false;
                    user2.Save();
                }
            }

            return list;
        }
        #endregion
    }
}