/*
 * XCoder v6.9.6383.25987
 * 作者：nnhy/STONE-PC
 * 时间：2017-07-05 15:34:41
 * 版权：版权所有 (C) 新生命开发团队 2002~2017
*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using NewLife;
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
            //// 用于引发基类的静态构造函数，所有层次的泛型实体类都应该有一个
            //var entity = new UserOnline();

            Meta.Modules.Add<TimeModule>();
            Meta.Modules.Add<IPModule>();

            var df = Meta.Factory.AdditionalFields;
            df.Add(__.Times);
            //df.Add(__.OnlineTime);

            var sc = Meta.SingleCache;
            if (sc.Expire < 20 * 60) sc.Expire = 20 * 60;
            sc.FindSlaveKeyMethod = k => Find(__.SessionID, k);
            sc.GetSlaveKeyMethod = e => e.SessionID;

#if !DEBUG
            //// 关闭SQL日志
            //Meta.Session.Dal.Db.ShowSQL = false;
#endif
        }

        /// <summary>验证数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew"></param>
        public override void Valid(Boolean isNew)
        {
            // 截取长度
            var len = _.Status.Length;
            if (len <= 0) len = 50;
            if (!Status.IsNullOrEmpty() && Status.Length > len) Status = Status.Substring(0, len);

            len = _.Page.Length;
            if (len <= 0) len = 50;
            if (!Page.IsNullOrEmpty() && Page.Length > len) Page = Page.Substring(0, len);

            //len = _.Title.Length;
            //if (len <= 0) len = 50;
            //if (!Title.IsNullOrEmpty() && Title.Length > len) Title = Title.Substring(0, len);
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
        /// <param name="cache"></param>
        /// <returns></returns>
        public static UserOnline FindBySessionID(String sessionid, Boolean cache = true)
        {
            if (sessionid.IsNullOrEmpty()) return null;

            if (cache)
                return Meta.SingleCache.GetItemWithSlaveKey(sessionid) as UserOnline;
            else
                return Find(__.SessionID, sessionid);
        }

        /// <summary>根据用户编号查找</summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        public static IList<UserOnline> FindAllByUserID(Int32 userid)
        {
            if (userid <= 0) return new List<UserOnline>();

            return FindAll(_.UserID == userid);
        }
        #endregion

        #region 高级查询
        /// <summary>查询满足条件的记录集，分页、排序</summary>
        /// <param name="userid">用户编号</param>
        /// <param name="start">开始时间</param>
        /// <param name="end">结束时间</param>
        /// <param name="key">关键字</param>
        /// <param name="param">分页排序参数，同时返回满足条件的总记录数</param>
        /// <returns>实体集</returns>
        public static IList<UserOnline> Search(Int32 userid, DateTime start, DateTime end, String key, PageParameter param)
        {
            var exp = new WhereExpression();

            if (userid >= 0) exp &= _.UserID == userid;
            exp &= _.UpdateTime.Between(start, end);

            if (!key.IsNullOrEmpty()) exp &= _.Name.Contains(key) | _.Page.Contains(key) | _.Status.Contains(key);

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
            var entity = GetOrAdd(sessionid, FindBySessionID, k => new UserOnline { SessionID = k, CreateIP = ip, CreateTime = DateTime.Now });
            //var entity = FindBySessionID(sessionid) ?? new UserOnline();
            //entity.SessionID = sessionid;
            entity.Page = page;
            entity.Status = status;

            entity.Times++;
            if (userid > 0) entity.UserID = userid;
            if (!name.IsNullOrEmpty()) entity.Name = name;

            // 累加在线时间
            entity.UpdateTime = DateTime.Now;
            entity.UpdateIP = ip;
            entity.OnlineTime = (Int32)(entity.UpdateTime - entity.CreateTime).TotalSeconds;
            entity.SaveAsync();

            Interlocked.Increment(ref _onlines);

            return entity;
        }

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
            StartTimer();

            if (user == null) return SetStatus(sessionid, page, status, 0, null, ip);

            //if (user is IAuthUser user2) user2.Online = true;
            //(user as IEntity).SaveAsync(1000);

            return SetStatus(sessionid, page, status, user.ID, user + "", ip);
        }

        private static TimerX _timer;
        private static Int32 _onlines;
        /// <summary>
        /// 启动定时器，定时清理离线用户
        /// </summary>
        /// <param name="period"></param>
        public static void StartTimer(Int32 period = 60)
        {
            if (_timer == null)
            {
                lock (typeof(UserOnline))
                {
                    if (_timer == null) _timer = new TimerX(s => ClearExpire(), null, 1000, period * 1000) { Async = true };
                }
            }
        }

        /// <summary>
        /// 关闭定时器
        /// </summary>
        public static void StopTimer()
        {
            _timer.TryDispose();
            _timer = null;
        }

        /// <summary>删除过期，指定过期时间</summary>
        /// <param name="secTimeout">超时时间，20 * 60秒</param>
        /// <returns></returns>
        public static IList<UserOnline> ClearExpire(Int32 secTimeout = 20 * 60)
        {
            // 无在线则不执行
            if (_onlines == 0 || Meta.Count == 0) return new List<UserOnline>();

            // 10分钟不活跃将会被删除
            var exp = _.UpdateTime < DateTime.Now.AddSeconds(-secTimeout);
            var list = FindAll(exp, null, null, 0, 0);
            list.Delete();

            // 修正在线数
            _onlines = Meta.Count - list.Count;

            // 设置离线
            foreach (var item in list)
            {
                var user = ManageProvider.Provider.FindByID(item.UserID);
                if (user is User user2)
                {
                    user2.Online = false;
                    user2.Save();
                }
            }

            // 设置统计

            return list;
        }
        #endregion
    }
}