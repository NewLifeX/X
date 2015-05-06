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
    public class PrecompiledMvcEngine : BuildManagerViewEngine, IVirtualPathFactory
    {
        private readonly IDictionary<String, Type> _mappings;
        private readonly String _baseVirtualPath;
        private readonly Lazy<DateTime> _assemblyLastWriteTime;
        //private readonly IViewPageActivator _viewPageActivator;

        /// <summary>优先使用物理文件</summary>
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
            //PrecompiledMvcEngine <>4__this = this;
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
            //_mappings = (
            //    from type in assembly.GetTypes()
            //    where typeof(WebPageRenderingBase).IsAssignableFrom(type)
            //    let pageVirtualPath = type.GetCustomAttributes(false).OfType<PageVirtualPathAttribute>().FirstOrDefault()
            //    where pageVirtualPath != null
            //    select new KeyValuePair<String, Type>(CombineVirtualPaths(_baseVirtualPath, pageVirtualPath.VirtualPath), type)).ToDictionary(t => t.Key, t => t.Value, StringComparer.OrdinalIgnoreCase);
            _mappings = GetTypeMappings(assembly, _baseVirtualPath);
            ViewLocationCache = new PrecompiledViewLocationCache(assembly.FullName, ViewLocationCache);
            //_viewPageActivator = (viewPageActivator ?? (DependencyResolver.Current.GetService<IViewPageActivator>() ?? DefaultViewPageActivator.Current));
        }

        /// <summary>文件是否存在</summary>
        /// <param name="controllerContext"></param>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        protected override Boolean FileExists(ControllerContext controllerContext, String virtualPath)
        {
            virtualPath = EnsureVirtualPathPrefix(virtualPath);
            return (!UsePhysicalViewsIfNewer || !IsPhysicalFileNewer(virtualPath)) && Exists(virtualPath);
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
            if (_mappings.TryGetValue(viewPath, out type))
                return new PrecompiledMvcView(viewPath, masterPath, type, runViewStartPages, FileExtensions, ViewPageActivator);
            else
                return null;
        }

        /// <summary>创建实例</summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public Object CreateInstance(String virtualPath)
        {
            virtualPath = EnsureVirtualPathPrefix(virtualPath);
            Object result;
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
                result = ViewPageActivator.Create(null, type);
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
            Boolean result;
            if (virtualPath.StartsWith(baseVirtualPath ?? String.Empty, StringComparison.OrdinalIgnoreCase))
            {
                if (!String.IsNullOrEmpty(baseVirtualPath))
                {
                    virtualPath = "~/" + virtualPath.Substring(baseVirtualPath.Length);
                }
                String path = HostingEnvironment.MapPath(virtualPath);
                result = (File.Exists(path) && File.GetLastWriteTimeUtc(path) > assemblyLastWriteTime.Value);
            }
            else
            {
                result = false;
            }
            return result;
        }

        private static String EnsureVirtualPathPrefix(String virtualPath)
        {
            if (!String.IsNullOrEmpty(virtualPath))
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

        internal static String NormalizeBaseVirtualPath(String virtualPath)
        {
            if (!String.IsNullOrEmpty(virtualPath))
            {
                virtualPath = EnsureVirtualPathPrefix(virtualPath);
                if (!virtualPath.EndsWith("/", StringComparison.Ordinal))
                {
                    virtualPath += "/";
                }
            }
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
                where typeof(WebPageRenderingBase).IsAssignableFrom(type)
                let pageVirtualPath = type.GetCustomAttributes(false).OfType<PageVirtualPathAttribute>().FirstOrDefault()
                where pageVirtualPath != null
                select new KeyValuePair<String, Type>(CombineVirtualPaths(baseVirtualPath, pageVirtualPath.VirtualPath), type)).ToDictionary(t => t.Key, t => t.Value, StringComparer.OrdinalIgnoreCase);
        }
    }
}