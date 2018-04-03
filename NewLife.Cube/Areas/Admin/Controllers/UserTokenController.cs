using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using NewLife.Cube.Entity;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>用户令牌控制器</summary>
    [DisplayName("用户令牌")]
    [Description("授权其他人直接拥有指定用户的身份，支持有效期，支持数据接口")]
    public class UserTokenController : EntityController<UserToken>
    {
        static UserTokenController()
        {
            MenuOrder = 40;

            FormFields.RemoveField("UserID");
        }

        /// <summary>搜索数据集</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected override IEnumerable<UserToken> Search(Pager p)
        {
            var token = p["Q"];
            var userid = p["UserID"].ToInt(-1);

            // 强制当前用户
            if (userid < 0)
            {
                var user = ManageProvider.Provider?.Current;
                userid = user.ID;
            }

            return UserToken.Search(token, userid, null, p["dtStart"].ToDateTime(), p["dtEnd"].ToDateTime(), p);
        }

        ///// <summary>新增时</summary>
        ///// <param name="entity"></param>
        ///// <returns></returns>
        //protected override Int32 OnInsert(UserToken entity)
        //{
        //    // 强制当前用户
        //    if (entity.UserID <= 0)
        //    {
        //        var user = ManageProvider.Provider?.Current;
        //        entity.UserID = user.ID;
        //    }

        //    return base.OnInsert(entity);
        //}

        ///// <summary>更新时</summary>
        ///// <param name="entity"></param>
        ///// <returns></returns>
        //protected override Int32 OnUpdate(UserToken entity)
        //{
        //    var user = ManageProvider.Provider?.Current;
        //    if (entity.UserID != user.ID) throw new InvalidOperationException("越权访问数据！");

        //    return base.OnUpdate(entity);
        //}

        ///// <summary>删除时</summary>
        ///// <param name="entity"></param>
        ///// <returns></returns>
        //protected override Int32 OnDelete(UserToken entity)
        //{
        //    var user = ManageProvider.Provider?.Current;
        //    if (entity.UserID != user.ID) throw new InvalidOperationException("越权访问数据！");

        //    return base.OnDelete(entity);
        //}

        ///// <summary>查找</summary>
        ///// <param name="key"></param>
        ///// <returns></returns>
        //protected override UserToken Find(Object key)
        //{
        //    var entity = base.Find(key);

        //    var user = ManageProvider.Provider?.Current;
        //    if (entity.UserID != user.ID) throw new InvalidOperationException("越权访问数据！");

        //    return entity;
        //}

        /// <summary>验证权限</summary>
        /// <param name="entity"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        protected override Boolean ValidPermission(UserToken entity, DataObjectMethodType type)
        {
            var user = ManageProvider.Provider?.Current;

            // 特殊处理添加操作
            if (type == DataObjectMethodType.Insert && entity.UserID <= 0)
            {
                entity.UserID = user.ID;
            }

            return entity.UserID == user.ID;
        }

        /// <summary>菜单不可见</summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        protected override IDictionary<MethodInfo, Int32> ScanActionMenu(IMenu menu)
        {
            if (menu.Visible)
            {
                menu.Visible = false;
                (menu as IEntity).Save();
            }

            return base.ScanActionMenu(menu);
        }
    }
}