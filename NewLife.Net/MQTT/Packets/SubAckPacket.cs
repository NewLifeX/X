using System;
using System.Collections.Generic;

namespace NewLife.Net.MQTT.Packets
{
    /// <summary>发布确认</summary>
    public sealed class SubAckPacket : PacketWithId
    {
        /// <summary>包类型</summary>
        public override PacketType PacketType => PacketType.SUBACK;

        /// <summary>返回代码</summary>
        public IReadOnlyList<QualityOfService> ReturnCodes { get; set; }

        public static SubAckPacket InResponseTo(SubscribePacket subscribePacket, QualityOfService maxQoS)
        {
            var subAckPacket = new SubAckPacket
            {
                PacketId = subscribePacket.PacketId
            };
            var subscriptionRequests = subscribePacket.Requests;
            var returnCodes = new QualityOfService[subscriptionRequests.Count];
            for (var i = 0; i < subscriptionRequests.Count; i++)
            {
                var requestedQos = subscriptionRequests[i].QualityOfService;
                returnCodes[i] = requestedQos <= maxQoS ? requestedQos : maxQoS;
            }

            subAckPacket.ReturnCodes = returnCodes;

            return subAckPacket;
        }
    }
}