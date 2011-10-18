using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using NewLife.Exceptions;
using NewLife.Reflection;
using System.Collections.Specialized;
using NewLife.Configuration;
using NewLife.Extension;

namespace NewLife.Model
{
    //TODO: 似乎无法在Xml注册中做到先获取内部类型再注册外部类型，因为被注册代码无法被马上执行，而内部实现马上就要被覆盖了。
    // 可以用Xml注册普通类型，那样的话，内部类型注册的时候，就需要检查是否已注册

    /// <summary>实现 <seealso cref="IObjectContainer"/> 接口的对象容器</summary>
    /// <remarks>
    /// 1，如果容器里面没有这个类型，则返回空；
    /// 2，如果容器里面包含这个类型，并且指向的实例不为空，则返回，单例；
    /// 3，如果容器里面包含这个类型，并且指向的实例为空，则创建对象返回，多实例；
    /// 4，如果有带参数构造函数，则从容器内获取各个参数的实例，最后创建对象返回。
    /// 
    /// 这里有一点跟我们以往的想法非常不同，我们都习惯没有对象的时候，创建并加入字典。
    /// 这里采用两种方式，注册类型的时候，如果指定了实例，则表示这个类型对应单一的实例；
    /// 如果不指定实例，则表示支持该类型，每次创建。
    /// </remarks>
    public class ObjectContainer : IObjectContainer
    {
        #region 当前静态对象容器
        private static IObjectContainer _Current;
        /// <summary>当前容器</summary>
        public static IObjectContainer Current
        {
            get { return _Current ?? (_Current = new ObjectContainer()); }
            set { _Current = value; }
        }
        #endregion

        #region 父容器
        //private IObjectContainer _Parent;
        ///// <summary>父容器</summary>
        //public virtual IObjectContainer Parent
        //{
        //    get { return _Parent; }
        //    protected set { _Parent = value; }
        //}

        //private List<IObjectContainer> _Childs;
        ///// <summary>子容器</summary>
        //protected virtual IList<IObjectContainer> Childs
        //{
        //    get { return _Childs ?? (_Childs = new List<IObjectContainer>()); }
        //}

        ///// <summary>
        ///// 移除所有子容器
        ///// </summary>
        ///// <returns></returns>
        //public virtual IObjectContainer RemoveAllChildContainers()
        //{
        //    if (_Childs != null) _Childs.Clear();

        //    return this;
        //}

        ///// <summary>
        ///// 创建子容器
        ///// </summary>
        ///// <returns></returns>
        //public virtual IObjectContainer CreateChildContainer()
        //{
        //    IObjectContainer container = TypeX.CreateInstance(this.GetType()) as IObjectContainer;
        //    if (container is ObjectContaner)
        //        (container as ObjectContaner).Parent = this;
        //    else
        //    {
        //        PropertyInfo pi = this.GetType().GetProperty("Parent", BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.NonPublic);
        //        if (pi != null && pi.CanWrite) pi.SetValue(container, this, null);

        //    }
        //    Childs.Add(container);

        //    return container;
        //}
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化一个对象容器实例，自动从配置文件中加载注册
        /// </summary>
        public ObjectContainer()
        {
            LoadConfig();
        }
        #endregion

        #region 对象字典
        private IDictionary<Type, IDictionary<String, IObjectMap>> _stores = null;
        private IDictionary<Type, IDictionary<String, IObjectMap>> Stores
        {
            get
            {
                return _stores ?? (_stores = new Dictionary<Type, IDictionary<String, IObjectMap>>());
            }
        }

        private IDictionary<String, IObjectMap> Find(Type type, Boolean add)
        {
            IDictionary<String, IObjectMap> dic = null;
            if (Stores.TryGetValue(type, out dic)) return dic;

            if (add)
            {
                lock (Stores)
                {
                    if (Stores.TryGetValue(type, out dic)) return dic;

                    dic = new Dictionary<String, IObjectMap>();
                    Stores.Add(type, dic);
                    return dic;
                }
            }

            return null;
        }

        private IObjectMap FindMap(IDictionary<String, IObjectMap> dic, String name)
        {
            IObjectMap map = null;
            // 如果找到，直接返回
            if (dic.TryGetValue(name, out map)) return map;

            if (!String.IsNullOrEmpty(name))
            {
                // 如果名称不为空，则试一试找空的
                if (dic.TryGetValue(String.Empty, out map)) return map;
            }
            else
            {
                // 如果名称为空，找第一个
                foreach (IObjectMap item in dic.Values)
                {
                    return item;
                }
            }
            return null;
        }

        class Map : IObjectMap
        {
            #region 属性
            private String _Name;
            /// <summary>名称</summary>
            public String Name
            {
                get { return _Name; }
                set { _Name = value; }
            }

            private String _TypeName;
            /// <summary>类型名</summary>
            public String TypeName
            {
                get { return _TypeName; }
                set { _TypeName = value; }
            }

            private Type _ImplementType;
            /// <summary>实现类型</summary>
            public Type ImplementType
            {
                get
                {
                    if (_ImplementType == null && !TypeName.IsNullOrWhiteSpace()) _ImplementType = TypeX.GetType(TypeName, true);
                    return _ImplementType;
                }
                set { _ImplementType = value; }
            }

            private Boolean hasCheck = false;

            private Object _Instance;
            /// <summary>实例</summary>
            public Object Instance
            {
                get
                {
                    if (_Instance != null || hasCheck) return _Instance;

                    // 如果模式指定使用实例，而实例又为空，则初始化一个实例
                    if ((Mode & ModeFlags.Singleton) != ModeFlags.Singleton) return _Instance;

                    hasCheck = true;
                    try
                    {
                        if (ImplementType != null) _Instance = TypeX.CreateInstance(ImplementType);
                    }
                    catch { }

                    return _Instance;
                }
                set
                {
                    _Instance = value;
                    if (value != null) ImplementType = value.GetType();
                }
            }

            private ModeFlags _Mode;
            /// <summary>模式</summary>
            public ModeFlags Mode
            {
                get { return _Mode; }
                set { _Mode = value; }
            }
            #endregion

            #region 方法
            public override string ToString()
            {
                return String.Format("[{0},{1}]", Name, ImplementType != null ? ImplementType.Name : null);
            }
            #endregion
        }

        /// <summary>
        /// 模式标记
        /// </summary>
        [Flags]
        enum ModeFlags
        {
            None = 0,

            /// <summary>
            /// 以单例模式注册，如果注册的是类型，则new一个实例
            /// </summary>
            Singleton = 1,

            /// <summary>
            /// 是否覆盖已有的注册
            /// </summary>
            Overwrite = 2,

            /// <summary>
            /// 是否扩展，扩展注册将附加在该接口的第一个注册项之后
            /// </summary>
            Extend = 4
        }
        #endregion

        #region 注册核心
        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <param name="to">实现类型</param>
        /// <param name="instance">实例</param>
        /// <param name="name">名称</param>
        /// <param name="overwrite">是否覆盖</param>
        /// <returns></returns>
        public virtual IObjectContainer Register(Type from, Type to, Object instance, String name = null, Boolean overwrite = true)
        {
            //if(to==null&&instance==null)
            return Register(from, to, instance, null, ModeFlags.None, name, overwrite);
        }

        private IObjectContainer Register(Type from, Type to, Object instance, String typeName, ModeFlags mode, String name, Boolean overwrite)
        {
            if (from == null) throw new ArgumentNullException("from");
            // 名称不能是null，否则字典里面会报错
            if (name == null) name = String.Empty;

            IDictionary<String, IObjectMap> dic = Find(from, true);
            // 删除已有的
            //if (dic.ContainsKey(name)) dic.Remove(name);
            IObjectMap old = null;
            Map map = null;
            if (dic.TryGetValue(name, out old))
            {
                // 是否允许覆盖
                if (!overwrite) return this;

                //if (OnRegistering != null) OnRegistering(this, new EventArgs<Type, IObjectMap>(from, old));

                if (old is Map)
                {
                    map = old as Map;
                    map.TypeName = typeName;
                    map.Mode = mode;
                    map.ImplementType = to;
                    map.Instance = instance;

                    return this;
                }
                else
                    dic.Remove(name);
            }

            map = new Map();
            map.Name = name;
            map.TypeName = typeName;
            map.Mode = mode;
            map.ImplementType = to;
            map.Instance = instance;
            if (!dic.ContainsKey(name))
            {
                if (OnRegistering != null) OnRegistering(this, new EventArgs<Type, IObjectMap>(from, map));
                dic.Add(name, map);
                if (OnRegistered != null) OnRegistered(this, new EventArgs<Type, IObjectMap>(from, map));
            }

            return this;
        }

        /// <summary>注册前事件</summary>
        public event EventHandler<EventArgs<Type, IObjectMap>> OnRegistering;

        /// <summary>注册后事件</summary>
        public event EventHandler<EventArgs<Type, IObjectMap>> OnRegistered;
        #endregion

        #region 注册
        ///// <summary>
        ///// 注册类型
        ///// </summary>
        ///// <param name="from">接口类型</param>
        ///// <param name="to">实现类型</param>
        ///// <param name="name">名称</param>
        ///// <param name="overwrite">是否覆盖</param>
        ///// <returns></returns>
        //public virtual IObjectContainer Register(Type from, Type to, String name = null, Boolean overwrite = true) { return Register(from, to, null, name, overwrite); }

        ///// <summary>
        ///// 注册类型和名称
        ///// </summary>
        ///// <param name="from">接口类型</param>
        ///// <param name="to">实现类型</param>
        ///// <param name="name">名称</param>
        ///// <returns></returns>
        //public virtual IObjectContainer Register(Type from, Type to, String name) { return Register(from, to, name, null); }

        ///// <summary>
        ///// 注册类型
        ///// </summary>
        ///// <typeparam name="TInterface">接口类型</typeparam>
        ///// <typeparam name="TImplement">实现类型</typeparam>
        ///// <returns></returns>
        //public virtual IObjectContainer Register<TInterface, TImplement>() { return Register(typeof(TInterface), null, typeof(TImplement), null); }

        /// <summary>
        /// 注册类型和名称
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <typeparam name="TImplement">实现类型</typeparam>
        /// <param name="name">名称</param>
        /// <param name="overwrite">是否覆盖</param>
        /// <returns></returns>
        public virtual IObjectContainer Register<TInterface, TImplement>(String name = null, Boolean overwrite = true) { return Register(typeof(TInterface), typeof(TImplement), null, name, overwrite); }

        ///// <summary>
        ///// 注册类型的实例
        ///// </summary>
        ///// <param name="from">接口类型</param>
        ///// <param name="instance">实例</param>
        ///// <returns></returns>
        //public virtual IObjectContainer Register(Type from, Object instance) { return Register(from, null, instance); }

        ///// <summary>
        ///// 注册类型指定名称的实例
        ///// </summary>
        ///// <param name="from">接口类型</param>
        ///// <param name="instance">实例</param>
        ///// <param name="name">名称</param>
        ///// <param name="overwrite">是否覆盖</param>
        ///// <returns></returns>
        //public virtual IObjectContainer Register(Type from, Object instance, String name = null, Boolean overwrite = true) { return Register(from, null, instance, name, overwrite); }

        ///// <summary>
        ///// 注册类型的实例
        ///// </summary>
        ///// <typeparam name="TInterface">接口类型</typeparam>
        ///// <param name="instance">实例</param>
        ///// <returns></returns>
        //public virtual IObjectContainer Register<TInterface>(Object instance) { return Register(typeof(TInterface), null, instance); }

        /// <summary>
        /// 注册类型指定名称的实例
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="instance">实例</param>
        /// <param name="name">名称</param>
        /// <param name="overwrite">是否覆盖</param>
        /// <returns></returns>
        public virtual IObjectContainer Register<TInterface>(Object instance, String name = null, Boolean overwrite = true) { return Register(typeof(TInterface), null, instance, name, overwrite); }
        #endregion

        #region 解析
        ///// <summary>
        ///// 解析类型的实例
        ///// </summary>
        ///// <param name="from">接口类型</param>
        ///// <returns></returns>
        //public virtual Object Resolve(Type from) { return Resolve(from, null); }

        /// <summary>
        /// 解析类型指定名称的实例
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual Object Resolve(Type from, String name = null)
        {
            if (from == null) throw new ArgumentNullException("from");
            // 名称不能是null，否则字典里面会报错
            if (name == null) name = String.Empty;

            IDictionary<String, IObjectMap> dic = Find(from, false);
            // 1，如果容器里面没有这个类型，则返回空
            // 这个type可能是接口类型
            if (dic == null) return null;

            // 2，如果容器里面包含这个类型，并且指向的实例不为空，则返回
            // 根据名称去找，找不到返回空
            //if (!dic.TryGetValue(name, out map) || map == null) return null;
            IObjectMap map = FindMap(dic, name);
            if (map == null) return null;
            if (map.Instance != null) return map.Instance;

            // 检查是否指定实现类型
            if (map.ImplementType == null) throw new XException("设计错误，名为{0}的{1}实现未找到！", name, from);

            Object obj = null;
            // 3，如果容器里面包含这个类型，并且指向的实例为空，则创建对象返回
            // 4，如果有带参数构造函数，则从容器内获取各个参数的实例，最后创建对象返回
            ConstructorInfo[] cis = map.ImplementType.GetConstructors();
            if (cis.Length <= 0)
                obj = TypeX.CreateInstance(map.ImplementType);
            else
            {
                // 找到无参数构造函数
                ConstructorInfo ci = map.ImplementType.GetConstructor(Type.EmptyTypes);
                if (ci != null)
                {
                    obj = ConstructorInfoX.Create(ci).CreateInstance(null);
                }
                else
                {
                    #region 构造函数注入
                    // 参数值缓存，避免相同类型参数，出现在不同构造函数中，造成重复Resolve的问题
                    Dictionary<Type, Object> pscache = new Dictionary<Type, Object>();
                    foreach (ConstructorInfo item in cis)
                    {
                        List<Object> ps = new List<Object>();
                        foreach (ParameterInfo pi in item.GetParameters())
                        {
                            Object pv = null;
                            // 处理值类型
                            if (pi.ParameterType.IsValueType)
                            {
                                pv = TypeX.CreateInstance(pi.ParameterType);
                                ps.Add(pv);
                                continue;
                            }

                            // 从缓存里面拿
                            if (pscache.TryGetValue(pi.ParameterType, out pv))
                            {
                                ps.Add(pv);
                                continue;
                            }

                            dic = Find(pi.ParameterType, false);
                            if (dic != null && dic.Count > 0)
                            {
                                // 解析该参数类型的实例
                                pv = Resolve(pi.ParameterType);
                                if (pv != null)
                                {
                                    pscache.Add(pi.ParameterType, pv);
                                    ps.Add(pv);
                                }
                            }

                            // 任意一个参数解析失败，都将导致失败
                            ps = null;
                            break;
                        }
                        // 取得完整参数，可以构造了
                        if (ps != null)
                        {
                            obj = ConstructorInfoX.Create(item).CreateInstance(ps.ToArray());
                            break;
                        }
                    }

                    // 遍历完所有构造函数都无法构造，失败！
                    if (obj == null) throw new XException("容器无法完整构造目标对象的任意一个构造函数！");
                    #endregion
                }
            }

            // 赋值注入
            foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(obj))
            {
                if (!pd.IsReadOnly && Stores.ContainsKey(pd.PropertyType)) pd.SetValue(obj, Resolve(pd.PropertyType));
            }

            return obj;
        }

        ///// <summary>
        ///// 解析类型的实例
        ///// </summary>
        ///// <typeparam name="TInterface">接口类型</typeparam>
        ///// <returns></returns>
        //public virtual TInterface Resolve<TInterface>() { return (TInterface)Resolve(typeof(TInterface), null); }

        /// <summary>
        /// 解析类型指定名称的实例
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual TInterface Resolve<TInterface>(String name = null) { return (TInterface)Resolve(typeof(TInterface), name); }

        /// <summary>
        /// 解析类型所有已注册的实例
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <returns></returns>
        public virtual IEnumerable<Object> ResolveAll(Type from)
        {
            if (from == null) throw new ArgumentNullException("from");

            IDictionary<String, IObjectMap> dic = Find(from, false);
            if (dic == null) yield break;

            foreach (IObjectMap item in dic.Values)
            {
                if (item.Instance != null) yield return item.Instance;
            }
        }

        /// <summary>
        /// 解析类型所有已注册的实例
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <returns></returns>
        public virtual IEnumerable<TInterface> ResolveAll<TInterface>()
        {
            IDictionary<String, IObjectMap> dic = Find(typeof(TInterface), false);
            if (dic == null) yield break;

            foreach (IObjectMap item in dic.Values)
            {
                if (item.Instance != null) yield return (TInterface)item.Instance;
            }
        }
        #endregion

        #region 解析类型
        ///// <summary>
        ///// 解析接口的实现类型
        ///// </summary>
        ///// <param name="from">接口类型</param>
        ///// <returns></returns>
        //public virtual Type ResolveType(Type from) { return ResolveType(from, null); }

        /// <summary>
        /// 解析接口指定名称的实现类型
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual Type ResolveType(Type from, String name = null)
        {
            if (from == null) throw new ArgumentNullException("from");
            // 名称不能是null，否则字典里面会报错
            if (name == null) name = String.Empty;

            IDictionary<String, IObjectMap> dic = Find(from, false);
            if (dic == null) return null;

            //IObjectMap map = null;
            //if (!dic.TryGetValue(name, out map) || map == null) return null;
            IObjectMap map = FindMap(dic, name);
            if (map == null) return null;

            return map.ImplementType;
        }

        ///// <summary>
        ///// 解析接口的实现类型
        ///// </summary>
        ///// <typeparam name="TInterface">接口类型</typeparam>
        ///// <returns></returns>
        //public virtual Type ResolveType<TInterface>() { return ResolveType(typeof(TInterface), null); }

        /// <summary>
        /// 解析接口指定名称的实现类型
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual Type ResolveType<TInterface>(String name = null) { return ResolveType(typeof(TInterface), name); }

        /// <summary>
        /// 解析类型所有已注册的实例
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <returns></returns>
        public virtual IEnumerable<Type> ResolveAllTypes(Type from)
        {
            if (from == null) throw new ArgumentNullException("from");

            IDictionary<String, IObjectMap> dic = Find(from, false);
            if (dic == null) yield break;

            foreach (IObjectMap item in dic.Values)
            {
                yield return item.ImplementType;
            }
        }

        /// <summary>
        /// 解析接口所有已注册的对象映射
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <returns></returns>
        public virtual IEnumerable<IObjectMap> ResolveAllMaps(Type from)
        {
            if (from == null) throw new ArgumentNullException("from");

            IDictionary<String, IObjectMap> dic = Find(from, false);
            if (dic == null) yield break;

            foreach (IObjectMap item in dic.Values)
            {
                yield return item;
            }
        }
        #endregion

        #region Xml配置文件注册
        const String CONFIG_PREFIX = "NewLife.ObjectContainer_";
        /// <summary>
        /// 加载配置
        /// </summary>
        protected virtual void LoadConfig()
        {
            NameValueCollection nvs = Config.GetConfigByPrefix(CONFIG_PREFIX);
            if (nvs == null || nvs.Count < 1) return;

            foreach (String item in nvs.Keys)
            {
                String key = item;
                String value = nvs[key];

                if (value.IsNullOrWhiteSpace()) continue;

                String name = key.Substring(CONFIG_PREFIX.Length);
                if (name.IsNullOrWhiteSpace()) continue;

                Type type = TypeX.GetType(name, true);
                if (type == null) throw new XException("未找到对象容器配置{0}中的类型{1}！", key, name);

                Map map = GetConfig(value);
                if (map == null) continue;

                Register(type, null, null, map.TypeName, map.Mode, map.Name, (map.Mode & ModeFlags.Overwrite) == ModeFlags.Overwrite);
            }
        }

        static Map GetConfig(String str)
        {
            IDictionary<String, String> dic = ParseDic(str);
            if (dic == null || dic.Count < 1)
            {
                if (!str.IsNullOrWhiteSpace()) return new Map { TypeName = str };
                return null;
            }

            Map map = new Map();
            foreach (KeyValuePair<String, String> item in dic)
            {
                switch (item.Key.ToLower())
                {
                    case "name":
                        map.Name = item.Value;
                        break;
                    case "type":
                        map.TypeName = item.Value;
                        break;
                    case "mode":
                        map.Mode = ModeFlags.None;
                        //// 默认覆盖
                        //map.Mode |= ModeFlags.Overwrite;
                        String[] ss = item.Value.Split(new Char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < ss.Length; i++)
                        {
                            try
                            {
                                ModeFlags mf = (ModeFlags)Enum.Parse(typeof(ModeFlags), ss[i], true);
                                map.Mode |= mf;
                            }
                            catch { }
                        }
                        break;
                    default:
                        break;
                }
            }
            return map;
        }

        static IDictionary<String, String> ParseDic(String str)
        {
            if (str.IsNullOrWhiteSpace()) return null;

            IDictionary<String, String> dic = new Dictionary<String, String>();
            String[] ss = str.Split(new Char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (ss == null || ss.Length < 1) return null;

            foreach (String item in ss)
            {
                Int32 p = item.IndexOf('=');
                // 在前后都不行
                if (p <= 0 || p >= item.Length - 1) continue;

                String key = item.Substring(0, p).Trim();
                dic[key] = item.Substring(p + 1).Trim();
            }

            return dic;
        }
        #endregion
    }
}