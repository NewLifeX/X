using System;
using System.Collections.Generic;
using System.Web;
using NewLife.Mvc;

/// <summary>
///Urls 的摘要说明
/// </summary>
public class Urls : IRouteConfig
{

    public void Config(RouteConfigManager cfg)
    {
        cfg.Route<TestController>("/Test")
            .Route(
                "/Module", typeof(TestModuleRoute),
                "/Test1$", typeof(TestController1),
                "/Test2", typeof(TestController2),
                ""
            );
    }
}