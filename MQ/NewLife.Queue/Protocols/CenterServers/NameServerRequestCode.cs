namespace NewLife.Queue.Protocols.CenterServers
{
    public enum NameServerRequestCode
    {
        RegisterBroker = 10000,
        UnregisterBroker = 10001,
        GetAllClusters = 10002,
        GetClusterBrokers = 10003,
        GetClusterBrokerStatusInfoList = 10004,
        GetTopicRouteInfo = 10005,
        GetTopicQueueInfo = 10006,
        GetTopicConsumeInfo = 10007,
        GetProducerList = 10008,
        GetConsumerList = 10009,
        CreateTopic = 10010,
        DeleteTopic = 10011,
        AddQueue = 10012,
        DeleteQueue = 10013,
        SetQueueProducerVisible = 10014,
        SetQueueConsumerVisible = 10015,
        SetQueueNextConsumeOffset = 10016,
        DeleteConsumerGroup = 10017,
        GetTopicAccumulateInfoList = 10018
    }
}
