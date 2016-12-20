using System;

namespace NewLife.Queue.Protocols.Brokers
{
    [Serializable]
    public class TopicQueueInfo
    {
        /// <summary>主题
        /// </summary>
        public string Topic { get; set; }
        /// <summary>队列ID
        /// </summary>
        public int QueueId { get; set; }
        /// <summary>队列当前最大Offset
        /// </summary>
        public long QueueCurrentOffset { get; set; }
        /// <summary>队列当前最小Offset
        /// </summary>
        public long QueueMinOffset { get; set; }
        /// <summary>队列当前被所有消费者都消费了的最小Offset
        /// </summary>
        public long QueueMinConsumedOffset { get; set; }
        /// <summary>队列当前未被消费的消息个数
        /// </summary>
        public long QueueNotConsumeCount { get; set; }
        /// <summary>对生产者是否可见
        /// </summary>
        public bool ProducerVisible { get; set; }
        /// <summary>对消费者是否可见
        /// </summary>
        public bool ConsumerVisible { get; set; }
        /// <summary>发送消息的吞吐，每1s统计一次
        /// </summary>
        public long SendThroughput { get; set; }

        /// <summary>计算队列未消费的消息数，即队列的消息堆积数
        /// </summary>
        /// <returns></returns>
        public long CalculateQueueNotConsumeCount()
        {
            if (QueueMinConsumedOffset >= 0)
            {
                return QueueCurrentOffset - QueueMinConsumedOffset;
            }
            else if (QueueCurrentOffset >= 0 && QueueMinOffset >= 0)
            {
                return QueueCurrentOffset - QueueMinOffset + 1;
            }
            return 0;
        }
    }
}
