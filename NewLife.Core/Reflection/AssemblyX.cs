using System;
using System.Collections.Generic;
using System.IO;
using NewLife.Linq;
using System.Reflection;
using System.Web;
using NewLife.Collections;

namespace NewLife.Reflection
{
    /// <summary>程序集辅助类。使用Create创建，保证每个程序集只有一个辅助类</summary>
    public class AssemblyX : FastIndexAccessor
    {
        #region 属性
        private Assembly _Asm;
        /// <summary>程序集</summary>
        public Assembly Asm
        {
            get { return _Asm; }
        }

        [NonSerialized]
        private List<String> hasLoaded = new List<String>();

        private String _Name;
        /// <summary>名称</summary>
        public String Name
        {
            get
            {
                if (String.IsNullOrEmpty(_Name) && !hasLoaded.Contains("Name"))
                {
                    hasLoaded.Add("Name");

                    _Name = Asm.GetName().Name;
                    //if (String.IsNullOrEmpty(_Name)) _Name = "未知";
                }
                return _Name;
            }
        }

        private String _Version;
        /// <summary>程序集版本</summary>
        public String Version
        {
            get
            {
                if (String.IsNullOrEmpty(_Version) && !hasLoaded.Contains("Version"))
                {
                    hasLoaded.Add("Version");

                    _Version = Asm.GetName().Version.ToString();
                    if (String.IsNullOrEmpty(_Version)) _Version = "1.0";
                }
                return _Version;
            }
        }

        private String _Title;
        /// <summary>程序集标题</summary>
        public String Title
        {
            get
            {
                if (String.IsNullOrEmpty(_Title) && !hasLoaded.Contains("Title"))
                {
                    hasLoaded.Add("Title");

                    _Title = GetCustomAttributeValue<AssemblyTitleAttribute, String>();
                }
                return _Title;
            }
        }

        private String _FileVersion;
        /// <summary>文件版本</summary>
        public String FileVersion
        {
            get
            {
                if (String.IsNullOrEmpty(_FileVersion) && !hasLoaded.Contains("FileVersion"))
                {
                    hasLoaded.Add("FileVersion");

                    _FileVersion = GetCustomAttributeValue<AssemblyFileVersionAttribute, String>();
                }
                return _FileVersion;
            }
        }

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
                        String[] ss = Version.Split(new Char[] { '.' });
                        Int32 d = Convert.ToInt32(ss[2]);
                        Int32 s = Convert.ToInt32(ss[3]);

                        DateTime dt = new DateTime(2000, 1, 1);
                        dt = dt.AddDays(d).AddSeconds(s * 2);

                        _Compile = dt;
                    }
                }
                return _Compile;
            }
        }

        private String _Company;
        /// <summary>公司名称</summary>
        public String Company
        {
            get
            {
                if (String.IsNullOrEmpty(_Company) && !hasLoaded.Contains("Company"))
                {
                    hasLoaded.Add("Company");

                    _Company = GetCustomAttributeValue<AssemblyCompanyAttribute, String>();
                }
                return _Company;
            }
        }

        private String _Description;
        /// <summary>说明</summary>
        public String Description
        {
            get
            {
                if (String.IsNullOrEmpty(_Description) && !hasLoaded.Contains("Description"))
                {
                    hasLoaded.Add("Description");

                    _Description = GetCustomAttributeValue<AssemblyDescriptionAttribute, String>();
                }
                return _Description;
            }
        }

        /// <summary>
        /// 获取包含清单的已加载文件的路径或 UNC 位置。
        /// </summary>
        public String Location
        {
            get
            {
                try
                {
                    return Asm == null ? null : Asm.Location;
                }
                catch { return null; }
            }
        }
        #endregion

        #region 构造
        private AssemblyX(Assembly asm)
        {
            _Asm = asm;
        }

        private static DictionaryCache<Assembly, AssemblyX> cache = new DictionaryCache<Assembly, AssemblyX>();
        /// <summary>
        /// 创建程序集辅助对象
        /// </summary>
        /// <param name="asm"></param>
        /// <returns></returns>
        public static AssemblyX Create(Assembly asm)
        {
            return cache.GetItem(asm, delegate(Assembly key)
            {
                return new AssemblyX(key);
            });
        }

        static AssemblyX()
        {
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += new ResolveEventHandler(CurrentDomain_ReflectionOnlyAssemblyResolve);
        }

        static Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            return Assembly.ReflectionOnlyLoad(args.Name);
        }
        #endregion

        #region 扩展属性
        //private IEnumerable<Type> _Types;
        /// <summary>类型集合，当前程序集的所有类型，包括私有和内嵌，非内嵌请直接调用Asm.GetTypes()</summary>
        public IEnumerable<Type> Types
        {
            get
            {
                //if (_Types == null)
                //{
                //    IEnumerable<Type> list = new ListX<Type>(Asm.GetTypes());
                //    for (int i = 0; i < list.Count; i++)
                //    {
                //        Type[] types = list[i].GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                //        if (types != null && types.Length > 0)
                //        {
                //            // 从下一个元素开始插入，让内嵌类紧挨着主类
                //            Int32 k = i + 1;
                //            foreach (Type item in types)
                //            {
                //                if (!list.Contains(item)) list.Insert(k++, item);
                //            }
                //        }
                //    }
                //    _Types = list;
                //}
                //return _Types == null ? null : _Types.Clone();

                Type[] ts = null;
                try
                {
                    ts = Asm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex) { ts = ex.Types; }
                if (ts == null || ts.Length < 1) yield break;

                // 先遍历一次ts，避免取内嵌类型带来不必要的性能损耗
                foreach (Type item in ts)
                {
                    if (item != null) yield return item;
                }

                List<Type> list = new List<Type>(ts);
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] == null) continue;

                    //yield return list[i];

                    Type[] ts2 = list[i].GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    if (ts2 != null && ts2.Length > 0)
                    {
                        // 从下一个元素开始插入，让内嵌类紧挨着主类
                        //Int32 k = i + 1;
                        foreach (Type item in ts2)
                        {
                            //if (!list.Contains(item)) list.Insert(k++, item);
                            // Insert将会导致大量的数组复制
                            list.Add(item);

                            yield return item;
                        }
                    }
                }
            }
        }

        //private IEnumerable<TypeX> _TypeXs;
        /// <summary>类型集合，当前程序集的所有类型</summary>
        public IEnumerable<TypeX> TypeXs
        {
            get
            {
                //if (_TypeXs == null)
                //    _TypeXs = ListX<TypeX>.From<Type>(Types, delegate(Type type) { return TypeX.Create(type); });
                //return _TypeXs == null ? null : _TypeXs.Clone();

                foreach (Type item in Types)
                {
                    yield return TypeX.Create(item);
                }
            }
        }

        /// <summary>
        /// 是否系统程序集
        /// </summary>
        public Boolean IsSystemAssembly
        {
            get
            {
                String name = Asm.FullName;
                if (name.EndsWith("PublicKeyToken=b77a5c561934e089")) return true;
                if (name.EndsWith("PublicKeyToken=b03f5f7f11d50a3a")) return true;

                return false;
            }
        }
        #endregion

        #region 获取特性
        /// <summary>
        /// 获取自定义属性
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <returns></returns>
        public TAttribute GetCustomAttribute<TAttribute>()
        {
            TAttribute[] avs = Asm.GetCustomAttributes(typeof(TAttribute), true) as TAttribute[];
            if (avs == null || avs.Length < 1) return default(TAttribute);

            return avs[0];
        }

        /// <summary>
        /// 获取自定义属性的值。可用于ReflectionOnly加载的程序集
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public TResult GetCustomAttributeValue<TAttribute, TResult>()
        {
            IList<CustomAttributeData> list = CustomAttributeData.GetCustomAttributes(Asm);
            if (list == null || list.Count < 1) return default(TResult);

            foreach (CustomAttributeData item in list)
            {
                if (typeof(TAttribute) != item.Constructor.DeclaringType) continue;

                if (item.ConstructorArguments != null && item.ConstructorArguments.Count > 0)
                    return (TResult)item.ConstructorArguments[0].Value;
            }

            return default(TResult);
        }
        #endregion

        #region 方法
        /// <summary>在程序集中查找类型</summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public Type GetType(String typeName)
        {
            Type type = Asm.GetType(typeName);
            if (type != null) return type;

            // 如果没有包含圆点，说明其不是FullName
            if (!typeName.Contains("."))
            {
                Type[] types = Asm.GetTypes();
                if (types != null && types.Length > 0)
                {
                    foreach (Type item in types)
                    {
                        if (item.Name == typeName) return item;
                    }
                }

                // 遍历所有类型，包括内嵌类型
                foreach (var item in TypeXs)
                {
                    if (item.Name == typeName) return item;
                }
            }

            return null;
        }
        #endregion

        #region 插件
        /// <summary>
        /// 查找插件
        /// </summary>
        /// <typeparam name="TPlugin"></typeparam>
        /// <returns></returns>
        public List<Type> FindPlugins<TPlugin>()
        {
            return FindPlugins(typeof(TPlugin));
        }

        private Dictionary<Type, List<Type>> _plugins = new Dictionary<Type, List<Type>>();
        /// <summary>
        /// 查找插件，带缓存
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public List<Type> FindPlugins(Type type)
        {
            // 如果type是null，则返回所有类型

            List<Type> list = null;
            if (_plugins.TryGetValue(type, out list)) return list;
            lock (_plugins)
            {
                if (_plugins.TryGetValue(type, out list)) return list;

                list = new List<Type>();
                foreach (Type item in Types)
                {
                    if (TypeX.Create(item).IsPlugin(type)) list.Add(item);
                }
                if (list.Count <= 0) list = null;

                _plugins.Add(type, list);

                return list;
            }
        }

        /// <summary>
        /// 查找所有非系统程序集中的所有插件
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEnumerable<Type> FindAllPlugins(Type type)
        {
            return FindAllPlugins(type, false);
        }

        /// <summary>
        /// 查找所有非系统程序集中的所有插件
        /// </summary>
        /// <remarks>继承类所在的程序集会引用baseType所在的程序集，利用这一点可以做一定程度的性能优化。</remarks>
        /// <param name="baseType"></param>
        /// <param name="isLoadAssembly">是否从未加载程序集中获取类型。使用仅反射的方法检查目标类型，如果存在，则进行常规加载</param>
        /// <returns></returns>
        public static IEnumerable<Type> FindAllPlugins(Type baseType, Boolean isLoadAssembly)
        {
            return FindAllPlugins(baseType, isLoadAssembly, true);
        }

        /// <summary>
        /// 查找所有非系统程序集中的所有插件
        /// </summary>
        /// <remarks>继承类所在的程序集会引用baseType所在的程序集，利用这一点可以做一定程度的性能优化。</remarks>
        /// <param name="baseType"></param>
        /// <param name="isLoadAssembly">是否从未加载程序集中获取类型。使用仅反射的方法检查目标类型，如果存在，则进行常规加载</param>
        /// <param name="excludeGlobalTypes">指示是否应检查来自所有引用程序集的类型。如果为 false，则检查来自所有引用程序集的类型。 否则，只检查来自非全局程序集缓存 (GAC) 引用的程序集的类型。</param>
        /// <returns></returns>
        internal static IEnumerable<Type> FindAllPlugins(Type baseType, Boolean isLoadAssembly, Boolean excludeGlobalTypes)
        {
            //if (baseType == null) throw new ArgumentNullException("baseType");

            String baseAssemblyName = baseType.Assembly.FullName;

            List<Type> list = new List<Type>();
            foreach (AssemblyX item in GetAssemblies())
            {
                // 如果excludeGlobalTypes为true，则指检查来自非GAC引用的程序集
                if (excludeGlobalTypes && item.Asm.GlobalAssemblyCache) continue;

                // 不搜索系统程序集，不搜索未引用基类所在程序集的程序集，优化性能
                if (item.IsSystemAssembly || !IsReferencedFrom(item.Asm, baseAssemblyName)) continue;

                List<Type> ts = item.FindPlugins(baseType);
                if (ts != null && ts.Count > 0)
                {
                    //list.AddRange(ts);
                    foreach (Type elm in ts)
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
                //// 尝试加载程序集
                //AssemblyX.ReflectionOnlyLoad();
                foreach (AssemblyX item in AssemblyX.ReflectionOnlyGetAssemblies())
                {
                    // 如果excludeGlobalTypes为true，则指检查来自非GAC引用的程序集
                    if (excludeGlobalTypes && item.Asm.GlobalAssemblyCache) continue;

                    // 不搜索系统程序集，不搜索未引用基类所在程序集的程序集，优化性能
                    if (item.IsSystemAssembly || !IsReferencedFrom(item.Asm, baseAssemblyName)) continue;

                    List<Type> ts = item.FindPlugins(baseType);
                    if (ts != null && ts.Count > 0)
                    {
                        // 真实加载
                        Assembly asm2 = Assembly.LoadFile(item.Asm.Location);
                        ts = AssemblyX.Create(asm2).FindPlugins(baseType);

                        if (ts != null && ts.Count > 0)
                        {
                            foreach (Type elm in ts)
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

        /// <summary>
        /// <paramref name="asm"/> 是否引用了 <paramref name="baseAsmName"/>
        /// </summary>
        /// <param name="asm">程序集</param>
        /// <param name="baseAsmName">被引用程序集全名</param>
        /// <returns></returns>
        public static Boolean IsReferencedFrom(Assembly asm, String baseAsmName)
        {
            if (asm.FullName.EqualIgnoreCase(baseAsmName)) return true;

            foreach (AssemblyName item in asm.GetReferencedAssemblies())
            {
                if (item.FullName.EqualIgnoreCase(baseAsmName)) return true;
            }

            return false;
        }
        #endregion

        #region 静态方法
        /// <summary>
        /// 获取指定程序域所有程序集的辅助类
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public static IEnumerable<AssemblyX> GetAssemblies(AppDomain domain)
        {
            if (domain == null) domain = AppDomain.CurrentDomain;

            Assembly[] asms = domain.GetAssemblies();
            if (asms == null || asms.Length < 1) return Enumerable.Empty<AssemblyX>();

            //List<AssemblyX> list = new List<AssemblyX>();
            //foreach (Assembly item in asms)
            //{
            //    list.Add(AssemblyX.Create(item));
            //}

            //return list;

            //return asms.Select(item => Create(item));
            return from e in asms select Create(e);

            //if (asms == null || asms.Length < 1) yield break;
            //foreach (Assembly item in asms)
            //{
            //    yield return Create(item);
            //}
        }

        /// <summary>
        /// 获取当前程序域所有程序集的辅助类
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<AssemblyX> GetAssemblies() { return GetAssemblies(AppDomain.CurrentDomain); }

        /// <summary>
        /// 只反射加载有效路径（应用程序是当前路径，Web是Bin目录）的所有程序集
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        [Obsolete("该成员在后续版本中将不再被支持！")]
        public static IEnumerable<AssemblyX> ReflectionOnlyGetAssemblies(AppDomain domain)
        {
            if (domain == null) domain = AppDomain.CurrentDomain;

            Assembly[] asms = domain.ReflectionOnlyGetAssemblies();
            if (asms == null || asms.Length < 1) return Enumerable.Empty<AssemblyX>();

            //List<AssemblyX> list = new List<AssemblyX>();
            //foreach (Assembly item in asms)
            //{
            //    list.Add(AssemblyX.Create(item));
            //}

            //return list;

            return asms.Select(item => Create(item));
        }

        /// <summary>
        /// 获取当前程序域所有程序集的辅助类
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<AssemblyX> ReflectionOnlyGetAssemblies()
        {
            //TODO: 将来这里可以改进为指定加载目录，也就是插件目录
            if (HttpRuntime.AppDomainId == null)
                return ReflectionOnlyLoad(AppDomain.CurrentDomain.BaseDirectory);
            else
                return ReflectionOnlyLoad(HttpRuntime.BinDirectory);
        }

        /// <summary>
        /// 只反射加载指定路径的所有程序集
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        static IEnumerable<AssemblyX> ReflectionOnlyLoad(String path)
        {
            if (!Directory.Exists(path)) yield break;

            // 先返回已加载的只加载程序集
            List<AssemblyX> loadeds2 = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies().Select(e => Create(e)).ToList();
            foreach (AssemblyX item in loadeds2)
            {
                yield return item;
            }

            // 再去遍历目录
            String[] ss = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
            if (ss == null || ss.Length < 1) yield break;

            List<AssemblyX> loadeds = AssemblyX.GetAssemblies().ToList<AssemblyX>();

            foreach (String item in ss)
            {
                if (!item.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                    !item.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) continue;

                if (loadeds.Any(e => e.Location.EqualIgnoreCase(item)) || loadeds2.Any(e => e.Location.EqualIgnoreCase(item))) continue;

                AssemblyX asmx = null;
                try
                {
                    //Assembly asm = Assembly.ReflectionOnlyLoad(File.ReadAllBytes(item));
                    Assembly asm = Assembly.ReflectionOnlyLoadFrom(item);

                    asmx = Create(asm);
                }
                catch { }

                if (asmx != null) yield return asmx;
            }
        }

        /// <summary>
        /// 只反射加载有效路径（应用程序是当前路径，Web是Bin目录）的所有程序集
        /// </summary>
        /// <returns></returns>
        [Obsolete("该成员在后续版本中将不再被支持！")]
        public static IEnumerable<AssemblyX> ReflectionOnlyLoad()
        {
            if (HttpRuntime.AppDomainId == null)
                return ReflectionOnlyLoad(AppDomain.CurrentDomain.BaseDirectory);
            else
                return ReflectionOnlyLoad(HttpRuntime.BinDirectory);
        }
        #endregion

        #region 重载
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            //return String.Format("{0} {1}", Name, Title);
            if (!String.IsNullOrEmpty(Title))
                return Title;
            else
                return Name;
        }

        /// <summary>
        /// 判断两个程序集是否相同，避免引用加载和执行上下文加载的相同程序集显示不同
        /// </summary>
        /// <param name="asm1"></param>
        /// <param name="asm2"></param>
        /// <returns></returns>
        public static Boolean Equal(Assembly asm1, Assembly asm2)
        {
            if (asm1 == asm2) return true;

            return asm1.FullName == asm2.FullName;
        }
        #endregion
    }
}