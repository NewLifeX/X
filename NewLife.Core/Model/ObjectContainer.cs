using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Model
{
    /// <summary>实现 <seealso cref="IObjectContainer"/> 接口的对象容器</summary>
    /// <remarks>
    /// 1，如果容器里面没有这个类型，则返回空；
    /// 2，如果容器里面包含这个类型，<see cref="ResolveInstance"/>返回单例；
    /// 3，如果容器里面包含这个类型，<see cref="Resolve(Type, Object)"/>创建对象返回多实例；
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
        /// <summary>当前容器</summary>
        public static IObjectContainer Current { get; set; } = new ObjectContainer();
        #endregion

        #region 构造函数
        /// <summary>初始化一个对象容器实例，自动从配置文件中加载注册</summary>
        public ObjectContainer() { }
        #endregion

        #region 对象字典
        private ConcurrentDictionary<Type, IDictionary<String, IObjectMap>> Stores { get; } = new ConcurrentDictionary<Type, IDictionary<String, IObjectMap>>();

        /// <summary>不存在又不添加时返回空列表</summary>
        /// <param name="type"></param>
        /// <param name="add"></param>
        /// <returns></returns>
        private IDictionary<String, IObjectMap> Find(Type type, Boolean add = false)
        {
            if (Stores.TryGetValue(type, out var dic)) return dic;

            if (!add) return null;

            return Stores.GetOrAdd(type, k => new Dictionary<String, IObjectMap>(StringComparer.OrdinalIgnoreCase));
        }

        private IObjectMap FindMap(IDictionary<String, IObjectMap> dic, Object id)
        {
            if (dic == null || dic.Count <= 0) return null;

            // 如果找到，直接返回
            if (dic.TryGetValue(id + "", out var map)) return map;

            return null;
        }

        class Map : IObjectMap
        {
            #region 属性
            /// <summary>名称</summary>
            public Object Identity { get; set; }

            /// <summary>实现类型</summary>
            public Type Type { get; set; }

            /// <summary>优先级</summary>
            public Int32 Priority { get; set; }

            private Boolean hasCheck = false;

            private Object _Instance;
            /// <summary>实例</summary>
            public Object Instance
            {
                get
                {
                    if (_Instance != null || hasCheck) return _Instance;

                    _Instance = Type?.CreateInstance();
                    hasCheck = true;

                    return _Instance;
                }
                set
                {
                    _Instance = value;
                    if (value != null) Type = value.GetType();
                }
            }
            #endregion

            #region 方法
            public override String ToString()
            {
                return String.Format("[{0},{1}]", Identity, Type?.Name);
            }
            #endregion
        }
        #endregion

        #region 注册
        /// <summary>注册</summary>
        /// <param name="from">接口类型</param>
        /// <param name="to">实现类型</param>
        /// <param name="instance">实例</param>
        /// <param name="id">标识</param>
        /// <param name="priority">优先级</param>
        /// <returns></returns>
        public virtual IObjectContainer Register(Type from, Type to, Object instance, Object id = null, Int32 priority = 0)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            var key = id + "";

            var dic = Find(from, true);
            Map map = null;
            if (dic.TryGetValue(key, out var old))
            {
                map = old as Map;
                if (map != null)
                {
                    // 优先级太小不能覆盖
                    if (priority <= map.Priority) return this;

                    map.Type = to;
                    map.Instance = instance;

                    return this;
                }
                else
                {
                    lock (dic)
                    {
                        dic.Remove(key);
                    }
                }
            }

            map = new Map
            {
                Identity = id,
                Priority = priority
            };
            if (to != null) map.Type = to;
            if (instance != null) map.Instance = instance;

            if (!dic.ContainsKey(key))
            {
                lock (dic)
                {
                    if (!dic.ContainsKey(key)) dic.Add(key, map);
                }
            }

            return this;
        }

        /// <summary>遍历所有程序集的所有类型，自动注册实现了指定接口或基类的类型。如果没有注册任何实现，则默认注册第一个排除类型</summary>
        /// <remarks>自动注册一般用于单实例功能扩展型接口</remarks>
        /// <param name="from">接口或基类</param>
        /// <param name="excludeTypes">要排除的类型，一般是内部默认实现</param>
        /// <returns></returns>
        public virtual IObjectContainer AutoRegister(Type from, params Type[] excludeTypes)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));

            if (excludeTypes == null) excludeTypes = Type.EmptyTypes;

            // 如果存在已注册项，并且优先级大于0，那么这里就不要注册了
            var dic = Find(from);
            if (dic != null && dic.Count > 0)
            {
                if (FindMap(dic, null) is Map map && map.Priority > 0) return this;
            }

            // 遍历所有程序集，自动加载
            foreach (var item in from.GetAllSubclasses(true))
            {
                if (Array.IndexOf(excludeTypes, item) < 0)
                {
                    // 实例化一次，让这个类有机会执行类型构造函数，可以获取旧的类型实现
                    var obj = item.CreateInstance();

                    if (XTrace.Debug) XTrace.WriteLine("为{0}自动注册{1}", from.FullName, item.FullName);

                    Register(from, null, obj);
                    return this;
                }
            }

            // 如果没有注册任何实现，则默认注册第一个排除类型
            if (excludeTypes.Length > 0) Register(from, excludeTypes[0], null);

            return this;
        }
        #endregion

        #region 解析
        private Object Resolve(Type from, Boolean getInstance, Object id)
        {
            if (from == null) throw new ArgumentNullException("from");
            // 名称不能是null，否则字典里面会报错
            if (id == null) id = String.Empty;

            var dic = Find(from);

            // 1，如果容器里面没有这个类型，则返回空
            // 这个type可能是接口类型
            if (dic == null || dic.Count <= 0) return null;

            // 2，如果容器里面包含这个类型，并且指向的实例不为空，则返回
            // 根据名称去找，找不到返回空
            var map = FindMap(dic, id);
            if (map == null) return null;

            // 如果就是为了取实例，直接返回
            if (getInstance) return map.Instance;
            // 否则每次都实例化

            // 检查是否指定实现类型，这种可能性极低，根本就不应该存在
            if (map.Type == null) throw new XException("设计错误，名为{0}的{1}实现未找到！", id, from);

            // 3，如果容器里面包含这个类型，并且指向的实例为空，则创建对象返回。不再支持构造函数依赖注入
            return map.Type.CreateInstance();
        }

        /// <summary>解析类型指定名称的实例</summary>
        /// <param name="from">接口类型</param>
        /// <param name="id">标识</param>
        /// <returns></returns>
        public virtual Object Resolve(Type from, Object id = null) { return Resolve(from, false, id); }

        /// <summary>解析类型指定名称的实例</summary>
        /// <param name="from">接口类型</param>
        /// <param name="id">标识</param>
        /// <returns></returns>
        public virtual Object ResolveInstance(Type from, Object id = null) { return Resolve(from, true, id); }
        #endregion

        #region 解析类型
        /// <summary>解析接口指定名称的实现类型</summary>
        /// <param name="from">接口类型</param>
        /// <param name="id">标识</param>
        /// <returns></returns>
        public virtual Type ResolveType(Type from, Object id = null)
        {
            if (from == null) throw new ArgumentNullException("from");

            var map = FindMap(Find(from), id);
            if (map == null) return null;

            return map.Type;
        }

        /// <summary>解析接口所有已注册的对象映射</summary>
        /// <param name="from">接口类型</param>
        /// <returns></returns>
        public virtual IEnumerable<IObjectMap> ResolveAll(Type from)
        {
            var dic = Find(from);
            if (dic != null)
                return Find(from).Values;
            else
                return new List<IObjectMap>();
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() { return String.Format("{0}[Count={1}]", GetType().Name, Stores.Count); }
        #endregion
    }
}