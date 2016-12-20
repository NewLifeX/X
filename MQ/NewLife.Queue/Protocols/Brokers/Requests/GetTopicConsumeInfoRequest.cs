using System;

namespace NewLife.Queue.Protocols.Brokers.Requests
{
    [Serializable]
    public class GetTopicConsumeInfoRequest
    {
        public string GroupName { get; private set; }
        public string Topic { get; private set; }

        public GetTopicConsumeInfoRequest(string groupName, string topic)
        {
            GroupName = groupName;
            Topic = topic;
        }
    }
}
