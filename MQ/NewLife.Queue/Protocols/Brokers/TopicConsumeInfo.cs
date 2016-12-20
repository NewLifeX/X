using System;

namespace NewLife.Queue.Protocols.Brokers
{
    [Serializable]
    public class TopicConsumeInfo
    {
        /// <summary>消费者的分组
        /// </summary>
        public string ConsumerGroup { get; set; }
        /// <summary>主题
        /// </summary>
        public string Topic { get; set; }
        /// <summary>队列ID
        /// </summary>
        public int QueueId { get; set; }
        /// <summary>队列当前位置
        /// </summary>
        public long QueueCurrentOffset { get; set; }
        /// <summary>队列消费位置
        /// </summary>
        public long ConsumedOffset { get; set; }
        /// <summary>客户端缓存的消息树
        /// </summary>
        public int ClientCachedMessageCount { get; set; }
        /// <summary>未消费消息数，即消息堆积数
        /// </summary>
        public long QueueNotConsumeCount { get; set; }
        /// <summary>在线消费者个数
        /// </summary>
        public int OnlineConsumerCount { get; set; }
        /// <summary>消费消息的吞吐，每10s统计一次
        /// </summary>
        public long ConsumeThroughput { get; set; }

        /// <summary>计算队列未消费的消息数，即队列的消息堆积数
        /// </summary>
        /// <returns></returns>
        public long CalculateQueueNotConsumeCount()
        {
            if (ConsumedOffset >= 0)
            {
                return QueueCurrentOffset - ConsumedOffset;
            }
            else if (QueueCurrentOffset >= 0)
            {
                return QueueCurrentOffset + 1;
            }
            return 0;
        }
    }
}
