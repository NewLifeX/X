namespace NewLife.Remoting
{
    /// <summary>表示一个特性，该特性用于处理由操作方法引发的异常。</summary>
    public class HandleErrorAttribute : FilterAttribute, IExceptionFilter
    {
        /// <summary>在发生异常时调用。</summary>
        /// <param name="filterContext">异常上下文</param>
        public virtual void OnException(ExceptionContext filterContext)
        {
            var ctx = filterContext;
            if (ctx.ExceptionHandled) return;

            //ctx.ExceptionHandled = true;

            if (ctx.Result == null) ctx.Result = ctx.Exception;
        }
    }
}