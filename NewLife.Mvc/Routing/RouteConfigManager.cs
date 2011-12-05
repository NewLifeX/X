using System;
using System.Collections.Generic;
using NewLife.Collections;
using NewLife.Reflection;

namespace NewLife.Mvc
{
    /// <summary>路由配置管理器</summary>
    public class RouteConfigManager : IList<Rule>
    {
        #region 公共

        /// <summary>
        /// 指定路径路由到指定控制器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public RouteConfigManager Route<T>(string path) where T : IController, new()
        {
            return Route(path, typeof(T), typeof(IController));
        }

        /// <summary>
        /// 指定路径路由到控制器工厂,工厂是单例的
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public RouteConfigManager RouteToFactory<T>(string path) where T : IControllerFactory, new()
        {
            return Route(path, typeof(T), typeof(IControllerFactory));
        }

        /// <summary>
        /// 指定路径路由到控制器工厂,自定义工厂的初始化
        /// </summary>
        /// <param name="path"></param>
        /// <param name="newFunc">工厂实例化方式</param>
        /// <returns></returns>
        public RouteConfigManager RouteToFactory(string path, Func<IControllerFactory> newFunc)
        {
            return Route(path, typeof(IControllerFactory), typeof(IControllerFactory), delegate(Rule r)
            {
                (r as FactoryRule).NewFactoryFunc = newFunc;
            });
        }

        /// <summary>
        /// 指定路径路由到模块,模块是一个独立的路由配置,可以相对于自身所路由的路径,进一步路由到具体的控制器或者工厂
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public RouteConfigManager RouteToModule<T>(string path) where T : IRouteConfigModule, new()
        {
            return Route(path, typeof(T), typeof(IRouteConfigModule));
        }

        /// <summary>
        /// 指定路径路由到指定名称的类型,目标类型需要是IController,IControllerFactory,IRouteConfigMoudule其中之一
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type">目标类型的完整名称,包括名称空间和类名</param>
        /// <returns></returns>
        public RouteConfigManager Route(string path, string type)
        {
            return Route(path, TypeX.GetType(type));
        }

        /// <summary>
        /// 指定路径路由到指定类型,类型需要是IController,IControllerFactory,IRouteConfigMoudule其中之一
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public RouteConfigManager Route(string path, Type type)
        {
            return Route(path, type, null);
        }

        /// <summary>
        /// 指定路径路由到指定类型,类型需要是IController,IControllerFactory,IRouteConfigMoudule其中之一
        ///
        /// onCreatedRule在创建了路由规则后会调用,可用于对路由规则做细节调整
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type">需要指定一个类型,可以是IController,IControllerFactory,IRouteConfigMoudule或其实现类型</param>
        /// <param name="onCreatedRule">根据Type类型的不同,参数可能是Rule或其子类FactoryRule,ModuleRule</param>
        /// <returns></returns>
        public RouteConfigManager Route(string path, Type type, Action<Rule> onCreatedRule)
        {
            return Route(path, type, typeof(object), null);
        }

        /// <summary>
        /// 指定多个路径路由到指定的目标,目标需要是IController,IControllerFactory,IRouteConfigMoudule其中之一
        /// </summary>
        /// <example>
        /// 一般用法:
        /// <code>
        /// Route(
        ///     "/foo", typeof(foo),
        ///     "/bar", "namespaceName.bar",
        ///     "" // 会忽略末尾不是成对出现的参数
        /// );
        /// </code>
        /// </example>
        /// <param name="args">多个路由规则,其中type可以是具体的Type或者字符串指定的类型名称</param>
        /// <returns></returns>
        public RouteConfigManager Route(params object[] args)
        {
            string path;
            int n = args.Length & ~1;

            for (int i = 0; i < n; i += 2)
            {
                path = args[i].ToString();
                object t = args[i + 1];
                if (t != null)
                {
                    if (t is string && !string.IsNullOrEmpty(t as string))
                    {
                        Route(path, t as string);
                    }
                    if (t is Type)
                    {
                        Route(path, t as Type, typeof(object));
                    }
                }
            }
            return this;
        }

        /// <summary>
        /// 忽略指定路径的路由请求 后续的路由规则将不会尝试匹配
        ///
        /// 在Route(params object[] args) 中可以使用IgnoreRoute类来忽略路由请求
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <see cref="IgnoreRoute"/>
        public RouteConfigManager Ignore(params string[] path)
        {
            foreach (var p in path)
            {
                RouteToFactory(p, IgnoreRoute.InstanceFunc);
            }
            return this;
        }

        /// <summary>
        /// 加载指定模块类型的路由配置,不同于RouteToModule(string path),这个相当于IRouteConfigModule.Config(this)
        /// </summary>
        /// <returns></returns>
        public RouteConfigManager Load<T>() where T : IRouteConfigModule, new()
        {
            T m;
            Load<T>(out m);
            return this;
        }

        /// <summary>
        /// 加载指定模块类型的路由配置,不同于RouteToModule(string path),这个相当于IRouteConfigModule.Config(this)
        ///
        /// 其中module是创建的T类型的实例,全局单例的
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="module"></param>
        /// <returns></returns>
        public RouteConfigManager Load<T>(out T module) where T : IRouteConfigModule, new()
        {
            IRouteConfigModule m = null;
            Load(typeof(T), out m);
            module = (T)m;
            return this;
        }

        /// <summary>
        /// 加载指定模块类型的路由配置,不同于RouteToModule(string path),这个相当于IRouteConfigModule.Config(this)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public RouteConfigManager Load(Type type)
        {
            IRouteConfigModule m;
            Load(type, out m);
            return this;
        }

        /// <summary>
        /// 加载指定模块类型的路由配置,不同于RouteToModule(string path),这个相当于IRouteConfigModule.Config(this)
        ///
        /// 其中module是创建的type类型的实例,全局单例的
        /// </summary>
        /// <param name="type"></param>
        /// <param name="module">type参数的具体实例,如果未创建会为null</param>
        /// <returns></returns>
        public RouteConfigManager Load(Type type, out IRouteConfigModule module)
        {
            module = null;
            if (type != null && typeof(IRouteConfigModule).IsAssignableFrom(type))
            {
                module = LoadModuleCache.GetItem(type, NewModuleFunc);
                Load(module);
            }
            return this;
        }

        /// <summary>
        /// 加载指定模块的路由配置,不同于RouteToModule(string path),这个相当于IRouteConfigModule.Config(this)
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        public RouteConfigManager Load(IRouteConfigModule module)
        {
            if (module != null)
            {
                try
                {
                    module.Config(this);
                }
                catch (RouteConfigException ex)
                {
                    if (ex.Module == null) ex.Module = module;
                    throw;
                }
            }
            return this;
        }

        /// <summary>
        /// 对路由规则进行排序 在使用这个路由配置前建议进行排序
        /// </summary>
        /// <returns></returns>
        public RouteConfigManager Sort()
        {
            return Sort(false);
        }

        /// <summary>
        /// 对路由规则进行排序 在使用这个路由配置前建议进行排序
        /// </summary>
        /// <param name="force">是否强制排序,一般使用false</param>
        public RouteConfigManager Sort(bool force)
        {
            if (!sorted || force)
            {
                StableSort(Rules, false, (a, b) => -(a.Path.Length - b.Path.Length));
                sorted = true;
            }
            return this;
        }

        #endregion 公共

        #region 私有

        List<Rule> _Rules;

        internal List<Rule> Rules
        {
            get
            {
                if (_Rules == null) _Rules = new List<Rule>();
                return _Rules;
            }
        }

        bool sorted = false;

        /// <summary>
        /// 最终添加路由配置的方法,上面的公共方法都会调用到这里
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        /// <param name="ruleType">路由规则类型,未知类型使用null或者typeof(object)</param>
        /// <param name="onCreatedRule">创建路由规则后的回调,参数中包含刚创建的路由规则</param>
        /// <returns></returns>
        internal virtual RouteConfigManager Route(string path, Type type, Type ruleType, Action<Rule> onCreatedRule = null)
        {
            Rule r = null;
            try
            {
                r = Rule.Create(path, type, ruleType);
            }
            catch (RouteConfigException ex)
            {
                if (ex.RoutePath == null) ex.RoutePath = path;
                throw;
            }
            if (onCreatedRule != null) onCreatedRule(r);
            Rules.Add(r);
            sorted = false;
            return this;
        }

        static DictionaryCache<Type, IRouteConfigModule>[] _LoadModuleCache = new DictionaryCache<Type, IRouteConfigModule>[] { null };

        /// <summary>
        /// 加载模块的IRouteConfigModule实例缓存,Type为键,用于Load&lt;Type&gt;()和Load(Type type)方法
        /// </summary>
        internal static DictionaryCache<Type, IRouteConfigModule> LoadModuleCache
        {
            get
            {
                if (_LoadModuleCache[0] == null)
                {
                    lock (_LoadModuleCache)
                    {
                        if (_LoadModuleCache[0] == null)
                        {
                            _LoadModuleCache[0] = new DictionaryCache<Type, IRouteConfigModule>();
                        }
                    }
                }
                return _LoadModuleCache[0];
            }
        }

        /// <summary>
        /// 实例化指定的IRouteConfigModule
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static IRouteConfigModule NewModuleFunc(Type type)
        {
            return (IRouteConfigModule)TypeX.CreateInstance(type);
        }

        #endregion 私有

        #region 静态方法

        /// <summary>
        /// 提供稳定排序,因为内部实现还是快速排序,所以需要指定isDesc参数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">待排序的的列表,返回值也将是这个</param>
        /// <param name="isDesc">使用comp比较相同的元素是否使用和默认顺序相反的顺序排列,想保持默认顺序的话应使用false</param>
        /// <param name="comp"></param>
        /// <returns></returns>
        public static IList<T> StableSort<T>(IList<T> list, bool isDesc, Comparison<T> comp)
        {
            KeyValuePair<int, T>[] sortList = new KeyValuePair<int, T>[list.Count];
            for (int i = 0; i < sortList.Length; i++)
            {
                sortList[i] = new KeyValuePair<int, T>(i, list[i]);
            }
            Array.Sort<KeyValuePair<int, T>>(sortList, delegate(KeyValuePair<int, T> a, KeyValuePair<int, T> b)
            {
                int r = comp(a.Value, b.Value);
                if (r == 0)
                {
                    r = a.Key - b.Key;
                    if (isDesc) return -r;
                }
                return r;
            });
            for (int i = 0; i < sortList.Length; i++)
            {
                list[i] = sortList[i].Value;
            }
            return list;
        }

        #endregion 静态方法

        #region 实现IList接口

        /// <summary>
        /// 实现IList接口
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Rule> GetEnumerator()
        {
            return Rules.GetEnumerator();
        }

        /// <summary>
        /// 实现IList接口
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// 实现IList接口
        /// </summary>
        public int Count
        {
            get
            {
                return Rules != null ? Rules.Count : 0;
            }
        }

        /// <summary>
        /// 实现IList接口
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(Rule item)
        {
            return Rules.IndexOf(item);
        }

        /// <summary>
        /// 实现IList接口
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        [Obsolete("请使用Route方法系列或Load方法", true)]
        public void Insert(int index, Rule item)
        {
        }

        /// <summary>
        /// 实现IList接口
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            Rules.RemoveAt(index);
        }

        /// <summary>
        /// 实现IList接口 get是可用的,set将抛出NotImplementedException异常,请使用Route方法系列或Load方法
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Rule this[int index]
        {
            get
            {
                return Rules[index];
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// 实现IList接口
        /// </summary>
        /// <param name="item"></param>
        [Obsolete("请使用Route方法系列或Load方法", true)]
        public void Add(Rule item)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 实现IList接口
        /// </summary>
        public void Clear()
        {
            Rules.Clear();
        }

        /// <summary>
        /// 实现IList接口
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(Rule item)
        {
            return Rules.Contains(item);
        }

        /// <summary>
        /// 实现IList接口
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(Rule[] array, int arrayIndex)
        {
            Rules.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// 实现IList接口
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// 实现IList接口
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(Rule item)
        {
            return Rules.Remove(item);
        }

        #endregion 实现IList接口
    }

    /// <summary>
    /// 路由配置异常
    /// </summary>
    public class RouteConfigException : ArgumentException
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="message"></param>
        public RouteConfigException(string message)
            : base(message) { }

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="message"></param>
        /// <param name="paramName"></param>
        public RouteConfigException(string message, string paramName)
            : base(message, paramName) { }

        /// <summary>
        /// 路由配置异常消息
        /// </summary>
        public override string Message
        {
            get
            {
                return string.Format("在路由配置 {0} 中的 {1} 路由项配置发生异常: {2}", Module != null ? Module.GetType().AssemblyQualifiedName : "", RoutePath, base.Message);
            }
        }

        internal IRouteConfigModule Module { get; set; }

        internal string RoutePath { get; set; }
    }
}