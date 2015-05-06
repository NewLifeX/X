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
        private static Lazy<Action<WebViewPage, string>> _overriddenLayoutSetter = new Lazy<Action<WebViewPage, string>>(() => CreateOverriddenLayoutSetterDelegate());
        private readonly Type _type;
        private readonly string _virtualPath;
        private readonly string _masterPath;
        private readonly IViewPageActivator _viewPageActivator;

        /// <summary>是否运行视图开始页ViewStart</summary>
        public bool RunViewStartPages { get; private set; }

        /// <summary>视图开始页扩展</summary>
        public IEnumerable<string> ViewStartFileExtensions { get; private set; }

        /// <summary>虚拟路径</summary>
        public string VirtualPath { get { return this._virtualPath; } }

        /// <summary>实例化预编译视图</summary>
        /// <param name="virtualPath"></param>
        /// <param name="type"></param>
        /// <param name="runViewStartPages"></param>
        /// <param name="fileExtension"></param>
        public PrecompiledMvcView(string virtualPath, Type type, bool runViewStartPages, IEnumerable<string> fileExtension) : this(virtualPath, null, type, runViewStartPages, fileExtension) { }

        /// <summary>实例化预编译视图</summary>
        /// <param name="virtualPath"></param>
        /// <param name="masterPath"></param>
        /// <param name="type"></param>
        /// <param name="runViewStartPages"></param>
        /// <param name="fileExtension"></param>
        public PrecompiledMvcView(string virtualPath, string masterPath, Type type, bool runViewStartPages, IEnumerable<string> fileExtension) : this(virtualPath, masterPath, type, runViewStartPages, fileExtension, null) { }

        /// <summary>实例化预编译视图</summary>
        /// <param name="virtualPath"></param>
        /// <param name="masterPath"></param>
        /// <param name="type"></param>
        /// <param name="runViewStartPages"></param>
        /// <param name="fileExtension"></param>
        /// <param name="viewPageActivator"></param>
        public PrecompiledMvcView(string virtualPath, string masterPath, Type type, bool runViewStartPages, IEnumerable<string> fileExtension, IViewPageActivator viewPageActivator)
        {
            this._type = type;
            this._virtualPath = virtualPath;
            this._masterPath = masterPath;
            this.RunViewStartPages = runViewStartPages;
            this.ViewStartFileExtensions = fileExtension;
            this._viewPageActivator = (viewPageActivator ?? (DependencyResolver.Current.GetService<IViewPageActivator>() ?? DefaultViewPageActivator.Current));
        }

        /// <summary>生成视图内容</summary>
        /// <param name="viewContext"></param>
        /// <param name="writer"></param>
        public void Render(ViewContext viewContext, TextWriter writer)
        {
            var webViewPage = this._viewPageActivator.Create(viewContext.Controller.ControllerContext, this._type) as WebViewPage;
            if (webViewPage == null) throw new InvalidOperationException("无效视图类型");

            if (!string.IsNullOrEmpty(this._masterPath))
            {
                _overriddenLayoutSetter.Value(webViewPage, this._masterPath);
            }
            webViewPage.VirtualPath = this._virtualPath;
            webViewPage.ViewContext = viewContext;
            webViewPage.ViewData = viewContext.ViewData;
            webViewPage.InitHelpers();

            WebPageRenderingBase startPage = null;
            if (this.RunViewStartPages) startPage = StartPage.GetStartPage(webViewPage, "_ViewStart", this.ViewStartFileExtensions);

            var pageContext = new WebPageContext(viewContext.HttpContext, webViewPage, null);
            webViewPage.ExecutePageHierarchy(pageContext, writer, startPage);
        }

        private static Action<WebViewPage, string> CreateOverriddenLayoutSetterDelegate()
        {
            var property = typeof(WebViewPage).GetProperty("OverridenLayoutPath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null)
                throw new NotSupportedException("The WebViewPage internal property \"OverridenLayoutPath\" does not exist, probably due to an unsupported run-time version.");

            var setMethod = property.GetSetMethod(true);
            if (setMethod == null)
                throw new NotSupportedException("The WebViewPage internal property \"OverridenLayoutPath\" exists but is missing a set method, probably due to an unsupported run-time version.");

            return (Action<WebViewPage, string>)Delegate.CreateDelegate(typeof(Action<WebViewPage, string>), setMethod, true);
        }
    }
}