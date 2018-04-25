using System;
using NewLife.Data;

namespace NewLife.Net.MQTT.Packets
{
    /// <summary>连接包</summary>
    public sealed class ConnectPacket : DataPacket
    {
        /// <summary>包类型</summary>
        public override PacketType PacketType => PacketType.CONNECT;

        public String ProtocolName { get; set; }

        public Int32 ProtocolLevel { get; set; }

        public Boolean CleanSession { get; set; }

        public Boolean HasWill { get; set; }

        public QualityOfService WillQualityOfService { get; set; }

        public Boolean WillRetain { get; set; }

        public Boolean HasPassword { get; set; }

        public Boolean HasUsername { get; set; }

        public Int32 KeepAliveInSeconds { get; set; }

        public String Username { get; set; }

        public String Password { get; set; }

        public String ClientId { get; set; }

        public String WillTopicName { get; set; }

        public Packet WillMessage { get; set; }
    }
}