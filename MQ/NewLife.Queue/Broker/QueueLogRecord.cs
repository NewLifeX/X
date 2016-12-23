using System;
using System.IO;
using NewLife.Queue.Storage;

namespace NewLife.Queue.Broker
{
    public class QueueLogRecord : ILogRecord
    {
        public long MessageLogPosition { get; private set; }
        public int TagCode { get; private set; }

        public QueueLogRecord() { }
        public QueueLogRecord(long messageLogPosition, int tagCode)
        {
            MessageLogPosition = messageLogPosition;
            TagCode = tagCode;
        }
        public void WriteTo(long logPosition, BinaryWriter writer)
        {
            writer.Write(MessageLogPosition);
            writer.Write(TagCode);
        }
        public void ReadFrom(byte[] recordBuffer)
        {
            MessageLogPosition = BitConverter.ToInt64(recordBuffer, 0);
            TagCode = BitConverter.ToInt32(recordBuffer, 8);
        }
    }
}
