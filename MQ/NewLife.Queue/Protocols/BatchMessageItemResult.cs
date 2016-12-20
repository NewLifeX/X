using System;

namespace NewLife.Queue.Protocols
{
    [Serializable]
    public class BatchMessageItemResult
    {
        public string MessageId { get; set; }
        public int Code { get; set; }
        public long QueueOffset { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime StoredTime { get; set; }
        public string Tag { get; set; }

        public BatchMessageItemResult() { }
        public BatchMessageItemResult(string messageId, int code, long queueOffset, DateTime createdTime, DateTime storedTime, string tag)
        {
            MessageId = messageId;
            Code = code;
            QueueOffset = queueOffset;
            CreatedTime = createdTime;
            StoredTime = storedTime;
            Tag = tag;
        }

        public override string ToString()
        {
            return string.Format("[MessageId={0},Code={1},QueueOffset={2},CreatedTime={3},StoredTime={4},Tag={5}]", MessageId, Code, QueueOffset, CreatedTime, StoredTime, Tag);
        }
    }
}
