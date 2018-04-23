using System;

namespace NewLife.Net.MQTT.Packets
{
    /// <summary>连接响应包</summary>
    public sealed class ConnAckPacket : DataPacket
    {
        /// <summary>包类型</summary>
        public override PacketType PacketType => PacketType.CONNACK;

        /// <summary>会话</summary>
        public Boolean SessionPresent { get; set; }

        /// <summary>响应代码</summary>
        public ConnectReturnCode ReturnCode { get; set; }
    }
}