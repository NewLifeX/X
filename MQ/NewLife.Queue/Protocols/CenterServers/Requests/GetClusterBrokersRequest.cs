using System;

namespace NewLife.Queue.Protocols.CenterServers.Requests
{
    [Serializable]
    public class GetClusterBrokersRequest
    {
        public string ClusterName { get; set; }
        public string Topic { get; set; }
        public bool OnlyFindMaster { get; set; }
    }
}
