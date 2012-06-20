using System;
using System.Collections.Generic;
using NewLife.Configuration;
using NewLife.Exceptions;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Model
{
    /// <summary>实现 <seealso cref="IObjectContainer"/> 接口的对象容器</summary>
    /// <remarks>
    /// 1，如果容器里面没有这个类型，则返回空；
    /// 2，如果容器里面包含这个类型，<see cref="ResolveInstance"/>返回单例；
    /// 3，如果容器里面包含这个类型，<see cref="Resolve(Type, Object, Boolean)"/>创建对象返回多实例；
    /// 4，如果有带参数构造函数，则从容器内获取各个参数的实例，最后创建对象返回。
    /// 
    /// 这里有一点跟大多数对象容器非常不同，其它对象容器会控制对象的生命周期，在对象不再使用时收回到容器里面。
    /// 这里的对象容器主要是为了用于解耦，所以只有最简单的功能实现。
    /// 
    /// 代码注册的默认优先级是0；
    /// 配置注册的默认优先级是1；
    /// 自动注册的外部实现（非排除项）的默认优先级是1，排除项的优先级是0；
    /// 所以，配置注册的优先级最高
    /// </remarks>
    public class ObjectContainer : IObjectContainer
    {
        #region 当前静态对象容器
        private static IObjectContainer _Current;
        /// <summary>当前容器</summary>
        public static IObjectContainer Current
        {
            get
            {
                if (_Current != null) return _Current;
                lock (typeof(ObjectContainer))
                {
                    if (_Current != null) return _Current;

                    return _Current = new ObjectContainer();
                }
            }
            set { _Current = value; }
        }
        #endregion

        #region 构造函数
        /// <summary>初始化一个对象容器实例，自动从配置文件中加载注册</summary>
        public ObjectContainer() { LoadConfig(); }
        #endregion

        #region 对象字典
        private IDictionary<Type, IDictionary<Object, IObjectMap>> _stores = null;
        private IDictionary<Type, IDictionary<Object, IObjectMap>> Stores { get { return _stores ?? (_stores = new Dictionary<Type, IDictionary<Object, IObjectMap>>()); } }

        private IDictionary<Object, IObjectMap> Find(Type type, Boolean add)
        {
            IDictionary<Object, IObjectMap> dic = null;
            if (Stores.TryGetValue(type, out dic)) return dic;

            if (add)
            {
                lock (Stores)
                {
                    if (Stores.TryGetValue(type, out dic)) return dic;

                    // 名称不区分大小写
                    //dic = new Dictionary<Object, IObjectMap>(StringComparer.OrdinalIgnoreCase);
                    dic = new Dictionary<Object, IObjectMap>();
                    Stores.Add(type, dic);
                    return dic;
                }
            }

            return null;
        }

        private IObjectMap FindMap(IDictionary<Object, IObjectMap> dic, Object id, Boolean extend = false)
        {
            IObjectMap map = null;
            // 名称不能是null，否则字典里面会报错
            if (id == null) id = String.Empty;
            // 如果找到，直接返回
            if (dic.TryGetValue(id, out map) || dic.TryGetValue(id + "", out map)) return map;

            //if (!String.IsNullOrEmpty(id))
            if (id == null || "" + id == String.Empty)
            {
                // 如果名称不为空，则试一试找空的
                if (dic.TryGetValue(String.Empty, out map)) return map;
            }
            else if (extend)
            {
                // 如果名称为空，找第一个
                foreach (var item in dic.Values)
                {
                    return item;
                }
            }
            return null;
        }

        class Map : IObjectMap
        {
            #region 属性
            private Object _Identity;
            /// <summary>名称</summary>
            public Object Identity { get { return _Identity; } set { _Identity = value; } }

            private String _TypeName;
            /// <summary>类型名</summary>
            public String TypeName { get { return _TypeName; } set { _TypeName = value; } }

            private Type _ImplementType;
            /// <summary>实现类型</summary>
            public Type ImplementType
            {
                get
                {
                    if (_ImplementType == null && !TypeName.IsNullOrWhiteSpace())
                    {
                        _ImplementType = TypeX.GetType(TypeName, true);
                        if (_ImplementType == null) throw new XException("无法找到类型{0}！", TypeName);
                    }
                    return _ImplementType;
                }
                set { _ImplementType = value; }
            }

            //private Boolean _Singleton;
            ///// <summary>单一实例</summary>
            //public Boolean Singleton { get { return _Singleton; } set { _Singleton = value; } }

            private Int32 _Priority;
            /// <summary>优先级</summary>
            public Int32 Priority { get { return _Priority; } set { _Priority = value; } }

            private Boolean hasCheck = false;

            private Object _Instance;
            /// <summary>实例</summary>
            public Object Instance
            {
                get
                {
                    if (_Instance != null || hasCheck) return _Instance;

                    //// 如果模式指定使用实例，而实例又为空，则初始化一个实例
                    ////if ((Mode & ModeFlags.Singleton) != ModeFlags.Singleton) return _Instance;
                    //if (!Singleton) return _Instance;

                    try
                    {
                        if (ImplementType != null) _Instance = TypeX.CreateInstance(ImplementType);
                    }
                    catch { }
                    hasCheck = true;

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
            public ModeFlags Mode { get { return _Mode; } set { _Mode = value; } }
            #endregion

            #region 方法
            public override string ToString()
            {
                return String.Format("[{0},{1}]", Identity, ImplementType != null ? ImplementType.Name : null);
            }
            #endregion
        }

        /// <summary>模式标记</summary>
        [Flags]
        enum ModeFlags
        {
            None = 0,

            ///// <summary>
            ///// 以单例模式注册，如果注册的是类型，则new一个实例
            ///// </summary>
            //Singleton = 1,

            ///// <summary>
            ///// 是否覆盖已有的注册
            ///// </summary>
            //Overwrite = 2,

            /// <summary>是否扩展，扩展注册将附加在该接口的第一个注册项之后</summary>
            Extend = 4
        }
        #endregion

        #region 注册核心
        /// <summary>注册</summary>
        /// <param name="from">接口类型</param>
        /// <param name="to">实现类型</param>
        /// <param name="instance">实例</param>
        /// <param name="id">标识</param>
        /// <param name="priority">优先级</param>
        /// <returns></returns>
        public virtual IObjectContainer Register(Type from, Type to, Object instance, Object id = null, Int32 priority = 0)
        {
            return Register(from, to, instance, null, ModeFlags.None, id, priority);
        }

        private IObjectContainer Register(Type from, Type to, Object instance, String typeName, ModeFlags mode, Object id, Int32 priority/*, Boolean singleton = false*/)
        {
            if (from == null) throw new ArgumentNullException("from");
            // 名称不能是null，否则字典里面会报错
            if (id == null) id = String.Empty;

            var dic = Find(from, true);
            IObjectMap old = null;
            Map map = null;
            if (dic.TryGetValue(id, out old) || dic.TryGetValue(id + "", out old))
            {
                map = old as Map;
                if (map != null)
                {
                    // 优先级太小不能覆盖
                    if (priority <= map.Priority) return this;

                    map.TypeName = typeName;
                    map.Mode = mode;
                    map.ImplementType = to;
                    map.Instance = instance;
                    //map.Singleton = instance != null || singleton;

                    if (OnRegistering != null) OnRegistering(this, new EventArgs<Type, IObjectMap>(from, map));
                    if (OnRegistered != null) OnRegistered(this, new EventArgs<Type, IObjectMap>(from, map));

                    return this;
                }
                else
                    dic.Remove(id);
            }

            map = new Map();
            map.Identity = id;
            map.TypeName = typeName;
            map.Mode = mode;
            map.Priority = priority;
            if (to != null) map.ImplementType = to;
            if (instance != null) map.Instance = instance;
            //map.Singleton = instance != null || singleton;

            if (!dic.ContainsKey(id))
            {
                if (OnRegistering != null) OnRegistering(this, new EventArgs<Type, IObjectMap>(from, map));
                dic.Add(id, map);
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
        /// <summary>注册类型和名称</summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <typeparam name="TImplement">实现类型</typeparam>
        /// <param name="id">标识</param>
        /// <param name="priority">优先级</param>
        /// <returns></returns>
        public virtual IObjectContainer Register<TInterface, TImplement>(Object id = null, Int32 priority = 0) { return Register(typeof(TInterface), typeof(TImplement), null, id, priority); }

        /// <summary>注册类型指定名称的实例</summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="instance">实例</param>
        /// <param name="id">标识</param>
        /// <param name="priority">优先级</param>
        /// <returns></returns>
        public virtual IObjectContainer Register<TInterface>(Object instance, Object id = null, Int32 priority = 0) { return Register(typeof(TInterface), null, instance, id, priority); }

        /// <summary>遍历所有程序集的所有类型，自动注册实现了指定接口或基类的类型。如果没有注册任何实现，则默认注册第一个排除类型</summary>
        /// <remarks>自动注册一般用于单实例功能扩展型接口</remarks>
        /// <param name="from">接口或基类</param>
        /// <param name="excludeTypes">要排除的类型，一般是内部默认实现</param>
        /// <returns></returns>
        public virtual IObjectContainer AutoRegister(Type from, params Type[] excludeTypes)
        {
            return AutoRegister(from, null, null, 0, excludeTypes);
        }

        /// <summary>遍历所有程序集的所有类型，自动注册实现了指定接口或基类的类型。如果没有注册任何实现，则默认注册第一个排除类型</summary>
        /// <param name="from">接口或基类</param>
        /// <param name="getidCallback">用于从外部类型对象中获取标识的委托</param>
        /// <param name="id">标识</param>
        /// <param name="priority">优先级</param>
        /// <param name="excludeTypes">要排除的类型，一般是内部默认实现</param>
        /// <returns></returns>
        public virtual IObjectContainer AutoRegister(Type from, Func<Object, Object> getidCallback = null, Object id = null, Int32 priority = 0, params Type[] excludeTypes)
        {
            if (from == null) throw new ArgumentNullException("from");

            if (excludeTypes == null) excludeTypes = Type.EmptyTypes;

            // 如果存在已注册项，并且优先级大于0，那么这里就不要注册了
            var dic = Find(from, false);
            if (dic != null)
            {
                var map = FindMap(dic, null, false) as Map;
                if (map != null && map.Priority > 0) return this;
            }

            // 遍历所有程序集，自动加载
            foreach (var item in AssemblyX.FindAllPlugins(from, true))
            {
                if (Array.IndexOf(excludeTypes, item) < 0)
                {
                    // 自动注册的优先级是1，高于默认的0
                    //Register(from, item, null, null, 1);
                    // 实例化一次，让这个类有机会执行类型构造函数，可以获取旧的类型实现
                    var obj = TypeX.CreateInstance(item);
                    // 如果指定了获取ID的委托，并且取得的ID与传入ID不一致，则不承认
                    if (getidCallback != null && id != getidCallback(obj)) continue;

                    if (XTrace.Debug) XTrace.WriteLine("为{0}自动注册{1}，标识={2}，优先级={3}！", from.FullName, item.FullName, id, priority + 1);

                    Register(from, null, obj, id, priority + 1);
                    return this;
                }
            }

            // 如果没有注册任何实现，则默认注册第一个排除类型
            if (excludeTypes.Length > 0)
            {
                //if (dic == null)
                {
                    Register(from, excludeTypes[0], null, id, priority);
                }
            }

            return this;
        }

        /// <summary>遍历所有程序集的所有类型，自动注册实现了指定接口或基类的类型。如果没有注册任何实现，则默认注册第一个排除类型</summary>
        /// <remarks>自动注册一般用于单实例功能扩展型接口</remarks>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <typeparam name="TImplement">要排除的类型，一般是内部默认实现</typeparam>
        /// <returns></returns>
        public virtual IObjectContainer AutoRegister<TInterface, TImplement>() { return AutoRegister(typeof(TInterface), typeof(TImplement)); }

        /// <summary>遍历所有程序集的所有类型，自动注册实现了指定接口或基类的类型。如果没有注册任何实现，则默认注册第一个排除类型</summary>
        /// <remarks>自动注册一般用于单实例功能扩展型接口</remarks>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <typeparam name="TImplement">要排除的类型，一般是内部默认实现</typeparam>
        /// <param name="getidCallback">用于从外部类型对象中获取标识的委托</param>
        /// <param name="id">标识</param>
        /// <param name="priority">优先级</param>
        /// <returns></returns>
        public virtual IObjectContainer AutoRegister<TInterface, TImplement>(Func<Object, Object> getidCallback = null, Object id = null, Int32 priority = 0) { return AutoRegister(typeof(TInterface), getidCallback, id, priority, typeof(TImplement)); }
        #endregion

        #region 解析
        private Object Resolve(Type from, Boolean getInstance, Object id, Boolean extend)
        {
            if (from == null) throw new ArgumentNullException("from");
            // 名称不能是null，否则字典里面会报错
            if (id == null) id = String.Empty;

            var dic = Find(from, false);
            // 1，如果容器里面没有这个类型，则返回空
            // 这个type可能是接口类型
            if (dic == null) return null;

            // 2，如果容器里面包含这个类型，并且指向的实例不为空，则返回
            // 根据名称去找，找不到返回空
            var map = FindMap(dic, id, extend);
            if (map == null) return null;
            // 如果就是为了取实例，直接返回
            if (getInstance) return map.Instance;
            // 否则每次都实例化
            //if (map.Instance != null) return map.Instance;

            // 检查是否指定实现类型，这种可能性极低，根本就不应该存在
            if (map.ImplementType == null) throw new XException("设计错误，名为{0}的{1}实现未找到！", id, from);

            Object obj = null;
            // 3，如果容器里面包含这个类型，并且指向的实例为空，则创建对象返回
            // 4，如果有带参数构造函数，则从容器内获取各个参数的实例，最后创建对象返回
            var cis = map.ImplementType.GetConstructors();
            if (cis.Length <= 0)
                obj = TypeX.CreateInstance(map.ImplementType);
            else
            {
                // 找到无参数构造函数
                var ci = map.ImplementType.GetConstructor(Type.EmptyTypes);
                if (ci != null)
                {
                    obj = ConstructorInfoX.Create(ci).CreateInstance(null);
                }
                else
                {
                    #region 构造函数注入
                    // 参数值缓存，避免相同类型参数，出现在不同构造函数中，造成重复Resolve的问题
                    var pscache = new Dictionary<Type, Object>();
                    foreach (var item in cis)
                    {
                        var ps = new List<Object>();
                        foreach (var pi in item.GetParameters())
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
                                    continue;
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

            //// 赋值注入
            //foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(obj))
            //{
            //    if (!pd.IsReadOnly && Stores.ContainsKey(pd.PropertyType)) pd.SetValue(obj, Resolve(pd.PropertyType));
            //}

            return obj;
        }

        /// <summary>解析类型指定名称的实例</summary>
        /// <param name="from">接口类型</param>
        /// <param name="id">标识</param>
        /// <param name="extend">扩展。若为ture，id为null而找不到时，采用第一个注册项；id不为null而找不到时，采用null注册项</param>
        /// <returns></returns>
        public virtual Object Resolve(Type from, Object id = null, Boolean extend = false)
        {
            return Resolve(from, false, id, extend);
        }

        /// <summary>解析类型指定名称的实例</summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="id">标识</param>
        /// <param name="extend">扩展。若为ture，name为null而找不到时，采用第一个注册项；name不为null而找不到时，采用null注册项</param>
        /// <returns></returns>
        public virtual TInterface Resolve<TInterface>(Object id = null, Boolean extend = false) { return (TInterface)Resolve(typeof(TInterface), false, id, extend); }

        /// <summary>解析类型指定名称的实例</summary>
        /// <param name="from">接口类型</param>
        /// <param name="id">标识</param>
        /// <param name="extend">扩展。若为ture，id为null而找不到时，采用第一个注册项；id不为null而找不到时，采用null注册项</param>
        /// <returns></returns>
        public virtual Object ResolveInstance(Type from, Object id = null, Boolean extend = false)
        {
            return Resolve(from, true, id, extend);
        }

        /// <summary>解析类型指定名称的实例</summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="id">标识</param>
        /// <param name="extend">扩展。若为ture，name为null而找不到时，采用第一个注册项；name不为null而找不到时，采用null注册项</param>
        /// <returns></returns>
        public virtual TInterface ResolveInstance<TInterface>(Object id = null, Boolean extend = false) { return (TInterface)Resolve(typeof(TInterface), true, id, extend); }

        /// <summary>解析类型所有已注册的实例</summary>
        /// <param name="from">接口类型</param>
        /// <returns></returns>
        public virtual IEnumerable<Object> ResolveAll(Type from)
        {
            if (from == null) throw new ArgumentNullException("from");

            var dic = Find(from, false);
            if (dic == null) yield break;

            foreach (var item in dic.Values)
            {
                if (item.Instance != null) yield return item.Instance;
            }
        }

        /// <summary>解析类型所有已注册的实例</summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <returns></returns>
        public virtual IEnumerable<TInterface> ResolveAll<TInterface>()
        {
            var dic = Find(typeof(TInterface), false);
            if (dic == null) yield break;

            foreach (var item in dic.Values)
            {
                if (item.Instance != null) yield return (TInterface)item.Instance;
            }
        }
        #endregion

        #region 解析类型
        /// <summary>解析接口指定名称的实现类型</summary>
        /// <param name="from">接口类型</param>
        /// <param name="id">标识</param>
        /// <param name="extend">扩展。若为ture，name为null而找不到时，采用第一个注册项；name不为null而找不到时，采用null注册项</param>
        /// <returns></returns>
        public virtual Type ResolveType(Type from, Object id = null, Boolean extend = false)
        {
            if (from == null) throw new ArgumentNullException("from");
            // 名称不能是null，否则字典里面会报错
            if (id == null) id = String.Empty;

            var dic = Find(from, false);
            if (dic == null) return null;

            var map = FindMap(dic, id, extend);
            if (map == null) return null;

            return map.ImplementType;
        }

        /// <summary>解析接口指定名称的实现类型</summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="id">标识</param>
        /// <param name="extend">扩展。若为ture，name为null而找不到时，采用第一个注册项；name不为null而找不到时，采用null注册项</param>
        /// <returns></returns>
        public virtual Type ResolveType<TInterface>(Object id = null, Boolean extend = false) { return ResolveType(typeof(TInterface), id, extend); }

        /// <summary>解析类型所有已注册的实例</summary>
        /// <param name="from">接口类型</param>
        /// <returns></returns>
        public virtual IEnumerable<Type> ResolveAllTypes(Type from)
        {
            if (from == null) throw new ArgumentNullException("from");

            var dic = Find(from, false);
            if (dic == null) yield break;

            foreach (var item in dic.Values)
            {
                yield return item.ImplementType;
            }
        }

        /// <summary>解析接口所有已注册的对象映射</summary>
        /// <param name="from">接口类型</param>
        /// <returns></returns>
        public virtual IEnumerable<IObjectMap> ResolveAllMaps(Type from)
        {
            if (from == null) throw new ArgumentNullException("from");

            var dic = Find(from, false);
            if (dic == null) yield break;

            foreach (var item in dic.Values)
            {
                yield return item;
            }
        }
        #endregion

        #region Xml配置文件注册
        const String CONFIG_PREFIX = "NewLife.ObjectContainer_";
        /// <summary>加载配置</summary>
        protected virtual void LoadConfig()
        {
            var nvs = Config.GetConfigByPrefix(CONFIG_PREFIX);
            if (nvs == null || nvs.Count < 1) return;

            foreach (var item in nvs)
            {
                if (item.Value.IsNullOrWhiteSpace()) continue;

                var name = item.Key;
                if (name.IsNullOrWhiteSpace()) continue;

                var type = TypeX.GetType(name, true);
                if (type == null)
                {
                    XTrace.WriteLine("未找到对象容器配置{0}中的类型{1}！", item.Key, name);
                    continue;
                }

                var map = GetConfig(item.Value);
                if (map == null) continue;

                if (XTrace.Debug) XTrace.WriteLine("为{0}配置注册{1}，标识Identity={2}，优先级Priority={3}！", type.FullName, map.TypeName, map.Identity, map.Priority);

                Register(type, null, null, map.TypeName, map.Mode, map.Identity, map.Priority/*, map.Singleton*/);
            }
        }

        static Map GetConfig(String str)
        {
            // 如果不含=，表示整个str就是类型Type
            if (!str.Contains("=")) return new Map() { TypeName = str };

            var dic = str.SplitAsDictionary();
            if (dic == null || dic.Count < 1)
            {
                if (!str.IsNullOrWhiteSpace()) return new Map { TypeName = str };
                return null;
            }

            var map = new Map();
            foreach (var item in dic)
            {
                switch (item.Key.ToLower())
                {
                    case "name":
                        map.Identity = item.Value;
                        break;
                    case "type":
                        map.TypeName = item.Value;
                        break;
                    case "singleton":
                        //map.Singleton = item.Value.EqualIgnoreCase("true") || item.Value == "1";
                        break;
                    case "priority":
                        Int32 n = 0;
                        if (Int32.TryParse(item.Value, out n)) map.Priority = n;
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
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString() { return String.Format("{0}[Count={1}]", this.GetType().Name, Stores.Count); }
        #endregion
    }
}