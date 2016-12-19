namespace NewLife.Remoting
{
    /// <summary>表示筛选器特性的基类。</summary>
    public abstract class ActionFilterAttribute : FilterAttribute, IActionFilter
    {
        /// <summary>在执行操作方法之前由框架调用。</summary>
        /// <param name="filterContext">筛选器上下文。</param>
        public virtual void OnActionExecuting(ActionExecutingContext filterContext) { }

        /// <summary>在执行操作方法后由框架调用。</summary>
        /// <param name="filterContext">筛选器上下文。</param>
        public virtual void OnActionExecuted(ActionExecutedContext filterContext) { }
    }
}