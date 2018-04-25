
namespace NewLife.Net.MQTT.Packets
{
    /// <summary>发布确认</summary>
    public sealed class PubAckPacket : PacketWithId
    {
        /// <summary>包类型</summary>
        public override PacketType PacketType => PacketType.PUBACK;

        /// <summary>在响应中</summary>
        public static PubAckPacket InResponseTo(PublishPacket publishPacket)
        {
            return new PubAckPacket
            {
                PacketId = publishPacket.PacketId
            };
        }
    }
}