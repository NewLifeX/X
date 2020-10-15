using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NewLife.Caching;

namespace NewLife.Collections
{
    /// <summary>主动式消息服务</summary>
    /// <typeparam name="T">数据类型</typeparam>
    public interface IQueueService<T>
    {
        /// <summary>发布消息</summary>
        /// <param name="topic">主题</param>
        /// <param name="value">消息</param>
        /// <returns></returns>
        Int32 Public(String topic, T value);

        /// <summary>订阅</summary>
        /// <param name="clientId">客户标识</param>
        /// <param name="topic">主题</param>
        Boolean Subscribe(String clientId, String topic);

        /// <summary>取消订阅</summary>
        /// <param name="clientId">客户标识</param>
        /// <param name="topic">主题</param>
        Boolean UnSubscribe(String clientId, String topic);

        /// <summary>消费消息</summary>
        /// <param name="clientId">客户标识</param>
        /// <param name="topic">主题</param>
        /// <param name="count">要拉取的消息数</param>
        /// <returns></returns>
        T[] Consume(String clientId, String topic, Int32 count);
    }

    /// <summary>轻量级主动式消息服务</summary>
    /// <typeparam name="T">数据类型</typeparam>
    public class QueueService<T> : IQueueService<T>
    {
        #region 属性
        /// <summary>数据存储</summary>
        public ICache Cache { get; set; } = MemoryCache.Instance;

        /// <summary>每个主题的所有订阅者</summary>
        private readonly ConcurrentDictionary<String, ConcurrentDictionary<String, IProducerConsumer<T>>> _topics = new ConcurrentDictionary<String, ConcurrentDictionary<String, IProducerConsumer<T>>>();
        #endregion

        #region 方法
        /// <summary>发布消息</summary>
        /// <param name="topic">主题</param>
        /// <param name="value">消息</param>
        /// <returns></returns>
        public Int32 Public(String topic, T value)
        {
            var rs = 0;
            if (_topics.TryGetValue(topic, out var clients))
            {
                // 向每个订阅者推送
                foreach (var item in clients)
                {
                    var queue = item.Value;
                    rs += queue.Add(new[] { value });
                }
            }

            return rs;
        }

        /// <summary>订阅</summary>
        /// <param name="clientId">客户标识</param>
        /// <param name="topic">主题</param>
        public Boolean Subscribe(String clientId, String topic)
        {
            var dic = _topics.GetOrAdd(topic, k => new ConcurrentDictionary<String, IProducerConsumer<T>>());
            if (dic.ContainsKey(clientId)) return false;

            // 创建队列
            var queue = Cache.GetQueue<T>($"{topic}_{clientId}");
            return dic.TryAdd(clientId, queue);
        }

        /// <summary>取消订阅</summary>
        /// <param name="clientId">客户标识</param>
        /// <param name="topic">主题</param>
        public Boolean UnSubscribe(String clientId, String topic)
        {
            if (_topics.TryGetValue(topic, out var clients))
            {
                return clients.TryRemove(clientId, out _);
            }

            return false;
        }

        /// <summary>消费消息</summary>
        /// <param name="clientId">客户标识</param>
        /// <param name="topic">主题</param>
        /// <param name="count"></param>
        /// <returns></returns>
        public T[] Consume(String clientId, String topic, Int32 count)
        {
            if (_topics.TryGetValue(topic, out var clients))
            {
                if (clients.TryGetValue(clientId, out var queue))
                {
                    return queue.Take(count).ToArray();
                }
            }

            return new T[0];
        }
        #endregion
    }
}