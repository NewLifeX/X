using System;
using System.Collections.Generic;

namespace NewLife.Caching
{
    /// <summary>生产者消费者接口</summary>
    /// <typeparam name="T"></typeparam>
    public interface IProducerConsumer<T>
    {
        /// <summary>生产添加</summary>
        /// <param name="values"></param>
        /// <returns></returns>
        Int32 Add(IEnumerable<T> values);

        /// <summary>消费获取</summary>
        /// <param name="count"></param>
        /// <returns></returns>
        IEnumerable<T> Take(Int32 count = 1);
    }
}