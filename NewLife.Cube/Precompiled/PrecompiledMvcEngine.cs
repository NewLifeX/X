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

namespace NewLife.Cube.Precompiled
{
    /// <summary>预编译Mvc引擎</summary>
    public class PrecompiledMvcEngine : VirtualPathProviderViewEngine, IVirtualPathFactory
    {
        private readonly IDictionary<string, Type> _mappings;
        private readonly string _baseVirtualPath;
        private readonly Lazy<DateTime> _assemblyLastWriteTime;
        private readonly IViewPageActivator _viewPageActivator;

        /// <summary>优先使用物理文件</summary>
        public bool PreemptPhysicalFiles { get; set; }

        /// <summary>使用更新的物理文件</summary>
        public bool UsePhysicalViewsIfNewer { get; set; }

        /// <summary>实例化预编译Mvc引擎</summary>
        /// <param name="assembly"></param>
        public PrecompiledMvcEngine(Assembly assembly) : this(assembly, null) { }

        /// <summary>实例化预编译Mvc引擎</summary>
        /// <param name="assembly"></param>
        /// <param name="baseVirtualPath"></param>
        public PrecompiledMvcEngine(Assembly assembly, string baseVirtualPath) : this(assembly, baseVirtualPath, null) { }

        /// <summary>实例化预编译Mvc引擎</summary>
        /// <param name="assembly"></param>
        /// <param name="baseVirtualPath"></param>
        /// <param name="viewPageActivator"></param>
        public PrecompiledMvcEngine(Assembly assembly, string baseVirtualPath, IViewPageActivator viewPageActivator)
        {
            //PrecompiledMvcEngine <>4__this = this;
            _assemblyLastWriteTime = new Lazy<DateTime>(() => assembly.GetLastWriteTimeUtc(DateTime.MaxValue));
            _baseVirtualPath = NormalizeBaseVirtualPath(baseVirtualPath);
            AreaViewLocationFormats = new string[]
			{
				"~/Areas/{2}/Views/{1}/{0}.cshtml",
				"~/Areas/{2}/Views/Shared/{0}.cshtml"
			};
            AreaMasterLocationFormats = new string[]
			{
				"~/Areas/{2}/Views/{1}/{0}.cshtml",
				"~/Areas/{2}/Views/Shared/{0}.cshtml"
			};
            AreaPartialViewLocationFormats = new string[]
			{
				"~/Areas/{2}/Views/{1}/{0}.cshtml",
				"~/Areas/{2}/Views/Shared/{0}.cshtml"
			};
            ViewLocationFormats = new string[]
			{
				"~/Views/{1}/{0}.cshtml",
				"~/Views/Shared/{0}.cshtml"
			};
            MasterLocationFormats = new string[]
			{
				"~/Views/{1}/{0}.cshtml",
				"~/Views/Shared/{0}.cshtml"
			};
            PartialViewLocationFormats = new string[]
			{
				"~/Views/{1}/{0}.cshtml",
				"~/Views/Shared/{0}.cshtml"
			};
            FileExtensions = new string[]
			{
				"cshtml"
			};
            _mappings = (
                from type in assembly.GetTypes()
                where typeof(WebPageRenderingBase).IsAssignableFrom(type)
                let pageVirtualPath = type.GetCustomAttributes(false).OfType<PageVirtualPathAttribute>().FirstOrDefault<PageVirtualPathAttribute>()
                where pageVirtualPath != null
                select new KeyValuePair<string, Type>(CombineVirtualPaths(_baseVirtualPath, pageVirtualPath.VirtualPath), type)).ToDictionary((KeyValuePair<string, Type> t) => t.Key, (KeyValuePair<string, Type> t) => t.Value, StringComparer.OrdinalIgnoreCase);
            ViewLocationCache = new PrecompiledViewLocationCache(assembly.FullName, ViewLocationCache);
            _viewPageActivator = (viewPageActivator ?? (DependencyResolver.Current.GetService<IViewPageActivator>() ?? DefaultViewPageActivator.Current));
        }

        /// <summary>文件是否存在</summary>
        /// <param name="controllerContext"></param>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        protected override bool FileExists(ControllerContext controllerContext, string virtualPath)
        {
            virtualPath = EnsureVirtualPathPrefix(virtualPath);
            return (!UsePhysicalViewsIfNewer || !IsPhysicalFileNewer(virtualPath)) && Exists(virtualPath);
        }

        /// <summary>创建分部视图</summary>
        /// <param name="controllerContext"></param>
        /// <param name="partialPath"></param>
        /// <returns></returns>
        protected override IView CreatePartialView(ControllerContext controllerContext, string partialPath)
        {
            partialPath = EnsureVirtualPathPrefix(partialPath);
            return CreateViewInternal(partialPath, null, false);
        }

        /// <summary>创建视图</summary>
        /// <param name="controllerContext"></param>
        /// <param name="viewPath"></param>
        /// <param name="masterPath"></param>
        /// <returns></returns>
        protected override IView CreateView(ControllerContext controllerContext, string viewPath, string masterPath)
        {
            viewPath = EnsureVirtualPathPrefix(viewPath);
            return CreateViewInternal(viewPath, masterPath, true);
        }

        private IView CreateViewInternal(string viewPath, string masterPath, bool runViewStartPages)
        {
            Type type;
            if (_mappings.TryGetValue(viewPath, out type))
                return new PrecompiledMvcView(viewPath, masterPath, type, runViewStartPages, FileExtensions, _viewPageActivator);
            else
                return null;
        }

        /// <summary>创建实例</summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public object CreateInstance(string virtualPath)
        {
            virtualPath = EnsureVirtualPathPrefix(virtualPath);
            object result;
            Type type;
            if (!PreemptPhysicalFiles && VirtualPathProvider.FileExists(virtualPath))
            {
                result = BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(WebPageRenderingBase));
            }
            else if (UsePhysicalViewsIfNewer && IsPhysicalFileNewer(virtualPath))
            {
                result = BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(WebViewPage));
            }
            else if (_mappings.TryGetValue(virtualPath, out type))
            {
                result = _viewPageActivator.Create(null, type);
            }
            else
            {
                result = null;
            }
            return result;
        }

        /// <summary>是否存在</summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public bool Exists(string virtualPath)
        {
            virtualPath = EnsureVirtualPathPrefix(virtualPath);
            return _mappings.ContainsKey(virtualPath);
        }

        private bool IsPhysicalFileNewer(string virtualPath)
        {
            return IsPhysicalFileNewer(virtualPath, _baseVirtualPath, _assemblyLastWriteTime);
        }

        /// <summary>是否物理文件更新</summary>
        /// <param name="virtualPath"></param>
        /// <param name="baseVirtualPath"></param>
        /// <param name="assemblyLastWriteTime"></param>
        /// <returns></returns>
        internal static bool IsPhysicalFileNewer(string virtualPath, string baseVirtualPath, Lazy<DateTime> assemblyLastWriteTime)
        {
            bool result;
            if (virtualPath.StartsWith(baseVirtualPath ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(baseVirtualPath))
                {
                    virtualPath = "~/" + virtualPath.Substring(baseVirtualPath.Length);
                }
                string path = HostingEnvironment.MapPath(virtualPath);
                result = (File.Exists(path) && File.GetLastWriteTimeUtc(path) > assemblyLastWriteTime.Value);
            }
            else
            {
                result = false;
            }
            return result;
        }

        private static string EnsureVirtualPathPrefix(string virtualPath)
        {
            if (!string.IsNullOrEmpty(virtualPath))
            {
                if (!virtualPath.StartsWith("~/", StringComparison.Ordinal))
                {
                    virtualPath = "~/" + virtualPath.TrimStart(new char[]
					{
						'/',
						'~'
					});
                }
            }
            return virtualPath;
        }

        internal static string NormalizeBaseVirtualPath(string virtualPath)
        {
            if (!string.IsNullOrEmpty(virtualPath))
            {
                virtualPath = EnsureVirtualPathPrefix(virtualPath);
                if (!virtualPath.EndsWith("/", StringComparison.Ordinal))
                {
                    virtualPath += "/";
                }
            }
            return virtualPath;
        }

        private static string CombineVirtualPaths(string baseVirtualPath, string virtualPath)
        {
            string result;
            if (!string.IsNullOrEmpty(baseVirtualPath))
            {
                result = VirtualPathUtility.Combine(baseVirtualPath, virtualPath.Substring(2));
            }
            else
            {
                result = virtualPath;
            }
            return result;
        }
    }
}