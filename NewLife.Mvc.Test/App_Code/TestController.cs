using System;
using System.Collections.Generic;
using System.Web;
using NewLife.Mvc;

/// <summary>
///TestController 的摘要说明
/// </summary>
public class TestController : GenericController, IController
{
    public TestController()
    {
        world = "world";
    }

    public override void Render(IDictionary<string, object> data)
    {
        Response.Write("Hello " + world + " ");
        Response.Write(Request.Url);
        Info();
    }

    public void Info()
    {
        RouteContext c = RouteContext.Current;
        Response.Write("<pre>" + Server.HtmlEncode(string.Format(@"
NewLife.Mvc.RouteContext.Current.RoutePath : {0}
NewLife.Mvc.RouteContext.Current.Module : {1}
NewLife.Mvc.RouteContext.Current.Factory : {2}
NewLife.Mvc.RouteContext.Current.Controller : {3}
NewLife.Mvc.RouteContext.Current.Path : {4}
", c.RoutePath, c.Module ?? null, c.Factory ?? null, c.Controller ?? null, c.Path)) + "</pre>");
    }

    private string _world;

    public string world
    {
        get { return _world; }
        set { _world = value; }
    }
}
public class TestController1 : TestController
{
    TestController1()
    {
        world = "第二个测试控制器";
    }
}

public class TestController2 : TestController
{
    TestController2()
    {
        world = "第三个控制器";
    }
}