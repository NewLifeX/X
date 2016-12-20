using System;
using System.Collections.Generic;

namespace NewLife.Queue.Protocols
{
    [Serializable]
    public class BatchMessageStoreResult
    {
        public string Topic { get; private set; }
        public int QueueId { get; private set; }
        public IEnumerable<BatchMessageItemResult> MessageResults { get; private set; }

        public BatchMessageStoreResult() { }
        public BatchMessageStoreResult(string topic, int queueId, IEnumerable<BatchMessageItemResult> messageResults)
        {
            Topic = topic;
            QueueId = queueId;
            MessageResults = messageResults;
        }

        public override string ToString()
        {
            return string.Format("[Topic:{0}, QueueId:{1}, MessageResults:[{2}]]",
                Topic,
                QueueId,
                string.Join(",", MessageResults));
        }
    }
}
