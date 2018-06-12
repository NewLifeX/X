using System;

namespace NewLife.Collections
{
    /// <summary>对象池接口</summary>
    /// <typeparam name="T"></typeparam>
    public interface IPool<T> where T : class
    {
        /// <summary>借出</summary>
        /// <param name="msTimeout">池满时等待的最大超时时间。超时后仍无法得到资源将抛出异常</param>
        /// <returns></returns>
        T Get(Int32 msTimeout = 0);

        /// <summary>归还</summary>
        /// <param name="value"></param>
        void Return(T value);

        /// <summary>清空已有对象</summary>
        void Clear();
    }
}