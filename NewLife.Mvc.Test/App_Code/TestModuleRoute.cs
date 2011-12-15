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
        cfg
            .Redirect("/redirect1$", "/foo")
            .Redirect("/redirect2$", "~/static.bmp")
            .Redirect("/redirect3$", "/", true)
            .Redirect("/redirect4$", delegate(RouteContext ctx, string routePath)
            {
                return "http://www.google.com/";
            })
            .Route<TestController>("/foo")
            .Route<TestController>("/foo$")
            .Route(
                "/f", typeof(TestFactory),
                "/foo2", "TestController",
                ""
            );
    }
}