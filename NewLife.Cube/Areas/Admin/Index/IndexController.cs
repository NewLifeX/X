using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Web;
using System.Web.Mvc;
using NewLife.Common;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>首页</summary>
    [DisplayName("首页")]
    public class IndexController : ControllerBaseX
    {
        /// <summary>首页</summary>
        /// <returns></returns>
        [EntityAuthorize(null, PermissionFlags.Detail)]
        public ActionResult Index()
        {
            ViewBag.User = ManageProvider.User;
            ViewBag.Config = SysConfig.Current;
            ViewBag.Main = Url.Action("Main");

            return View();
        }

        /// <summary>服务器信息</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [DisplayName("服务器信息")]
        [EntityAuthorize("Main", PermissionFlags.Detail)]
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