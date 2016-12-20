using System;

namespace NewLife.Queue.Protocols.CenterServers.Requests
{
    [Serializable]
    public class DeleteConsumerGroupForClusterRequest
    {
        public string ClusterName { get; set; }
        public string GroupName { get; set; }
    }
}
