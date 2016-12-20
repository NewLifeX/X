using System;

namespace NewLife.Queue.Protocols.CenterServers.Requests
{
    [Serializable]
    public class GetProducerListRequest
    {
        public string ClusterName { get; set; }
        public bool OnlyFindMaster { get; set; }
    }
}
