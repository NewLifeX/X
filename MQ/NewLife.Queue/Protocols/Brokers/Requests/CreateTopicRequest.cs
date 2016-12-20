using System;

namespace NewLife.Queue.Protocols.Brokers.Requests
{
    [Serializable]
    public class CreateTopicRequest
    {
        public string Topic { get; private set; }
        public int? InitialQueueCount { get; private set; }

        public CreateTopicRequest(string topic, int? initialQueueCount = null)
        {
            Topic = topic;
            InitialQueueCount = initialQueueCount;
        }
    }
}
