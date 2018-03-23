using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Mvc;
using NewLife.Reflection;
using XCode.Membership;

namespace NewLife.Cube
{
    /// <summary>对象控制器</summary>
    public abstract class ObjectController<TObject> : ControllerBaseX
    {
        /// <summary>要展现和修改的对象</summary>
        protected abstract TObject Value { get; set; }

        /// <summary>菜单顺序。扫描是会反射读取</summary>
        protected static Int32 MenuOrder { get; set; }

        /// <summary>动作执行前</summary>
        /// <param name="filterContext"></param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            // 显示名和描述
            var name = GetType().GetDisplayName() ?? typeof(TObject).GetDisplayName() ?? typeof(TObject).Name;
            var des = GetType().GetDescription() ?? typeof(TObject).GetDescription();

            ViewBag.Title = name;
            ViewBag.HeaderTitle = name;

            var txt = "";
            if (txt.IsNullOrEmpty()) txt = (ViewBag.Menu as IMenu)?.Remark;
            if (txt.IsNullOrEmpty()) txt = des;
            ViewBag.HeaderContent = txt;

            if (Value != null) ViewBag.Properties = GetMembers(Value);
        }

        /// <summary>执行后</summary>
        /// <param name="filterContext"></param>
        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);

            var title = ViewBag.Title + "";
            HttpContext.Items["Title"] = title;
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
        //[HttpPost]
        //[DisplayName("修改")]
        [EntityAuthorize(PermissionFlags.Update)]
        public ActionResult Update(TObject obj)
        {
            WriteLog(obj);

            // 反射处理内部复杂成员
            var keys = Request.Form.AllKeys;
            foreach (var item in obj.GetType().GetProperties(true))
            {
                if (Type.GetTypeCode(item.PropertyType) == TypeCode.Object)
                {
                    var pv = obj.GetValue(item);
                    foreach (var pi in item.PropertyType.GetProperties(true))
                    {
                        if (keys.Contains(pi.Name))
                        {
                            var v = (Object)Request.Form[pi.Name];
                            if (pi.PropertyType == typeof(Boolean)) v = GetBool(pi.Name);

                            pv.SetValue(pi, v);
                        }
                    }
                }
            }

            Value = obj;

            if (Request.IsAjaxRequest())
                return Json(new { result = "success", content = "保存成功" });
            else
                return View("ObjectForm", obj);
        }

        Boolean GetBool(String name)
        {
            var v = Request[name];
            if (v.IsNullOrEmpty()) return false;

            v = v.Split(",")[0];

            if (!v.EqualIgnoreCase("true", "false")) throw new XException("非法布尔值Request[{0}]={1}", name, v);

            return v.ToBoolean();
        }

        /// <summary>写日志</summary>
        /// <param name="obj"></param>
        protected virtual void WriteLog(TObject obj)
        {
            // 构造修改日志
            var sb = new StringBuilder();
            var cfg = Value;
            foreach (var pi in obj.GetType().GetProperties(true))
            {
                if (!pi.CanWrite) continue;

                var v1 = obj.GetValue(pi);
                var v2 = cfg.GetValue(pi);
                if (!Equals(v1, v2) && (pi.PropertyType != typeof(String) || v1 + "" != v2 + ""))
                {
                    if (sb.Length > 0) sb.Append(", ");

                    var name = pi.GetDisplayName();
                    if (name.IsNullOrEmpty()) name = pi.Name;
                    sb.AppendFormat("{0}:{1}=>{2}", name, v2, v1);
                }
            }
            LogProvider.Provider.WriteLog(obj.GetType(), "修改", sb.ToString());
        }

        /// <summary>获取要显示编辑的成员</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected virtual PropertyInfo[] GetMembers(Object obj)
        {
            var type = Value as Type;
            if (type == null) type = obj.GetType();

            var pis = type.GetProperties(true);
            //pis = pis.Where(pi => pi.CanWrite && pi.GetIndexParameters().Length == 0 && pi.GetCustomAttribute<XmlIgnoreAttribute>() == null).ToArray();
            return pis.ToArray();
        }
    }
}