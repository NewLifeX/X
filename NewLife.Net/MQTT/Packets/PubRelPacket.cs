
namespace NewLife.Net.MQTT.Packets
{
    /// <summary>发布</summary>
    public sealed class PubRelPacket : PacketWithId
    {
        /// <summary>包类型</summary>
        public override PacketType PacketType => PacketType.PUBREL;

        /// <summary>服务质量</summary>
        public override QualityOfService QualityOfService => QualityOfService.AtLeastOnce;

        /// <summary>包类型</summary>
        public static PubRelPacket InResponseTo(PubRecPacket publishPacket)
        {
            return new PubRelPacket
            {
                PacketId = publishPacket.PacketId
            };
        }
    }
}