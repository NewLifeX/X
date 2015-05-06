using System;
using System.Collections.Generic;
using System.Web.Compilation;
using System.Web.Mvc;
using System.Web.WebPages;

namespace NewLife.Cube.Precompiled
{
    /// <summary>复合预编译Mvc引擎</summary>
    public class CompositePrecompiledMvcEngine : VirtualPathProviderViewEngine, IVirtualPathFactory
    {
        private struct ViewMapping
        {
            public Type Type { get; set; }

            public PrecompiledViewAssembly ViewAssembly { get; set; }
        }
        private readonly IDictionary<string, ViewMapping> _mappings = new Dictionary<string, ViewMapping>(StringComparer.OrdinalIgnoreCase);
        private readonly IViewPageActivator _viewPageActivator;

        /// <summary>复合预编译Mvc引擎</summary>
        /// <param name="viewAssemblies"></param>
        public CompositePrecompiledMvcEngine(params PrecompiledViewAssembly[] viewAssemblies)
            : this(viewAssemblies, null)
        {
        }

        /// <summary>复合预编译Mvc引擎</summary>
        /// <param name="viewAssemblies"></param>
        /// <param name="viewPageActivator"></param>
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
                    _mappings[current2.Key] = new ViewMapping
                    {
                        Type = current2.Value,
                        ViewAssembly = current
                    };
                }
            }
            _viewPageActivator = (viewPageActivator ?? (DependencyResolver.Current.GetService<IViewPageActivator>() ?? DefaultViewPageActivator.Current));
        }

        /// <summary>文件是否存在</summary>
        /// <param name="controllerContext"></param>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        protected override bool FileExists(ControllerContext controllerContext, string virtualPath)
        {
            ViewMapping viewMapping;
            return _mappings.TryGetValue(virtualPath, out viewMapping) && (!viewMapping.ViewAssembly.UsePhysicalViewsIfNewer || !viewMapping.ViewAssembly.IsPhysicalFileNewer(virtualPath)) && Exists(virtualPath);
        }

        /// <summary>创建分部视图</summary>
        /// <param name="controllerContext"></param>
        /// <param name="partialPath"></param>
        /// <returns></returns>
        protected override IView CreatePartialView(ControllerContext controllerContext, string partialPath)
        {
            return CreateViewInternal(partialPath, null, false);
        }

        /// <summary>创建视图</summary>
        /// <param name="controllerContext"></param>
        /// <param name="viewPath"></param>
        /// <param name="masterPath"></param>
        /// <returns></returns>
        protected override IView CreateView(ControllerContext controllerContext, string viewPath, string masterPath)
        {
            return CreateViewInternal(viewPath, masterPath, true);
        }

        private IView CreateViewInternal(string viewPath, string masterPath, bool runViewStartPages)
        {
            ViewMapping viewMapping;
            if (_mappings.TryGetValue(viewPath, out viewMapping))
            return new PrecompiledMvcView(viewPath, masterPath, viewMapping.Type, runViewStartPages, base.FileExtensions, _viewPageActivator);
            else
            return null;
        }

        /// <summary>创建实例</summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public object CreateInstance(string virtualPath)
        {
            ViewMapping viewMapping;
            object result;
            if (!_mappings.TryGetValue(virtualPath, out viewMapping))
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
                result = _viewPageActivator.Create(null, viewMapping.Type);
            }
            return result;
        }

        /// <summary>是否存在</summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public bool Exists(string virtualPath)
        {
            return _mappings.ContainsKey(virtualPath);
        }
    }
}
