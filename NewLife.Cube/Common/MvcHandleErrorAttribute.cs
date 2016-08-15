using System.Linq;
using System.Web.Mvc;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Cube
{
    /// <summary>拦截错误的特性</summary>
    public class MvcHandleErrorAttribute : HandleErrorAttribute
    {
        /// <summary>拦截异常</summary>
        /// <param name="filterContext"></param>
        public override void OnException(ExceptionContext filterContext)
        {
            if (!filterContext.ExceptionHandled)
            {
                // 判断控制器是否在管辖范围之内，不拦截其它控制器的异常信息
                var ns = filterContext.Controller.GetType().Name;
                if (ns.EndsWith(".Controllers"))
                {
                    var list = typeof(AreaRegistrationBase).GetAllSubclasses().ToList();
                    if (!list.Any(e => e.Namespace == ns))
                    {
                        XTrace.WriteException(filterContext.Exception);
                        filterContext.ExceptionHandled = true;

                        var vr = new ViewResult();
                        vr.ViewName = "Error";
                        vr.ViewBag.Context = filterContext;

                        filterContext.Result = vr;
                    }
                }
            }

            base.OnException(filterContext);
        }
    }
}