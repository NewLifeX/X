using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using NewLife.Xml;

namespace NewLife.Cube
{
    /// <summary>设置控制器</summary>
    public class ConfigController<TConfig> : ObjectController<TConfig> where TConfig : XmlConfig<TConfig>, new()
    {
        /// <summary>要展现和修改的对象</summary>
        protected override TConfig Value
        {
            get
            {
                return XmlConfig<TConfig>.Current;
            }
            set
            {
                if (value != null)
                {
                    var cfg = XmlConfig<TConfig>.Current;
                    value.ConfigFile = cfg.ConfigFile;
                    value.Save();
                }
                XmlConfig<TConfig>.Current = value;
            }
        }

        /// <summary>重载。过滤掉标识为XmlIgnore的属性</summary>
        /// <param name="filterContext"></param>
        protected override void OnActionExecuting(System.Web.Mvc.ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            var pds = ViewBag.Properties as IList<PropertyDescriptor>;
            pds = pds.Where(e => !e.Attributes.Contains(new XmlIgnoreAttribute())).ToList();
        }
    }
}