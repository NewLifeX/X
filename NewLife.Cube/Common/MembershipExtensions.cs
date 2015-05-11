using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using XCode.Membership;

namespace NewLife.Cube.Common
{
    /// <summary>用户扩展</summary>
    public static class MembershipExtensions
    {
        /// <summary>用户只有拥有当前菜单的指定权限</summary>
        /// <param name="user"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public static Boolean Has(this IUser user, PermissionFlags flag)
        {
            if (user == null || user.Role == null) return false;

            var menu = ManageProvider.Menu.Current;
            if (menu == null) throw new Exception("无法定位当前权限菜单！");

            return user.Role.Has(menu.ID, flag);
        }
    }
}