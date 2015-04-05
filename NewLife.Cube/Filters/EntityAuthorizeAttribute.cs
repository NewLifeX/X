using System;
using System.Web;
using System.Web.Mvc;
using NewLife.Cube.Controllers;
using NewLife.Log;
using XCode.Membership;

namespace NewLife.Cube.Filters
{
    public class EntityAuthorizeAttribute : AuthorizeAttribute
    {
        protected override Boolean AuthorizeCore(HttpContextBase httpContext)
        {
            var user = MemberProvider.User;
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
            var user = MemberProvider.User;
            if (user != null)
            {
                // 控制器基类
                var type = filterContext.Controller.GetType().BaseType;
                // 如果不是实体控制器基类，无法做更细的检查，授权通过
                if (!type.IsGenericType) return;

                if (typeof(EntityController<>).IsAssignableFrom(type.GetGenericTypeDefinition()))
                {
                    var entity = type.GetGenericArguments()[0];
                    XTrace.WriteLine("{0}.{1}", entity.FullName, filterContext.ActionDescriptor.ActionName);

                    return;
                }
            }

            HandleUnauthorizedRequest(filterContext);
        }
    }
}