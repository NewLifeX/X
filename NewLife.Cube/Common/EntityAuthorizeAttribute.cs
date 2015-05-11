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
        /// <summary>资源名称。需要增加新菜单而不需要控制器名称时，指定资源名称</summary>
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
        /// <param name="permission"></param>
        public EntityAuthorizeAttribute(PermissionFlags permission = PermissionFlags.None)
        {
            //ResourceName = resourceName;
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

            // 根据请求Url定位资源菜单
            var url = filterContext.HttpContext.Request.AppRelativeCurrentExecutionFilePath;
            var menu = ManageProvider.Menu.Current;
            if (menu != null)
            {
                var role = (user as IUser).Role;
                if (role.Has(menu.ID, Permission)) return;
            }
            else
            {
                XTrace.WriteLine("设计错误！验证权限时无法找到[{0}]的菜单", url);
            }

            var vr = new ViewResult();
            vr.ViewName = "NoPermission";
            vr.ViewBag.Context = filterContext;
            vr.ViewBag.Resource = menu != null ? (menu + "") : url;
            vr.ViewBag.Permission = Permission;

            filterContext.Result = vr;

        }
        #endregion
    }
}