using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using NewLife.Common;
using NewLife.Reflection;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>首页</summary>
    [DisplayName("首页")]
    public class IndexController : ControllerBaseX
    {
        /// <summary>首页</summary>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Detail)]
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
        [EntityAuthorize(PermissionFlags.Detail, ResourceName = "Main")]
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

            ViewBag.MyAsms = AssemblyX.GetMyAssemblies().OrderBy(e => e.Name).OrderByDescending(e => e.Compile).ToArray();

            var Asms = AssemblyX.GetAssemblies(null).ToArray();
            Asms = Asms.OrderBy(e => e.Name).OrderByDescending(e => e.Compile).ToArray();
            ViewBag.Asms = Asms;

            return View();
        }

        /// <summary>菜单不可见</summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        protected override IDictionary<MethodInfo, int> ScanActionMenu(IMenu menu)
        {
            if (menu.Visible)
            {
                menu.Visible = false;
                menu.Save();
            }

            return base.ScanActionMenu(menu);
        }
    }
}