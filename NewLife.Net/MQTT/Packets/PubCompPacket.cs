
namespace NewLife.Net.MQTT.Packets
{
    /// <summary>发布完成</summary>
    public sealed class PubCompPacket : PacketWithId
    {
        /// <summary>包类型</summary>
        public override PacketType PacketType => PacketType.PUBCOMP;

        /// <summary>包类型</summary>
        public static PubCompPacket InResponseTo(PubRelPacket publishPacket)
        {
            return new PubCompPacket
            {
                PacketId = publishPacket.PacketId
            };
        }
    }
}