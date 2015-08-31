using System;
using System.Collections.Generic;
using System.Web.Compilation;
using System.Web.Mvc;
using System.Web.WebPages;

namespace NewLife.Cube.Precompiled
{
    /// <summary>复合预编译Mvc引擎</summary>
    public class CompositePrecompiledMvcEngine : BuildManagerViewEngine, IVirtualPathFactory
    {
        private struct ViewMapping
        {
            public Type Type { get; set; }

            public PrecompiledViewAssembly ViewAssembly { get; set; }
        }
        private readonly IDictionary<String, ViewMapping> _mappings = new Dictionary<String, ViewMapping>(StringComparer.OrdinalIgnoreCase);
        //private readonly IViewPageActivator _viewPageActivator;

        /// <summary>复合预编译Mvc引擎</summary>
        /// <param name="viewAssemblies"></param>
        public CompositePrecompiledMvcEngine(params PrecompiledViewAssembly[] viewAssemblies) : this(viewAssemblies, null) { }

        /// <summary>复合预编译Mvc引擎</summary>
        /// <param name="viewAssemblies"></param>
        /// <param name="viewPageActivator"></param>
        public CompositePrecompiledMvcEngine(IEnumerable<PrecompiledViewAssembly> viewAssemblies, IViewPageActivator viewPageActivator)
            : base(viewPageActivator)
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
            //_viewPageActivator = (viewPageActivator ?? (DependencyResolver.Current.GetService<IViewPageActivator>() ?? DefaultViewPageActivator.Current));
        }

        /// <summary>文件是否存在。如果存在，则由当前引擎创建视图</summary>
        /// <param name="controllerContext"></param>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        protected override Boolean FileExists(ControllerContext controllerContext, String virtualPath)
        {
            ViewMapping viewMapping;
            // 如果映射表不存在，就不要掺合啦
            if (!_mappings.TryGetValue(virtualPath, out viewMapping)) return false;

            //if (!Exists(virtualPath)) return false;

            var asm = viewMapping.ViewAssembly;
            // 两个条件任意一个满足即可使用物理文件
            // 如果不要求取代物理文件，并且虚拟文件存在，则使用物理文件创建
            if (!asm.PreemptPhysicalFiles && VirtualPathProvider.FileExists(virtualPath)) return false;

            // 如果使用较新的物理文件，且物理文件的确较新，则使用物理文件创建
            if (asm.UsePhysicalViewsIfNewer && asm.IsPhysicalFileNewer(virtualPath)) return false;

            return true;
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
            if (!_mappings.TryGetValue(viewPath, out viewMapping)) return null;

            return new PrecompiledMvcView(viewPath, masterPath, viewMapping.Type, runViewStartPages, FileExtensions, ViewPageActivator);
        }

        /// <summary>创建实例。Start和Layout会调用这里</summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public Object CreateInstance(String virtualPath)
        {
            ViewMapping viewMapping;

            // 如果没有该映射，则直接返回空
            if (!_mappings.TryGetValue(virtualPath, out viewMapping)) return null;

            var asm = viewMapping.ViewAssembly;
            // 两个条件任意一个满足即可使用物理文件
            // 如果不要求取代物理文件，并且虚拟文件存在，则使用物理文件创建
            if (!asm.PreemptPhysicalFiles && VirtualPathProvider.FileExists(virtualPath))
                return BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(WebPageRenderingBase));

            // 如果使用较新的物理文件，且物理文件的确较新，则使用物理文件创建
            if (asm.UsePhysicalViewsIfNewer && asm.IsPhysicalFileNewer(virtualPath))
                return BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(WebViewPage));

            // 最后使用内嵌类创建
            return ViewPageActivator.Create(null, viewMapping.Type);
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