using System;

namespace NewLife.Remoting
{
    /// <summary>提供异常上下文。</summary>
    public class ExceptionContext : ControllerContext
    {
        /// <summary>获取或设置异常对象。</summary>
        public virtual Exception Exception { get; set; }

        /// <summary>获取或设置一个值，该值指示是否已处理异常。</summary>
        public Boolean ExceptionHandled { get; set; }

        /// <summary>获取或设置操作结果。</summary>
        public Object Result { get; set; }

        /// <summary>实例化</summary>
        public ExceptionContext() { }

        /// <summary>拷贝实例化</summary>
        /// <param name="context"></param>
        public ExceptionContext(ControllerContext context) : base(context) { }
    }
}