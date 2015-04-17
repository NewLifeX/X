using System;
using System.ComponentModel;
using System.IO;
using System.Web.Mvc;
using System.Web.Optimization;
using NewLife.Compression;
using NewLife.Log;
using XCode.Membership;

namespace NewLife.Cube.Admin
{
    /// <summary>权限管理区域注册</summary>
    [DisplayName("管理平台")]
    public class AdminAreaRegistration : AreaRegistrationBase
    {
        /// <summary>注册区域</summary>
        /// <param name="context"></param>
        public override void RegisterArea(AreaRegistrationContext context)
        {
            //base.RegisterArea(context);

            context.Routes.IgnoreRoute("bootstrap/{*relpath}");

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
            XTrace.WriteLine("初始化权限管理体系");
            var user = ManageProvider.User;
        }

        ///// <summary>自动扫描控制器，并添加到菜单</summary>
        ///// <remarks>默认操作当前注册区域的下一级Controllers命名空间</remarks>
        //protected override void ScanController()
        //{
        //    base.ScanController();

        //    var menu = ManageProvider.Menu.Root.FindByPath(AreaName);
        //    if (menu != null && menu.DisplayName.IsNullOrEmpty())
        //    {
        //        menu.DisplayName = "管理平台";
        //        menu.Save();
        //    }
        //}
    }
}