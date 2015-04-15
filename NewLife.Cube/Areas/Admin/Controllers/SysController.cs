using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NewLife.Common;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>系统设置控制器</summary>
    [DisplayName("高级设置")]
    public class SysController : Controller
    {
        /// <summary>系统设置</summary>
        /// <param name="config"></param>
        /// <returns></returns>
        [DisplayName("系统设置")]
        public ActionResult Index(SysConfig config)
        {
            if (HttpContext.Request.HttpMethod == "POST")
            {
                config.Save(SysConfig.Current.ConfigFile);
            }
            config = SysConfig.Current;

            return View("SysConfig", config);
        }

        //public ActionResult Index(SysConfig config)
        //{
        //    config.Save();

        //    return View("SysConfig", config);
        //}
    }
}
