using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using NewLife.Collections;
using System.IO;
using System.Web;

namespace NewLife.Reflection
{
    /// <summary>
    /// 程序集辅助类。使用Create创建，保证每个程序集只有一个辅助类
    /// </summary>
    public class AssemblyX : FastIndexAccessor
    {
        #region 属性
        private Assembly _Asm;
        /// <summary>程序集</summary>
        public Assembly Asm
        {
            get { return _Asm; }
            //set { _Asm = value; }
        }

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
            //set { _Name = value; }
        }

        private String _Version;
        /// <summary>
        /// 程序集版本
        /// </summary>
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

                    //AssemblyTitleAttribute av = Attribute.GetCustomAttribute(Asm, typeof(AssemblyTitleAttribute)) as AssemblyTitleAttribute;
                    //AssemblyTitleAttribute av = GetCustomAttribute<AssemblyTitleAttribute>();
                    //if (av != null) _Title = av.Title;
                    _Title = GetCustomAttributeValue<AssemblyTitleAttribute, String>();
                }
                return _Title;
            }
            //set { _Title = value; }
        }

        private String _FileVersion;
        /// <summary>
        /// 文件版本
        /// </summary>
        public String FileVersion
        {
            get
            {
                if (String.IsNullOrEmpty(_FileVersion) && !hasLoaded.Contains("FileVersion"))
                {
                    hasLoaded.Add("FileVersion");

                    //AssemblyFileVersionAttribute av = Attribute.GetCustomAttribute(Asm, typeof(AssemblyFileVersionAttribute)) as AssemblyFileVersionAttribute;
                    //if (av != null) _FileVersion = av.Version;
                    //AssemblyFileVersionAttribute av = GetCustomAttribute<AssemblyFileVersionAttribute>();
                    //if (av != null) _FileVersion = av.Version;
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

                    //AssemblyCompanyAttribute av = Attribute.GetCustomAttribute(Asm, typeof(AssemblyCompanyAttribute)) as AssemblyCompanyAttribute;
                    //if (av != null) _Company = av.Company;
                    //AssemblyCompanyAttribute av = GetCustomAttribute<AssemblyCompanyAttribute>();
                    //if (av != null) _Company = av.Company;
                    _Company = GetCustomAttributeValue<AssemblyCompanyAttribute, String>();
                }
                return _Company;
            }
            set { _Company = value; }
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

                    //AssemblyDescriptionAttribute av = Attribute.GetCustomAttribute(Asm, typeof(AssemblyDescriptionAttribute)) as AssemblyDescriptionAttribute;
                    //if (av != null) _Description = av.Description;
                    //AssemblyDescriptionAttribute av = GetCustomAttribute<AssemblyDescriptionAttribute>();
                    //if (av != null) _Description = av.Description;
                    _Description = GetCustomAttributeValue<AssemblyDescriptionAttribute, String>();
                }
                return _Description;
            }
            //set { _Description = value; }
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
        private ListX<Type> _Types;
        /// <summary>类型集合，当前程序集的所有类型</summary>
        public ListX<Type> Types
        {
            get
            {
                //if (_Types == null) _Types = ListX<TypeX>.From(new ListX<Type>(Asm.GetTypes()));
                if (_Types == null) _Types = new ListX<Type>(Asm.GetTypes());
                return _Types == null ? null : _Types.Clone();
            }
        }

        private ListX<TypeX> _TypeXs;
        /// <summary>类型集合，当前程序集的所有类型</summary>
        public ListX<TypeX> TypeXs
        {
            get
            {
                if (_TypeXs == null)
                    _TypeXs = ListX<TypeX>.From<Type>(Types, delegate(Type type) { return TypeX.Create(type); });
                return _TypeXs == null ? null : _TypeXs.Clone();
            }
        }

        /// <summary>
        /// 是否系统程序集
        /// </summary>
        public Boolean IsSystemAssembly
        {
            get
            {
                return Asm.FullName.EndsWith("PublicKeyToken=b77a5c561934e089");
            }
        }

        private static Int32 Add(Int32 x, Int32 y)
        {
            return x + y;
        }

        private String Change(String str)
        {
            return _Title = str;
        }
        #endregion

        #region 方法
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
            //AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += new ResolveEventHandler(CurrentDomain_ReflectionOnlyAssemblyResolve);

            //try
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
            //finally
            //{
            //    AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= new ResolveEventHandler(CurrentDomain_ReflectionOnlyAssemblyResolve);
            //}
        }

        /// <summary>
        /// 查找插件
        /// </summary>
        /// <typeparam name="TPlugin"></typeparam>
        /// <returns></returns>
        public ListX<Type> FindPlugins<TPlugin>()
        {
            return FindPlugins(typeof(TPlugin));
        }

        private Dictionary<Type, ListX<Type>> _plugins = new Dictionary<Type, ListX<Type>>();
        /// <summary>
        /// 查找插件，带缓存
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public ListX<Type> FindPlugins(Type type)
        {
            ListX<Type> list = null;
            if (_plugins.TryGetValue(type, out list)) return list;
            lock (_plugins)
            {
                if (_plugins.TryGetValue(type, out list)) return list;

                ListX<TypeX> list2 = TypeXs;
                if (list2 == null || list2.Count < 1)
                {
                    list = null;
                }
                else
                {
                    list = new ListX<Type>();
                    foreach (TypeX item in list2)
                    {
                        if (item.IsPlugin(type)) list.Add(item.Type);
                    }
                    if (list.Count <= 0) list = null;
                }

                _plugins.Add(type, list);

                return list;
            }
        }

        /// <summary>
        /// 查找所有非系统程序集中的所有插件
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ListX<Type> FindAllPlugins(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            ListX<AssemblyX> asms = GetAssemblies();
            ListX<Type> list = new ListX<Type>();
            foreach (AssemblyX item in asms)
            {
                if (item.IsSystemAssembly) continue;

                ListX<Type> ts = item.FindPlugins(type);
                if (ts != null && ts.Count > 0) list.AddRange(ts);
            }
            return list.Count > 0 ? list : null;
        }
        #endregion

        #region 静态方法
        /// <summary>
        /// 获取指定程序域所有程序集的辅助类
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public static ListX<AssemblyX> GetAssemblies(AppDomain domain)
        {
            if (domain == null) domain = AppDomain.CurrentDomain;

            Assembly[] asms = domain.GetAssemblies();
            if (asms == null || asms.Length < 1) return null;

            ListX<AssemblyX> list = new ListX<AssemblyX>();
            foreach (Assembly item in asms)
            {
                list.Add(AssemblyX.Create(item));
            }

            return list;
        }

        /// <summary>
        /// 获取当前程序域所有程序集的辅助类
        /// </summary>
        /// <returns></returns>
        public static ListX<AssemblyX> GetAssemblies() { return GetAssemblies(AppDomain.CurrentDomain); }

        /// <summary>
        /// 只反射加载有效路径（应用程序是当前路径，Web是Bin目录）的所有程序集
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public static ListX<AssemblyX> ReflectionOnlyGetAssemblies(AppDomain domain)
        {
            if (domain == null) domain = AppDomain.CurrentDomain;

            Assembly[] asms = domain.ReflectionOnlyGetAssemblies();
            if (asms == null || asms.Length < 1) return null;

            ListX<AssemblyX> list = new ListX<AssemblyX>();
            foreach (Assembly item in asms)
            {
                list.Add(AssemblyX.Create(item));
            }

            return list;
        }

        /// <summary>
        /// 获取当前程序域所有程序集的辅助类
        /// </summary>
        /// <returns></returns>
        public static ListX<AssemblyX> ReflectionOnlyGetAssemblies() { return ReflectionOnlyGetAssemblies(AppDomain.CurrentDomain); }

        /// <summary>
        /// 只反射加载指定路径的所有程序集
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static ListX<AssemblyX> ReflectionOnlyLoad(String path)
        {
            if (!Directory.Exists(path)) return null;

            String[] ss = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
            if (ss == null || ss.Length < 1) return null;

            //AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += new ResolveEventHandler(CurrentDomain_ReflectionOnlyAssemblyResolve);

            //try
            {
                ListX<AssemblyX> loadeds = AssemblyX.GetAssemblies();
                ListX<AssemblyX> loadeds2 = AssemblyX.ReflectionOnlyGetAssemblies();

                ListX<AssemblyX> list = new ListX<AssemblyX>();
                foreach (String item in ss)
                {
                    if (!item.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                        !item.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) continue;

                    if (loadeds != null && loadeds.Exists(delegate(AssemblyX elm)
                    {
                        return item.Equals(elm.Location, StringComparison.OrdinalIgnoreCase);
                    })) continue;

                    if (loadeds2 != null && loadeds2.Exists(delegate(AssemblyX elm)
                    {
                        return item.Equals(elm.Location, StringComparison.OrdinalIgnoreCase);
                    })) continue;

                    try
                    {
                        //Assembly asm = Assembly.ReflectionOnlyLoad(File.ReadAllBytes(item));
                        Assembly asm = Assembly.ReflectionOnlyLoadFrom(item);

                        list.Add(AssemblyX.Create(asm));
                    }
                    catch { }
                }

                return list;
            }
            //finally
            //{
            //    AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= new ResolveEventHandler(CurrentDomain_ReflectionOnlyAssemblyResolve);
            //}
        }

        /// <summary>
        /// 只反射加载有效路径（应用程序是当前路径，Web是Bin目录）的所有程序集
        /// </summary>
        /// <returns></returns>
        public static ListX<AssemblyX> ReflectionOnlyLoad()
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