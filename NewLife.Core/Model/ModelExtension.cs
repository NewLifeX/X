using System;
using NewLife.Reflection;

namespace System
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
            if (provider == null) return default(T);

            // 服务类是否当前类的基类
            if (provider.GetType().As<T>()) return (T)provider;

            return (T)provider.GetService(typeof(T));
        }
    }

    //#if __CORE__
    //    /// <summary>定义用于检索服务对象的机制；也即，向其他对象提供自定义支持的对象。</summary>
    //    public interface IServiceProvider
    //    {
    //        /// <summary>获取指定类型的服务对象。</summary>
    //        /// <param name="serviceType"></param>
    //        /// <returns></returns>
    //        Object GetService(Type serviceType);
    //    }
    //#endif
}