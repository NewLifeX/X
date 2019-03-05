using System;

namespace NewLife.Collections
{
    /// <summary>集群</summary>
    /// <typeparam name="T"></typeparam>
    public interface ICluster<T>
    {
        /// <summary>从集群中获取资源</summary>
        /// <param name="create">是否创建</param>
        /// <returns></returns>
        T Get(Boolean create);

        /// <summary>归还</summary>
        /// <param name="value"></param>
        Boolean Put(T value);
    }
}