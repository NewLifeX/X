using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using NewLife.Exceptions;
using NewLife.Reflection;

namespace NewLife.Model
{
    /// <summary>对象容器</summary>
    /// <remarks>
    /// 1，如果容器里面没有这个类型，则直接的创建对象返回
    /// 2，如果容器里面包含这个类型，并且指向的实例不为空，则返回
    /// 3，如果容器里面包含这个类型，并且指向的实例为空，则创建对象返回
    /// 4，如果有带参数构造函数，则从容器内获取各个参数的实例，最后创建对象返回
    /// 
    /// 这里有一点跟我们以往的想法非常不同，我们都习惯没有对象的时候，创建并加入字典。
    /// 这里采用两种方式，注册类型的时候，如果指定了实例，则表示这个类型对应单一的实例；
    /// 如果不指定实例，则表示支持该类型，每次创建。
    /// </remarks>
    public class ObjectContaner : IObjectContainer
    {
        #region 当前静态对象容器
        private static IObjectContainer _Current = new ObjectContaner();
        /// <summary>当前容器</summary>
        public static IObjectContainer Current
        {
            get { return _Current; }
            set { _Current = value; }
        }
        #endregion

        #region 父容器
        private IObjectContainer _Parent;
        /// <summary>父容器</summary>
        public virtual IObjectContainer Parent
        {
            get { return _Parent; }
            protected set { _Parent = value; }
        }

        private List<IObjectContainer> _Childs;
        /// <summary>子容器</summary>
        protected virtual IList<IObjectContainer> Childs
        {
            get { return _Childs ?? (_Childs = new List<IObjectContainer>()); }
        }

        /// <summary>
        /// 移除所有子容器
        /// </summary>
        /// <returns></returns>
        public virtual IObjectContainer RemoveAllChildContainers()
        {
            if (_Childs != null) _Childs.Clear();

            return this;
        }

        /// <summary>
        /// 创建子容器
        /// </summary>
        /// <returns></returns>
        public virtual IObjectContainer CreateChildContainer()
        {
            IObjectContainer container = TypeX.CreateInstance(this.GetType()) as IObjectContainer;
            if (container is ObjectContaner)
                (container as ObjectContaner).Parent = this;
            else
            {
                PropertyInfo pi = this.GetType().GetProperty("Parent", BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pi != null && pi.CanWrite) pi.SetValue(container, this, null);

            }
            Childs.Add(container);

            return container;
        }
        #endregion

        #region 对象字典
        private Dictionary<Type, Dictionary<String, Object>> _stores = null;
        private Dictionary<Type, Dictionary<String, Object>> Stores { get { return _stores ?? (_stores = new Dictionary<Type, Dictionary<String, Object>>()); } }

        private Dictionary<String, Object> Find(Type type, Boolean add)
        {
            Dictionary<String, Object> dic = null;
            if (Stores.TryGetValue(type, out dic)) return dic;

            if (add)
            {
                lock (Stores)
                {
                    if (Stores.TryGetValue(type, out dic)) return dic;

                    dic = new Dictionary<string, object>();
                    Stores.Add(type, dic);
                    return dic;
                }
            }

            return null;
        }
        #endregion

        #region 注册
        /// <summary>
        /// 注册类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual IObjectContainer RegisterType(Type type) { return RegisterType(type, null); }

        /// <summary>
        /// 注册类型和名称
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual IObjectContainer RegisterType(Type type, String name)
        {
            Dictionary<String, Object> dic = Find(type, true);
            if (!dic.ContainsKey(name)) dic.Add(name, null);

            return this;
        }

        /// <summary>
        /// 注册类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual IObjectContainer RegisterType<T>() { return RegisterType(typeof(T), null); }

        /// <summary>
        /// 注册类型和名称
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual IObjectContainer RegisterType<T>(String name) { return RegisterType(typeof(T), name); }

        /// <summary>
        /// 注册类型的实例
        /// </summary>
        /// <param name="type"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        public virtual IObjectContainer RegisterInstance(Type type, Object instance) { return RegisterInstance(type, null, instance); }

        /// <summary>
        /// 注册类型指定名称的实例
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        public virtual IObjectContainer RegisterInstance(Type type, String name, Object instance)
        {
            Dictionary<String, Object> dic = Find(type, true);
            if (dic.ContainsKey(name))
                dic[name] = instance;
            else
                dic.Add(name, instance);

            return this;
        }

        /// <summary>
        /// 注册类型的实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <returns></returns>
        public virtual IObjectContainer RegisterInstance<T>(Object instance) { return RegisterInstance(typeof(T), null, instance); }

        /// <summary>
        /// 注册类型指定名称的实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        public virtual IObjectContainer RegisterInstance<T>(String name, Object instance) { return RegisterInstance(typeof(T), name, instance); }
        #endregion

        #region 解析
        /// <summary>
        /// 解析类型的实例
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual Object Resolve(Type type) { return Resolve(type, null); }

        /// <summary>
        /// 解析类型指定名称的实例
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual Object Resolve(Type type, String name)
        {
            Dictionary<String, Object> dic = Find(type, false);
            // 1，如果容器里面没有这个类型，则直接的创建对象返回
            //if (dic == null) return Activator.CreateInstance(type, false);
            if (dic == null) return TypeX.CreateInstance(type);

            Object obj = null;
            // 2，如果容器里面包含这个类型，并且指向的实例不为空，则返回
            if (dic.TryGetValue(name, out obj)) return obj;

            // 3，如果容器里面包含这个类型，并且指向的实例为空，则创建对象返回
            // 4，如果有带参数构造函数，则从容器内获取各个参数的实例，最后创建对象返回
            ConstructorInfo[] cis = type.GetConstructors();
            if (cis.Length <= 0)
                obj = TypeX.CreateInstance(type);
            else if (cis.Length == 1)
            {
                List<Object> ps = new List<Object>();
                foreach (ParameterInfo pi in cis[0].GetParameters())
                {
                    dic = Find(pi.ParameterType, false);
                    if (dic != null && dic.Count > 0)
                        ps.Add(Resolve(pi.ParameterType));
                    else
                        ps.Add(null);
                }
                obj = ConstructorInfoX.Create(cis[0]).CreateInstance(ps.ToArray());
            }
            else
                throw new XException("目标对象有多个构造函数，容器无法选择！");

            // 赋值注入
            foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(obj))
            {
                if (!pd.IsReadOnly && Stores.ContainsKey(pd.PropertyType)) pd.SetValue(obj, Resolve(pd.PropertyType));
            }

            return null;
        }

        /// <summary>
        /// 解析类型的实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual T Resolve<T>() { return (T)Resolve(typeof(T), null); }

        /// <summary>
        /// 解析类型指定名称的实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual T Resolve<T>(String name) { return (T)Resolve(typeof(T), name); }

        /// <summary>
        /// 解析类型所有已注册的实例
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual IEnumerable<Object> ResolveAll(Type type)
        {
            Dictionary<String, Object> dic = Find(type, false);
            if (dic == null) yield break;

            foreach (Object item in dic.Values)
            {
                yield return item;
            }
        }

        /// <summary>
        /// 解析类型所有已注册的实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual IEnumerable<T> ResolveAll<T>()
        {
            Dictionary<String, Object> dic = Find(typeof(T), false);
            if (dic == null) yield break;

            foreach (Object item in dic.Values)
            {
                yield return (T)item;
            }
        }
        #endregion

        #region 注释
        //private static Dictionary<string, object> CreateConstructorParameter(Type type)
        //{
        //    Dictionary<string, object> paramArray = new Dictionary<string, object>();

        //    ConstructorInfo[] cis = type.GetConstructors();
        //    if (cis.Length > 1) throw new Exception("目标对象有多个构造函数，容器无法选择！");

        //    foreach (ParameterInfo pi in cis[0].GetParameters())
        //    {
        //        if (Stores.ContainsKey(pi.ParameterType)) paramArray.Add(pi.Name, GetInstance(pi.ParameterType));
        //    }
        //    return paramArray;
        //}

        //public static object GetInstance(Type type)
        //{
        //    /* 这里是重点！
        //     * 1，如果容器里面没有这个类型，则直接的创建对象返回
        //     * 2，如果有带参数构造函数，则从容器内获取各个参数的实例，最后创建对象返回
        //     * 3，如果容器里面包含这个类型，并且指向的实例不为空，则返回
        //     * 4，如果容器里面包含这个类型，并且指向的实例为空，则创建对象返回
        //     * 
        //     * 这里有一点跟我以往的想法非常不同，我都习惯没有对象的时候，创建并加入字典。
        //     * 这里采用两种方式，注册类型的时候，如果指定了实例，则表示这个类型对应单一的实例；
        //     * 如果不指定实例，则表示支持该类型，每次创建。
        //     */

        //    Object obj = null;
        //    if (!Stores.TryGetValue(type, out obj)) return Activator.CreateInstance(type, false);

        //    // 构造函数注入
        //    ConstructorInfo[] cis = type.GetConstructors();
        //    if (cis.Length != 0)
        //    {
        //        Dictionary<string, object> paramArray = CreateConstructorParameter(type);
        //        List<object> cArray = new List<object>();
        //        foreach (ParameterInfo pi in cis[0].GetParameters())
        //        {
        //            if (paramArray.ContainsKey(pi.Name))
        //                cArray.Add(paramArray[pi.Name]);
        //            else
        //                cArray.Add(null);
        //        }
        //        return cis[0].Invoke(cArray.ToArray());
        //    }
        //    else if (obj != null)
        //        return obj;
        //    else
        //    {
        //        obj = Activator.CreateInstance(type, false);
        //        // 赋值注入
        //        foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(obj))
        //        {
        //            if (!pd.IsReadOnly && Stores.ContainsKey(pd.PropertyType)) pd.SetValue(obj, GetInstance(pd.PropertyType));
        //        }
        //        return obj;
        //    }
        //}

        //public static void Register(Type type, object obj)
        //{
        //    if (Stores.ContainsKey(type))
        //        Stores[type] = obj;
        //    else
        //        Stores.Add(type, obj);
        //}

        //public static void Register(Type type)
        //{
        //    if (!Stores.ContainsKey(type)) Stores.Add(type, null);
        //}
        #endregion
    }
}