using System;
using System.Web.Mvc;
using NewLife.Common;
using NewLife.Cube.Controllers;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    public class IndexController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.User = ManageProvider.User;
            ViewBag.Config = SysConfig.Current;
            ViewBag.Main = Url.Action("Main");

            return View();
        }
    }
}