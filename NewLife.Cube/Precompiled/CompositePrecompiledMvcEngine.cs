using System;
using System.Collections.Generic;
using System.Web.Compilation;
using System.Web.Mvc;
using System.Web.WebPages;

namespace RazorGenerator.Mvc
{
    public class CompositePrecompiledMvcEngine : VirtualPathProviderViewEngine, IVirtualPathFactory
    {
        private struct ViewMapping
        {
            public Type Type { get; set; }

            public PrecompiledViewAssembly ViewAssembly { get; set; }
        }
        private readonly IDictionary<string, ViewMapping> _mappings = new Dictionary<string, ViewMapping>(StringComparer.OrdinalIgnoreCase);
        private readonly IViewPageActivator _viewPageActivator;
        public CompositePrecompiledMvcEngine(params PrecompiledViewAssembly[] viewAssemblies)
            : this(viewAssemblies, null)
        {
        }
        public CompositePrecompiledMvcEngine(IEnumerable<PrecompiledViewAssembly> viewAssemblies, IViewPageActivator viewPageActivator)
        {
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
            foreach (PrecompiledViewAssembly current in viewAssemblies)
            {
                foreach (KeyValuePair<string, Type> current2 in current.GetTypeMappings())
                {
                    this._mappings[current2.Key] = new ViewMapping
                    {
                        Type = current2.Value,
                        ViewAssembly = current
                    };
                }
            }
            this._viewPageActivator = (viewPageActivator ?? (DependencyResolver.Current.GetService<IViewPageActivator>() ?? DefaultViewPageActivator.Current));
        }
        protected override bool FileExists(ControllerContext controllerContext, string virtualPath)
        {
            ViewMapping viewMapping;
            return this._mappings.TryGetValue(virtualPath, out viewMapping) && (!viewMapping.ViewAssembly.UsePhysicalViewsIfNewer || !viewMapping.ViewAssembly.IsPhysicalFileNewer(virtualPath)) && this.Exists(virtualPath);
        }
        protected override IView CreatePartialView(ControllerContext controllerContext, string partialPath)
        {
            return this.CreateViewInternal(partialPath, null, false);
        }
        protected override IView CreateView(ControllerContext controllerContext, string viewPath, string masterPath)
        {
            return this.CreateViewInternal(viewPath, masterPath, true);
        }
        private IView CreateViewInternal(string viewPath, string masterPath, bool runViewStartPages)
        {
            ViewMapping viewMapping;
            IView result;
            if (this._mappings.TryGetValue(viewPath, out viewMapping))
            {
                result = new PrecompiledMvcView(viewPath, masterPath, viewMapping.Type, runViewStartPages, base.FileExtensions, this._viewPageActivator);
            }
            else
            {
                result = null;
            }
            return result;
        }
        public object CreateInstance(string virtualPath)
        {
            ViewMapping viewMapping;
            object result;
            if (!this._mappings.TryGetValue(virtualPath, out viewMapping))
            {
                result = null;
            }
            else if (!viewMapping.ViewAssembly.PreemptPhysicalFiles && base.VirtualPathProvider.FileExists(virtualPath))
            {
                result = BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(WebPageRenderingBase));
            }
            else if (viewMapping.ViewAssembly.UsePhysicalViewsIfNewer && viewMapping.ViewAssembly.IsPhysicalFileNewer(virtualPath))
            {
                result = BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(WebViewPage));
            }
            else
            {
                result = this._viewPageActivator.Create(null, viewMapping.Type);
            }
            return result;
        }
        public bool Exists(string virtualPath)
        {
            return this._mappings.ContainsKey(virtualPath);
        }
    }
}
