using System;
using System.Diagnostics;
using System.Web;
using System.Web.Mvc;
using NewLife.Common;
using NewLife.Cube.Filters;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    [EntityAuthorize]
    public class IndexController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.User = ManageProvider.User;
            ViewBag.Config = SysConfig.Current;
            ViewBag.Main = Url.Action("Main");

            return View();
        }

        public ActionResult Main(String id)
        {
            if (id == "Restart")
            {
                HttpRuntime.UnloadAppDomain();
                id = null;
            }
            
            ViewBag.Act = id;
            ViewBag.User = ManageProvider.User;
            ViewBag.Config = SysConfig.Current;

            String name = Request.ServerVariables["Server_SoftWare"];
            if (String.IsNullOrEmpty(name)) name = Process.GetCurrentProcess().ProcessName;

            // 检测集成管道，低版本.Net不支持，请使用者根据情况自行注释
            try
            {
                if (HttpRuntime.UsingIntegratedPipeline) name += " [集成管道]";
            }
            catch { }

            ViewBag.WebServerName = name;

            return View();
        }
    }
}