using System;

namespace NewLife.Collections
{
    /// <summary>对象池接口</summary>
    /// <typeparam name="T"></typeparam>
    public interface IPool<T> where T : class
    {
        /// <summary>获取</summary>
        /// <returns></returns>
        T Get();

        /// <summary>归还</summary>
        /// <param name="value"></param>
        Boolean Return(T value);

        /// <summary>清空</summary>
        Int32 Clear();
    }
}