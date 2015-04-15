using System.Web.Mvc;
using NewLife.Log;

namespace NewLife.Cube
{
    /// <summary>拦截错误的特性</summary>
    public class MvcHandleErrorAttribute : HandleErrorAttribute
    {
        /// <summary>拦截异常</summary>
        /// <param name="filterContext"></param>
        public override void OnException(ExceptionContext filterContext)
        {
            XTrace.WriteException(filterContext.Exception);
            filterContext.ExceptionHandled = true;

            var vr = new ViewResult();
            vr.ViewName = "Error";
            vr.ViewBag.Context = filterContext;

            filterContext.Result = vr;

            base.OnException(filterContext);
        }
    }
}