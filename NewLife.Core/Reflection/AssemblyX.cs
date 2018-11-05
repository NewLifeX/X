using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
#if __MOBILE__
#elif __CORE__
#else
using System.Web;
#endif
using NewLife.Log;

namespace NewLife.Reflection
{
    /// <summary>程序集辅助类。使用Create创建，保证每个程序集只有一个辅助类</summary>
    public class AssemblyX
    {
        #region 属性
        /// <summary>程序集</summary>
        public Assembly Asm { get; }

        [NonSerialized]
        private List<String> hasLoaded = new List<String>();

        private String _Name;
        /// <summary>名称</summary>
        public String Name => _Name ?? (_Name = "" + Asm.GetName().Name);

        private String _Version;
        /// <summary>程序集版本</summary>
        public String Version => _Version ?? (_Version = "" + Asm.GetName().Version);

        private String _Title;
        /// <summary>程序集标题</summary>
        public String Title => _Title ?? (_Title = "" + Asm.GetCustomAttributeValue<AssemblyTitleAttribute, String>());

        private String _FileVersion;
        /// <summary>文件版本</summary>
        public String FileVersion => _FileVersion ?? (_FileVersion = "" + Asm.GetCustomAttributeValue<AssemblyFileVersionAttribute, String>());

        private DateTime _Compile;
        /// <summary>编译时间</summary>
        public DateTime Compile
        {
            get
            {
                if (_Compile <= DateTime.MinValue && !hasLoaded.Contains("Compile"))
                {
                    hasLoaded.Add("Compile");

                    if (!String.IsNullOrEmpty(Version))
                    {
                        var ss = Version.Split(new Char[] { '.' });
                        var d = Convert.ToInt32(ss[2]);
                        var s = Convert.ToInt32(ss[3]);

                        var dt = new DateTime(2000, 1, 1);
                        dt = dt.AddDays(d).AddSeconds(s * 2);

                        _Compile = dt;
                    }
                }
                return _Compile;
            }
        }

        private Version _CompileVersion;
        /// <summary>编译版本</summary>
        public Version CompileVersion
        {
            get
            {
                if (_CompileVersion == null)
                {
                    var ver = Asm.GetName().Version;
                    if (ver == null) ver = new Version(1, 0);

                    var dt = Compile;
                    ver = new Version(ver.Major, ver.Minor, dt.Year, dt.Month * 100 + dt.Day);

                    _CompileVersion = ver;
                }
                return _CompileVersion;
            }
        }

        private String _Company;
        /// <summary>公司名称</summary>
        public String Company => _Company ?? (_Company = "" + Asm.GetCustomAttributeValue<AssemblyCompanyAttribute, String>());

        private String _Description;
        /// <summary>说明</summary>
        public String Description => _Description ?? (_Description = "" + Asm.GetCustomAttributeValue<AssemblyDescriptionAttribute, String>());

        /// <summary>获取包含清单的已加载文件的路径或 UNC 位置。</summary>
        public String Location
        {
            get
            {
                try
                {
#if !__IOS__ && !__CORE__
                    return Asm == null || Asm is _AssemblyBuilder || Asm.IsDynamic ? null : Asm.Location;
#else
                    return Asm == null || Asm.IsDynamic ? null : Asm.Location;
#endif
                }
                catch { return null; }
            }
        }
        #endregion

        #region 构造
        private AssemblyX(Assembly asm) => Asm = asm;

        private static ConcurrentDictionary<Assembly, AssemblyX> cache = new ConcurrentDictionary<Assembly, AssemblyX>();
        /// <summary>创建程序集辅助对象</summary>
        /// <param name="asm"></param>
        /// <returns></returns>
        public static AssemblyX Create(Assembly asm)
        {
            if (asm == null) return null;

            return cache.GetOrAdd(asm, key => new AssemblyX(key));
        }

        static AssemblyX()
        {
#if !__MOBILE__
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += (sender, args) =>
            {
                var flag = XTrace.Debug && XTrace.Log.Level <= LogLevel.Debug;
                if (flag) XTrace.WriteLine("[{0}]请求只反射加载[{1}]", args.RequestingAssembly?.FullName, args.Name);
                return Assembly.ReflectionOnlyLoad(args.Name);
            };
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var flag = XTrace.Debug && XTrace.Log.Level <= LogLevel.Debug;
                if (flag) XTrace.WriteLine("[{0}]请求加载[{1}]", args.RequestingAssembly?.FullName, args.Name);
                return OnResolve(args.Name);
            };
#endif
        }
        #endregion

        #region 扩展属性
        //private IEnumerable<Type> _Types;
        /// <summary>类型集合，当前程序集的所有类型，包括私有和内嵌，非内嵌请直接调用Asm.GetTypes()</summary>
        public IEnumerable<Type> Types
        {
            get
            {
                Type[] ts = null;
                try
                {
                    ts = Asm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    if (ex.LoaderExceptions != null && XTrace.Log.Level == LogLevel.Debug)
                    {
                        XTrace.WriteLine("加载[{0}]{1}的类型时发生个{2}错误！", this, Location, ex.LoaderExceptions.Length);
                        foreach (var le in ex.LoaderExceptions)
                        {
                            XTrace.WriteException(le);
                        }
                    }
                    ts = ex.Types;
                }
                if (ts == null || ts.Length < 1) yield break;

                // 先遍历一次ts，避免取内嵌类型带来不必要的性能损耗
                foreach (var item in ts)
                {
                    if (item != null) yield return item;
                }

                var queue = new Queue<Type>(ts);
                while (queue.Count > 0)
                {
                    var item = queue.Dequeue();
                    if (item == null) continue;

                    var ts2 = item.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    if (ts2 != null && ts2.Length > 0)
                    {
                        // 从下一个元素开始插入，让内嵌类紧挨着主类
                        //Int32 k = i + 1;
                        foreach (var elm in ts2)
                        {
                            //if (!list.Contains(item)) list.Insert(k++, item);
                            // Insert将会导致大量的数组复制
                            queue.Enqueue(elm);

                            yield return elm;
                        }
                    }
                }
            }
        }

        /// <summary>是否系统程序集</summary>
        public Boolean IsSystemAssembly => CheckSystem(Asm);

        private static Boolean CheckSystem(Assembly asm)
        {
            if (asm == null) return false;

            var name = asm.FullName;
            if (name.EndsWith("PublicKeyToken=b77a5c561934e089")) return true;
            if (name.EndsWith("PublicKeyToken=b03f5f7f11d50a3a")) return true;
            if (name.EndsWith("PublicKeyToken=89845dcd8080cc91")) return true;
            if (name.EndsWith("PublicKeyToken=31bf3856ad364e35")) return true;

            return false;
        }
        #endregion

        #region 静态属性
        /// <summary>入口程序集</summary>
        public static AssemblyX Entry => Create(Assembly.GetEntryAssembly());
        #endregion

        #region 方法
        ConcurrentDictionary<String, Type> typeCache2 = new ConcurrentDictionary<String, Type>();
        /// <summary>从程序集中查找指定名称的类型</summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public Type GetType(String typeName)
        {
            if (String.IsNullOrEmpty(typeName)) throw new ArgumentNullException("typeName");

            return typeCache2.GetOrAdd(typeName, GetTypeInternal);
        }

        /// <summary>在程序集中查找类型</summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        Type GetTypeInternal(String typeName)
        {
            var type = Asm.GetType(typeName);
            if (type != null) return type;

            // 如果没有包含圆点，说明其不是FullName
            if (!typeName.Contains("."))
            {
                //try
                //{
                //    var types = Asm.GetTypes();
                //    if (types != null && types.Length > 0)
                //    {
                //        foreach (var item in types)
                //        {
                //            if (item.Name == typeName) return item;
                //        }
                //    }
                //}
                //catch (ReflectionTypeLoadException ex)
                //{
                //    if (XTrace.Debug)
                //    {
                //        //XTrace.WriteException(ex);
                //        XTrace.WriteLine("加载[{0}]{1}的类型时发生个{2}错误！", this, Location, ex.LoaderExceptions.Length);

                //        foreach (var item in ex.LoaderExceptions)
                //        {
                //            XTrace.WriteException(item);
                //        }
                //    }

                //    return null;
                //}
                //catch (Exception ex)
                //{
                //    if (XTrace.Debug) XTrace.WriteException(ex);

                //    return null;
                //}

                // 遍历所有类型，包括内嵌类型
                foreach (var item in Types)
                {
                    if (item.Name == typeName) return item;
                }
            }

            return null;
        }
        #endregion

        #region 插件
        /// <summary>查找插件</summary>
        /// <typeparam name="TPlugin"></typeparam>
        /// <returns></returns>
        internal List<Type> FindPlugins<TPlugin>() => FindPlugins(typeof(TPlugin));

        private ConcurrentDictionary<Type, List<Type>> _plugins = new ConcurrentDictionary<Type, List<Type>>();
        /// <summary>查找插件，带缓存</summary>
        /// <param name="baseType">类型</param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal List<Type> FindPlugins(Type baseType)
        {
            // 如果type是null，则返回所有类型

            if (_plugins.TryGetValue(baseType, out var list)) return list;

            list = new List<Type>();
            foreach (var item in Types)
            {
                if (item.IsInterface || item.IsAbstract || item.IsGenericType) continue;
                if (item != baseType && item.As(baseType)) list.Add(item);
            }
            if (list.Count <= 0) list = null;

            _plugins.TryAdd(baseType, list);

            return list;
        }

        /// <summary>查找所有非系统程序集中的所有插件</summary>
        /// <remarks>继承类所在的程序集会引用baseType所在的程序集，利用这一点可以做一定程度的性能优化。</remarks>
        /// <param name="baseType"></param>
        /// <param name="isLoadAssembly">是否从未加载程序集中获取类型。使用仅反射的方法检查目标类型，如果存在，则进行常规加载</param>
        /// <param name="excludeGlobalTypes">指示是否应检查来自所有引用程序集的类型。如果为 false，则检查来自所有引用程序集的类型。 否则，只检查来自非全局程序集缓存 (GAC) 引用的程序集的类型。</param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static IEnumerable<Type> FindAllPlugins(Type baseType, Boolean isLoadAssembly = false, Boolean excludeGlobalTypes = true)
        {
            var baseAssemblyName = baseType.Assembly.GetName().Name;

            // 如果基类所在程序集没有强命名，则搜索时跳过所有强命名程序集
            // 因为继承类程序集的强命名要求基类程序集必须强命名
            var signs = baseType.Assembly.GetName().GetPublicKey();
            var hasNotSign = signs == null || signs.Length <= 0;

            var list = new List<Type>();
            foreach (var item in GetAssemblies())
            {
                signs = item.Asm.GetName().GetPublicKey();
                if (hasNotSign && signs != null && signs.Length > 0) continue;

                // 如果excludeGlobalTypes为true，则指检查来自非GAC引用的程序集
                if (excludeGlobalTypes && item.Asm.GlobalAssemblyCache) continue;

                // 不搜索系统程序集，不搜索未引用基类所在程序集的程序集，优化性能
                if (item.IsSystemAssembly || !IsReferencedFrom(item.Asm, baseAssemblyName)) continue;

                var ts = item.FindPlugins(baseType);
                if (ts != null && ts.Count > 0)
                {
                    foreach (var elm in ts)
                    {
                        if (!list.Contains(elm))
                        {
                            list.Add(elm);
                            yield return elm;
                        }
                    }
                }
            }
            if (isLoadAssembly)
            {
                foreach (var item in ReflectionOnlyGetAssemblies())
                {
                    // 如果excludeGlobalTypes为true，则指检查来自非GAC引用的程序集
                    if (excludeGlobalTypes && item.Asm.GlobalAssemblyCache) continue;

                    // 不搜索系统程序集，不搜索未引用基类所在程序集的程序集，优化性能
                    if (item.IsSystemAssembly || !IsReferencedFrom(item.Asm, baseAssemblyName)) continue;

                    var ts = item.FindPlugins(baseType);
                    if (ts != null && ts.Count > 0)
                    {
                        // 真实加载
                        if (XTrace.Debug)
                        {
                            // 如果是本目录的程序集，去掉目录前缀
                            var file = item.Asm.Location;
                            var root = ".".GetFullPath();
                            if (file.StartsWithIgnoreCase(root)) file = file.Substring(root.Length).TrimStart("\\");
                            XTrace.WriteLine("AssemblyX.FindAllPlugins(\"{0}\") => {1}", baseType.FullName, file);
                        }
                        var asm2 = Assembly.LoadFile(item.Asm.Location);
                        ts = Create(asm2).FindPlugins(baseType);

                        if (ts != null && ts.Count > 0)
                        {
                            foreach (var elm in ts)
                            {
                                if (!list.Contains(elm))
                                {
                                    list.Add(elm);
                                    yield return elm;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary><paramref name="asm"/> 是否引用了 <paramref name="baseAsmName"/></summary>
        /// <param name="asm">程序集</param>
        /// <param name="baseAsmName">被引用程序集全名</param>
        /// <returns></returns>
        private static Boolean IsReferencedFrom(Assembly asm, String baseAsmName)
        {
            //if (asm.FullName.EqualIgnoreCase(baseAsmName)) return true;
            if (asm.GetName().Name.EqualIgnoreCase(baseAsmName)) return true;

            foreach (var item in asm.GetReferencedAssemblies())
            {
                //if (item.FullName.EqualIgnoreCase(baseAsmName)) return true;
                if (item.Name.EqualIgnoreCase(baseAsmName)) return true;
            }

            return false;
        }

        /// <summary>根据名称获取类型</summary>
        /// <param name="typeName">类型名</param>
        /// <param name="isLoadAssembly">是否从未加载程序集中获取类型。使用仅反射的方法检查目标类型，如果存在，则进行常规加载</param>
        /// <returns></returns>
        internal static Type GetType(String typeName, Boolean isLoadAssembly)
        {
            var type = Type.GetType(typeName);
            if (type != null) return type;

            // 加速基础类型识别，忽略大小写
            if (!typeName.Contains("."))
            {
                foreach (var item in Enum.GetNames(typeof(TypeCode)))
                {
                    if (typeName.EqualIgnoreCase(item))
                    {
                        type = Type.GetType("System." + item);
                        if (type != null) return type;
                    }
                }
            }

            // 尝试本程序集
            var asms = new[] {
                Create(Assembly.GetExecutingAssembly()),
                Create(Assembly.GetCallingAssembly()),
                Create(Assembly.GetEntryAssembly()) };
            var loads = new List<AssemblyX>();

            foreach (var asm in asms)
            {
                if (asm == null || loads.Contains(asm)) continue;
                loads.Add(asm);

                type = asm.GetType(typeName);
                if (type != null) return type;
            }

            // 尝试所有程序集
            foreach (var asm in GetAssemblies())
            {
                if (loads.Contains(asm)) continue;
                loads.Add(asm);

                type = asm.GetType(typeName);
                if (type != null) return type;
            }

            // 尝试加载只读程序集
            if (!isLoadAssembly) return null;

            foreach (var asm in ReflectionOnlyGetAssemblies())
            {
                type = asm.GetType(typeName);
                if (type != null)
                {
                    // 真实加载
                    var file = asm.Asm.Location;
                    try
                    {
                        type = null;
                        var asm2 = Assembly.LoadFile(file);
                        var type2 = Create(asm2).GetType(typeName);
                        if (type2 == null) continue;
                        type = type2;
                        if (XTrace.Debug)
                        {
                            var root = ".".GetFullPath();
                            if (file.StartsWithIgnoreCase(root)) file = file.Substring(root.Length).TrimStart("\\");
                            XTrace.WriteLine("TypeX.GetType(\"{0}\") => {1}", typeName, file);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (XTrace.Debug) XTrace.WriteException(ex);
                    }

                    return type;
                }
            }

            return null;
        }
        #endregion

        #region 静态加载
        /// <summary>获取指定程序域所有程序集</summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public static IEnumerable<AssemblyX> GetAssemblies(AppDomain domain = null)
        {
            if (domain == null) domain = AppDomain.CurrentDomain;

            var asms = domain.GetAssemblies();
            if (asms == null || asms.Length < 1) return Enumerable.Empty<AssemblyX>();

            //return asms.Select(item => Create(item));
            return from e in asms select Create(e);

            //foreach (var item in asms)
            //{
            //    yield return Create(item);
            //}
        }

        private static ICollection<String> _AssemblyPaths;
        /// <summary>程序集目录集合</summary>
        public static ICollection<String> AssemblyPaths
        {
            get
            {
                if (_AssemblyPaths == null)
                {
                    var set = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

                    var basedir = AppDomain.CurrentDomain.BaseDirectory;
                    set.Add(basedir);
#if !__MOBILE__ && !__CORE__
                    if (HttpRuntime.AppDomainId != null) set.Add(HttpRuntime.BinDirectory);
#else
                    if (Directory.Exists("bin".GetFullPath())) set.Add("bin".GetFullPath());
#endif
                    var plugin = Setting.Current.GetPluginPath();
                    if (!set.Contains(plugin)) set.Add(plugin);

                    //// 增加所有程序集所在目录为搜索目录，便于查找程序集
                    //foreach (var asm in GetAssemblies())
                    //{
                    //    // GAC程序集和系统程序集跳过
                    //    if (asm.Asm.GlobalAssemblyCache) continue;
                    //    if (asm.IsSystemAssembly) continue;
                    //    if (String.IsNullOrEmpty(asm.Location)) continue;

                    //    var dir = Path.GetDirectoryName(asm.Location).EnsureEnd("\\");
                    //    if (!set.Contains(dir)) set.Add(dir);
                    //}

                    _AssemblyPaths = set;
                }
                return _AssemblyPaths;
            }
            set { _AssemblyPaths = value; }
        }

        /// <summary>获取当前程序域所有只反射程序集的辅助类</summary>
        /// <returns></returns>
        public static IEnumerable<AssemblyX> ReflectionOnlyGetAssemblies()
        {
            var loadeds = GetAssemblies().ToList();

            // 先返回已加载的只加载程序集
            var loadeds2 = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies().Select(e => Create(e)).ToList();
            foreach (var item in loadeds2)
            {
                if (loadeds.Any(e => e.Location.EqualIgnoreCase(item.Location))) continue;
                // 尽管目录不一样，但这两个可能是相同的程序集
                // 这里导致加载了不同目录的同一个程序集，然后导致对象容器频繁报错
                //if (loadeds.Any(e => e.Asm.FullName.EqualIgnoreCase(item.Asm.FullName))) continue;
                // 相同程序集不同版本，全名不想等
                if (loadeds.Any(e => e.Asm.GetName().Name.EqualIgnoreCase(item.Asm.GetName().Name))) continue;

                yield return item;
            }

            foreach (var item in AssemblyPaths)
            {
                foreach (var asm in ReflectionOnlyLoad(item)) yield return asm;
            }
        }

        private static ICollection<String> _BakImages = new List<String>();
        /// <summary>只反射加载指定路径的所有程序集</summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IEnumerable<AssemblyX> ReflectionOnlyLoad(String path)
        {
            if (!Directory.Exists(path)) yield break;

            // 先返回已加载的只加载程序集
            var loadeds2 = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies().Select(e => Create(e)).ToList();

            // 再去遍历目录
            var ss = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
            if (ss == null || ss.Length < 1) yield break;

            var loadeds = GetAssemblies().ToList();

            var ver = new Version(Assembly.GetExecutingAssembly().ImageRuntimeVersion.TrimStart('v'));

            foreach (var item in ss)
            {
                // 仅尝试加载dll和exe，不加载vshost文件
                if (!item.EndsWithIgnoreCase(".dll", ".exe") || item.EndsWithIgnoreCase(".vshost.exe")) continue;
                if (_BakImages.Contains(item)) continue;

                if (loadeds.Any(e => e.Location.EqualIgnoreCase(item)) ||
                    loadeds2.Any(e => e.Location.EqualIgnoreCase(item))) continue;

#if !__MOBILE__ && !__CORE__
                var asm = ReflectionOnlyLoadFrom(item, ver);
                if (asm == null) continue;
#else

                var asm = (Assembly)null;
                try
                {
                    asm = Assembly.LoadFrom(item);
                }
                catch (BadImageFormatException)
                {
                    _BakImages.Add(item);
                    //XTrace.WriteLine(ex.ToString());
                }

                if (asm == null) continue;
#endif

                // 不搜索系统程序集，优化性能
                if (CheckSystem(asm)) continue;

                // 尽管目录不一样，但这两个可能是相同的程序集
                // 这里导致加载了不同目录的同一个程序集，然后导致对象容器频繁报错
                //if (loadeds.Any(e => e.Asm.FullName.EqualIgnoreCase(asm.FullName)) ||
                //    loadeds2.Any(e => e.Asm.FullName.EqualIgnoreCase(asm.FullName))) continue;
                // 相同程序集不同版本，全名不想等
                if (loadeds.Any(e => e.Asm.GetName().Name.EqualIgnoreCase(asm.GetName().Name)) ||
                    loadeds2.Any(e => e.Asm.GetName().Name.EqualIgnoreCase(asm.GetName().Name))) continue;

                var asmx = Create(asm);
                if (asmx != null) yield return asmx;
            }
        }

#if !__MOBILE__ && !__CORE__
        /// <summary>只反射加载指定路径的所有程序集</summary>
        /// <param name="file"></param>
        /// <param name="ver"></param>
        /// <returns></returns>
        public static Assembly ReflectionOnlyLoadFrom(String file, Version ver = null)
        {
            // 仅加载.Net文件，并且小于等于当前版本
            if (!PEImage.CanLoad(file, ver, XTrace.Debug)) return null;

            try
            {
                return Assembly.ReflectionOnlyLoadFrom(file);
            }
            catch { return null; }
        }
#endif

        /// <summary>获取当前应用程序的所有程序集，不包括系统程序集，仅限本目录</summary>
        /// <returns></returns>
        public static List<AssemblyX> GetMyAssemblies()
        {
            var list = new List<AssemblyX>();
            var hs = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            var cur = AppDomain.CurrentDomain.BaseDirectory;
            foreach (var asmx in GetAssemblies())
            {
                // 加载程序集列表很容易抛出异常，全部屏蔽
                try
                {
                    if (String.IsNullOrEmpty(asmx.FileVersion)) continue;
                    var file = asmx.Asm.CodeBase;
                    if (String.IsNullOrEmpty(file)) continue;
                    file = file.TrimStart("file:///");
                    file = file.Replace("/", "\\");
                    if (!file.StartsWithIgnoreCase(cur)) continue;

                    if (!hs.Contains(file))
                    {
                        hs.Add(file);
                        list.Add(asmx);
                    }
                }
                catch { }
            }
#if !__CORE__
            foreach (var asmx in ReflectionOnlyGetAssemblies())
            {
                // 加载程序集列表很容易抛出异常，全部屏蔽
                try
                {
                    if (String.IsNullOrEmpty(asmx.FileVersion)) continue;
                    var file = asmx.Asm.CodeBase;
                    if (String.IsNullOrEmpty(file)) continue;
                    file = file.TrimStart("file:///");
                    file = file.Replace("/", "\\");
                    if (!file.StartsWithIgnoreCase(cur)) continue;

                    if (!hs.Contains(file))
                    {
                        hs.Add(file);
                        list.Add(asmx);
                    }
                }
                catch { }
            }
#endif
            return list;
        }

        /// <summary>在对程序集的解析失败时发生</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static Assembly OnResolve(String name)
        {
            foreach (var item in GetAssemblies())
            {
                if (item.Asm.FullName == name) return item.Asm;
            }

            foreach (var item in ReflectionOnlyGetAssemblies())
            {
                if (item.Asm.FullName == name)
                {
                    // 只反射程序集需要真实加载
                    try
                    {
                        var asm = Assembly.LoadFile(item.Asm.Location);
                        if (asm != null) return asm;
                    }
                    catch (Exception ex) { XTrace.WriteException(ex); }

                    //return item.Asm;
                }
            }

            // 支持加载不同版本
            var p = name.IndexOf(", ");
            if (p > 0)
            {
                name = name.Substring(0, p);
                foreach (var item in GetAssemblies())
                {
                    if (item.Asm.GetName().Name == name) return item.Asm;
                }

                foreach (var item in ReflectionOnlyGetAssemblies())
                {
                    if (item.Asm.GetName().Name == name)
                    {
                        try
                        {
                            var asm = Assembly.LoadFile(item.Asm.Location);
                            if (asm != null) return asm;
                        }
                        catch (Exception ex) { XTrace.WriteException(ex); }
                    }
                }
            }

            return null;
        }
        #endregion

        #region 重载
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            if (!String.IsNullOrEmpty(Title))
                return Title;
            else
                return Name;
        }

        ///// <summary>判断两个程序集是否相同，避免引用加载和执行上下文加载的相同程序集显示不同</summary>
        ///// <param name="asm1"></param>
        ///// <param name="asm2"></param>
        ///// <returns></returns>
        //public static Boolean Equal(Assembly asm1, Assembly asm2)
        //{
        //    if (asm1 == asm2) return true;

        //    return asm1.FullName == asm2.FullName;
        //}
        #endregion
    }
}