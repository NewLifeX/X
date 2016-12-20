using System;

namespace NewLife.Queue.Protocols
{
    [Serializable]
    public class MessageStoreResult
    {
        public string MessageId { get; private set; }
        public int Code { get; private set; }
        public string Topic { get; private set; }
        public string Tag { get; private set; }
        public int QueueId { get; private set; }
        public long QueueOffset { get; private set; }
        public DateTime CreatedTime { get; private set; }
        public DateTime StoredTime { get; private set; }

        public MessageStoreResult() { }
        public MessageStoreResult(string messageId, int code, string topic, int queueId, long queueOffset, DateTime createdTime, DateTime storedTime, string tag = null)
        {
            MessageId = messageId;
            Code = code;
            Topic = topic;
            Tag = tag;
            QueueId = queueId;
            QueueOffset = queueOffset;
            CreatedTime = createdTime;
            StoredTime = storedTime;
        }

        public override string ToString()
        {
            return string.Format("[MessageId:{0}, Code:{1}, Topic:{2}, QueueId:{3}, QueueOffset:{4}, Tag:{5}, CreatedTime:{6}, StoredTime:{7}]",
                MessageId,
                Code,
                Topic,
                QueueId,
                QueueOffset,
                Tag,
                CreatedTime,
                StoredTime);
        }
    }
}
