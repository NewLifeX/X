using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;
using System.Web.Hosting;

namespace NewLife.Cube.Precompiled
{
    /// <summary>视图路径提供者</summary>
    public class ViewPathProvider : VirtualPathProvider
    {
        /// <summary>文件是否存在</summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public override bool FileExists(string virtualPath)
        {
            return Pages.IsExistByVirtualPath(virtualPath) || base.FileExists(virtualPath);
        }

        /// <summary>获取视图文件</summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public override VirtualFile GetFile(string virtualPath)
        {
            if (Pages.IsExistByVirtualPath(virtualPath))
            {
                return new ViewFile(virtualPath);
            }
            return base.GetFile(virtualPath);
        }

        /// <summary>获取缓存依赖</summary>
        /// <param name="virtualPath"></param>
        /// <param name="virtualPathDependencies"></param>
        /// <param name="utcStart"></param>
        /// <returns></returns>
        public override CacheDependency GetCacheDependency(string virtualPath, System.Collections.IEnumerable virtualPathDependencies, DateTime utcStart)
        {
            if (Pages.IsExistByVirtualPath(virtualPath))
                return ViewCacheDependencyManager.Instance.Get(virtualPath);

            return Previous.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);
        }
    }
}