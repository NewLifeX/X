using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;

namespace NewLife.Cube.Precompiled
{
    /// <summary>前端控制器</summary>
    public class FrontendController : Controller
    {
        /// <summary>默认</summary>
        /// <returns></returns>
        public ActionResult Default()
        {
            var actionName = (RouteData.Values["viewName"] ?? "Default").ToString();
            return View(actionName);
        }
    }
}