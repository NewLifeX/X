using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Entity
{
    /// <summary>用户令牌。授权其他人直接拥有指定用户的身份，支持有效期，支持数据接口</summary>
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public partial class UserToken : Entity<UserToken>
    {
        #region 对象操作
        static UserToken()
        {
            // 累加字段
            //Meta.Factory.AdditionalFields.Add(__.Logins);

            // 过滤器 UserModule、TimeModule、IPModule
            Meta.Modules.Add<UserModule>();
            Meta.Modules.Add<TimeModule>();
            Meta.Modules.Add<IPModule>();


            var sc = Meta.SingleCache;
            sc.FindSlaveKeyMethod = k => Find(_.Token == k);
            sc.GetSlaveKeyMethod = e => e.Token;
            sc.SlaveKeyIgnoreCase = true;
        }

        /// <summary>检查参数</summary>
        /// <param name="isNew"></param>
        public override void Valid(Boolean isNew)
        {
            if (UserID <= 0) throw new ArgumentNullException(nameof(UserID));
        }
        #endregion

        #region 扩展属性
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id">编号</param>
        /// <returns>实体对象</returns>
        public static UserToken FindByID(Int32 id)
        {
            if (id <= 0) return null;

            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.ID == id);

            // 单对象缓存
            //return Meta.SingleCache[id];

            return Find(_.ID == id);
        }

        /// <summary>根据令牌查找</summary>
        /// <param name="token">令牌</param>
        /// <returns>实体对象</returns>
        public static UserToken FindByToken(String token)
        {
            if (token.IsNullOrEmpty()) return null;

            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Token == token);

            //return Find(_.Token == token);
            return Meta.SingleCache[token] as UserToken;
        }

        /// <summary>根据用户查找</summary>
        /// <param name="userid">用户</param>
        /// <returns>实体列表</returns>
        public static IList<UserToken> FindAllByUserID(Int32 userid)
        {
            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.UserID == userid);

            return FindAll(_.UserID == userid);
        }
        #endregion

        #region 高级查询
        /// <summary>高级查询</summary>
        /// <param name="token"></param>
        /// <param name="userid"></param>
        /// <param name="isEnable"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static IList<UserToken> Search(String token, Int32 userid, Boolean? isEnable, DateTime start, DateTime end, Pager p)
        {
            var exp = _.Expire.Between(start, end);
            if (userid >= 0) exp &= _.UserID == userid;
            if (isEnable != null) exp &= _.Enable == isEnable;
            if (!token.IsNullOrEmpty()) exp &= _.Token == token;

            return FindAll(exp, p);
        }
        #endregion

        #region 业务操作
        /// <summary>验证token是否可用</summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static IUser Valid(String token)
        {
            if (token.IsNullOrEmpty()) throw new Exception("缺少令牌token");

            var ut = UserToken.FindByToken(token);
            if (ut == null) throw new Exception("无效令牌token");

            ut.Times++;
            ut.LastIP = WebHelper.UserHost;
            ut.LastTime = DateTime.Now;
            ut.SaveAsync(5000);

            if (!ut.Enable || ut.UserID <= 0) throw new Exception("令牌已停用");

            if (ut.Expire.Year > 2000 && ut.Expire < DateTime.Now) throw new Exception("令牌已过期");

            var user = ManageProvider.Provider.FindByID(ut.UserID) as IUser;
            if (user == null || !user.Enable) throw new Exception("无效令牌身份");

            // 拥有系统角色
            if (!user.Roles.Any(r => r.IsSystem)) throw new Exception("无权查看该数据");

            return user;
        }
        #endregion
    }
}