using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using NewLife.Collections;
using NewLife.Log;

namespace NewLife.Reflection;

/// <summary>程序集辅助类。使用Create创建，保证每个程序集只有一个辅助类</summary>
public class AssemblyX
{
    #region 属性
    /// <summary>程序集</summary>
    public Assembly Asm { get; }

    private String _Name;
    /// <summary>名称</summary>
    public String Name => _Name ??= "" + Asm.GetName().Name;

    private String _Version;
    /// <summary>程序集版本</summary>
    public String Version => _Version ??= "" + Asm.GetName().Version;

    private String _Title;
    /// <summary>程序集标题</summary>
    public String Title => _Title ??= "" + Asm.GetCustomAttributeValue<AssemblyTitleAttribute, String>();

    private String _FileVersion;
    /// <summary>文件版本</summary>
    public String FileVersion
    {
        get
        {
            if (_FileVersion == null)
            {
                var ver = Asm.GetCustomAttributeValue<AssemblyInformationalVersionAttribute, String>();
                if (!ver.IsNullOrEmpty())
                {
                    var p = ver.IndexOf('+');
                    if (p > 0) ver = ver[..p];
                }
                _FileVersion = ver;
            }

            if (_FileVersion == null) _FileVersion = Asm.GetCustomAttributeValue<AssemblyFileVersionAttribute, String>();

            if (_FileVersion == null) _FileVersion = "";

            return _FileVersion;
        }
    }

    private DateTime? _Compile;
    /// <summary>编译时间</summary>
    public DateTime Compile
    {
        get
        {
            if (_Compile == null)
            {
                var time = GetCompileTime(Version);
                if (time == time.Date && FileVersion.Contains("-beta")) time = GetCompileTime(FileVersion);

                _Compile = time;
            }
            return _Compile.Value;
        }
    }

    private String _Company;
    /// <summary>公司名称</summary>
    public String Company => _Company ??= "" + Asm.GetCustomAttributeValue<AssemblyCompanyAttribute, String>();

    private String _Description;
    /// <summary>说明</summary>
    public String Description => _Description ??= "" + Asm.GetCustomAttributeValue<AssemblyDescriptionAttribute, String>();

    /// <summary>获取包含清单的已加载文件的路径或 UNC 位置。</summary>
    public String Location
    {
        get
        {
            try
            {
                return Asm == null || Asm.IsDynamic ? null : Asm.Location;
            }
            catch { return null; }
        }
    }
    #endregion

    #region 构造
    private AssemblyX(Assembly asm) => Asm = asm;

    private static readonly ConcurrentDictionary<Assembly, AssemblyX> cache = new();
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
        //AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += OnReflectionOnlyAssemblyResolve;
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
    }

    private static Assembly OnAssemblyResolve(Object sender, ResolveEventArgs args)
    {
        var flag = XTrace.Log.Level <= LogLevel.Debug;
        if (flag) XTrace.WriteLine("[{0}]请求加载[{1}]", args.RequestingAssembly?.FullName, args.Name);
        //if (!flag) return null;

        try
        {
            // 尝试在请求者所在目录加载
            var file = args.RequestingAssembly?.Location;
            if (!file.IsNullOrEmpty())
            {
                var name = args.Name;
                var p = name.IndexOf(',');
                if (p > 0) name = name[..p];

                file = Path.GetDirectoryName(file).CombinePath(name + ".dll");
                if (File.Exists(file)) return Assembly.LoadFrom(file);
            }

            // 辅助解析程序集。程序集加载过程中，被依赖程序集未能解析时，是否协助解析，默认false
            if (Setting.Current.AssemblyResolve)
                return OnResolve(args.Name);
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
        }

        return null;
    }
    #endregion

    #region 扩展属性
    //private IEnumerable<Type> _Types;
    /// <summary>类型集合，当前程序集的所有类型，包括私有和内嵌，非内嵌请直接调用Asm.GetTypes()</summary>
    public IEnumerable<Type> Types
    {
        get
        {
            Type[] ts;
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
            if (ts == null || ts.Length <= 0) yield break;

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
    public static AssemblyX Entry { get; set; } = Create(Assembly.GetEntryAssembly());

    /// <summary>
    /// 加载过滤器，如果返回 false 表示跳过加载。
    /// </summary>
    public static Func<String, Boolean> ResolveFilter { get; set; }
    #endregion

    #region 方法
    private readonly ConcurrentDictionary<String, Type> typeCache2 = new();
    /// <summary>从程序集中查找指定名称的类型</summary>
    /// <param name="typeName"></param>
    /// <returns></returns>
    public Type GetType(String typeName)
    {
        if (String.IsNullOrEmpty(typeName)) throw new ArgumentNullException(nameof(typeName));

        return typeCache2.GetOrAdd(typeName, GetTypeInternal);
    }

    /// <summary>在程序集中查找类型</summary>
    /// <param name="typeName"></param>
    /// <returns></returns>
    private Type GetTypeInternal(String typeName)
    {
        var type = Asm.GetType(typeName);
        if (type != null) return type;

        // 如果没有包含圆点，说明其不是FullName
        if (!typeName.Contains('.'))
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
    private readonly ConcurrentDictionary<Type, List<Type>> _plugins = new();
    /// <summary>查找插件，带缓存</summary>
    /// <param name="baseType">类型</param>
    /// <returns></returns>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public IList<Type> FindPlugins(Type baseType)
    {
        // 如果type是null，则返回所有类型
        if (_plugins.TryGetValue(baseType, out var list)) return list;

        list = new List<Type>();
        try
        {
            foreach (var item in Asm.GetTypes())
            {
                if (item.IsInterface || item.IsAbstract || item.IsGenericType) continue;
                if (item != baseType && item.As(baseType)) list.Add(item);
            }
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
        }

        _plugins.TryAdd(baseType, list);

        return list;
    }

    /// <summary>查找所有非系统程序集中的所有插件</summary>
    /// <remarks>继承类所在的程序集会引用baseType所在的程序集，利用这一点可以做一定程度的性能优化。</remarks>
    /// <param name="baseType"></param>
    /// <param name="isLoadAssembly">是否从未加载程序集中获取类型。使用仅反射的方法检查目标类型，如果存在，则进行常规加载</param>
    /// <param name="excludeGlobalTypes">指示是否应检查来自所有引用程序集的类型。如果为 false，则检查来自所有引用程序集的类型。 否则，只检查来自非全局程序集缓存 (GAC) 引用的程序集的类型。</param>
    /// <returns></returns>
    public static IEnumerable<Type> FindAllPlugins(Type baseType, Boolean isLoadAssembly = false, Boolean excludeGlobalTypes = true)
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

            //// 如果excludeGlobalTypes为true，则指检查来自非GAC引用的程序集
            //if (excludeGlobalTypes && item.Asm.GlobalAssemblyCache) continue;

            // 不搜索系统程序集，不搜索未引用基类所在程序集的程序集，优化性能
            if (item.IsSystemAssembly || !IsReferencedFrom(item.Asm, baseAssemblyName)) continue;

            var ts = item.FindPlugins(baseType);
            foreach (var elm in ts)
            {
                if (!list.Contains(elm))
                {
                    list.Add(elm);
                    yield return elm;
                }
            }
        }
        if (isLoadAssembly)
        {
            foreach (var item in ReflectionOnlyGetAssemblies())
            {
                //// 如果excludeGlobalTypes为true，则指检查来自非GAC引用的程序集
                //if (excludeGlobalTypes && item.Asm.GlobalAssemblyCache) continue;

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
                        var root = AppDomain.CurrentDomain.BaseDirectory;
                        if (file.StartsWithIgnoreCase(root)) file = file.Substring(root.Length).TrimStart("\\");
                        XTrace.WriteLine("AssemblyX.FindAllPlugins(\"{0}\") => {1}", baseType.FullName, file);
                    }
                    var asm2 = Assembly.LoadFrom(item.Asm.Location);
                    ts = Create(asm2).FindPlugins(baseType);

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

    /// <summary><paramref name="asm"/> 是否引用了 <paramref name="baseAsmName"/></summary>
    /// <param name="asm">程序集</param>
    /// <param name="baseAsmName">被引用程序集全名</param>
    /// <returns></returns>
    private static Boolean IsReferencedFrom(Assembly asm, String baseAsmName)
    {
        if (asm.GetName().Name.EqualIgnoreCase(baseAsmName)) return true;

        foreach (var item in asm.GetReferencedAssemblies())
        {
            if (item.Name.EqualIgnoreCase(baseAsmName)) return true;
        }

        return false;
    }

    /// <summary>根据名称获取类型</summary>
    /// <param name="typeName">类型名</param>
    /// <param name="isLoadAssembly">是否从未加载程序集中获取类型。使用仅反射的方法检查目标类型，如果存在，则进行常规加载</param>
    /// <returns></returns>
    public static Type GetType(String typeName, Boolean isLoadAssembly)
    {
        var type = Type.GetType(typeName);
        if (type != null) return type;

        // 数组
        if (typeName.EndsWith("[]"))
        {
            var elemType = GetType(typeName[0..^2], isLoadAssembly);
            if (elemType == null) return null;

            return elemType.MakeArrayType();
        }

        // 加速基础类型识别，忽略大小写
        if (!typeName.Contains('.'))
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
                    var asm2 = Assembly.LoadFrom(file);
                    var type2 = Create(asm2).GetType(typeName);
                    if (type2 == null) continue;

                    type = type2;
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
        if (asms == null || asms.Length <= 0) return Enumerable.Empty<AssemblyX>();

        return asms.Select(item => Create(item));
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
                if (!basedir.IsNullOrEmpty()) set.Add(basedir);

                var cfg = Setting.Current;
                if (!cfg.PluginPath.IsNullOrEmpty())
                {
                    var plugin = cfg.PluginPath.GetFullPath();
                    if (!set.Contains(plugin)) set.Add(plugin);

                    plugin = cfg.PluginPath.GetBasePath();
                    if (!set.Contains(plugin)) set.Add(plugin);
                }

                _AssemblyPaths = set;
            }
            return _AssemblyPaths;
        }
        set => _AssemblyPaths = value;
    }

    /// <summary>获取当前程序域所有只反射程序集的辅助类。NETCore不支持只反射加载，该方法动态加载DLL后返回</summary>
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

    private static readonly ConcurrentHashSet<String> _BakImages = new();
    /// <summary>只反射加载指定路径的所有程序集。NETCore不支持只反射加载，该方法动态加载DLL后返回</summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static IEnumerable<AssemblyX> ReflectionOnlyLoad(String path)
    {
        if (!Directory.Exists(path)) yield break;

        // 先返回已加载的只加载程序集
        var loadeds2 = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies().Select(e => Create(e)).ToList();

        // 再去遍历目录
        var ss = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
        if (ss == null || ss.Length <= 0) yield break;

        var loadeds = GetAssemblies().ToList();

        var ver = new Version(Assembly.GetExecutingAssembly().ImageRuntimeVersion.TrimStart('v'));

        foreach (var item in ss)
        {
            // 仅尝试加载dll
            if (!item.EndsWithIgnoreCase(".dll")) continue;
            if (_BakImages.Contains(item)) continue;

            if (loadeds.Any(e => e.Location.EqualIgnoreCase(item)) ||
                loadeds2.Any(e => e.Location.EqualIgnoreCase(item))) continue;

            Assembly asm = null;
            try
            {
                asm = Assembly.LoadFrom(item);
            }
            catch
            {
                _BakImages.TryAdd(item);
            }
            if (asm == null) continue;

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
                if (asmx.FileVersion.IsNullOrEmpty()) continue;

                var file = "";
                //file = asmx.Asm.CodeBase;
                if (file.IsNullOrEmpty()) file = asmx.Asm.Location;
                if (file.IsNullOrEmpty()) continue;

                if (file.StartsWith("file:///"))
                {
                    file = file.TrimStart("file:///");
                    if (Path.DirectorySeparatorChar == '\\')
                        file = file.Replace('/', '\\');
                    else
                        file = file.Replace('\\', '/').EnsureStart("/");
                }
                if (!file.StartsWithIgnoreCase(cur)) continue;

                if (!hs.Contains(file))
                {
                    hs.Add(file);
                    list.Add(asmx);
                }
            }
            catch { }
        }
        return list;
    }

    /// <summary>在对程序集的解析失败时发生</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private static Assembly OnResolve(String name)
    {
        foreach (var item in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (item.FullName == name) return item;
        }

        // 支持加载不同版本
        var p = name.IndexOf(',');
        if (p > 0)
        {
            name = name[..p];
            foreach (var item in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (item.GetName().Name == name) return item;
            }

            // 查找文件并加载
            foreach (var item in AssemblyPaths)
            {
                var file = item.CombinePath(name + ".dll");
                if (File.Exists(file))
                {
                    try
                    {
                        var asm = Assembly.LoadFrom(file);
                        //var asm = Assembly.Load(File.ReadAllBytes(file));
                        if (asm != null && asm.GetName().Name == name) return asm;
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteException(ex);
                    }
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

    #region 辅助
    /// <summary>根据版本号计算得到编译时间</summary>
    /// <param name="version"></param>
    /// <returns></returns>
    public static DateTime GetCompileTime(String version)
    {
        var ss = version?.Split(new Char[] { '.' });
        if (ss == null || ss.Length < 4) return DateTime.MinValue;

        var d = ss[2].ToInt();
        var s = ss[3].ToInt();
        var y = DateTime.Today.Year;

        // 指定年月日的版本格式 1.0.yyyy.mmdd-betaHHMM
        if (d <= y && d >= y - 10)
        {
            var dt = new DateTime(d, 1, 1);
            if (s > 0)
            {
                if (s >= 200) dt = dt.AddMonths(s / 100 - 1);
                s %= 100;
                if (s > 1) dt = dt.AddDays(s - 1);
            }
            else
            {
                var str = ss[3];
                var p = str.IndexOf('-');
                if (p > 0)
                {
                    s = str[..p].ToInt();
                    if (s > 0)
                    {
                        if (s >= 200) dt = dt.AddMonths(s / 100 - 1);
                        s %= 100;
                        if (s > 1) dt = dt.AddDays(s - 1);
                    }

                    if (str.Length >= 4 + 1 + 4)
                    {
                        s = str[^4..].ToInt();
                        if (s > 0) dt = dt.AddHours(s / 100).AddMinutes(s % 100).ToLocalTime();
                    }
                }
            }

            return dt;
        }
        else
        {
            var dt = new DateTime(2000, 1, 1);
            dt = dt.AddDays(d).AddSeconds(s * 2);

            return dt;
        }
    }
    #endregion
}
