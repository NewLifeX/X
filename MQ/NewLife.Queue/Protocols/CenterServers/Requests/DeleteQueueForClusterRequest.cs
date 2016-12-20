using System;

namespace NewLife.Queue.Protocols.CenterServers.Requests
{
    [Serializable]
    public class DeleteQueueForClusterRequest
    {
        public string ClusterName { get; set; }
        public string Topic { get; set; }
        public int QueueId { get; set; }
    }
}
