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
    public override void Execute()
    {
        Response.Write("Hello " + world + " ");
        Response.Write(Request.Url);
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