using System;
using System.Collections.Generic;
using NewLife.Queue.Protocols.Brokers;

namespace NewLife.Queue.Protocols.CenterServers
{
    [Serializable]
    public class BrokerConsumerListInfo
    {
        public BrokerInfo BrokerInfo { get; set; }
        public IList<ConsumerInfo> ConsumerList { get; set; }

        public BrokerConsumerListInfo()
        {
            ConsumerList = new List<ConsumerInfo>();
        }
    }
}
