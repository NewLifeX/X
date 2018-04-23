using System;
using System.Collections.Generic;

namespace NewLife.Remoting
{
    /// <summary>提供 ActionExecuting 方法的上下文。</summary>
    public class ActionExecutingContext : ControllerContext
    {
        /// <summary>获取或设置操作方法参数。</summary>
        public virtual IDictionary<String, Object> ActionParameters { get; set; }

        /// <summary>获取或设置由操作方法返回的结果。</summary>
        public Object Result { get; set; }

        /// <summary>实例化</summary>
        public ActionExecutingContext() { }

        /// <summary>拷贝实例化</summary>
        /// <param name="context"></param>
        public ActionExecutingContext(ControllerContext context) : base(context) { }
    }
}