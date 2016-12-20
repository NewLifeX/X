using System;

namespace NewLife.Queue.Protocols.Brokers.Requests
{
    [Serializable]
    public class GetConsumerIdsForTopicRequest
    {
        public string GroupName { get; private set; }
        public string Topic { get; private set; }

        public GetConsumerIdsForTopicRequest(string groupName, string topic)
        {
            GroupName = groupName;
            Topic = topic;
        }

        public override string ToString()
        {
            return string.Format("[GroupName:{0}, Topic:{1}]", GroupName, Topic);
        }
    }
}
