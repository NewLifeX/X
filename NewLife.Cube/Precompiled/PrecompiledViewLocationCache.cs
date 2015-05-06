using System;
using System.Web;
using System.Web.Mvc;

namespace NewLife.Cube.Precompiled
{
    /// <summary>预编译视图程序集缓存</summary>
	public class PrecompiledViewLocationCache : IViewLocationCache
	{
		private readonly string _assemblyName;
		private readonly IViewLocationCache _innerCache;

        /// <summary>实例化</summary>
        /// <param name="assemblyName"></param>
        /// <param name="innerCache"></param>
		public PrecompiledViewLocationCache(string assemblyName, IViewLocationCache innerCache)
		{
			this._assemblyName = assemblyName;
			this._innerCache = innerCache;
		}

        /// <summary>获取视图位置</summary>
        /// <param name="httpContext"></param>
        /// <param name="key"></param>
        /// <returns></returns>
		public string GetViewLocation(HttpContextBase httpContext, string key)
		{
			key = this._assemblyName + "::" + key;
			return this._innerCache.GetViewLocation(httpContext, key);
		}

        /// <summary>插入视图位置</summary>
        /// <param name="httpContext"></param>
        /// <param name="key"></param>
        /// <param name="virtualPath"></param>
		public void InsertViewLocation(HttpContextBase httpContext, string key, string virtualPath)
		{
			key = this._assemblyName + "::" + key;
			this._innerCache.InsertViewLocation(httpContext, key, virtualPath);
		}
	}
}