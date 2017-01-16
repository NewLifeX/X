using System;

namespace NewLife.Remoting
{
    /// <summary>控制器上下文</summary>
    public class ControllerContext
    {
        /// <summary>控制器实例</summary>
        public Object Controller { get; set; }

        /// <summary>处理动作</summary>
        public ApiAction Action { get; set; }

        /// <summary>会话</summary>
        public IApiSession Session { get; set; }

        /// <summary>实例化</summary>
        public ControllerContext() { }

        /// <summary>拷贝实例化</summary>
        /// <param name="context"></param>
        public ControllerContext(ControllerContext context)
        {
            Controller = context.Controller;
            Action = context.Action;
        }

        [ThreadStatic]
        private static ControllerContext _Current;
        /// <summary>当前线程上下文</summary>
        public static ControllerContext Current { get { return _Current; }set { _Current = value; } }
    }
}