using System;
using System.Collections.Generic;

namespace NewLife.Remoting
{
    /// <summary>定义操作筛选器中使用的方法。</summary>
    public interface IActionFilter
    {
        /// <summary>在执行操作方法之前调用。</summary>
        /// <param name="filterContext"></param>
        void OnActionExecuting(ControllerContext filterContext);

        /// <summary>在执行操作方法后调用。</summary>
        /// <param name="filterContext"></param>
        void OnActionExecuted(ControllerContext filterContext);
    }
}