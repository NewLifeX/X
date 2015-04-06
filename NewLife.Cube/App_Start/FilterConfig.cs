using System.Web;
using System.Web.Mvc;
using NewLife.Cube.Filters;

namespace NewLife.Cube
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new MvcHandleErrorAttribute());
            filters.Add(new EntityAuthorizeAttribute());
        }
    }
}