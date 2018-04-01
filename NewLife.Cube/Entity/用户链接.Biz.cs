using System;
using System.Collections.Generic;
using NewLife.Serialization;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Entity
{
    /// <summary>用户链接。第三方绑定</summary>
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public partial class UserConnect : Entity<UserConnect>
    {
        #region 对象操作
        static UserConnect()
        {
            // 累加字段
            //Meta.Factory.AdditionalFields.Add(__.Logins);

            // 过滤器 UserModule、TimeModule、IPModule
            Meta.Modules.Add<UserModule>();
            Meta.Modules.Add<TimeModule>();
            Meta.Modules.Add<IPModule>();
        }

        /// <summary>验证数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew">是否插入</param>
        public override void Valid(Boolean isNew)
        {
            // 如果没有脏数据，则不需要进行任何处理
            if (!HasDirty) return;

            // 在新插入数据或者修改了指定字段时进行修正
            // 处理当前已登录用户信息，可以由UserModule过滤器代劳
            /*var user = ManageProvider.User;
            if (user != null)
            {
                if (isNew && !Dirtys[nameof(CreateUserID)) nameof(CreateUserID) = user.ID;
                if (!Dirtys[nameof(UpdateUserID)]) nameof(UpdateUserID) = user.ID;
            }*/
            //if (isNew && !Dirtys[nameof(CreateTime)]) nameof(CreateTime) = DateTime.Now;
            //if (!Dirtys[nameof(UpdateTime)]) nameof(UpdateTime) = DateTime.Now;
            //if (isNew && !Dirtys[nameof(CreateIP)]) nameof(CreateIP) = WebHelper.UserHost;
            //if (!Dirtys[nameof(UpdateIP)]) nameof(UpdateIP) = WebHelper.UserHost;

            // 检查唯一索引
            // CheckExist(isNew, __.Provider, __.UserID);
        }

        ///// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        //[EditorBrowsable(EditorBrowsableState.Never)]
        //protected override void InitData()
        //{
        //    // InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用
        //    if (Meta.Session.Count > 0) return;

        //    if (XTrace.Debug) XTrace.WriteLine("开始初始化UserConnect[用户链接]数据……");

        //    var entity = new UserConnect();
        //    entity.ID = 0;
        //    entity.Provider = "abc";
        //    entity.UserID = 0;
        //    entity.OpenID = "abc";
        //    entity.LinkID = 0;
        //    entity.NickName = "abc";
        //    entity.Avatar = "abc";
        //    entity.AccessToken = "abc";
        //    entity.RefreshToken = "abc";
        //    entity.Expire = DateTime.Now;
        //    entity.Enable = true;
        //    entity.CreateUserID = 0;
        //    entity.CreateIP = "abc";
        //    entity.CreateTime = DateTime.Now;
        //    entity.UpdateUserID = 0;
        //    entity.UpdateIP = "abc";
        //    entity.UpdateTime = DateTime.Now;
        //    entity.Remark = "abc";
        //    entity.Insert();

        //    if (XTrace.Debug) XTrace.WriteLine("完成初始化UserConnect[用户链接]数据！");
        //}

        ///// <summary>已重载。基类先调用Valid(true)验证数据，然后在事务保护内调用OnInsert</summary>
        ///// <returns></returns>
        //public override Int32 Insert()
        //{
        //    return base.Insert();
        //}

        ///// <summary>已重载。在事务保护范围内处理业务，位于Valid之后</summary>
        ///// <returns></returns>
        //protected override Int32 OnDelete()
        //{
        //    return base.OnDelete();
        //}
        #endregion

        #region 扩展属性
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id">编号</param>
        /// <returns>实体对象</returns>
        public static UserConnect FindByID(Int32 id)
        {
            if (id <= 0) return null;

            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.ID == id);

            // 单对象缓存
            //return Meta.SingleCache[id];

            return Find(_.ID == id);
        }

        /// <summary>根据提供商、用户查找</summary>
        /// <param name="provider">提供商</param>
        /// <param name="openid">身份标识</param>
        /// <returns>实体对象</returns>
        public static UserConnect FindByProviderAndOpenID(String provider, String openid)
        {
            //// 实体缓存
            //if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Provider == provider && e.OpenID == openid);

            return Find(_.Provider == provider & _.OpenID == openid);
        }

        /// <summary>根据用户查找</summary>
        /// <param name="userid">用户</param>
        /// <returns>实体列表</returns>
        public static IList<UserConnect> FindAllByUserID(Int32 userid)
        {
            //// 实体缓存
            //if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.UserID == userid);

            return FindAll(_.UserID == userid);
        }
        #endregion

        #region 高级查询
        #endregion

        #region 业务操作
        /// <summary>填充用户</summary>
        /// <param name="client"></param>
        public virtual void Fill(OAuthClient client)
        {
            var uc = this;
            if (!client.NickName.IsNullOrEmpty()) uc.NickName = client.NickName;
            if (!client.Avatar.IsNullOrEmpty()) uc.Avatar = client.Avatar;

            uc.LinkID = client.UserID;
            //ub.OpenID = client.OpenID;
            uc.AccessToken = client.AccessToken;
            uc.RefreshToken = client.RefreshToken;
            uc.Expire = client.Expire;

            if (client.Items != null) uc.Remark = client.Items.ToJson();
        }
        #endregion
    }
}