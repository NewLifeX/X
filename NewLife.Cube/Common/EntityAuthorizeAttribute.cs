using System;
using System.Web;
using System.Web.Mvc;
using XCode.Membership;

namespace NewLife.Cube
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

                var role = (user as IUser).Role;
                if (role.Has(1, Permission)) return;
            }

            HandleUnauthorizedRequest(filterContext);
        }
    }
}