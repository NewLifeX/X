using System;

namespace NewLife.Remoting
{
    /// <summary>提供 ActionExecuted 方法的上下文。</summary>
    public class ActionExecutedContext : ControllerContext
    {
        /// <summary>获取或设置一个值，该值指示此 <see cref="ActionExecutedContext"/> 对象已被取消。</summary>
        public virtual Boolean Canceled { get; set; }

        /// <summary>获取或设置在操作方法的执行过程中发生的异常（如果有）。</summary>
        public virtual Exception Exception { get; set; }

        /// <summary>获取或设置一个值，该值指示是否处理异常。</summary>
        public Boolean ExceptionHandled { get; set; }

        /// <summary>获取或设置由操作方法返回的结果。</summary>
        public Object Result { get; set; }

        /// <summary>实例化</summary>
        public ActionExecutedContext() { }

        /// <summary>拷贝实例化</summary>
        /// <param name="context"></param>
        public ActionExecutedContext(ControllerContext context) : base(context)
        {
            // 可能发生了异常
            if (context is ExceptionContext etx)
            {
                Exception = etx.Exception;
                ExceptionHandled = etx.ExceptionHandled;
                Result = etx.Result;
            }
        }
    }
}