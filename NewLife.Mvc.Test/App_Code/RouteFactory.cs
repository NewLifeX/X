using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Mvc;
using System.Web;

class RouteFactory : IControllerFactory
{
    public IController GetController(IRouteContext context)
    {
        HttpRequest r = HttpContext.Current.Request;
        string host = r.Headers["Host"];
        string[] hostport = host.Split(':');
        int port = 80;
        if (hostport.Length > 1)
        {
            Int32.TryParse(hostport[1], out port);
        }
        host = hostport[0];
        if (host.EndsWith(".localhost"))
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
