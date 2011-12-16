using System.Web;
using NewLife.Reflection;

namespace NewLife.Mvc
{
    /// <summary>
    /// 静态资源控制器工厂
    /// </summary>
    public class StaticRoute : IControllerFactory
    {
        static StaticRoute _Instance;

        /// <summary>
        /// StaticRoute类的全局实例
        /// </summary>
        public static StaticRoute Instance
        {
            get
            {
                if (_Instance == null) // 对实例化一次不很需要 所以没加锁
                {
                    _Instance = new StaticRoute();
                }
                return _Instance;
            }
        }

        /// <summary>
        /// 返回StaticRoute类的全局实例
        /// </summary>
        /// <returns></returns>
        internal static IControllerFactory InstanceFunc()
        {
            return Instance;
        }

        internal Func<IRouteContext, bool> Filter { get; set; }

        /// <summary>
        /// 实现静态资源路由
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IController GetController(IRouteContext context)
        {
            if (Filter == null || Filter(context))
            {
                HttpCacheConfig.StaticCache(HttpContext.Current.Response.Cache);
                return IgnoreRoute.Controller;
            }
            return null;
        }
        /// <summary>
        /// 实现IControllerFactory接口
        /// </summary>
        /// <param name="handler"></param>
        public void ReleaseController(IController handler)
        {
        }
    }
}