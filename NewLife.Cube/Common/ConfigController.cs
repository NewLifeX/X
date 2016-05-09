using System.Web.Mvc;
using System.IO;
using System;
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

        /// <summary>已重载</summary>
        /// <param name="filterContext"></param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var fi = XmlConfig<TConfig>._.ConfigFile;
            if (fi.IsNullOrEmpty() || !fi.AsFile().Exists) throw new Exception("无法找到配置文件 {0}".F(fi));

            var bs = this.Bootstrap();
            bs.MaxColumn = 1;
            bs.LabelWidth = 3;

            base.OnActionExecuting(filterContext);
        }
    }
}