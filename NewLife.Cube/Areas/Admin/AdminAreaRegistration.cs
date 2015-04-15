using System;
using System.IO;
using System.Threading;
using System.Web.Mvc;
using System.Web.Optimization;
using NewLife.Compression;
using NewLife.Log;
using XCode.Membership;

namespace NewLife.Cube.Admin
{
    /// <summary>权限管理区域注册</summary>
    public class AdminAreaRegistration : AreaRegistration
    {
        /// <summary>区域名称</summary>
        public override string AreaName
        {
            get
            {
                return this.GetType().Name.TrimEnd("AreaRegistration");
            }
        }

        /// <summary>注册区域</summary>
        /// <param name="context"></param>
        public override void RegisterArea(AreaRegistrationContext context)
        {
            XTrace.WriteLine("开始注册权限管理区域" + AreaName);

            context.Routes.IgnoreRoute("bootstrap/{*relpath}");

            context.MapRoute(
                AreaName,
                AreaName + "/{controller}/{action}/{id}",
                new { controller = "Index", action = "Index", id = UrlParameter.Optional },
                  new[] { this.GetType().Namespace + ".Controllers" }
            );

            // 所有已存在文件的请求都交给Mvc处理，比如Admin目录
            //routes.RouteExistingFiles = true;

            // 注册视图引擎
            RazorViewEngineX.Register(ViewEngines.Engines);

            // 注册绑定提供者
            EntityModelBinderProvider.Register();

            // 注册过滤器
            var filters = GlobalFilters.Filters;
            filters.Add(new MvcHandleErrorAttribute());
            filters.Add(new EntityAuthorizeAttribute());

            // 自动解压Bootstrap
            var bs = "bootstrap".AsDirectory();
            if (!bs.Exists)
            {
                var bszip = "bootstrap.zip".GetFullPath();
                if (File.Exists(bszip))
                {
                    XTrace.WriteLine("自动解压释放Bootstrap");
                    ZipFile.Extract(bszip, ".".GetFullPath());
                }
            }

            // 绑定资源，绑定路径不能跟物理目录相同，否则因为上面的忽略路由而得不到处理
            var bundles = BundleTable.Bundles;
            bundles.Add(new StyleBundle("~/bootstrap_css").IncludeDirectory("~/bootstrap/css", "*.css", true));
            bundles.Add(new ScriptBundle("~/bootstrap_js").IncludeDirectory("~/bootstrap/js", "*.js", true));

            // 自动检查并添加菜单
            ThreadPool.QueueUserWorkItem(s =>
            {
                XTrace.WriteLine("初始化权限管理体系");
                var user = ManageProvider.User;

                // 延迟几秒钟等其它地方初始化完成
                Thread.Sleep(3000);
                XTrace.WriteLine("初始化菜单体系");
                ManageProvider.Menu.ScanController(AreaName, this.GetType().Assembly, this.GetType().Namespace + ".Controllers");

                var menu = ManageProvider.Menu.Root.FindByPath(AreaName);
                if (menu != null && menu.DisplayName.IsNullOrEmpty())
                {
                    menu.DisplayName = "管理平台";
                    menu.Save();
                }
            });
        }
    }
}