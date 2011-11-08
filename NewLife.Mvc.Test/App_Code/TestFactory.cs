using System;
using System.Collections.Generic;
using System.Web;
using NewLife.Mvc;

/// <summary>
///TestFactory 的摘要说明
/// </summary>
public class TestFactory : IControllerFactory
{
    public bool Support(string path)
    {
        return true;
    }

    public IController Create()
    {
        return new TestController();
    }
}