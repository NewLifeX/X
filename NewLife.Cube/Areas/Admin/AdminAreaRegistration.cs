using System;
using System.Threading;
using System.Web.Mvc;
using System.Web.Optimization;
using NewLife.Model;
using XCode.Membership;

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

            // 注册绑定提供者
            EntityModelBinderProvider.Register();

            // 绑定资源，绑定路径不能跟物理目录相同，否则因为上面的忽略路由而得不到处理
            var bundles = BundleTable.Bundles;
            bundles.Add(new StyleBundle("~/bootstrap_css").IncludeDirectory("~/bootstrap/css", "*.css", true));
            bundles.Add(new ScriptBundle("~/bootstrap_js").IncludeDirectory("~/bootstrap/js", "*.js", true));

            // 自动检查并添加菜单
            ThreadPool.QueueUserWorkItem(s =>
            {
                // 延迟几秒钟等其它地方初始化完成
                Thread.Sleep(3000);
                ManageProvider.Menu.ScanController(AreaName, this.GetType().Assembly, this.GetType().Namespace + ".Controllers");

                var menu = ManageProvider.Menu.Root.FindByPath(AreaName);
                if (menu != null&&menu.DisplayName.IsNullOrEmpty())
                {
                    menu.DisplayName = "管理平台";
                    menu.Save();
                }
            });
        }
    }
}