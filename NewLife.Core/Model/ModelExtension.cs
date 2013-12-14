using System;

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
            return (T)provider.GetService(typeof(T));
        }
    }
}