using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>文件管理</summary>
    [DisplayName("文件管理")]
    [EntityAuthorize(PermissionFlags.Detail)]
    public class FileController : ControllerBaseX
    {
        /// <summary>文件管理主视图</summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            return View();
        }

    }
}