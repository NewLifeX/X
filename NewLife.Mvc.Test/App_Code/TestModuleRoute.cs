using System;
using System.Collections.Generic;
using System.Web;
using NewLife.Mvc;

/// <summary>
///TestModuleRoute 的摘要说明
/// </summary>
public class TestModuleRoute : IRouteConfigModule
{
    public void Config(RouteConfigManager cfg)
    {
        cfg.Route<TestController>("/foo")
            .Route<TestController>("/foo$")
            .Route(
                "/f", typeof(TestFactory),
                "/foo2", "TestController",
                ""
            );
    }
}