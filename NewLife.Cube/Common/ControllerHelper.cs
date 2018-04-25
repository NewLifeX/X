using System;
using System.Web.Mvc;
using XCode.Membership;

namespace NewLife.Cube
{
    /// <summary>控制器帮助类</summary>
    public static class ControllerHelper
    {
        #region Json响应
        /// <summary>返回结果并跳转</summary>
        /// <param name="data">结果。可以是错误文本、成功文本、其它结构化数据</param>
        /// <param name="url">提示信息后跳转的目标地址，[refresh]表示刷新当前页</param>
        /// <returns></returns>
        public static ActionResult JsonTips(Object data, String url = null)
        {
            var vr = new JsonResult
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
            //vr.Data = data;
            //vr.ContentType = contentType;
            //vr.ContentEncoding = contentEncoding;

            if (data is Exception ex)
                vr.Data = new { result = false, data = ex.GetTrue()?.Message, url };
            else
                vr.Data = new { result = true, data, url };

            return vr;
        }

        /// <summary>返回结果并刷新</summary>
        /// <param name="data">消息</param>
        /// <returns></returns>
        public static ActionResult JsonRefresh(Object data) => JsonTips(data, "[refresh]");
        #endregion

        /// <summary>无权访问</summary>
        /// <param name="filterContext"></param>
        /// <param name="pm"></param>
        /// <returns></returns>
        public static ActionResult NoPermission(this AuthorizationContext filterContext, PermissionFlags pm)
        {
            var act = filterContext.ActionDescriptor;
            var ctrl = act.ControllerDescriptor;

            var res = "[{0}/{1}]".F(ctrl.ControllerName, act.ActionName);
            var msg = "访问资源 {0} 需要 {1} 权限".F(res, pm.GetDescription());
            LogProvider.Provider.WriteLog("访问", "拒绝", msg);

            var ctx = filterContext.HttpContext;
            var menu = ctx.Items["CurrentMenu"] as IMenu;

            var vr = new ViewResult()
            {
                ViewName = "NoPermission"
            };
            vr.ViewBag.Context = filterContext;
            vr.ViewBag.Resource = res;
            vr.ViewBag.Permission = pm;
            vr.ViewBag.Menu = menu;

            return vr;
        }

        /// <summary>无权访问</summary>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="pm"></param>
        /// <returns></returns>
        public static ActionResult NoPermission(this Controller controller, String action, PermissionFlags pm)
        {
            var res = "[{0}/{1}]".F(controller.GetType().Name.TrimEnd("Controller"), action);
            var msg = "访问资源 {0} 需要 {1} 权限".F(res, pm.GetDescription());
            LogProvider.Provider.WriteLog("访问", "拒绝", msg);

            var ctx = controller.HttpContext;
            var menu = ctx.Items["CurrentMenu"] as IMenu;

            var vr = new ViewResult()
            {
                ViewName = "NoPermission"
            };
            vr.ViewBag.Resource = res;
            vr.ViewBag.Permission = pm;
            vr.ViewBag.Menu = menu;

            return vr;
        }

        /// <summary>无权访问</summary>
        /// <param name="controller"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static ActionResult NoPermission(this ControllerBase controller, NoPermissionException ex)
        {
            var ctx = controller.ControllerContext.HttpContext;
            var res = ctx.Request.Url.AbsolutePath;
            var pm = ex.Permission;
            var msg = "无权访问数据[{0}]，没有该数据的 {1} 权限".F(res, pm.GetDescription());
            LogProvider.Provider.WriteLog("访问", "拒绝", msg);

            var menu = ctx.Items["CurrentMenu"] as IMenu;

            var vr = new ViewResult()
            {
                ViewName = "NoPermission"
            };
            vr.ViewBag.Resource = res;
            vr.ViewBag.Permission = pm;
            vr.ViewBag.Menu = menu;

            return vr;
        }
    }
}