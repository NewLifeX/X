using NewLife.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using XCode.Membership;

namespace NewLife.Cube.AdminMobile.Controllers
{
    /// <summary>移动首页</summary>
    [DisplayName("移动首页")]
    public class IndexController : ControllerBaseX
    {
        /// <summary>首页</summary>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Detail)]
        //[AllowAnonymous]
        public ActionResult Index()
        {
            ViewBag.User = ManageProvider.User;
            ViewBag.Config = SysConfig.Current;

            // 工作台页面
            var startPage = Request["page"];
            if (startPage.IsNullOrEmpty()) startPage = Setting.Current.StartPage;

            ViewBag.Main = startPage;

            return View();
        }
    }
}