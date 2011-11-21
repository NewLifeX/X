using System;
using System.Collections.Generic;
using System.Web;
using NewLife.Mvc;

/// <summary>
///TestFactory 的摘要说明
/// </summary>
public class TestFactory : IControllerFactory
{
    public IController GetController(IRouteContext context)
    {
        return new TestController();
    }

    public void ReleaseController(IController handler)
    {
    }
}