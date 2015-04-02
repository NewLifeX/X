using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

/// <summary>测试用控制器</summary>
public class TestController : Controller
{
    public ActionResult Index(String id, String catchall)
    {
        return View();
    }
}