using System;
using System.Web.Mvc;
using XCode.Membership;

namespace NewLife.Cube
{
    /// <summary>用户扩展</summary>
    public static class MembershipExtensions
    {
        /// <summary>用户只有拥有当前菜单的指定权限</summary>
        /// <param name="user">指定用户</param>
        /// <param name="flags">是否拥有多个权限中的任意一个，或的关系。如果需要表示与的关系，可以传入一个多权限位合并</param>
        /// <returns></returns>
        [Obsolete]
        public static Boolean Has(this IUser user, params PermissionFlags[] flags)
        {
            if (user == null || user.Role == null) return false;

            var menu = ManageProvider.Menu.Current;
            if (menu == null) throw new Exception("无法定位当前权限菜单！");

            // 如果没有指定权限子项，则指判断是否拥有资源
            if (flags == null || flags.Length == 0) return user.Role.Has(menu.ID);

            //return user.Role.Has(menu.ID, flag);
            foreach (var item in flags)
            {
                // 菜单必须拥有这些权限位才行
                if (menu.Permissions.ContainsKey((Int32)item))
                {
                    // 如果判断None，则直接返回
                    if (item == PermissionFlags.None) return true;

                    if (user.Role.Has(menu.ID, item)) return true;
                }
            }
            return false;
        }

        /// <summary>用户只有拥有当前菜单的指定权限</summary>
        /// <param name="user">指定用户</param>
        /// <param name="respath"></param>
        /// <param name="flags">是否拥有多个权限中的任意一个，或的关系。如果需要表示与的关系，可以传入一个多权限位合并</param>
        /// <returns></returns>
        [Obsolete]
        public static Boolean Has(this IUser user, String respath, params PermissionFlags[] flags)
        {
            if (user == null || user.Role == null) return false;

            var menu = ManageProvider.Menu.Root.FindByPath(respath);
            if (menu == null) throw new XException("无法定位权限菜单{0}！", respath);

            // 如果没有指定权限子项，则指判断是否拥有资源
            if (flags == null || flags.Length == 0) return user.Role.Has(menu.ID);

            foreach (var item in flags)
            {
                // 菜单必须拥有这些权限位才行
                if (menu.Permissions.ContainsKey((Int32)item))
                {
                    // 如果判断None，则直接返回
                    if (item == PermissionFlags.None) return true;

                    if (user.Role.Has(menu.ID, item)) return true;
                }
            }
            return false;
        }

        /// <summary>用户只有拥有当前菜单的指定权限</summary>
        /// <param name="page">页面</param>
        /// <param name="flags">是否拥有多个权限中的任意一个，或的关系。如果需要表示与的关系，可以传入一个多权限位合并</param>
        /// <returns></returns>
        public static Boolean Has(this WebViewPage page, params PermissionFlags[] flags)
        {
            // 没有用户时无权
            var user = page.ViewBag.User as IUser ?? page.User.Identity as IUser;
            if (user == null) return false;

            // 没有菜单时不做权限控制
            var menu = page.ViewBag.Menu as IMenu;
            if (menu == null) return true;

            return user.Has(menu, flags);
        }
    }
}