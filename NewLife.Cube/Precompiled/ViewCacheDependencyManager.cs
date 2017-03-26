using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;

namespace NewLife.Cube.Precompiled
{
    /// <summary>视图缓存依赖管理</summary>
    public class ViewCacheDependencyManager
    {
        private static Dictionary<String, ViewCacheDependency> dependencies = new Dictionary<String, ViewCacheDependency>();
        private static volatile ViewCacheDependencyManager instance;
        private static Object syncRoot = new Object();
        private ViewCacheDependencyManager() { }

        /// <summary>实例</summary>
        public static ViewCacheDependencyManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new ViewCacheDependencyManager();
                        }
                    }
                }
                return instance;
            }
        }

        /// <summary>获取缓存依赖</summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public CacheDependency Get(String virtualPath)
        {
            if (!dependencies.ContainsKey(virtualPath))
                dependencies.Add(virtualPath, new ViewCacheDependency(virtualPath));

            return dependencies[virtualPath];
        }

        /// <summary>验证</summary>
        /// <param name="virtualPath"></param>
        public void Invalidate(String virtualPath)
        {
            if (dependencies.ContainsKey(virtualPath))
            {
                var dependency = dependencies[virtualPath];
                dependency.Invalidate();
                dependency.Dispose();
                dependencies.Remove(virtualPath);
            }
        }
    }
}