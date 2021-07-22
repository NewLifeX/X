using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NewLife.Caching
{
    /// <summary>轻量级生产者消费者接口</summary>
    /// <remarks>
    /// 不一定支持Ack机制；也不支持消息体与消息键分离
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public interface IProducerConsumer<T>
    {
        /// <summary>元素个数</summary>
        Int32 Count { get; }

        /// <summary>集合是否为空</summary>
        Boolean IsEmpty { get; }

        /// <summary>生产添加</summary>
        /// <param name="values"></param>
        /// <returns></returns>
        Int32 Add(params T[] values);

        /// <summary>消费获取一批</summary>
        /// <param name="count"></param>
        /// <returns></returns>
        IEnumerable<T> Take(Int32 count = 1);

        /// <summary>消费获取一个</summary>
        /// <param name="timeout">超时。默认0秒，永久等待</param>
        /// <returns></returns>
        T TakeOne(Int32 timeout = 0);

        /// <summary>异步消费获取一个</summary>
        /// <param name="timeout">超时。默认0秒，永久等待</param>
        /// <returns></returns>
        Task<T> TakeOneAsync(Int32 timeout = 0);

        /// <summary>确认消费</summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        Int32 Acknowledge(params String[] keys);
    }
}