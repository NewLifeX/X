using System;
using System.Collections.Generic;
using System.Globalization;

namespace NewLife.Model
{
    /// <summary>本类为应用程序提供服务定位。</summary>
    public class ServiceLocator : IServiceLocator
    {
        #region 当前静态服务定位
        private static IServiceLocator _Current = new ServiceLocator();
        /// <summary>当前容器</summary>
        public static IServiceLocator Current
        {
            get { return _Current; }
            set { _Current = value; }
        }
        #endregion

        #region 服务定位接口
        /// <summary>
        /// <see cref="IServiceProvider.GetService"/> 实现
        /// </summary>
        /// <param name="serviceType">请求的服务类型。</param>
        /// <returns>请求的对象。</returns>
        public virtual object GetService(Type serviceType)
        {
            return GetInstance(serviceType, null);
        }

        /// <summary>
        /// 获取 <paramref name="serviceType"/> 的一个实例
        /// </summary>
        /// <param name="serviceType">请求的服务类型。</param>
        /// <returns>请求的服务对象。</returns>
        public virtual object GetInstance(Type serviceType)
        {
            return GetInstance(serviceType, null);
        }

        /// <summary>
        /// 获取 <paramref name="serviceType"/> 中指定 <paramref name="key"/> 的一个实例
        /// </summary>
        /// <param name="serviceType">请求的服务类型。</param>
        /// <param name="key">注册的服务名称。</param>
        /// <returns>请求的服务对象。</returns>
        public virtual object GetInstance(Type serviceType, string key)
        {
            try
            {
                return DoGetInstance(serviceType, key);
            }
            catch (Exception ex)
            {
                throw new Exception(FormatExceptionMessage(ex, serviceType, key), ex);
            }
        }

        /// <summary>
        /// 获取已注册的所有 <paramref name="serviceType"/> 实例。
        /// </summary>
        /// <param name="serviceType">请求的服务类型。</param>
        /// <returns>请求的服务对象序列。</returns>
        public virtual IEnumerable<object> GetAllInstances(Type serviceType)
        {
            try
            {
                return DoGetAllInstances(serviceType);
            }
            catch (Exception ex)
            {
                throw new Exception(FormatActivateAllExceptionMessage(ex, serviceType), ex);
            }
        }

        /// <summary>
        /// 获取 <typeparamref name="TService"/> 的一个实例
        /// </summary>
        /// <typeparam name="TService">Type of object requested.</typeparam>
        /// <exception cref="Exception">if there is are errors resolving
        /// the service instance.</exception>
        /// <returns>The requested service instance.</returns>
        public virtual TService GetInstance<TService>()
        {
            return (TService)GetInstance(typeof(TService), null);
        }

        /// <summary>
        /// Get an instance of the given named <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="TService">Type of object requested.</typeparam>
        /// <param name="key">Name the object was registered with.</param>
        /// <exception cref="Exception">if there is are errors resolving
        /// the service instance.</exception>
        /// <returns>The requested service instance.</returns>
        public virtual TService GetInstance<TService>(string key)
        {
            return (TService)GetInstance(typeof(TService), key);
        }

        /// <summary>
        /// Get all instances of the given <typeparamref name="TService"/> currently
        /// registered in the container.
        /// </summary>
        /// <typeparam name="TService">Type of object requested.</typeparam>
        /// <exception cref="Exception">if there is are errors resolving
        /// the service instance.</exception>
        /// <returns>A sequence of instances of the requested <typeparamref name="TService"/>.</returns>
        public virtual IEnumerable<TService> GetAllInstances<TService>()
        {
            foreach (object item in GetAllInstances(typeof(TService)))
            {
                yield return (TService)item;
            }
        }
        #endregion

        #region 服务容器
        /// <summary>
        /// When implemented by inheriting classes, this method will do the actual work of resolving
        /// the requested service instance.
        /// </summary>
        /// <param name="serviceType">Type of instance requested.</param>
        /// <param name="key">Name of registered service you want. May be null.</param>
        /// <returns>The requested service instance.</returns>
        protected virtual Object DoGetInstance(Type serviceType, string key)
        {
            return ObjectContaner.Current.Resolve(serviceType, key);
        }

        /// <summary>
        /// When implemented by inheriting classes, this method will do the actual work of
        /// resolving all the requested service instances.
        /// </summary>
        /// <param name="serviceType">Type of service requested.</param>
        /// <returns>Sequence of service instance objects.</returns>
        protected virtual IEnumerable<Object> DoGetAllInstances(Type serviceType)
        {
            //throw new NotImplementedException();
            return ObjectContaner.Current.ResolveAll(serviceType);
        }
        #endregion

        #region 格式化异常
        /// <summary>
        /// Format the exception message for use in an <see cref="Exception"/>
        /// that occurs while resolving a single service.
        /// </summary>
        /// <param name="actualException">The actual exception thrown by the implementation.</param>
        /// <param name="serviceType">Type of service requested.</param>
        /// <param name="key">Name requested.</param>
        /// <returns>The formatted exception message string.</returns>
        protected virtual string FormatExceptionMessage(Exception actualException, Type serviceType, string key)
        {
            return string.Format(CultureInfo.CurrentUICulture, "Activation error occured while trying to get instance of type {0}, key \"{1}\"", serviceType.Name, key);
        }

        /// <summary>
        /// Format the exception message for use in an <see cref="Exception"/>
        /// that occurs while resolving multiple service instances.
        /// </summary>
        /// <param name="actualException">The actual exception thrown by the implementation.</param>
        /// <param name="serviceType">Type of service requested.</param>
        /// <returns>The formatted exception message string.</returns>
        protected virtual string FormatActivateAllExceptionMessage(Exception actualException, Type serviceType)
        {
            return string.Format(CultureInfo.CurrentUICulture, "Activation error occured while trying to get all instances of type {0}", serviceType.Name);
        }
        #endregion
    }
}