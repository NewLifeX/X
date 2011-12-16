using System;
using System.Web;

namespace NewLife.Mvc
{
    /// <summary>
    /// 路由特定Http请求时的缓存配置
    ///
    /// HttpCachePolicy类型参数一般来自HttpResponse.Cache
    /// </summary>
    public class HttpCacheConfig
    {
        // TODO 考虑默认缓存配置文件设置方式

        /// <summary>
        /// 重定向路由的缓存
        /// </summary>
        /// <param name="c"></param>
        /// <param name="isPermanently"></param>
        public static void RedirectCache(HttpCachePolicy c, bool isPermanently)
        {
            if (isPermanently)
            {
                if (!Route.Debug)
                {
                    StaticCache(c);
                }
                else
                {
                    c.AppendCacheExtension("NewLife.Mvc.RedirectCache=Permanently");
                    NoCache(c);
                }
            }
            else
            {
                NoCache(c);
            }
        }

        /// <summary>
        /// 指定不缓存
        /// </summary>
        /// <param name="c"></param>
        public static void NoCache(HttpCachePolicy c)
        {
            c.SetCacheability(HttpCacheability.NoCache);
            c.SetExpires(DateTime.MinValue);
            c.SetMaxAge(TimeSpan.FromMilliseconds(0));
        }

        /// <summary>
        /// 静态资源的缓存
        /// </summary>
        /// <param name="c"></param>
        public static void StaticCache(HttpCachePolicy c)
        {
            c.SetCacheability(HttpCacheability.Public);
            c.SetExpires(DateTime.Now + TimeSpan.FromDays(14));
            c.SetMaxAge(TimeSpan.FromDays(14));
        }
    }
}