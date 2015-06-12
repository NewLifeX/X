using System;
using System.Text;
using System.Web.Mvc;
using System.Xml.Serialization;
using NewLife.Reflection;
using NewLife.Xml;
using XCode.Membership;

namespace NewLife.Cube
{
    /// <summary>设置控制器</summary>
    public class ConfigController<TConfig> : ControllerBaseX where TConfig : XmlConfig<TConfig>, new()
    {
        /// <summary>动作执行前</summary>
        /// <param name="filterContext"></param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var name = this.GetType().GetDisplayName();
            if (name.IsNullOrEmpty()) name = typeof(TConfig).GetDisplayName();
            if (name.IsNullOrEmpty()) name = typeof(TConfig).Name;

            ViewBag.HeaderTitle = name;
            ViewBag.HeaderContent = null;

            base.OnActionExecuting(filterContext);
        }

        /// <summary>系统设置</summary>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Detail)]
        public ActionResult Index()
        {
            var config = XmlConfig<TConfig>.Current;
            return View("ObjectForm", config);
        }

        /// <summary>系统设置</summary>
        /// <param name="config"></param>
        /// <returns></returns>
        [HttpPost]
        [EntityAuthorize(PermissionFlags.Update)]
        public ActionResult Index(TConfig config)
        {
            // 构造修改日志
            var sb = new StringBuilder();
            var cfg = XmlConfig<TConfig>.Current;
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

            config.Save(cfg.ConfigFile);
            XmlConfig<TConfig>.Current = null;

            if (Request.IsAjaxRequest())
                return Json(new { result = "success", content = "保存成功" });
            else
                return View("ObjectForm", config);
        }
    }
}