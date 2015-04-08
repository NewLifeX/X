using System;
using System.Web.Mvc;
using System.Web.Optimization;

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
            context.Routes.IgnoreRoute("bootstrap/{*relpath}");
            
            context.MapRoute(
                AreaName,
                AreaName + "/{controller}/{action}/{id}",
                new { controller = "Index", action = "Index", id = UrlParameter.Optional },
                  new[] { this.GetType().Namespace + ".Controllers" }
            );

            // 注册视图引擎
            RazorViewEngineX.Register(ViewEngines.Engines);

            // 绑定资源
            var bundles = BundleTable.Bundles;
            bundles.Add(new StyleBundle("~/bootstrap/css").Include("~/bootstrap/css/*.css"));

            bundles.Add(new ScriptBundle("~/bootstrap/js").Include("~/bootstrap/js/*.js"));
        }
    }
}