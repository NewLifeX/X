using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using NewLife.Log;

namespace NewLife.Cube
{
    /// <summary>拦截错误的特性</summary>
    public class MvcHandleErrorAttribute : HandleErrorAttribute
    {
        private static HashSet<String> NotFoundFiles = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

        /// <summary>拦截异常</summary>
        /// <param name="ctx"></param>
        public override void OnException(ExceptionContext ctx)
        {
            if (ctx.ExceptionHandled) return;

            //XTrace.WriteException(ctx.Exception);
            var ex = ctx.Exception?.GetTrue();
            if (ex != null)
            {
                // 避免反复出现缺少文件
                if (ex is HttpException hex && (UInt32)hex.ErrorCode == 0x80004005)
                {
                    var url = HttpContext.Current.Request.RawUrl + "";
                    if (!NotFoundFiles.Contains(url))
                        NotFoundFiles.Add(url);
                    else
                        ex = null;
                }

                // 拦截没有权限
                if (ex is NoPermissionException nex)
                {
                    ctx.Result = ctx.Controller.NoPermission(nex);
                    ctx.ExceptionHandled = true;
                }

                if (ex != null) XTrace.WriteException(ex);
            }
            if (ctx.ExceptionHandled) return;

            // 判断控制器是否在管辖范围之内，不拦截其它控制器的异常信息
            if (Setting.Current.CatchAllException || AreaRegistrationBase.Contains(ctx.Controller))
            {
                ctx.ExceptionHandled = true;

                var ctrl = "";
                var act = "";
                if (ctx.RouteData.Values.ContainsKey("controller")) ctrl = ctx.RouteData.Values["controller"] + "";
                if (ctx.RouteData.Values.ContainsKey("action")) act = ctx.RouteData.Values["action"] + "";

                if (ctx.RequestContext.HttpContext.Request.IsAjaxRequest())
                {
                    if (act.IsNullOrEmpty()) act = "操作";
                    ctx.Result = ControllerHelper.JsonTips("[{0}]失败！{1}".F(act, ex.Message));
                }
                else
                {
                    var vr = new ViewResult
                    {
                        ViewName = "CubeError"
                    };
                    vr.ViewBag.Context = ctx;

                    var vd = vr.ViewData = ctx.Controller.ViewData;
                    vd.Model = new HandleErrorInfo(ex, ctrl, act);

                    ctx.Result = vr;
                }
            }

            base.OnException(ctx);
        }
    }
}