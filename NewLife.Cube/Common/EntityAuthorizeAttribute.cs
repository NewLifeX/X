using System;
using System.Web;
using System.Web.Mvc;
using NewLife.Log;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube
{
    /// <summary>实体授权特性</summary>
    public class EntityAuthorizeAttribute : AuthorizeAttribute
    {
        #region 属性
        /// <summary>授权项</summary>
        public PermissionFlags Permission { get; }

        /// <summary>是否全局特性</summary>
        internal Boolean IsGlobal;
        #endregion

        #region 构造
        /// <summary>实例化实体授权特性</summary>
        public EntityAuthorizeAttribute() { }

        /// <summary>实例化实体授权特性</summary>
        /// <param name="permission"></param>
        public EntityAuthorizeAttribute(PermissionFlags permission)
        {
            if (permission <= PermissionFlags.None) throw new ArgumentNullException(nameof(permission));

            Permission = permission;
        }
        #endregion

        #region 方法
        /// <summary>授权核心</summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        protected override Boolean AuthorizeCore(HttpContextBase httpContext) => httpContext.User?.Identity is IUser user;

        /// <summary>授权发生时触发</summary>
        /// <param name="filterContext"></param>
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            // 只验证管辖范围
            if (!AreaRegistrationBase.Contains(filterContext.Controller)) return;

            var prv = ManageProvider.Provider;
            prv.SetPrincipal();

            var act = filterContext.ActionDescriptor;
            var ctrl = act.ControllerDescriptor;

            // 根据控制器定位资源菜单
            var ctx = filterContext.HttpContext;
            var menu = ctx.Items["CurrentMenu"] as IMenu;
            if (menu == null)
            {
                var mf = ManageProvider.Menu;
                var m1 = ctrl.ControllerType.FullName;
                var m2 = m1 + "." + act.ActionName;
                menu = mf.FindByFullName(m2) ?? mf.FindByFullName(m1);

                // 当前菜单
                filterContext.Controller.ViewBag.Menu = menu;
                // 兼容旧版本视图权限
                ctx.Items["CurrentMenu"] = menu;
            }

            // 如果控制器或者Action放有该特性，则跳过全局
            if (IsGlobal)
            {
                if (act.IsDefined(typeof(EntityAuthorizeAttribute), true) || ctrl.IsDefined(typeof(EntityAuthorizeAttribute), true)) return;
            }

            // 允许匿名访问时，直接跳过检查
            if (act.IsDefined(typeof(AllowAnonymousAttribute), true) || ctrl.IsDefined(typeof(AllowAnonymousAttribute), true)) return;

            // 判断当前登录用户
            var user = prv.TryLogin();
            if (user == null)
            {
                var retUrl = ctx.Request.Url?.PathAndQuery;

                var rurl = "~/Admin/User/Login".AppendReturn(retUrl);
                ctx.Response.Redirect(rurl);
                return;
            }

            // 判断权限
            if (menu != null && user is IUser user2)
            {
                if (user2.Has(menu, Permission)) return;
            }
            else
            {
                XTrace.WriteLine("设计错误！验证权限时无法找到[{0}/{1}]的菜单", ctrl.ControllerType.FullName, act.ActionName);
            }

            var res = "[{0}/{1}]".F(ctrl.ControllerName, act.ActionName);
            var msg = "访问资源 {0} 需要 {1} 权限".F(res, Permission.GetDescription());
            LogProvider.Provider.WriteLog("访问", "拒绝", msg);

            var vr = new ViewResult()
            {
                ViewName = "NoPermission"
            };
            vr.ViewBag.Context = filterContext;
            vr.ViewBag.Resource = res;
            vr.ViewBag.Permission = Permission;
            vr.ViewBag.Menu = menu;

            filterContext.Result = vr;
        }
        #endregion
    }
}