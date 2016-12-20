using System;

namespace NewLife.Queue.Protocols.CenterServers.Requests
{
    [Serializable]
    public class GetConsumerListRequest
    {
        public string ClusterName { get; set; }
        public string ConsumerGroup { get; set; }
        public string Topic { get; set; }
        public bool OnlyFindMaster { get; set; }
    }
}
