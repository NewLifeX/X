using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.WebPages;
using NewLife.Cube.Precompiled;
using NewLife.IO;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Threading;
using NewLife.Web;
using XCode;
using XCode.Membership;
#if !NET4
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.Cube
{
    /// <summary>区域注册基类</summary>
    /// <remarks>
    /// 提供以下功能：
    /// 1，区域名称。从类名中截取。其中DisplayName特性作为菜单中文名。
    /// 2，静态构造注册一次视图引擎、绑定提供者、过滤器
    /// 3，注册区域默认路由
    /// </remarks>
    public abstract class AreaRegistrationBase : AreaRegistration
    {
        /// <summary>区域名称</summary>
        public override String AreaName { get; }

        /// <summary>预编译引擎集合。便于外部设置属性</summary>
        public static PrecompiledViewAssembly[] PrecompiledEngines { get; private set; }

        /// <summary>所有区域类型</summary>
        public static Type[] Areas { get; private set; }

        /// <summary>实例化区域注册</summary>
        public AreaRegistrationBase()
        {
            AreaName = GetType().Name.TrimEnd("AreaRegistration");
        }

        static AreaRegistrationBase()
        {
            XTrace.WriteLine("{0} Start 初始化魔方 {0}", new String('=', 32));
            Assembly.GetExecutingAssembly().WriteVersion();

            // 遍历所有引用了AreaRegistrationBase的程序集
            var list = new List<PrecompiledViewAssembly>();
            foreach (var asm in FindAllArea())
            {
                XTrace.WriteLine("注册区域视图程序集：{0}", asm.FullName);

                var pva = new PrecompiledViewAssembly(asm);
                list.Add(pva);
            }
            PrecompiledEngines = list.ToArray();

            var engine = new CompositePrecompiledMvcEngine(PrecompiledEngines);
            XTrace.WriteLine("注册复合预编译引擎，共有视图程序集{0}个", list.Count);
            //ViewEngines.Engines.Insert(0, engine);
            // 预编译引擎滞后，让其它引擎先工作
            ViewEngines.Engines.Add(engine);

            // StartPage lookups are done by WebPages. 
            VirtualPathFactoryManager.RegisterVirtualPathFactory(engine);

            // 注册绑定提供者
            EntityModelBinderProvider.Register();

            // 注册过滤器
            XTrace.WriteLine("注册过滤器：{0}", typeof(MvcHandleErrorAttribute).FullName);
            XTrace.WriteLine("注册过滤器：{0}", typeof(EntityAuthorizeAttribute).FullName);
            var filters = GlobalFilters.Filters;
            filters.Add(new MvcHandleErrorAttribute());
            filters.Add(new EntityAuthorizeAttribute() { IsGlobal = true });

            // 从数据库或者资源文件加载模版页面的例子
            //HostingEnvironment.RegisterVirtualPathProvider(new ViewPathProvider());

            //var routes = RouteTable.Routes;
            //routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            //routes.MapMvcAttributeRoutes();

            //routes.MapRoute(
            //    name: "Virtual",
            //    url: "{*viewName}",
            //    defaults: new { controller = "Frontend", action = "Default" },
            //    constraints: new { controller = "Frontend", action = "Default" }
            //);

            // 自动检查并下载魔方资源
            Task.Factory.StartNew(CheckContent, TaskCreationOptions.LongRunning).LogException();

            XTrace.WriteLine("{0} End   初始化魔方 {0}", new String('=', 32));
        }

        /// <summary>遍历所有引用了AreaRegistrationBase的程序集</summary>
        /// <returns></returns>
        static List<Assembly> FindAllArea()
        {
            var list = new List<Assembly>();
            Areas = typeof(AreaRegistrationBase).GetAllSubclasses(false).ToArray();
            foreach (var item in Areas)
            {
                var asm = item.Assembly;
                if (!list.Contains(asm))
                {
                    list.Add(asm);
                    //yield return asm;
                }
            }

            // 为了能够实现模板覆盖，程序集相互引用需要排序，父程序集在前
            list.Sort((x, y) =>
            {
                if (x == y) return 0;
                if (x != null && y == null) return 1;
                if (x == null && y != null) return -1;

                //return x.GetReferencedAssemblies().Any(e => e.FullName == y.FullName) ? 1 : -1;
                // 对程序集引用进行排序时，不能使用全名，当魔方更新而APP没有重新编译时，版本的不同将会导致全名不同，无法准确进行排序
                var yname = y.GetName().Name;
                return x.GetReferencedAssemblies().Any(e => e.Name == yname) ? 1 : -1;
            });

            return list;
        }

        static void CheckContent()
        {
            // 释放ico图标
            var ico = "favicon.ico";
            var ico2 = ico.GetFullPath();
            if (!File.Exists(ico2))
            {
                // 延迟时间释放，给子系统覆盖的机会
                TimerX.Delay(s =>
                {
                    if (!File.Exists(ico2)) Assembly.GetExecutingAssembly().ReleaseFile(ico, ico2);
                }, 15000);
            }

            // 检查魔方样式
            var js = "~/Content/Cube.js".GetFullPath();
            var css = "~/Content/Cube.css".GetFullPath();
            if (File.Exists(js) && File.Exists(css))
            {
                // 判断脚本时间
                var dt = DateTime.MinValue;
                var ss = File.ReadAllLines(js);
                for (var i = 0; i < 5; i++)
                {
                    if (DateTime.TryParse(ss[i].TrimStart("//").Trim(), out dt)) break;
                }
                // 要求脚本最小更新时间
                if (dt >= "2017-12-07 00:00:00".ToDateTime()) return;
            }

            var url = Setting.Current.PluginServer;
            if (url.IsNullOrEmpty()) return;

            var wc = new WebClientX(true, true)
            {
                Log = XTrace.Log
            };
            wc.DownloadLinkAndExtract(url, "Cube_Content", "~/Content".GetFullPath(), true);
        }

        /// <summary>注册区域</summary>
        /// <param name="context"></param>
        public override void RegisterArea(AreaRegistrationContext context)
        {
            var ns = GetType().Namespace + ".Controllers";
            XTrace.WriteLine("开始注册权限管理区域[{0}]，控制器命名空间[{1}]", AreaName, ns);

            // 注册本区域默认路由

            // Json输出，需要配置web.config
            //context.MapRoute(
            //    AreaName + "_Data",
            //    AreaName + "/{controller}.json/",
            //    new { controller = "Index", action = "Index", id = UrlParameter.Optional, output = "json" },
            //    new[] { ns }
            //);
            // Json输出，不需要配置web.config
            //context.MapRoute(
            //    AreaName + "_Json",
            //    AreaName + "/{controller}Json/{action}/{id}",
            //    new { controller = "Index", action = "Export", id = UrlParameter.Optional, output = "json" },
            //    new[] { ns }
            //);
            //context.MapRoute(
            //    AreaName + "_Detail",
            //    AreaName + "/{controller}/{id}",
            //    new { controller = "Index", action = "Detail" },
            //    new[] { ns }
            //);
            //context.MapRoute(
            //    AreaName + "_Detail_Json",
            //    AreaName + "/{controller}/{id}/Json",
            //    new { controller = "Index", action = "Detail", output = "json" },
            //    new { id = @"\d+" },
            //    new[] { ns }
            //);
            //context.MapRoute(
            //    AreaName + "_Json",
            //    AreaName + "/{controller}/Json",
            //    new { controller = "Index", action = "Index", output = "json" },
            //    new[] { ns }
            //);
            // 本区域默认配置
            context.MapRoute(
                AreaName,
                AreaName + "/{controller}/{action}/{id}",
                new { controller = "Index", action = "Index", id = UrlParameter.Optional },
                new[] { ns }
            );

            // 所有已存在文件的请求都交给Mvc处理，比如Admin目录
            //routes.RouteExistingFiles = true;

            // 自动检查并添加菜单
            TaskEx.Run(() =>
            {
                try
                {
                    ScanController();
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
            });
        }

        /// <summary>自动扫描控制器，并添加到菜单</summary>
        /// <remarks>默认操作当前注册区域的下一级Controllers命名空间</remarks>
        protected virtual void ScanController()
        {
#if DEBUG
            XTrace.WriteLine("{0}.ScanController", GetType().Name.TrimEnd("AreaRegistration"));
#endif
            var mf = ManageProvider.Menu;
            if (mf == null) return;

            using (var tran = (mf as IEntityOperate).CreateTrans())
            {
                XTrace.WriteLine("初始化[{0}]的菜单体系", AreaName);
                mf.ScanController(AreaName, GetType().Assembly, GetType().Namespace + ".Controllers");

                // 更新区域名称为友好中文名
                var menu = mf.Root.FindByPath(AreaName);
                if (menu != null && menu.DisplayName.IsNullOrEmpty())
                {
                    var dis = GetType().GetDisplayName();
                    var des = GetType().GetDescription();

                    if (!dis.IsNullOrEmpty()) menu.DisplayName = dis;
                    if (!des.IsNullOrEmpty()) menu.Remark = des;

                    (menu as IEntity).Save();
                }

                tran.Commit();
            }
        }

        private static ICollection<String> _areas;
        /// <summary>判断控制器是否归属于魔方管辖</summary>
        /// <param name="controller"></param>
        /// <returns></returns>
        public static Boolean Contains(IController controller)
        {
            // 判断控制器是否在管辖范围之内，不拦截其它控制器的异常信息
            var ns = controller.GetType().Namespace;
            if (!ns.EndsWith(".Controllers")) return false;

            if (_areas == null) _areas = new HashSet<String>(Areas.Select(e => e.Namespace));

            // 该控制器父级命名空间必须有对应的区域注册类，才会拦截其异常
            ns = ns.TrimEnd(".Controllers");
            //return Areas.Any(e => e.Namespace == ns);
            return _areas.Contains(ns);
        }
    }
}