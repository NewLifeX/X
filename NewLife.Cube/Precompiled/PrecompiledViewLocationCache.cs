using System;
using System.Web;
using System.Web.Mvc;

namespace NewLife.Cube.Precompiled
{
    /// <summary>预编译视图程序集缓存</summary>
	public class PrecompiledViewLocationCache : IViewLocationCache
	{
		private readonly String _assemblyName;
		private readonly IViewLocationCache _innerCache;

        /// <summary>实例化</summary>
        /// <param name="assemblyName"></param>
        /// <param name="innerCache"></param>
		public PrecompiledViewLocationCache(String assemblyName, IViewLocationCache innerCache)
		{
			_assemblyName = assemblyName;
			_innerCache = innerCache;
		}

        /// <summary>获取视图位置</summary>
        /// <param name="httpContext"></param>
        /// <param name="key"></param>
        /// <returns></returns>
		public String GetViewLocation(HttpContextBase httpContext, String key)
		{
			key = _assemblyName + "::" + key;
			return _innerCache.GetViewLocation(httpContext, key);
		}

        /// <summary>插入视图位置</summary>
        /// <param name="httpContext"></param>
        /// <param name="key"></param>
        /// <param name="virtualPath"></param>
		public void InsertViewLocation(HttpContextBase httpContext, String key, String virtualPath)
		{
			key = _assemblyName + "::" + key;
			_innerCache.InsertViewLocation(httpContext, key, virtualPath);
		}
	}
}