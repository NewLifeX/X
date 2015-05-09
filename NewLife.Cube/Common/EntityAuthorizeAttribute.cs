using System;
using System.Web;
using System.Web.Mvc;
using NewLife.Log;
using XCode.Membership;

namespace NewLife.Cube
{
    /// <summary>实体授权特性</summary>
    public class EntityAuthorizeAttribute : AuthorizeAttribute
    {
        #region 属性
        private String _ResourceName;
        /// <summary>资源名称。判断当前登录用户是否有权访问该资源，资源由 区域/控制器/动作 三部分组成，左斜杠分割，若不完整则自动补上区域和控制器</summary>
        public String ResourceName { get { return _ResourceName; } set { _ResourceName = value; } }

        private PermissionFlags _Permission;
        /// <summary>授权项</summary>
        public PermissionFlags Permission { get { return _Permission; } set { _Permission = value; } }

        /// <summary>是否全局特性</summary>
        internal Boolean IsGlobal;
        #endregion

        #region 构造
        /// <summary>实例化实体授权特性</summary>
        public EntityAuthorizeAttribute() { }

        /// <summary>实例化实体授权特性</summary>
        /// <param name="resourceName"></param>
        /// <param name="permission"></param>
        public EntityAuthorizeAttribute(String resourceName, PermissionFlags permission = PermissionFlags.None)
        {
            ResourceName = resourceName;
            Permission = permission;
        }
        #endregion

        #region 方法
        /// <summary>授权核心</summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        protected override Boolean AuthorizeCore(HttpContextBase httpContext)
        {
            var user = ManageProvider.User;
            return user != null;
        }

        /// <summary>授权发生时触发</summary>
        /// <param name="filterContext"></param>
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            //// 基类方法会检查AllowAnonymous
            //base.OnAuthorization(filterContext);
            //if (filterContext.Result == null) return;

            var act = filterContext.ActionDescriptor;

            // 如果控制器或者Action放有该特性，则跳过全局
            if (IsGlobal)
            {
                if (act.IsDefined(typeof(EntityAuthorizeAttribute), true) || act.ControllerDescriptor.IsDefined(typeof(EntityAuthorizeAttribute), true)) return;
            }

            // 允许匿名访问时，直接跳过检查
            if (act.IsDefined(typeof(AllowAnonymousAttribute), true) || act.ControllerDescriptor.IsDefined(typeof(AllowAnonymousAttribute), true)) return;

            // 判断当前登录用户
            var user = ManageProvider.User;
            if (user == null)
            {
                HandleUnauthorizedRequest(filterContext);
                return;
            }

            // 控制权限的资源由 区域、控制器、动作 三部分组成
            // 资源名
            var res = ResourceName;
            var ss = res.Split("/", "\\", ".");

            var area = ss.Length >= 3 ? ss[ss.Length - 3] : null;
            var controller = ss.Length >= 2 ? ss[ss.Length - 2] : null;
            var action = ss.Length >= 1 ? ss[ss.Length - 1] : null;

            if (area.IsNullOrEmpty()) area = filterContext.RouteData.DataTokens["Area"] + "";
            if (controller.IsNullOrEmpty()) controller = act.ControllerDescriptor.ControllerName;
            if (action.IsNullOrEmpty()) action = act.ActionName;

            // 如果不足三部分，则需要在前面加上区域名
            res = "{0}/{1}/{2}".F(area, controller, action);

            var menu = ManageProvider.Menu.Root.FindByPath(res);
            if (menu != null)
            {
                var role = (user as IUser).Role;
                if (role.Has(menu.ID, Permission)) return;
            }
            else
            {
                XTrace.WriteLine("设计错误！验证权限时无法找到[{0}]的菜单", res);
            }

            var vr = new ViewResult();
            vr.ViewName = "NoPermission";
            vr.ViewBag.Context = filterContext;
            vr.ViewBag.Resource = menu != null ? (menu + "") : res;
            vr.ViewBag.Permission = Permission;

            filterContext.Result = vr;

        }
        #endregion
    }
}