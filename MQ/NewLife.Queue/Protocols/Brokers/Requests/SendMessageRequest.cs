using System;

namespace NewLife.Queue.Protocols.Brokers.Requests
{
    [Serializable]
    public class SendMessageRequest
    {
        public int QueueId { get; set; }
        public Message Message { get; set; }
        public string ProducerAddress { get; set; }
    }
}
