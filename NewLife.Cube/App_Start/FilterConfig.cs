using System.Web;
using System.Web.Mvc;
using NewLife.Cube.Filters;

namespace NewLife.Cube
{
    /// <summary>过滤器配置</summary>
    public class FilterConfig
    {
        /// <summary>注册全局过滤器</summary>
        /// <param name="filters"></param>
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new MvcHandleErrorAttribute());
            filters.Add(new EntityAuthorizeAttribute());
        }
    }
}