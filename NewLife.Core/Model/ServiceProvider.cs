using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Design;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Model
{
    /// <summary>
    /// 服务对象提供者，优先查找从构造函数传入的外部提供者，然后是全局的Current，最后才是当前提供者。
    /// 支持枚举提供者内部注册的服务类型。
    /// </summary>
    public class ServiceProvider : IServiceProvider, IEnumerable<Type>
    {
        #region 构造
        IServiceProvider _provider;

        /// <summary>
        /// 实例化
        /// </summary>
        public ServiceProvider() { LoadConfig(); }

        /// <summary>
        /// 通过指定一个基础提供者来实例化一个新的提供者，优先基础提供者
        /// </summary>
        /// <param name="provider"></param>
        public ServiceProvider(IServiceProvider provider) : this() { _provider = provider; }
        #endregion

        #region 静态方法
        /// <summary>
        /// 获取服务对象的泛型实现，能够层层深入，处理无限层嵌套的服务提供者
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static TService GetService<TService>(IServiceProvider provider)
        {
            if (provider == null) return default(TService);

            //Object obj = provider.GetService(typeof(TService));
            //if (obj != null) return (TService)obj;

            //// 递归处理内层服务提供者。不知道会不会因为服务提供者相互嵌套而造成死循环
            //IServiceProvider isp = provider.GetService(typeof(IServiceProvider)) as IServiceProvider;
            //return isp == null ? default(TService) : GetService<TService>(isp);

            return (TService)GetService(provider, typeof(TService));
        }

        [ThreadStatic]
        private static List<IServiceProvider> sps;
        /// <summary>
        /// 获取服务对象，能够层层深入，处理无限层嵌套的服务提供者
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public static Object GetService(IServiceProvider provider, Type serviceType)
        {
            if (provider == null) return null;
            if (serviceType == null) throw new ArgumentNullException("serviceType");

            if (serviceType == typeof(IServiceProvider)) return provider;

            // 通过线程安全（线程静态）的sps，来测底的避免相互嵌套形成死循环
            // 如果sps不为空，表明当前线程正处于GetService中，不允许进入！
            if (sps != null) return null;

            try
            {
                // 采用循环处理，避免因为服务提供者相互嵌套而导致死循环
                sps = new List<IServiceProvider>();
                sps.Add(provider);
                for (int i = 0; i < sps.Count; i++)
                {
                    IServiceProvider sp = sps[i];

                    Object obj = provider.GetService(serviceType);
                    if (obj != null) return obj;

                    sp = provider.GetService(typeof(IServiceProvider)) as IServiceProvider;
                    if (sp != null && !sps.Contains(sp)) sps.Add(sp);
                }

                return null;
            }
            finally { sps = null; }
        }
        #endregion

        #region 默认提供者
        private static IServiceProvider _Current;
        /// <summary>默认服务对象提供者</summary>
        public static IServiceProvider Current
        {
            get
            {
                if (_Current == null)
                {
                    // 从配置文件那默认提供者
                    String name = Config.GetConfig<String>("NewLife.ServiceProvider");
                    if (!String.IsNullOrEmpty(name))
                    {
                        Type type = TypeX.GetType(name);
                        if (type != null && typeof(IServiceProvider).IsAssignableFrom(type))
                        {
                            _Current = TypeX.CreateInstance(type) as IServiceProvider;

                            if (type != typeof(ServiceProvider)) _Current = new ServiceProvider(_Current);
                        }
                    }
                    if (_Current == null) _Current = new ServiceProvider();
                }
                return _Current;
            }
            set { _Current = value; }
        }

        /// <summary>
        /// 使用默认提供者获取服务对象的泛型实现
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        public static TService GetCurrentService<TService>()
        {
            return GetService<TService>(Current);
        }
        #endregion

        #region 注册
        //TODO 非常需要一个线程安全，而又有很高性能的字典
        private IDictionary<Type, Object> dic = new Dictionary<Type, Object>();
        /// <summary>
        /// 注册服务对象
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="obj"></param>
        void Register(Type serviceType, Object obj)
        {
            if (serviceType == null) throw new ArgumentNullException("serviceType");

            lock (dic)
            {
                if (obj != null)
                {
                    if (dic.ContainsKey(serviceType))
                        dic[serviceType] = obj;
                    else
                        dic.Add(serviceType, obj);
                }
                else
                {
                    if (dic.ContainsKey(serviceType)) dic.Remove(serviceType);
                }
            }
        }

        /// <summary>
        /// 取消注册服务对象
        /// </summary>
        /// <param name="serviceType"></param>
        void UnRegister(Type serviceType)
        {
            if (serviceType == null) throw new ArgumentNullException("serviceType");

            Register(serviceType, null);
        }

        void LoadConfig()
        {
            var ps = Config.GetConfigByPrefix("NewLife.ServiceProvider_");
            if (ps == null || ps.Count < 1) return;

            foreach (String item in ps.Keys)
            {
                try
                {
                    Type serviceType = TypeX.GetType(item);

                    Type objType = null;
                    String typeName = ps[item];
                    if (!String.IsNullOrEmpty(typeName)) objType = TypeX.GetType(typeName);

                    Register(serviceType, objType == null ? null : TypeX.CreateInstance(objType));
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
            }
        }
        #endregion

        #region IServiceProvider 成员
        /// <summary>
        /// 获取服务对象
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public virtual object GetService(Type serviceType)
        {
            if (serviceType == null) throw new ArgumentNullException("serviceType");

            if (serviceType == typeof(IServiceProvider)) return this;

            if (_provider != null)
            {
                // 调用静态GetService，避免死循环
                Object obj = GetService(_provider, serviceType);
                if (obj != null) return obj;
            }

            // 内部列表
            {
                Object obj = null;
                if (dic.TryGetValue(serviceType, out obj)) return obj;
            }

            // 如果当前类不是ServiceProvider，则采用NewLife.ServiceProvider
            if (this != Current && _provider != Current)
            {
                Object obj = GetService(Current, serviceType);
                if (obj != null) return obj;
            }

            if (serviceType == typeof(ITypeDiscoveryService)) return new TypeDiscoveryService();
            if (serviceType == typeof(ITypeResolutionService)) return new TypeResolutionService();

            return null;
        }

        /// <summary>
        /// 获取服务对象的泛型实现
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        public TService GetService<TService>()
        {
            Object obj = GetService(typeof(TService));
            if (obj == null) return default(TService);
            return (TService)obj;
        }
        #endregion

        #region IEnumerable<Type> 成员
        /// <summary>
        /// 获取迭代器
        /// </summary>
        /// <returns></returns>
        IEnumerator<Type> IEnumerable<Type>.GetEnumerator()
        {
            return dic.Keys.GetEnumerator();
        }
        #endregion

        #region IEnumerable 成员
        /// <summary>
        /// 获取迭代器
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (this as IEnumerable<Type>).GetEnumerator();
        }
        #endregion
    }
}