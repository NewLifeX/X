using System;

namespace NewLife.Net.MQTT.Packets
{
    /// <summary>数据包</summary>
    public abstract class DataPacket
    {
        /// <summary>包类型</summary>
        public abstract PacketType PacketType { get; }

        /// <summary>双向</summary>
        public virtual Boolean Duplicate => false;

        /// <summary>服务质量</summary>
        public virtual QualityOfService QualityOfService => QualityOfService.AtMostOnce;

        /// <summary>保留请求</summary>
        public virtual Boolean RetainRequested => false;

        /// <summary>已重载</summary>
        public override String ToString() => $"{GetType().Name}[Type={PacketType}, QualityOfService={QualityOfService}, Duplicate={Duplicate}, Retain={RetainRequested}]";
    }
}