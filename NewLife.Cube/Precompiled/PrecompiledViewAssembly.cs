using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.WebPages;

namespace NewLife.Cube.Precompiled
{
    /// <summary>预编译视图程序集</summary>
    public class PrecompiledViewAssembly
    {
        private readonly string _baseVirtualPath;
        private readonly Assembly _assembly;
        private readonly Lazy<DateTime> _assemblyLastWriteTime;

        /// <summary>优先使用物理文件</summary>
        public bool PreemptPhysicalFiles { get; set; }

        /// <summary>使用更新的物理文件</summary>
        public bool UsePhysicalViewsIfNewer { get; set; }

        /// <summary>实例化预编译视图程序集</summary>
        /// <param name="assembly"></param>
        public PrecompiledViewAssembly(Assembly assembly) : this(assembly, null) { }

        /// <summary>实例化预编译视图程序集</summary>
        /// <param name="assembly"></param>
        /// <param name="baseVirtualPath"></param>
        public PrecompiledViewAssembly(Assembly assembly, string baseVirtualPath)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");

            _baseVirtualPath = PrecompiledMvcEngine.NormalizeBaseVirtualPath(baseVirtualPath);
            _assembly = assembly;
            _assemblyLastWriteTime = new Lazy<DateTime>(() => _assembly.GetLastWriteTimeUtc(DateTime.MaxValue));
        }

        /// <summary>为指定类型所在程序集创建实例</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="baseVirtualPath"></param>
        /// <param name="usePhysicalViewsIfNewer"></param>
        /// <param name="preemptPhysicalFiles"></param>
        /// <returns></returns>
        public static PrecompiledViewAssembly OfType<T>(string baseVirtualPath, bool usePhysicalViewsIfNewer = false, bool preemptPhysicalFiles = false)
        {
            return new PrecompiledViewAssembly(typeof(T).Assembly, baseVirtualPath)
            {
                UsePhysicalViewsIfNewer = usePhysicalViewsIfNewer,
                PreemptPhysicalFiles = preemptPhysicalFiles
            };
        }

        /// <summary>为指定类型所在程序集创建实例</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="usePhysicalViewsIfNewer"></param>
        /// <param name="preemptPhysicalFiles"></param>
        /// <returns></returns>
        public static PrecompiledViewAssembly OfType<T>(bool usePhysicalViewsIfNewer = false, bool preemptPhysicalFiles = false)
        {
            return new PrecompiledViewAssembly(typeof(T).Assembly)
            {
                UsePhysicalViewsIfNewer = usePhysicalViewsIfNewer,
                PreemptPhysicalFiles = preemptPhysicalFiles
            };
        }

        /// <summary>遍历获取所有类型映射</summary>
        /// <returns></returns>
        public IDictionary<string, Type> GetTypeMappings()
        {
            return (
                from type in _assembly.GetTypes()
                where typeof(WebPageRenderingBase).IsAssignableFrom(type)
                let pageVirtualPath = type.GetCustomAttributes(false).OfType<PageVirtualPathAttribute>().FirstOrDefault<PageVirtualPathAttribute>()
                where pageVirtualPath != null
                select new KeyValuePair<string, Type>(PrecompiledViewAssembly.CombineVirtualPaths(_baseVirtualPath, pageVirtualPath.VirtualPath), type)).ToDictionary((KeyValuePair<string, Type> t) => t.Key, (KeyValuePair<string, Type> t) => t.Value, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>物理文件是否更新</summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public bool IsPhysicalFileNewer(string virtualPath)
        {
            return PrecompiledMvcEngine.IsPhysicalFileNewer(virtualPath, _baseVirtualPath, _assemblyLastWriteTime);
        }

        private static string CombineVirtualPaths(string baseVirtualPath, string virtualPath)
        {
            if (!string.IsNullOrEmpty(baseVirtualPath))
                return VirtualPathUtility.Combine(baseVirtualPath, virtualPath.Substring(2));
            else
                return virtualPath;
        }
    }
}