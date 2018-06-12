using System;

namespace NewLife.Collections
{
    /// <summary>对象池接口</summary>
    /// <typeparam name="T"></typeparam>
    public interface IPool<T> where T : class
    {
        /// <summary>借出</summary>
        /// <returns></returns>
        T Get();

        /// <summary>归还</summary>
        /// <param name="value"></param>
        void Return(T value);

        /// <summary>清空已有对象</summary>
        void Clear();
    }
}