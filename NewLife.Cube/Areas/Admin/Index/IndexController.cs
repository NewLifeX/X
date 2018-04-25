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
using XCode;
using System.Web.Hosting;
using System.IO;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>首页</summary>
    [DisplayName("首页")]
    public class IndexController : ControllerBaseX
    {
        /// <summary>菜单顺序。扫描是会反射读取</summary>
        protected static Int32 MenuOrder { get; set; }

        static IndexController()
        {
            MenuOrder = 10;
        }

        /// <summary>首页</summary>
        /// <returns></returns>
        //[EntityAuthorize(PermissionFlags.Detail)]
        [AllowAnonymous]
        public ActionResult Index()
        {
            var user = ManageProvider.Provider.TryLogin();
            if (user == null) return RedirectToAction("Login", "User", new { r = Request.Url.PathAndQuery });

            ViewBag.User = ManageProvider.User;
            ViewBag.Config = SysConfig.Current;

            // 工作台页面
            var startPage = Request["page"];
            if (startPage.IsNullOrEmpty()) startPage = Setting.Current.StartPage;

            ViewBag.Main = startPage;

            return View();
        }

        /// <summary>服务器信息</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [DisplayName("服务器信息")]
        [EntityAuthorize(PermissionFlags.Detail)]
        public ActionResult Main(String id)
        {
            //if (id == "Restart")
            //{
            //    HttpRuntime.UnloadAppDomain();
            //    id = null;
            //}

            ViewBag.Act = id;
            //ViewBag.User = ManageProvider.User;
            ViewBag.Config = SysConfig.Current;

            var name = Request.ServerVariables["Server_SoftWare"];
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

            //return View();
            switch ((id + "").ToLower())
            {
                case "processmodules": return View("ProcessModules");
                case "assembly": return View("Assembly");
                case "session": return View("Session");
                case "cache": return View("Cache");
                case "servervar": return View("ServerVar");
                default: return View();
            }
        }

        /// <summary>重启</summary>
        /// <returns></returns>
        [DisplayName("重启")]
        [EntityAuthorize((PermissionFlags)16)]
        public ActionResult Restart()
        {
            //System.Web.HttpContext.Current.User = null;
            //try
            //{
            //    Process.GetCurrentProcess().Kill();
            //}
            //catch { }
            //try
            {
                //AppDomain.Unload(AppDomain.CurrentDomain);
                //HttpContext.User = null;
                //HttpRuntime.UnloadAppDomain();
                //HostingEnvironment.InitiateShutdown();
                //ApplicationManager.GetApplicationManager().ShutdownAll();
                // 通过修改web.config时间来重启站点，稳定可靠
                var wc = "web.config".GetFullPath();
                System.IO.File.SetLastWriteTime(wc, DateTime.Now);
            }
            //catch { }

            return RedirectToAction(nameof(Main));
        }

        /// <summary>菜单不可见</summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        protected override IDictionary<MethodInfo, Int32> ScanActionMenu(IMenu menu)
        {
            if (menu.Visible)
            {
                menu.Visible = false;
                (menu as IEntity).Save();
            }

            return base.ScanActionMenu(menu);
        }
    }
}