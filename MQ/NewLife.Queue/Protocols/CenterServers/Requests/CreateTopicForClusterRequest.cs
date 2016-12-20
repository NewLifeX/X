using System;

namespace NewLife.Queue.Protocols.CenterServers.Requests
{
    [Serializable]
    public class CreateTopicForClusterRequest
    {
        public string ClusterName { get; set; }
        public string Topic { get; set; }
        public int? InitialQueueCount { get; set; }
    }
}
