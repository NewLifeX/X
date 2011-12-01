using System;
using System.Collections.Generic;
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
            return Route(path, type, typeof(object));
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
        /// 从指定类型的模块加载路由配置
        ///
        /// 这将会实例化指定类型
        /// </summary>
        /// <typeparam name="T">指定类型,将会返回这个类型的实例</typeparam>
        /// <returns></returns>
        public T Load<T>() where T : IRouteConfigModule, new()
        {
            // TODO 考虑是否有必要优化
            return (T)Load(typeof(T));
        }

        /// <summary>
        /// 从指定类型的模块加载路由配置
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public IRouteConfigModule Load(Type type)
        {
            // TODO 考虑是否有必要优化
            IRouteConfigModule m = (IRouteConfigModule)TypeX.CreateInstance(type);
            if (m == null) return null;
            Load(m);
            return m;
        }

        /// <summary>
        /// 从指定模块加载路由配置
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
                    ex.Module = module;
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

        #region 内部

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
                ex.RoutePath = path;
                throw;
            }
            if (onCreatedRule != null) onCreatedRule(r);
            Rules.Add(r);
            sorted = false;
            return this;
        }

        #endregion 内部

        #region 稳定排序实现

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
            StableSortItem<T>[] sortList = new StableSortItem<T>[list.Count];
            for (int i = 0; i < sortList.Length; i++)
            {
                sortList[i] = new StableSortItem<T>(i, list[i]);
            }
            Array.Sort<StableSortItem<T>>(sortList, delegate(StableSortItem<T> a, StableSortItem<T> b)
            {
                int r = comp(a.Value, b.Value);
                if (r == 0)
                {
                    r = a.Index - b.Index;
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

        /// <summary>
        /// 稳定排序使用的集合元素,只是简单的记录原始集合的元素顺序
        /// </summary>
        /// <typeparam name="T"></typeparam>
        struct StableSortItem<T>
        {
            public StableSortItem(int index, T val)
                : this()
            {
                Index = index;
                Value = val;
            }

            public int Index { get; set; }

            public T Value { get; set; }
        }

        #endregion 稳定排序实现

        #region 实现IList接口

        public IEnumerator<Rule> GetEnumerator()
        {
            return Rules.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count
        {
            get
            {
                return Rules != null ? Rules.Count : 0;
            }
        }

        public int IndexOf(Rule item)
        {
            return Rules.IndexOf(item);
        }

        [Obsolete("请使用Route方法系列或Load方法", true)]
        public void Insert(int index, Rule item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            Rules.RemoveAt(index);
        }

        public Rule this[int index]
        {
            get
            {
                return Rules[index];
            }
            set
            {
                Rules[index] = value;
            }
        }

        [Obsolete("请使用Route方法系列或Load方法", true)]
        public void Add(Rule item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            Rules.Clear();
        }

        public bool Contains(Rule item)
        {
            return Rules.Contains(item);
        }

        public void CopyTo(Rule[] array, int arrayIndex)
        {
            Rules.CopyTo(array, arrayIndex);
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

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