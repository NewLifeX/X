
using System;

namespace NewLife.Net.MQTT.Packets
{
    /// <summary>发布包</summary>
    public sealed class PublishPacket : PacketWithId, IByteBufferHolder
    {
        readonly QualityOfService qos;
        readonly Boolean duplicate;
        readonly Boolean retainRequested;

        public PublishPacket(QualityOfService qos, Boolean duplicate, Boolean retain)
        {
            this.qos = qos;
            this.duplicate = duplicate;
            retainRequested = retain;
        }

        /// <summary>包类型</summary>
        public override PacketType PacketType => PacketType.PUBLISH;

        public override Boolean Duplicate => duplicate;

        public override QualityOfService QualityOfService => qos;

        public override Boolean RetainRequested => retainRequested;

        public String TopicName { get; set; }

        public Packet Payload { get; set; }

        public Int32 ReferenceCount => Payload.ReferenceCount;

        public IReferenceCounted Retain()
        {
            Payload.Retain();
            return this;
        }

        public IReferenceCounted Retain(Int32 increment)
        {
            Payload.Retain(increment);
            return this;
        }

        public IReferenceCounted Touch()
        {
            Payload.Touch();
            return this;
        }

        public IReferenceCounted Touch(Object hint)
        {
            Payload.Touch(hint);
            return this;
        }

        public Boolean Release() => Payload.Release();

        public Boolean Release(Int32 decrement) => Payload.Release(decrement);

        Packet IByteBufferHolder.Content => Payload;

        public IByteBufferHolder Copy() => this.Replace(Payload.Copy());

        public IByteBufferHolder Replace(Packet content)
        {
            var result = new PublishPacket(qos, duplicate, retainRequested);
            result.TopicName = TopicName;
            result.Payload = content;
            return result;
        }

        IByteBufferHolder IByteBufferHolder.Duplicate() => this.Replace(Payload.Duplicate());

        public IByteBufferHolder RetainedDuplicate() => this.Replace(Payload.RetainedDuplicate());
    }
}