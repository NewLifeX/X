using System.ComponentModel;
using NewLife.Reflection;
using System.Text;
using System.Web.Mvc;
using NewLife.Common;
using XCode.Membership;
using System;
using System.Xml.Serialization;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>系统设置控制器</summary>
    [DisplayName("高级设置")]
    public class SysController : ControllerBaseX
    {
        /// <summary>系统设置</summary>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Detail)]
        public ActionResult Index()
        {
            ViewBag.HeaderTitle = "系统设置";
            ViewBag.HeaderContent = "设置系统全局参数";

            var config = SysConfig.Current;
            return View("SysConfig", config);
        }

        /// <summary>系统设置</summary>
        /// <param name="config"></param>
        /// <returns></returns>
        [HttpPost]
        [EntityAuthorize(PermissionFlags.Update)]
        public ActionResult Index(SysConfig config)
        {
            ViewBag.HeaderTitle = "系统设置";
            ViewBag.HeaderContent = "设置系统全局参数";

            // 构造修改日志
            var sb = new StringBuilder();
            var cfg = SysConfig.Current;
            foreach (var pi in config.GetType().GetProperties())
            {
                if (!pi.CanWrite) continue;
                if (pi.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;

                var v1 = config.GetValue(pi);
                var v2 = cfg.GetValue(pi);
                if (!Object.Equals(v1, v2))
                {
                    if (sb.Length > 0) sb.Append(",");

                    var name = pi.GetDisplayName();
                    if (name.IsNullOrEmpty()) name = pi.Name;
                    sb.AppendFormat("{0}:{1}=>{2}", name, v2, v1);
                }
            }
            LogProvider.Provider.WriteLog(config.GetType(), "修改", sb.ToString());

            config.Save(SysConfig.Current.ConfigFile);
            SysConfig.Current = null;

            if (Request.IsAjaxRequest())
                return Json(new { result = "success", content = "保存成功" });
            else
                return View("SysConfig", config);
        }
    }
}