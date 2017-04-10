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
            // 判断控制器是否在管辖范围之内，不拦截其它控制器的异常信息
            if (!ctx.ExceptionHandled && AreaRegistrationBase.Contains(ctx.Controller))
            {
                //XTrace.WriteException(ctx.Exception);
                var ex = ctx.Exception?.GetTrue();
                if (ex != null)
                {
                    // 避免反复出现缺少文件
                    var hex = ex as HttpException;
                    if (hex != null && (UInt32)hex.ErrorCode == 0x80004005)
                    {
                        var url = HttpContext.Current.Request.RawUrl + "";
                        if (!NotFoundFiles.Contains(url))
                            NotFoundFiles.Add(url);
                        else
                            ex = null;
                    }

                    if (ex != null) XTrace.WriteException(ex);
                }

                ctx.ExceptionHandled = true;

                var vr = new ViewResult();
                vr.ViewName = "Error";
                vr.ViewBag.Context = ctx;

                ctx.Result = vr;
            }

            base.OnException(ctx);
        }
    }
}