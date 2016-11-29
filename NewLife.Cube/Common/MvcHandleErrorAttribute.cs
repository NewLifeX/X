using System.Web.Mvc;
using NewLife.Log;

namespace NewLife.Cube
{
    /// <summary>拦截错误的特性</summary>
    public class MvcHandleErrorAttribute : HandleErrorAttribute
    {
        /// <summary>拦截异常</summary>
        /// <param name="ctx"></param>
        public override void OnException(ExceptionContext ctx)
        {
            // 判断控制器是否在管辖范围之内，不拦截其它控制器的异常信息
            if (!ctx.ExceptionHandled && AreaRegistrationBase.Contains(ctx.Controller))
            {
                XTrace.WriteException(ctx.Exception);
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