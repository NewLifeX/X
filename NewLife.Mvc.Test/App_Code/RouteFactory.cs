using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Mvc;
using System.Web;

class RouteFactory : IControllerFactory
{
    public IController GetController(IRouteContext context)
    {
        if (HttpContext.Current.Request.Url.Host.EndsWith(".localhost.com"))
        {
            try
            {
                return context.RouteTo<TestModuleRoute>();
            }
            catch
            {
                context.RouteTo(new TestModuleRoute());
                context.RouteTo(new RouteConfigManager());
            }
        }
        return null;
    }

    public void ReleaseController(IController handler)
    {
    }
}
