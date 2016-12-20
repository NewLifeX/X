using System;

namespace NewLife.Queue.Protocols
{
    [Serializable]
    public class MessageQueue
    {
        public string BrokerName { get; private set; }
        public string Topic { get; private set; }
        public int QueueId { get; private set; }

        public MessageQueue() { }
        public MessageQueue(string brokerName, string topic, int queueId)
        {
            BrokerName = brokerName;
            Topic = topic;
            QueueId = queueId;
        }

        public override string ToString()
        {
            return string.Format("[BrokerName={0}, Topic={1}, QueueId={2}]", BrokerName, Topic, QueueId);
        }
    }
}
