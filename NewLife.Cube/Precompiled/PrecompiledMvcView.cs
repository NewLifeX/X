using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Web.Mvc;
using System.Web.WebPages;
namespace RazorGenerator.Mvc
{
	public class PrecompiledMvcView : IView
	{
		private static Lazy<Action<WebViewPage, string>> _overriddenLayoutSetter = new Lazy<Action<WebViewPage, string>>(() => PrecompiledMvcView.CreateOverriddenLayoutSetterDelegate());
		private readonly Type _type;
		private readonly string _virtualPath;
		private readonly string _masterPath;
		private readonly IViewPageActivator _viewPageActivator;
		public bool RunViewStartPages
		{
			get;
			private set;
		}
		public IEnumerable<string> ViewStartFileExtensions
		{
			get;
			private set;
		}
		public string VirtualPath
		{
			get
			{
				return this._virtualPath;
			}
		}
		public PrecompiledMvcView(string virtualPath, Type type, bool runViewStartPages, IEnumerable<string> fileExtension) : this(virtualPath, null, type, runViewStartPages, fileExtension)
		{
		}
		public PrecompiledMvcView(string virtualPath, string masterPath, Type type, bool runViewStartPages, IEnumerable<string> fileExtension) : this(virtualPath, masterPath, type, runViewStartPages, fileExtension, null)
		{
		}
		public PrecompiledMvcView(string virtualPath, string masterPath, Type type, bool runViewStartPages, IEnumerable<string> fileExtension, IViewPageActivator viewPageActivator)
		{
			this._type = type;
			this._virtualPath = virtualPath;
			this._masterPath = masterPath;
			this.RunViewStartPages = runViewStartPages;
			this.ViewStartFileExtensions = fileExtension;
			this._viewPageActivator = (viewPageActivator ?? (DependencyResolver.Current.GetService<IViewPageActivator>() ?? DefaultViewPageActivator.Current));
		}
		public void Render(ViewContext viewContext, TextWriter writer)
		{
			WebViewPage webViewPage = this._viewPageActivator.Create(viewContext.Controller.ControllerContext, this._type) as WebViewPage;
			if (webViewPage == null)
			{
				throw new InvalidOperationException("Invalid view type");
			}
			if (!string.IsNullOrEmpty(this._masterPath))
			{
				PrecompiledMvcView._overriddenLayoutSetter.Value(webViewPage, this._masterPath);
			}
			webViewPage.VirtualPath = this._virtualPath;
			webViewPage.ViewContext = viewContext;
			webViewPage.ViewData = viewContext.ViewData;
			webViewPage.InitHelpers();
			WebPageRenderingBase startPage = null;
			if (this.RunViewStartPages)
			{
				startPage = StartPage.GetStartPage(webViewPage, "_ViewStart", this.ViewStartFileExtensions);
			}
			WebPageContext pageContext = new WebPageContext(viewContext.HttpContext, webViewPage, null);
			webViewPage.ExecutePageHierarchy(pageContext, writer, startPage);
		}
		private static Action<WebViewPage, string> CreateOverriddenLayoutSetterDelegate()
		{
			PropertyInfo property = typeof(WebViewPage).GetProperty("OverridenLayoutPath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property == null)
			{
				throw new NotSupportedException("The WebViewPage internal property \"OverridenLayoutPath\" does not exist, probably due to an unsupported run-time version.");
			}
			MethodInfo setMethod = property.GetSetMethod(true);
			if (setMethod == null)
			{
				throw new NotSupportedException("The WebViewPage internal property \"OverridenLayoutPath\" exists but is missing a set method, probably due to an unsupported run-time version.");
			}
			return (Action<WebViewPage, string>)Delegate.CreateDelegate(typeof(Action<WebViewPage, string>), setMethod, true);
		}
	}
}
