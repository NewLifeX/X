using System;
using NewLife.Reflection;

#if !__CORE__
namespace NewLife.Model
{
    /// <summary>模型扩展</summary>
    public static class ModelExtension
    {
        /// <summary>获取指定类型的服务对象</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static T GetService<T>(this IServiceProvider provider)
        {
            if (provider == null) return default;

            //// 服务类是否当前类的基类
            //if (provider.GetType().As<T>()) return (T)provider;

            return (T)provider.GetService(typeof(T));
        }

        /// <summary>获取必要的服务，不存在时抛出异常</summary>
        /// <param name="provider"></param>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public static Object GetRequiredService(this IServiceProvider provider, Type serviceType)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

            return provider.GetService(serviceType) ?? throw new InvalidOperationException($"未注册类型{serviceType.FullName}");
        }

        /// <summary>获取必要的服务，不存在时抛出异常</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static T GetRequiredService<T>(this IServiceProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            return (T)provider.GetRequiredService(typeof(T));
        }
    }
}
#endif