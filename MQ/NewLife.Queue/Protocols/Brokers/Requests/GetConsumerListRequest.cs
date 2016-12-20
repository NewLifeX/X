using System;

namespace NewLife.Queue.Protocols.Brokers.Requests
{
    [Serializable]
    public class GetConsumerListRequest
    {
        public string GroupName { get; private set; }
        public string Topic { get; private set; }

        public GetConsumerListRequest(string groupName, string topic)
        {
            GroupName = groupName;
            Topic = topic;
        }
    }
}
