using System;

namespace NewLife.Queue.Protocols.Brokers
{
    [Serializable]
    public class QueueKey : IComparable<QueueKey>, IComparable
    {
        public string Topic { get; set; }
        public int QueueId { get; set; }

        public QueueKey() { }
        public QueueKey(string topic, int queueId)
        {
            Topic = topic;
            QueueId = queueId;
        }

        public static bool operator ==(QueueKey left, QueueKey right)
        {
            return IsEqual(left, right);
        }
        public static bool operator !=(QueueKey left, QueueKey right)
        {
            return !IsEqual(left, right);
        }
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            var other = (QueueKey)obj;

            return Topic == other.Topic && QueueId == other.QueueId;
        }
        public override int GetHashCode()
        {
            return (Topic + QueueId.ToString()).GetHashCode();
        }
        public override string ToString()
        {
            return string.Format("{0}@{1}", Topic, QueueId);
        }

        private static bool IsEqual(QueueKey left, QueueKey right)
        {
            if (ReferenceEquals(left, null) ^ ReferenceEquals(right, null))
            {
                return false;
            }
            return ReferenceEquals(left, null) || left.Equals(right);
        }

        public int CompareTo(QueueKey other)
        {
            return ToString().CompareTo(other.ToString());
        }
        public int CompareTo(object obj)
        {
            return ToString().CompareTo(obj.ToString());
        }
    }
}
