using System;
using System.Collections.Generic;
using NewLife.Queue.Protocols.Brokers;

namespace NewLife.Queue.Protocols.CenterServers
{
    [Serializable]
    public class BrokerProducerListInfo
    {
        public BrokerInfo BrokerInfo { get; set; }
        public IList<string> ProducerList { get; set; }

        public BrokerProducerListInfo()
        {
            ProducerList = new List<string>();
        }
    }
}
