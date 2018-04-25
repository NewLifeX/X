using System;

namespace NewLife.Net.MQTT.Packets
{
    /// <summary>带ID数据包</summary>
    public abstract class PacketWithId : DataPacket
    {
        /// <summary>包ID</summary>
        public Int32 PacketId { get; set; }
    }
}