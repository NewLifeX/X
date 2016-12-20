using System;

namespace NewLife.Queue.Protocols.Brokers.Requests
{
    [Serializable]
    public class DeleteTopicRequest
    {
        public string Topic { get; private set; }

        public DeleteTopicRequest(string topic)
        {
            Topic = topic;
        }
    }
}
