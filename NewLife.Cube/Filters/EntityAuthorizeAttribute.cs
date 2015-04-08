using System;
using System.Web;
using System.Web.Mvc;
using NewLife.Reflection;
using XCode.Membership;

namespace NewLife.Cube.Filters
{
    /// <summary>实体授权特性</summary>
    public class EntityAuthorizeAttribute : AuthorizeAttribute
    {
        private String _ResourceName;
        /// <summary>资源名称。判断当前登录用户是否有权访问该资源</summary>
        public String ResourceName { get { return _ResourceName; } set { _ResourceName = value; } }

        private PermissionFlags _Permission;
        /// <summary>授权项</summary>
        public PermissionFlags Permission { get { return _Permission; } set { _Permission = value; } }

        protected override Boolean AuthorizeCore(HttpContextBase httpContext)
        {
            var user = ManageProvider.User;
            return user != null;
        }

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            //// 基类方法会检查AllowAnonymous
            //base.OnAuthorization(filterContext);
            //if (filterContext.Result == null) return;

            // 允许匿名访问时，直接跳过检查
            var act = filterContext.ActionDescriptor;
            if (act.IsDefined(typeof(AllowAnonymousAttribute), true) || act.ControllerDescriptor.IsDefined(typeof(AllowAnonymousAttribute), true)) return;

            // 判断当前登录用户
            var user = ManageProvider.User;
            if (user != null)
            {
                var res = ResourceName;
                if (res.IsNullOrEmpty()) res = act.ControllerDescriptor.ControllerName.TrimEnd("Controller");

                var eop = ManageProvider.Provider.GetService<IMenu>();

                var role = user as IUser;
                if (role.Acquire(1, Permission)) return;
            }

            HandleUnauthorizedRequest(filterContext);
        }
    }
}