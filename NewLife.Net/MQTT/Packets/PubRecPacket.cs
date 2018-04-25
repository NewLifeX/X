
namespace NewLife.Net.MQTT.Packets
{
    /// <summary>发布</summary>
    public sealed class PubRecPacket : PacketWithId
    {
        /// <summary>包类型</summary>
        public override PacketType PacketType => PacketType.PUBREC;

        /// <summary>在响应中</summary>
        public static PubRecPacket InResponseTo(PublishPacket publishPacket)
        {
            return new PubRecPacket
            {
                PacketId = publishPacket.PacketId
            };
        }
    }
}