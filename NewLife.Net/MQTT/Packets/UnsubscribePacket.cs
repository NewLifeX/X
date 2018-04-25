using System;
using System.Collections.Generic;

namespace NewLife.Net.MQTT.Packets
{
    /// <summary>取消订阅</summary>
    public sealed class UnsubscribePacket : PacketWithId
    {
        public UnsubscribePacket() { }

        public UnsubscribePacket(Int32 packetId, params String[] topicFilters)
        {
            PacketId = packetId;
            TopicFilters = topicFilters;
        }

        /// <summary>包类型</summary>
        public override PacketType PacketType => PacketType.UNSUBSCRIBE;

        /// <summary>服务质量</summary>
        public override QualityOfService QualityOfService => QualityOfService.AtLeastOnce;

        /// <summary>主题过滤器</summary>
        public IEnumerable<String> TopicFilters { get; set; }
    }
}