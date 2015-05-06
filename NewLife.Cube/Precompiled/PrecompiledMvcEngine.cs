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

namespace RazorGenerator.Mvc
{
    public class PrecompiledMvcEngine : VirtualPathProviderViewEngine, IVirtualPathFactory
    {
        private readonly IDictionary<string, Type> _mappings;
        private readonly string _baseVirtualPath;
        private readonly Lazy<DateTime> _assemblyLastWriteTime;
        private readonly IViewPageActivator _viewPageActivator;
        public bool PreemptPhysicalFiles { get; set; }

        public bool UsePhysicalViewsIfNewer { get; set; }

        public PrecompiledMvcEngine(Assembly assembly)
            : this(assembly, null)
        {
        }
        public PrecompiledMvcEngine(Assembly assembly, string baseVirtualPath)
            : this(assembly, baseVirtualPath, null)
        {
        }
        public PrecompiledMvcEngine(Assembly assembly, string baseVirtualPath, IViewPageActivator viewPageActivator)
		{
            //PrecompiledMvcEngine <>4__this = this;
			this._assemblyLastWriteTime = new Lazy<DateTime>(() => assembly.GetLastWriteTimeUtc(DateTime.MaxValue));
			this._baseVirtualPath = PrecompiledMvcEngine.NormalizeBaseVirtualPath(baseVirtualPath);
			base.AreaViewLocationFormats = new string[]
			{
				"~/Areas/{2}/Views/{1}/{0}.cshtml",
				"~/Areas/{2}/Views/Shared/{0}.cshtml"
			};
			base.AreaMasterLocationFormats = new string[]
			{
				"~/Areas/{2}/Views/{1}/{0}.cshtml",
				"~/Areas/{2}/Views/Shared/{0}.cshtml"
			};
			base.AreaPartialViewLocationFormats = new string[]
			{
				"~/Areas/{2}/Views/{1}/{0}.cshtml",
				"~/Areas/{2}/Views/Shared/{0}.cshtml"
			};
			base.ViewLocationFormats = new string[]
			{
				"~/Views/{1}/{0}.cshtml",
				"~/Views/Shared/{0}.cshtml"
			};
			base.MasterLocationFormats = new string[]
			{
				"~/Views/{1}/{0}.cshtml",
				"~/Views/Shared/{0}.cshtml"
			};
			base.PartialViewLocationFormats = new string[]
			{
				"~/Views/{1}/{0}.cshtml",
				"~/Views/Shared/{0}.cshtml"
			};
			base.FileExtensions = new string[]
			{
				"cshtml"
			};
			this._mappings = (
				from type in assembly.GetTypes()
				where typeof(WebPageRenderingBase).IsAssignableFrom(type)
				let pageVirtualPath = type.GetCustomAttributes(false).OfType<PageVirtualPathAttribute>().FirstOrDefault<PageVirtualPathAttribute>()
				where pageVirtualPath != null
				select new KeyValuePair<string, Type>(PrecompiledMvcEngine.CombineVirtualPaths(this._baseVirtualPath, pageVirtualPath.VirtualPath), type)).ToDictionary((KeyValuePair<string, Type> t) => t.Key, (KeyValuePair<string, Type> t) => t.Value, StringComparer.OrdinalIgnoreCase);
			base.ViewLocationCache = new PrecompiledViewLocationCache(assembly.FullName, base.ViewLocationCache);
			this._viewPageActivator = (viewPageActivator ?? (DependencyResolver.Current.GetService<IViewPageActivator>() ?? DefaultViewPageActivator.Current));
		}
        protected override bool FileExists(ControllerContext controllerContext, string virtualPath)
        {
            virtualPath = PrecompiledMvcEngine.EnsureVirtualPathPrefix(virtualPath);
            return (!this.UsePhysicalViewsIfNewer || !this.IsPhysicalFileNewer(virtualPath)) && this.Exists(virtualPath);
        }
        protected override IView CreatePartialView(ControllerContext controllerContext, string partialPath)
        {
            partialPath = PrecompiledMvcEngine.EnsureVirtualPathPrefix(partialPath);
            return this.CreateViewInternal(partialPath, null, false);
        }
        protected override IView CreateView(ControllerContext controllerContext, string viewPath, string masterPath)
        {
            viewPath = PrecompiledMvcEngine.EnsureVirtualPathPrefix(viewPath);
            return this.CreateViewInternal(viewPath, masterPath, true);
        }
        private IView CreateViewInternal(string viewPath, string masterPath, bool runViewStartPages)
        {
            Type type;
            IView result;
            if (this._mappings.TryGetValue(viewPath, out type))
            {
                result = new PrecompiledMvcView(viewPath, masterPath, type, runViewStartPages, base.FileExtensions, this._viewPageActivator);
            }
            else
            {
                result = null;
            }
            return result;
        }
        public object CreateInstance(string virtualPath)
        {
            virtualPath = PrecompiledMvcEngine.EnsureVirtualPathPrefix(virtualPath);
            object result;
            Type type;
            if (!this.PreemptPhysicalFiles && base.VirtualPathProvider.FileExists(virtualPath))
            {
                result = BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(WebPageRenderingBase));
            }
            else if (this.UsePhysicalViewsIfNewer && this.IsPhysicalFileNewer(virtualPath))
            {
                result = BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(WebViewPage));
            }
            else if (this._mappings.TryGetValue(virtualPath, out type))
            {
                result = this._viewPageActivator.Create(null, type);
            }
            else
            {
                result = null;
            }
            return result;
        }
        public bool Exists(string virtualPath)
        {
            virtualPath = PrecompiledMvcEngine.EnsureVirtualPathPrefix(virtualPath);
            return this._mappings.ContainsKey(virtualPath);
        }
        private bool IsPhysicalFileNewer(string virtualPath)
        {
            return PrecompiledMvcEngine.IsPhysicalFileNewer(virtualPath, this._baseVirtualPath, this._assemblyLastWriteTime);
        }
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
                virtualPath = PrecompiledMvcEngine.EnsureVirtualPathPrefix(virtualPath);
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
