using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Xml.Serialization;
using NewLife.Reflection;
using XCode.Membership;

namespace NewLife.Cube
{
    /// <summary>对象控制器</summary>
    public abstract class ObjectController<TObject> : ControllerBaseX
    {
        /// <summary>动作执行前</summary>
        /// <param name="filterContext"></param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            var name = this.GetType().GetDisplayName() ?? typeof(TObject).GetDisplayName() ?? typeof(TObject).Name;

            var des = this.GetType().GetDescription() ?? typeof(TObject).GetDescription();

            ViewBag.HeaderTitle = name;
            ViewBag.HeaderContent = des;

            var pds = TypeDescriptor.GetProperties(Value);
            ViewBag.Properties = pds.Cast<PropertyDescriptor>().ToList();
        }

        /// <summary>显示对象</summary>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Detail)]
        public ActionResult Index()
        {
            return View("ObjectForm", Value);
        }

        /// <summary>保存对象</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [HttpPost]
        [EntityAuthorize(PermissionFlags.Update)]
        public ActionResult Index(TObject obj)
        {
            WriteLog(obj);

            Value = obj;

            if (Request.IsAjaxRequest())
                return Json(new { result = "success", content = "保存成功" });
            else
                return View("ObjectForm", obj);
        }

        /// <summary>要展现和修改的对象</summary>
        protected abstract TObject Value { get; set; }

        /// <summary>写日志</summary>
        /// <param name="obj"></param>
        protected virtual void WriteLog(TObject obj)
        {
            // 构造修改日志
            var sb = new StringBuilder();
            var cfg = Value;
            foreach (var pi in obj.GetType().GetProperties())
            {
                if (!pi.CanWrite) continue;
                if (pi.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;

                var v1 = obj.GetValue(pi);
                var v2 = cfg.GetValue(pi);
                if (!Object.Equals(v1, v2) && (pi.PropertyType != typeof(String) || v1 + "" != v2 + ""))
                {
                    if (sb.Length > 0) sb.Append(", ");

                    var name = pi.GetDisplayName();
                    if (name.IsNullOrEmpty()) name = pi.Name;
                    sb.AppendFormat("{0}:{1}=>{2}", name, v2, v1);
                }
            }
            LogProvider.Provider.WriteLog(obj.GetType(), "修改", sb.ToString());
        }
    }
}