using System;
using System.IO;
using System.Text;
using NewLife.Queue.Protocols;
using NewLife.Queue.Storage;
using NewLife.Queue.Utilities;

namespace NewLife.Queue.Broker
{
    [Serializable]
    public class MessageLogRecord : QueueMessage, ILogRecord
    {
        private static readonly byte[] EmptyBytes = new byte[0];
        private readonly Action<MessageLogRecord, object> _callback;
        private readonly object _parameter;

        public MessageLogRecord() { }
        public MessageLogRecord(
            string topic,
            int code,
            byte[] body,
            int queueId,
            long queueOffset,
            DateTime createdTime,
            DateTime storedTime,
            string tag, string producerAddress, Action<MessageLogRecord, object> callback, object parameter)
            : base(null, topic, code, body, queueId, queueOffset, createdTime, storedTime, tag, producerAddress)
        {
            _callback = callback;
            _parameter = parameter;
        }

        public void WriteTo(long logPosition, BinaryWriter writer)
        {
            LogPosition = logPosition;
            MessageId = MessageIdUtil.CreateMessageId(logPosition);

            //logPosition
            writer.Write(LogPosition);

            //messageId
            var messageIdBytes = Encoding.UTF8.GetBytes(MessageId);
            writer.Write(messageIdBytes.Length);
            writer.Write(messageIdBytes);

            //topic
            var topicBytes = Encoding.UTF8.GetBytes(Topic);
            writer.Write(topicBytes.Length);
            writer.Write(topicBytes);

            //tag
            var tagBytes = EmptyBytes;
            if (!string.IsNullOrEmpty(Tag))
            {
                tagBytes = Encoding.UTF8.GetBytes(Tag);
            }
            writer.Write(tagBytes.Length);
            writer.Write(tagBytes);

            //producerAddress
            var producerAddressBytes = Encoding.UTF8.GetBytes(ProducerAddress);
            writer.Write(producerAddressBytes.Length);
            writer.Write(producerAddressBytes);

            //code
            writer.Write(Code);

            //body
            writer.Write(Body.Length);
            writer.Write(Body);

            //queueId
            writer.Write(QueueId);

            //queueOffset
            writer.Write(QueueOffset);

            //createdTime
            writer.Write(CreatedTime.Ticks);

            //storedTime
            writer.Write(StoredTime.Ticks);
        }
        public void OnPersisted()
        {
            if (_callback != null)
            {
                _callback(this, _parameter);
            }
        }
    }
}
