using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
                var user = ManageProvider.User;
                if (!user.Roles.Any(e => e.IsSystem)) userid = user.ID;
            }

            return UserToken.Search(token, userid, null, p["dtStart"].ToDateTime(), p["dtEnd"].ToDateTime(), p);
        }

        /// <summary>验证权限</summary>
        /// <param name="entity">实体对象</param>
        /// <param name="type">操作类型</param>
        /// <param name="post">是否提交数据阶段</param>
        /// <returns></returns>
        protected override Boolean ValidPermission(UserToken entity, DataObjectMethodType type, Boolean post)
        {
            var user = ManageProvider.Provider?.Current;

            // 系统角色拥有特权
            if (user is UserX user2 && user2.Roles.Any(e => e.IsSystem)) return true;

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