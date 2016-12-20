namespace NewLife.Queue.Protocols.Brokers
{
    public enum BrokerRequestCode
    {
        SendMessage = 10,
        PullMessage = 11,
        BatchSendMessage = 12,
        ProducerHeartbeat = 100,
        ConsumerHeartbeat = 101,
        GetConsumerIdsForTopic = 102,
        UpdateQueueConsumeOffsetRequest = 103,
        GetBrokerStatisticInfo = 1000,
        GetTopicQueueInfo = 1001,
        GetTopicConsumeInfo = 1002,
        GetProducerList = 1003,
        GetConsumerList = 1004,
        CreateTopic = 1005,
        DeleteTopic = 1006,
        AddQueue = 1007,
        DeleteQueue = 1008,
        SetQueueProducerVisible = 1009,
        SetQueueConsumerVisible = 1010,
        SetQueueNextConsumeOffset = 1011,
        DeleteConsumerGroup = 1012,
        GetMessageDetail = 1013,
        GetLastestMessages = 1014
    }
}
