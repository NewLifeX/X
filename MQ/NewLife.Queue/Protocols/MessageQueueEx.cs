using System;

namespace NewLife.Queue.Protocols
{
    [Serializable]
    public class MessageQueueEx : MessageQueue
    {
        public int ClientCachedMessageCount { get; set; }

        public MessageQueueEx() : base() { }
        public MessageQueueEx(string brokerName, string topic, int queueId) : base(brokerName, topic, queueId) { }

        public override string ToString()
        {
            return string.Format("[BrokerName={0}, Topic={1}, QueueId={2}, ClientCachedMessageCount={3}]", BrokerName, Topic, QueueId, ClientCachedMessageCount);
        }
    }
}
