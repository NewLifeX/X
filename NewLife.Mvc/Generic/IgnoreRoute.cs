using System;

namespace NewLife.Mvc
{
    /// <summary>
    /// 忽略路由请求 后续的路由规则将不会尝试匹配 一般用于避免路由处理静态资源 在IIS设置为集成模式时
    ///
    /// 在自定义的工厂内部需要忽略请求可以使用静态的Controller属性
    /// </summary>
    public sealed class IgnoreRoute : IControllerFactory
    {
        static IController _Controller;

        /// <summary>
        /// 在自定义的工厂内部使用的,忽略请求
        /// </summary>
        public static IController Controller
        {
            get
            {
                if (_Controller == null) // 对实例化一次不很需要 所以没加锁
                {
                    _Controller = new IgnoreRouteController();
                }
                return _Controller;
            }
        }

        static IgnoreRoute _Instance;

        /// <summary>
        /// IgnoreRoute类的全局实例
        /// </summary>
        public static IgnoreRoute Instance
        {
            get
            {
                if (_Instance == null) // 对实例化一次不很需要 所以没加锁
                {
                    _Instance = new IgnoreRoute();
                }
                return _Instance;
            }
        }

        /// <summary>
        /// 返回IgnoreRoute类的全局实例
        /// </summary>
        /// <returns></returns>
        internal static IControllerFactory InstanceFunc()
        {
            return Instance;
        }

        /// <summary>
        /// 返回指定控制器是否表示忽略请求,如果参数ctl为null也将返回true;
        /// </summary>
        /// <param name="ctl"></param>
        /// <returns></returns>
        public static bool IsIgnore(IController ctl)
        {
            return ctl == null || ctl == Controller || ctl is IgnoreRouteController;
        }

        /// <summary>
        /// 实现 IControllerFactory 接口
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IController GetController(IRouteContext context)
        {
            return Controller;
        }

        /// <summary>
        /// 实现 IControllerFactory 接口
        /// </summary>
        /// <param name="handler"></param>
        public void ReleaseController(IController handler)
        {
        }

        /// <summary>
        /// 忽略路由请求 的控制器
        /// </summary>
        internal class IgnoreRouteController : IController
        {
            public void ProcessRequest(IRouteContext context)
            {
                throw new NotImplementedException("忽略的路由请求不应该被执行,请使用IgnoreRoute.IsIgnore判断是否是需要忽略的路由请求");
            }

            public bool IsReusable
            {
                get { return true; }
            }
        }
    }
}