using System;

namespace NewLife.Queue.Protocols.Brokers.Requests
{
    [Serializable]
    public class GetTopicQueueInfoRequest
    {
        public string Topic { get; private set; }

        public GetTopicQueueInfoRequest(string topic)
        {
            Topic = topic;
        }
    }
}
