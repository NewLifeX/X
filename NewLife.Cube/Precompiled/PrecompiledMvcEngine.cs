using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Compilation;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.WebPages;
using NewLife.Reflection;

namespace NewLife.Cube.Precompiled
{
    /// <summary>预编译Mvc引擎</summary>
    public class PrecompiledMvcEngine : BuildManagerViewEngine, IVirtualPathFactory
    {
        private readonly IDictionary<String, Type> _mappings;
        private readonly String _baseVirtualPath;
        private readonly Lazy<DateTime> _assemblyLastWriteTime;
        //private readonly IViewPageActivator _viewPageActivator;

        /// <summary>取代物理文件，优先内嵌类</summary>
        public Boolean PreemptPhysicalFiles { get; set; }

        /// <summary>使用更新的物理文件</summary>
        public Boolean UsePhysicalViewsIfNewer { get; set; }

        /// <summary>实例化预编译Mvc引擎</summary>
        /// <param name="assembly"></param>
        public PrecompiledMvcEngine(Assembly assembly) : this(assembly, null) { }

        /// <summary>实例化预编译Mvc引擎</summary>
        /// <param name="assembly"></param>
        /// <param name="baseVirtualPath"></param>
        public PrecompiledMvcEngine(Assembly assembly, String baseVirtualPath) : this(assembly, baseVirtualPath, null) { }

        /// <summary>实例化预编译Mvc引擎</summary>
        /// <param name="assembly"></param>
        /// <param name="baseVirtualPath"></param>
        /// <param name="viewPageActivator"></param>
        public PrecompiledMvcEngine(Assembly assembly, String baseVirtualPath, IViewPageActivator viewPageActivator)
            : base(viewPageActivator)
        {
            // 为了实现物理文件“重载覆盖”的效果，强制使用物理文件
            PreemptPhysicalFiles = false;
            UsePhysicalViewsIfNewer = false;

            _assemblyLastWriteTime = new Lazy<DateTime>(() => assembly.GetLastWriteTimeUtc(DateTime.MaxValue));
            _baseVirtualPath = NormalizeBaseVirtualPath(baseVirtualPath);
            AreaViewLocationFormats = new String[]
            {
                "~/Areas/{2}/Views/{1}/{0}.cshtml",
                "~/Areas/{2}/Views/Shared/{0}.cshtml"
            };
            AreaMasterLocationFormats = new String[]
            {
                "~/Areas/{2}/Views/{1}/{0}.cshtml",
                "~/Areas/{2}/Views/Shared/{0}.cshtml"
            };
            AreaPartialViewLocationFormats = new String[]
            {
                "~/Areas/{2}/Views/{1}/{0}.cshtml",
                "~/Areas/{2}/Views/Shared/{0}.cshtml"
            };
            ViewLocationFormats = new String[]
            {
                "~/Views/{1}/{0}.cshtml",
                "~/Views/Shared/{0}.cshtml"
            };
            MasterLocationFormats = new String[]
            {
                "~/Views/{1}/{0}.cshtml",
                "~/Views/Shared/{0}.cshtml"
            };
            PartialViewLocationFormats = new String[]
            {
                "~/Views/{1}/{0}.cshtml",
                "~/Views/Shared/{0}.cshtml"
            };
            FileExtensions = new String[]
            {
                "cshtml"
            };
            _mappings = GetTypeMappings(assembly, _baseVirtualPath);
            ViewLocationCache = new PrecompiledViewLocationCache(assembly.FullName, ViewLocationCache);
            //_viewPageActivator = (viewPageActivator ?? (DependencyResolver.Current.GetService<IViewPageActivator>() ?? DefaultViewPageActivator.Current));
        }

        /// <summary>文件是否存在。如果存在，则由当前引擎创建视图</summary>
        /// <param name="controllerContext"></param>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        protected override Boolean FileExists(ControllerContext controllerContext, String virtualPath)
        {
            virtualPath = EnsureVirtualPathPrefix(virtualPath);

            // 如果映射表不存在，就不要掺合啦
            if (!Exists(virtualPath)) return false;

            // 两个条件任意一个满足即可使用物理文件
            // 如果不要求取代物理文件，并且虚拟文件存在，则使用物理文件创建
            if (!PreemptPhysicalFiles && VirtualPathProvider.FileExists(virtualPath)) return false;

            // 如果使用较新的物理文件，且物理文件的确较新，则使用物理文件创建
            if (UsePhysicalViewsIfNewer && IsPhysicalFileNewer(virtualPath)) return false;

            return true;
        }

        /// <summary>创建分部视图</summary>
        /// <param name="controllerContext"></param>
        /// <param name="partialPath"></param>
        /// <returns></returns>
        protected override IView CreatePartialView(ControllerContext controllerContext, String partialPath)
        {
            partialPath = EnsureVirtualPathPrefix(partialPath);
            return CreateViewInternal(partialPath, null, false);
        }

        /// <summary>创建视图</summary>
        /// <param name="controllerContext"></param>
        /// <param name="viewPath"></param>
        /// <param name="masterPath"></param>
        /// <returns></returns>
        protected override IView CreateView(ControllerContext controllerContext, String viewPath, String masterPath)
        {
            viewPath = EnsureVirtualPathPrefix(viewPath);
            return CreateViewInternal(viewPath, masterPath, true);
        }

        private IView CreateViewInternal(String viewPath, String masterPath, Boolean runViewStartPages)
        {
            Type type;
            if (!_mappings.TryGetValue(viewPath, out type)) return null;

            return new PrecompiledMvcView(viewPath, masterPath, type, runViewStartPages, FileExtensions, ViewPageActivator);
        }

        /// <summary>创建实例。Start和Layout会调用这里</summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public Object CreateInstance(String virtualPath)
        {
            virtualPath = EnsureVirtualPathPrefix(virtualPath);

            // 两个条件任意一个满足即可使用物理文件
            // 如果不要求取代物理文件，并且虚拟文件存在，则使用物理文件创建
            if (!PreemptPhysicalFiles && VirtualPathProvider.FileExists(virtualPath))
                return BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(WebPageRenderingBase));

            // 如果使用较新的物理文件，且物理文件的确较新，则使用物理文件创建
            if (UsePhysicalViewsIfNewer && IsPhysicalFileNewer(virtualPath))
                return BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(WebViewPage));

            // 最后使用内嵌类创建
            Type type;
            if (_mappings.TryGetValue(virtualPath, out type))
                return ViewPageActivator.Create(null, type);

            return null;
        }

        /// <summary>是否存在</summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public Boolean Exists(String virtualPath)
        {
            virtualPath = EnsureVirtualPathPrefix(virtualPath);
            return _mappings.ContainsKey(virtualPath);
        }

        private Boolean IsPhysicalFileNewer(String virtualPath)
        {
            return IsPhysicalFileNewer(virtualPath, _baseVirtualPath, _assemblyLastWriteTime);
        }

        /// <summary>是否物理文件更新</summary>
        /// <param name="virtualPath"></param>
        /// <param name="baseVirtualPath"></param>
        /// <param name="assemblyLastWriteTime"></param>
        /// <returns></returns>
        internal static Boolean IsPhysicalFileNewer(String virtualPath, String baseVirtualPath, Lazy<DateTime> assemblyLastWriteTime)
        {
            if (!virtualPath.StartsWithIgnoreCase(baseVirtualPath + "")) return false;

            if (!String.IsNullOrEmpty(baseVirtualPath)) virtualPath = "~/" + virtualPath.Substring(baseVirtualPath.Length);

            var path = HostingEnvironment.MapPath(virtualPath);
            return File.Exists(path) && File.GetLastWriteTimeUtc(path) > assemblyLastWriteTime.Value;
        }

        private static String EnsureVirtualPathPrefix(String virtualPath)
        {
            if (!String.IsNullOrEmpty(virtualPath))
            {
                if (!virtualPath.StartsWith("~/", StringComparison.Ordinal))
                    virtualPath = "~/" + virtualPath.TrimStart('/', '~');
            }
            return virtualPath;
        }

        internal static String NormalizeBaseVirtualPath(String virtualPath)
        {
            if (!String.IsNullOrEmpty(virtualPath))
                virtualPath = EnsureVirtualPathPrefix(virtualPath).EnsureEnd("/");

            return virtualPath;
        }

        private static String CombineVirtualPaths(String baseVirtualPath, String virtualPath)
        {
            if (!String.IsNullOrEmpty(baseVirtualPath))
                return VirtualPathUtility.Combine(baseVirtualPath, virtualPath.Substring(2));
            else
                return virtualPath;
        }

        /// <summary>遍历获取所有类型映射</summary>
        /// <param name="asm"></param>
        /// <param name="baseVirtualPath"></param>
        /// <returns></returns>
        public static IDictionary<String, Type> GetTypeMappings(Assembly asm, String baseVirtualPath)
        {
            return (
                from type in asm.GetTypes()
                where type.As<WebPageRenderingBase>()
                let pageVirtualPath = type.GetCustomAttributes(false).OfType<PageVirtualPathAttribute>().FirstOrDefault()
                where pageVirtualPath != null
                select new KeyValuePair<String, Type>(CombineVirtualPaths(baseVirtualPath, pageVirtualPath.VirtualPath), type)).ToDictionary(t => t.Key, t => t.Value, StringComparer.OrdinalIgnoreCase);
        }
    }
}