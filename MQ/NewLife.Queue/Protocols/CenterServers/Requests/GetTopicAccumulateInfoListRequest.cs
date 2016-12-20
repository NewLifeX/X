using System;

namespace NewLife.Queue.Protocols.CenterServers.Requests
{
    [Serializable]
    public class GetTopicAccumulateInfoListRequest
    {
        public long AccumulateThreshold { get; set; }
    }
}
