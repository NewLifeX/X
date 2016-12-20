using System;
using NewLife.Queue.Protocols.Brokers;

namespace NewLife.Queue.Protocols.CenterServers.Requests
{
    [Serializable]
    public class BrokerUnRegistrationRequest
    {
        public BrokerInfo BrokerInfo { get; set; }
    }
}
