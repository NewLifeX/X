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
        if (host.EndsWith("test.localhost"))
        {
            try
            {
                return context.RouteTo<TestModuleRoute>();
            }
            catch
            {
                ModuleRule m = new ModuleRule();
                m.Module = new TestModuleRoute();
                context.RouteTo("", m); // 自行控制模块和模块路由配置实例初始化
            }
        }
        return null;
    }

    public void ReleaseController(IController handler)
    {
    }
}
