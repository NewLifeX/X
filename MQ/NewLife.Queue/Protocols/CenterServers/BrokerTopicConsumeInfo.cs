using System;
using System.Collections.Generic;
using NewLife.Queue.Protocols.Brokers;

namespace NewLife.Queue.Protocols.CenterServers
{
    [Serializable]
    public class BrokerTopicConsumeInfo
    {
        public BrokerInfo BrokerInfo { get; set; }
        public IList<TopicConsumeInfo> TopicConsumeInfoList { get; set; }

        public BrokerTopicConsumeInfo()
        {
            TopicConsumeInfoList = new List<TopicConsumeInfo>();
        }
    }
}
