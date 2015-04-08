using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NewLife.Cube.Admin
{
    public class AdminAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return this.GetType().Name.TrimEnd("AreaRegistration");
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                AreaName,
                AreaName + "/{controller}/{action}/{id}",
                new { controller = "Index", action = "Index", id = UrlParameter.Optional },
                  new[] { this.GetType().Namespace + ".Controllers" }
            );

            // 注册视图引擎
            RazorViewEngineX.Register(ViewEngines.Engines);
        }
    }
}