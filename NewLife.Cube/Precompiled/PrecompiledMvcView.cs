using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Web.Mvc;
using System.Web.WebPages;

namespace NewLife.Cube.Precompiled
{
    /// <summary>预编译视图</summary>
    public class PrecompiledMvcView : IView
    {
        private static Lazy<Action<WebViewPage, String>> _overriddenLayoutSetter = new Lazy<Action<WebViewPage, String>>(() => CreateOverriddenLayoutSetterDelegate());
        private readonly Type _type;
        private readonly String _virtualPath;
        private readonly String _masterPath;
        private readonly IViewPageActivator _viewPageActivator;

        /// <summary>是否运行视图开始页ViewStart</summary>
        public Boolean RunViewStartPages { get; private set; }

        /// <summary>视图开始页扩展</summary>
        public IEnumerable<String> ViewStartFileExtensions { get; private set; }

        /// <summary>虚拟路径</summary>
        public String VirtualPath { get { return _virtualPath; } }

        ///// <summary>实例化预编译视图</summary>
        ///// <param name="virtualPath"></param>
        ///// <param name="type"></param>
        ///// <param name="runViewStartPages"></param>
        ///// <param name="fileExtension"></param>
        //public PrecompiledMvcView(String virtualPath, Type type, Boolean runViewStartPages, IEnumerable<String> fileExtension) : this(virtualPath, null, type, runViewStartPages, fileExtension) { }

        ///// <summary>实例化预编译视图</summary>
        ///// <param name="virtualPath"></param>
        ///// <param name="masterPath"></param>
        ///// <param name="type"></param>
        ///// <param name="runViewStartPages"></param>
        ///// <param name="fileExtension"></param>
        //public PrecompiledMvcView(String virtualPath, String masterPath, Type type, Boolean runViewStartPages, IEnumerable<String> fileExtension) : this(virtualPath, masterPath, type, runViewStartPages, fileExtension, null) { }

        /// <summary>实例化预编译视图</summary>
        /// <param name="virtualPath"></param>
        /// <param name="masterPath"></param>
        /// <param name="type"></param>
        /// <param name="runViewStartPages"></param>
        /// <param name="fileExtension"></param>
        /// <param name="viewPageActivator"></param>
        public PrecompiledMvcView(String virtualPath, String masterPath, Type type, Boolean runViewStartPages, IEnumerable<String> fileExtension, IViewPageActivator viewPageActivator)
        {
            _type = type;
            _virtualPath = virtualPath;
            _masterPath = masterPath;
            RunViewStartPages = runViewStartPages;
            ViewStartFileExtensions = fileExtension;
            //_viewPageActivator = (viewPageActivator ?? (DependencyResolver.Current.GetService<IViewPageActivator>() ?? DefaultViewPageActivator.Current));
            _viewPageActivator = viewPageActivator;
        }

        /// <summary>生成视图内容</summary>
        /// <param name="viewContext"></param>
        /// <param name="writer"></param>
        public void Render(ViewContext viewContext, TextWriter writer)
        {
            var webViewPage = _viewPageActivator.Create(viewContext.Controller.ControllerContext, _type) as WebViewPage;
            if (webViewPage == null) throw new InvalidOperationException("无效视图类型");

            if (!String.IsNullOrEmpty(_masterPath))
            {
                _overriddenLayoutSetter.Value(webViewPage, _masterPath);
            }
            webViewPage.VirtualPath = _virtualPath;
            webViewPage.ViewContext = viewContext;
            webViewPage.ViewData = viewContext.ViewData;
            webViewPage.InitHelpers();

            WebPageRenderingBase startPage = null;
            if (RunViewStartPages) startPage = StartPage.GetStartPage(webViewPage, "_ViewStart", ViewStartFileExtensions);

            var pageContext = new WebPageContext(viewContext.HttpContext, webViewPage, null);
            webViewPage.ExecutePageHierarchy(pageContext, writer, startPage);
        }

        private static Action<WebViewPage, String> CreateOverriddenLayoutSetterDelegate()
        {
            var property = typeof(WebViewPage).GetProperty("OverridenLayoutPath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null)
                throw new NotSupportedException("The WebViewPage internal property \"OverridenLayoutPath\" does not exist, probably due to an unsupported run-time version.");

            var setMethod = property.GetSetMethod(true);
            if (setMethod == null)
                throw new NotSupportedException("The WebViewPage internal property \"OverridenLayoutPath\" exists but is missing a set method, probably due to an unsupported run-time version.");

            return (Action<WebViewPage, String>)Delegate.CreateDelegate(typeof(Action<WebViewPage, String>), setMethod, true);
        }
    }
}