using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.WebPages;
namespace RazorGenerator.Mvc
{
	public class PrecompiledViewAssembly
	{
		private readonly string _baseVirtualPath;
		private readonly Assembly _assembly;
		private readonly Lazy<DateTime> _assemblyLastWriteTime;
		public bool PreemptPhysicalFiles
		{
			get;
			set;
		}
		public bool UsePhysicalViewsIfNewer
		{
			get;
			set;
		}
		public PrecompiledViewAssembly(Assembly assembly) : this(assembly, null)
		{
		}
		public PrecompiledViewAssembly(Assembly assembly, string baseVirtualPath)
		{
			if (assembly == null)
			{
				throw new ArgumentNullException("assembly");
			}
			this._baseVirtualPath = PrecompiledMvcEngine.NormalizeBaseVirtualPath(baseVirtualPath);
			this._assembly = assembly;
			this._assemblyLastWriteTime = new Lazy<DateTime>(() => this._assembly.GetLastWriteTimeUtc(DateTime.MaxValue));
		}
		public static PrecompiledViewAssembly OfType<T>(string baseVirtualPath, bool usePhysicalViewsIfNewer = false, bool preemptPhysicalFiles = false)
		{
			return new PrecompiledViewAssembly(typeof(T).Assembly, baseVirtualPath)
			{
				UsePhysicalViewsIfNewer = usePhysicalViewsIfNewer,
				PreemptPhysicalFiles = preemptPhysicalFiles
			};
		}
		public static PrecompiledViewAssembly OfType<T>(bool usePhysicalViewsIfNewer = false, bool preemptPhysicalFiles = false)
		{
			return new PrecompiledViewAssembly(typeof(T).Assembly)
			{
				UsePhysicalViewsIfNewer = usePhysicalViewsIfNewer,
				PreemptPhysicalFiles = preemptPhysicalFiles
			};
		}
		public IDictionary<string, Type> GetTypeMappings()
		{
			return (
				from type in this._assembly.GetTypes()
				where typeof(WebPageRenderingBase).IsAssignableFrom(type)
				let pageVirtualPath = type.GetCustomAttributes(false).OfType<PageVirtualPathAttribute>().FirstOrDefault<PageVirtualPathAttribute>()
				where pageVirtualPath != null
				select new KeyValuePair<string, Type>(PrecompiledViewAssembly.CombineVirtualPaths(this._baseVirtualPath, pageVirtualPath.VirtualPath), type)).ToDictionary((KeyValuePair<string, Type> t) => t.Key, (KeyValuePair<string, Type> t) => t.Value, StringComparer.OrdinalIgnoreCase);
		}
		public bool IsPhysicalFileNewer(string virtualPath)
		{
			return PrecompiledMvcEngine.IsPhysicalFileNewer(virtualPath, this._baseVirtualPath, this._assemblyLastWriteTime);
		}
		private static string CombineVirtualPaths(string baseVirtualPath, string virtualPath)
		{
			string result;
			if (!string.IsNullOrEmpty(baseVirtualPath))
			{
				result = VirtualPathUtility.Combine(baseVirtualPath, virtualPath.Substring(2));
			}
			else
			{
				result = virtualPath;
			}
			return result;
		}
	}
}
