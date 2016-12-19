namespace NewLife.Remoting
{
    /// <summary>定义异常筛选器所需的方法。</summary>
	public interface IExceptionFilter
    {
        /// <summary>在发生异常时调用。</summary>
        /// <param name="filterContext">筛选器上下文。</param>
        void OnException(ExceptionContext filterContext);
    }
}