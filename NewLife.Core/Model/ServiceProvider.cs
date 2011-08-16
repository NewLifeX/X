using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.Design;
using NewLife.Configuration;
using NewLife.Reflection;

namespace NewLife.Model
{
    /// <summary>
    /// 服务对象提供者
    /// </summary>
    public class ServiceProvider : IServiceProvider
    {
        #region 构造
        IServiceProvider _provider;

        /// <summary>
        /// 实例化
        /// </summary>
        public ServiceProvider() { }

        /// <summary>
        /// 通过指定一个基础提供者来实例化一个新的提供者，优先基础提供者
        /// </summary>
        /// <param name="provider"></param>
        public ServiceProvider(IServiceProvider provider) { _provider = provider; }
        #endregion

        #region 静态方法
        /// <summary>
        /// 获取服务对象的泛型实现
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static TService GetService<TService>(IServiceProvider provider)
        {
            if (provider == null) return default(TService);

            Object obj = provider.GetService(typeof(TService));
            if (obj == null) return default(TService);
            return (TService)obj;
        }
        #endregion

        #region 默认提供者
        private static IServiceProvider _Default;
        /// <summary>默认服务对象提供者</summary>
        public static IServiceProvider Default
        {
            get
            {
                if (_Default == null)
                {
                    // 从配置文件那默认提供者
                    String name = Config.GetConfig<String>("NewLife.ServiceProvider");
                    if (!String.IsNullOrEmpty(name))
                    {
                        Type type = TypeX.GetType(name);
                        if (type != null)
                        {
                            //_Default = TypeX.CreateInstance(type) as IServiceProvider;
                            Object obj = TypeX.CreateInstance(type);
                            // 有可能提供者没有实现IServiceProvider接口，我们用鸭子类型给它处理一下
                            _Default = TypeX.ChangeType<IServiceProvider>(obj);

                            if (type != typeof(ServiceProvider)) _Default = new ServiceProvider(_Default);
                        }
                    }
                    if (_Default == null) _Default = new ServiceProvider();
                }
                return _Default;
            }
            set { _Default = value; }
        }

        /// <summary>
        /// 使用默认提供者获取服务对象的泛型实现
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        public static TService GetDefaultService<TService>()
        {
            return GetService<TService>(Default);
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
            if (_provider != null)
            {
                Object obj = _provider.GetService(serviceType);
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
    }
}