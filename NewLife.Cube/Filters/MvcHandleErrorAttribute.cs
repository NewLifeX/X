using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NewLife.Log;

namespace NewLife.Cube.Filters
{
    /// <summary>拦截错误的特性</summary>
    public class MvcHandleErrorAttribute : HandleErrorAttribute
    {
        public override void OnException(ExceptionContext filterContext)
        {
            XTrace.WriteException(filterContext.Exception);

            base.OnException(filterContext);
        }
    }
}