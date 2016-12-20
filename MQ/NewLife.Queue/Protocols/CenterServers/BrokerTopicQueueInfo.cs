using System;
using System.Collections.Generic;
using NewLife.Queue.Protocols.Brokers;

namespace NewLife.Queue.Protocols.CenterServers
{
    [Serializable]
    public class BrokerTopicQueueInfo
    {
        public BrokerInfo BrokerInfo { get; set; }
        public IList<TopicQueueInfo> TopicQueueInfoList { get; set; }

        public BrokerTopicQueueInfo()
        {
            TopicQueueInfoList = new List<TopicQueueInfo>();
        }
    }
}
