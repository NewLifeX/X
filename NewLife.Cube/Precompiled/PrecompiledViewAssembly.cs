using System;
using System.Collections.Generic;
using System.Reflection;

namespace NewLife.Cube.Precompiled
{
    /// <summary>预编译视图程序集</summary>
    public class PrecompiledViewAssembly
    {
        private readonly String _baseVirtualPath;
        private readonly Assembly _assembly;
        private readonly Lazy<DateTime> _assemblyLastWriteTime;

        /// <summary>取代物理文件，优先内嵌类</summary>
        public Boolean PreemptPhysicalFiles { get; set; }

        /// <summary>仅在物理文件较新时使用物理文件</summary>
        public Boolean UsePhysicalViewsIfNewer { get; set; }

        /// <summary>实例化预编译视图程序集</summary>
        /// <param name="assembly"></param>
        public PrecompiledViewAssembly(Assembly assembly) : this(assembly, null) { }

        /// <summary>实例化预编译视图程序集</summary>
        /// <param name="assembly"></param>
        /// <param name="baseVirtualPath"></param>
        public PrecompiledViewAssembly(Assembly assembly, String baseVirtualPath)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");

            // 为了实现物理文件“重载覆盖”的效果，强制使用物理文件
            PreemptPhysicalFiles = false;
            UsePhysicalViewsIfNewer = false;

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
        public static PrecompiledViewAssembly OfType<T>(String baseVirtualPath, Boolean usePhysicalViewsIfNewer = false, Boolean preemptPhysicalFiles = false)
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
        public static PrecompiledViewAssembly OfType<T>(Boolean usePhysicalViewsIfNewer = false, Boolean preemptPhysicalFiles = false)
        {
            return new PrecompiledViewAssembly(typeof(T).Assembly)
            {
                UsePhysicalViewsIfNewer = usePhysicalViewsIfNewer,
                PreemptPhysicalFiles = preemptPhysicalFiles
            };
        }

        /// <summary>遍历获取所有类型映射</summary>
        /// <returns></returns>
        public IDictionary<String, Type> GetTypeMappings()
        {
            return PrecompiledMvcEngine.GetTypeMappings(_assembly, _baseVirtualPath);
        }

        /// <summary>物理文件是否更新</summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public Boolean IsPhysicalFileNewer(String virtualPath)
        {
            return PrecompiledMvcEngine.IsPhysicalFileNewer(virtualPath, _baseVirtualPath, _assemblyLastWriteTime);
        }

    }
}