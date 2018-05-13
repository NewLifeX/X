using System;
using System.Collections.Generic;

namespace NewLife.Remoting
{
    /// <summary>控制器上下文</summary>
    public class ControllerContext
    {
        /// <summary>控制器实例</summary>
        public Object Controller { get; set; }

        /// <summary>处理动作</summary>
        public ApiAction Action { get; set; }

        /// <summary>真实动作名称</summary>
        public String ActionName { get; set; }

        /// <summary>会话</summary>
        public IApiSession Session { get; set; }

        /// <summary>请求</summary>
        public Object Request { get; set; }

        /// <summary>请求参数</summary>
        public IDictionary<String, Object> Parameters { get; set; }

        /// <summary>获取或设置操作方法参数。</summary>
        public virtual IDictionary<String, Object> ActionParameters { get; set; }

        /// <summary>获取或设置由操作方法返回的结果。</summary>
        public Object Result { get; set; }

        /// <summary>获取或设置在操作方法的执行过程中发生的异常（如果有）。</summary>
        public virtual Exception Exception { get; set; }

        /// <summary>获取或设置一个值，该值指示是否处理异常。</summary>
        public Boolean ExceptionHandled { get; set; }

        /// <summary>实例化</summary>
        public ControllerContext() { }

        /// <summary>拷贝实例化</summary>
        /// <param name="context"></param>
        public ControllerContext(ControllerContext context)
        {
            Controller = context.Controller;
            Action = context.Action;
            ActionName = context.ActionName;
            Session = context.Session;
            Request = context.Request;
            Parameters = context.Parameters;
        }

        [ThreadStatic]
        private static ControllerContext _Current;
        /// <summary>当前线程上下文</summary>
        public static ControllerContext Current { get { return _Current; } set { _Current = value; } }
    }
}