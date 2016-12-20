using System;
using NewLife.Queue.Protocols.Brokers;

namespace NewLife.Queue.Protocols.CenterServers
{
    [Serializable]
    public class BrokerStatusInfo
    {
        public BrokerInfo BrokerInfo { get; set; }
        public long TotalSendThroughput { get; set; }
        public long TotalConsumeThroughput { get; set; }
        public long TotalUnConsumedMessageCount { get; set; }
    }
}
