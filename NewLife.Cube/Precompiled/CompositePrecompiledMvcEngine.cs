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
        private readonly IDictionary<String, ViewMapping> _mappings = new Dictionary<String, ViewMapping>(StringComparer.OrdinalIgnoreCase);
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
            foreach (var asm in viewAssemblies)
            {
                foreach (var type in asm.GetTypeMappings())
                {
                    _mappings[type.Key] = new ViewMapping
                    {
                        Type = type.Value,
                        ViewAssembly = asm
                    };
                }
            }
            _viewPageActivator = (viewPageActivator ?? (DependencyResolver.Current.GetService<IViewPageActivator>() ?? DefaultViewPageActivator.Current));
        }

        /// <summary>文件是否存在</summary>
        /// <param name="controllerContext"></param>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        protected override Boolean FileExists(ControllerContext controllerContext, String virtualPath)
        {
            ViewMapping viewMapping;
            return _mappings.TryGetValue(virtualPath, out viewMapping) && (!viewMapping.ViewAssembly.UsePhysicalViewsIfNewer || !viewMapping.ViewAssembly.IsPhysicalFileNewer(virtualPath)) && Exists(virtualPath);
        }

        /// <summary>创建分部视图</summary>
        /// <param name="controllerContext"></param>
        /// <param name="partialPath"></param>
        /// <returns></returns>
        protected override IView CreatePartialView(ControllerContext controllerContext, String partialPath)
        {
            return CreateViewInternal(partialPath, null, false);
        }

        /// <summary>创建视图</summary>
        /// <param name="controllerContext"></param>
        /// <param name="viewPath"></param>
        /// <param name="masterPath"></param>
        /// <returns></returns>
        protected override IView CreateView(ControllerContext controllerContext, String viewPath, String masterPath)
        {
            return CreateViewInternal(viewPath, masterPath, true);
        }

        private IView CreateViewInternal(String viewPath, String masterPath, Boolean runViewStartPages)
        {
            ViewMapping viewMapping;
            if (_mappings.TryGetValue(viewPath, out viewMapping))
                return new PrecompiledMvcView(viewPath, masterPath, viewMapping.Type, runViewStartPages, FileExtensions, _viewPageActivator);
            else
                return null;
        }

        /// <summary>创建实例</summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public Object CreateInstance(String virtualPath)
        {
            ViewMapping viewMapping;
            Object result;
            if (!_mappings.TryGetValue(virtualPath, out viewMapping))
            {
                result = null;
            }
            else if (!viewMapping.ViewAssembly.PreemptPhysicalFiles && VirtualPathProvider.FileExists(virtualPath))
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
        public Boolean Exists(String virtualPath)
        {
            return _mappings.ContainsKey(virtualPath);
        }
    }
}