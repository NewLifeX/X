
using System.Collections.Generic;

namespace NewLife.Net.MQTT.Packets
{
    /// <summary>订阅</summary>
    public sealed class SubscribePacket : PacketWithId
    {
        public SubscribePacket()
        {
        }

        public SubscribePacket(System.Int32 packetId, params SubscriptionRequest[] requests)
        {
            PacketId = packetId;
            Requests = requests;
        }

        /// <summary>包类型</summary>
        public override PacketType PacketType => PacketType.SUBSCRIBE;

        /// <summary>服务质量</summary>
        public override QualityOfService QualityOfService => QualityOfService.AtLeastOnce;

        /// <summary>请求集合</summary>
        public IReadOnlyList<SubscriptionRequest> Requests { get; set; }
    }
}