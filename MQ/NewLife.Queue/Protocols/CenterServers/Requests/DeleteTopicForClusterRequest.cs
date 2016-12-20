using System;

namespace NewLife.Queue.Protocols.CenterServers.Requests
{
    [Serializable]
    public class DeleteTopicForClusterRequest
    {
        public string ClusterName { get; set; }
        public string Topic { get; set; }
    }
}
