using System;
using System.Web;
using System.Web.Mvc;
namespace RazorGenerator.Mvc
{
	public class PrecompiledViewLocationCache : IViewLocationCache
	{
		private readonly string _assemblyName;
		private readonly IViewLocationCache _innerCache;
		public PrecompiledViewLocationCache(string assemblyName, IViewLocationCache innerCache)
		{
			this._assemblyName = assemblyName;
			this._innerCache = innerCache;
		}
		public string GetViewLocation(HttpContextBase httpContext, string key)
		{
			key = this._assemblyName + "::" + key;
			return this._innerCache.GetViewLocation(httpContext, key);
		}
		public void InsertViewLocation(HttpContextBase httpContext, string key, string virtualPath)
		{
			key = this._assemblyName + "::" + key;
			this._innerCache.InsertViewLocation(httpContext, key, virtualPath);
		}
	}
}
