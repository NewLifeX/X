using System;
using NewLife.Queue.Utilities;

namespace NewLife.Queue.Protocols
{
    [Serializable]
    public class QueueMessage : Message
    {
        public long LogPosition { get; set; }
        public string MessageId { get; set; }
        public int QueueId { get; set; }
        public long QueueOffset { get; set; }
        public DateTime StoredTime { get; set; }
        public string BrokerName { get; set; }
        public string ProducerAddress { get; set; }

        public QueueMessage() { }
        public QueueMessage(string messageId, string topic, int code, byte[] body, int queueId, long queueOffset, DateTime createdTime, DateTime storedTime, string tag, string producerAddress)
            : base(topic, code, body, createdTime, tag)
        {
            MessageId = messageId;
            QueueId = queueId;
            QueueOffset = queueOffset;
            StoredTime = storedTime;
            ProducerAddress = producerAddress;
        }

        public virtual void ReadFrom(byte[] recordBuffer)
        {
            var srcOffset = 0;

            LogPosition = ByteUtil.DecodeLong(recordBuffer, srcOffset, out srcOffset);
            MessageId = ByteUtil.DecodeString(recordBuffer, srcOffset, out srcOffset);
            Topic = ByteUtil.DecodeString(recordBuffer, srcOffset, out srcOffset);
            Tag = ByteUtil.DecodeString(recordBuffer, srcOffset, out srcOffset);
            ProducerAddress = ByteUtil.DecodeString(recordBuffer, srcOffset, out srcOffset);
            Code = ByteUtil.DecodeInt(recordBuffer, srcOffset, out srcOffset);
            Body = ByteUtil.DecodeBytes(recordBuffer, srcOffset, out srcOffset);
            QueueId = ByteUtil.DecodeInt(recordBuffer, srcOffset, out srcOffset);
            QueueOffset = ByteUtil.DecodeLong(recordBuffer, srcOffset, out srcOffset);
            CreatedTime = ByteUtil.DecodeDateTime(recordBuffer, srcOffset, out srcOffset);
            StoredTime = ByteUtil.DecodeDateTime(recordBuffer, srcOffset, out srcOffset);
        }
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(MessageId);
        }

        public override string ToString()
        {
            return string.Format("[Topic={0},QueueId={1},QueueOffset={2},MessageId={3},LogPosition={4},Code={5},CreatedTime={6},StoredTime={7},BodyLength={8},Tag={9},ProducerAddress={10}]",
                Topic,
                QueueId,
                QueueOffset,
                MessageId,
                LogPosition,
                Code,
                CreatedTime,
                StoredTime,
                Body.Length,
                Tag,
                ProducerAddress);
        }
    }
}
