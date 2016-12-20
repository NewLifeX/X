using System;

namespace NewLife.Queue.Protocols.Brokers.Requests
{
    [Serializable]
    public class UpdateQueueOffsetRequest
    {
        public string ConsumerGroup { get; set; }
        public MessageQueue MessageQueue { get; set; }
        public long QueueOffset { get; set; }

        public UpdateQueueOffsetRequest(string consumerGroup, MessageQueue messageQueue, long queueOffset)
        {
            ConsumerGroup = consumerGroup;
            MessageQueue = messageQueue;
            QueueOffset = queueOffset;
        }

        public override string ToString()
        {
            return string.Format("[ConsumerGroup:{0}, MessageQueue:{1}, QueueOffset:{2}]", ConsumerGroup, MessageQueue, QueueOffset);
        }
    }
}
